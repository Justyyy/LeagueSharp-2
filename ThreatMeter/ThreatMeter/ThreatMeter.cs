using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace ThreatMeter
{
    internal static class ThreatMeter
    {
        internal static Menu Config;
        internal static Orbwalking.Orbwalker Orbwalker;
        internal static Obj_AI_Hero Player;

        internal static void Load()
        {
            try
            {
                Player = ObjectManager.Player;
                
                //Print the welcome message
                Game.PrintChat("Threat Meter Loaded!");

                //Load the menu.
                Config = new Menu("ThreatMeter", "ThreatMeter", true);

                //Add the menu as main menu.
                Config.AddToMainMenu();
                CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            
        }
    }
}
