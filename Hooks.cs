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


        private static void CombatDirector_SetNextSpawnAsBoss(On.RoR2.CombatDirector.orig_SetNextSpawnAsBoss orig, CombatDirector self)
        {
            orig(self);
            if(RoR2Cheats.nextBoss)
            {
                var selection = ClassicStageInfo.instance.monsterSelection;
                DirectorCard selected = selection.GetChoice(0).value;
                
                for (int i = 0; i < ClassicStageInfo.instance.monsterSelection.Count; i++)
                {
                    Debug.Log(selection.GetChoice(i).value.spawnCard.name.ToUpper());
                    if (selection.GetChoice(i).value.spawnCard.prefab.GetComponent<CharacterMaster>().bodyPrefab.GetComponent<CharacterBody>().isChampion == true)
                    {
                        if (selection.GetChoice(i).value.spawnCard.name.ToUpper().Contains(RoR2Cheats.nextBossName.ToUpper()))
                        {
                            selected = selection.GetChoice(i).value;
                            Debug.Log("Matched: " + selected.spawnCard.name + " with :" + RoR2Cheats.nextBossName);
                        }
                    }
                }
                self.OverrideCurrentMonsterCard(selected);
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
