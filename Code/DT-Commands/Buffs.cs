using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using static DebugToolkit.Log;
using static DebugToolkit.Util;

namespace DebugToolkit.Commands
{
    class Buffs
    {
        [ConCommand(commandName = "list_buff", flags = ConVarFlags.None, helpText = Lang.LISTBUFF_HELP)]
        [AutoComplete(Lang.LISTQUERY_ARGS)]
        private static void CCListBuff(ConCommandArgs args)
        {
            var sb = new StringBuilder();
            var arg = args.Count > 0 ? args[0] : "";
            var indices = StringFinder.Instance.GetBuffsFromPartial(arg);
            foreach (var index in indices)
            {
                var buff = BuffCatalog.GetBuffDef(index);
                sb.AppendLine($"[{(int)index}]{buff.name} (stackable={buff.canStack})");
            }
            var s = sb.Length > 0 ? sb.ToString().TrimEnd('\n') : string.Format(Lang.NOMATCH_ERROR, "buffs", arg);
            Log.MessageNetworked(s, args, LogLevel.MessageClientOnly);
        }

        [ConCommand(commandName = "list_dot", flags = ConVarFlags.None, helpText = Lang.LISTDOT_HELP)]
        [AutoComplete(Lang.LISTQUERY_ARGS)]
        private static void CCListDot(ConCommandArgs args)
        {
            var sb = new StringBuilder();
            var arg = args.Count > 0 ? args[0] : "";
            var indices = StringFinder.Instance.GetDotsFromPartial(arg);
            foreach (var index in indices)
            {
                sb.AppendLine($"[{(int)index}]{index}");
            }
            var s = sb.Length > 0 ? sb.ToString().TrimEnd('\n') : string.Format(Lang.NOMATCH_ERROR, "DoT", arg);
            Log.MessageNetworked(s, args, LogLevel.MessageClientOnly);
        }

        [ConCommand(commandName = "dump_buffs", flags = ConVarFlags.None, helpText = Lang.DUMPBUFFS_HELP)]
        private static void CCDumpBuffs(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.Message(Lang.NOTINARUN_ERROR);
                return;
            }
            var sb = new StringBuilder();
            foreach (var body in CharacterBody.readOnlyInstancesList)
            {
                sb.AppendLine($"--- {body.name} {body.corePosition}");
                foreach (var buffDef in BuffCatalog.buffDefs)
                {
                    var count = body.GetBuffCount(buffDef);
                    if (count != 0)
                    {
                        var colorHexString = RoR2.Util.RGBToHex(buffDef.buffColor);
                        sb.AppendLine($"<color=#{colorHexString}>{buffDef.name}</color> {count}");
                    }
                }
                sb.AppendLine();
            }
            Log.MessageNetworked(sb.ToString().TrimEnd('\n'), args, LogLevel.MessageClientOnly);
        }

        [ConCommand(commandName = "give_buff", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.GIVEBUFF_HELP)]
        [AutoComplete(Lang.GIVEBUFF_ARGS)]
        private static void CCGiveBuff(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            bool isDedicatedServer = args.sender == null;
            if (args.Count == 0 || (isDedicatedServer && (args.Count < 4 || args[3] == Lang.DEFAULT_VALUE)))
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.GIVEBUFF_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }

            int iCount = 1;
            if (args.Count > 1 && args[1] != Lang.DEFAULT_VALUE && !TextSerialization.TryParseInvariant(args[1], out iCount))
            {
                Log.MessageNetworked(String.Format(Lang.PARSE_ERROR, "count", "int"), args, LogLevel.MessageClientOnly);
                return;
            }
            if (iCount < 0)
            {
                args.userArgs[1] = (-iCount).ToString();
                CCRemoveBuff(args);
                return;
            }

            float duration = 0f;
            if (args.Count > 2 && args[2] != Lang.DEFAULT_VALUE && !TextSerialization.TryParseInvariant(args[2], out duration))
            {
                Log.MessageNetworked(String.Format(Lang.PARSE_ERROR, "duration", "float"), args, LogLevel.MessageClientOnly);
                return;
            }
            if (duration < 0f)
            {
                Log.MessageNetworked(String.Format(Lang.NEGATIVE_ARG, "duration"), args, LogLevel.MessageClientOnly);
                return;
            }

            var target = ParseTarget(args, 3);
            if (target.failMessage != null)
            {
                Log.MessageNetworked(target.failMessage, args, LogLevel.MessageClientOnly);
                return;
            }

