using System;
using System.ComponentModel;
using System.Linq;
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
        public static bool BarrelIsCast { get; set; }

        public static float QObjectMaxDamageTime { get; set; }

        public static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
            
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            _qObject = null;
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
            TargetSelector.AddToMenu(targetSelectorMenu);
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

            //Config.AddSubMenu(new Menu("LaneClear", "LaneClear"));
            //Config.SubMenu("LaneClear").AddItem(new MenuItem("UseQLaneClear", "Use Q").SetValue(true));
            //Config.SubMenu("LaneClear").AddItem(new MenuItem("UseWLaneClear", "Use W").SetValue(true));
            //Config.SubMenu("LaneClear").AddItem(new MenuItem("UseELaneClear", "Use E").SetValue(true));
            //Config.SubMenu("LaneClear").AddItem(new MenuItem("UseRLaneClear", "Use R").SetValue(true));

            Config.AddSubMenu(new Menu("Misc", "Misc"));
            Config.SubMenu("Misc")
                .AddItem(new MenuItem("UseRKillsteal", "Killsteal with R").SetValue(true));
            Config.SubMenu("Misc")
                .AddItem(new MenuItem("UseEAntiGapcloser", "E on Gapclose (Incomplete)").SetValue(true));
            Config.SubMenu("Misc")
                .AddItem(new MenuItem("UseRAntiGapcloser", "R on Gapclose (Incomplete)").SetValue(true));
            Config.SubMenu("Misc")
                .AddItem(new MenuItem("UsePackets", "Use Packets").SetValue(false));
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
            Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;
            Game.OnGameInput += Game_OnGameInput;
            GameObject.OnCreate += OnCreateObject;
            GameObject.OnDelete += OnDeleteObject;
            Config.AddToMainMenu();
            Game.PrintChat("'Roll Out The Barrel' Loaded!");
        }

        private static void Game_OnGameInput(GameInputEventArgs args)
        {
            if (args.Input == ".status")
            {
                Game.PrintChat(_qObject == null ? "qObject does not exist." : _qObject.ToString());
                Game.PrintChat(BarrelIsCast == false ? "BarrelIsCast = FALSE" : "BarrelIsCast = TRUE");
            }
        }

        private static void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
        }

        public static void Orbwalking_AfterAttack(Obj_AI_Base unit, Obj_AI_Base target)
        {
            //throw new NotImplementedException();
        }

        public static bool QActivated
        {
            get { return ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).ToggleState == 1 || _qObject != null; }
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
                //LaneClear();
            }
            CheckKillSteal();
        }

        private static void CheckKillSteal()
        {
            if (_player.Position.CountEnemysInRange((int) R.Range) > 0 && R.IsReady())
            {
                foreach(Obj_AI_Hero hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => R.IsKillable(hero) && hero.IsValidTarget(R.Range)))
                {
                    R.Cast(hero);
                }
            }
        }

        private static void ThrowBarrel(Obj_AI_Hero tar, bool packet)
        {
            if (BarrelIsCast) return;
            if (Q.Cast(tar, packet) == Spell.CastStates.SuccessfullyCasted)
            {
                BarrelIsCast = true;
            }
        }
        //private static void ThrowBarrel(Vector3 pos, bool packet)
        //{
        //    if (BarrelIsCast) return;
        //    if (Q.Cast(pos, packet) == Spell.CastStates.SuccessfullyCasted)
        //    {
        //        BarrelIsCast = true;
        //    }
        //    new Spell().
        //}

        private static bool SecondQReady()
        {
            return Q.IsReady() && _player.HasBuff("GragasQ");
        }

        private static bool FirstQReady()
        {
            if (Q.IsReady() && !_player.HasBuff("GragasQ"))
            {
                BarrelIsCast = false;
                return true;
            }
            return false;
        }


        private static void ExplodeBarrel()
        {
            if (!BarrelIsCast) return;
            Q.Cast();
            BarrelIsCast = false;
        }

        private static void Harass()
        {
            var useQ = Config.Item("UseQHarass").GetValue<bool>();

            if (!useQ) return;
            var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            if (FirstQReady() && t.IsValidTarget(Q.Range))
            {
                ThrowBarrel(t, Config.Item("UsePackets").GetValue<bool>());
            }
            if (SecondQReady())
            {
                
                if (t.IsMoving)
                {
                    ExplodeBarrel();
                }
                if ((Game.Time - QObjectMaxDamageTime) >= 0)
                {
                    if (_qObject != null && t.Distance(_qObject.Position) < _qObject.BoundingRadius)
                    {
                        ExplodeBarrel();
                    }
                }
            }
        }

        public static float GetComboDamage(Obj_AI_Base target)
        {
            float comboDamage = 0;
            var abilityFlag = false;
            bool hasSheen = false;
            bool hasIceborn = false;
            bool hasLichBane = false;
            if (_player.InventoryItems.Any(item => item.DisplayName == "Sheen"))
            {
                hasSheen = true;
            }
            if (_player.InventoryItems.Any(item => item.DisplayName == "Iceborn Gauntlet"))
            {
                hasSheen = false;
                hasIceborn = true;
            }
            if (_player.InventoryItems.Any(item => item.DisplayName == "Lich Bane"))
            {
                hasSheen = false;
                hasIceborn = false;
                hasLichBane = true;
            }
            
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
                comboDamage += (float) _player.CalcDamage(target, Damage.DamageType.Magical, _player.BaseAttackDamage*.75) + (float) (_player.FlatMagicDamageMod*.50);
            }
            else if (hasIceborn && abilityFlag)
            {
                comboDamage += (float) _player.CalcDamage(target, Damage.DamageType.Physical, (_player.BaseAttackDamage*1.25));
            }
            else if (hasSheen && abilityFlag)
            {
                comboDamage += (float) _player.CalcDamage(target, Damage.DamageType.Physical, (_player.BaseAttackDamage * 1));
            }
            return comboDamage;
        }

        //private static void LaneClear()
        //{
        //    var useQ = Config.Item("UseQLaneClear").GetValue<bool>();
        //    var useW = Config.Item("UseWLaneClear").GetValue<bool>();
        //    var useE = Config.Item("UseELaneClear").GetValue<bool>();

        //    var rangedMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range,
        //        MinionTypes.Ranged);
        //    var allMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range);

        //    var jungleMinions = MinionManager.GetMinions(ObjectManager.Player.Position, Q.Range,
        //        MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

        //    allMinions.AddRange(jungleMinions);
        //    MinionManager.FarmLocation rangedLocation;
        //    MinionManager.FarmLocation location;
        //    MinionManager.FarmLocation bLocation;
        //    if (useQ && Q.IsReady())
        //    {
        //        var barrelRoll = _player.HasBuff("GragasQ");
        //        rangedLocation = Q.GetCircularFarmLocation(rangedMinions);
        //        location = Q.GetCircularFarmLocation(allMinions);
        //        bLocation = (location.MinionsHit > rangedLocation.MinionsHit + 1)
        //            ? location
        //            : rangedLocation;

        //        if (!barrelRoll && bLocation.MinionsHit > 0)
        //        {
        //            Q.Cast(bLocation.Position.To3D());
        //        }
        //        if (barrelRoll)
        //        {
        //            var minionsHit =
        //                allMinions.Count(
        //                    minion =>
        //                        Vector3.Distance(bLocation.Position.To3D(), minion.ServerPosition) <= Q.Width &&
        //                        Q.GetDamage(minion) > minion.Health);
        //            if (minionsHit >= 3)
        //            {
        //                ExplodeBarrel();
        //            }
        //        }
        //    }
        //    if (useE && E.IsReady())
        //    {
        //        rangedLocation = Q.GetCircularFarmLocation(rangedMinions);
        //        location = Q.GetCircularFarmLocation(allMinions);
        //        bLocation = (location.MinionsHit > rangedLocation.MinionsHit + 1)
        //            ? location
        //            : rangedLocation;
        //        if (bLocation.MinionsHit > 2)
        //        {
        //            E.Cast(bLocation.Position.To3D());
        //        }
        //    }
        //    if (useW && W.IsReady())
        //    {
        //        W.Cast();
        //    }
        //}

        

        private static void OnCreateObject(GameObject sender, EventArgs args)
        {
            if (!sender.Name.Contains("Gragas") || !sender.Name.Contains("Q_Ally")) return;
            _qObject = sender;
            _qObjectCreateTime = Game.Time;
            BarrelIsCast = true;
            QObjectMaxDamageTime = _qObjectCreateTime + 2;
        }

        private static void OnDeleteObject(GameObject sender, EventArgs args)
        {
            if (!sender.Name.Contains("Gragas") || !sender.Name.Contains("Q_Ally")) return;
            _qObject = null;
            BarrelIsCast = false;
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


            Obj_AI_Hero t;
            if (useW && W.IsReady())
            {
                if (W.IsReady())
                {
                    W.Cast();
                }
            }
            if (useQ)
            {
                t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
                if (FirstQReady() && t.IsValidTarget(Q.Range))
                {
                    ThrowBarrel(t, Config.Item("UsePackets").GetValue<bool>());
                }
                if (SecondQReady())
                {
                    if (t.IsMoving)
                    {
                        ExplodeBarrel();
                    }
                    if ((Game.Time - QObjectMaxDamageTime) >= 0)
                    {
                        if (_qObject != null && t.Distance(_qObject.Position) < _qObject.BoundingRadius)
                        {
                            ExplodeBarrel();
                        }
                    }
                }
            }
            

            if (useE && E.IsReady())
            {
                t = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
                if (t.IsValidTarget(E.Range))
                {
                    if (E.Cast(t) == Spell.CastStates.SuccessfullyCasted)
                    {
                        if (_player.HasBuff("gragaswself"))
                            ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, t);
                    }
                }
                //
            }


            if (useR && R.IsReady())
            {
                t = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Magical);
                if (t.IsValidTarget(R.Range))
                {
                    if (R.IsKillable(t))
                    {
                        if (RKillStealIsTargetInQ(t))
                        {
                            if (Q.IsKillable(t))
                            {
                                ExplodeBarrel();
                            }
                        }
                        else
                        {
                            var pred = Prediction.GetPrediction(t, R.Delay, R.Width/2, R.Speed);
                            R.Cast(pred.CastPosition);
                        }
                    }
                }
            }
        }

        private static bool RKillStealIsTargetInQ(Obj_AI_Hero target)
        {
            if (_qObject == null) return false;
            return target.Distance(_qObject.Position) < Q2.Range/2;
        }
    }
}
