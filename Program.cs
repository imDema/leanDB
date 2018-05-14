using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace leandb
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }
    }
    public interface ILeanDBObject
    {
        Guid GetGuid();
        void Serialize(Stream outp);
        void Deserialize(Stream inpt);
    }
    /// <summary>
    /// Main database interface
    /// </summary>
    public interface ILeanDB
    {
        int BlockSize{get;}
        string Path{get; set;}
        void Insert (ILeanDBObject obj);
        void Delete (ILeanDBObject obj);
        void Update (ILeanDBObject obj);
        ILeanDBObject Find<T> (T property);
    }
    /// <summary>
    /// Handles block writing, reading and deletion
    /// </summary>
    public interface IRecord
    {
        Stream DataStream{get;set;}
        int BlockSize{get;}
        Stack BlockList{get;set;}
        void Write(Stream stream, uint index);
        void Read(Stream outp, uint index);
        void Free(uint index);
    }
    /// <summary>
    /// Blocks of data to be read fully, must be 128*n size for alignment purposes
    /// </summary>
    public class BlockRW
    {
        int BlockSize;
        int HeaderSize = sizeof(int)*2;
        int ContentSize;

        Stream DataStream;

        void Write(Stream stream, uint index)
        {

        }
        /// <summary>
        /// Read block at stream start and follow the headers until finished.
        /// returns next block or 0 if this is last
        /// </summary>
        /// <param name="outp">Stream to write to</param>
        /// <param name="inpt">Stream to read from</param>
        void Read(Stream outp)
        {
            uint next;
            using(BinaryReader br = new BinaryReader(DataStream))
            {
                //First read the header
                uint contiguos = br.ReadUInt32();
                next = br.ReadUInt32();

                //Copy the content of the first block
                byte[] buffer = new byte[ContentSize];
                DataStream.Read(buffer,0,ContentSize);
                outp.Write(buffer,0,ContentSize);
                //Read contiguos blocks
                for (int i=0; i<contiguos; i++)
                {
                    DataStream.Read(buffer, HeaderSize, ContentSize);
                    outp.Write(buffer,0,ContentSize);
                }
            }
            //Go to next block and keep reading or stop
            if(next != 0)
            {
                Seek(next);
                Read(outp);
            }    
        }

        private void Seek(uint index)
        {
            DataStream.Seek(index * BlockSize, SeekOrigin.Begin);
        }

        public BlockRW(int _blockSize, Stream _dataStream)
        {
            BlockSize = _blockSize;
            ContentSize = BlockSize - HeaderSize;
            DataStream = _dataStream;
        }
    }
}
