using System;
using System.Collections;
using System.Collections.Generic;

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
        ILeanDBObject Find<T> (T property);
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

    public interface IBlockStorage
    {

    }


    public interface ILeanDBObject
    {

    }
}
