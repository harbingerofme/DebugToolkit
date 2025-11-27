using DebugToolkit.Code;
using DebugToolkit.Commands;
using DebugToolkit.Permissions;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using R2API.Utils;
using RoR2;
using RoR2.ConVar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Console = RoR2.Console;

namespace DebugToolkit
{
    public sealed class Hooks
    {
        private const ConVarFlags AllFlagsNoCheat = ConVarFlags.None | ConVarFlags.Archive | ConVarFlags.Engine | ConVarFlags.ExecuteOnServer | ConVarFlags.SenderMustBeServer;
        private const string GRAY = "<color=#808080>";
        private const string COMMAND_SIGNATURE = "<color=#ffa000>{0} {1}</color>";

        private static int[] autoCompleteIndices;

        private static On.RoR2.Console.orig_RunCmd _origRunCmd;
        private static bool buddha;
        private static bool god;

        private static GameObject commandSignatureText;
        private static CombatDirector bossDirector;
        private static bool skipSpawnIfTooCheapBackup;

        private static readonly Dictionary<CharacterMaster, PingCache> pingedTargets = new Dictionary<CharacterMaster, PingCache>();

        public static void InitializeHooks()
        {
            var allFlags = (System.Reflection.BindingFlags)(-1);

            var moveNext = typeof(RoR2.Console).
                GetNestedTypes(allFlags).
                FirstOrDefault(t => t.Name.Contains(nameof(Console.AwakeCoroutine))).
                GetMethods(allFlags).
                FirstOrDefault(m => m.Name.Contains("MoveNext"));
            new ILHook(moveNext, UnlockConsole);

            On.RoR2.Console.AwakeCoroutine += InitCommandsAndFreeConvars;

            var runCmdHook = new Hook(typeof(Console).GetMethodCached("RunCmd"),
                typeof(Hooks).GetMethodCached(nameof(LogNetworkCommandsAndCheckPermissions)), new HookConfig { Priority = 1 });
            _origRunCmd = runCmdHook.GenerateTrampoline<On.RoR2.Console.orig_RunCmd>();
            IL.RoR2.Console.RunCmd += WarnUserOfServerCommandOffline;

            IL.RoR2.UI.ConsoleWindow.CheckConsoleKey += ToggleConsoleWindowWithCustomKey;

            On.RoR2.Console.AutoComplete.SetSearchString += BetterAutoCompletion;
            RoR2Application.onLoad += Items.InitDroptableData;
            RoR2Application.onLoad += Spawners.InitPortals;
            RoR2Application.onLoad += AutoCompleteManager.RegisterAutoCompleteCommands;
            Run.onRunStartGlobal += Items.CollectItemTiers;
            On.RoR2.UI.ConsoleWindow.Start += AddConCommandSignatureHint;
            On.RoR2.UI.ConsoleWindow.OnInputFieldValueChanged += UpdateCommandSignature;
            IL.RoR2.UI.ConsoleWindow.ApplyAutoComplete += ApplyTextWithoutColorTags;
            IL.RoR2.UI.ConsoleWindow.Update += SmoothDropDownSuggestionNavigation;
            IL.RoR2.UI.HUD.Update += AllowTabAutocompleteConCommands;

            IL.RoR2.Networking.NetworkManagerSystem.CCSetScene += EnableCheatsInCCSetScene;
            On.RoR2.Networking.NetworkManagerSystem.CCSceneList += OverrideVanillaSceneList;
            On.RoR2.SaveSystem.Save += Profile.PreventSave;
            On.RoR2.PingerController.RebuildPing += InterceptPing;
            IL.RoR2.InfiniteTowerRun.BeginNextWave += InfiniteTowerRun_BeginNextWave;

            // Next boss and family event hooks
            On.RoR2.CombatDirector.SetNextSpawnAsBoss += SetNextBossForDirector;
            On.RoR2.InfiniteTowerExplicitSpawnWaveController.Initialize += SetNextBossForSimulacrumDirector;
            On.RoR2.CombatDirector.AttemptSpawnOnTarget += OverrideBossCombatDirectorSpawnResult;
            On.RoR2.ClassicStageInfo.RebuildCards += ForceFamilyEvent;
            On.RoR2.DccsPool.GenerateWeightedCategorySelection += ForceFamilyEventForDccsPoolStages;
            On.RoR2.FamilyDirectorCardCategorySelection.IsAvailable += ForceFamilyDirectorCardCategorySelectionToBeAvailable;
            Run.onRunDestroyGlobal += delegate (Run _)
            {
                CurrentRun.ResetNextBoss();
                CurrentRun.forceFamilyEvent = false;
            };

            // Networking and noclip hooks
            On.RoR2.NetworkSession.Start += NetworkManager.CreateNetworkObject;
            On.RoR2.NetworkSession.OnDestroy += NetworkManager.DestroyNetworkObject;
            On.RoR2.Networking.NetworkManagerSystem.OnStopClient += Command_Noclip.DisableOnStopClient;
            On.RoR2.MapZone.TeleportBody += Command_Noclip.DisableOOBCheck;
            Run.onRunDestroyGlobal += Command_Noclip.DisableOnRunDestroy;

            //Buddha Mode hook
            On.RoR2.HealthComponent.TakeDamage += NonLethalDamage;
            On.RoR2.CharacterMaster.Awake += SetGodMode;

            // Console logging fixes - temporary
            IL.RoR2.Console.ShowHelpText += FixCommandHelpText;
            IL.RoR2.Console.CCFind += FixCCFind;
        }

