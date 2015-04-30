using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace RollOutTheBarrel
{
    internal class Gragas
    {
        #region Init

        public Gragas()
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static GameObject _barrel;
        private const string ChampionName = "Gragas";
        private static Obj_AI_Hero _player;
        private static Orbwalking.Orbwalker _orbwalker;
        private static Menu _config;
        private static Spell _q, _w, _e, _r;
        //private static List<Obj_AI_Hero> Heroes;
        private static DateTime? _qCastTime;
        private static bool BarrelOut { get; set; }
        private static Vector3 drawInsecPoint { get; set; }


        private static void Game_OnGameLoad(EventArgs args)
        {
            //Heroes = new List<Obj_AI_Hero>();
            _player = ObjectManager.Player;
            BarrelOut = false;
            insecPoint = new Vector3();
            drawInsecPoint = new Vector3();
            //Init Spells
            InitSpells();
            InitMenu();
            _config.AddToMainMenu();
            //set barrel to null at start to avoid possible npe
            _barrel = null;
            Drawing.OnDraw += Drawing_OnDraw;
            GameObject.OnCreate += GameObject_OnCreate;
            GameObject.OnDelete += GameObject_OnDelete;
            CustomEvents.Unit.OnDash += Unit_OnDash;
            Game.OnUpdate += Game_OnUpdate;
            Game.PrintChat("Gragass Loaded.");
        }


        private static void InitSpells()
        {
            //init spells
            _q = new Spell(SpellSlot.Q, 775);
            _w = new Spell(SpellSlot.W, 0);
            _e = new Spell(SpellSlot.E, 600);
            _r = new Spell(SpellSlot.R, 1050);

            //set skillshots
            _q.SetSkillshot(0.3f, 110f, 1000f, false, SkillshotType.SkillshotCircle);
            _e.SetSkillshot(0.3f, 50, 1000, true, SkillshotType.SkillshotLine);
            _r.SetSkillshot(0.3f, 700, 1000, false, SkillshotType.SkillshotCircle);
        }

        private static void InitMenu()
        {
            _config = new Menu("Gragas", ChampionName, true);
            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            _config.AddSubMenu(targetSelectorMenu);

            _config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            _orbwalker = new Orbwalking.Orbwalker(_config.SubMenu("Orbwalking"));


            _config.AddSubMenu(new Menu("Combo", "Combo"));
            _config.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
            _config.SubMenu("Combo").AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
            _config.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            _config.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));

            _config.AddSubMenu(new Menu("Harass", "Harass"));
            _config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));

            _config.AddSubMenu(new Menu("LaneClear", "Lane/Jungle Clear"));
            _config.SubMenu("LaneClear").AddItem(new MenuItem("UseQLaneClear", "Use Q").SetValue(true));
            _config.SubMenu("LaneClear").AddItem(new MenuItem("UseWLaneClear", "Use W").SetValue(true));
            _config.SubMenu("LaneClear").AddItem(new MenuItem("UseELaneClear", "Use E").SetValue(true));
            _config.SubMenu("LaneClear").AddItem(new MenuItem("UseRLaneClear", "Use R").SetValue(true));
            var miscMenu = new Menu("Misc", "Misc");
            {
                miscMenu.AddItem(new MenuItem("UseRKillsteal", "Killsteal with R").SetValue(true));
                miscMenu.AddItem(new MenuItem("UseEAntiGapcloser", "E on Gapclose").SetValue(true));
                miscMenu.AddItem(
                    new MenuItem("InsecKey", "Insec Key (disabled)").SetValue(new KeyBind("T".ToCharArray()[0],
                        KeyBindType.Press)));
                _config.AddSubMenu(miscMenu);
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

                _config.AddSubMenu(drawMenu);
            }
        }

        #endregion

        #region Flags

        private static bool UseQCombo
        {
            get { return _config.Item("UseQCombo").GetValue<bool>(); }
        }

        private static bool UseWCombo
        {
            get { return _config.Item("UseWCombo").GetValue<bool>(); }
        }

        private static bool UseECombo
        {
            get { return _config.Item("UseECombo").GetValue<bool>(); }
        }

        private static bool UseRCombo
        {
            get { return _config.Item("UseRCombo").GetValue<bool>(); }
        }

        private static bool UseQLaneClear
        {
            get { return _config.Item("UseQLaneClear").GetValue<bool>(); }
        }

        private static bool UseWLaneClear
        {
            get { return _config.Item("UseWLaneClear").GetValue<bool>(); }
        }

        private static bool UseELaneClear
        {
            get { return _config.Item("UseELaneClear").GetValue<bool>(); }
        }

        private static bool ActiveInsec
        {
            get { return _config.Item("InsecKey").IsActive(); }
        }

        private static bool UseEAntiGapcloser
        {
            get { return _config.Item("UseEAntiGapcloser").GetValue<bool>(); }
        }

        #endregion

        #region GameObjects

        private static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            if (sender.Name == "Gragas_Base_Q_Ally.troy")
            {
                _barrel = sender;
                _qCastTime = DateTime.Now;
            }
        }

        private static void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            if (sender.Name == "Gragas_Base_Q_Ally.troy")
            {
                _barrel = null;
                BarrelOut = false;
                _qCastTime = null;
            }
        }

        private static double TimeSinceLastQ()
        {
            if (_qCastTime != null) return (DateTime.Now - _qCastTime.Value).TotalSeconds;
            return 0;
        }

        #endregion

        #region Events

        private static void Game_OnUpdate(EventArgs args)
        {
            //DEBUG Hero list
            //var heroes = ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsEnemy && hero.Distance(Player) < 1500);
            //Heroes = heroes.ToList();
            var target = TargetSelector.GetTarget(_player, _r.Range - 100, TargetSelector.DamageType.Magical);
            drawInsecPoint = _player.Position.Extend(target.Position, _player.Distance(target) + 80);
            if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                Combo();
            }
            else if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                LaneClear();
            }
            else if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
            {
                Mixed();
            }
            else if (ActiveInsec)
            {
                //var target = TargetSelector.GetTarget(_player, _r.Range - 100, TargetSelector.DamageType.Magical);

                Insec(target);
            }
        }


        private static void Drawing_OnDraw(EventArgs args)
        {
            //Drawing.DrawText(Player.HPBarPosition.X + 40, Player.HPBarPosition.Y + 40, Color.NavajoWhite, "Time Since Q: {0}", TimeSinceLastQ());
            Drawing.DrawCircle(PlayerPos, 65, Color.NavajoWhite);
            Drawing.DrawCircle(QPos, 65, Color.NavajoWhite);
            Drawing.DrawCircle(TargetPos, 65, Color.NavajoWhite);
            Drawing.DrawLine(PlayerPos.To2D(), QPos.To2D(), 10, Color.NavajoWhite);
            Drawing.DrawLine(QPos.To2D(), TargetPos.To2D(), 10, Color.NavajoWhite);
        }

        private static void Unit_OnDash(Obj_AI_Base sender, Dash.DashItem args)
        {
            if (!UseEAntiGapcloser) return;
            if (sender.IsAlly || sender.IsMinion) return;
            if (sender.Distance(_player) < _e.Range && TargetRunningAway((Obj_AI_Hero) sender))
            {
                _e.Cast(_e.GetPrediction(sender).CastPosition);
            }
        }

        #endregion

        #region Misc

        private static float GetComboDamage(Obj_AI_Hero hero)
        {
            return 0f;
        }

        private static bool TargetRunningAway(Obj_AI_Hero hero)
        {
            var isfacingplayer = hero.IsFacing(_player);
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

            if (UseQCombo && _q.IsReady())
            {
                var target = _q.GetTarget();
                if (target != null)
                {
                    if (target.IsValidTarget(_q.Range) && _barrel == null && !TargetRunningAway(target))
                    {
                        _q.Cast(_q.GetPrediction(target).CastPosition);
                    }
                    else if (TimeSinceLastQ() >= 2.5)
                    {
                        _q.Cast();
                    }
                    else if (_barrel != null && TargetRunningAway(target))
                    {
                        _q.Cast();
                    }
                }
            }

            #endregion

            #region W

            if (UseWCombo && _w.IsReady())
            {
                var target = _w.GetTarget();
                if (target.IsValidTarget(_e.Range))
                {
                    _w.GetTarget();
                    _w.Cast();
                }
            }

            #endregion

            #region E

            if (UseECombo && _e.IsReady())
            {
                var target = _e.GetTarget();
                if (target != null && target.IsValidTarget(_e.Range) && !TargetRunningAway(target))
                {
                    _e.CastIfHitchanceEquals(target, HitChance.High);
                }
                if (target != null && target.IsValidTarget(_e.Range - 100) && TargetRunningAway(target))
                {
                    _e.CastIfHitchanceEquals(target, HitChance.High);
                }
            }

            #endregion

            #region R

            if (UseRCombo && _r.IsReady())
            {
                var target = _r.GetTarget();
                if (target != null && target.IsValidTarget(_r.Range) && _r.IsKillable(target))
                {
                    _r.Cast(_r.GetPrediction(target).CastPosition);
                }
            }

            #endregion
        }


        private static void LaneClear()
        {
            #region init laneclear

            var minions = MinionManager.GetMinions(1000);
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

            if (_player.HasBuff("gragaswself") && highestHealth != null)
            {
                if (highestHealth.Team == GameObjectTeam.Neutral)
                {
                    _player.IssueOrder(GameObjectOrder.AttackUnit, highestHealth);
                    //_orbwalker.SetMovement(false);
                }
            }
            //_orbwalker.SetMovement(true);

            #endregion

            #region Q

            if (UseQLaneClear && _q.IsReady())
            {
                if (TimeSinceLastQ() >= 2.5)
                {
                    _q.Cast();
                }
                else if (_barrel == null && minions.Count > 0 && !BarrelOut)
                {
                    BarrelOut = true;
                    _q.Cast(MinionWithMostNeighbors(minions, 60).Position);
                }
            }

            #endregion

            #region W

            if (UseWLaneClear && _w.IsReady())
            {
                if (highestHealth != null)
                {
                    _w.Cast();
                }
            }

            #endregion

            #region E

            if (UseELaneClear && _e.IsReady())
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
                    if (minionMost.Distance(_player) < 100)
                    {
                        _e.Cast(minionMost.Position);
                    }
                }
            }

            #endregion
        }


        private static void Mixed()
        {
            #region Q

            if (_q.IsReady())
            {
                var target = _q.GetTarget();
                if (target != null)
                {
                    if (target.IsValidTarget(_q.Range) && _barrel == null && !TargetRunningAway(target))
                    {
                        _q.Cast(_q.GetPrediction(target).CastPosition);
                    }
                    else if (TimeSinceLastQ() >= 2.5)
                    {
                        _q.Cast();
                    }
                    else if (_barrel != null && TargetRunningAway(target))
                    {
                        _q.Cast();
                    }
                }
            }

            #endregion
        }

        private static void Insec(Obj_AI_Hero t)
        {
            //var qtarget = _q.GetTarget();
            ////CurrentQTarget = qtarget;
            //Orbwalking.Orbwalk(null, Game.CursorPos);
            //Game.PrintChat(BarrelOut.ToString());
            //if (!BarrelOut)
            //{
            //    _q.Cast(qtarget);
            //    CurrentQTarget = qtarget;
            //}
            //if (BarrelOut)
            //{
            //    SetInsecVectors(_player.Position, _barrel.Position, CurrentQTarget.Position);
            //}
        }


        public static Obj_AI_Base CurrentQTarget { get; set; }

        public static Vector3 insecPoint { get; set; }

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

        #endregion

        #region TestVectors

        public static Vector3 PlayerPos { get; set; }
        public static Vector3 QPos { get; set; }
        public static Vector3 TargetPos { get; set; }
        public static void SetInsecVectors(Vector3 playerPos, Vector3 qPos, Vector3 targetPos)
        {
            PlayerPos = playerPos;
            QPos = qPos;
            TargetPos = targetPos;
        }
        #endregion
    }
}
