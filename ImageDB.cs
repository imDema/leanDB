using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Threading.Tasks;

namespace leandb
{
    class ImageDB : Database<Image>
    {
            // Custom indexes
        /// <summary>
        /// Index by poster username
        /// </summary>
        IndexerInt indexUser = new IndexerInt();
        private readonly string FilenameIndexUser = "iuser.ldbi";
        private string PathIndexUser { get => GetIndexPath(FilenameIndexUser); }
        public List<Image> SelectByUser(int user)
        {
            List<int> indexes = indexUser[user];
            List<Image> images = new List<Image>();
            //Parallel
            Parallel.ForEach(indexes, (index) =>
            {
                using (MemoryStream ms = new MemoryStream())
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

        //TODO IMPLEMENT DATE INDEX
        private readonly string FilenameIndexDate = "idate.ldbi";
        private string PathIndexDate { get => GetIndexPath(FilenameIndexDate); }

        /// <summary>
        /// Index by image tags
        /// </summary>
        IndexerString indexTag = new IndexerString();
        private readonly string FilenameIndexTag = "itag.ldbi";
        private string PathIndexTag { get => GetIndexPath(FilenameIndexTag); }


        //Abstract method implementation
        public override void AddToIndexes(Image obj, int index)
        {
            indexUser.Add(obj.user, index);
            //TODO IMPLEMENT INDEXDATE
            foreach (string tag in obj.tags)
            {
                indexTag.Add(tag, index);
            }
        }

        public override void RemoveFromIndexes(Image obj, int index)
        {
            indexUser.Remove(obj.user, index);
            //TODO IMPLEMENT INDEXDATE
            foreach (string tag in obj.tags)
            {
                indexTag.Remove(tag, index);
            }
        }

        public override void SaveIndexes()
        {
            using (FileStream fs = File.Open(PathIndexUser, FileMode.Create, FileAccess.Write))
            {
                indexUser.Serialize(fs);
            }

            using (FileStream fs = File.Open(PathIndexTag, FileMode.Create, FileAccess.Write))
            {
                indexTag.Serialize(fs);
            }

            //TODO IMPLEMENT INDEXDATE
            //using (FileStream fs = File.Open(DateIndexLocation, FileMode.Create, FileAccess.Write))
            //{
            //    bf.Serialize(fs, indexDate);
            //}
        }

        public override void LoadIndexes()
        {
            BinaryFormatter bf = new BinaryFormatter();
            //USER (INT)
            if (File.Exists(PathIndexUser))
            {
                using (FileStream fs = File.OpenRead(PathIndexUser))
                {
                    indexUser = new IndexerInt(fs);
                }
            }
            else indexUser = new IndexerInt();

            //TAG (STRING)
            if (File.Exists(PathIndexTag))
            {
                using (FileStream fs = File.OpenRead(PathIndexTag))
                {
                    indexTag = new IndexerString(fs);
                }
            }
            else indexTag = new IndexerString();

            //TODO IMPLEMENT INDEX DATE
        }

        //Constructors
        public ImageDB(IRecord record, string path)
        {
            this.location = path;
            this.record = record;
            LoadData();
        }
    }

    abstract class Indexer<T> : Dictionary<T,List<int>>
    {
        /// <summary>
        /// Add the index to the list at the key position or create new
        /// </summary>
        /// <param name="key">Property to look for</param>
        /// <param name="index">Index to add</param>
        public void Add(T key, int index)
        {
            List<int> list;
            //Leggi la lista esistente con chieve 'key', se è nulla list = nuova lista vuota
            if (this.ContainsKey(key))
                list = this[key];
            else
                list = new List<int>();

            //Aggiungi alla lista e salva nella table
            list.Add(index);
            this[key] = list;
        }
        /// <summary>
        /// Go to specified key and remove from list the specified index.
        /// If the list is empty delete the dictionary entry
        /// </summary>
        /// <param name="key">Property to look for</param>
        /// <param name="index">Index to delete</param>
        public void Remove(T key, int index)
        {
            this[key].Remove(index);
            if(this[key].Count == 0)
            {
                this.Remove(key);
            }
        }
        public void Serialize(Stream outp)
        {
            using (BinaryWriter bw = new BinaryWriter(outp, System.Text.Encoding.UTF8, true))
            {
                bw.Write(Count);
                foreach (KeyValuePair<T, List<int>> item in this)
                {
                    //Serialize key
                    SerializeKey(item, bw);
                    //Serialize value
                    WriteList(bw, item.Value);
                }
            }
        }
        public void Deserialize(Stream inpt)
        {
            using (BinaryReader br = new BinaryReader(inpt, System.Text.Encoding.UTF8, true))
            {
                int cnt = br.ReadInt32();
                for (int i = 0; i < cnt; i++)
                {
                    //Deserialize key
                    T key = DeserializeKey(br);
                    //Deserialize value
                    List<int> value = new List<int>();
                    ReadList(value, br);
                    //Add to dictionary
                    Add(key, value);
                }
            }
        }
        internal abstract void SerializeKey(KeyValuePair<T, List<int>> item, BinaryWriter bw);
        internal abstract T DeserializeKey(BinaryReader br);
        internal void WriteList(BinaryWriter bw, List<int> list)
        {
            bw.Write(list.Count);
            foreach (int n in list)
            {
                bw.Write(n);
            }
        }
        internal void ReadList(List<int> list, BinaryReader br)
        {
            for (int i = 0, n = br.ReadInt32(); i < n; i++)
            {
                list.Add(br.ReadInt32());
            }
        }
        public Indexer(Stream stream) : base()
        {
            Deserialize(stream);
        }
        public Indexer() : base() { }
    }
    class IndexerInt : Indexer<int>
    {
        internal override int DeserializeKey(BinaryReader br)
        {
            return br.ReadInt32();
        }
        internal override void SerializeKey(KeyValuePair<int, List<int>> item, BinaryWriter bw)
        {
            bw.Write(item.Key);
        }
        public IndexerInt() : base() { }
        public IndexerInt(Stream stream) : base(stream) { }
    }
    class IndexerString : Indexer<string>
    {
        internal override string DeserializeKey(BinaryReader br)
        {
            return br.ReadString();
        }
        internal override void SerializeKey(KeyValuePair<string, List<int>> item, BinaryWriter bw)
        {
            bw.Write(item.Key);
        }
        public IndexerString() : base() { }
        public IndexerString(Stream stream) : base(stream) { }
    }

    class Image : ILeanDBObject
    {
        private Guid guid = Guid.NewGuid();
        public Guid Guid { get => guid; }

        public int likes;
        public int dislikes;
        

        public string imgid;
        public int user;
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
                user = br.ReadInt32();
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