        private static void ToggleConsoleWindowWithCustomKey(ILContext il)
        {
            var c = new ILCursor(il);
            if (!c.TryGotoNext(
                MoveType.After,
                x => x.MatchCallOrCallvirt<Input>("GetKeyDown")))
            {
                Log.Message("Failed to patch " + il.Method.Name);
                return;
            }
            c.EmitDelegate<Func<bool, bool>>(isKeyPressed =>
            {
                if (isKeyPressed)
                {
                    return true;
                }
                var customKey = MacroSystem.ConsoleKey.Value;
                return customKey != KeyCode.None && Input.GetKeyDown(customKey);
            });
        }

        private static void FixCommandHelpText(ILContext il)
        {
            var c = new ILCursor(il);
            if (c.Instrs.Count > 1)
            {
                Log.Message("Skipped patching the command help text method. Maybe fixed already?", Log.LogLevel.Warning, Log.Target.Bepinex);
                return;
            }
            c.Emit(OpCodes.Ldarg_0);
            c.Emit<Console>(OpCodes.Call, "GetHelpText");
            c.Emit<Debug>(OpCodes.Call, "Log");
        }

        private static void FixCCFind(ILContext il)
        {
            var c = new ILCursor(il);
            if (!c.TryGotoNext(
                x => x.MatchCallOrCallvirt<Console>("Find"),
                x => x.MatchPop()))
            {
                Log.Message("Could not patch \"find\" command. Maybe fixed already?", Log.LogLevel.Warning, Log.Target.Bepinex);
                return;
            }
            c.Index += 1;
            c.Emit<Debug>(OpCodes.Call, "Log");
            c.Remove();
        }

        private static void SetGodMode(On.RoR2.CharacterMaster.orig_Awake orig, CharacterMaster self)
        {
            orig(self);
            if (NetworkServer.active)
            {
                self.godMode |= self.playerCharacterMasterController && god;
            }
        }

