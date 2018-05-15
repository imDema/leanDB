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
        IRecord RecordHandler{get;}
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
        Stack<uint> BlockList{get;set;}
        /// <summary>
        /// Write data to free blocks listed in the Blocklist
        /// </summary>
        /// <param name="stream">Data to write to blocks</param>
        void Write(Stream stream);
        /// <summary>
        /// Read data from continuous and linked blocks
        /// </summary>
        /// <param name="outp"></param>
        /// <param name="index"></param>
        void Read(Stream outp, uint index);
        /// <summary>
        /// Set block and linked + contiguous as free and push them to the stack
        /// </summary>
        /// <param name="index"></param>
        void Free(uint index);
    }
    /// <summary>
    /// Blocks of data to be read fully, must be 128*n size for alignment purposes
    /// </summary>
    public class BlockRW
    {
        int BlockSize;
        int HeaderSize = sizeof(int)*2 + sizeof(bool);
        int ContentSize;

        Stream DataStream;
        /// <summary>
        /// Write to contiguous blocks and return the number of blocks after the first
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public uint Write(Stream stream, MemoryStream tempoutp)
        {

        }
        /// <summary>
        /// Read block at stream start and follow the headers until finished.
        /// returns next block or 0 if this is last
        /// </summary>
        /// <param name="outp">Stream to write to</param>
        /// <param name="inpt">Stream to read from</param>
        public uint Read(Stream outp)
        {
            uint next;
            using(BinaryReader br = new BinaryReader(DataStream))
            {
                //First read the header
                next = br.ReadUInt32();
                uint contiguous = br.ReadUInt32();
                bool active = br.ReadBoolean();

                //If landed on inactive space exit
                if(!active) return 0;

                //Copy the content of the first block
                byte[] buffer = new byte[ContentSize];
                DataStream.Read(buffer,0,ContentSize);
                outp.Write(buffer,0,ContentSize);
                //Read contiguous blocks
                for (int i=0; i<contiguous; i++)
                {
                    DataStream.Read(buffer, HeaderSize, ContentSize);
                    outp.Write(buffer,0,ContentSize);
                }
            }
            return next;
        }

        uint FreeBlock()
        {
            long Beginning = DataStream.Position;
            using (BinaryReader br = new BinaryReader(DataStream))
            {
                uint next = br.ReadUInt32();
                uint cont = br.ReadUInt32();
                DataStream.Seek(Beginning, SeekOrigin.Begin);
                byte[] deletebuffer = new byte[HeaderSize];
                DataStream.Write(deletebuffer,0,0);
                DataStream.Write(deletebuffer, ContentSize,cont);
            }
        }

        public BlockRW(int _blockSize, Stream _dataStream)
        {
            BlockSize = _blockSize;
            ContentSize = BlockSize - HeaderSize;
            DataStream = _dataStream;
        }
    }
}
