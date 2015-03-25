using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace ThreatMeter
{
    internal class ThreatMeter
    {
        private static Obj_AI_Hero _player;
        public ThreatMeter()
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;    
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            _player = ObjectManager.Player;
            Game.PrintChat("ThreatMeter loaded.");
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (Game.Time % 2 == 0)
            {
                Game.PrintChat(_player.ChampionName);
            }
        }
    }
}
