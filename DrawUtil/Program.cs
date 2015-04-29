using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace DrawUtil
{
    class Program
    {
        
        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnStart;
        }

        public static List<Vector3> MinionPosList;
        public static Obj_AI_Base Player;

        private static void Game_OnStart(EventArgs args)
        {
            MinionPosList = new List<Vector3>();
            
            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Game.PrintChat("DrawUtil Loaded.");
        }

        private static void Drawing_OnDraw(EventArgs args)
        {

            foreach (var v in MinionPosList)
            {
                Drawing.DrawCircle(v, 50, Color.NavajoWhite);
            }
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (MinionPosList.Count > 0) MinionPosList.Clear();
            var minions = MinionManager.GetMinions(20000);
            foreach (var m in minions)
            {
                MinionPosList.Add(m.Position);
            }
            //Game.PrintChat("Cycle completed.");
        }
    }
}
