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
            UnlockConsole();
            ConCommandHooks();
            FreeTheConvars();
            ClassicStageInfoHooks();
            SeedHooks();
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

        private static void ClassicStageInfoHooks()
        {
            On.RoR2.CombatDirector.SetNextSpawnAsBoss += CombatDirector_SetNextSpawnAsBoss;
            IL.RoR2.ClassicStageInfo.Awake += ClassicStageInfo_Awake;
            On.RoR2.TeleporterInteraction.OnChargingFinished += TeleporterInteraction_OnChargingFinished;
        }

        private static void TeleporterInteraction_OnChargingFinished(On.RoR2.TeleporterInteraction.orig_OnChargingFinished orig, TeleporterInteraction self)
        {
            orig(self);
            //RoR2Cheats.FAMCHANCE = 0.02f;
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
            //c.Index++;

        }

        public static void SceneDirector_onPrePopulateSceneServer(SceneDirector director)
        {
            //Note that this is not a hook, but an event subscription.
            director.SetFieldValue("monsterCredit", 0);
        }

        private static void CombatDirector_SetNextSpawnAsBoss(On.RoR2.CombatDirector.orig_SetNextSpawnAsBoss orig, CombatDirector self)
        {
            orig(self);
            if (RoR2Cheats.nextBoss)
            {
                var cats = RoR2.ClassicStageInfo.instance.GetFieldValue<DirectorCardCategorySelection>("monsterCategories");
                var mons = cats.categories[0].cards;
                DirectorCard selected;
                //var selection = ClassicStageInfo.instance.monsterSelection;
                //DirectorCard selected = selection.GetChoice(0).value;

                for (int i = 0; i < mons.Length; i++)
                {
                    Debug.Log(mons[i].spawnCard.name.ToUpper());

                    if (mons[i].spawnCard.name.ToUpper().Contains(RoR2Cheats.nextBossName.ToUpper()))
                    {
                        selected = mons[i];
                        Debug.Log("Matched: " + selected.spawnCard.name + " with :" + RoR2Cheats.nextBossName);
                        self.OverrideCurrentMonsterCard(selected);
                    }
                    //Debug.Log(selection.GetChoice(i).value.spawnCard.name.ToUpper());
                    //if (selection.GetChoice(i).value.spawnCard.prefab.GetComponent<CharacterMaster>().bodyPrefab.GetComponent<CharacterBody>().isChampion == true)
                    //{
                    //    if (selection.GetChoice(i).value.spawnCard.name.ToUpper().Contains(RoR2Cheats.nextBossName.ToUpper()))
                    //    {
                    //        selected = selection.GetChoice(i).value;
                    //        Debug.Log("Matched: " + selected.spawnCard.name + " with :" + RoR2Cheats.nextBossName);
                    //    }
                    //}
                }
                //self.OverrideCurrentMonsterCard(selected);
            }
        }

    }
}
