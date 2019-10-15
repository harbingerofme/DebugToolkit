using MonoMod.Cil;
using System;
using UnityEngine;
using RoR2;
using R2API.Utils;

namespace RoR2Cheats
{
    public class Hooks
    {
        private const ConVarFlags AllFlagsNoCheat = ConVarFlags.None | ConVarFlags.Archive | ConVarFlags.Engine | ConVarFlags.ExecuteOnServer | ConVarFlags.SenderMustBeServer;
        public static void InitializeHooks()
        {
            UnlockConsole();
            ConCommandHooks();
            FreeTheConvars();

            SeedHooks();

            CameraFOVHooks();

            SetupNoEnemyIL();

            SetupFOVIL();

            //IL.RoR2.Networking.GameNetworkManager.FixedUpdateServer += GameNetworkManager_FixedUpdateServer;
            //IL.RoR2.Networking.GameNetworkManager.cctor += GameNetworkManager_cctor;
        }

        private static void FreeTheConvars()
        {
            void removeCheatFlag (RoR2.ConVar.BaseConVar cv)
            {
                cv.flags &= AllFlagsNoCheat;
            }

            On.RoR2.Console.InitConVars += (orig, self) =>
            {
                orig(self);
                removeCheatFlag(self.FindConVar("sv_time_transmit_interval"));
                removeCheatFlag(self.FindConVar("run_scene_override"));
                removeCheatFlag(self.FindConVar("stage1_pod"));
                self.FindConVar("timescale").helpText += " Use time_scale instead!";
                self.FindConVar("director_combat_disable").helpText += " Use no_enemies instead!";
                self.FindConVar("timestep").helpText += " Let the ror2cheats team know if you need this convar.";
                self.FindConVar("cmotor_safe_collision_step_threshold").helpText += " Let the ror2cheats team know if you need this convar.";
                self.FindConVar("cheats").helpText += " But you already have the RoR2Cheats mod installed...";
            };

            
        }

        private static void UnlockConsole()
        {
            IL.RoR2.Console.Awake += (ILContext il) =>
            {
                ILCursor c = new ILCursor(il);
                c.GotoNext(
                    MoveType.After,
                    x => x.MatchCastclass(typeof(ConCommandAttribute))
                    );
                c.EmitDelegate<Func<ConCommandAttribute, ConCommandAttribute>>(
                    (conAttr) =>
                    {
                        conAttr.flags &= AllFlagsNoCheat;
                        if (conAttr.commandName == "run_set_stages_cleared")
                        {
                            conAttr.helpText = MagicVars.RUNSETSTAGESCLEARED_HELP;
                        }
                        return conAttr;
                    });
            };
        }

        private static void ConCommandHooks()
        {
            On.RoR2.Console.Awake += (orig, self) =>
            {
                orig(self);
                R2API.Utils.CommandHelper.RegisterCommands(self);
            };
        }

        private static void SeedHooks()
        {
            On.RoR2.PreGameController.Awake += (orig,self)=> 
            {
                orig(self);
                if (Cheats.seed != 0)
                {
                    self.runSeed = Cheats.seed;
                }
            };
        }



        private static void CameraFOVHooks()
        {
            On.RoR2.CameraRigController.Start += (orig, self) =>
            {
                self.baseFov = Cheats.FieldOfVision;
                orig(self);
            };
        }

        private static void SetupNoEnemyIL()
        {
            IL.RoR2.CombatDirector.FixedUpdate += il =>
            {
                var c = new ILCursor(il);
                if (c.TryGotoNext(x => x.MatchStfld("RoR2.CombatDirector", "monsterCredit")))
                {
                    c.EmitDelegate<Func<float, float>>((f) =>
                    {
                        return Cheats.noEnemies ? 0f : f;
                    });
                }
                else
                {
                    Debug.LogWarning("RoR2Cheats - Could not create IL hook IL.RoR2.CombatDirector.FixedUpdate");
                }
            };

            IL.RoR2.TeleporterInteraction.OnStateChanged += il =>
            {
                var c = new ILCursor(il);
                if (c.TryGotoNext(x => x.MatchStfld("RoR2.CombatDirector", "monsterCredit")))
                {
                    c.EmitDelegate<Func<float, float>>((f) =>
                    {
                        return Cheats.noEnemies ? 0f : f;
                    });
                }
                else
                {
                    Debug.LogWarning("RoR2Cheats - Could not create IL hook IL.RoR2.TeleporterInteraction.OnStateChanged");
                }
            };
            IL.RoR2.SceneDirector.Start += il =>
            {
                var c = new ILCursor(il);
                if (c.TryGotoNext(x => x.MatchStfld("RoR2.SceneDirector", "monsterCredit")))
                {
                    c.EmitDelegate<Func<int, int>>((i) =>
                    {
                        return Cheats.noEnemies ? 0 : i;
                    });
                }
                else
                {
                    Debug.LogWarning("RoR2Cheats - Could not create IL hook IL.RoR2.SceneDirector.Start");
                }
            };
        }

