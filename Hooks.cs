using MonoMod.Cil;
using System;
using RoR2;
using R2API.Utils;
using Mono.Cecil.Cil;
using System.Text;

namespace RoR2Cheats
{
    internal sealed class Hooks
    {
        private const ConVarFlags AllFlagsNoCheat = ConVarFlags.None | ConVarFlags.Archive | ConVarFlags.Engine | ConVarFlags.ExecuteOnServer | ConVarFlags.SenderMustBeServer;
        public static void InitializeHooks()
        {
            IL.RoR2.Console.Awake += UnlockConsole;
            On.RoR2.Console.InitConVars += InitCommandsAndFreeConvars;
            CommandHelper.AddToConsoleWhenReady();
            On.RoR2.Console.RunCmd += LogNetworkCommands;
            RoR2.Console.onLogReceived += ScrollThatConsoleDown;
        }

        private static void ScrollThatConsoleDown(RoR2.Console.Log _)
        {
            RoR2.UI.ConsoleWindow.instance.outputField.verticalScrollbar.value = 1f;
        }

        private static void LogNetworkCommands(On.RoR2.Console.orig_RunCmd orig, RoR2.Console self, NetworkUser sender, string concommandName, System.Collections.Generic.List<string> userArgs)
        {
            if (sender!= null && sender.isLocalPlayer == false)
            {
                StringBuilder s = new StringBuilder();
                userArgs.ForEach((str) => s.AppendLine(str));
                Log.Message(string.Format(Lang.NETWORKING_OTHERPLAYER_4, sender.userName, sender.id.value, concommandName, s.ToString()));
            }
            orig(self,sender,concommandName,userArgs);
        }

        private static void InitCommandsAndFreeConvars(On.RoR2.Console.orig_InitConVars orig, RoR2.Console self)
        {
            void removeCheatFlag (RoR2.ConVar.BaseConVar cv)
            {
                cv.flags &= AllFlagsNoCheat;
            }
            orig(self);
            removeCheatFlag(self.FindConVar("sv_time_transmit_interval"));
            removeCheatFlag(self.FindConVar("run_scene_override"));
            removeCheatFlag(self.FindConVar("stage1_pod"));
            self.FindConVar("timescale").helpText += " Use time_scale instead!";
            self.FindConVar("director_combat_disable").helpText += " Use no_enemies instead!";
            self.FindConVar("timestep").helpText += " Let the ror2cheats team know if you need this convar.";
            self.FindConVar("cmotor_safe_collision_step_threshold").helpText += " Let the ror2cheats team know if you need this convar.";
            self.FindConVar("cheats").helpText += " But you already have the RoR2Cheats mod installed...";
        }

        private static void UnlockConsole(ILContext il)
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
                        conAttr.helpText = Lang.RUNSETSTAGESCLEARED_HELP;
                    }
                    return conAttr;
                });
        }

        internal static void ForceFamilyEvent(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.GotoNext(
                x => x.MatchCallOrCallvirt<Xoroshiro128Plus>("get_nextNormalizedFloat"),
                x => x.MatchLdcR4(0.02f),
                x => x.MatchBgtUn(out _)
                );
            c.Next.Next.OpCode = OpCodes.Nop;
            c.Index++;
            c.EmitDelegate<Func<float>>(() =>
            {
                return 1.0f;
            });
        }

        internal static void RemoveFamilyEvent(On.RoR2.Stage.orig_Start orig, Stage self)
        {
            orig(self);
            IL.RoR2.ClassicStageInfo.Awake -= ForceFamilyEvent;
            On.RoR2.Stage.Start -= RemoveFamilyEvent;
        }


        internal static void CombatDirector_SetNextSpawnAsBoss(On.RoR2.CombatDirector.orig_SetNextSpawnAsBoss orig, CombatDirector self)
        {
            orig(self);
            self.monsterCredit *= 100; 
            var selected = RoR2Cheats.nextBoss;
            selected.cost = (int)((self.monsterCredit / RoR2Cheats.nextBossCount) / RoR2Cheats.GetTierDef(RoR2Cheats.nextBossElite).costMultiplier);
            self.OverrideCurrentMonsterCard(selected);
            self.SetFieldValue<CombatDirector.EliteTierDef>("currentActiveEliteTier", RoR2Cheats.GetTierDef(RoR2Cheats.nextBossElite));
            self.SetFieldValue<EliteIndex>("currentActiveEliteIndex", RoR2Cheats.nextBossElite);
            Log.Message($"{selected.spawnCard.name} cost has been set to {selected.cost} for {RoR2Cheats.nextBossCount} {RoR2Cheats.nextBossElite.ToString()} bosses with available credit: {self.monsterCredit}",Log.LogLevel.Info);
            RoR2Cheats.nextBossCount = 1;
            RoR2Cheats.nextBossElite = EliteIndex.None;
            On.RoR2.CombatDirector.SetNextSpawnAsBoss -= CombatDirector_SetNextSpawnAsBoss;
        }

        internal static void SeedHook(On.RoR2.PreGameController.orig_Awake orig, PreGameController self)
        {
            orig(self);
            if (RoR2Cheats.seed != 0)
            {
                self.runSeed = RoR2Cheats.seed;
            }
        }

        internal static void OnPrePopulateSetMonsterCreditZero(SceneDirector director)
        {
            //Note that this is not a hook, but an event subscription.
            director.SetFieldValue("monsterCredit", 0);
        }
    }
}
