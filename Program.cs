using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace LookaJayce
{
    class Program
    {
        private static Menu menu;
        private static Orbwalking.Orbwalker Orbwalker;
        private static Obj_AI_Hero Player = ObjectManager.Player;

        //Start Spells
        private static Spell Qcharge = new Spell(SpellSlot.Q, 1600);//Pew pew
        private static Spell Qcannon = new Spell(SpellSlot.Q, 1050);//Q shot
        private static Spell Wcannon = new Spell(SpellSlot.W, 0);   //Attack speed
        private static Spell Ecannon = new Spell(SpellSlot.E, 650); //Gate
        private static Spell Qhammer = new Spell(SpellSlot.Q, 600); //To the skies
        private static Spell Whammer = new Spell(SpellSlot.W, 285); //Electric field
        private static Spell Ehammer = new Spell(SpellSlot.E, 270); //Knock back
        private static Spell Rswitch = new Spell(SpellSlot.R, 0);   //Switch forms

        //Cooldowns
        public static float[] QcannonTrueCD = {  8,  8,  8,  8,  8 };
        public static float[] WcannonTrueCD = { 14, 12, 10,  8,  6 };
        public static float[] EcannonTrueCD = { 16, 16, 16, 16, 16 };
        public static float[] QhammerTrueCD = { 16, 14, 12, 10,  8 };
        public static float[] WhammerTrueCD = { 10, 10, 10, 10, 10 };
        public static float[] EhammerTrueCD = { 14, 12, 12, 11, 10 };

        private static float QcannonCD, WcannonCD, EcannonCD;
        private static float QhammerCD, WhammerCD, EhammerCD;
        private static float QcannonCDrem, WcannonCDrem, EcannonCDrem;
        private static float QhammerCDrem, WhammerCDrem, EhammerCDrem;

        //status
        private static readonly SpellDataInst Qdata = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q);
        private static readonly SpellDataInst Wdata = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W);
        private static readonly SpellDataInst Edata = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E);
        private static bool isHammer;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.ChampionName != "Jayce") return;
            

            //Main menu
            menu = new Menu("Jayce", "Jayce", true);

            //Add orbwalker
            menu.AddSubMenu(new Menu("Orbwalker", "Orbwalker"));
            Orbwalker = new Orbwalking.Orbwalker(menu.SubMenu("Orbwalker"));

            //Target selector
            var ts = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(ts);
            menu.AddSubMenu(ts);

            Qcannon.SetSkillshot(0.250f, 70f, 1500, true, SkillshotType.SkillshotLine);
            Qcharge.SetSkillshot(0.250f, 70f, 2180, true, SkillshotType.SkillshotLine);

            Menu misc = new Menu("Misc", "Misc");
            {
                misc.AddItem(new MenuItem("ks", "Kill Steal", true).SetValue(true));
                misc.AddItem(new MenuItem("VerticalGate", "Vertical Gate", true).SetValue(false));
                misc.AddItem(new MenuItem("GateDistance", "Gate Distance", true)).SetValue(new Slider(30, 5, 60));
                misc.AddItem(new MenuItem("QuickScope", "QuickScope mouse", true)).SetValue(new KeyBind('T', KeyBindType.Press));
                misc.AddItem(new MenuItem("Flee", "Tactical Retreat", true)).SetValue(new KeyBind('A', KeyBindType.Press));
                misc.AddItem(new MenuItem("AntiGapcloser", "Anti Gapcloser", true).SetValue(true));
                misc.AddItem(new MenuItem("Interrupt", "Interrupt", true).SetValue(true));
                menu.AddSubMenu(misc);
            }

            //Drawings menu:
            Menu drawMenu = new Menu("Drawings", "Drawings");
            {
                drawMenu.AddItem(new MenuItem("DisableDrawing", "Disable Drawing", true).SetValue(false));
                drawMenu.AddItem(new MenuItem("Qcharge", "Q Charge", true).SetValue(true));
                drawMenu.AddItem(new MenuItem("Qcannon", "E Cannon", true).SetValue(true));
                drawMenu.AddItem(new MenuItem("Ecannon", "E Cannon", true).SetValue(false));
                drawMenu.AddItem(new MenuItem("Qhammer", "Q Hammer", true).SetValue(true));
                drawMenu.AddItem(new MenuItem("Ehammer", "E Hammer", true).SetValue(false));
                drawMenu.AddItem(new MenuItem("drawcds", "Draw Cooldowns", true).SetValue(true));
                menu.AddSubMenu(drawMenu);
            }
            menu.AddToMainMenu();

            Game.OnUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter2.OnInterruptableTarget += InterrupterOnPossibleToInterrupt;
            Obj_AI_Hero.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;

            Game.PrintChat("LookaJayce By Lookaside Loaded! GLHF :D");
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead) return;
            isHammer = !Qdata.Name.Contains("jayceshockblast"); //Update Form
            ProcessCDs();

            if (menu.Item("ks", true).GetValue<bool>()) //KS Check
            {
                MLG_quickscope();
            }

            if (menu.Item("QuickScope", true).GetValue<KeyBind>().Active) //Shootmouse
            {
                Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                if (isHammer && Rswitch.IsReady()) Rswitch.Cast();
                if (!isHammer && Ecannon.IsReady() && Qcannon.IsReady())
                {
                    Qcannon.Cast(Game.CursorPos);
                    Ecannon.Cast(getGateVector(Game.CursorPos));
                }
            }

            if (menu.Item("Flee", true).GetValue<KeyBind>().Active) //Flee
            {
                Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                if (!isHammer && Ecannon.IsReady())
                    Ecannon.Cast(getGateVector(Game.CursorPos));
                if (Rswitch.IsReady()) Rswitch.Cast();
            }

            switch (Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    Combo();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    Harass();
                    break;
            }
        }

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(Qcharge.Range, TargetSelector.DamageType.Physical);

            var QchargePred = Qcharge.GetPrediction(target);
            var QcannonPred = Qcannon.GetPrediction(target);

            if (!isHammer)
            {

                //Try QE
                if (QchargePred.Hitchance >= HitChance.High && GotManaFor(true, false, true) && Ecannon.IsReady() && Qcannon.IsReady())
                {
                    Qcharge.Cast(target);
                    Ecannon.Cast(getGateVector(QchargePred.CastPosition));
                }
                //Try Q
                else if (QcannonPred.Hitchance >= HitChance.High && GotManaFor(true) && Qcannon.IsReady())
                {
                    Qcannon.Cast(target);
                }

                //Use W
                if (Player.Distance(target.Position) <= 450 && GotManaFor(false, true) && Wcannon.IsReady())
                {
                    //Activate muramana
                    //Activate ghostblade
                    Wcannon.Cast();
                }

                //No abilities left and in range to hammer Q?
                if (!Qcannon.IsReady() && !Wcannon.IsReady() && Rswitch.IsReady() && Player.Distance(target.Position) <= 500)
                {
                    Rswitch.Cast();
                }
            }

            if (isHammer)
            {
                if (Player.Distance(target.Position) <= Qhammer.Range && GotManaFor(true) && Qhammer.IsReady())
                {
                    Qhammer.Cast(target);
                    if (Whammer.IsReady())
                    {
                        Whammer.Cast();
                    }
                }

                //No abilities left?
                if (!Qhammer.IsReady() && !Whammer.IsReady() && Rswitch.IsReady())
                {
                    Rswitch.Cast();
                }
            }
        }

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(Qcharge.Range, TargetSelector.DamageType.Physical);

            var QchargePred = Qcharge.GetPrediction(target);
            var QcannonPred = Qcannon.GetPrediction(target);

            if (!isHammer)
            {
                //Try QE
                if (QchargePred.Hitchance >= HitChance.High && GotManaFor(true, false, true) && Ecannon.IsReady() && Qcannon.IsReady())
                {
                    Qcharge.Cast(target);
                    Ecannon.Cast(getGateVector(QchargePred.CastPosition));
                }
                //Try Q
                else if (QcannonPred.Hitchance >= HitChance.High && GotManaFor(true) && Qcannon.IsReady())
                {
                    Qcannon.Cast(target);
                }
            }
        }

        private static void KnockAway(Obj_AI_Base target)
        {
            if (Player.Distance(target.Position) > Ehammer.Range || !Ehammer.IsReady()) return;
            if (!isHammer && Rswitch.IsReady()) Rswitch.Cast(); //Hammer form
            if (isHammer && Ehammer.IsReady()) Ehammer.Cast(target); //Knock back
        }

        private static void insec()
        {
            //coming soon...
        }

        private static void MLG_quickscope()
        {
            foreach (Obj_AI_Hero enemy in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsValidTarget(Qcharge.Range) && x.IsEnemy && !x.IsDead))
            {
                //Try Qcharge
                if ((Player.GetSpellDamage(enemy, SpellSlot.Q) * 1.4 - 20) > enemy.Health && Qcharge.GetPrediction(enemy).Hitchance >= HitChance.High && GotManaFor(true, false, true) && Ecannon.IsReady() && Qcannon.IsReady())
                {
                    if (isHammer && Rswitch.IsReady()) Rswitch.Cast();
                    if (!isHammer)
                    {
                        Qcharge.Cast(enemy);
                        Ecannon.Cast(getGateVector(Qcharge.GetPrediction(enemy).CastPosition));
                    }
                }
                //Try Qcannon
                else if ((Player.GetSpellDamage(enemy, SpellSlot.Q) - 20) > enemy.Health && Qcannon.GetPrediction(enemy).Hitchance >= HitChance.High && GotManaFor(true) && Qcannon.IsReady())
                {
                    if (isHammer && Rswitch.IsReady()) Rswitch.Cast();
                    if (!isHammer)
                    {
                        Qcannon.Cast(enemy);
                    }
                }

                //Try QEhammer
                if (Player.Distance(enemy.ServerPosition) <= Qhammer.Range + enemy.BoundingRadius && (Player.GetSpellDamage(enemy, SpellSlot.E) + Player.GetSpellDamage(enemy, SpellSlot.Q, 1) - 20) > enemy.Health && GotManaFor(true, false, true) && Qhammer.IsReady() && Ehammer.IsReady())
                {
                    if (!isHammer && Rswitch.IsReady()) Rswitch.Cast();

                    if (isHammer)
                    {
                        Qhammer.Cast(enemy);
                        Ehammer.Cast(enemy);
                    }
                }
                //Try Ehammer
                if (Player.Distance(enemy.ServerPosition) <= Ehammer.Range && Player.GetSpellDamage(enemy, SpellSlot.E) > enemy.Health && GotManaFor(false, false, true) && Ehammer.IsReady())
                {
                    if (!isHammer && Rswitch.IsReady()) Rswitch.Cast();

                    if (isHammer)
                    {
                        Ehammer.Cast(enemy);
                    }
                }
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (menu.Item("DisableDrawing", true).GetValue<bool>()) return;

            if (menu.Item("Qcharge", true).GetValue<bool>() && !isHammer)
                Render.Circle.DrawCircle(Player.Position, Qcharge.Range, (Qcharge.IsReady() && Ecannon.IsReady()) ? Color.Green : Color.Red);

            if (menu.Item("Qcannon", true).GetValue<bool>() && !isHammer)
                Render.Circle.DrawCircle(Player.Position, Qcannon.Range, Qcannon.IsReady() ? Color.Green : Color.Red);

            if (menu.Item("Qhammer", true).GetValue<bool>() && isHammer)
                Render.Circle.DrawCircle(Player.Position, Qhammer.Range, Qhammer.IsReady() ? Color.Green : Color.Red);

            if (menu.Item("Ecannon", true).GetValue<bool>() && !isHammer)
                Render.Circle.DrawCircle(Player.Position, Ecannon.Range, Ecannon.IsReady() ? Color.Green : Color.Red);

            if (menu.Item("Ehammer", true).GetValue<bool>() && isHammer)
                Render.Circle.DrawCircle(Player.Position, Ehammer.Range, Ehammer.IsReady() ? Color.Green : Color.Red);



            if (menu.Item("drawcds", true).GetValue<bool>())
            {
                Vector2 wts = Drawing.WorldToScreen(Player.Position);
                if (isHammer)
                {
                    if (QcannonCD == 0)
                        Drawing.DrawText(wts[0] - 80, wts[1] + 10, Color.White, "Q Ready");
                    else
                        Drawing.DrawText(wts[0] - 80, wts[1] + 10, Color.Orange, "Q: " + QcannonCD.ToString("0.0"));
                    if (WcannonCD == 0)
                        Drawing.DrawText(wts[0] - 40, wts[1] + 30, Color.White, "W Ready");
                    else
                        Drawing.DrawText(wts[0] - 40, wts[1] + 30, Color.Orange, "W: " + WcannonCD.ToString("0.0"));
                    if (EcannonCD == 0)
                        Drawing.DrawText(wts[0], wts[1] + 10, Color.White, "E Ready");
                    else
                        Drawing.DrawText(wts[0], wts[1] + 10, Color.Orange, "E: " + EcannonCD.ToString("0.0"));
                }
                else
                {
                    if (QhammerCD == 0)
                        Drawing.DrawText(wts[0] - 80, wts[1] + 10, Color.White, "Q Ready");
                    else
                        Drawing.DrawText(wts[0] - 80, wts[1] + 10, Color.Orange, "Q: " + QhammerCD.ToString("0.0"));
                    if (WhammerCD == 0)
                        Drawing.DrawText(wts[0] - 40, wts[1] + 30, Color.White, "W Ready");
                    else
                        Drawing.DrawText(wts[0] - 40, wts[1] + 30, Color.Orange, "W: " + WhammerCD.ToString("0.0"));
                    if (EhammerCD == 0)
                        Drawing.DrawText(wts[0], wts[1] + 10, Color.White, "E Ready");
                    else
                        Drawing.DrawText(wts[0], wts[1] + 10, Color.Orange, "E: " + EhammerCD.ToString("0.0"));
                }
            }
        }

        private static Vector2 getGateVector(Vector3 pos)
        {
            int distance = menu.Item("GateDistance", true).GetValue<Slider>().Value;
            if (menu.Item("VerticalGate", true).GetValue<bool>())
            {
                distance = (new Random().NextDouble() > 0.5) ? distance : -distance;
                var v2 = Vector3.Normalize(pos - Player.ServerPosition) * 10;
                var bom = new Vector2(v2.Y, -v2.X);
                return Player.ServerPosition.To2D() + bom;
            }
            else
            {
                var v2 = Vector3.Normalize(pos - Player.ServerPosition) * (distance * 10);
                var bom = new Vector2(v2.X, v2.Y);
                return Player.ServerPosition.To2D() + bom;
            }
        }

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            KnockAway(gapcloser.Sender);
        }

        private static void InterrupterOnPossibleToInterrupt(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            KnockAway(sender);
        }


        //START COOLDOWN STUFF
        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs spell)
        {
            if (unit.IsMe) GetCooldowns(spell);
        }

        private static void GetCooldowns(GameObjectProcessSpellCastEventArgs spell)
        {
            if (isHammer)
            {
                if (spell.SData.Name == "JayceToTheSkies")
                    QhammerCDrem = Game.Time + CalculateCd(QhammerTrueCD[Qhammer.Level - 1]);
                if (spell.SData.Name == "JayceStaticField")
                    WhammerCDrem = Game.Time + CalculateCd(WhammerTrueCD[Whammer.Level - 1]);
                if (spell.SData.Name == "JayceThunderingBlow")
                    EhammerCDrem = Game.Time + CalculateCd(EhammerTrueCD[Ehammer.Level - 1]);
            }
            else
            {
                if (spell.SData.Name == "jayceshockblast")
                    QcannonCDrem = Game.Time + CalculateCd(QcannonTrueCD[Qcannon.Level - 1]);
                if (spell.SData.Name == "jaycehypercharge")
                    WcannonCDrem = Game.Time + CalculateCd(WcannonTrueCD[Wcannon.Level - 1]);
                if (spell.SData.Name == "jayceaccelerationgate")
                    EcannonCDrem = Game.Time + CalculateCd(EcannonTrueCD[Ecannon.Level - 1]);
            }
        }
        
        private static float CalculateCd(float time)
        {
            return time + (time * Player.PercentCooldownMod);
        }

        private static void ProcessCDs()
        {
            QhammerCD = ((QhammerCDrem - Game.Time) > 0) ? (QhammerCDrem - Game.Time) : 0;
            WhammerCD = ((WhammerCDrem - Game.Time) > 0) ? (WhammerCDrem - Game.Time) : 0;
            EhammerCD = ((EhammerCDrem - Game.Time) > 0) ? (EhammerCDrem - Game.Time) : 0;
            QcannonCD = ((QcannonCDrem - Game.Time) > 0) ? (QcannonCDrem - Game.Time) : 0;
            WcannonCD = ((WcannonCDrem - Game.Time) > 0) ? (WcannonCDrem - Game.Time) : 0;
            EcannonCD = ((EcannonCDrem - Game.Time) > 0) ? (EcannonCDrem - Game.Time) : 0;
        }
        //END COOLDOWN STUFF

        private static bool GotManaFor(bool q = false, bool w = false, bool e = false)
        {
            float manaNeeded = 0;
            if (q) manaNeeded += Qdata.ManaCost;
            if (w) manaNeeded += Wdata.ManaCost;
            if (e) manaNeeded += Edata.ManaCost;
            return manaNeeded <= Player.Mana;
        }

    }
}