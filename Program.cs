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
        void Write(Stream stream, uint index);
        void Read(Stream outp, uint index);
        void Free(uint index);
    }
    /// <summary>
    /// Blocks of data to be read fully, must be 128*n size for alignment purposes
    /// </summary>
    public interface IBlock
    {
        void Write(Stream stream, uint index);
        void Read(Stream outp, uint index);
    }
}
