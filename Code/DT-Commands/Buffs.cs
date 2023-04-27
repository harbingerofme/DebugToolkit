using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static DebugToolkit.Log;

namespace DebugToolkit.Commands
{
    class Buffs
    {
        [ConCommand(commandName = "list_buff", flags = ConVarFlags.None, helpText = Lang.LISTBUFF_HELP)]
        private static void CCListBuff(ConCommandArgs args)
        {
            var sb = new StringBuilder();
            IEnumerable<BuffIndex> buffList;
            if (args.Count > 0)
            {
                buffList = (IEnumerable<BuffIndex>)StringFinder.Instance.GetBuffsFromPartial(args.GetArgString(0));
                if (buffList.Count() == 0) sb.AppendLine($"No buff that matches \"{args[0]}\".");
            }
            else
            {
                buffList = (IEnumerable<BuffIndex>)BuffCatalog.buffDefs.Select(b => b.buffIndex);
            }
            foreach (var buffIndex in buffList)
            {
                var definition = BuffCatalog.GetBuffDef(buffIndex);
                sb.AppendLine($"[{(int)buffIndex}]{definition.name} (stackable={definition.canStack})");
            }
            Log.MessageNetworked(sb.ToString(), args, LogLevel.MessageClientOnly);
        }

        [ConCommand(commandName = "list_dot", flags = ConVarFlags.None, helpText = Lang.LISTDOT_HELP)]
        private static void CCListDot(ConCommandArgs args)
        {
            var sb = new StringBuilder();
            IEnumerable<DotController.DotIndex> dotList;
            if (args.Count > 0)
            {
                dotList = (IEnumerable<DotController.DotIndex>)StringFinder.Instance.GetDotsFromPartial(args.GetArgString(0));
                if (dotList.Count() == 0) sb.AppendLine($"No DoT that matches \"{args[0]}\".");
            }
            else
            {
                dotList = (IEnumerable<DotController.DotIndex>)Enum.GetValues(typeof(DotController.DotIndex)).Cast<DotController.DotIndex>().Where(d => d >= DotController.DotIndex.Bleed && d < DotController.DotIndex.Count);
            }
            foreach (var dotIndex in dotList)
            {
                sb.AppendLine($"[{(int)dotIndex}]{dotIndex}");
            }
            Log.MessageNetworked(sb.ToString(), args, LogLevel.MessageClientOnly);
        }

        [ConCommand(commandName = "give_buff", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.GIVEBUFF_HELP)]
        [AutoCompletion(typeof(BuffCatalog), "buffDefs")]
        private static void CCGiveBuff(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            bool isDedicatedServer = args.sender == null;
            if (args.Count == 0 || (args.Count < 4 && isDedicatedServer))
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
                Log.MessageNetworked(String.Format(Lang.NEGATIVE_ARG, "count"), args, LogLevel.MessageClientOnly);
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

            CharacterMaster target = args.senderMaster;
            if (args.Count > 3 && args[3] != Lang.DEFAULT_VALUE)
            {
                target = Util.GetTargetFromArgs(args.userArgs, 3, isDedicatedServer);
                if (target == null && !isDedicatedServer && args[3].ToUpper() == Lang.PINGED)
                {
                    Log.MessageNetworked(Lang.PINGEDBODY_NOTFOUND, args, LogLevel.MessageClientOnly);
                    return;
                }
            }
            if (target == null)
            {
                Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                return;
            }
            NetworkUser player = target.playerCharacterMasterController?.networkUser;
            string targetName = (player != null) ? player.masterController.GetDisplayName() : target.gameObject.name;

            CharacterBody body = target.GetBody();
            if (body == null)
            {
                // I believe we should only reach this for dead players, so the error message is appropriate
                Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                return;
            }

            var buff = StringFinder.Instance.GetBuffFromPartial(args[0]);
            if (buff == BuffIndex.None)
            {
                Log.MessageNetworked(Lang.OBJECT_NOTFOUND + args[0] + ":" + buff, args, LogLevel.MessageClientOnly);
                return;
            }
            // Buffs that can't stack can only get up to 1 stack. The following ceiling is so
            // we both accurately report how many stacks are granted, and also to avoid giving
            // 1000 stacks, for example, to a buff with no effect.
            var canStack = BuffCatalog.GetBuffDef(buff).canStack;
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
                Log.MessageNetworked($"Gave {iCount} {buff} to {targetName}", args);
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
                Log.MessageNetworked($"Gave {iCount} {buff} to {targetName} for {duration} seconds", args);
            }
        }

