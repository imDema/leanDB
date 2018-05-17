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

        private readonly string GuidIndexLocation;
        private readonly string UserIndexLocation;
        private readonly string DateIndexLocation;
        private readonly string TagIndexLocation;

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
            if (indexGuid.ContainsKey(obj.Guid)) throw new Exception("Item already exists in database");
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

        private void InitIndexes()
        {
            BinaryFormatter bf = new BinaryFormatter();
            
            if(File.Exists(GuidIndexLocation))
            {
                using(FileStream fs = File.OpenRead(GuidIndexLocation))
                {
                    indexGuid = bf.Deserialize(fs) as Dictionary<Guid,int> ?? throw new ArgumentNullException($"File {GuidIndexLocation} does not contain a valid Indexer");
                }
            }
            else  indexGuid = new Dictionary<Guid,int>();
            
            if (File.Exists(UserIndexLocation))
            {
                using(FileStream fs = File.OpenRead(UserIndexLocation))
                {
                    indexUser = bf.Deserialize(fs) as Indexer<string> ?? throw new ArgumentNullException($"File {UserIndexLocation} does not contain a valid Indexer");
                }
            }
            else indexUser = new Indexer<string>();

            if (File.Exists(TagIndexLocation))
            {
                using(FileStream fs = File.OpenRead(TagIndexLocation))
                {
                    indexTag = bf.Deserialize(fs) as Indexer<string> ?? throw new ArgumentNullException($"File {TagIndexLocation} does not contain a valid Indexer");
                }
            }
            else indexTag = new Indexer<string>();

            //TODO IMPLEMENT INDEX DATE
        }
        private void WriteIndexes()
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (FileStream fs = File.Open(GuidIndexLocation, FileMode.Create, FileAccess.Write))
            {
                bf.Serialize(fs, IndexGuid);
            }

            using (FileStream fs = File.Open(UserIndexLocation, FileMode.Create, FileAccess.Write))
            {
                bf.Serialize(fs, indexUser);
            }
            
            using (FileStream fs = File.Open(TagIndexLocation, FileMode.Create, FileAccess.Write))
            {
                bf.Serialize(fs, indexTag);
            }
            
            //using (FileStream fs = File.Open(DateIndexLocation, FileMode.Create, FileAccess.Write))
            //{
            //    bf.Serialize(fs, indexDate);
            //}
        }

        public void SaveData()
        {
            WriteIndexes();
            record.SaveData();
        }

        public ImageDB(IRecord record, string path)
        {
            this.location = path;
            this.record = record;
            GuidIndexLocation = Path.Combine(location, "guid.ldbi");
            UserIndexLocation = Path.Combine(location, "user.ldbi");
            TagIndexLocation = Path.Combine(location, "tag.ldbi");
            DateIndexLocation = Path.Combine(location, "date.ldbi");
            InitIndexes();
        }
    }

    [Serializable]
    class Indexer<T> : Dictionary<T,List<int>>, ISerializable
    {
        public void Add(T key, int value)
        {
            List<int> list;
            //Leggi la lista esistente con chieve 'key', se è nulla list = nuova lista vuota
            if (this.ContainsKey(key))
                list = this[key];
            else
                list = new List<int>();

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

        public override string ToString()
        {
            string tagstring = "";
            foreach (string s in tags)
            {
                tagstring += s + ",";
            }
            tagstring.TrimEnd(',');
            return String.Format($"GUID:\t{guid.ToString()}\npos:\t{likes}\nneg:\t{dislikes}\nimgid:\t{imgid}\nuser:\t{user}\ndate:\t{date.ToString()}\ntags\t{tagstring}");
        }
        /// <summary>
        /// Serialize this item to the output stream
        /// </summary>
        /// <param name="outp">Stream to serialize to</param>
        public void Serialize(Stream outp)
        {
            outp.Position = 0;
            using (BinaryWriter bw = new BinaryWriter(outp,System.Text.Encoding.UTF8 ,true))
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
            using (BinaryReader br = new BinaryReader(inpt, System.Text.Encoding.UTF8, true))
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