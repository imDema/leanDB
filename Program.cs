using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace leandb
{
    class Program
    {
        static void Main(string[] args)
        {
            DateTime timer = DateTime.Now;
            string path = Path.GetDirectoryName( System.Reflection.Assembly.GetEntryAssembly().Location);
            using(BlockRW Block = new BlockRW(256, path))
            {
                Console.WriteLine($"Block handler initialized. {(DateTime.Now.Ticks - timer.Ticks) / 10000f} ms");
                timer = DateTime.Now;
                RecordFormatter Record = new RecordFormatter(Block, path);
                Console.WriteLine($"Record handler intitialized. {(DateTime.Now.Ticks - timer.Ticks) / 10000f} ms");
                timer = DateTime.Now;
                ImageDB Database = new ImageDB(Record, path);
                Console.WriteLine($"Database initialized. {(DateTime.Now.Ticks - timer.Ticks) / 10000f} ms\n[{Database.IndexGuid.Count}] items currently stored\n");
                Console.Write("Generate random images?[int] ");
                int n = int.Parse(Console.ReadLine());
                for (int i = 0; i<n; i++)
                {
                    Database.Insert(GenerateTestImg());
                }
                //Console.WriteLine("Testing find by guid of 200 random guids");
                //Random random = new Random();
                //Guid[] guids = new Guid[200];
                //for (int i = 0; i < guids.Length; i++)
                //{
                //    guids[i] = Database.IndexGuid.ToArray()[random.Next(0, Database.IndexGuid.Count)].Key;
                //}
                //timer = DateTime.Now;
                //foreach (Guid g in guids)
                //{
                //    Database.Select(g);
                //}
                //Console.WriteLine($"Done {(DateTime.Now.Ticks - timer.Ticks) / 100f} ms");
                Console.WriteLine($"[{Database.IndexGuid.Count}] items in database.\nTesting simple calculation for all items");
                timer = DateTime.Now;
                Image test;
                double cdis = 0;
                foreach(var item in Database.IndexGuid)
                {
                    test = Database.Select(item.Key);
                    cdis += LowerBound(test.likes, test.dislikes);
                }
                Console.WriteLine($"Done {(DateTime.Now.Ticks - timer.Ticks) / 10000f}ms\nResult: average lower bound bayesian rating = {cdis / Database.IndexGuid.Count}");
                timer = DateTime.Now;
                Database.SaveData();
                Console.WriteLine($"Data Saved to files {(DateTime.Now.Ticks - timer.Ticks) / 10000f}ms");
                Console.ReadKey();
            }
        }
        static Image GenerateTestImg()
        {
            Random random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            string[] tags = { "analisi1", "mauri_luca", "ghilardi_dino", "analisi2", "migliavacca_cristian", "elettrotecnica", "gal", "metrangolo_pierangelo", "internet_e_reti", "bramanti_marco", "trifoglio", "bizzarri_federico", "boella_marco", "chimica", "fisica" };
            Image img = new Image()
            {
                likes = random.Next(0, 250),
                dislikes = random.Next(0, 200),
                imgid = new string(Enumerable.Repeat(chars, 64).Select(s => s[random.Next(s.Length)]).ToArray()),
                user = random.Next(),
                date = new DateTime(random.Next(2017, 2019), random.Next(1, 12), random.Next(1, 28), random.Next(0, 24), random.Next(0, 60), random.Next(0, 60)),
                tags = new List<string>()
            };
            for(int i = 0, n = random.Next(1,4); i < n; i++)
            {
                img.tags.Add(tags[random.Next(0, tags.Length)]);
            }
            return img;
        }

        static double LowerBound(int pos, int neg)
        {
            int n = pos + neg + 5;
            double z = 1.51d;
            //double p = (double)pos/n;
            //Bayesian
            const double a = 4d, b = 1d;
            double p = (pos + a) / (n + a + b);
            //End Bayesian
            if (n == 0) return 0;
            return (p + (z * z) / (2 * n) - z * System.Math.Sqrt((p * (1 - p) + z * z / (4 * n)) / n)) / (1 + z * z / n);
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
