using System;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace Tristana
{
    internal class Program
    {
        public const string ChampionName = "Tristana";
        public static Orbwalking.Orbwalker Orbwalker;
        public static Spell Q, W, E, R;
        public static Menu Config;
        private static Obj_AI_Hero _player;

        public static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            _player = ObjectManager.Player;
            if (_player.BaseSkinName != ChampionName) return;
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

            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
            
            Game.OnUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;
            Config.AddToMainMenu();
            Game.PrintChat("'Rocket Girl Tristana' Loaded!");

        }

        private static float GetComboDamage(Obj_AI_Hero hero)
        {
            float comboDamage = 0;
            return comboDamage;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            throw new NotImplementedException();
        }

        private static void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            var sender = gapcloser.Sender;
            if (Config.Item("UseAntiGapcloser").GetValue<bool>() != true) return;
            if (sender.IsValidTarget(R.Range))
            {
                R.CastOnUnit(sender);
            }
        }



        private static void Game_OnGameUpdate(EventArgs args)
        {
            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsEnemy))
            {
                foreach (var buff in hero.Buffs)
                {
                    if (!E.IsReady() && buff.SourceName == "Tristana")
                    {
                        Game.PrintChat("------------------");
                        Game.PrintChat("Source: {0}", buff.SourceName);
                        Game.PrintChat("Display: {0}", buff.DisplayName);
                        Game.PrintChat("Name: {0}", buff.Name);
                    }
                }
            }
            Q.Range = 541 + 9 * (_player.Level - 1);
            E.Range = 541 + 9 * (_player.Level - 1);
            R.Range = 541 + 9 * (_player.Level - 1);
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
            }
            else
            {
                if (useE && E.IsReady()) { E.Cast(target); }
            }
        }

        private static void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
        }

        public static void Orbwalking_AfterAttack(Obj_AI_Base unit, Obj_AI_Base target)
        {
        }

        public static void CheckForExecute()
        {
            foreach (var enemy in 
                         ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget(R.Range) && ObjectManager.Player.GetSpellDamage(enemy, SpellSlot.R) - 55 > enemy.Health))
            {
                R.CastOnUnit(enemy);
            }
        }

        private static void Combo()
        {
            var useQ = Config.Item("UseQCombo").GetValue<bool>();
            var useE = Config.Item("UseECombo").GetValue<bool>();
            var useR = Config.Item("UseRCombo").GetValue<bool>();
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            if (target == null) return;
            if (useQ && Q.IsReady())
            {
                Q.Cast();
            }
            if (useE && E.IsReady())
            {
                E.Cast(target);
            }
            if (!useR || !R.IsReady()) return;
            if (R.IsKillable(target))
            {
                R.CastOnUnit(target);
            }
        }
    }
}