        private static void NonLethalDamage(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
        {
            if (buddha && self.body.isPlayerControlled)
            {
                damageInfo.damageType |= DamageType.NonLethal;
            }
            orig(self, damageInfo);
        }

        private static void InfiniteTowerRun_BeginNextWave(ILContext il)
        {
            var c = new ILCursor(il);
            if (!c.TryGotoNext(x => x.MatchLdloc(0)))
            {
                Log.Message("Failed to patch RoR2.InfiniteTowerRun.BeginNextWave", Log.LogLevel.Warning, Log.Target.Bepinex);
                return;
            }
            c.Index += 1;
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<GameObject, InfiniteTowerRun, GameObject>>((wavePrefab, run) =>
            {
                wavePrefab = CurrentRun.selectedWavePrefab ?? wavePrefab;
                CurrentRun.selectedWavePrefab = null;
                return wavePrefab;
            });
        }

        internal struct PingCache
        {
            internal CharacterBody body;
            internal CharacterMaster master;
        }

        private static void InterceptPing(On.RoR2.PingerController.orig_RebuildPing orig, PingerController self, PingerController.PingInfo pingInfo)
        {
            orig(self, pingInfo);
            var master = self.GetComponent<CharacterMaster>();
            if (!master)
            {
                return;
            }
            if (self.pingIndicator && self.pingIndicator.pingTarget)
            {
                var body = self.pingIndicator.pingTarget.GetComponent<CharacterBody>();
                if (body != null)
                {
                    pingedTargets[master] = new PingCache
                    {
                        body = body,
                        master = body.master
                    };
                }
                else
                {
                    pingedTargets[master] = default;
                }
            }
            else
            {
                pingedTargets[master] = default;
            }
        }

        private static void OverrideVanillaSceneList(On.RoR2.Networking.NetworkManagerSystem.orig_CCSceneList orig, ConCommandArgs args)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var sceneDef in SceneCatalog.allSceneDefs.Where((def) => !Run.instance || !def.requiredExpansion || Run.instance.IsExpansionEnabled(def.requiredExpansion)))
            {
                stringBuilder.AppendLine($"[{sceneDef.sceneDefIndex}] - {sceneDef.cachedName} ");
            }
            Log.Message(stringBuilder, Log.LogLevel.MessageClientOnly);
        }

        private static void EnableCheatsInCCSetScene(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            var method = typeof(SceneCatalog).GetMethodCached(nameof(SceneCatalog.GetSceneDefFromSceneName));
            var newMethod = typeof(Hooks).GetMethodCached(nameof(FilterExpansionLockedScenes));
            c.GotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt(method)
                );
            c.Remove();
            c.Emit(OpCodes.Call, newMethod);

