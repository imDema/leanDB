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
            string path = Path.GetDirectoryName( System.Reflection.Assembly.GetEntryAssembly().Location);
            using(BlockRW Block = new BlockRW(256, path))
            {
                Console.WriteLine("Block handler initialized.");
                RecordFormatter Record = new RecordFormatter(Block, path);
                Console.WriteLine("Record handler intitialized.");
                ImageDB Database = new ImageDB(Record, path);
                Console.WriteLine("Database initialized.");

                //Testimg
                Image testimg0 = new Image()
                {
                    likes = 45,
                    dislikes = 5,
                    imgid = "telegram:img47ae5fd47be6b5c74",
                    user = "a3f65da56bc",
                    date = new DateTime(2018, 03, 24),
                    tags = new List<string>(new string[] { "daniele_braga", "trifoglio" })
                };

                Image testimg1 = new Image()
                {
                    likes = 24,
                    dislikes = 10,
                    imgid = "telegram:img5658de4c5237d5fe5",
                    user = "65f97cd68e9",
                    date = new DateTime(2018, 03, 17),
                    tags = new List<string>(new string[] { "luca_mauri", "algebra", "test" })
                };

                Image testimg2 = new Image()
                {
                    likes = 103,
                    dislikes = 25,
                    imgid = "telegram:img581f57d3c57abe754b",
                    user = "589c67de6f4",
                    date = new DateTime(2018, 05, 15),
                    tags = new List<string>(new string[] { "gruosso_giambattista" })
                };

                Database.Insert(testimg0);
                Database.Insert(testimg1);
                Console.WriteLine("Added test images.");
                Console.WriteLine("Finding image by GUID test:");
                Console.WriteLine(Database.Select(testimg1.Guid).ToString());
                Database.SaveData();
                Console.WriteLine("Data Saved to files");
            }
        }
    }

    public interface ILeanDBObject
    {
        Guid Guid{get;}
        void Serialize(Stream outp);
        void Deserialize(Stream inpt);
    }
    /// <summary>
    /// Main database interface
    /// </summary>
    public interface ILeanDB<T>
    {
        string Location{get;}
        IRecord RecordHandler{get;}
        Dictionary<Guid,int> IndexGuid{get;}
        /// <summary>
        /// Use record to insert the object in the database and index it
        /// </summary>
        /// <param name="obj">Object to insert</param>
        void Insert (T obj);
        /// <summary>
        /// Remove the object with matching guid from the database
        /// </summary>
        /// <param name="guid">Guid of the item to delete</param>
        void Remove (Guid guid);
        /// <summary>
        /// Find the object with matching guid and update its properties
        /// </summary>
        /// <param name="obj">Object to update</param>
        void Update (T obj);
        /// <summary>
        /// Lookup an item in the indexes and read from record
        /// </summary>
        /// <param name="guid">GUID of the item to retrieve</param>
        /// <returns></returns>
        T Select (Guid guid);
        /// <summary>
        /// Save index data to file
        /// </summary>
        void SaveData();
    }
    /// <summary>
    /// Handles block writing, reading and deletion
    /// </summary>
    public interface IRecord
    {
        /// <summary>
        /// Tuple contains index and continuous free space
        /// </summary>
        Stack<Tuple<int,int>> BlockList{get;set;}
        IBlock BlockStructure{get;}
        /// <summary>
        /// Write data to free blocks listed in the Blocklist
        /// </summary>
        /// <param name="stream">Data to write to blocks</param>
        /// <returns>Returns index where the item was stored</returns>
        int Write(Stream stream);
        /// <summary>
        /// Read data from continuous and linked blocks
        /// </summary>
        /// <param name="outp"></param>
        /// <param name="index"></param>
        void Read(Stream outp, int index);
        /// <summary>
        /// Set block and linked + contiguous as free and push them to the stack
        /// </summary>
        /// <param name="index"></param>
        void Free(int index);
        /// <summary>
        /// Save blocklist data
        /// </summary>
        void SaveData();
    }
    /// <summary>
    /// Blocks of data to be read fully, must be 128*n size for alignment purposes
    /// </summary>
    public interface IBlock : IDisposable
    {
        int BlockSize{get;}
        int HeaderSize{get;}
        int ContentSize{get;}

        /// <summary>
        /// Write data to sequential blocks starting at index
        /// </summary>
        /// <param name="stream">Data to write</param>
        /// <param name="next">Index of next block</param>
        /// <param name="cont">Number of blocks to write</param>
        /// <param name="index">Index of starting block</param>
        void Write(Stream stream, int next, int cont, int index);
        /// <summary>
        /// Read data starting from index and return the next block sequence index
        /// </summary>
        /// <param name="outp">Stream were the  read data is written</param>
        /// <param name="index">Index of the first block to read</param>
        /// <returns>Returns the index of the linked block sequence</returns>
        int Read(Stream outp, int index);

        /// <summary>
        /// Set contiguous blocks as free
        /// </summary>
        /// <param name="index">Index of starting block</param>
        /// <param name="freed">Returns the block sequence marked as free</param>
        /// <returns>Returns header "next" parameter</returns>
        int FreeBlocks(int index, out Tuple<int,int> freed);
    }
}
