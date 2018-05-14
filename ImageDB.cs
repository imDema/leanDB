using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace leandb
{
    class ImageDB : ILeanDB
    {
        HashBlockDictionary IndexGuid = new HashBlockDictionary();
        HashBlockDictionary IndexUser = new HashBlockDictionary();
        HashBlockDictionary IndexTag = new HashBlockDictionary();

        private int blockSize = 128;
        public int BlockSize
        {
            get { return blockSize;}
        }
        private string path;
        public string Path
        {
            get { return path;}
            set { path = value;}
        }
        RecordFormatter record;

        public void Delete(ILeanDBObject obj)
        {
            throw new NotImplementedException();
        }

        public ILeanDBObject Find<T>(T property)
        {
            throw new NotImplementedException();
        }

        public void Insert(ILeanDBObject obj)
        {

            //1. Serialize the object
            MemoryStream objStream = new MemoryStream();
            obj.Serialize(objStream);

            //2. Write the record
            
            //3. Index the item in the hashtables


        }

        class RecordFormatter : IRecord
        {
            private Stack<uint> blockList;
            public Stack<uint> BlockList
            {
                get{return blockList;}
                set{blockList = value;}
            }
            private Stream dataStream;
            public Stream DataStream { get => dataStream; set => dataStream = value; }
            private int blockSize;
            public int BlockSize { get => blockSize;}

            BlockRW brw;

            public void Free(uint index)
            {
                throw new NotImplementedException();
            }

            public void Read(Stream outp, uint index)
            {
                //Keep reading till next = 0
                uint next = index;
                while(next != 0)
                {
                    Seek(index);
                    next = brw.Read(outp);
                }
            }

            public void Write(Stream stream)
            {
                uint pos = blockList.Pop();
                uint next = 0;
                do
                {
                    using (MemoryStream ts = new MemoryStream())
                    {
                        uint cont = brw.Write(stream,ts);
                        ts.Seek(0,SeekOrigin.Begin);
                        
                        using (BinaryWriter bw = new BinaryWriter(dataStream))
                        {
                            if(stream.Position < stream.Length-1)
                            {
                                next = blockList.Pop();
                            }
                            bw.Write(next);
                            bw.Write(cont);
                        }
                        Seek(pos);
                        ts.CopyTo(dataStream);
                        pos = next;
                    }
                } while(next != 0);
            }

            private void Seek(uint index)
            {
                dataStream.Seek(index * blockSize, SeekOrigin.Begin);
            }

            private void Link(uint pos, uint next, uint contiguos)
            {
                Seek(pos);
                using (BinaryWriter bw = new BinaryWriter(dataStream))
                {
                    bw.Write(next);
                    bw.Write(contiguos);
                }
            }

            public RecordFormatter(int blockSize)
            {
                this.blockSize = blockSize;
                brw = new BlockRW(blockSize, dataStream),
            }
        }

        public void Update(ILeanDBObject obj)
        {
            throw new NotImplementedException();
        }
    }

    class HashBlockDictionary : Hashtable
    {
        public void Add(object key, uint value)
        {
            //Leggi la lista esistente con chieve 'key', se è nulla list = nuova lista vuota
            List<uint> list = this[key] as List<uint> ?? new List<uint>();

            //Aggiungi alla lista e salva nella table
            list.Add(value);
            this[key] = list;
        }
    }


    [Serializable]
    class Image : ILeanDBObject
    {
        private Guid guid = Guid.NewGuid();
        public Guid GetGuid() {return guid; }

        public uint likes;
        public uint dislikes;

        public string imgid;
        public string user;
        public List<string> tags;

        /// <summary>
        /// Serialize this item to the output stream
        /// </summary>
        /// <param name="outp">Stream to serialize to</param>
        public void Serialize(Stream outp)
        {
            using (BinaryWriter bw = new BinaryWriter(outp))
            {
                bw.Write(guid.ToByteArray());

                bw.Write(likes);
                bw.Write(dislikes);

                bw.Write(imgid);
                bw.Write(user);

                bw.Write(tags.Count);
                foreach (string str in tags)
                {
                    bw.Write(str);
                }
            }
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

                likes = br.ReadUInt32();
                dislikes = br.ReadUInt32();

                imgid = br.ReadString();
                user = br.ReadString();

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