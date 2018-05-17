using System;
using System.IO;
namespace leandb
{

    public class BlockRW : IBlock, IDisposable
    {
        readonly string dataPath = "data.ldb";
        readonly int blockSize;
        public int BlockSize{get => blockSize;}

        private readonly int headerSize = sizeof(int) * 2 + sizeof(bool);
        public int HeaderSize { get => headerSize; }

        public int ContentSize{get => blockSize - headerSize;}
        Stream dataStream;
        /// <summary>
        /// Write cont blocks from stream to DataStream, starting from index and linking next in the header
        /// </summary>
        /// <param name="stream">Data to write</param>
        /// <param name="next">Index of the next block to link</param>
        /// <param name="cont">Number of sequential blocks to write</param>
        /// <param name="index">Index of the first block to write</param>
        public void Write(Stream stream, int next, int cont, int index)
        {
            byte[] buffer = new byte[ContentSize];
            using(MemoryStream ms = new MemoryStream(blockSize*cont))
            {
                using (BinaryWriter bw = new BinaryWriter(ms, System.Text.Encoding.UTF8, true))
                {
                    while(cont > 0)
                    {
                        bw.Write(next);
                        bw.Write(cont--);
                        bw.Write(true); //Mark as active
                        stream.Read(buffer,0,ContentSize);
                        bw.Write(buffer);
                    }
                }
                Seek(index);
                ms.WriteTo(dataStream);
            }
        }
        /// <summary>
        /// Read block at stream start and follow the headers until finished.
        /// returns next block or 0 if this is last
        /// </summary>
        /// <param name="outp">Stream to write to</param>
        /// <param name="inpt">Stream to read from</param>
        public int Read(Stream outp, int index)
        {
            int next, cont;
            bool active;
            //Go to the index
            Seek(index);
            using(BinaryReader br = new BinaryReader(dataStream,System.Text.Encoding.UTF8, true))
            {
                //First read the header
                next = br.ReadInt32();
                cont = br.ReadInt32();
                active = br.ReadBoolean();
            }

            //If landed on inactive space exit
            if(!active) throw new Exception($"While reading at index {index} (DataStream.Position = {dataStream.Position}) landed on inactive block!");

            //Copy the content of the first block group one at a time
            byte[] buffer = new byte[ContentSize];
            for (int i = 0; i < cont; i++)
            {
                dataStream.Read(buffer,0,ContentSize);
                outp.Write(buffer,0,ContentSize);
                dataStream.Position += HeaderSize;
            }
            return next;
        }
        /// <summary>
        /// Set sequence of block to free
        /// </summary>
        /// <param name="index">Index of the first block</param>
        /// <param name="freed">Returns the leftover blocks</param>
        /// <returns></returns>
        public int FreeBlocks(int index, out Tuple<int,int> freed)
        {
            Seek(index);
            int next, cont;
            using (BinaryReader br = new BinaryReader(dataStream, System.Text.Encoding.UTF8, true))
            {
                next = br.ReadInt32();
                cont = br.ReadInt32();
            }
            Seek(index);
            byte[] deletebuffer = new byte[HeaderSize];
            for(int i = 0; i<cont; i++)
            {
                dataStream.Write(deletebuffer,0,HeaderSize);
                dataStream.Position += ContentSize;
            }
            freed = new Tuple<int,int>(index, cont);
            return next;
        }

        private void Seek(int index)
        {
            dataStream.Position = index * blockSize;
        }

        public void Dispose()
        {
            dataStream.Dispose();
        }

        public BlockRW(int _blockSize, string path)
        {
            blockSize = _blockSize;
            dataStream = File.Open(Path.Combine(path, dataPath),FileMode.OpenOrCreate,FileAccess.ReadWrite);
        }
    }
}