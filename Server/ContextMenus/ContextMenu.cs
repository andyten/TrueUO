#region References
using System;
using System.Collections.Generic;
#endregion

namespace Server.ContextMenus
{
	/// <summary>
	///     Represents the state of an active context menu. This includes who opened the menu, the menu's focus object, and a list of
	///     <see
	///         cref="ContextMenuEntry">
	///         entries
	///     </see>
	///     that the menu is composed of.
	///     <seealso cref="ContextMenuEntry" />
	/// </summary>
	public sealed class ContextMenu : IDisposable
	{
		public bool IsDisposed { get; private set; }

		/// <summary>
		///     Gets the <see cref="Mobile" /> who opened this ContextMenu.
		/// </summary>
		public Mobile From { get; private set; }

		/// <summary>
		///     Gets an object of the <see cref="Mobile" /> or <see cref="Item" /> for which this ContextMenu is on.
		/// </summary>
		public IEntity Target { get; private set; }

		/// <summary>
		///     Gets the list of <see cref="ContextMenuEntry">entries</see> contained in this ContextMenu.
		/// </summary>
		public ContextMenuEntry[] Entries { get; private set; }

		/// <summary>
		///     Instantiates a new ContextMenu instance.
		/// </summary>
		/// <param name="from">
		///     The <see cref="Mobile" /> who opened this ContextMenu.
		///     <seealso cref="From" />
		/// </param>
		/// <param name="target">
		///     The <see cref="Mobile" /> or <see cref="Item" /> for which this ContextMenu is on.
		///     <seealso cref="Target" />
		/// </param>
		public ContextMenu(Mobile from, IEntity target)
		{
			From = from;
			Target = target;

			List<ContextMenuEntry> list = new List<ContextMenuEntry>();

			if (target is Mobile mobile)
			{
				mobile.GetContextMenuEntries(from, list);
			}
			else if (target is Item item)
			{
				item.GetContextMenuEntries(from, list);
			}

            for (var index = 0; index < list.Count; index++)
            {
                ContextMenuEntry e = list[index];

                e.Owner = this;
            }

            Entries = list.ToArray();

			list.Clear();
			list.TrimExcess();
		}

		~ContextMenu()
		{
			Dispose();
		}

        public void Dispose()
		{
			if (IsDisposed)
			{
				return;
			}

			IsDisposed = true;

			if (Entries != null)
            {
                for (var index = 0; index < Entries.Length; index++)
                {
                    ContextMenuEntry e = Entries[index];

                    if (e != null)
                    {
                        e.Dispose();
                    }
                }

                Entries = null;
            }

			if (From != null)
			{
				if (From.ContextMenu == this)
				{
					From.ContextMenu = null;
				}

				From = null;
			}

			Target = null;
		}

		public static bool Display(Mobile m, IEntity target)
		{
			if (m == null || target == null || m.Map != target.Map)
			{
				return false;
			}

			if (target is Mobile && !Utility.InUpdateRange(m, target.Location))
			{
				return false;
			}

			if (target is Item item && !Utility.InUpdateRange(m, item.GetWorldLocation()))
			{
				return false;
			}

			if (!m.CheckContextMenuDisplay(target))
			{
				return false;
			}

			ContextMenu c = new ContextMenu(m, target);

			if (c.Entries.Length <= 0)
			{
				return false;
			}

			if (target is Item i)
			{
				object root = i.RootParent;

				if (root is Mobile mobile && mobile != m && mobile.AccessLevel >= m.AccessLevel)
                {
                    for (var index = 0; index < c.Entries.Length; index++)
                    {
                        ContextMenuEntry e = c.Entries[index];

                        if (!e.NonLocalUse)
                        {
                            e.Enabled = false;
                        }
                    }
                }
			}

			m.ContextMenu = c;

			return true;
		}

		/// <summary>
		/// Returns the proper index of Enhanced Client Context Menu when sent from the icon on 
		/// the vendors status bar. Only known are Bank, Bulk Order Info and Bribe
		/// </summary>
		/// <param name="index">pre-described index sent by client. Must be 0x64 or higher</param>
		/// <returns>actual index of pre-desribed index from client</returns>
		public int GetIndexEC(int index)
		{
			int number = index;

			switch (index)
			{
				case 0x0078:
				number = 3006105;
				break;   // Bank
				case 0x0193:
				number = 3006152;
				break;   // Bulk Order Info
				case 0x01A3:
				number = 1152294;
				break;   // Bribe
				case 0x032A:
				number = 3000197;
				break;   // Add Party Member
				case 0x032B:
				number = 3000198;
				break;   // Remove Party Member
				case 0x012D:
				number = 3006130;
				break;   // Tame
				case 0x082:
				number = 3006107;
				break;    // Command: Guard
				case 0x083:
				number = 3006108;
				break;    // Command: Follow
				case 0x086:
				number = 3006111;
				break;    // Command: Kill
				case 0x087:
				number = 3006114;
				break;    // Command: Stay
				case 0x089:
				number = 3006112;
				break;    // Command: Stop
				case 0x0140:
				number = 1113797;
				break;   // Enable PVP Warning TODO: Not Enabled
				case 0x025A:
				number = 3006205;
				break;   // Release Co-Ownership TODO: Not Enabled
				case 0x025C:
				number = 3006207;
				break;   // Leave House
				case 0x0196:
				number = 3006156;
				break;   // Quest Conversation
				case 0x0194:
				number = 3006154;
				break;   // View Quest Log
				case 0x0195:
				number = 3006155;
				break;   // Cancel Quest
				case 0x0321:
				number = 3006169;
				break;   // Toggle Quest Item
				case 0x01A0:
				number = 1114299;
				break;   // Open Item Insurance Menu
				case 0x01A2:
				number = 3006201;
				break;   // Toggle Item Insurance
				case 0x0396:
				number = 1115022;
				break;   // Open Titles Menu
				case 0x0393:
				number = 1049594;
				break;   // Loyalty Rating
				case 0x0134:
				number = 3006157;
				break;   // Cancel Protection
				case 0x03F2:
				number = 1152531;
				break;   // Void Pool
				case 0x03F5:
				number = 1154112;
				break;   // Allow Trades
				case 0x03F6:
				number = 1154113;
				break;   // Refuse Trades
				case 0x0334:
				number = 3006168;
				break;   // Siege Bless Item
			}

			if (index >= 0x64)
			{
				for (int i = 0; i < Entries.Length; i++)
				{
					if (Entries[i].Number == number)
					{
						return i;
					}
				}
			}

			return index;
		}
	}
}
