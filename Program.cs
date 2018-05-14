using System;
using System.Collections;

namespace leandb
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }
    }

    public interface ILeanDB
    {
        
        void Insert (ILeanDBObject obj);
        void Delete (ILeanDBObject obj);
        void Update (ILeanDBObject obj);
        ILeanDBObject Find (Guid guid);
    }

    public interface ILeanDBObject
    {

    }
}
