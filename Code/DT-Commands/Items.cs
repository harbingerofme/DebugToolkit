
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
                if (args.Count < 3)
                {
                    Log.Message(Lang.DS_REQUIREFULLQUALIFY, LogLevel.Error);
                    Log.Message(Lang.GIVEITEM_ARGS, LogLevel.Message);
                    return;
                }
            }

            int iCount = 1; // it'll get overwritten by the TryParse...
            if (args.Count >= 2 && args[1] != Lang.DEFAULT_VALUE)
            {
                iCount = int.TryParse(args[1], out iCount) ? iCount : 1;
            }

            CharacterBody target = args.senderBody;
            if (args.Count >= 3 && args[2] != Lang.DEFAULT_VALUE)
            {
                target = Util.GetBodyFromArgs(args.userArgs, 2, isDedicatedServer);
            }
            if (target == null)
            {
                if (!isDedicatedServer && args[2].ToUpper() == Lang.PINGED)
                {
                    Log.MessageNetworked(Lang.PINGEDBODY_NOTFOUND, args, LogLevel.MessageClientOnly);
                }
                else
                {
                    Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                }
                return;
            }
            NetworkUser player = RoR2.Util.LookUpBodyNetworkUser(target);
            string targetName = (player != null) ? player.masterController.GetDisplayName() : target.gameObject.name;

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

            CharacterBody target = args.senderBody;
            if (args.Count >= 2 && args[1] != Lang.DEFAULT_VALUE)
            {
                target = Util.GetBodyFromArgs(args.userArgs, 1, isDedicatedServer);
            }
            if (target == null)
            {
                if (!isDedicatedServer && args[1] == Lang.PINGED.ToUpper())
                {
                    Log.MessageNetworked(Lang.PINGEDBODY_NOTFOUND, args, LogLevel.MessageClientOnly);
                }
                else
                {
                    Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                }
                return;
            }
            NetworkUser player = RoR2.Util.LookUpBodyNetworkUser(target);
            string targetName = (player != null) ? player.masterController.GetDisplayName() : target.gameObject.name;

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

            CharacterBody target = args.senderBody;
            if (args.Count >= 2 && args[1] != Lang.DEFAULT_VALUE)
            {
                target = Util.GetBodyFromArgs(args.userArgs, 1, isDedicatedServer);
            }
            if (target == null)
            {
                if (!isDedicatedServer && args[1].ToUpper() == Lang.PINGED)
                {
                    Log.MessageNetworked(Lang.PINGEDBODY_NOTFOUND, args, LogLevel.MessageClientOnly);
                }
                else
                {
                    Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                }
                return;
            }
            NetworkUser player = RoR2.Util.LookUpBodyNetworkUser(target);
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

            int iCount = 1;
            if (args.Count >= 2)
            {
                iCount = int.TryParse(args[1], out iCount) ? iCount : 1;
            }

            CharacterBody target = args.senderBody;
            if (args.Count >= 3 && args[2] != Lang.DEFAULT_VALUE)
            {
                target = Util.GetBodyFromArgs(args.userArgs, 2, isDedicatedServer);
            }
            if (target == null)
            {
                if (!isDedicatedServer && args[2].ToUpper() == Lang.PINGED)
                {
                    Log.MessageNetworked(Lang.PINGEDBODY_NOTFOUND, args, LogLevel.MessageClientOnly);
                }
                else
                {
                    Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                }
                return;
            }
            NetworkUser player = RoR2.Util.LookUpBodyNetworkUser(target);
            string targetName = (player != null) ? player.masterController.GetDisplayName() : target.gameObject.name;

            Inventory inventory = target.inventory;
            if (inventory == null)
            {
                Log.MessageNetworked(Lang.INVENTORY_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }

            var item = StringFinder.Instance.GetItemFromPartial(args[0]);
            if (item != ItemIndex.None)
            {
                inventory.RemoveItem(item, iCount);
                Log.MessageNetworked($"Removed {iCount} {item} from {targetName}", args);
            }
            else
            {
                Log.MessageNetworked(Lang.OBJECT_NOTFOUND + args[0] + ":" + item, args, LogLevel.MessageClientOnly);
            }
        }

        [ConCommand(commandName = "remove_item_stacks", flags = ConVarFlags.ExecuteOnServer, helpText = "Removes all the stacks of a specified item from a character. " + Lang.REMOVEITEMSTACKS_ARGS)]
        private static void CCRemoveItemStacks(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Log.MessageNetworked(Lang.REMOVEITEMSTACKS_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }

            bool isDedicatedServer = false;
            if (args.sender == null)
            {
                isDedicatedServer = true;
                if (args.Count < 2)
                {
                    Log.Message(Lang.DS_REQUIREFULLQUALIFY, LogLevel.Error);
                    return;
                }
            }

            CharacterBody target = args.senderBody;
            if (args.Count >= 2 && args[1] != Lang.DEFAULT_VALUE)
            {
                target = Util.GetBodyFromArgs(args.userArgs, 1, isDedicatedServer);
            }
            if (target == null)
            {
                if (!isDedicatedServer && args[1].ToUpper() == Lang.PINGED)
                {
                    Log.MessageNetworked(Lang.PINGEDBODY_NOTFOUND, args, LogLevel.MessageClientOnly);
                }
                else
                {
                    Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                }
                return;
            }
            NetworkUser player = RoR2.Util.LookUpBodyNetworkUser(target);
            string targetName = (player != null) ? player.masterController.GetDisplayName() : target.gameObject.name;

            Inventory inventory = target.inventory;
            if (inventory == null)
            {
                Log.MessageNetworked(Lang.INVENTORY_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }

            var item = StringFinder.Instance.GetItemFromPartial(args[0]);
            if (item != ItemIndex.None)
            {
                int iCount = inventory.GetItemCount(item);
                inventory.RemoveItem(item, iCount);
                Log.MessageNetworked($"Removed {iCount} {item} from {targetName}", args);
            }
            else
            {
                Log.MessageNetworked(Lang.OBJECT_NOTFOUND + args[0] + ":" + item, args, LogLevel.MessageClientOnly);
            }
        }

        [ConCommand(commandName = "remove_all_items", flags = ConVarFlags.ExecuteOnServer, helpText = "Removes all items from a character. " + Lang.REMOVEALLITEMS_ARGS)]
        private static void CCRemoveAllItems(ConCommandArgs args)
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

            CharacterBody target = args.senderBody;
            if (args.Count >= 1 && args[0] != Lang.DEFAULT_VALUE)
            {
                target = Util.GetBodyFromArgs(args.userArgs, 0, isDedicatedServer);
            }
            if (target == null)
            {
                if (!isDedicatedServer && args[0].ToUpper() == Lang.PINGED)
                {
                    Log.MessageNetworked(Lang.PINGEDBODY_NOTFOUND, args, LogLevel.MessageClientOnly);
                }
                else
                {
                    Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                }
                return;
            }
            NetworkUser player = RoR2.Util.LookUpBodyNetworkUser(target);
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

            CharacterBody target = args.senderBody;
            if (args.Count >= 1 && args[0] != Lang.DEFAULT_VALUE)
            {
                target = Util.GetBodyFromArgs(args.userArgs, 0, isDedicatedServer);
            }
            if (target == null)
            {
                if (!isDedicatedServer && args[0].ToUpper() == Lang.PINGED)
                {
                    Log.MessageNetworked(Lang.PINGEDBODY_NOTFOUND, args, LogLevel.MessageClientOnly);
                }
                else
                {
                    Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                }
                return;
            }
            NetworkUser player = RoR2.Util.LookUpBodyNetworkUser(target);
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
