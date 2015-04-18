/*
using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace LookasideBar
{
    public class Jayce
    {
        private Menu menu;
        private Orbwalking.Orbwalker Orbwalker;
        private Obj_AI_Hero Player = ObjectManager.Player;

        //Start Spells
        private Spell Qcannon = new Spell(SpellSlot.Q, 1050);//Q shot
        private Spell Wcannon = new Spell(SpellSlot.W, 0);   //Attack speed
        private Spell Ecannon = new Spell(SpellSlot.E, 650); //Gate
        private Spell Qcharge = new Spell(SpellSlot.Q, 1600);//Pew pew
        private Spell Qhammer = new Spell(SpellSlot.Q, 600); //To the skies
        private Spell Whammer = new Spell(SpellSlot.W, 285); //Electric field
        private Spell Ehammer = new Spell(SpellSlot.E, 240); //Knock back
        private Spell Rswitch = new Spell(SpellSlot.R, 0);   //Switch forms

        //CoolDowns
        private readonly float[] QcannonCD = { 8, 8, 8, 8, 8 };
        private readonly float[] WcannonCD = { 14, 12, 10, 8, 6 };
        private readonly float[] EcannonCD = { 16, 16, 16, 16, 16 };

        private readonly float[] QhammerCD = { 16, 14, 12, 10, 8 };
        private readonly float[] WhammerCD = { 10, 10, 10, 10, 10 };
        private readonly float[] EhammerCD = { 14, 13, 12, 11, 10 };
        

        //status
        private readonly SpellDataInst Qdata = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q);
        private bool isHammer;

        public Jayce(Menu config)
        {
            //Jayce stuffs
            menu = config.AddSubMenu(new Menu("Jayce", "Jayce"));
            menu.AddItem(new MenuItem("Enable", "Enable").SetValue(true));

            Menu misc = new Menu("Misc", "Misc");
            {
                misc.AddItem(new MenuItem("ks", "Kill Steal", true).SetValue(true));
                misc.AddItem(new MenuItem("VerticalGate", "Vertical Gate", true).SetValue(false));
                misc.AddItem(new MenuItem("GateDistance", "Gate Distance", true)).SetValue(new Slider(20, 3, 60));
                misc.AddItem(new MenuItem("ShootMouse", "Shoot QE mouse", true)).SetValue(new KeyBind('T', KeyBindType.Press));
            }
            
            //Orbwalker
            menu.AddSubMenu(new Menu("Orbwalker", "Orbwalker"));
            Orbwalker = new Orbwalking.Orbwalker(menu.SubMenu("Orbwalker"));

            //Target Selector
            Menu ts = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(ts);

            Qcannon.SetSkillshot(0.3f, 70f, 1500, true, SkillshotType.SkillshotLine);
            Qcharge.SetSkillshot(0.3f, 70f, 2180, true, SkillshotType.SkillshotLine);
            Ecannon.SetSkillshot(0.1f, 120, float.MaxValue, false, SkillshotType.SkillshotCircle);
            Qhammer.SetTargetted(0.25f, float.MaxValue);
            Ehammer.SetTargetted(0.25f, float.MaxValue);
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            //Dead?
            if (Player.IsDead) return;

            //Check form
            isHammer = !Qdata.Name.Contains("jayceshockblast");

            if (menu.Item("ks", true).GetValue<bool>())
                noScope();

            if (menu.Item("shoot").GetValue<KeyBind>().Active)
            {
                Qcannon.Cast(Game.CursorPos);
                Ecannon.Cast(getGateVector(Game.CursorPos));
            }

            switch (Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    Combo();
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    //code
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    //code
                    break;
            }
        }

        private void Combo()
        {
            Obj_AI_Hero target = TargetSelector.GetTarget(1300, TargetSelector.DamageType.Physical);
        }

        private void noScope()
        {
            foreach (Obj_AI_Hero enemy in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsValidTarget(Qcharge.Range) && x.IsEnemy && !x.IsDead))
            {
                if ((Player.GetSpellDamage(enemy, SpellSlot.Q) * 1.4 - 20) > enemy.Health && Qcannon.IsReady() && Ecannon.IsReady() && Player.Distance(enemy.ServerPosition) <= Qcharge.Range)
                {
                    //Try QE first
                    if (isHammer && Rswitch.IsReady())
                    {
                        Rswitch.Cast();
                        castQcharge(enemy, true);
                    }
                }
            }
        }

        private void castQcharge(Obj_AI_Hero target, bool useE)
        {
            var tarPred = Qcharge.GetPrediction(target);
             if (tarPred.Hitchance >= HitChance.High && useE)
             {
                 Qcharge.Cast(target);
                 Ecannon.Cast(getGateVector(tarPred.CastPosition));
             }
        }

        private Vector2 getGateVector(Vector3 pos)
        {
            if (menu.Item("VerticalGate").GetValue<bool>())
            {
                var rnd = new Random();
                var neg = rnd.Next(0, 1);
                var away = menu.Item("GateDistance").GetValue<Slider>().Value;
                away = (neg == 1) ? away : -away;
                var v2 = Vector3.Normalize(pos - Player.ServerPosition) * away;
                var bom = new Vector2(v2.Y, -v2.X);
                return Player.ServerPosition.To2D() + bom;
            }
            else
            {
                var v2 = Vector3.Normalize(pos - Player.ServerPosition) * 300;
                var bom = new Vector2(v2.X, v2.Y);
                return Player.ServerPosition.To2D() + bom;
            }
            
        }
    }
}
*/