using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using R2API.Utils;
using RoR2;
using RoR2.ConVar;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Console = RoR2.Console;

namespace DebugToolkit
{
    [R2APISubmoduleDependency("CommandHelper")]
    internal sealed class Hooks
    {
        private const ConVarFlags AllFlagsNoCheat = ConVarFlags.None | ConVarFlags.Archive | ConVarFlags.Engine | ConVarFlags.ExecuteOnServer | ConVarFlags.SenderMustBeServer;

        private static On.RoR2.Console.orig_RunCmd _origRunCmd;
        public static void InitializeHooks()
        {
            IL.RoR2.Console.Awake += UnlockConsole;
            On.RoR2.Console.InitConVars += InitCommandsAndFreeConvars;
            CommandHelper.AddToConsoleWhenReady();

            var runCmdHook = new Hook(typeof(Console).GetMethodCached("RunCmd"),
                typeof(Hooks).GetMethodCached(nameof(LogNetworkCommandsAndCheckPermissions)), new HookConfig { Priority = 1 });
            _origRunCmd = runCmdHook.GenerateTrampoline<On.RoR2.Console.orig_RunCmd>();
            
            On.RoR2.Console.AutoComplete.SetSearchString += BetterAutoCompletion;
            On.RoR2.Console.AutoComplete.ctor += CommandArgsAutoCompletion;
            IL.RoR2.UI.ConsoleWindow.Update += SmoothDropDownSuggestionNavigation;
            IL.RoR2.Networking.GameNetworkManager.CCSetScene += EnableCheatsInCCSetScene;

            // Noclip hooks
            var hookConfig = new HookConfig() { ManualApply = true };
            Command_Noclip.OnServerChangeSceneHook = new Hook(typeof(NetworkManager).GetMethodCached("ServerChangeScene"),
    typeof(Command_Noclip).GetMethodCached("DisableOnServerSceneChange"),hookConfig);
            Command_Noclip.origServerChangeScene = Command_Noclip.OnServerChangeSceneHook.GenerateTrampoline<Command_Noclip.d_ServerChangeScene>();
            Command_Noclip.OnClientChangeSceneHook = new Hook(typeof(NetworkManager).GetMethodCached("ClientChangeScene"),
                typeof(Command_Noclip).GetMethodCached("DisableOnClientSceneChange"),hookConfig);
            Command_Noclip.origClientChangeScene = Command_Noclip.OnClientChangeSceneHook.GenerateTrampoline<Command_Noclip.d_ClientChangeScene>();
        }