            var buff = StringFinder.Instance.GetBuffFromPartial(args[0]);
            if (buff == BuffIndex.None)
            {
                Log.MessageNetworked(string.Format(Lang.OBJECT_NOTFOUND, "buff", args[0]), args, LogLevel.MessageClientOnly);
                return;
            }
            var name = BuffCatalog.GetBuffDef(buff).name;
            // Buffs that can't stack can only get up to 1 stack. The following ceiling is so
            // we both accurately report how many stacks are granted, and also to avoid giving
            // 1000 stacks, for example, to a buff with no effect.
            var canStack = BuffCatalog.GetBuffDef(buff).canStack;
            var body = target.body;
            if (duration == 0f)
            {
                if (!canStack)
                {
                    iCount = Math.Min(iCount, 1 - body.GetBuffCount(buff));
                }
                for (int i = 0; i < iCount; i++)
                {
                    body.AddBuff(buff);
                }
                Log.MessageNetworked(string.Format(Lang.GIVEOBJECT, iCount, name, target.name), args);
            }
            else
            {
                if (!canStack)
                {
                    iCount = Math.Min(iCount, 1);
                }
                for (int i = 0; i < iCount; i++)
                {
                    body.AddTimedBuff(buff, duration);
                }
                Log.MessageNetworked($"Gave {iCount} {name} to {target.name} for <color=#53E9FF>{duration} seconds</color>", args);
            }
        }

        [ConCommand(commandName = "remove_buff", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.REMOVEBUFF_HELP)]
        [AutoComplete(Lang.REMOVEBUFF_ARGS)]
        private static void CCRemoveBuff(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            bool isDedicatedServer = args.sender == null;
            if (args.Count == 0 || (isDedicatedServer && (args.Count < 4 || args[3] == Lang.DEFAULT_VALUE)))
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.REMOVEBUFF_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }

            int iCount = 1;
            if (args.Count > 1 && args[1] != Lang.DEFAULT_VALUE && !TextSerialization.TryParseInvariant(args[1], out iCount))
            {
                Log.MessageNetworked(String.Format(Lang.PARSE_ERROR, "count", "int"), args, LogLevel.MessageClientOnly);
                return;
            }
            if (iCount < 0)
            {
                args.userArgs[1] = (-iCount).ToString();
                CCGiveBuff(args);
                return;
            }

            bool isTimed = false;
            if (args.Count > 2 && args[2] != Lang.DEFAULT_VALUE && !Util.TryParseBool(args[2], out isTimed))
            {
                Log.MessageNetworked(String.Format(Lang.PARSE_ERROR, "is_timed", "bool"), args, LogLevel.MessageClientOnly);
                return;
            }

            var target = ParseTarget(args, 3);
            if (target.failMessage != null)
            {
                Log.MessageNetworked(target.failMessage, args, LogLevel.MessageClientOnly);
                return;
            }

            var buff = StringFinder.Instance.GetBuffFromPartial(args[0]);
            if (buff == BuffIndex.None)
            {
                Log.MessageNetworked(string.Format(Lang.OBJECT_NOTFOUND, "buff", args[0]), args, LogLevel.MessageClientOnly);
                return;
            }
            var name = BuffCatalog.GetBuffDef(buff).name;
            var body = target.body;
            if (isTimed)
            {
                var timedBuffCount = 0;
                foreach (var timedBuff in body.timedBuffs)
                {
                    if (timedBuff.buffIndex == buff)
                    {
                        timedBuffCount++;
                    }
                }
                iCount = Math.Min(iCount, timedBuffCount);
                if (iCount == timedBuffCount)
                {
                    body.ClearTimedBuffs(buff);
                }
                else
                {
                    for (int i = 0; i < iCount; i++)
                    {
                        body.RemoveOldestTimedBuff(buff);
                    }
                }
                Log.MessageNetworked($"Removed the {iCount} oldest timed {name} from {target.name}", args);
            }
            else
            {
                var buffStacks = body.GetBuffCount(buff);
                iCount = Math.Min(iCount, buffStacks);
                body.SetBuffCount(buff, buffStacks - iCount);
                Log.MessageNetworked(string.Format(Lang.REMOVEOBJECT, iCount, name, target.name), args);
            }
        }

        [ConCommand(commandName = "remove_buff_stacks", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.REMOVEBUFFSTACKS_HELP)]
        [AutoComplete(Lang.REMOVEBUFFSTACKS_ARGS)]
        private static void CCRemoveBuffStacks(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            bool isDedicatedServer = args.sender == null;
            if (args.Count == 0 || (isDedicatedServer && (args.Count < 3 || args[2] == Lang.DEFAULT_VALUE)))
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.REMOVEBUFFSTACKS_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }

            bool isTimed = false;
            if (args.Count > 1 && args[1] != Lang.DEFAULT_VALUE && !Util.TryParseBool(args[1], out isTimed))
            {
                Log.MessageNetworked(String.Format(Lang.PARSE_ERROR, "is_timed", "bool"), args, LogLevel.MessageClientOnly);
                return;
            }

            var target = ParseTarget(args, 2);
            if (target.failMessage != null)
            {
                Log.MessageNetworked(target.failMessage, args, LogLevel.MessageClientOnly);
                return;
            }

            var buff = StringFinder.Instance.GetBuffFromPartial(args[0]);
            if (buff == BuffIndex.None)
            {
                Log.MessageNetworked(string.Format(Lang.OBJECT_NOTFOUND, "buff", args[0]), args, LogLevel.MessageClientOnly);
                return;
            }
            var name = BuffCatalog.GetBuffDef(buff).name;
            var body = target.body;
            if (isTimed)
            {
                var stacks = 0;
                foreach (var timedBuff in body.timedBuffs)
                {
                    if (timedBuff.buffIndex == buff)
                    {
                        stacks++;
                    }
                }
                body.ClearTimedBuffs(buff);
                Log.MessageNetworked(string.Format(Lang.REMOVEOBJECT, stacks, "timed " + name, target.name), args);
            }
            else
            {
                var stacks = body.GetBuffCount(buff);
                body.SetBuffCount(buff, 0);
                Log.MessageNetworked(string.Format(Lang.REMOVEOBJECT, stacks, name, target.name), args);
            }
        }

        [ConCommand(commandName = "remove_all_buffs", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.REMOVEALLBUFFS_HELP)]
        [AutoComplete(Lang.REMOVEALLBUFFS_ARGS)]
        private static void CCRemoveAllBuffs(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            bool isDedicatedServer = args.sender == null;
            if (isDedicatedServer && (args.Count < 2 || args[1] == Lang.DEFAULT_VALUE))
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.REMOVEALLBUFFS_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }

            bool isTimed = false;
            if (args.Count > 0 && args[0] != Lang.DEFAULT_VALUE && !Util.TryParseBool(args[0], out isTimed))
            {
                Log.MessageNetworked(String.Format(Lang.PARSE_ERROR, "is_timed", "bool"), args, LogLevel.MessageClientOnly);
                return;
            }

            var target = ParseTarget(args, 1);
            if (target.failMessage != null)
            {
                Log.MessageNetworked(target.failMessage, args, LogLevel.MessageClientOnly);
                return;
            }

            var body = target.body;
            if (isTimed)
            {
                for (int i = 0; i < BuffCatalog.buffCount; i++)
                {
                    body.ClearTimedBuffs((BuffIndex)i);
                }
                Log.MessageNetworked($"Reset all timed buffs for {target.name}", args);
            }
            else
            {
                for (int i = 0; i < BuffCatalog.buffCount; i++)
                {
                    body.SetBuffCount((BuffIndex)i, 0);
                }
                Log.MessageNetworked($"Reset all buffs for {target.name}", args);
            }
        }

        [ConCommand(commandName = "give_dot", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.GIVEDOT_HELP)]
        [AutoComplete(Lang.GIVEDOT_ARGS)]
        private static void CCGiveDot(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            bool isDedicatedServer = args.sender == null;
            if (args.Count == 0 || (isDedicatedServer && (args.Count < 4 || args[2] == Lang.DEFAULT_VALUE || args[3] == Lang.DEFAULT_VALUE)))
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.GIVEDOT_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }

            int iCount = 1;
            if (args.Count > 1 && args[1] != Lang.DEFAULT_VALUE && !TextSerialization.TryParseInvariant(args[1], out iCount))
            {
                Log.MessageNetworked(String.Format(Lang.PARSE_ERROR, "count", "int"), args, LogLevel.MessageClientOnly);
                return;
            }

            var target = ParseTarget(args, 2);
            if (target.failMessage != null)
            {
                Log.MessageNetworked(target.failMessage, args, LogLevel.MessageClientOnly);
                return;
            }

            var attacker = ParseTarget(args, 3);
            if (attacker.failMessage != null)
            {
                Log.MessageNetworked(attacker.failMessage, args, LogLevel.MessageClientOnly);
                return;
            }

            var dot = StringFinder.Instance.GetDotFromPartial(args[0]);
            if (dot == DotController.DotIndex.None)
            {
                Log.MessageNetworked(string.Format(Lang.OBJECT_NOTFOUND, "dot", args[0]), args, LogLevel.MessageClientOnly);
                return;
            }
            float duration = 5f; // Fallback default
            float damageMultiplier = 1f;
            uint? maxStacksFromAttacker = null;
            switch (dot)
            {
                case DotController.DotIndex.Bleed:
                    duration = 4f;
                    break;
                case DotController.DotIndex.Burn:
                    // The burn duration has multiple different sources, e.g. gasoline, molotov, grandparent, and elites. Hardcoding this for now.
                    duration = 3f;
                    break;
                case DotController.DotIndex.Helfire:
                    duration = 12f;
                    break;
                case DotController.DotIndex.PercentBurn:
                    duration = 8f;
                    break;
                case DotController.DotIndex.Poison:
                    duration = 10f;
                    break;
                case DotController.DotIndex.Blight:
                    duration = 5f;
                    break;
                case DotController.DotIndex.SuperBleed:
                    duration = 15f;
                    break;
                case DotController.DotIndex.StrongerBurn:
                    duration = 3f;
                    var inventory = attacker.body.inventory;
                    // Let's have at least one stack
                    int stacks = (inventory != null) ? Math.Max(inventory.GetItemCountEffective(DLC1Content.Items.StrengthenBurn), 1) : 1;
                    damageMultiplier = (1 + 3 * stacks);
                    break;
                case DotController.DotIndex.Fracture:
                    duration = DotController.GetDotDef(DotController.DotIndex.Fracture).interval;
                    break;
                case DotController.DotIndex.LunarRuin:
                    duration = 5f;
                    break;
                case DotController.DotIndex.Electrocution:
                    duration = 4.7f;
                    maxStacksFromAttacker = 1U;
                    break;
                default:
                    Log.MessageNetworked($"No explicit duration set for this DoT, defaulting to {duration}. " + Lang.NOMESSAGE, args, LogLevel.MessageClientOnly);
                    break;
            }
            var dotInfo = new InflictDotInfo
            {
                victimObject = target.body.gameObject,
                attackerObject = attacker.body.gameObject,
                dotIndex = dot,
                duration = duration,
                damageMultiplier = damageMultiplier,
                maxStacksFromAttacker = maxStacksFromAttacker,
            };
            for (int i = 0; i < iCount; i++)
            {
                DotController.InflictDot(ref dotInfo);
            }
            Log.MessageNetworked($"Added {iCount} {dot} to {target.name} from {attacker.name}", args);
        }

        [ConCommand(commandName = "remove_dot", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.REMOVEDOT_HELP)]
        [AutoComplete(Lang.REMOVEDOT_ARGS)]
        private static void CCRemoveDot(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            bool isDedicatedServer = args.sender == null;
            if (args.Count == 0 || (isDedicatedServer && (args.Count < 3 || args[2] == Lang.DEFAULT_VALUE)))
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.REMOVEDOT_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }

            int iCount = 1;
            if (args.Count > 1 && args[1] != Lang.DEFAULT_VALUE && !TextSerialization.TryParseInvariant(args[1], out iCount))
            {
                Log.MessageNetworked(String.Format(Lang.PARSE_ERROR, "count", "int"), args, LogLevel.MessageClientOnly);
                return;
            }

            var target = ParseTarget(args, 2);
            if (target.failMessage != null)
            {
                Log.MessageNetworked(target.failMessage, args, LogLevel.MessageClientOnly);
                return;
            }

            var dot = StringFinder.Instance.GetDotFromPartial(args[0]);
            if (dot == DotController.DotIndex.None)
            {
                Log.MessageNetworked(string.Format(Lang.OBJECT_NOTFOUND, "dot", args[0]), args, LogLevel.MessageClientOnly);
                return;
            }
            var controller = DotController.FindDotController(target.body.gameObject);
            if (controller == null)
            {
                Log.MessageNetworked(Lang.DOTCONTROLLER_NOTFOUND, args, LogLevel.MessageClientOnly);
                return;
            }
            var dotStacks = new List<KeyValuePair<int, float>>();
            for (int i = controller.dotStackList.Count - 1; i >= 0; i--)
            {
                var stack = controller.dotStackList[i];
                if (stack.dotIndex == dot)
                {
                    dotStacks.Add(new KeyValuePair<int, float>(i, stack.timer));
                }
            }
            // Sorting from longest to shortest expiration timer
            dotStacks.Sort((p1, p2) => p2.Value.CompareTo(p1.Value));
            iCount = Math.Min(iCount, dotStacks.Count);
            for (int i = 0; i < iCount; i++)
            {
                controller.RemoveDotStackAtServer(dotStacks[i].Key);
            }
            Log.MessageNetworked(string.Format(Lang.REMOVEOBJECT, iCount, dot, target.name), args);
        }

        [ConCommand(commandName = "remove_dot_stacks", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.REMOVEDOTSTACKS_HELP)]
        [AutoComplete(Lang.REMOVEDOTSTACKS_ARGS)]
        private static void CCRemoveDotStacks(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            bool isDedicatedServer = args.sender == null;
            if (args.Count == 0 || (isDedicatedServer && (args.Count < 2 || args[1] == Lang.DEFAULT_VALUE)))
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.REMOVEDOTSTACKS_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }

            var target = ParseTarget(args, 1);
            if (target.failMessage != null)
            {
                Log.MessageNetworked(target.failMessage, args, LogLevel.MessageClientOnly);
                return;
            }

            var dot = StringFinder.Instance.GetDotFromPartial(args[0]);
            if (dot == DotController.DotIndex.None)
            {
                Log.MessageNetworked(string.Format(Lang.OBJECT_NOTFOUND, "dot", args[0]), args, LogLevel.MessageClientOnly);
                return;
            }
            var controller = DotController.FindDotController(target.body.gameObject);
            if (controller == null)
            {
                Log.MessageNetworked(Lang.DOTCONTROLLER_NOTFOUND, args, LogLevel.MessageClientOnly);
                return;
            }
            int stacks = 0;
            for (int i = controller.dotStackList.Count - 1; i >= 0; i--)
            {
                var stack = controller.dotStackList[i];
                if (stack.dotIndex == dot)
                {
                    controller.RemoveDotStackAtServer(i);
                    stacks++;
                }
            }
            // This is going to happen in the next frame anyway
            if (controller.dotStackList.Count == 0)
            {
                UnityEngine.Object.Destroy(controller.gameObject);
            }
            Log.MessageNetworked(string.Format(Lang.REMOVEOBJECT, stacks, dot, target.name), args);
        }

        [ConCommand(commandName = "remove_all_dots", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.REMOVEALLDOTS_HELP)]
        [AutoComplete(Lang.REMOVEALLDOTS_ARGS)]
        private static void CCRemoveAllDots(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            bool isDedicatedServer = args.sender == null;
            if (isDedicatedServer && (args.Count < 1 || args[0] == Lang.DEFAULT_VALUE))
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.REMOVEALLDOTS_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }

            var target = ParseTarget(args, 0);
            if (target.failMessage != null)
            {
                Log.MessageNetworked(target.failMessage, args, LogLevel.MessageClientOnly);
                return;
            }

            DotController.RemoveAllDots(target.body.gameObject);
            Log.MessageNetworked($"Reseting DoTs for {target.name}", args);
        }

        internal static CommandTarget ParseTarget(ConCommandArgs args, int index)
        {
            string failMessage = null;
            var target = args.senderBody;
            string targetName = null;
            var isDedicatedServer = args.sender == null;
            if (args.Count > index && args[index] != Lang.DEFAULT_VALUE)
            {
                // Try to get target from the master initially to account for ping -> target revival
                // as in that case the cached pinged body would be stale.
                var targetMaster = Util.GetTargetFromArgs(args, index);
                if (targetMaster == null && !isDedicatedServer && args[index].ToUpperInvariant() == Lang.PINGED)
                {
                    // Account for masterless bodies
                    target = Hooks.GetPingedTarget(args.senderMaster).body;
                    if (target == null)
                    {
                        failMessage = Lang.PINGEDBODY_NOTFOUND;
                    }
                }
                else
                {
                    target = targetMaster?.GetBody();
                }
            }
            if (target == null && failMessage == null)
            {
                failMessage = Lang.PLAYER_NOTFOUND;
            }

            if (failMessage != null)
            {
                return new CommandTarget
                {
                    failMessage = failMessage
                };
            }
            var player = target.master?.playerCharacterMasterController?.networkUser;
            targetName = (player != null) ? player.masterController.GetDisplayName() : target.gameObject.name;
            return new CommandTarget
            {
                body = target,
                name = targetName
            };
        }
    }
}
