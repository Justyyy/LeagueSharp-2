using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace Gragas
{
    internal class Program
    {
        public const string ChampionName = "Gragas";
        public static Orbwalking.Orbwalker Orbwalker;
        public static Spell Q, Q2, W, E, R;
        public static Menu Config;
        private static Obj_AI_Hero _player;
        private static GameObject _qObject;
        private static float _qObjectCreateTime;
        private static bool _canUseQLaunch = true;

        public static bool CanUseQLaunch
        {
            get { return _canUseQLaunch; }
            set { _canUseQLaunch = value; }
        }

        public static float QObjectMaxDamageTime { get; set; }

        public static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
            _qObject = null;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            _player = ObjectManager.Player;
            if (_player.BaseSkinName != ChampionName) return;
            Game.PrintChat("Loading 'Roll Out The Barrel'...");

            Q = new Spell(SpellSlot.Q, 1100);
            Q.SetSkillshot(0.3f, 110f, 1000f, false, SkillshotType.SkillshotCircle);

            Q2 = new Spell(SpellSlot.Q, 250);

            W = new Spell(SpellSlot.W, 0);

            E = new Spell(SpellSlot.E, 1100);
            E.SetSkillshot(0.3f, 50, 1000, true, SkillshotType.SkillshotLine);

            R = new Spell(SpellSlot.R, 1100);
            R.SetSkillshot(0.3f, 700, 1000, false, SkillshotType.SkillshotCircle);

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
            Config.SubMenu("Misc")
                .AddItem(new MenuItem("UseEAntiGapcloser", "E on Gapclose (Incomplete)").SetValue(true));
            Config.SubMenu("Misc")
                .AddItem(new MenuItem("UseRAntiGapcloser", "R on Gapclose (Incomplete)").SetValue(true));
            var drawMenu = new Menu("Drawing", "Drawing");
            {
                drawMenu.AddItem(new MenuItem("Draw_Disabled", "Disable All").SetValue(false));
                drawMenu.AddItem(new MenuItem("Draw_Q", "Draw Q").SetValue(true));
                drawMenu.AddItem(new MenuItem("Draw_W", "Draw W").SetValue(true));
                drawMenu.AddItem(new MenuItem("Draw_E", "Draw E").SetValue(true));
                drawMenu.AddItem(new MenuItem("Draw_R", "Draw R").SetValue(true));
                drawMenu.AddItem(new MenuItem("Draw_R_Killable", "Draw R Mark on Killable").SetValue(true));

                MenuItem drawComboDamageMenu = new MenuItem("Draw_ComboDamage", "Draw Combo Damage").SetValue(true);
                MenuItem drawFill = new MenuItem("Draw_Fill", "Draw Combo Damage Fill").SetValue(new Circle(true, Color.FromArgb(90, 255, 169, 4)));
                drawMenu.AddItem(drawComboDamageMenu);
                drawMenu.AddItem(drawFill);
                DamageIndicator.DamageToUnit = GetComboDamage;
                DamageIndicator.Enabled = drawComboDamageMenu.GetValue<bool>();
                DamageIndicator.Fill = drawFill.GetValue<Circle>().Active;
                DamageIndicator.FillColor = drawFill.GetValue<Circle>().Color;
                drawComboDamageMenu.ValueChanged +=
                    delegate(object sender, OnValueChangeEventArgs eventArgs)
                    {
                        DamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
                    };
                drawFill.ValueChanged +=
                    delegate(object sender, OnValueChangeEventArgs eventArgs)
                    {
                        DamageIndicator.Fill = eventArgs.GetNewValue<Circle>().Active;
                        DamageIndicator.FillColor = eventArgs.GetNewValue<Circle>().Color;
                    };

                Config.AddSubMenu(drawMenu);
            }

            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;

            Game.OnGameUpdate += Game_OnGameUpdate;
            Orbwalking.AfterAttack += Orbwalking_AfterAttack;
            Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;
            GameObject.OnCreate += OnCreateObject;
            GameObject.OnDelete += OnDeleteObject;
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
            _player = ObjectManager.Player;
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                Combo();
            }
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
            {
                Harass();
            }
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                LaneClear();
            }
            Game.PrintChat(_qObject.ToString());
        }

        private static void Harass()
        {
            var useQ = Config.Item("UseQHarass").GetValue<bool>();

            if (useQ)
            {
                var t = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
                if (Q.IsReady() && _qObject == null && t.IsValidTarget(Q.Range))
                {
                    PredictionOutput pred = Q.GetPrediction(t, true);
                    Q.Cast(pred.CastPosition, true);
                }
                if (_qObject != null)
                {
                    if ((Game.Time - QObjectMaxDamageTime) >= 0)
                    {
                        if (t.Distance(_qObject.Position) < Q2.Range)
                        {
                            Q.Cast();
                        }
                    }
                }
            }
        }

        public static float GetComboDamage(Obj_AI_Base target)
        {
            float comboDamage = 0;
            var abilityFlag = false;
            var hasLichBane = _player.InventoryItems.Any(item => item.DisplayName == "Lich Bane");
            if (Q.IsReady())
            {
                comboDamage += (float) _player.GetSpellDamage(target, SpellSlot.Q);
                abilityFlag = true;
            }
            if (W.IsReady())
            {
                comboDamage += (float) _player.GetSpellDamage(target, SpellSlot.W);
                abilityFlag = true;
            }
            if (E.IsReady())
            {
                comboDamage += (float) _player.GetSpellDamage(target, SpellSlot.E);
                abilityFlag = true;
            }
            if (R.IsReady())
            {
                comboDamage += (float) _player.GetSpellDamage(target, SpellSlot.R);
                abilityFlag = true;
            }
            if (hasLichBane && abilityFlag)
            {
                comboDamage += (float) (_player.BaseAttackDamage*.75) + (float) (_player.FlatMagicDamageMod*.50);
            }
            return comboDamage;
        }

        private static void LaneClear()
        {
            var useQ = Config.Item("UseQLaneClear").GetValue<bool>();
            var useW = Config.Item("UseWLaneClear").GetValue<bool>();
            var useE = Config.Item("UseELaneClear").GetValue<bool>();

            var rangedMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range,
                MinionTypes.Ranged);
            var allMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range);

            var jungleMinions = MinionManager.GetMinions(ObjectManager.Player.Position, Q.Range,
                MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

            allMinions.AddRange(jungleMinions);
            MinionManager.FarmLocation rangedLocation;
            MinionManager.FarmLocation location;
            MinionManager.FarmLocation bLocation;
            if (useQ && Q.IsReady())
            {
                var barrelRoll = _player.HasBuff("GragasQ");
                rangedLocation = Q.GetCircularFarmLocation(rangedMinions);
                location = Q.GetCircularFarmLocation(allMinions);
                bLocation = (location.MinionsHit > rangedLocation.MinionsHit + 1)
                    ? location
                    : rangedLocation;

                if (!barrelRoll && bLocation.MinionsHit > 0)
                {
                    Q.Cast(bLocation.Position.To3D());
                }
                if (barrelRoll)
                {
                    var minionsHit =
                        allMinions.Count(
                            minion =>
                                Vector3.Distance(bLocation.Position.To3D(), minion.ServerPosition) <= Q.Width &&
                                Q.GetDamage(minion) > minion.Health);
                    if (minionsHit >= 3)
                    {
                        Q.Cast();
                    }
                }
            }
            if (useE && E.IsReady())
            {
                rangedLocation = Q.GetCircularFarmLocation(rangedMinions);
                location = Q.GetCircularFarmLocation(allMinions);
                bLocation = (location.MinionsHit > rangedLocation.MinionsHit + 1)
                    ? location
                    : rangedLocation;
                if (bLocation.MinionsHit > 2)
                {
                    E.Cast(bLocation.Position.To3D());
                }
            }
            if (useW && W.IsReady())
            {
                W.Cast();
            }
        }

        

        private static void OnCreateObject(GameObject sender, EventArgs args)
        {
            if (sender.Name.Contains("Gragas") && sender.Name.Contains("Q_Ally"))
            {
                _qObject = sender;
                _qObjectCreateTime = Game.Time;
                QObjectMaxDamageTime = _qObjectCreateTime + 2;
                CanUseQLaunch = false;
            }
        }

        private static void OnDeleteObject(GameObject sender, EventArgs args)
        {
            if (sender.Name.Contains("Gragas") && sender.Name.Contains("Q_Ally"))
            {
                _qObject = null;
                CanUseQLaunch = true;
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



            if (useQ && Q.IsReady())
            {
                var t = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
                if (Q.IsReady() && _qObject == null && t.IsValidTarget(Q.Range))
                {
                    PredictionOutput pred = Q.GetPrediction(t, true);
                    Q.Cast(pred.CastPosition, true);
                }
                if (_qObject != null)
                {
                    if ((Game.Time - QObjectMaxDamageTime) >= 0)
                    {
                        if (t.Distance(_qObject.Position) < Q2.Range)
                        {
                            Q.Cast();
                        }
                    }
                }
            }
            if (useW && W.IsReady())
            {
                if (W.IsReady())
                {
                    W.Cast();
                }
            }

            if (useE && W.IsReady())
            {
                var t = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Magical);
                if (E.IsReady() && t.IsValidTarget(E.Range))
                {
                    var pred = Prediction.GetPrediction(t, E.Delay, E.Width / 2, E.Speed);
                    E.Cast(pred.CastPosition);
                }
                //
            }



            if (useR && R.IsReady())
            {
                var t = SimpleTs.GetTarget(R.Range, SimpleTs.DamageType.Magical);
                if (R.IsReady() && t.IsValidTarget(R.Range))
                {
                    if (R.IsKillable(t))
                    {
                        if (!RKillStealIsTargetInQ(t))
                        {
                            var pred = Prediction.GetPrediction(t, R.Delay, R.Width/2, R.Speed);
                            R.Cast(pred.CastPosition);
                        }
                        else
                        {
                            if (Q.IsKillable(t))
                            {
                                Q.Cast();
                            }
                        }
                    }
                }
            }
        }

        private static void ComboQ()
        {
            var t = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
            if (Q.IsReady() && _qObject == null && t.IsValidTarget(Q.Range))
            {
                Q.Cast(t, false, true);
                _qObject = new GameObject();
            }
        }

        private static void ComboQ2()
        {
            var t = SimpleTs.GetTarget(Q2.Range, SimpleTs.DamageType.Magical);
            if (_qObject == null) return;
            if (t.Distance(_qObject.Position) < Q2.Range)
            {
                Q.Cast();
            }
        }

        private static void ComboW()
        {
            var t = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Magical);
            if (W.IsReady() && _player.Distance(t) < 250)
            {
                W.Cast();
            }
        }

        private static void ComboE()
        {
            var t = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Magical);
            if (E.IsReady() && t.IsValidTarget(E.Range))
            {
                E.Cast(t, false, true);
            }
        }

        private static void ComboR()
        {
            var t = SimpleTs.GetTarget(R.Range, SimpleTs.DamageType.Magical);
            Game.PrintChat(R.GetDamage(t, 1).ToString(CultureInfo.InvariantCulture));
            if (R.IsReady() && t.IsValidTarget(R.Range) && R.IsKillable(t))
            {
                R.Cast(t, false, true);
            }
        }

        private static bool RKillStealIsTargetInQ(Obj_AI_Hero target)
        {
            if (_qObject != null)
            {
                if (target.Distance(_qObject.Position) < Q2.Range/2)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
