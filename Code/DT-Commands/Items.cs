
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static DebugToolkit.Log;

namespace DebugToolkit.Commands
{
    class Items
    {
        [ConCommand(commandName = "list_item", flags = ConVarFlags.None, helpText = "List all items and their availability. "+ Lang.LISTITEM_ARGS)]
        private static void CCListItem(ConCommandArgs args)
        {
            var sb = new StringBuilder();
            IEnumerable<ItemIndex> itemList;
            if (args.Count > 0)
            {
                itemList = (IEnumerable<ItemIndex>)StringFinder.Instance.GetItemsFromPartial(args.GetArgString(0));
                if (itemList.Count() == 0) sb.AppendLine($"No item that matches \"{args.GetArgString(0)}\".");
            } else
            {
                itemList = (IEnumerable<ItemIndex>)ItemCatalog.allItems;
            }
            foreach (var itemIndex in itemList)
            {
                var definition = ItemCatalog.GetItemDef(itemIndex);
                string enabled = false.ToString();
                var realName = Language.currentLanguage.GetLocalizedStringByToken(definition.nameToken);
                if (Run.instance)
                {
                    enabled = Run.instance.IsItemAvailable(itemIndex).ToString();
                }
                sb.AppendLine($"[{(int)itemIndex}]: {definition.name} \"{realName}\" (enabled={enabled})");
            }
            Log.Message(sb.ToString());
        }

        [ConCommand(commandName = "list_equip", flags = ConVarFlags.None, helpText = "List all equipment and their availability. " + Lang.LISTEQUIP_ARGS)]
        private static void CCListEquip(ConCommandArgs args)
        {
            var sb = new StringBuilder();
            IEnumerable<EquipmentIndex> list;
            if (args.Count > 0)
            {
                list = StringFinder.Instance.GetEquipsFromPartial(args.GetArgString(0));
                    if (list.Count() == 0) sb.AppendLine($"No equipment that matches \"{args.GetArgString(0)}\".");
            }
            else
            {
                list = EquipmentCatalog.allEquipment;
            }

            foreach (var equipmentIndex in list)
            {
                var definition = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
                string enabled = false.ToString();
                var realName = Language.currentLanguage.GetLocalizedStringByToken(definition.nameToken);
                if (Run.instance)
                {
                    enabled = Run.instance.IsEquipmentAvailable(equipmentIndex).ToString();
                }
                sb.AppendLine($"[{(int)equipmentIndex}]: {definition.name} \"{realName}\"  (enabled={enabled})");
            }
            Log.Message(sb.ToString());
        }


