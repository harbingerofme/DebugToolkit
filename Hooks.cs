using MonoMod.Cil;
using System;
using UnityEngine;

namespace RoR2Cheats
{
    public class Hooks
    {

        public static void InitializeHooks()
        {
            ConCommandHooks();

            CameraFOVHooks();

            SetupNoEnemyIL();
            SetupFOVIL();
        }


        private static void CameraFOVHooks()
        {
            On.RoR2.CameraRigController.Start += (orig, self) =>
            {
                self.baseFov = Cheats.FieldOfVision;
                orig(self);
            };
        }

        private static void ConCommandHooks()
        {
            On.RoR2.Console.Awake += (orig, self) =>
            {
                Generic.CommandHelper.RegisterCommands(self);
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

    }
}
