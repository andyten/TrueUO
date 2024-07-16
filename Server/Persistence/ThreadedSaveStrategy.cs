using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Server.Guilds;

namespace Server
{
    public class ThreadedSaveStrategy : ISaveStrategy
    {
        private readonly Queue<Item> _DecayQueue = new();
        private bool _AllFilesSaved = true;
        private readonly List<string> _ExpectedFiles = new();

        public bool Save()
		{
            Thread saveItemsThread = new Thread(SaveItems)
            {
                Name = "Item Save Subset"
            };

            saveItemsThread.Start();

            SaveMobiles();
            SaveGuilds();

            saveItemsThread.Join();
            return _AllFilesSaved;
        }

        public void ProcessDecay()
        {
            while (_DecayQueue.Count > 0)
            {
                Item item = _DecayQueue.Dequeue();

                if (item != null && item.OnDecay())
                {
                    item.Delete();
                }
            }
        }

        private void SaveItems()
        {
            Dictionary<Serial, Item> items = World.Items;
            int itemCount = items.Count;

            List<List<Item>> chunks = new List<List<Item>>();
            int chunkSize = 150000;

            List<Item> currentChunk = new List<Item>();
            int index = 0;

            foreach (var item in items.Values)
            {
                if (index % chunkSize == 0 && currentChunk.Count > 0)
                {
                    chunks.Add(currentChunk);
                    currentChunk = new List<Item>();

                    int currentChuckIndex = chunks.Count - 1;

                    _ExpectedFiles.Add(World.ItemIndexPath.Replace(".idx", $"_{currentChuckIndex.ToString("D" + 8)}.idx"));
                    _ExpectedFiles.Add(World.ItemDataPath.Replace(".bin", $"_{currentChuckIndex.ToString("D" + 8)}.bin"));
                }

                currentChunk.Add(item);
                index++;
            }

            if (currentChunk.Count > 0)
            {
                chunks.Add(currentChunk);
            }

            int totalItemCount = 0;

            using (BinaryFileWriter tdb = new BinaryFileWriter(World.ItemTypesPath, false))
            {
                Parallel.ForEach(chunks, (chunk, state, chunkIndex) =>
                {
                    string idxPath = World.ItemIndexPath.Replace(".idx", $"_{chunkIndex.ToString("D" + 8)}.idx");
                    string binPath = World.ItemDataPath.Replace(".bin", $"_{chunkIndex.ToString("D" + 8)}.bin");

                    using (BinaryFileWriter idx = new BinaryFileWriter(idxPath, false))
                    using (BinaryFileWriter bin = new BinaryFileWriter(binPath, true))
                    {
                        int itemsWritten = 0;
                        idx.Write(chunk.Count);
                        foreach (Item item in chunk)
                        {
                            if (item.Decays && item.Parent == null && item.Map != Map.Internal && (item.LastMoved + item.DecayTime) <= DateTime.UtcNow)
                            {
                                _DecayQueue.Enqueue(item);
                            }

                            long start = bin.Position;

                            idx.Write(item.m_TypeRef);
                            idx.Write(item.Serial);
                            idx.Write(start);

                            item.Serialize(bin);

                            idx.Write((int)(bin.Position - start));

                            item.FreeCache();
                            itemsWritten++;
                        }
                        Interlocked.Add(ref totalItemCount, itemsWritten);
                    }
                });
                tdb.Write(World.m_ItemTypes.Count);

                for (int i = 0; i < World.m_ItemTypes.Count; ++i)
                {
                    tdb.Write(World.m_ItemTypes[i].FullName);
                }

                //Will keep this one for now but will remove at a later date 6/11/2024
                Console.WriteLine($"Saved {totalItemCount} vs Original {itemCount} item count.");

            }
           
            if (totalItemCount != itemCount)
            {
                _AllFilesSaved = false;
                Console.WriteLine($"Expected to save {itemCount}, but only saved {totalItemCount}. Un-threaded Save will be triggered");
            }

            foreach (string item in _ExpectedFiles)
            {
                if (!File.Exists(item))
                {
                    _AllFilesSaved = false;
                    Console.WriteLine($"Save is missing file {item}. Un-threaded Save will be triggered");
                }
            }
        }

        private static void SaveMobiles()
        {
            Dictionary<Serial, Mobile> mobiles = World.Mobiles;

            BinaryFileWriter idx = new BinaryFileWriter(World.MobileIndexPath, false);
            BinaryFileWriter tdb = new BinaryFileWriter(World.MobileTypesPath, false);
            BinaryFileWriter bin = new BinaryFileWriter(World.MobileDataPath, true);

            idx.Write(mobiles.Count);
            foreach (Mobile m in mobiles.Values)
            {
                long start = bin.Position;

                idx.Write(m.m_TypeRef);
                idx.Write(m.Serial);
                idx.Write(start);

                m.Serialize(bin);

                idx.Write((int)(bin.Position - start));

                m.FreeCache();
            }

            tdb.Write(World.m_MobileTypes.Count);

            for (int i = 0; i < World.m_MobileTypes.Count; ++i)
                tdb.Write(World.m_MobileTypes[i].FullName);

            idx.Close();
            tdb.Close();
            bin.Close();
        }

        private static void SaveGuilds()
        {
            BinaryFileWriter idx = new BinaryFileWriter(World.GuildIndexPath, false);
            BinaryFileWriter bin = new BinaryFileWriter(World.GuildDataPath, true);

            idx.Write(BaseGuild.List.Count);
            foreach (BaseGuild guild in BaseGuild.List.Values)
            {
                long start = bin.Position;

                idx.Write(0);//guilds have no typeid
                idx.Write(guild.Id);
                idx.Write(start);

                guild.Serialize(bin);

                idx.Write((int)(bin.Position - start));
            }

            idx.Close();
            bin.Close();
        }
    }
}
