using Server.Items;
using System.Collections.Generic;
using System.IO;

namespace Server.Commands
{
    public class SignParserDelete
    {
        private static readonly Queue<Item> m_ToDelete = new Queue<Item>();
        public static void Initialize()
        {
            CommandSystem.Register("SignGenDelete", AccessLevel.Administrator, SignGenDelete_OnCommand);
        }

        [Usage("SignGenDelete")]
        [Description("Deletes world/shop signs on all facets.")]
        public static void SignGenDelete_OnCommand(CommandEventArgs c)
        {
            WeakEntityCollection.Delete("sign");
            // Retained for backward compatibility
            Parse(c.Mobile);
        }

        public static void Parse(Mobile from)
        {
            string cfg = Path.Combine(Core.BaseDirectory, "Data/signs.cfg");

            if (File.Exists(cfg))
            {
                List<SignEntry> list = new List<SignEntry>();
                from.SendMessage("Deleting signs, please wait.");

                using (StreamReader ip = new StreamReader(cfg))
                {
                    string line;

                    while ((line = ip.ReadLine()) != null)
                    {
                        string[] split = line.Split(' ');

                        SignEntry e = new SignEntry(new Point3D(Utility.ToInt32(split[2]), Utility.ToInt32(split[3]), Utility.ToInt32(split[4])),
                            Utility.ToInt32(split[1]), Utility.ToInt32(split[0]));

                        list.Add(e);
                    }
                }

                Map[] brit = { Map.Felucca, Map.Trammel };
                Map[] fel = { Map.Felucca };
                Map[] tram = { Map.Trammel };
                Map[] ilsh = { Map.Ilshenar };
                Map[] malas = { Map.Malas };
                Map[] tokuno = { Map.Tokuno };
                Map[] termur = { Map.TerMur };

                for (int i = 0; i < list.Count; ++i)
                {
                    SignEntry e = list[i];
                    Map[] maps = null;

                    switch (e.m_Map)
                    {
                        case 0:
                            maps = brit;
                            break; // Trammel and Felucca
                        case 1:
                            maps = fel;
                            break;  // Felucca
                        case 2:
                            maps = tram;
                            break; // Trammel
                        case 3:
                            maps = ilsh;
                            break; // Ilshenar
                        case 4:
                            maps = malas;
                            break; // Malas
                        case 5:
                            maps = tokuno;
                            break; // Tokuno Islands
                        case 6:
                            maps = termur;
                            break; // Ter Mur
                    }

                    for (int j = 0; maps != null && j < maps.Length; ++j)
                        Delete_Static(e.m_ItemID, e.m_Location, maps[j]);
                }

                from.SendMessage("Sign deleting complete.");
            }
            else
            {
                from.SendMessage("{0} not found!", cfg);
            }
        }

        public static void Delete_Static(int itemID, Point3D location, Map map)
        {
            IPooledEnumerable eable = map.GetItemsInRange(location, 0);

            foreach (Item item in eable)
            {
                if (item is Sign && item.Z == location.Z && item.ItemID == itemID)
                    m_ToDelete.Enqueue(item);
            }

            eable.Free();

            while (m_ToDelete.Count > 0)
                m_ToDelete.Dequeue().Delete();
        }

        private class SignEntry
        {
            public readonly Point3D m_Location;
            public readonly int m_ItemID;
            public readonly int m_Map;
            public SignEntry(Point3D pt, int itemID, int mapLoc)
            {
                m_Location = pt;
                m_ItemID = itemID;
                m_Map = mapLoc;
            }
        }
    }
}
