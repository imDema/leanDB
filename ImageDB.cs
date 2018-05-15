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
        IRecord record;
        public IRecord RecordHandler { get  {return record;}}

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
            int index;
            //1. Serialize the object
            using(MemoryStream objStream = new MemoryStream())
            {
                obj.Serialize(objStream);
                //2. Write to the IRecord
                index = record.Write(objStream);
            }
            //3. Index the item in the hashtables


        }

        class RecordFormatter : IRecord
        {
            private Stack<Tuple<int,int>> blockList;
            public Stack<Tuple<int,int>> BlockList
            {
                get{return blockList;}
                set{blockList = value;}
            }
            private Stream dataStream;
            public Stream DataStream { get => dataStream; set => dataStream = value; }
            private int blockSize;
            public int BlockSize { get => blockSize;}

            IBlock brw;
            public IBlock BlockStructure{get{return brw;}}

            public void Free(int index)
            {
                do
                {
                    Tuple<int,int> freed;
                    index = BlockStructure.FreeBlocks(index, out freed);
                    blockList.Push(freed);
                }
                while(index != 0);
            }

            public void Read(Stream outp, int index)
            {
                //Keep reading till next = 0
                int next = index;
                while(next != 0)
                {
                    next = brw.Read(outp, next);
                }
            }
            /// <summary>
            /// Get the index of free blocks from the block list and use IBlock.Write() until the provided stream is exhausted
            /// </summary>
            /// <param name="stream"></param>
            public int Write(Stream stream)
            {
                Tuple<int,int> pos = blockList.Pop();
                Tuple<int,int> blockRemains = writeSub(stream,pos);
                if(blockRemains.Item2 > 0)
                {
                    blockList.Push(blockRemains);
                }
                return pos.Item1;
            }
            /// <summary>
            /// Recursively write on first free group until all the data has been written
            /// </summary>
            /// <param name="stream">Data to write</param>
            /// <param name="blockpos">Position of the starting block group</param>
            /// <returns>A Tuple containing the remains of the last group (Item1 : Index, Item2 : Number of unused elements)</returns>
            private Tuple<int,int> writeSub(Stream stream, Tuple<int,int> blockpos)
            { 
                //Calculate if first block group is enough for the stream
                int left = Convert.ToInt32(stream.Length - stream.Position-1);
                int nblocks = left % brw.ContentSize > 0 ? left/brw.ContentSize + 1 : left/brw.ContentSize;
                //If it is write and return what's remaining of the block group
                if(blockpos.Item2 >= nblocks)
                {
                    brw.Write(stream, 0,  nblocks, blockpos.Item1);
                    return new Tuple<int,int>(blockpos.Item1 + nblocks, blockpos.Item2 - nblocks);
                }
                //If it's not enough get a new block group, write, link to the next one and 'recurse'
                else
                {
                    Tuple<int,int> next = blockList.Pop();
                    brw.Write(stream, next.Item1, blockpos.Item2, blockpos.Item1);
                    return writeSub(stream, next);
                }
            }

            public RecordFormatter(int blockSize)
            {
                this.blockSize = blockSize;
                brw = new BlockRW(blockSize, dataStream);
            }
        }

        public void Update(ILeanDBObject obj)
        {
            throw new NotImplementedException();
        }
    }

    public class BlockRW : IBlock
    {
        int blockSize;
        public int BlockSize{get => blockSize;}

        private int headerSize = sizeof(int) * 2 + sizeof(bool);
        public int HeaderSize { get => headerSize; }

        public int ContentSize{get => blockSize - headerSize;}
        Stream dataStream;
        /// <summary>
        /// Write cont blocks from stream to DataStream, starting from index and linking next in the header
        /// </summary>
        /// <param name="stream">Data to write</param>
        /// <param name="next">Index of the next block to link</param>
        /// <param name="cont">Number of sequential blocks to write</param>
        /// <param name="index">Index of the first block to write</param>
        public void Write(Stream stream, int next, int cont, int index)
        {
            byte[] buffer = new byte[ContentSize];
            using(MemoryStream ms = new MemoryStream(blockSize*cont))
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    while(cont > 0)
                    {
                        bw.Write(next);
                        bw.Write(cont--);
                        bw.Write(true); //Mark as active
                        stream.Read(buffer,0,ContentSize);
                        bw.Write(buffer);
                    }
                }
                Seek(index);
                ms.CopyTo(dataStream);
            }
        }
        /// <summary>
        /// Read block at stream start and follow the headers until finished.
        /// returns next block or 0 if this is last
        /// </summary>
        /// <param name="outp">Stream to write to</param>
        /// <param name="inpt">Stream to read from</param>
        public int Read(Stream outp, int index)
        {
            int next, cont;
            bool active;
            //Go to the index
            Seek(index);
            using(BinaryReader br = new BinaryReader(dataStream))
            {
                //First read the header
                next = br.ReadInt32();
                cont = br.ReadInt32();
                active = br.ReadBoolean();
            }

            //If landed on inactive space exit
            if(!active) throw new Exception($"While reading at index {index} (DataStream.Position = {dataStream.Position}) landed on inactive block!");

            //Copy the content of the first block group one at a time
            byte[] buffer = new byte[ContentSize];
            for (int i = 0; i < cont; i++)
            {
                dataStream.Read(buffer,0,ContentSize);
                outp.Write(buffer,0,ContentSize);
                dataStream.Position += HeaderSize;
            }
            return next;
        }

        public int FreeBlocks(int index, out Tuple<int,int> freed)
        {
            Seek(index);
            int next, cont;
            using (BinaryReader br = new BinaryReader(dataStream))
            {
                next = br.ReadInt32();
                cont = br.ReadInt32();
            }
            Seek(index);
            byte[] deletebuffer = new byte[HeaderSize];
            for(int i = 0; i<cont; i++)
            {
                dataStream.Write(deletebuffer,0,HeaderSize);
                dataStream.Position += ContentSize;
            }
            freed = new Tuple<int,int>(index, cont);
            return next;
        }

        private void Seek(int index)
        {
            dataStream.Position = index * blockSize;
        }

        public BlockRW(int _blockSize, Stream _dataStream)
        {
            blockSize = _blockSize;
            dataStream = _dataStream;
        }
    }

    class HashBlockDictionary : Hashtable
    {
        public void Add(object key, int value)
        {
            //Leggi la lista esistente con chieve 'key', se è nulla list = nuova lista vuota
            List<int> list = this[key] as List<int> ?? new List<int>();

            //Aggiungi alla lista e salva nella table
            list.Add(value);
            this[key] = list;
        }
    }

    class Image : ILeanDBObject
    {
        private Guid guid = Guid.NewGuid();
        public Guid GetGuid() {return guid; }

        public int likes;
        public int dislikes;

        public string imgid;
        public string user;
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