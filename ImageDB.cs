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
        //Custom indexes
        IndexerInt indexUser = new IndexerInt();
        //TODO IMPLEMENT indexDate
        IndexerString indexTag = new IndexerString();


        private readonly string GuidIndexLocation;
        private readonly string UserIndexLocation;
        private readonly string DateIndexLocation;
        private readonly string TagIndexLocation;

        public List<Image> SelectUser(int user)
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

        private void InitIndexes()
        {
            BinaryFormatter bf = new BinaryFormatter();
            
            
            
            //USER (INT)
            if (File.Exists(UserIndexLocation))
            {
                using(FileStream fs = File.OpenRead(UserIndexLocation))
                {
                    indexUser = new IndexerInt(fs);
                }
            }
            else indexUser = new IndexerInt();

            //TAG (STRING)
            if (File.Exists(TagIndexLocation))
            {
                using(FileStream fs = File.OpenRead(TagIndexLocation))
                {
                    indexTag = new IndexerString(fs);
                }
            }
            else indexTag = new IndexerString();

            //TODO IMPLEMENT INDEX DATE
        }
        private void WriteIndexes()
        {
            using (FileStream fs = File.Open(UserIndexLocation, FileMode.Create, FileAccess.Write))
            {
                indexUser.Serialize(fs);
            }
            
            using (FileStream fs = File.Open(TagIndexLocation, FileMode.Create, FileAccess.Write))
            {
                indexTag.Serialize(fs);
            }
            
            //using (FileStream fs = File.Open(DateIndexLocation, FileMode.Create, FileAccess.Write))
            //{
            //    bf.Serialize(fs, indexDate);
            //}
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

    abstract class Indexer<T> : Dictionary<T,List<int>>
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