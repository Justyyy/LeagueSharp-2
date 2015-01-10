using System;
using System.Linq;
using System.Windows.Input;
using Gragas;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace RollOutTheBarrel
{
    class Gragas
    {
        public static Obj_AI_Hero Player;
        public const string ChampionName = "Gragas";
        public static Spell Q, W, E, R;
        public static Orbwalking.Orbwalker Orbwalker;
        public static Menu Config;
        public static GameObject Bomb;
        public static Vector3 UltPos;

        public Gragas()
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        static void Game_OnGameLoad(EventArgs args)
        {
            Q = new Spell(SpellSlot.Q, 775);
            W = new Spell(SpellSlot.W, 0);
            E = new Spell(SpellSlot.E, 600);
            R = new Spell(SpellSlot.R, 1050);
            Q.SetSkillshot(0.3f, 110f, 1000f, false, SkillshotType.SkillshotCircle);
            E.SetSkillshot(0.3f, 50, 1000, true, SkillshotType.SkillshotLine);
            R.SetSkillshot(0.3f, 700, 1000, false, SkillshotType.SkillshotCircle);
            Config = new Menu("Gragas", ChampionName, true);
            Game.PrintChat("Loading RollOutTheBarrel...");
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
            var miscMenu = new Menu("Misc", "Misc");
            {
                miscMenu.AddItem(new MenuItem("UseRKillsteal", "Killsteal with R").SetValue(true));
                miscMenu.AddItem(new MenuItem("UseEAntiGapcloser", "E on Gapclose (Incomplete)").SetValue(true));
                miscMenu.AddItem(new MenuItem("InsecKey", "Insec Key (Disabled)").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
                miscMenu.AddItem(new MenuItem("UseRAntiGapcloser", "R on Gapclose (Incomplete)").SetValue(true));
                miscMenu.AddItem(new MenuItem("UsePackets", "Use Packets").SetValue(false));
                Config.AddSubMenu(miscMenu);
            }


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
            Config.AddToMainMenu();

            Player = ObjectManager.Player;
            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            GameObject.OnCreate += OnCreateObject;
            GameObject.OnDelete += GameObject_OnDelete;


            Game.PrintChat("<font color=\"#FF9966\">RollOutTheBarrel -</font> <font color=\"#FFFFFF\">Loaded</font>");
        }
        static void OnCreateObject(GameObject sender, EventArgs args)
        {
            if (sender.Name == "Gragas_Base_Q_Ally.troy")
            {
                Bomb = sender;
                BombCreateTime = Game.Time;
                BombMaxDamageTime = BombCreateTime + 2;
                BarrelIsCast = true;
            }
            if (sender.Name == "Gragas_Base_R_End.troy")
            {
                Exploded = true;
                UltPos = sender.Position;
                Utility.DelayAction.Add(600, () => {Exploded = false;});
            }
            
        }

        static void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            if (sender.Name == "Gragas_Base_Q_Ally.troy")
            {
                Bomb = null;
            }
        }

        static void Game_OnGameUpdate(EventArgs args)
        {
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            if (Orbwalker.ActiveMode.ToString().ToLower() == "combo")
            {
                Combo(target);
            }
            if (Orbwalker.ActiveMode.ToString().ToLower() == "mixed")
            {
                Harass(target);
            }

            if (Config.Item("InsecKey").GetValue<KeyBind>().Active)
            {
                Insec(target);
            }
        }

        static bool FirstQReady()
        {
            if (Q.IsReady() && !ObjectManager.Player.HasBuff("GragasQ"))
            {
                BarrelIsCast = false;
                return true;
            }
            return false;
        }

        static bool SecondQReady()
        {
            return Q.IsReady() && ObjectManager.Player.HasBuff("GragasQ");
        }

        static void ExplodeBarrel()
        {
            if (!BarrelIsCast) return;
            Q.Cast();
            BarrelIsCast = false;
        }

        static void ThrowBarrel(Obj_AI_Hero tar, bool packet)
        {
            if (BarrelIsCast) return;
            if (Q.Cast(tar, packet) == Spell.CastStates.SuccessfullyCasted)
            {
                BarrelIsCast = true;
            }
        }

        public static bool BarrelIsCast { get; set; }

        static void Harass(Obj_AI_Hero t)
        {
            var useQ = Config.Item("UseQHarass").GetValue<bool>();
            if (useQ)
            {
                if (FirstQReady() && t.IsValidTarget(Q.Range))
                {
                    ThrowBarrel(t, Config.Item("UsePackets").GetValue<bool>());
                }
                if (SecondQReady())
                {
                    if (t.IsMoving && t.Distance(Bomb.Position) < Bomb.BoundingRadius)
                    {
                        ExplodeBarrel();
                    }
                    if ((Game.Time - BombMaxDamageTime) >= 0)
                    {
                        if (Bomb != null && t.Distance(Bomb.Position) < Bomb.BoundingRadius)
                        {
                            ExplodeBarrel();
                        }
                    }
                }
            }
        }

        static void Combo(Obj_AI_Hero t)
        {
            var useQ = Config.Item("UseQCombo").GetValue<bool>();
            var useW = Config.Item("UseWCombo").GetValue<bool>();
            var useE = Config.Item("UseECombo").GetValue<bool>();
            var useR = Config.Item("UseRCombo").GetValue<bool>();
            if (useW && W.IsReady() && t.IsValidTarget(Q.Range))
            {
                W.Cast();
            }
            if (useQ && Q.IsReady())
            {
                if (FirstQReady() && t.IsValidTarget(Q.Range))
                {
                    ThrowBarrel(t, Config.Item("UsePackets").GetValue<bool>());
                }
                if (SecondQReady())
                {
                    if (t.IsMoving && t.Distance(Bomb.Position) < Bomb.BoundingRadius)
                    {
                        ExplodeBarrel();
                    }
                    if ((Game.Time - BombMaxDamageTime) >= 0)
                    {
                        if (Bomb != null && t.Distance(Bomb.Position) < Bomb.BoundingRadius)
                        {
                            ExplodeBarrel();
                        }
                    }
                }
            }


            if (useE && E.IsReady())
            {
                if (E.WillHit(t, E.GetPrediction(t).CastPosition, 30))
                {
                    if (t.IsValidTarget(E.Range))
                    {
                        if (E.Cast(t) == Spell.CastStates.SuccessfullyCasted)
                        {
                            if (ObjectManager.Player.HasBuff("gragaswself"))
                                ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, t);
                        }
                    }
                }
            }


            if (useR && R.IsReady())
            {
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
                            var pred = Prediction.GetPrediction(t, R.Delay, R.Width / 2, R.Speed);
                            R.Cast(pred.CastPosition);
                        }
                    }
                }
            }
        }

        static bool RKillStealIsTargetInQ(Obj_AI_Hero target)
        {
            return Bomb != null && target.Distance(Bomb.Position) < Bomb.BoundingRadius / 2;
        }

        public static double BombMaxDamageTime { get; set; }
        public static double BombCreateTime { get; set; }

        static float GetComboDamage(Obj_AI_Base target)
        {
            float comboDamage = 0;
            var abilityFlag = false;
            bool hasSheen = false;
            bool hasIceborn = false;
            bool hasLichBane = false;
            if (ObjectManager.Player.InventoryItems.Any(item => item.DisplayName == "Sheen"))
            {
                hasSheen = true;
            }
            if (ObjectManager.Player.InventoryItems.Any(item => item.DisplayName == "Iceborn Gauntlet"))
            {
                hasSheen = false;
                hasIceborn = true;
            }
            if (ObjectManager.Player.InventoryItems.Any(item => item.DisplayName == "Lich Bane"))
            {
                hasSheen = false;
                hasIceborn = false;
                hasLichBane = true;
            }

            if (Q.IsReady())
            {
                comboDamage += (float)ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q);
                abilityFlag = true;
            }
            if (W.IsReady())
            {
                comboDamage += (float)ObjectManager.Player.GetSpellDamage(target, SpellSlot.W);
                abilityFlag = true;
            }
            if (E.IsReady())
            {
                comboDamage += (float)ObjectManager.Player.GetSpellDamage(target, SpellSlot.E);
                abilityFlag = true;
            }
            if (R.IsReady())
            {
                comboDamage += (float)ObjectManager.Player.GetSpellDamage(target, SpellSlot.R);
                abilityFlag = true;
            }
            if (hasLichBane && abilityFlag)
            {
                comboDamage += (float)ObjectManager.Player.CalcDamage(target, Damage.DamageType.Magical, ObjectManager.Player.BaseAttackDamage * .75) + (float)(ObjectManager.Player.FlatMagicDamageMod * .50);
            }
            else if (hasIceborn && abilityFlag)
            {
                comboDamage += (float)ObjectManager.Player.CalcDamage(target, Damage.DamageType.Physical, (ObjectManager.Player.BaseAttackDamage * 1.25));
            }
            else if (hasSheen && abilityFlag)
            {
                comboDamage += (float)ObjectManager.Player.CalcDamage(target, Damage.DamageType.Physical, (ObjectManager.Player.BaseAttackDamage * 1));
            }
            return comboDamage;
        }
        static void Insec(Obj_AI_Hero t)
        {
            Orbwalking.Orbwalk(null, Game.CursorPos);
            var pred = Prediction.GetPrediction(t, R.Delay, R.Width, R.Speed);
            if (Q.IsReady() && E.IsReady() && R.IsReady())
            {
                R.Cast(pred.CastPosition);
                if (Exploded)
                {
                    Vector3 ePos = t.Position;
                    Vector3 qCastPos = UltPos.Extend(ePos, 700);
                    Utility.DelayAction.Add(150, () =>
                    {
                        Q.Cast(qCastPos);
                        E.Cast(qCastPos);
                    });
                }
            }
        }

        public static bool Exploded { get; set; }

        static void Drawing_OnDraw(EventArgs args)
        {
            //var hppos = ObjectManager.Player.HPBarPosition;
            //var ppos = Drawing.WorldToScreen(ObjectManager.Player.Position);
            //Drawing.DrawText(ppos[0] - 80, ppos[1], Color.Green, Orbwalker.ActiveMode.ToString());
            //if (Keyboard.IsKeyDown(Key.T))
            //{
            //    Drawing.DrawText(ppos[0] - 40, ppos[1], Color.Red, "INSEC ACTIVE");
            //}
            //if (Q.IsReady() && E.IsReady() && R.IsReady())
            //{
            //    Drawing.DrawText(hppos[0] + 20, hppos[1] - 45, Color.LawnGreen, "Insec Ready");
            //}
            //else
            //{
            //    Drawing.DrawText(hppos[0] + 20, hppos[1] - 45, Color.Red, "Insec Not Ready");
            //}

        }
    }

}