using System;
using System.Linq;
using LeagueSharp;
using SharpDX;
using LeagueSharp.Common;
using System.Collections.Generic;

namespace Tristana
{
    internal class Program
    {
        public const string ChampionName = "Tristana";
        public static Orbwalking.Orbwalker Orbwalker;
        public static Spell Q, W, E, R;
        public static Menu Config;
        private static Obj_AI_Hero Player;

        public static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            Player = ObjectManager.Player;
            if (Player.BaseSkinName != ChampionName) return;
            Game.PrintChat("Loading 'Rocket Girl Tristana'...");
            Q = new Spell(SpellSlot.Q, 550);
            W = new Spell(SpellSlot.W, 900);
            E = new Spell(SpellSlot.E, 550);
            R = new Spell(SpellSlot.R, 550);

            Config = new Menu("RocketGirl Tristana", ChampionName, true);

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));

            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(32, KeyBindType.Press)));

            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("HarassActive", "Harass").SetValue(new KeyBind(67, KeyBindType.Press)));

            Config.AddSubMenu(new Menu("Misc", "Misc"));
            Config.SubMenu("Misc").AddItem(new MenuItem("UseAntiGapcloser", "R on Gapclose").SetValue(true));

            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;

            Game.OnGameUpdate += Game_OnGameUpdate;
            Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;
            Config.AddToMainMenu();
            Game.PrintChat("'Rocket Girl Tristana' Loaded!");

        }

        private static void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            var sender = gapcloser.Sender;
            if (Config.Item("UseAntiGapcloser").GetValue<bool>() == true)
            {
                if (sender.IsValidTarget(R.Range))
                {
                    R.CastOnUnit(sender);
                }
            }
        }



        private static void Game_OnGameUpdate(EventArgs args)
        {
            Q.Range = 541 + 9 * (Player.Level - 1);
            E.Range = 541 + 9 * (Player.Level - 1);
            R.Range = 541 + 9 * (Player.Level - 1);
            if (Config.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                Combo();
            }
            if (Config.Item("HarassActive").GetValue<KeyBind>().Active)
            {
                Harass();
            }
        }

        private static void Harass()
        {
            var useE = Config.Item("UseEHarass").GetValue<bool>();
            var target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
            if (target == null)
            {
                return;
            }
            else
            {
                if (useE && E.IsReady()) { E.Cast(target); }
            }
        }

        private static void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
        }

        private static void Orbwalking_AfterAttack(Obj_AI_Base unit, Obj_AI_Base target)
        {
        }

        private static void CheckForExecute()
        {
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget(R.Range)))
            {
                if (R.IsReady() && Damage.IsKillable(ObjectManager.Player, enemy, new[] { Tuple.Create<SpellSlot, int>(SpellSlot.R, 1)} ))
                {
                    R.CastOnUnit(enemy);
                }
            }
        }

        private static void Combo()
        {
            var useQ = Config.Item("UseQCombo").GetValue<bool>();
            var useE = Config.Item("UseECombo").GetValue<bool>();
            var useR = Config.Item("UseRCombo").GetValue<bool>();
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            if (target == null)
            {
                return;
            }
            else
            {
                if (useQ && Q.IsReady()) { Q.Cast(); }
                if (useE && E.IsReady()) { E.Cast(target); }
                if (useR && R.IsReady())
                {
                    if (Damage.IsKillable(ObjectManager.Player, target, new[] { Tuple.Create<SpellSlot, int>(SpellSlot.R, 1) }))
                    {
                        R.CastOnUnit(target);
                    }
                }
            }
        }
    }
}