            c.GotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt<RoR2.Console.CheatsConVar>("get_boolValue")
                );
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldc_I4_1);
        }

        public static SceneDef FilterExpansionLockedScenes(string sceneName)
        {
            var def = SceneCatalog.GetSceneDefFromSceneName(sceneName);
            if (!def)
            {
                return null;
            }
            if (!Run.instance || !def.requiredExpansion || Run.instance.IsExpansionEnabled(def.requiredExpansion))
            {
                return def;
            }
            Debug.Log(string.Format(Lang.EXPANSION_LOCKED, "scene", Util.GetExpansion(def.requiredExpansion)));
            return null;
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

        private static System.Collections.IEnumerator InitCommandsAndFreeConvars(On.RoR2.Console.orig_AwakeCoroutine orig, Console self)
        {
            void RemoveCheatFlag(BaseConVar cv)
            {
                if (cv != null)
                {
                    cv.flags &= AllFlagsNoCheat;
                }
            }

            void ExpandHelpText(BaseConVar cv, string text)
            {
                if (cv != null)
                {
                    cv.helpText += text;
                }
            }

            var iterator = orig(self);
            while (iterator.MoveNext())
            {
                yield return iterator.Current;
            }

            RemoveCheatFlag(self.FindConVar("sv_time_transmit_interval"));
            RemoveCheatFlag(self.FindConVar("run_scene_override"));
            RemoveCheatFlag(self.FindConVar("stage1_pod"));
            RemoveCheatFlag(self.FindConVar("spite_bomb_coefficient"));
            RemoveCheatFlag(self.FindConVar("bag_disable_breakout"));
            RemoveCheatFlag(self.FindConVar("bounce_velocity"));
            RemoveCheatFlag(self.FindConVar("junk_unlimited"));

            const string MOD_MESSAGE = " Let the DebugToolkit team know if you need this convar.";
            ExpandHelpText(self.FindConVar("timescale"), " Use time_scale instead!");
            ExpandHelpText(self.FindConVar("director_combat_disable"), " Use no_enemies instead!");
            ExpandHelpText(self.FindConVar("timestep"), MOD_MESSAGE);
            ExpandHelpText(self.FindConVar("cmotor_safe_collision_step_threshold"), MOD_MESSAGE);
            ExpandHelpText(self.FindConVar("ai_update_interval"), MOD_MESSAGE);
            ExpandHelpText(self.FindConVar("contact_damage_update_interval"), MOD_MESSAGE);
            ExpandHelpText(self.FindConVar("log_async_assets"), MOD_MESSAGE);
            ExpandHelpText(self.FindConVar("preload_scenes"), MOD_MESSAGE);
            ExpandHelpText(self.FindConVar("hitbox_show"), MOD_MESSAGE); // doesn't do anything, hence why not unlocking
            ExpandHelpText(self.FindConVar("cheats"), " But you already have the DebugToolkit mod installed...");
        }

        // ReSharper disable once UnusedMember.Local
        private static void LogNetworkCommandsAndCheckPermissions(Console self, Console.CmdSender sender, string concommandName, List<string> userArgs)
        {
            var sb = new StringBuilder();
            userArgs.ForEach((str) => sb.AppendLine(str));

            if (sender.networkUser != null && sender.localUser == null)
            {
                Log.Message(string.Format(Lang.NETWORKING_OTHERPLAYER_4, sender.networkUser.userName, sender.networkUser.id.value, concommandName, sb));
            }
            else if (Application.isBatchMode)
            {
                Log.Message(string.Format(Lang.NETWORKING_OTHERPLAYER_4, "Server", 0, concommandName, sb));
            }

            var canExecute = true;

            if (sender.networkUser != null && sender.localUser == null && PermissionSystem.IsEnabled.Value)
            {
                canExecute = PermissionSystem.CanUserExecute(sender.networkUser, concommandName, userArgs);
            }

            if (canExecute)
            {
                _origRunCmd(self, sender, concommandName, userArgs);
                ScrollConsoleDown();
            }
        }

        private static void WarnUserOfServerCommandOffline(ILContext il)
        {
            var c = new ILCursor(il);
            if (!c.TryGotoNext(
                MoveType.After,
                x => x.MatchCallOrCallvirt<Console>("ForwardCmdToServer")))
            {
                Log.Message("Failed to patch RoR2.Console.RunCmd", Log.LogLevel.Error, Log.Target.Bepinex);
                return;
            }
            var nextInstruction = c.Next;
            c.Emit<NetworkSession>(OpCodes.Call, "get_instance");
            c.Emit(OpCodes.Brtrue_S, nextInstruction);
            c.Emit(OpCodes.Ldstr, "This command requires a network or game session.");
            c.Emit<Debug>(OpCodes.Call, "Log");
        }

        private static void AddConCommandSignatureHint(On.RoR2.UI.ConsoleWindow.orig_Start orig, RoR2.UI.ConsoleWindow self)
        {
            orig(self);

            var footer = self.gameObject.transform.GetChild(0).GetChild(1);
            commandSignatureText = new GameObject("CommandHint", typeof(RectTransform));
            commandSignatureText.transform.SetParent(footer);

            var transform = commandSignatureText.transform as RectTransform;
            transform.anchorMin = new Vector2(0f, 0f);
            transform.anchorMax = new Vector2(1f, 1);
            transform.pivot = new Vector2(0f, 0.5f);
            transform.offsetMin = new Vector2(14f, 29f);
            transform.offsetMax = new Vector2(-26f, 19f);
            transform.localScale = footer.transform.GetChild(0).localScale;

            var text = commandSignatureText.AddComponent<RoR2.UI.HGTextMeshProUGUI>();
            text.fontSize = 14;
            text.overflowMode = TMPro.TextOverflowModes.Ellipsis;
            text.enableWordWrapping = false;
        }

        private static void UpdateCommandSignature(On.RoR2.UI.ConsoleWindow.orig_OnInputFieldValueChanged orig, RoR2.UI.ConsoleWindow self, string text)
        {
            orig(self, text);
            if (!commandSignatureText || !commandSignatureText.TryGetComponent<RoR2.UI.HGTextMeshProUGUI>(out var textGui))
            {
                return;
            }
            // If arriving here from submitting a command
            if (text == "")
            {
                AutoCompleteManager.ClearCommandOptions();
            }
            var command = AutoCompleteManager.CurrentCommand;
            var signature = AutoCompleteManager.CurrentSignature;
            if (command == null || signature == null)
            {
                textGui.text = "";
            }
            else
            {
                textGui.text = string.Format(COMMAND_SIGNATURE, command, signature);
            }
        }

        internal static void ScrollConsoleDown()
        {
            if (RoR2.UI.ConsoleWindow.instance && RoR2.UI.ConsoleWindow.instance.outputField.verticalScrollbar)
            {
                RoR2.UI.ConsoleWindow.instance.outputField.verticalScrollbar.value = 1f;
            }
        }

        private static bool BetterAutoCompletion(On.RoR2.Console.AutoComplete.orig_SetSearchString orig, Console.AutoComplete self, string newSearchString)
        {
            if (self.searchString == newSearchString)
            {
                return false;
            }
            self.searchString = newSearchString;
            var tokens = new Console.Lexer(newSearchString).GetTokens().ToList();
            tokens.RemoveAt(tokens.Count - 1);
            if (tokens.Count == 0)
            {
                return false;
            }
            // Since commands can be chained with a semi-colon, we need to keep the
            // latest command name so we can display the relevant autofill options.
            // Incidentally, if there is no semi-colon, the index will just be 0.
            var lastCommandNameIndex = tokens.LastIndexOf(";") + 1;
            var list = new List<MatchInfo>();
            if (tokens[tokens.Count - 1] == ";" || newSearchString.EndsWith(tokens[lastCommandNameIndex]))
            {
                AutoCompleteManager.ClearCommandOptions();
                var names = Console.instance.allConVars.Keys.ToList();
                names.AddRange(Console.instance.concommandCatalog.Keys);
                names.Sort();
                var commandName = tokens[tokens.Count - 1] == ";" ? "" : tokens[lastCommandNameIndex].ToLowerInvariant();
                foreach (var conVar in names)
                {
                    var i = conVar.IndexOf(commandName);
                    if (i == 0)
                    {
                        // Commands that start with the input string are prioritised
                        // The shorter the conVar length, the better the partial matching
                        list.Add(new MatchInfo
                        {
                            similarity = 1000 + commandName.Length - conVar.Length,
                            str = conVar.Substring(0, commandName.Length) + GrayOutText(conVar, commandName.Length)
                        });
                    }
                    else if (i > 0)
                    {
                        list.Add(new MatchInfo
                        {
                            similarity = commandName.Length - conVar.Length,
                            str = GrayOutText(conVar, 0, i) + conVar.Substring(i, commandName.Length) + GrayOutText(conVar, i + commandName.Length)
                        });
                    }
                }
            }
            else
            {
                var tokenIndex = tokens.Count - 1;
                // If we have just completed the last argument and typed space,
                // we need to provide autocompletion options for the next one
                if (!newSearchString.EndsWith(tokens[tokens.Count - 1]))
                {
                    tokenIndex++;
                    // We don't want the option index to persist when switching arguments
                    var dropdown = RoR2.UI.ConsoleWindow.instance.autoCompleteDropdown;
                    if (dropdown != null)
                    {
                        dropdown.SetValue(0, false);
                        dropdown.Hide();
                        RoR2.UI.ConsoleWindow.instance.inputField.Select();
                    }
                }
                var commandName = tokens[lastCommandNameIndex].ToLowerInvariant();
                if (commandName != AutoCompleteManager.CurrentCommand)
                {
                    AutoCompleteManager.PrepareCommandOptions(commandName);
                }
                var parameters = AutoCompleteManager.CurrentParameters;
                // The argument tokens start from position 1, while the command parameters are 0-based
                var paramIndex = tokenIndex - lastCommandNameIndex - 1;
                if (paramIndex < parameters.Length && parameters[paramIndex].Count > 0)
                {
                    var suggestions = parameters[paramIndex];
                    var tokenName = tokenIndex < tokens.Count ? tokens[tokenIndex] : "";
                    if (tokenName == "")
                    {
                        foreach (var suggestion in suggestions)
                        {
                            list.Add(new MatchInfo
                            {
                                similarity = int.MinValue,
                                str = GrayOutText(suggestion.name, 0),
                                index = suggestion.index
                            });
                        }
                    }
                    else
                    {
                        if (TextSerialization.TryParseInvariant(tokenName, out int tokenAsInt))
                        {
                            tokenName = tokenAsInt.ToString();
                        }
                        foreach (var suggestion in suggestions)
                        {
                            if (suggestion.name.IndexOf(tokenName, StringComparison.InvariantCultureIgnoreCase) >= 0)
                            {
                                var coloredStrings = new List<string>();
                                int similarity = int.MinValue;
                                foreach (var option in suggestion.name.Split('|'))
                                {
                                    var i = option.IndexOf(tokenName, StringComparison.InvariantCultureIgnoreCase);
                                    if (i == 0)
                                    {
                                        similarity = Math.Max(1000 + tokenName.Length - option.Length, similarity);
                                        coloredStrings.Add(option.Substring(0, tokenName.Length) + GrayOutText(option, tokenName.Length));
                                    }
                                    else if (i > 0)
                                    {
                                        similarity = Math.Max(tokenName.Length - option.Length, similarity);
                                        coloredStrings.Add(GrayOutText(option, 0, i) + option.Substring(i, tokenName.Length) + GrayOutText(option, i + tokenName.Length));
                                    }
                                    else
                                    {
                                        coloredStrings.Add(GrayOutText(option, 0));
                                    }
                                }
                                list.Add(new MatchInfo
                                {
                                    similarity = similarity,
                                    str = string.Join("|", coloredStrings),
                                    index = suggestion.index
                                });
                            }
                        }
                    }
                }

            }
            list = list.OrderByDescending(m => m.similarity).ToList();
            self.resultsList = list.Select(m => m.str).ToList();
            autoCompleteIndices = list.Select(m => m.index).ToArray();
            return true;

            string GrayOutText(string text, int startIndex, int length = -1)
            {
                text = length < 0 ? text.Substring(startIndex) : text.Substring(startIndex, length);
                return GRAY + text + "</color>";
            }
        }

        internal struct MatchInfo
        {
            public int similarity;
            public string str;
            public int index;
        }

        private static void ApplyTextWithoutColorTags(ILContext il)
        {
            var c = new ILCursor(il);
            if (!c.TryGotoNext(
                MoveType.After,
                x => x.MatchCallvirt(typeof(TMPro.TMP_Dropdown.OptionData), "get_text")
            ))
            {
                Log.Message("Failed to patch ConsoleWindow", Log.LogLevel.Error, Log.Target.Bepinex);
                return;
            }
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Func<string, RoR2.UI.ConsoleWindow, int, string>>((text, consoleWindow, optionIndex) =>
            {
                var options = text.Split('|');
                var index = autoCompleteIndices[optionIndex];
                if (index >= options.Length)
                {
                    index = 0;
                }
                text = options[index].Replace(GRAY, "").Replace("</color>", "");
                var inputText = consoleWindow.inputField.text;
                var tokens = new Console.Lexer(inputText).GetTokens().ToList();
                tokens.RemoveAt(tokens.Count - 1);
                if (tokens.Count == 0)
                {
                    return text;
                }
                var lastToken = tokens[tokens.Count - 1];
                if (!inputText.EndsWith(lastToken))
                {
                    return inputText + text;
                }
                return inputText.Substring(0, inputText.Length - lastToken.Length) + text;
            });
        }

        private const float changeSelectedItemTimer = 0.1f;
        private static float _lastSelectedItemChange;
        private static void SmoothDropDownSuggestionNavigation(ILContext il)
        {
            var cursor = new ILCursor(il);

            var getKey = il.Import(typeof(Input).GetMethodCached("GetKey", new[] { typeof(KeyCode) }));

            var inputKey = (int)KeyCode.UpArrow;
            cursor.GotoNext(
                MoveType.After,
                x => x.MatchLdcI4(inputKey),
                x => x.MatchCallOrCallvirt<Input>("GetKeyDown")
            );
            cursor.Prev.Operand = getKey;
            cursor.Emit(OpCodes.Ldc_I4, inputKey);
            cursor.EmitDelegate(LimitChangeItemFrequency);

            inputKey = (int)KeyCode.DownArrow;
            cursor.GotoNext(
                MoveType.After,
                x => x.MatchLdcI4(inputKey),
                x => x.MatchCallOrCallvirt<Input>("GetKeyDown")
            );
            cursor.Prev.Operand = getKey;
            cursor.Emit(OpCodes.Ldc_I4, inputKey);
            cursor.EmitDelegate(LimitChangeItemFrequency);

            cursor.GotoNext(
                x => x.MatchLdloc(3),
                x => x.MatchBrfalse(out _)
            );
            cursor.Index += 1;
            cursor.EmitDelegate(ScrollAutocompleteNextWithTab);

            for (int i = 0; i < 3; i++)
            {
                if (i != 1)
                {
                    cursor.GotoNext(
                        x => x.MatchLdloc(3),
                        x => x.MatchBrfalse(out _)
                    );
                    cursor.Index += 1;
                    cursor.EmitDelegate(ScrollAutocompleteNextWithTab);
                }
            }

            cursor.GotoNext(
                x => x.MatchLdloc(2),
                x => x.MatchBrfalse(out _)
            );
            cursor.Index += 1;
            cursor.EmitDelegate<Func<bool, bool>>(previousKeyPressed =>
            {
                return previousKeyPressed || (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Tab));
            });

            bool LimitChangeItemFrequency(bool canChangeItem, int key)
            {
                if (canChangeItem && Time.timeScale > 0f)
                {
                    var timeNow = Time.time;
                    if (timeNow > changeSelectedItemTimer * Time.timeScale + _lastSelectedItemChange)
                    {
                        _lastSelectedItemChange = timeNow;

                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (Time.timeScale <= 0f)
                {
                    return Input.GetKeyDown((KeyCode)key);
                }

                return canChangeItem;
            }

            bool ScrollAutocompleteNextWithTab(bool isNextKeyPressed)
            {
                return isNextKeyPressed || (!Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Tab));
            }
        }

        private static void AllowTabAutocompleteConCommands(ILContext il)
        {
            var c = new ILCursor(il);
            ILLabel jumpLabel = null;
            if (!c.TryGotoNext(
                    x => x.MatchLdstr("info"),
                    x => x.MatchCallOrCallvirt<Rewired.Player>("GetButtonDown")) ||
                !c.TryGotoNext(
                    MoveType.After,
                    x => x.MatchCallOrCallvirt<RoR2.UI.HUD>("get_targetBodyObject"),
                    x => x.MatchCallOrCallvirt<UnityEngine.Object>("op_Implicit"),
                    x => x.MatchBrfalse(out jumpLabel)))
            {
                Log.Message("Failed to patch RoR2.UI.HUD.Update", Log.LogLevel.Error, Log.Target.Bepinex);
                return;
            }
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<RoR2.UI.HUD, bool>>(self =>
            {
                var uiObject = self.eventSystemProvider?.eventSystem?.currentSelectedGameObject;
                return !(uiObject && uiObject == RoR2.UI.ConsoleWindow.instance?.inputField?.gameObject);
            });
            c.Emit(OpCodes.Brfalse_S, jumpLabel.Target);
        }

        private static WeightedSelection<DccsPool.Category> ForceFamilyEventForDccsPoolStages(On.RoR2.DccsPool.orig_GenerateWeightedCategorySelection orig, DccsPool self)
        {
            var selectedCategories = orig(self);
            if (CurrentRun.forceFamilyEvent)
            {
                var familyCategories = new WeightedSelection<DccsPool.Category>();
                foreach (var category in self.poolCategories)
                {
                    if (ClassicStageInfo.instance.IsCategorySelectionAFamily(category))
                    {
                        familyCategories.AddChoice(category, category.categoryWeight);
                    }
                }
                if (familyCategories.Count > 0)
                {
                    return familyCategories;
                }
            }
            return selectedCategories;
        }

        private static bool ForceFamilyDirectorCardCategorySelectionToBeAvailable(On.RoR2.FamilyDirectorCardCategorySelection.orig_IsAvailable orig, FamilyDirectorCardCategorySelection self)
        {
            return orig(self) || CurrentRun.forceFamilyEvent;
        }

        internal static void ForceFamilyEvent(On.RoR2.ClassicStageInfo.orig_RebuildCards orig, ClassicStageInfo self, DirectorCardCategorySelection forcedMonsterCategory, DirectorCardCategorySelection forcedInteractableCategory)
        {
            orig(self, forcedMonsterCategory, forcedInteractableCategory);
            CurrentRun.forceFamilyEvent = false;
        }

        internal static void SetNextBossForDirector(On.RoR2.CombatDirector.orig_SetNextSpawnAsBoss orig, CombatDirector self)
        {
            orig(self);
            /* This method is called for the following:
             * - Teleporter
             * - Simulacrum boss wave (except from the augments of Mithrix and Scav)
             * - Deep Void Signals: ignore this
             */
            if (CurrentRun.nextBoss == null || self.gameObject.name.Contains("DeepVoidPortalBattery"))
            {
                return;
            }

            bossDirector = self;

            var selectedBossCard = CurrentRun.nextBoss;
            self.OverrideCurrentMonsterCard(selectedBossCard);

            var selectedElite = CurrentRun.nextBossElite;
            self.currentActiveEliteDef = selectedElite;
            self.currentActiveEliteTier = Spawners.GetTierDef(selectedElite);
            var eliteName = (selectedElite == null) ? "non-elite" : selectedElite.name;

            var count = CurrentRun.nextBossCount;
            self.monsterCredit = selectedBossCard.cost * count * self.currentActiveEliteTier.costMultiplier;
            skipSpawnIfTooCheapBackup = self.skipSpawnIfTooCheap;
            self.skipSpawnIfTooCheap = false;

            Log.Message($"The director credits have been set to {self.monsterCredit} to spawn {count} {eliteName} {selectedBossCard.spawnCard.name}", Log.LogLevel.Info);
        }

        private static bool OverrideBossCombatDirectorSpawnResult(On.RoR2.CombatDirector.orig_AttemptSpawnOnTarget orig, CombatDirector self, Transform spawnTarget, DirectorPlacementRule.PlacementMode placementMode)
        {
            var success = orig(self, spawnTarget, placementMode);
            if (self != bossDirector)
            {
                return success;
            }

            if (bossDirector.spawnCountInCurrentWave >= CurrentRun.nextBossCount || !success)
            {
                self.skipSpawnIfTooCheap = skipSpawnIfTooCheapBackup;
                bossDirector = null;
                CurrentRun.ResetNextBoss();
                return false;
            }
            return success;
        }

        internal static void SetNextBossForSimulacrumDirector(On.RoR2.InfiniteTowerExplicitSpawnWaveController.orig_Initialize orig, InfiniteTowerExplicitSpawnWaveController self, int waveIndex, Inventory enemyInventory, GameObject spawnTargetObject)
        {
            if (CurrentRun.nextBoss != null)
            {
                self.spawnList = new InfiniteTowerExplicitSpawnWaveController.SpawnInfo[]
                {
                    new InfiniteTowerExplicitSpawnWaveController.SpawnInfo
                    {
                        spawnCard = (CharacterSpawnCard)CurrentRun.nextBoss.spawnCard,
                        eliteDef = CurrentRun.nextBossElite,
                        count = CurrentRun.nextBossCount
                    }
                };
            }
            orig(self, waveIndex, enemyInventory, spawnTargetObject);
            CurrentRun.ResetNextBoss();
        }

        internal static void SeedHook(On.RoR2.PreGameController.orig_Awake orig, PreGameController self)
        {
            orig(self);
            if (CurrentRun.seed != 0)
            {
                self.runSeed = CurrentRun.seed;
            }
        }

        internal static void DenyMapSpawns(On.RoR2.CombatDirector.orig_SpendAllCreditsOnMapSpawns_Transform_WeightedSelection1 orig, CombatDirector self, Transform mapSpawnTarget, WeightedSelection<DirectorCard> list)
        {
            self.monsterCredit = 0f;
            orig(self, mapSpawnTarget, list);
        }

        internal static void DenyExperience(On.RoR2.ExperienceManager.orig_AwardExperience orig, ExperienceManager self, Vector3 origin, CharacterBody body, ulong amount)
        {
            return;
        }

        internal static PingCache GetPingedTarget(CharacterMaster player)
        {
            if (pingedTargets.TryGetValue(player, out var ping))
            {
                return ping;
            }
            return default;
        }

        internal static bool ToggleBuddha()
        {
            return buddha = !buddha;
        }

        internal static bool ToggleGod()
        {
            return god = !god;
        }
    }
}