        [ConCommand(commandName = "give_item", flags = ConVarFlags.ExecuteOnServer, helpText = "Gives the specified item to a character. " + Lang.GIVEITEM_ARGS)]
        [AutoCompletion(typeof(ItemCatalog), "itemDefs", "nameToken")]
        private static void CCGiveItem(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Log.MessageNetworked(Lang.GIVEITEM_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }

            bool isDedicatedServer = false;
            if (args.sender == null)
            {
                isDedicatedServer = true;
                if (args.Count < 2)
                {
                    Log.Message(Lang.DS_REQUIREFULLQUALIFY, LogLevel.Error);
                    Log.Message(Lang.GIVEITEM_ARGS, LogLevel.Message);
                    return;
                }
            }

            CharacterBody target = args.sender?.GetCurrentBody();
            string targetName = args.sender?.masterController.GetDisplayName();

            int iCount = 1; // it'll get overwritten by the TryParse...
            bool secondArgIsCount = false;
            if (args.Count >= 2)
            {
                // secondArgIsCount allow to give directly the player name without specifying item count
                secondArgIsCount = int.TryParse(args[1], out iCount);
                bool hasTargetArg = !secondArgIsCount || args.Count >= 3;
                int targetArgIndex = secondArgIsCount ? 2 : 1;
                bool hasFoundPlayer = false;

                if (isDedicatedServer)
                {
                    if (hasTargetArg)
                    {
                        hasFoundPlayer = Util.GetBodyFromUser(args.userArgs, targetArgIndex, out target, out targetName);
                    }
                }
                else
                {
                    // We default to the command caller
                    hasFoundPlayer = true;
                    if (hasTargetArg)
                    {
                        if (args[targetArgIndex].ToUpper() == Lang.PINGED)
                        {
                            var pingedBody = Hooks.GetPingedBody();
                            if (pingedBody)
                            {
                                target = pingedBody;
                                targetName = target.gameObject.name;
                            }
                            else
                            {
                                Log.MessageNetworked(Lang.PINGEDBODY_NOTFOUND, args, LogLevel.MessageClientOnly);
                                return;
                            }
                        }
                        else
                        {
                            hasFoundPlayer = Util.GetBodyFromUser(args.userArgs, targetArgIndex, out target, out targetName);
                        }
                    }
                }

                if (!hasFoundPlayer)
                {
                    Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                    return;
                }
            }

            iCount = secondArgIsCount ? iCount : 1;
            Inventory inventory = target.inventory;
            if (inventory == null)
            {
                Log.MessageNetworked(Lang.INVENTORY_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }

            var item = StringFinder.Instance.GetItemFromPartial(args[0]);
            if (item != ItemIndex.None)
            {
                inventory.GiveItem(item, iCount);
                Log.MessageNetworked($"Gave {iCount} {item} to {targetName}", args);
            }
            else
            {
                Log.MessageNetworked(Lang.OBJECT_NOTFOUND + args[0] + ":" + item, args, LogLevel.MessageClientOnly);
            }
        }

        [ConCommand(commandName = "random_items", flags = ConVarFlags.ExecuteOnServer, helpText = "Generates the specified amount of items for a character from the available item pools at random.")]
        private static void CCRandom_items(ConCommandArgs args)
        {
            bool isDedicatedServer = false;
            if (args.sender == null)
            {
                isDedicatedServer = true;
                if (args.Count < 2)
                {
                    Log.Message(Lang.DS_REQUIREFULLQUALIFY, LogLevel.Error);
                    Log.Message(Lang.RANDOM_ITEM_ARGS, LogLevel.Message);
                    return;
                }
            }

            if (args.Count < 1)
            {
                Log.MessageNetworked(Lang.RANDOM_ITEM_ARGS, args);
                return;
            }

            CharacterBody target = args.sender?.GetCurrentBody();
            string targetName = args.sender?.masterController.GetDisplayName();

            if (args.Count >= 2)
            {
                bool hasFoundPlayer = false;
                if (isDedicatedServer)
                {
                    hasFoundPlayer = Util.GetBodyFromUser(args.userArgs, 1, out target, out targetName);
                }
                else
                {
                    hasFoundPlayer = true;
                    if (args[1].ToUpper() == Lang.PINGED)
                    {
                        var pingedBody = Hooks.GetPingedBody();
                        if (pingedBody)
                        {
                            target = pingedBody;
                            targetName = target.gameObject.name;
                        }
                        else
                        {
                            Log.MessageNetworked(Lang.PINGEDBODY_NOTFOUND, args, LogLevel.MessageClientOnly);
                            return;
                        }
                    }
                    else
                    {
                        hasFoundPlayer = Util.GetBodyFromUser(args.userArgs, 1, out target, out targetName);
                    }
                }

                if (!hasFoundPlayer)
                {
                    Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                    return;
                }
            }

            Inventory inventory = target.inventory;
            if (inventory == null)
            {
                Log.MessageNetworked(Lang.INVENTORY_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }

            var a = args.TryGetArgInt(0);
            if (a.HasValue && a.Value > 0)
            {
                inventory.GiveRandomItems(a.Value, false, false);
                Log.MessageNetworked($"Generated {a.Value} items for {targetName}!", args);
            }
            else
            {
                Log.MessageNetworked(Lang.RANDOM_ITEM_ARGS, args, LogLevel.MessageClientOnly);
            }

        }

        [ConCommand(commandName = "give_equip", flags = ConVarFlags.ExecuteOnServer, helpText = "Gives the specified equipment to a character. " + Lang.GIVEEQUIP_ARGS)]
        [AutoCompletion(typeof(EquipmentCatalog), "equipmentDefs", "nameToken")]
        private static void CCGiveEquipment(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Log.MessageNetworked(Lang.GIVEEQUIP_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }

            bool isDedicatedServer = false;
            if (args.sender == null)
            {
                isDedicatedServer = true;
                if (args.Count < 2)
                {
                    Log.Message(Lang.DS_REQUIREFULLQUALIFY, LogLevel.Error);
                    Log.Message(Lang.GIVEEQUIP_ARGS, LogLevel.Message);
                    return;
                }
            }

            CharacterBody target = args.sender?.GetCurrentBody();
            string targetName = args.sender?.masterController.GetDisplayName();

            if (args.Count >= 2)
            {
                bool hasFoundPlayer = false;
                if (isDedicatedServer)
                {
                    hasFoundPlayer = Util.GetBodyFromUser(args.userArgs, 1, out target, out targetName);
                }
                else
                {
                    hasFoundPlayer = true;
                    if (args[1].ToUpper() == Lang.PINGED)
                    {
                        var pingedBody = Hooks.GetPingedBody();
                        if (pingedBody)
                        {
                            target = pingedBody;
                            targetName = target.gameObject.name;
                        }
                        else
                        {
                            Log.MessageNetworked(Lang.PINGEDBODY_NOTFOUND, args, LogLevel.MessageClientOnly);
                            return;
                        }
                    }
                    else
                    {
                        hasFoundPlayer = Util.GetBodyFromUser(args.userArgs, 1, out target, out targetName);
                    }
                }

                if (!hasFoundPlayer)
                {
                    Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                    return;
                }
            }

            Inventory inventory = target.inventory;
            if (inventory == null)
            {
                Log.MessageNetworked(Lang.INVENTORY_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }

            var equip = StringFinder.Instance.GetEquipFromPartial(args[0]);
            if (equip != EquipmentIndex.None)
            {
                inventory.SetEquipmentIndex(equip);
            }
            else if (args[0].ToLower() == "random")
            {
                inventory.GiveRandomEquipment();
                equip = inventory.GetEquipmentIndex();
            }
            else
            {
                Log.MessageNetworked(Lang.OBJECT_NOTFOUND + args[0] + ":" + equip, args, LogLevel.MessageClientOnly);
                return;
            }

            Log.MessageNetworked($"Gave {equip} to {targetName}", args);
        }

        [ConCommand(commandName = "give_lunar", flags = ConVarFlags.ExecuteOnServer, helpText = "Gives a lunar coin to you. " + Lang.GIVELUNAR_ARGS)]
        private static void CCGiveLunar(ConCommandArgs args)
        {
            if (args.sender == null)
            {
                Log.Message("Can't modify Lunar coins of other users directly.", LogLevel.Error);
                return;
            }
            int amount = 1;
            if (args.Count > 0)
            {
                amount = args.GetArgInt(0);
            }
            string str = "Nothing happened. Big suprise.";
            NetworkUser target = RoR2.Util.LookUpBodyNetworkUser(args.senderBody);
            if (amount > 0)
            {
                target.AwardLunarCoins((uint)amount);
                str = string.Format(Lang.GIVELUNAR_2, "Gave", amount);
            }
            if (amount < 0)
            {
                amount *= -1;
                target.DeductLunarCoins((uint)(amount));
                str = string.Format(Lang.GIVELUNAR_2, "Removed", amount);
            }
            Log.MessageNetworked(str, args);
        }

        [ConCommand(commandName = "create_pickup", flags = ConVarFlags.ExecuteOnServer, helpText = "Creates a PickupDroplet infront of your position. " + Lang.CREATEPICKUP_ARGS)]
        private static void CCCreatePickup(ConCommandArgs args)
        {
            args.CheckArgumentCount(1);
            if (args.sender == null)
            {
                if (args.Count <= 3)
                {
                    Log.Message(Lang.DS_REQUIREFULLQUALIFY, LogLevel.Error);
                    return;
                }
            }
            NetworkUser player = args.sender;
            if (args.Count >= 3)
            {
                player = Util.GetNetUserFromString(args.userArgs, 2);
                if (player == null)
                {
                    Log.Message(Lang.PLAYER_NOTFOUND, LogLevel.MessageClientOnly);
                    if (args.sender == null)
                    {
                        return;
                    }
                    player = args.sender;
                }
            }
            Transform transform = player.GetCurrentBody().gameObject.transform;

            bool searchEquip = true, searchItem = true;
            if (args.Count == 2)
            {
                if (args[1].Equals("item", StringComparison.OrdinalIgnoreCase))
                {
                    searchEquip = false;
                }
                if (args[1].ToUpper().StartsWith("EQUIP"))
                {
                    searchItem = false;
                }
            }
            PickupIndex final = PickupIndex.none;
            EquipmentIndex equipment = EquipmentIndex.None;
            ItemIndex item = ItemIndex.None;

            if (searchEquip)
            {
                equipment = StringFinder.Instance.GetEquipFromPartial(args[0]);
                final = PickupCatalog.FindPickupIndex(equipment);
            }
            if (searchItem)
            {
                item = StringFinder.Instance.GetItemFromPartial(args[0]);
                final = PickupCatalog.FindPickupIndex(item);
            }
            if (item != ItemIndex.None && equipment != EquipmentIndex.None)
            {
                Log.MessageNetworked(string.Format(Lang.CREATEPICKUP_AMBIGIOUS_2, item, equipment), args, LogLevel.MessageClientOnly);
                return;
            }

            if (item == ItemIndex.None && equipment == EquipmentIndex.None)
            {
                if (args[0].ToUpper().Contains("COIN"))
                {
                    final = PickupCatalog.FindPickupIndex("LunarCoin.Coin0");
                }
                else
                {
                    Log.MessageNetworked(Lang.CREATEPICKUP_NOTFOUND, args, LogLevel.MessageClientOnly);
                    return;
                }
            }
            Log.MessageNetworked(string.Format(Lang.CREATEPICKUP_SUCCES_1, final), args);
            PickupDropletController.CreatePickupDroplet(final, transform.position, transform.forward * 40f);
        }

        [ConCommand(commandName = "remove_item", flags = ConVarFlags.ExecuteOnServer, helpText = "Removes the specified quantities of an item from a character. " + Lang.REMOVEITEM_ARGS)]
        private static void CCRemoveItem(ConCommandArgs args)
        {
            NetworkUser player = args.sender;
            if (args.Count == 0)
            {
                Log.MessageNetworked(Lang.REMOVEITEM_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }
            bool isDedicatedServer = false;
            if (args.sender == null)
            {
                isDedicatedServer = true;
                if (args.Count < 3)
                {
                    Log.Message(Lang.DS_REQUIREFULLQUALIFY, LogLevel.Error);
                    return;
                }
            }

            CharacterBody target = args.sender?.GetCurrentBody();
            string targetName = args.sender?.masterController.GetDisplayName();

            int iCount = 1;
            bool secondArgIsCount = false;
            if (args.Count >= 2)
            {
                //TODO: Fix the count argument to always be the second one?
                // Being able to write `remove_item all <target>` is convenient.
                // However, if the target is the partial name "all", this will be considered a count argument
                // and without a target, it will default to the command caller. Is this too much of an edge case?
                secondArgIsCount = int.TryParse(args[1], out iCount) || args[1].ToUpper() == Lang.ALL;
                bool hasTargetArg = !secondArgIsCount || args.Count >= 3;
                int targetArgIndex = secondArgIsCount ? 2 : 1;
                bool hasFoundPlayer = false;

                if (isDedicatedServer)
                {
                    if (hasTargetArg)
                    {
                        hasFoundPlayer = Util.GetBodyFromUser(args.userArgs, targetArgIndex, out target, out targetName);
                    }
                }
                else
                {
                    hasFoundPlayer = true;
                    if (hasTargetArg)
                    {
                        if (args[targetArgIndex].ToUpper() == Lang.PINGED)
                        {
                            var pingedBody = Hooks.GetPingedBody();
                            if (pingedBody)
                            {
                                target = pingedBody;
                                targetName = target.gameObject.name;
                            }
                            else
                            {
                                Log.MessageNetworked(Lang.PINGEDBODY_NOTFOUND, args, LogLevel.MessageClientOnly);
                                return;
                            }
                        }
                        else
                        {
                            hasFoundPlayer = Util.GetBodyFromUser(args.userArgs, targetArgIndex, out target, out targetName);
                        }
                    }
                }

                if (!hasFoundPlayer)
                {
                    Log.Message(Lang.PLAYER_NOTFOUND, LogLevel.MessageClientOnly);
                    return;
                }
            }

            iCount = secondArgIsCount ? iCount : 1;
            Inventory inventory = target.inventory;
            if (inventory == null)
            {
                Log.MessageNetworked(Lang.INVENTORY_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }

            if (args[0].ToUpper() == Lang.ALL)
            {

                if (inventory)
                {
                    var tempObj = new GameObject();
                    inventory.CopyItemsFrom(tempObj.AddComponent<Inventory>());
                    UnityEngine.Object.Destroy(tempObj);
                    Log.MessageNetworked($"Reseting inventory for {targetName}", args);
                }

                return;
            }
            var item = StringFinder.Instance.GetItemFromPartial(args[0]);
            if (item != ItemIndex.None)
            {
                if (secondArgIsCount && args.Count >= 2 && args[1].ToUpper() == Lang.ALL)
                {
                    iCount = inventory.GetItemCount(item);
                }
                inventory.RemoveItem(item, iCount);
                Log.MessageNetworked($"Removed {iCount} {item} from {targetName}", args);
            }
            else
            {
                Log.MessageNetworked(Lang.OBJECT_NOTFOUND + args[0] + ":" + item, args, LogLevel.MessageClientOnly);
            }
        }

        [ConCommand(commandName = "remove_equip", flags = ConVarFlags.ExecuteOnServer, helpText = "Removes the equipment from a character. " + Lang.REMOVEEQUIP_ARGS)]
        private static void CCRemoveEquipment(ConCommandArgs args)
        {
            bool isDedicatedServer = false;
            if (args.sender == null)
            {
                isDedicatedServer = true;
                if (args.Count < 1)
                {
                    Log.Message(Lang.DS_REQUIREFULLQUALIFY, LogLevel.Error);
                    return;
                }
            }

            CharacterBody target = args.sender?.GetCurrentBody();
            string targetName = args.sender?.masterController.GetDisplayName();

            if (args.Count >= 1)
            {
                bool hasFoundPlayer = false;
                if (isDedicatedServer)
                {
                    hasFoundPlayer = Util.GetBodyFromUser(args.userArgs, 0, out target, out targetName);
                }
                else
                {
                    hasFoundPlayer = true;
                    if (args[0].ToUpper() == Lang.PINGED)
                    {
                        var pingedBody = Hooks.GetPingedBody();
                        if (pingedBody)
                        {
                            target = pingedBody;
                            targetName = target.gameObject.name;
                        }
                        else
                        {
                            Log.MessageNetworked(Lang.PINGEDBODY_NOTFOUND, args, LogLevel.MessageClientOnly);
                            return;
                        }
                    }
                    else
                    {
                        hasFoundPlayer = Util.GetBodyFromUser(args.userArgs, 0, out target, out targetName);
                    }
                }
                if (!hasFoundPlayer)
                {
                    Log.Message(Lang.PLAYER_NOTFOUND, LogLevel.MessageClientOnly);
                    return;
                }
            }

            Inventory inventory = target.inventory;
            if (inventory == null)
            {
                Log.MessageNetworked(Lang.INVENTORY_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }

            inventory.SetEquipmentIndex(EquipmentIndex.None);

            Log.MessageNetworked($"Removed current Equipment from {targetName}", args);
        }
    }
}
