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
                if (RoR2Cheats.seed != 0)
                {
                    self.runSeed = RoR2Cheats.seed;
                }
            };
        }

        public static void SceneDirector_onPrePopulateSceneServer(SceneDirector director)
        {
            //Note that this is not a hook, but an event subscription.
            director.SetFieldValue("monsterCredit", 0);
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
