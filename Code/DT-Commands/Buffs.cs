using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using static DebugToolkit.Log;

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
            if (!ArgumentParser.AssertInARun(args))
            {
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
            if (!ArgumentParser.AssertInARun(args) ||
                !ArgumentParser.AssertRequiredArguments(args, Lang.GIVEBUFF_ARGS, 1, 4) ||
                !ArgumentParser.TryParseBuff(args, 0, out var buffDef) ||
                !ArgumentParser.TryParseOptionalInt(args, 1, "count", 1, out var count, min: 0) ||
                !ArgumentParser.TryParseOptionalFloat(args, 2, "duration", 0f, out var duration, min: 0f) ||
                !ArgumentParser.TryParsePlayerOrPingedBodyTarget(args, 3, out var target))
            {
                return;
            }

            var buffName = buffDef.name;
            // Buffs that can't stack can only get up to 1 stack. The following ceiling is so
            // we both accurately report how many stacks are granted, and also to avoid giving
            // 1000 stacks, for example, to a buff with no effect.
            var canStack = buffDef.canStack;
            var body = target.body;
            if (duration == 0f)
            {
                if (!canStack)
                {
                    count = Math.Min(count, 1 - body.GetBuffCount(buffDef));
                }
                for (int i = 0; i < count; i++)
                {
                    body.AddBuff(buffDef);
                }
                Log.MessageNetworked(string.Format(Lang.GIVEOBJECT_WITH_STACKS, count, buffName, target.name), args);
            }
            else
            {
                if (!canStack)
                {
                    count = Math.Min(count, 1);
                }
                for (int i = 0; i < count; i++)
                {
                    body.AddTimedBuff(buffDef, duration);
                }
                Log.MessageNetworked($"Gave {count} {buffName} to {target.name} for {duration} seconds", args);
            }
        }

        [ConCommand(commandName = "remove_buff", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.REMOVEBUFF_HELP)]
        [AutoComplete(Lang.REMOVEBUFF_ARGS)]
        private static void CCRemoveBuff(ConCommandArgs args)
        {
            if (!ArgumentParser.AssertInARun(args) ||
                !ArgumentParser.AssertRequiredArguments(args, Lang.REMOVEBUFF_ARGS, 1, 4) ||
                !ArgumentParser.TryParseBuff(args, 0, out var buffDef) ||
                !ArgumentParser.TryParseOptionalInt(args, 1, "count", 1, out var count, min: 0) ||
                !ArgumentParser.TryParseOptionalBool(args, 2, "timed", false, out var isTimed) ||
                !ArgumentParser.TryParsePlayerOrPingedBodyTarget(args, 3, out var target))
            {
                return;
            }

            var name = buffDef.name;
            var body = target.body;
            if (isTimed)
            {
                var timedBuffCount = 0;
                foreach (var timedBuff in body.timedBuffs)
                {
                    if (timedBuff.buffIndex == buffDef.buffIndex)
                    {
                        timedBuffCount++;
                    }
                }
                count = Math.Min(count, timedBuffCount);
                if (count == timedBuffCount)
                {
                    body.ClearTimedBuffs(buffDef);
                }
                else
                {
                    for (int i = 0; i < count; i++)
                    {
                        body.RemoveOldestTimedBuff(buffDef);
                    }
                }
                Log.MessageNetworked($"Removed the {count} oldest timed {name} from {target.name}.", args);
            }
            else
            {
                var buffStacks = body.GetBuffCount(buffDef);
                count = Math.Min(count, buffStacks);
                body.SetBuffCount(buffDef.buffIndex, buffStacks - count);
                Log.MessageNetworked(string.Format(Lang.REMOVEOBJECT_WITH_STACKS, count, name, target.name), args);
            }
        }

        [ConCommand(commandName = "remove_buff_stacks", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.REMOVEBUFFSTACKS_HELP)]
        [AutoComplete(Lang.REMOVEBUFFSTACKS_ARGS)]
        private static void CCRemoveBuffStacks(ConCommandArgs args)
        {
            if (!ArgumentParser.AssertInARun(args) ||
                !ArgumentParser.AssertRequiredArguments(args, Lang.REMOVEBUFFSTACKS_ARGS, 1, 3) ||
                !ArgumentParser.TryParseBuff(args, 0, out var buffDef) ||
                !ArgumentParser.TryParseOptionalBool(args, 1, "timed", false, out var isTimed) ||
                !ArgumentParser.TryParsePlayerOrPingedBodyTarget(args, 2, out var target))
            {
                return;
            }

            var name = buffDef.name;
            var body = target.body;
            if (isTimed)
            {
                var stacks = 0;
                foreach (var timedBuff in body.timedBuffs)
                {
                    if (timedBuff.buffIndex == buffDef.buffIndex)
                    {
                        stacks++;
                    }
                }
                body.ClearTimedBuffs(buffDef);
                Log.MessageNetworked(string.Format(Lang.REMOVEOBJECT_WITH_STACKS, stacks, "timed " + name, target.name), args);
            }
            else
            {
                var stacks = body.GetBuffCount(buffDef);
                body.SetBuffCount(buffDef.buffIndex, 0);
                Log.MessageNetworked(string.Format(Lang.REMOVEOBJECT_WITH_STACKS, stacks, name, target.name), args);
            }
        }

        [ConCommand(commandName = "remove_all_buffs", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.REMOVEALLBUFFS_HELP)]
        [AutoComplete(Lang.REMOVEALLBUFFS_ARGS)]
        private static void CCRemoveAllBuffs(ConCommandArgs args)
        {
            if (!ArgumentParser.AssertInARun(args) ||
                !ArgumentParser.AssertRequiredArguments(args, Lang.REMOVEALLBUFFS_ARGS, 0, 2) ||
                !ArgumentParser.TryParseOptionalBool(args, 0, "is_timed", false, out var isTimed) ||
                !ArgumentParser.TryParsePlayerOrPingedBodyTarget(args, 1, out var target))
            {
                return;
            }

            var body = target.body;
            if (isTimed)
            {
                for (int i = 0; i < BuffCatalog.buffCount; i++)
                {
                    body.ClearTimedBuffs((BuffIndex)i);
                }
                Log.MessageNetworked(string.Format(Lang.RESETOBJECT, "all timed buffs", target.name), args);
            }
            else
            {
                for (int i = 0; i < BuffCatalog.buffCount; i++)
                {
                    body.SetBuffCount((BuffIndex)i, 0);
                }
                Log.MessageNetworked(string.Format(Lang.RESETOBJECT, "all buffs", target.name), args);
            }
        }

        [ConCommand(commandName = "give_dot", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.GIVEDOT_HELP)]
        [AutoComplete(Lang.GIVEDOT_ARGS)]
        private static void CCGiveDot(ConCommandArgs args)
        {
            if (!ArgumentParser.AssertInARun(args) ||
                !ArgumentParser.AssertRequiredArguments(args, Lang.GIVEDOT_ARGS, 1, 4) ||
                !ArgumentParser.TryParseDot(args, 0, out var dotIndex) ||
                !ArgumentParser.TryParseOptionalInt(args, 1, "count", 1, out var count, min: 0) ||
                !ArgumentParser.TryParsePlayerOrPingedBodyTarget(args, 2, out var target) ||
                !ArgumentParser.TryParsePlayerOrPingedBodyTarget(args, 3, out var attacker))
            {
                return;
            }

            float duration = 5f; // Fallback default
            float damageMultiplier = 1f;
            uint? maxStacksFromAttacker = null;
            switch (dotIndex)
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
                dotIndex = dotIndex,
                duration = duration,
                damageMultiplier = damageMultiplier,
                maxStacksFromAttacker = maxStacksFromAttacker,
            };
            for (int i = 0; i < count; i++)
            {
                DotController.InflictDot(ref dotInfo);
            }
            Log.MessageNetworked($"Added {count} {dotIndex} to {target.name} from {attacker.name}", args);
        }

        [ConCommand(commandName = "remove_dot", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.REMOVEDOT_HELP)]
        [AutoComplete(Lang.REMOVEDOT_ARGS)]
        private static void CCRemoveDot(ConCommandArgs args)
        {
            if (!ArgumentParser.AssertInARun(args) ||
                !ArgumentParser.AssertRequiredArguments(args, Lang.REMOVEDOT_ARGS, 1, 3) ||
                !ArgumentParser.TryParseDot(args, 0, out var dotIndex) ||
                !ArgumentParser.TryParseOptionalInt(args, 1, "count", 1, out var count, min: 0) ||
                !ArgumentParser.TryParsePlayerOrPingedBodyTarget(args, 2, out var target))
            {
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
                if (stack.dotIndex == dotIndex)
                {
                    dotStacks.Add(new KeyValuePair<int, float>(i, stack.timer));
                }
            }
            // Sorting from longest to shortest expiration timer
            dotStacks.Sort((p1, p2) => p2.Value.CompareTo(p1.Value));
            count = Math.Min(count, dotStacks.Count);
            for (int i = 0; i < count; i++)
            {
                controller.RemoveDotStackAtServer(dotStacks[i].Key);
            }
            Log.MessageNetworked(string.Format(Lang.REMOVEOBJECT_WITH_STACKS, count, dotIndex, target.name), args);
        }

        [ConCommand(commandName = "remove_dot_stacks", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.REMOVEDOTSTACKS_HELP)]
        [AutoComplete(Lang.REMOVEDOTSTACKS_ARGS)]
        private static void CCRemoveDotStacks(ConCommandArgs args)
        {
            if (!ArgumentParser.AssertInARun(args) ||
                !ArgumentParser.AssertRequiredArguments(args, Lang.REMOVEDOTSTACKS_ARGS, 1, 2) ||
                !ArgumentParser.TryParseDot(args, 0, out var dotIndex) ||
                !ArgumentParser.TryParsePlayerOrPingedBodyTarget(args, 1, out var target))
            {
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
                if (stack.dotIndex == dotIndex)
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
            Log.MessageNetworked(string.Format(Lang.REMOVEOBJECT_WITH_STACKS, stacks, dotIndex, target.name), args);
        }

        [ConCommand(commandName = "remove_all_dots", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.REMOVEALLDOTS_HELP)]
        [AutoComplete(Lang.REMOVEALLDOTS_ARGS)]
        private static void CCRemoveAllDots(ConCommandArgs args)
        {
            if (!ArgumentParser.AssertInARun(args) ||
                !ArgumentParser.AssertRequiredArguments(args, Lang.REMOVEALLDOTS_ARGS, 0, 1) ||
                !ArgumentParser.TryParsePlayerOrPingedBodyTarget(args, 0, out var target))
            {
                return;
            }

            DotController.RemoveAllDots(target.body.gameObject);
            Log.MessageNetworked(string.Format(Lang.RESETOBJECT, "all DoTs", target.name), args);
        }
    }
}
