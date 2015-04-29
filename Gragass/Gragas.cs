using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace Gragass
{
    internal class Gragas
    {
        #region Init

        public Gragas()
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static GameObject Barrel;
        private const string ChampionName = "Gragas";
        private static Obj_AI_Hero Player;
        private static Orbwalking.Orbwalker Orbwalker;
        private static Menu Config;
        private static Spell Q, W, E, R;
        private static List<Obj_AI_Hero> Heroes;
        private static DateTime? QCastTime;
        private static bool BarrelOut { get; set; }


        private static void Game_OnGameLoad(EventArgs args)
        {
            Heroes = new List<Obj_AI_Hero>();
            Player = ObjectManager.Player;
            BarrelOut = false;
            //Init Spells
            InitSpells();
            InitMenu();
            Config.AddToMainMenu();
            //set barrel to null at start to avoid possible npe
            Barrel = null;
            Drawing.OnDraw += Drawing_OnDraw;
            GameObject.OnCreate += GameObject_OnCreate;
            GameObject.OnDelete += GameObject_OnDelete;
            Game.OnUpdate += Game_OnUpdate;
            Game.PrintChat("Gragass Loaded.");
        }

        private static void InitSpells()
        {
            //init spells
            Q = new Spell(SpellSlot.Q, 775);
            W = new Spell(SpellSlot.W, 0);
            E = new Spell(SpellSlot.E, 600);
            R = new Spell(SpellSlot.R, 1050);

            //set skillshots
            Q.SetSkillshot(0.3f, 110f, 1000f, false, SkillshotType.SkillshotCircle);
            E.SetSkillshot(0.3f, 50, 1000, true, SkillshotType.SkillshotLine);
            R.SetSkillshot(0.3f, 700, 1000, false, SkillshotType.SkillshotCircle);
        }

        private static void InitMenu()
        {
            Config = new Menu("Gragas", ChampionName, true);
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
                miscMenu.AddItem(
                    new MenuItem("InsecKey", "Insec Key").SetValue(new KeyBind("T".ToCharArray()[0],
                        KeyBindType.Press)));
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

                var drawComboDamageMenu = new MenuItem("Draw_ComboDamage", "Draw Combo Damage").SetValue(true);
                var drawFill =
                    new MenuItem("Draw_Fill", "Draw Combo Damage Fill").SetValue(new Circle(true,
                        Color.FromArgb(90, 255, 169, 4)));
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
        }

        #endregion
        #region GameObjects
        private static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            if (sender.Name == "Gragas_Base_Q_Ally.troy")
            {
                Barrel = sender;
                QCastTime = DateTime.Now;
            }
        }

        private static void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            if (sender.Name == "Gragas_Base_Q_Ally.troy")
            {
                Barrel = null;
                BarrelOut = false;
                QCastTime = null;
            }
        }

        private static double TimeSinceLastQ()
        {
            if (QCastTime != null) return (DateTime.Now - QCastTime.Value).TotalSeconds;
            return 0;
        }

        #endregion
        #region Events

        private static void Game_OnUpdate(EventArgs args)
        {
            var heroes = ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsEnemy && hero.Distance(Player) < 1500);
            Heroes = heroes.ToList();
            switch (Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    Combo();
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    LaneClear();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    Mixed();
                    break;
            }
        }


        private static void Drawing_OnDraw(EventArgs args)
        {
            //Drawing.DrawText(Player.HPBarPosition.X + 40, Player.HPBarPosition.Y + 40, Color.NavajoWhite, "Time Since Q: {0}", TimeSinceLastQ());
        }

        #endregion
        #region Misc
        private static float GetComboDamage(Obj_AI_Hero hero)
        {
            return 0f;
        }

        private static bool TargetRunningAway(Obj_AI_Hero hero)
        {
            var isfacingplayer = hero.IsFacing(Player);
            var ismoving = hero.IsMoving;
            return !isfacingplayer && ismoving && !hero.IsMovementImpaired();
        }

        #endregion
        #region Modes

        private static void Combo()
        {
            #region High Priority Functions

            //if (Player.HasBuff("gragaswself") && CurrentWTarget != null)
            //{
            //    Player.IssueOrder(GameObjectOrder.AttackUnit, CurrentWTarget);
            //    Orbwalker.SetMovement(false);
            //}
            //Orbwalker.SetMovement(true);

            #endregion

            #region Q

            if (Q.IsReady())
            {
                var target = Q.GetTarget();
                if (target != null)
                {
                    if (target.IsValidTarget(Q.Range) && Barrel == null && !TargetRunningAway(target))
                    {
                        Q.Cast(Q.GetPrediction(target).CastPosition);
                    }
                    else if (TimeSinceLastQ() >= 2.5)
                    {
                        Q.Cast();
                    }
                    else if (Barrel != null && TargetRunningAway(target))
                    {
                        Q.Cast();
                    }
                }
            }

            #endregion

            #region W

            if (W.IsReady())
            {
                var target = W.GetTarget();
                if (target.IsValidTarget(E.Range))
                {
                    CurrentWTarget = W.GetTarget();
                    W.Cast();
                }
            }

            #endregion

            #region E

            if (E.IsReady())
            {
                var target = E.GetTarget();
                if (target != null && target.IsValidTarget(E.Range) && !TargetRunningAway(target))
                {
                    E.Cast(E.GetPrediction(target).CastPosition);
                }
            }

            #endregion

            #region R

            if (R.IsReady())
            {
                var target = R.GetTarget();
                if (target != null && target.IsValidTarget(R.Range) && R.IsKillable(target))
                {
                    R.Cast(R.GetPrediction(target).CastPosition);
                }
            }

            #endregion
        }

        private static Obj_AI_Base CurrentWTarget { get; set; }

        private static void LaneClear()
        {
            #region init laneclear

            var minions = MinionManager.GetMinions(1000, MinionTypes.All, MinionTeam.Enemy);
            minions.AddRange(MinionManager.GetMinions(1000, MinionTypes.All, MinionTeam.Neutral));
            Obj_AI_Minion highestHealth = null;
            foreach (var minion in minions)
            {
                if (highestHealth == null)
                {
                    highestHealth = minion as Obj_AI_Minion;
                    continue;
                }
                if (minion.Health > highestHealth.Health)
                {
                    highestHealth = minion as Obj_AI_Minion;
                }
            }

            #endregion

            #region High Priority Functions

            if (Player.HasBuff("gragaswself") && highestHealth != null)
            {
                if (highestHealth.Team == GameObjectTeam.Neutral)
                {
                    Player.IssueOrder(GameObjectOrder.AttackUnit, highestHealth);
                    Orbwalker.SetMovement(false);
                }
            }
            Orbwalker.SetMovement(true);

            #endregion

            #region Q

            if (Q.IsReady())
            {
                var pos = new List<Obj_AI_Base>();
                foreach (var minion in minions)
                {
                    if (pos.Count == 0)
                    {
                        pos.Add(minion);
                    }
                    if (minion.Distance(pos[0]) < 60)
                    {
                        pos.Add(minion);
                    }
                }
                if (Barrel == null && pos.Count > 2 && !BarrelOut)
                {
                    BarrelOut = true;
                    Q.Cast(MinionWithMostNeighbors(pos, 60).Position);
                }
                else if (TimeSinceLastQ() >= 2.5)
                {
                    Q.Cast();
                }
                else if (Barrel == null && minions.Count > 0 && !BarrelOut)
                {
                    BarrelOut = true;
                    Q.Cast(MinionWithMostNeighbors(minions, 60).Position);
                }
            }

            #endregion

            #region W

            if (W.IsReady())
            {
                if (highestHealth != null)
                {
                    W.Cast();
                }
            }

            #endregion

            #region E

            if (E.IsReady())
            {
                var pos = new List<Obj_AI_Base>();
                foreach (var minion in minions)
                {
                    if (pos.Count == 0)
                    {
                        pos.Add(minion);
                    }
                    if (minion.Distance(pos[0]) < 60)
                    {
                        pos.Add(minion);
                    }
                }
                if (pos.Count > 0)
                {
                    var minionMost = MinionWithMostNeighbors(pos, 30);
                    if (minionMost.Distance(Player) < 100)
                    {
                        E.Cast(minionMost.Position);
                    }
                }
            }

            #endregion
        }

        private static Obj_AI_Base MinionWithMostNeighbors(List<Obj_AI_Base> minions, int range)
        {
            var organized = new Dictionary<Obj_AI_Base, int>();
            foreach (var minion in minions)
            {
                var count = minions.Count(min => minion.Distance(min) < range);
                if (!organized.ContainsKey(minion))
                {
                    organized.Add(minion, count);
                }
            }
            var sorted = organized.OrderBy(entry => entry.Value);
            return sorted.Last().Key;
        }

        private static void Mixed()
        {
            #region Q

            if (Q.IsReady())
            {
                var target = Q.GetTarget();
                if (target != null)
                {
                    if (target.IsValidTarget(Q.Range) && Barrel == null && !TargetRunningAway(target))
                    {
                        Q.Cast(Q.GetPrediction(target).CastPosition);
                    }
                    else if (TimeSinceLastQ() >= 2.5)
                    {
                        Q.Cast();
                    }
                    else if (Barrel != null && TargetRunningAway(target))
                    {
                        Q.Cast();
                    }
                }
            }

            #endregion
        }

        #endregion
    }
}
