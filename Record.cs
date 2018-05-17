using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
namespace leandb 
{
    public class RecordFormatter : IRecord
    {
        private Stack<Tuple<int,int>> blockList;
        public Stack<Tuple<int,int>> BlockList
        {
            get{return blockList;}
            set{blockList = value;}
        }
        private readonly string location;

        public string Location
        {
            get { return location; }
        }


        IBlock blockHandler;
        public IBlock BlockStructure{get{return blockHandler;}}

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
            outp.Position = 0;
            //Keep reading till next = 0
            int next = index;
            while(next != 0)
            {
                next = blockHandler.Read(outp, next);
            }
            outp.Position = 0;
        }
        /// <summary>
        /// Get the index of free blocks from the block list and use IBlock.Write() until the provided stream is exhausted
        /// </summary>
        /// <param name="stream"></param>
        public int Write(Stream stream)
        {
            Tuple<int,int> pos = blockList.Pop();
            Tuple<int,int> blockRemains = WriteSub(stream,pos);
            if(blockRemains.Item2 != 0)
            {
                blockList.Push(blockRemains);
            }
            return pos.Item1;
        }
        /// <summary>
        /// Recursively write on first free group until all the data has been written
        /// </summary>
        /// <param name="stream">Data to write</param>
        /// <param name="freeBlocks">Position of the starting block group</param>
        /// <returns>A Tuple containing the remains of the last group (Item1 : Index, Item2 : Number of unused elements)</returns>
        private Tuple<int,int> WriteSub(Stream stream, Tuple<int,int> freeBlocks)
        { 
            //Calculate if first block group is enough for the stream
            int left = Convert.ToInt32(stream.Length - stream.Position-1);
            int nblocks = left % blockHandler.ContentSize > 0 ? left/blockHandler.ContentSize + 1 : left/blockHandler.ContentSize;
            //If tail space just write and return new tail index
            if(freeBlocks.Item2 == -1)
            {
                blockHandler.Write(stream, 0,  nblocks, freeBlocks.Item1);
                return new Tuple<int, int>(freeBlocks.Item1 + nblocks, -1);
            }
            //If enough there is enough space in the sequence write and return any leftover
            if(freeBlocks.Item2 >= nblocks)
            {
                blockHandler.Write(stream, 0,  nblocks, freeBlocks.Item1);
                return new Tuple<int,int>(freeBlocks.Item1 + nblocks, freeBlocks.Item2 - nblocks);
            }
            //If it's not enough get a new block group, write, link to the next one and 'recurse'
            else
            {
                Tuple<int,int> next = blockList.Pop();
                blockHandler.Write(stream, next.Item1, freeBlocks.Item2, freeBlocks.Item1);
                return WriteSub(stream, next);
            }
        }

        private void InitBlockList()
        {
            if(File.Exists(location))
            {
                using(FileStream fs = File.OpenRead(location))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    blockList = bf.Deserialize(fs) as Stack<Tuple<int,int>> ?? throw new ArgumentNullException($"File {location} does not contain a valid Indexer");
                }
            }
            else
            {
                blockList = new Stack<Tuple<int,int>>();
                blockList.Push(new Tuple<int,int>(0,-1));
            }
        }
        private void SaveBlockList()
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (FileStream fs = File.Open(location, FileMode.Create, FileAccess.Write))
            {
                bf.Serialize(fs, BlockList);
            }
        }

        public void SaveData()
        {
            SaveBlockList();
        }

        public RecordFormatter(IBlock blockHandler, string path)
        {
            this.blockHandler = blockHandler;
            location = Path.Combine(path, "blocks.ldbl");
            InitBlockList();
        }
    }
}