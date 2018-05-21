using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
                Block.LoadToRam();
                Console.WriteLine($"Block handler initialized. {(DateTime.Now.Ticks - timer.Ticks) / 10000f} ms");
                timer = DateTime.Now;
                RecordFormatter Record = new RecordFormatter(Block, path);
                Console.WriteLine($"Record handler intitialized. {(DateTime.Now.Ticks - timer.Ticks) / 10000f} ms");
                timer = DateTime.Now;
                ImageDB Database = new ImageDB(Record, path);
                Console.WriteLine($"Database initialized. {(DateTime.Now.Ticks - timer.Ticks) / 10000f} ms");
                char c;
                int n;
                do
                {
                    Console.WriteLine($"\nLeanDB CLI test.\n usage:\n\tg : generate random items  \tc : do sample computation\n\tr : remove random items\tx : save and exit\n\n[{Database.IndexGuid.Count}] items currently stored\nLeanDB> ");
                    c = Console.ReadKey().KeyChar;
                    Console.Clear();
                    switch (c)
                    {
                        case 'g':
                            Console.Write("Generate random. How many?[int] ");
                            n = int.Parse(Console.ReadLine());
                            timer = DateTime.Now;
                            Parallel.For(0, n, i =>
                            {
                                Database.Insert(GenerateTestImg());
                            });
                            break;


                        case 'c':
                            Console.WriteLine("Testing sample computational work for every item in the database. Sorting by rating");
                            timer = DateTime.Now;
                            List<Tuple<double,Guid>> ratingmap = new List<Tuple<double,Guid>>();
                            Parallel.ForEach(Database.IndexGuid, item =>
                            //foreach (var item in Database.IndexGuid)
                            {
                                Image test = Database.Find(item.Key);
                                Tuple<double,Guid> imgrating = new Tuple<double,Guid>(LowerBound(test.likes,test.dislikes),test.Guid);
                                ratingmap.Add(imgrating);
                            }
                            );
                            ratingmap.Sort();
                            for(int i = 0; i<10; i++)
                            {
                                System.Console.WriteLine(Database.Find(ratingmap[ratingmap.Count-1-i].Item2).ToString() + '\n'); 
                            }
                            break;


                        case 'r':
                            Console.Write($"Delete random items. How many? ");
                            n = int.Parse(Console.ReadLine());
                            Random random = new Random();
                            timer = DateTime.Now;
                            DeleteImages(Database,random.Next(0,Database.indexGuid.Keys.Count - n),n);
                            break;

                        case 'x':
                            Console.WriteLine("Terminating...");
                            timer = DateTime.Now;
                            break;

                        default:
                            Console.WriteLine("ERROR: Invalid input\n");
                            break;
                    }
                    Console.WriteLine($"\t({(DateTime.Now.Ticks - timer.Ticks)/10000f}ms)");
                } while (c != 'x');
                timer = DateTime.Now;
                Database.SaveData();
                Block.CommitChangesToDisk(Database.Location);
                Console.WriteLine($"Data Saved to files {(DateTime.Now.Ticks - timer.Ticks) / 10000f}ms");
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

        static void DeleteImages<T>(Database<T> db , int offset, int count) where T : ILeanDBObject, new()
        {
            var arr = db.IndexGuid.Keys.ToArray();
            Parallel.For(offset, offset + count, i =>
            //for(int i = offset; i < count + offset; i++)
            {
                db.Remove(arr[i]);
            //}
            });
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
