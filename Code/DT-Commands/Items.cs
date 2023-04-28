
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
        [ConCommand(commandName = "list_item", flags = ConVarFlags.None, helpText = Lang.LISTITEM_HELP)]
        private static void CCListItem(ConCommandArgs args)
        {
            var sb = new StringBuilder();
            IEnumerable<ItemIndex> itemList;
            if (args.Count > 0)
            {
                itemList = (IEnumerable<ItemIndex>)StringFinder.Instance.GetItemsFromPartial(args.GetArgString(0));
                if (itemList.Count() == 0) sb.AppendLine($"No item that matches \"{args[0]}\".");
            } else
            {
                itemList = (IEnumerable<ItemIndex>)ItemCatalog.allItems;
            }
            foreach (var itemIndex in itemList)
            {
                var definition = ItemCatalog.GetItemDef(itemIndex);
                bool enabled = false;
                var realName = Language.currentLanguage.GetLocalizedStringByToken(definition.nameToken);
                if (Run.instance)
                {
                    enabled = Run.instance.IsItemAvailable(itemIndex);
                }
                sb.AppendLine($"[{(int)itemIndex}]: {definition.name} \"{realName}\" (enabled={enabled})");
            }
            Log.MessageNetworked(sb.ToString(), args, LogLevel.MessageClientOnly);
        }

        [ConCommand(commandName = "list_equip", flags = ConVarFlags.None, helpText = Lang.LISTEQUIP_HELP)]
        private static void CCListEquip(ConCommandArgs args)
        {
            var sb = new StringBuilder();
            IEnumerable<EquipmentIndex> list;
            if (args.Count > 0)
            {
                list = StringFinder.Instance.GetEquipsFromPartial(args[0]);
                if (list.Count() == 0) sb.AppendLine($"No equipment that matches \"{args[0]}\".");
            }
            else
            {
                list = EquipmentCatalog.allEquipment;
            }

            foreach (var equipmentIndex in list)
            {
                var definition = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
                bool enabled = false;
                var realName = Language.currentLanguage.GetLocalizedStringByToken(definition.nameToken);
                if (Run.instance)
                {
                    enabled = Run.instance.IsEquipmentAvailable(equipmentIndex);
                }
                sb.AppendLine($"[{(int)equipmentIndex}]: {definition.name} \"{realName}\" (enabled={enabled})");
            }
            Log.MessageNetworked(sb.ToString(), args, LogLevel.MessageClientOnly);
        }

        [ConCommand(commandName = "give_item", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.GIVEITEM_HELP)]
        [AutoCompletion(typeof(ItemCatalog), "itemDefs", "nameToken")]
        private static void CCGiveItem(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            bool isDedicatedServer = args.sender == null;
            if (args.Count == 0 || (args.Count < 3 && isDedicatedServer))
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.GIVEITEM_ARGS, args, LogLevel.MessageClientOnly);
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

            Inventory inventory = target.inventory;
            if (inventory == null)
            {
                Log.MessageNetworked(Lang.INVENTORY_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }

            var item = StringFinder.Instance.GetItemFromPartial(args[0]);
            if (item == ItemIndex.None)
            {
                Log.MessageNetworked(Lang.OBJECT_NOTFOUND + args[0] + ":" + item, args, LogLevel.MessageClientOnly);
                return;
            }
            inventory.GiveItem(item, iCount);
            Log.MessageNetworked($"Gave {iCount} {item} to {targetName}", args);
        }

        [ConCommand(commandName = "random_items", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.RANDOMITEM_HELP)]
        private static void CCRandomItems(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            bool isDedicatedServer = args.sender == null;
            if (args.Count == 0 || (args.Count < 3 && isDedicatedServer))
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.RANDOMITEM_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }

            bool lunarEnabled = false;
            bool voidEnabled = false;
            if (args.Count > 1 && args[1] != Lang.DEFAULT_VALUE)
            {
                switch (args[1].ToLower())
                {
                    case "lunar":
                        lunarEnabled = true;
                        break;
                    case "void":
                        voidEnabled = true;
                        break;
                    case "both":
                        lunarEnabled= true;
                        voidEnabled = true;
                        break;
                    default:
                        Log.MessageNetworked(String.Format(Lang.INVALID_ARG_VALUE, "include"), args, LogLevel.MessageClientOnly);
                        return;
                }
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

            Inventory inventory = target.inventory;
            if (inventory == null)
            {
                Log.MessageNetworked(Lang.INVENTORY_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }

            if (!TextSerialization.TryParseInvariant(args[0], out int iCount))
            {
                Log.MessageNetworked(String.Format(Lang.PARSE_ERROR, "count", "int"), args, LogLevel.MessageClientOnly);
                return;
            }
            if (iCount > 0)
            {
                inventory.GiveRandomItems(iCount, lunarEnabled, voidEnabled);
                Log.MessageNetworked($"Generated {iCount} items for {targetName}!", args);
            }
            else
            {
                Log.MessageNetworked("'count' should be a non-zero positive integer.", args, LogLevel.MessageClientOnly);
            }
        }

        [ConCommand(commandName = "give_equip", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.GIVEEQUIP_HELP)]
        [AutoCompletion(typeof(EquipmentCatalog), "equipmentDefs", "nameToken")]
        private static void CCGiveEquipment(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            bool isDedicatedServer = args.sender == null;
            if (args.Count == 0 || (args.Count < 2 && isDedicatedServer))
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.GIVEEQUIP_ARGS, args, LogLevel.MessageClientOnly);
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
            else if (args[0].ToUpper() == Lang.RANDOM)
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

        [ConCommand(commandName = "give_lunar", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.GIVELUNAR_HELP)]
        private static void CCGiveLunar(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            if (args.sender == null)
            {
                Log.Message("Can't modify Lunar coins of other users directly.", LogLevel.MessageClientOnly);
                return;
            }
            int amount = 1;
            if (args.Count > 0 && args[0] != Lang.DEFAULT_VALUE && !TextSerialization.TryParseInvariant(args[0], out amount))
            {
                Log.MessageNetworked(String.Format(Lang.PARSE_ERROR, "amount", "int"), args, LogLevel.MessageClientOnly);
                return;
            }
            string str = "Nothing happened. Big surprise.";
            NetworkUser target = args.sender;
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

        [ConCommand(commandName = "create_pickup", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.CREATEPICKUP_HELP)]
        private static void CCCreatePickup(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            if (args.Count == 0 || (args.Count < 3 && args.sender == null))
            {
                Log.Message(Lang.INSUFFICIENT_ARGS + Lang.CREATEPICKUP_ARGS, LogLevel.MessageClientOnly);
                return;
            }
            NetworkUser player = args.sender;
            if (args.Count > 2)
            {
                player = Util.GetNetUserFromString(args.userArgs, 2);
                if (player == null)
                {
                    Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                    return;
                }
            }
            Transform transform = player.GetCurrentBody()?.gameObject.transform;
            if (transform == null)
            {
                // We could possibly use `player.master.deathFootPosition` instead
                Log.MessageNetworked("Can't spawn an object with relation to a dead player.", args, LogLevel.MessageClientOnly);
                return;
            }

            bool searchEquip = true, searchItem = true;
            if (args.Count > 1 && args[1] != Lang.DEFAULT_VALUE)
            {
                switch (args[1].ToUpper())
                {
                    case Lang.ITEM:
                        searchEquip = false;
                        break;
                    case Lang.EQUIP:
                        searchItem = false;
                        break;
                    default:
                        Log.MessageNetworked(String.Format(Lang.INVALID_ARG_VALUE, "search"), args, LogLevel.MessageClientOnly);
                        return;
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
                if (args[0].ToUpper() == Lang.COIN)
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

        [ConCommand(commandName = "remove_item", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.REMOVEITEM_HELP)]
        private static void CCRemoveItem(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            bool isDedicatedServer = args.sender == null;
            if (args.Count == 0 || (args.Count < 3 && isDedicatedServer))
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.REMOVEITEM_ARGS, args, LogLevel.MessageClientOnly);
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

            Inventory inventory = target.inventory;
            if (inventory == null)
            {
                Log.MessageNetworked(Lang.INVENTORY_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }

            var item = StringFinder.Instance.GetItemFromPartial(args[0]);
            if (item == ItemIndex.None)
            {
                Log.MessageNetworked(Lang.OBJECT_NOTFOUND + args[0] + ":" + item, args, LogLevel.MessageClientOnly);
                return;
            }
            inventory.RemoveItem(item, iCount);
            Log.MessageNetworked($"Removed {iCount} {item} from {targetName}", args);
        }

        [ConCommand(commandName = "remove_item_stacks", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.REMOVEITEMSTACKS_HELP)]
        private static void CCRemoveItemStacks(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            bool isDedicatedServer = false;
            if (args.Count == 0 || (args.Count < 2 && isDedicatedServer))
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.REMOVEITEMSTACKS_ARGS, args, LogLevel.MessageClientOnly);
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

            Inventory inventory = target.inventory;
            if (inventory == null)
            {
                Log.MessageNetworked(Lang.INVENTORY_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }

            var item = StringFinder.Instance.GetItemFromPartial(args[0]);
            if (item == ItemIndex.None)
            {
                Log.MessageNetworked(Lang.OBJECT_NOTFOUND + args[0] + ":" + item, args, LogLevel.MessageClientOnly);
                return;
            }
            int iCount = inventory.GetItemCount(item);
            inventory.RemoveItem(item, iCount);
            Log.MessageNetworked($"Removed {iCount} {item} from {targetName}", args);
        }

        [ConCommand(commandName = "remove_all_items", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.REMOVEALLITEMS_HELP)]
        private static void CCRemoveAllItems(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            bool isDedicatedServer = args.sender == null;
            if (args.Count < 1 && isDedicatedServer)
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.REMOVEALLITEMS_ARGS, args,LogLevel.MessageClientOnly);
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

            Inventory inventory = target.inventory;
            if (inventory == null)
            {
                Log.MessageNetworked(Lang.INVENTORY_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }

            var tempObj = new GameObject();
            inventory.CopyItemsFrom(tempObj.AddComponent<Inventory>());
            UnityEngine.Object.Destroy(tempObj);
            Log.MessageNetworked($"Reseting inventory for {targetName}", args);
        }

        [ConCommand(commandName = "remove_equip", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.REMOVEEQUIP_HELP)]
        private static void CCRemoveEquipment(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            bool isDedicatedServer = args.sender == null;
            if (args.Count < 1 && isDedicatedServer)
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.REMOVEEQUIP_ARGS, args, LogLevel.MessageClientOnly);
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
