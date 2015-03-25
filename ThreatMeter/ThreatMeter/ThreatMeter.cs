using System;
using LeagueSharp;
using LeagueSharp.Common;

namespace ThreatMeter
{
    internal class Tm
    {
        private static Obj_AI_Hero _player;
        public Tm()
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
