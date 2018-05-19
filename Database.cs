using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace leandb
{
    abstract class Database<T> where T : ILeanDBObject, new()
    {
        internal string location;
        /// <summary>
        /// Directory where the indexes are to be stored
        /// </summary>
        public string Location { get => location; }

        public string FilenameIndexGuid = "iguid.ldbi";
        public string PathIndexGuid { get => GetIndexPath(FilenameIndexGuid); }

        internal Dictionary<Guid, int> indexGuid;
        /// <summary>
        /// Main index. Each Guid corresponds to one and only one index that point to the first block where the object is stored.
        /// </summary>
        public Dictionary<Guid, int> IndexGuid { get => indexGuid; }

        internal IRecord record;
        /// <summary>
        /// Translates indexes into the object they point at
        /// </summary>
        public IRecord RecordHandler { get { return record; } }

        /// <summary>
        /// Commissions item storage to RecordHandler and Indexes it
        /// </summary>
        /// <param name="obj">Object to insert</param>
        public void Insert(T obj)
        {
            int index;
            if (indexGuid.ContainsKey(obj.Guid)) throw new Exception("Item already exists in database");
            //1. Serialize the object
            using (MemoryStream objStream = new MemoryStream())
            {
                obj.Serialize(objStream);
                //2. Write to the IRecord
                index = record.Write(objStream);
            }
            //3. Index the item in the hashtables
            indexGuid.Add(obj.Guid, index);
            AddToIndexes(obj, index);
        }
        /// <summary>
        /// Commissions item deletion to RecordHandler
        /// </summary>
        /// <param name="guid">Guid of the item to delete</param>
        public void Remove(Guid guid)
        {
            T obj = Select(guid);
            int index = indexGuid[guid];
            record.Free(index);
            indexGuid.Remove(guid);
            RemoveFromIndexes(obj, index);
        }
        public void Remove (T obj) { Remove(obj.Guid); }
        /// <summary>
        /// Get item with matching Guid from Indexes and retreive it with RecordHandler, commission deletion of old version and storage of new one
        /// </summary>
        /// <param name="obj">Object to update</param>
        public void Update(T obj)
        {
            Remove(obj);
            Insert(obj);
        }

        /// <summary>
        /// Lookup an item in the indexes and read from record
        /// </summary>
        /// <param name="guid">GUID of the item to retrieve</param>
        /// <returns></returns>
        public T Select(Guid guid)
        {
            int index = indexGuid[guid];
            T obj;
            using (MemoryStream ms = new MemoryStream())
            {
                record.Read(ms, index);
                obj = new T();
                obj.Deserialize(ms);
            }
            return obj;
        }

        /// <summary>
        /// Add the object to all custom indexes
        /// </summary>
        /// <param name="obj">Object to add to indexes</param>
        /// <param name="index">Index to assign</param>
        public abstract void AddToIndexes(T obj, int index);
        /// <summary>
        /// Remove the object from all custom indexes
        /// </summary>
        /// <param name="obj">object to remove from all custom indexes</param>
        public abstract void RemoveFromIndexes(T obj, int index);
        
        /// <summary>
        /// Save index data to file
        /// </summary>
        public void SaveData()
        {
            SaveIndexGuid();
            SaveIndexes();
            record.SaveData();
        }
        public void LoadData()
        {
            LoadIndexGuid();
            LoadIndexes();
        }
        internal void SaveIndexGuid()
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (FileStream fs = File.Open(GetIndexPath(FilenameIndexGuid), FileMode.Create, FileAccess.Write))
            {
                bf.Serialize(fs, IndexGuid);
            }
        }
        /// <summary>
        /// Load IndexGuid from the file at PathIndexGuid if file doesn't exist sets IndexGuid to new()
        /// </summary>
        internal void LoadIndexGuid()
        {
            //GUID
            BinaryFormatter bf = new BinaryFormatter();
            string igpth = GetIndexPath(FilenameIndexGuid);
            if (File.Exists(igpth))
            {
                using (FileStream fs = File.OpenRead(igpth))
                {
                    indexGuid = bf.Deserialize(fs) as Dictionary<Guid, int> ?? throw new ArgumentNullException($"File {igpth} does not contain a valid {indexGuid.GetType()}");
                }
            }
            else indexGuid = new Dictionary<Guid, int>();
        }
        /// <summary>
        /// Save all custom indexes to files (it's recommended to save them in the Location directory)
        /// </summary>
        public abstract void SaveIndexes();
        /// <summary>
        /// Load all custom indexes from files to local variables
        /// </summary>
        public abstract void LoadIndexes();

        internal string GetIndexPath(string indexFilename)
        {
            return Path.Combine(Location, indexFilename);
        }
    }
}