        private static void EnableCheatsInCCSetScene(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.Goto(4);
            c.RemoveRange(2);
            c.Emit(OpCodes.Ldc_I4_1);
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
		
		private static void InitCommandsAndFreeConvars(On.RoR2.Console.orig_InitConVars orig, RoR2.Console self)
        {
            void removeCheatFlag (BaseConVar cv)
            {
                cv.flags &= AllFlagsNoCheat;
            }
            orig(self);
            removeCheatFlag(self.FindConVar("sv_time_transmit_interval"));
            removeCheatFlag(self.FindConVar("run_scene_override"));
            removeCheatFlag(self.FindConVar("stage1_pod"));
            self.FindConVar("timescale").helpText += " Use time_scale instead!";
            self.FindConVar("director_combat_disable").helpText += " Use no_enemies instead!";
            self.FindConVar("timestep").helpText += " Let the DebugToolkit team know if you need this convar.";
            self.FindConVar("cmotor_safe_collision_step_threshold").helpText += " Let the DebugToolkit team know if you need this convar.";
            self.FindConVar("cheats").helpText += " But you already have the DebugToolkit mod installed...";
            IntConVar mmConvar = (IntConVar) self.FindConVar("max_messages");
            if(mmConvar.value == 25)
            {
                mmConvar.SetString("100");
            }
        }

        // ReSharper disable once UnusedMember.Local
        private static void LogNetworkCommandsAndCheckPermissions(Console self, NetworkUser sender, string concommandName, List<string> userArgs)
        {
            StringBuilder s = new StringBuilder();
            userArgs.ForEach((str) => s.AppendLine(str));

            if (sender != null && sender.isLocalPlayer == false)
            {
                Log.Message(string.Format(Lang.NETWORKING_OTHERPLAYER_4, sender.userName, sender.id.value, concommandName, s.ToString()));
            }
            else if (Application.isBatchMode)
            {
                Log.Message(string.Format(Lang.NETWORKING_OTHERPLAYER_4, "Server", 0, concommandName, s.ToString()));
            }

            var canExecute = true;

            if (sender != null && !sender.isLocalPlayer && PermissionSystem.IsEnabled.Value)
            {
                canExecute = PermissionSystem.CanUserExecute(sender, concommandName, userArgs);
            }

            if (canExecute)
            {
                _origRunCmd(self, sender, concommandName, userArgs);
                ScrollConsoleDown();
            }
        }

        internal static void ScrollConsoleDown()
        {
            if (RoR2.UI.ConsoleWindow.instance && RoR2.UI.ConsoleWindow.instance.outputField.verticalScrollbar)
            {
                RoR2.UI.ConsoleWindow.instance.outputField.verticalScrollbar.value = 1f;
            }
        }
		
		private static bool BetterAutoCompletion(On.RoR2.Console.AutoComplete.orig_SetSearchString orig, RoR2.Console.AutoComplete self, string newSearchString)
        {
            var searchString = self.GetFieldValue<string>("searchString");
            var searchableStrings = self.GetFieldValue<List<string>>("searchableStrings");

            newSearchString = newSearchString.ToLower(CultureInfo.InvariantCulture);
            if (newSearchString == searchString)
            {
                return false;
            }
            self.SetFieldValue("searchString", newSearchString);

            self.resultsList = new List<string>();
            foreach (var searchableString in searchableStrings)
            {
                if (searchableString.ToLower(CultureInfo.InvariantCulture).Contains(newSearchString)) // StartWith case
                {
                    self.resultsList.Add(searchableString);
                }
                else // similar string in the middle of the user command arg
                {
                    string searchableStringsInvariant = searchableString.ToLower(CultureInfo.InvariantCulture);
                    string userArg = newSearchString.Substring(newSearchString.IndexOf(' ') + 1);
                    if (newSearchString.IndexOf(' ') > 0 && searchableString.IndexOf(' ') > 0)
                    {
                        string userCmd = newSearchString.Substring(0, newSearchString.IndexOf(' '));
                        string searchableStringsCmd = searchableString.Substring(0, searchableString.IndexOf(' '));
                        string searchableStringsArg = searchableStringsInvariant.Substring(searchableStringsInvariant.IndexOf(' ') + 1);
                        if (searchableStringsArg.Contains(userArg) && userCmd.Equals(searchableStringsCmd))
                        {
                            self.resultsList.Add(searchableString);
                        }
                    }
                }
            }
            return true;
        }

        private static void CommandArgsAutoCompletion(On.RoR2.Console.AutoComplete.orig_ctor orig, RoR2.Console.AutoComplete self, RoR2.Console console)
        {
            orig(self, console);

            var searchableStrings = self.GetFieldValue<List<string>>("searchableStrings");
            var tmp = new List<string>();

            tmp.AddRange(ArgsAutoCompletion.CommandsWithStaticArgs);
            tmp.AddRange(ArgsAutoCompletion.CommandsWithDynamicArgs());

            tmp.Sort();
            searchableStrings.AddRange(tmp);

            self.SetFieldValue("searchableStrings", searchableStrings);
        }

        private const float changeSelectedItemTimer = 0.1f;
        private static float _lastSelectedItemChange;
        private static void SmoothDropDownSuggestionNavigation(ILContext il)
        {
            var cursor = new ILCursor(il);

            var getKey = il.Import(typeof(Input).GetMethodCached("GetKey", new []{ typeof(KeyCode) }));
            
            cursor.GotoNext(
                MoveType.After,
                x => x.MatchLdcI4(0x111),
                x => x.MatchCallOrCallvirt<Input>("GetKeyDown")
            );
            cursor.Prev.Operand = getKey;
            cursor.EmitDelegate<Func<bool, bool>>(LimitChangeItemFrequency);

            cursor.GotoNext(
                MoveType.After,
                x => x.MatchLdcI4(0x112),
                x => x.MatchCallOrCallvirt<Input>("GetKeyDown")
            );
            cursor.Prev.Operand = getKey;
            cursor.EmitDelegate<Func<bool, bool>>(LimitChangeItemFrequency);

            bool LimitChangeItemFrequency(bool canChangeItem)
            {
                if (canChangeItem)
                {
                    var timeNow = Time.time;
                    if (timeNow > changeSelectedItemTimer + _lastSelectedItemChange)
                    {
                        _lastSelectedItemChange = timeNow;

                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

                return canChangeItem;
            }
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
            var selected = DebugToolkit.nextBoss;
            //todo: fix this line.
            // selected.cost = (int)((self.monsterCredit / DebugToolkit.nextBossCount) / DebugToolkit.GetTierDef(DebugToolkit.nextBossElite).costMultiplier);
            self.OverrideCurrentMonsterCard(selected);
            self.SetFieldValue<CombatDirector.EliteTierDef>("currentActiveEliteTier", DebugToolkit.GetTierDef(DebugToolkit.nextBossElite));
            self.SetFieldValue<EliteIndex>("currentActiveEliteIndex", DebugToolkit.nextBossElite);
            Log.Message($"{selected.spawnCard.name} cost has been set to {selected.cost} for {DebugToolkit.nextBossCount} {DebugToolkit.nextBossElite.ToString()} bosses with available credit: {self.monsterCredit}",Log.LogLevel.Info);
            DebugToolkit.nextBossCount = 1;
            DebugToolkit.nextBossElite = EliteIndex.None;
            On.RoR2.CombatDirector.SetNextSpawnAsBoss -= CombatDirector_SetNextSpawnAsBoss;
        }

        internal static void SeedHook(On.RoR2.PreGameController.orig_Awake orig, PreGameController self)
        {
            orig(self);
            if (DebugToolkit.seed != 0)
            {
                self.runSeed = DebugToolkit.seed;
            }
        }

        internal static void OnPrePopulateSetMonsterCreditZero(SceneDirector director)
        {
            //Note that this is not a hook, but an event subscription.
            director.SetFieldValue("monsterCredit", 0);
        }
    }
}