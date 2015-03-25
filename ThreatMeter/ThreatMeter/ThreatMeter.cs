using System;
using LeagueSharp;
using LeagueSharp.Common;

namespace ThreatMeter
{
    internal class Tm
    {
        public static Obj_AI_Hero Player;

        public Tm()
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {

        }
    }

}
