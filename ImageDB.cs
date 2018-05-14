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

            //2. Write the record

            //3. Index the item in the hashtables


        }

        public void Update(ILeanDBObject obj)
        {
            throw new NotImplementedException();
        }
    }

    public interface ILeanDBObject
    {
        Guid GetGuid();
    }

    [Serializable]
    class Image : ILeanDBObject
    {
        private Guid guid = Guid.NewGuid();
        public Guid GetGuid() {return guid; }

        public uint likes;
        public uint dislikes;

        public string path;
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
                path = tmp.path;
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