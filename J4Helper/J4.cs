﻿using System;
using LeagueSharp;
using LeagueSharp.Common;

namespace J4Helper
{
    internal class J4
    {
        public static Obj_AI_Hero Player;
        public const string ChampionName = "JarvanIV";
        public static Spell Q, W, E, R;
        public static Orbwalking.Orbwalker Orbwalker;
        public static Menu Config;

        public J4()
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            Player = ObjectManager.Player;
            if (Player.ChampionName != ChampionName) return;
            Q = new Spell(SpellSlot.Q, 700f);
            Q.SetSkillshot(0.5f, 70f, float.MaxValue, false, SkillshotType.SkillshotLine);

            E = new Spell(SpellSlot.E, 830f);
            E.SetSkillshot(0.5f, 70f, float.MaxValue, false, SkillshotType.SkillshotCircle);
            Config = new Menu("J4Helper", "J4Helper", true);
            //Orbwalker
            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            //Misc
            Config.AddSubMenu(new Menu("Keys", "Keys"));
            Config.SubMenu("Keys")
                .AddItem(
                    new MenuItem("EQMouse", "EQ to Mouse").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
            Config.AddToMainMenu();
            Game.OnUpdate += Game_OnUpdate;
            Game.PrintChat("J4Helper Loaded.");
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (Config.Item("EQMouse").IsActive())
            {
                FlagSwag();
            }
        }

        private static void FlagSwag()
        {
            Game.PrintChat("test");
            if (Config.Item("UseEQF").GetValue<bool>() && Q.IsReady() && Q.IsReady())
            {
                E.Cast(Game.CursorPos);
                Q.Cast(Game.CursorPos);
            }
        }
    }
}