        [ConCommand(commandName = "remove_buff", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.REMOVEBUFF_HELP)]
        [AutoCompletion(typeof(BuffCatalog), "buffDefs")]
        private static void CCRemoveBuff(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            bool isDedicatedServer = args.sender == null;
            if (args.Count == 0 || (args.Count < 4 && isDedicatedServer))
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
                Log.MessageNetworked(String.Format(Lang.NEGATIVE_ARG, "count"), args, LogLevel.MessageClientOnly);
                return;
            }

            bool isTimed = false;
            if (args.Count > 2 && args[2] != Lang.DEFAULT_VALUE && !Util.TryParseBool(args[2], out isTimed))
            {
                Log.MessageNetworked(String.Format(Lang.PARSE_ERROR, "is_timed", "bool"), args, LogLevel.MessageClientOnly);
                return;
            }

            CharacterMaster target = args.senderMaster;
            if (args.Count > 3 && args[3] != Lang.DEFAULT_VALUE)
            {
                target = Util.GetTargetFromArgs(args.userArgs, 3, isDedicatedServer);
                if (target == null && !isDedicatedServer && args[3].ToUpper() == Lang.PINGED)
                {
                    Log.MessageNetworked(Lang.PINGEDBODY_NOTFOUND, args, LogLevel.MessageClientOnly);
                    return;
                }
            }
            if (target == null)
            {
                Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                return;
            }
            NetworkUser player = target.playerCharacterMasterController?.networkUser;
            string targetName = (player != null) ? player.masterController.GetDisplayName() : target.gameObject.name;

            CharacterBody body = target.GetBody();
            if (body == null)
            {
                Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                return;
            }

