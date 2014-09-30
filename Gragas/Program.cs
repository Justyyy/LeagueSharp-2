﻿using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gragas
{
    internal class Program
    {
        public const string ChampionName = "Gragas";
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
            Game.PrintChat("Loading 'Roll Out The Barrel'...");

            Q = new Spell(SpellSlot.Q, 850);
            Q.SetSkillshot(0.7f, 125f, float.MaxValue, false, SkillshotType.SkillshotCircle);

            W = new Spell(SpellSlot.W, 0);

            E = new Spell(SpellSlot.E, 600);

            R = new Spell(SpellSlot.R, 1150);
            R.SetSkillshot(0.7f, 125f, float.MaxValue, false, SkillshotType.SkillshotCircle);

            Config = new Menu("Roll Out The Barrel", ChampionName, true);

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            SimpleTs.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));

            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));

            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));

            Config.AddSubMenu(new Menu("LaneClear", "LaneClear"));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseQLaneClear", "Use Q").SetValue(true));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseWLaneClear", "Use W").SetValue(true));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseELaneClear", "Use E").SetValue(true));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseRLaneClear", "Use R").SetValue(true));

            Config.AddSubMenu(new Menu("Misc", "Misc"));
            Config.SubMenu("Misc").AddItem(new MenuItem("UseEAntiGapcloser", "E on Gapclose (Incomplete)").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("UseRAntiGapcloser", "R on Gapclose (Incomplete)").SetValue(true));

            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;

            Game.OnGameUpdate += Game_OnGameUpdate;
            Orbwalking.AfterAttack += Orbwalking_AfterAttack;
            Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;
            Config.AddToMainMenu();
            Game.PrintChat("'Roll Out The Barrel' Loaded!");

        }

        private static void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            //throw new NotImplementedException();
        }

        private static void Orbwalking_AfterAttack(Obj_AI_Base unit, Obj_AI_Base target)
        {
            //throw new NotImplementedException();
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (Program.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                //Game.PrintChat("combo");
                Combo();
            }
            if (Program.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
            {
                //Game.PrintChat("harass");
                Harass();
            }
            if (Program.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                //Game.PrintChat("laneclear");
                LaneClear();
            }
        }

        private static void Harass()
        {
            var useQ = Config.Item("UseQHarass").GetValue<bool>();

            var qTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
            //double damage = ObjectManager.Player.GetSpellDamage(target, SpellSlot.R, 1);
            //Game.PrintChat(damage.ToString());

            if (qTarget == null)
            {
                return;
            }
            else
            {
                bool barrelRoll = ObjectManager.Player.HasBuff("GragasQ");
                if (useQ && qTarget.IsValidTarget(Q.Range) && Q.IsReady())
                {
                    SharpDX.Vector3 predPos = Q.GetPrediction(qTarget).CastPosition;
                    if (!barrelRoll)
                    {
                        Q.Cast(predPos);
                    }
                    if (barrelRoll)
                    {

                        if (getRemainingBarrelRoll() < 3.2)
                        {
                            Q.CastIfWillHit(qTarget, 1);
                        }
                        Q.CastIfWillHit(qTarget, 3);
                    }

                }
            }
        }

        private static void LaneClear()
        {
            var useQ = Config.Item("UseQLaneClear").GetValue<bool>();
            var useW = Config.Item("UseWLaneClear").GetValue<bool>();
            var useE = Config.Item("UseELaneClear").GetValue<bool>();
            var useR = Config.Item("UseRLaneClear").GetValue<bool>();

            var rangedMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.Ranged);
            var allMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range);

            var jungleMinions = MinionManager.GetMinions(ObjectManager.Player.Position, Q.Range, MinionTypes.All, MinionTeam.NotAlly);

            foreach (var minion in jungleMinions)
            {
                allMinions.Add(minion);
            }
            if (useQ && Q.IsReady())
            {
                bool barrelRoll = Player.HasBuff("GragasQ");
                var rangedLocation = Q.GetCircularFarmLocation(rangedMinions);
                var location = Q.GetCircularFarmLocation(allMinions);
                var bLocation = (location.MinionsHit > rangedLocation.MinionsHit + 1) ? location : rangedLocation;

                if (!barrelRoll && bLocation.MinionsHit > 0)
                {
                    Q.Cast(bLocation.Position.To3D());

                }
                if (barrelRoll)
                {
                    int minionsHit = 0;
                    foreach (var minion in allMinions)
                    {
                        if (Vector3.Distance(bLocation.Position.To3D(), minion.ServerPosition) <= Q.Width && Q.GetDamage(minion) > minion.Health)
                        {
                            minionsHit += 1;
                        }
                    }
                    if (minionsHit >= 3)
                    {
                        Q.Cast();
                    }

                }
            }
            if (useE && E.IsReady())
            {
                var rangedLocation = Q.GetCircularFarmLocation(rangedMinions);
                var location = Q.GetCircularFarmLocation(allMinions);
                var bLocation = (location.MinionsHit > rangedLocation.MinionsHit + 1) ? location : rangedLocation;
                if (bLocation.MinionsHit > 2)
                {
                    E.Cast(bLocation.Position.To3D());
                }
            }
        }

        private static void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            //throw new NotImplementedException();
        }

        private static void Combo()
        {
            var useQ = Config.Item("UseQCombo").GetValue<bool>();
            var useW = Config.Item("UseWCombo").GetValue<bool>();
            var useE = Config.Item("UseECombo").GetValue<bool>();
            var useR = Config.Item("UseRCombo").GetValue<bool>();

            var qTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
            var eTarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Magical);
            var rTarget = SimpleTs.GetTarget(R.Range, SimpleTs.DamageType.Magical);


            //double damage = ObjectManager.Player.GetSpellDamage(target, SpellSlot.R, 1);
            //Game.PrintChat(damage.ToString());

            if (qTarget == null)
            {
                return;
            }
            else
            {
                bool barrelRoll = ObjectManager.Player.HasBuff("GragasQ");
                if (useQ && qTarget.IsValidTarget(Q.Range) && Q.IsReady())
                {
                    SharpDX.Vector3 predPos = Q.GetPrediction(qTarget).CastPosition;
                    if (!barrelRoll)
                    {
                        Q.Cast(predPos);
                    }
                    if (barrelRoll)
                    {
                        
                        if (getRemainingBarrelRoll() < 3.2)
                        {
                            Q.CastIfWillHit(qTarget, 1);
                        }
                        Q.CastIfWillHit(qTarget, 3);
                    }

                }
                if (useW && W.IsReady())
                {
                    W.Cast();
                }
                if (useE && eTarget.IsValidTarget(E.Range) && E.IsReady())
                {
                    PredictionOutput po = E.GetPrediction(eTarget, true);
                    E.Cast(po.CastPosition, true);
                }
                if (useR && R.IsReady())
                {

                    //Game.PrintChat("R is Ready.");
                    if (Damage.IsKillable(ObjectManager.Player, rTarget, new[] { Tuple.Create<SpellSlot, int>(SpellSlot.R, 1) }))
                    {
                        PredictionOutput prediction;
                        prediction = R.GetPrediction(rTarget, true);
                        R.Cast(prediction.CastPosition);
                    }
                }
            }
        }

        private static float getRemainingBarrelRoll()
        {
            foreach (var buff in ObjectManager.Player.Buffs)
            {
                if (buff.Name == "GragasQ")
                {
                    //Game.PrintChat("Remaining Barrel Roll Time: " + (buff.EndTime - Game.Time).ToString());
                    return buff.EndTime - Game.Time;
                }
            }
            return 0;
        }
    }
}
