using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Threading.Tasks;

namespace leandb
{
    class ImageDB : ILeanDB<Image>
    {
        Dictionary<Guid,int> indexGuid;
        public Dictionary<Guid,int> IndexGuid {get => indexGuid; }
        Indexer<string> indexUser = new Indexer<string>();
        //TODO IMPLEMENT indexDate
        Indexer<string> indexTag = new Indexer<string>();

        private string location;
        public string Location {get => location;}
        IRecord record;
        public IRecord RecordHandler { get  {return record;}}

        private readonly string indexGuidPath = "iguid.ldi";
        private readonly string indexUserPath = "iuser.ldi";
        private readonly string indexDatePath = "idate.ldi";
        private readonly string indexTagPath = "itag.ldi";

        public void Remove(Image obj)
        {
            Remove(obj.Guid);
        }
        public void Remove(Guid guid)
        {
            Image img = Select(guid);
            int index = indexGuid[guid];
            record.Free(index);            
            indexGuid.Remove(guid);
            indexUser.Remove(img.user, index);
            foreach(string tag in img.tags)
            {
                indexTag.Remove(tag, index);
            }
        }

        public Image Select(Guid guid)
        {
            int index = indexGuid[guid];
            Image img;
            using (MemoryStream ms = new MemoryStream())
            {
                record.Read(ms, index);
                img = new Image(ms);
            }
            return img;
        }
        public List<Image> SelectUser(string user)
        {
            List<int> indexes = indexUser[user];
            List<Image> images = new List<Image>();
            //Parallel
            Parallel.ForEach(indexes,(index) =>
            {
                using(MemoryStream ms = new MemoryStream())
                {
                    record.Read(ms, index);
                    images.Add(new Image(ms));
                }
            });
            //Sequential
            // foreach(int index in indexes )
            // {
            //     using(MemoryStream ms = new MemoryStream())
            //     {
            //         record.Read(ms, index);
            //         images.Add(new Image(ms));
            //     }
            // }
            return images;
        }

        public void Insert(Image obj)
        {
            int index;
            //1. Serialize the object
            using(MemoryStream objStream = new MemoryStream())
            {
                obj.Serialize(objStream);
                //2. Write to the IRecord
                index = record.Write(objStream);
            }
            //3. Index the item in the hashtables
            indexGuid.Add(obj.Guid, index);
            indexUser.Add(obj.user, index);
            foreach (string tag in obj.tags)
            {
                indexTag.Add(tag, index);
            }
        }

        public void Update(Image obj)
        {
            Remove(obj);
            Insert(obj);
        }
        public ImageDB(IRecord record, string path)
        {
            this.location = path;
            this.record = record;
            InitIndexes(path);
        }

        private void InitIndexes(string path)
        {
            BinaryFormatter bf = new BinaryFormatter();
            string pathGuid = Path.Combine(path, indexGuidPath);
            if(File.Exists(pathGuid))
            {
                using(FileStream fs = File.OpenRead(pathGuid))
                {
                    indexGuid = bf.Deserialize(fs) as Dictionary<Guid,int> ?? throw new ArgumentNullException($"File {pathGuid} does not contain a valid Indexer");
                }
            }
            else  indexGuid = new Dictionary<Guid,int>();


            string pathUser = Path.Combine(path, indexUserPath);
            if (File.Exists(pathUser))
            {
                using(FileStream fs = File.OpenRead(pathUser))
                {
                    indexUser = bf.Deserialize(fs) as Indexer<string> ?? throw new ArgumentNullException($"File {pathUser} does not contain a valid Indexer");
                }
            }
            else indexUser = new Indexer<string>();


            string pathTag = Path.Combine(path, indexTagPath);
            if (File.Exists(pathTag))
            {
                using(FileStream fs = File.OpenRead(pathTag))
                {
                    indexTag = bf.Deserialize(fs) as Indexer<string> ?? throw new ArgumentNullException($"File {pathTag} does not contain a valid Indexer");
                }
            }
            else indexTag = new Indexer<string>();
        }
    }

    class Indexer<T> : Dictionary<T,List<int>>
    {
        public void Add(T key, int value)
        {
            //Leggi la lista esistente con chieve 'key', se è nulla list = nuova lista vuota
            List<int> list = this[key] as List<int> ?? new List<int>();

            //Aggiungi alla lista e salva nella table
            list.Add(value);
            this[key] = list;
        }
        public void Remove(T key, int value)
        {
            this[key].Remove(value);
            if(this[key].Count == 0)
            {
                this.Remove(key);
            }
        }
    }

    class Image : ILeanDBObject
    {
        private Guid guid = Guid.NewGuid();
        public Guid Guid { get => guid; }

        public int likes;
        public int dislikes;
        

        public string imgid;
        public string user;
        public DateTime date;
        public List<string> tags;

        /// <summary>
        /// Serialize this item to the output stream
        /// </summary>
        /// <param name="outp">Stream to serialize to</param>
        public void Serialize(Stream outp)
        {
            outp.Position = 0;
            using (BinaryWriter bw = new BinaryWriter(outp))
            {
                bw.Write(guid.ToByteArray());

                bw.Write(likes);
                bw.Write(dislikes);

                bw.Write(imgid);
                bw.Write(user);
                bw.Write(date.Ticks);

                bw.Write(tags.Count);
                foreach (string str in tags)
                {
                    bw.Write(str);
                }
            }
            outp.Position = 0;
        }
        /// <summary>
        /// Deserialize the provided stream to this Image
        /// </summary>
        /// <param name="inpt">Stream to deserialize from</param>
        public void Deserialize(Stream inpt)
        {
            using (BinaryReader br = new BinaryReader(inpt))
            {
                guid = new Guid(br.ReadBytes(16)); //sizeof(Guid)

                likes = br.ReadInt32();
                dislikes = br.ReadInt32();

                imgid = br.ReadString();
                user = br.ReadString();
                date = new DateTime(br.ReadInt64());

                tags = new List<string>();
                int tagCount = br.ReadInt32();
                for (int i = 0; i < tagCount; i++)
                {
                    tags.Add(br.ReadString());
                }
            }
        }

        public Image(){}

        public Image(Stream stream)
        {
            Deserialize(stream);
        }
    }
}