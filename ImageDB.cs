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

        
        public void Serialize(Stream outp)
        {
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(outp, this);
        }
        public void Deserialize(Stream inpt)
        {
            BinaryFormatter bf = new BinaryFormatter();
            Image tmp = bf.Deserialize(inpt) as Image;
            if(tmp != null)
            {
                guid = tmp.GetGuid();
                likes = tmp.likes;
                dislikes = tmp.dislikes;
                imgid = tmp.imgid;
                tags = tmp.tags;
            }
        }

        public Image(){}

        public Image(Stream stream)
        {
            Deserialize(stream);
        }
    }
}