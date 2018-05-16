using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
namespace leandb 
{
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
            outp.Position = 0;
            //Keep reading till next = 0
            int next = index;
            while(next != 0)
            {
                next = brw.Read(outp, next);
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
}