            var buff = StringFinder.Instance.GetBuffFromPartial(args[0]);
            if (buff == BuffIndex.None)
            {
                Log.MessageNetworked(Lang.OBJECT_NOTFOUND + args[0] + ":" + buff, args, LogLevel.MessageClientOnly);
                return;
            }
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
                Log.MessageNetworked($"Removed the {iCount} oldest timed {buff} from {targetName}", args);
            }
            else
            {
                for (int i = 0; i < iCount; i++)
                {
                    body.RemoveBuff(buff);
                }
                Log.MessageNetworked($"Removed {iCount} {buff} from {targetName}", args);
            }
        }

        [ConCommand(commandName = "remove_buff_stacks", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.REMOVEBUFFSTACKS_HELP)]
        [AutoCompletion(typeof(BuffCatalog), "buffDefs")]
        private static void CCRemoveBuffStacks(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            bool isDedicatedServer = args.sender == null;
            if (args.Count == 0 || (args.Count < 3 && isDedicatedServer))
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

            CharacterMaster target = args.senderMaster;
            if (args.Count > 2 && args[2] != Lang.DEFAULT_VALUE)
            {
                target = Util.GetTargetFromArgs(args.userArgs, 2, isDedicatedServer);
                if (target == null && !isDedicatedServer && args[2].ToUpper() == Lang.PINGED)
                {
                    Log.MessageNetworked(Lang.PINGEDBODY_NOTFOUND, args, LogLevel.MessageClientOnly);
                    return;
                }
            }
            if (target == null)
            {
                Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                return;
            }
            NetworkUser player = target.playerCharacterMasterController?.networkUser;
            string targetName = (player != null) ? player.masterController.GetDisplayName() : target.gameObject.name;

            CharacterBody body = target.GetBody();
            if (body == null)
            {
                Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                return;
            }

            var buff = StringFinder.Instance.GetBuffFromPartial(args[0]);
            if (buff == BuffIndex.None)
            {
                Log.MessageNetworked(Lang.OBJECT_NOTFOUND + args[0] + ":" + buff, args, LogLevel.MessageClientOnly);
                return;
            }
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
                Log.MessageNetworked($"Removed {stacks} timed {buff} from {targetName}", args);
            }
            else
            {
                var stacks = body.GetBuffCount(buff);
                body.SetBuffCount(buff, 0);
                Log.MessageNetworked($"Removed {stacks} {buff} from {targetName}", args);
            }
        }

        [ConCommand(commandName = "remove_all_buffs", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.REMOVEALLBUFFS_HELP)]
        private static void CCRemoveAllBuffs(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            bool isDedicatedServer = args.sender == null;
            if (args.Count < 2 && isDedicatedServer)
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

            CharacterMaster target = args.senderMaster;
            if (args.Count > 1 && args[1] != Lang.DEFAULT_VALUE)
            {
                target = Util.GetTargetFromArgs(args.userArgs, 1, isDedicatedServer);
                if (target == null && !isDedicatedServer && args[1].ToUpper() == Lang.PINGED)
                {
                    Log.MessageNetworked(Lang.PINGEDBODY_NOTFOUND, args, LogLevel.MessageClientOnly);
                    return;
                }
            }
            if (target == null)
            {
                Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                return;
            }
            NetworkUser player = target.playerCharacterMasterController?.networkUser;
            string targetName = (player != null) ? player.masterController.GetDisplayName() : target.gameObject.name;

            CharacterBody body = target.GetBody();
            if (body == null)
            {
                Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                return;
            }

            if (isTimed)
            {
                for (int i = 0; i < BuffCatalog.buffCount; i++)
                {
                    body.ClearTimedBuffs((BuffIndex)i);
                }
                Log.MessageNetworked($"Reset all timed buffs for {targetName}", args);
            }
            else
            {
                for (int i = 0; i < BuffCatalog.buffCount; i++)
                {
                    body.SetBuffCount((BuffIndex)i, 0);
                }
                Log.MessageNetworked($"Reset all buffs from {targetName}", args);
            }
        }

        [ConCommand(commandName = "give_dot", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.GIVEDOT_HELP)]
        [AutoCompletion(typeof(DotController.DotIndex), null)]
        private static void CCGiveDot(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            bool isDedicatedServer = args.sender == null;
            if (args.Count == 0 || (args.Count < 4 && isDedicatedServer))
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

            CharacterMaster targetMaster = args.senderMaster;
            if (args.Count > 2 && args[2] != Lang.DEFAULT_VALUE)
            {
                targetMaster = Util.GetTargetFromArgs(args.userArgs, 2, isDedicatedServer);
                if (targetMaster == null && !isDedicatedServer && args[2].ToUpper() == Lang.PINGED)
                {
                    Log.MessageNetworked(Lang.PINGEDBODY_NOTFOUND, args, LogLevel.MessageClientOnly);
                    return;
                }
            }
            if (targetMaster == null)
            {
                Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                return;
            }
            NetworkUser targetPlayer = targetMaster.playerCharacterMasterController?.networkUser;
            string targetName = (targetPlayer != null) ? targetPlayer.masterController.GetDisplayName() : targetMaster.gameObject.name;

            GameObject target = targetMaster.bodyInstanceObject;
            if (target == null)
            {
                Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                return;
            }

            CharacterMaster attackerMaster = args.senderMaster;
            if (args.Count > 3 && args[3] != Lang.DEFAULT_VALUE)
            {
                attackerMaster = Util.GetTargetFromArgs(args.userArgs, 3, isDedicatedServer);
                if (attackerMaster == null && !isDedicatedServer && args[3].ToUpper() == Lang.PINGED)
                {
                    Log.MessageNetworked(Lang.PINGEDBODY_NOTFOUND, args, LogLevel.MessageClientOnly);
                    return;
                }
            }
            if (attackerMaster == null)
            {
                Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                return;
            }
            NetworkUser attackerPlayer = attackerMaster.playerCharacterMasterController?.networkUser;
            string attackerName = (attackerPlayer != null) ? attackerPlayer.masterController.GetDisplayName() : attackerMaster.gameObject.name;

            GameObject attacker = attackerMaster.bodyInstanceObject;
            if (attacker == null)
            {
                Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                return;
            }

            var dot = StringFinder.Instance.GetDotFromPartial(args[0]);
            if (dot == DotController.DotIndex.None)
            {
                Log.MessageNetworked(Lang.OBJECT_NOTFOUND + args[0] + ":" + dot, args, LogLevel.MessageClientOnly);
                return;
            }
            float duration = 8f; // PercentBurn will have this
            float damageMultiplier = 1f;
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
                    var inventory = attacker.GetComponent<CharacterBody>().inventory;
                    // Let's have at least one stack
                    int stacks = (inventory != null) ? Math.Max(inventory.GetItemCount(DLC1Content.Items.StrengthenBurn), 1) : 1;
                    damageMultiplier = (1 + 3 * stacks);
                    break;
                case DotController.DotIndex.Fracture:
                    duration = DotController.GetDotDef(DotController.DotIndex.Fracture).interval;
                    break;
            }
            for (int i = 0; i < iCount; i++)
            {
                DotController.InflictDot(target, attacker, dot, duration, damageMultiplier);
            }
            Log.MessageNetworked($"Added {iCount} {dot} to {targetName} from {attackerName}", args);
        }

        [ConCommand(commandName = "remove_dot", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.REMOVEDOT_HELP)]
        private static void CCRemoveDot(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            bool isDedicatedServer = args.sender == null;
            if (args.Count == 0 || (args.Count < 3 && isDedicatedServer))
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

            CharacterMaster target = args.senderMaster;
            if (args.Count > 2 && args[2] != Lang.DEFAULT_VALUE)
            {
                target = Util.GetTargetFromArgs(args.userArgs, 2, isDedicatedServer);
                if (target == null && !isDedicatedServer && args[2].ToUpper() == Lang.PINGED)
                {
                    Log.MessageNetworked(Lang.PINGEDBODY_NOTFOUND, args, LogLevel.MessageClientOnly);
                    return;
                }
            }
            if (target == null)
            {
                Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                return;
            }
            NetworkUser player = target.playerCharacterMasterController?.networkUser;
            string targetName = (player != null) ? player.masterController.GetDisplayName() : target.gameObject.name;

            GameObject body = target.bodyInstanceObject;
            if (body == null)
            {
                Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                return;
            }

            var dot = StringFinder.Instance.GetDotFromPartial(args[0]);
            if (dot == DotController.DotIndex.None)
            {
                Log.MessageNetworked(Lang.OBJECT_NOTFOUND + args[0] + ":" + dot, args, LogLevel.MessageClientOnly);
                return;
            }
            var controller = DotController.FindDotController(body);
            if (controller == null)
            {
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
            Log.MessageNetworked($"Removed {iCount} {dot} from {targetName}", args);
        }

        [ConCommand(commandName = "remove_dot_stacks", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.REMOVEDOTSTACKS_HELP)]
        private static void CCRemoveDotStacks(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            bool isDedicatedServer = args.sender == null;
            if (args.Count == 0 || (args.Count < 2 && isDedicatedServer))
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.REMOVEDOTSTACKS_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }

            CharacterMaster target = args.senderMaster;
            if (args.Count > 1 && args[1] != Lang.DEFAULT_VALUE)
            {
                target = Util.GetTargetFromArgs(args.userArgs, 1, isDedicatedServer);
                if (target == null && !isDedicatedServer && args[1].ToUpper() == Lang.PINGED)
                {
                    Log.MessageNetworked(Lang.PINGEDBODY_NOTFOUND, args, LogLevel.MessageClientOnly);
                    return;
                }
            }
            if (target == null)
            {
                Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                return;
            }
            NetworkUser player = target.playerCharacterMasterController?.networkUser;
            string targetName = (player != null) ? player.masterController.GetDisplayName() : target.gameObject.name;

            GameObject body = target.bodyInstanceObject;
            if (body == null)
            {
                Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                return;
            }

            var dot = StringFinder.Instance.GetDotFromPartial(args[0]);
            if (dot == DotController.DotIndex.None)
            {
                Log.MessageNetworked(Lang.OBJECT_NOTFOUND + args[0] + ":" + dot, args, LogLevel.MessageClientOnly);
                return;
            }
            var controller = DotController.FindDotController(body);
            if (controller == null)
            {
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
            Log.MessageNetworked($"Removed {stacks} {dot} from {targetName}", args);
        }

        [ConCommand(commandName = "remove_all_dots", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.REMOVEALLDOTS_HELP)]
        private static void CCRemoveAllDots(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            bool isDedicatedServer = args.sender == null;
            if (args.Count < 1 && isDedicatedServer)
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.REMOVEALLDOTS_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }

            CharacterMaster target = args.senderMaster;
            if (args.Count > 0 && args[0] != Lang.DEFAULT_VALUE)
            {
                target = Util.GetTargetFromArgs(args.userArgs, 0, isDedicatedServer);
                if (target == null && !isDedicatedServer && args[0].ToUpper() == Lang.PINGED)
                {
                    Log.MessageNetworked(Lang.PINGEDBODY_NOTFOUND, args, LogLevel.MessageClientOnly);
                    return;
                }
            }
            if (target == null)
            {
                Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                return;
            }
            NetworkUser player = target.playerCharacterMasterController?.networkUser;
            string targetName = (player != null) ? player.masterController.GetDisplayName() : target.gameObject.name;

            GameObject body = target.bodyInstanceObject;
            if (body == null)
            {
                Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                return;
            }

            DotController.RemoveAllDots(body);
            Log.MessageNetworked($"Reseting DoTs for {targetName}", args);
        }
    }
}
