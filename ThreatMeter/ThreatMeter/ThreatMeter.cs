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

                //Add the target selector.
                //TargetSelector.AddToMenu(Config.SubMenu("Selector"));

                //Add the orbwalking.
                //Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalker"));

                //Load the crosshair
                //Crosshair.Load();

                //Check if the champion is supported
                //try
                //{
                //    var type = Type.GetType("ProSeries.Champions." + Player.ChampionName);
                //    if (type != null)
                //        type.GetMethod("Load").Invoke(null, null);
                //}
                //catch (NullReferenceException)
                //{
                //    Game.PrintChat(Player.ChampionName + " is not supported yet! however the orbwalking will work");
                //}

                //Add ADC items usage.
                //ItemManager.Load();

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
