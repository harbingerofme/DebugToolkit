using MonoMod.Cil;
using System;
using UnityEngine;
using RoR2;
using R2API.Utils;
using Mono.Cecil.Cil;

namespace RoR2Cheats
{
    public class Hooks
    {
        private const ConVarFlags AllFlagsNoCheat = ConVarFlags.None | ConVarFlags.Archive | ConVarFlags.Engine | ConVarFlags.ExecuteOnServer | ConVarFlags.SenderMustBeServer;
        public static void InitializeHooks()
        {
            IL.RoR2.Console.Awake += UnlockConsole;
            On.RoR2.Console.InitConVars += InitCommandsAndFreeConvars;
            CommandHelper.AddToConsoleWhenReady();

            On.RoR2.PreGameController.Awake += SeedHook;

            On.RoR2.CombatDirector.SetNextSpawnAsBoss += CombatDirector_SetNextSpawnAsBoss;

            //IL.RoR2.Networking.GameNetworkManager.FixedUpdateServer += GameNetworkManager_FixedUpdateServer;
            //IL.RoR2.Networking.GameNetworkManager.cctor += GameNetworkManager_cctor;
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
                        conAttr.helpText = MagicVars.RUNSETSTAGESCLEARED_HELP;
                    }
                    return conAttr;
                });
        }

        private static void ClassicStageInfo_Awake(ILContext il)
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
                return RoR2Cheats.FAMCHANCE;
            });
        }

        private static void CombatDirector_SetNextSpawnAsBoss(On.RoR2.CombatDirector.orig_SetNextSpawnAsBoss orig, CombatDirector self)
        {
            orig(self);
            self.monsterCredit *= 100; //remove round issues from card cost
            if (RoR2Cheats.nextBoss)
            {
                var mons = Alias.Instance.GetDirectorCards();
                DirectorCard selected = mons[0];

                for (int i = 0; i < mons.Count; i++)
                {
                    Debug.Log(mons[i].spawnCard.name.ToUpper());

                    if (mons[i].spawnCard.name.ToUpper().Contains(RoR2Cheats.nextBossName.ToUpper()))
                    {
                        selected = mons[i];
                        selected.cost = (int)((self.monsterCredit / RoR2Cheats.nextBossCount) / RoR2Cheats.GetTierDef(RoR2Cheats.nextBossElite).costMultiplier);
                        self.OverrideCurrentMonsterCard(selected);
                        self.SetFieldValue<CombatDirector.EliteTierDef>("currentActiveEliteTier", RoR2Cheats.GetTierDef(RoR2Cheats.nextBossElite));
                        self.SetFieldValue<EliteIndex>("currentActiveEliteIndex", RoR2Cheats.nextBossElite);
                        Debug.Log($"{selected.spawnCard.name} cost has been set to {selected.cost} for {RoR2Cheats.nextBossCount} {RoR2Cheats.nextBossElite.ToString()} bosses");
                        return;
                    }
                }
            }
        }

        private static void SeedHook(On.RoR2.PreGameController.orig_Awake orig, PreGameController self)
        {
            orig(self);
            if (RoR2Cheats.seed != 0)
            {
                self.runSeed = RoR2Cheats.seed;
            }
        }

        public static void OnPrePopulateSetMonsterCreditZero(SceneDirector director)
        {
            //Note that this is not a hook, but an event subscription.
            director.SetFieldValue("monsterCredit", 0);
        }
    }
}
