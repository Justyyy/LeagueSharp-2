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
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
