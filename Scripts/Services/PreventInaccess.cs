using System.Collections.Generic;

namespace Server.Misc
{
    /*
    * This system prevents the inability for server staff to
    * access their server due to data overflows during login.
    *
    * Whenever a staff character's NetState is disposed right after
    * the login process, the character is moved to and logged out
    * at a "safe" alternative.
    *
    * The location the character was moved from will be reported
    * to the player upon the next successful login.
    *
    * This system does not affect non-staff players.
    */
    public static class PreventInaccess
    {
        public static readonly bool Enabled = true;

        private static readonly LocationInfo[] _Destinations =
        [
            new LocationInfo(new Point3D(5275, 1163, 0), Map.Felucca), // Jail
            new LocationInfo(new Point3D(5275, 1163, 0), Map.Trammel),
            new LocationInfo(new Point3D(5445, 1153, 0), Map.Felucca), // Green acres
            new LocationInfo(new Point3D(5445, 1153, 0), Map.Trammel)
        ];

        private static Dictionary<Mobile, LocationInfo> _MoveHistory;

        public static void Initialize()
        {
            _MoveHistory = new Dictionary<Mobile, LocationInfo>();
        }

        public static void OnLogin(Mobile from)
        {
            if (from == null || from.IsPlayer())
            {
                return;
            }

            if (HasDisconnected(from))
            {
                if (!_MoveHistory.ContainsKey(from))
                {
                    _MoveHistory[from] = new LocationInfo(from.Location, from.Map);
                }

                LocationInfo dest = GetRandomDestination();

                from.Location = dest.Location;
                from.Map = dest.Map;
            }
            else if (_MoveHistory.TryGetValue(from, out LocationInfo orig))
            {
                from.SendMessage($"Your character was moved from {orig.Location} ({orig.Map}) due to a detected client crash.");

                _MoveHistory.Remove(from);
            }
        }

        private static bool HasDisconnected(Mobile m)
        {
            return m.NetState == null || m.NetState.Socket == null;
        }

        private static LocationInfo GetRandomDestination()
        {
            return _Destinations[Utility.Random(_Destinations.Length)];
        }

        private class LocationInfo(Point3D loc, Map map)
        {
            public Point3D Location { get; } = loc;
            public Map Map { get; } = map;
        }
    }
}
