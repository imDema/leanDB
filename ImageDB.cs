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
            Stack blockList;
            BlockFormatter blockFormatter;

            public void Free(uint index)
            {
                throw new NotImplementedException();
            }

            public void Read(Stream outp, uint index)
            {
                throw new NotImplementedException();
            }

            public void Write(Stream stream, uint index)
            {
                throw new NotImplementedException();
            }
        }

        public void Update(ILeanDBObject obj)
        {
            throw new NotImplementedException();
        }
    }

    class BlockFormatter : IBlock
    {
        Stream DataStream;
        uint BlockSize; 

        public void Read(Stream outp, uint index)
        {
            throw new NotImplementedException();
        }

        public void Write(Stream stream, uint index)
        {
            throw new NotImplementedException();
        }
        
        public BlockFormatter(Stream dataStream, uint blockSize)
        {
            DataStream = dataStream;
            BlockSize = blockSize;
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