        private static void SetupFOVIL()
        {

            IL.RoR2.CameraRigController.Update += il =>
            {
                var c = new ILCursor(il);
                if (c.TryGotoNext(
                    x => x.MatchLdcR4(1.3f)
                ))
                {
                    c.Index++;
                    c.EmitDelegate<Func<float, float>>((f) => { return 1; });
                }
                else
                {
                    Debug.LogWarning("RoR2Cheats - Could not create IL hook IL.RoR2.CameraRigController.Update");
                }
            };

            IL.EntityStates.Huntress.BackflipState.FixedUpdate += il =>
            {
                var c = new ILCursor(il);
                if (c.TryGotoNext(x => x.MatchLdcR4(60f)))
                {
                    c.Index++;
                    c.EmitDelegate<Func<float, float>>(f => { return Cheats.FieldOfVision - 10f; });
                }
                else
                {
                    Debug.LogWarning("RoR2Cheats - Could not create IL hook IL.EntityStates.Huntress.BackflipState.FixedUpdate");
                }
            };

            IL.EntityStates.Commando.DodgeState.FixedUpdate += il =>
            {
                var c = new ILCursor(il);
                if (c.TryGotoNext(x => x.MatchLdcR4(60f)))
                {
                    c.Index++;
                    c.EmitDelegate<Func<float, float>>(f => { return Cheats.FieldOfVision - 10f; });
                }
                else
                {
                    Debug.LogWarning("RoR2Cheats - Could not create IL hook IL.EntityStates.Commando.DodgeState.FixedUpdate");
                }
            };
        }

        //private static void GameNetworkManager_cctor(ILContext il)
        //{
        //    ILCursor c = new ILCursor(il);
        //    c.GotoNext(
        //        x => x.MatchLdstr("sv_time_transmit_interval"),
        //        x => x.MatchLdcI4(out _),
        //        x => x.MatchLdcR4(out _)
        //        );
        //    c.Next.Next.Next.Operand = Cheats.TickIntervalMulti;

        //}

        //private static void GameNetworkManager_FixedUpdateServer(ILContext il)
        //{
        //    ILCursor c = new ILCursor(il);
        //    //c.GotoNext(
        //    //    x => x.MatchLdarg(0),
        //    //    x => x.MatchLdfld("RoR2.Networking.GameNetworkManager", "timeTransmitTimer"),
        //    //    x => x.MatchLdsfld("RoR2.Networking.GameNetworkManager", "svTimeTransmitInterval")
        //    //    );
        //    //c.Index += 4;
        //    //c.Emit(OpCodes.Ldc_R4, Cheats.TickIntervalMulti);
        //    //c.Emit(OpCodes.Mul);
        //    c.GotoNext(
        //        x => x.MatchLdarg(0),
        //        x => x.MatchLdfld("RoR2.Networking.GameNetworkManager", "timeTransmitTimer"),
        //        x => x.MatchLdsfld("RoR2.Networking.GameNetworkManager", "svTimeTransmitInterval")
        //        );
        //    //c.Index += 4;
        //    //c.Emit(OpCodes.Ldc_R4, Cheats.TickIntervalMulti);
        //    //c.Emit(OpCodes.Mul);
        //    //c.Prev.OpCode = OpCodes.Nop;
        //    c.Index += 2;
        //    c.RemoveRange(2);
        //    c.Emit(OpCodes.Ldc_R4, Cheats.TickRate);

        //}

    }
}
