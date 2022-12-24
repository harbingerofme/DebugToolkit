
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static DebugToolkit.Log;

namespace DebugToolkit.Commands
{
    class Items
    {
        [ConCommand(commandName = "list_item", flags = ConVarFlags.None, helpText = "List all items and their availability.")]
        private static void CCListItem(ConCommandArgs _)
        {
            var sb = new StringBuilder();
            foreach (var itemIndex in (IEnumerable<ItemIndex>)ItemCatalog.allItems)
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

        [ConCommand(commandName = "list_equip", flags = ConVarFlags.None, helpText = "List all items and their availability.")]
        private static void CCListEquip(ConCommandArgs _)
        {
            var sb = new StringBuilder();
            foreach (var equipmentIndex in (IEnumerable<EquipmentIndex>)EquipmentCatalog.allEquipment)
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


        [ConCommand(commandName = "give_item", flags = ConVarFlags.ExecuteOnServer, helpText = "Gives the specified item to the player in the specified quantity. " + Lang.GIVEITEM_ARGS)]
        [AutoCompletion(typeof(ItemCatalog), "itemDefs", "nameToken")]
        private static void CCGiveItem(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Log.MessageNetworked(Lang.GIVEITEM_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }

            if (args.sender == null)
            {
                if (args.Count < 2)
                {
                    Log.Message(Lang.DS_REQUIREFULLQUALIFY, LogLevel.Error);
                    Log.Message(Lang.GIVEITEM_ARGS, LogLevel.Message);
                    return;
                }
            }

            NetworkUser player = args.sender;
            Inventory inventory = args.sender?.master.inventory;

            int iCount = 1; // it'll get overwritten by the TryParse...
            bool secondArgIsCount = false;
            if (args.Count >= 2)
            {
                // secondArgIsCount allow to give directly the player name without specifying item count
                secondArgIsCount = int.TryParse(args[1], out iCount);

                var tmpPlayer = Util.GetNetUserFromString(args.userArgs, secondArgIsCount ? 2 : 1);
                if (tmpPlayer == null)
                {
                    if (args.sender == null)
                    {
                        Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                        return;
                    }
                }
                else
                {
                    player = tmpPlayer;
                }

                inventory = player.master.inventory;
            }

            iCount = secondArgIsCount ? iCount : 1;

            var item = StringFinder.Instance.GetItemFromPartial(args[0]);
            if (item != ItemIndex.None)
            {
                inventory?.GiveItem(item, iCount);
            }
            else
            {
                Log.MessageNetworked(Lang.OBJECT_NOTFOUND + args[0] + ":" + item, args, LogLevel.MessageClientOnly);
            }

            Log.MessageNetworked($"Gave {iCount} {item} to {player.masterController.GetDisplayName()}", args);
        }

        [ConCommand(commandName = "random_items", flags = ConVarFlags.ExecuteOnServer, helpText = "Generates the specified amount of items for the specified player from the available item pools at random.")]
        private static void CCRandom_items(ConCommandArgs args)
        {
            if (args.Count < 2 && args.sender == null)
            {
                Log.Message(Lang.DS_REQUIREFULLQUALIFY, LogLevel.Error);
                Log.Message(Lang.RANDOM_ITEM_ARGS, LogLevel.Message);
                return;
            }

            if (args.Count < 1)
            {
                Log.MessageNetworked(Lang.RANDOM_ITEM_ARGS, args);
                return;
            }

            NetworkUser player = args.sender;
            Inventory inventory = player?.master.inventory;

            if (args.Count >= 2)
            {
                player = Util.GetNetUserFromString(args.userArgs, 1);
                if (player == null)
                {
                    Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                    if (args.sender == null)
                    {
                        return;
                    }
                }

                inventory = player == null ? inventory : player.master.inventory;
            }

            var a = args.TryGetArgInt(0);
            if (a.HasValue && a.Value > 0)
            {
                inventory.GiveRandomItems(a.Value, false, false);
                Log.MessageNetworked($"Generated {a.Value} items!", args);
            }
            else
            {
                Log.MessageNetworked(Lang.RANDOM_ITEM_ARGS, args, LogLevel.MessageClientOnly);
            }


        }

        [ConCommand(commandName = "give_equip", flags = ConVarFlags.ExecuteOnServer, helpText = "Gives the specified item to the player. " + Lang.GIVEEQUIP_ARGS)]
        [AutoCompletion(typeof(EquipmentCatalog), "equipmentDefs", "nameToken")]
        private static void CCGiveEquipment(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                Log.MessageNetworked(Lang.GIVEEQUIP_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }

            if (args.sender == null)
            {
                if (args.Count < 2)
                {
                    Log.Message(Lang.DS_REQUIREFULLQUALIFY, LogLevel.Error);
                    Log.Message(Lang.GIVEEQUIP_ARGS, LogLevel.Message);
                    return;
                }
            }

            NetworkUser player = args.sender;
            Inventory inventory = args.sender?.master.inventory;

            if (args.Count >= 2)
            {
                player = Util.GetNetUserFromString(args.userArgs, 1);
                if (player == null)
                {
                    Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                    if (args.sender == null)
                    {
                        return;
                    }
                }

                inventory = player == null ? inventory : player.master.inventory;
            }

            var equip = StringFinder.Instance.GetEquipFromPartial(args[0]);
            if (equip != EquipmentIndex.None)
            {
                inventory?.SetEquipmentIndex(equip);
            }
            else if (args[0].ToLower() == "random")
            {
                inventory?.GiveRandomEquipment();
                equip = inventory.GetEquipmentIndex();
            }
            else
            {
                Log.MessageNetworked(Lang.OBJECT_NOTFOUND + args[0] + ":" + equip, args, LogLevel.MessageClientOnly);
            }

            Log.MessageNetworked($"Gave {equip} to {player.masterController.GetDisplayName()}", args);
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

        [ConCommand(commandName = "remove_item", flags = ConVarFlags.ExecuteOnServer, helpText = "Removes the specified quantities of an item from a player. " + Lang.REMOVEITEM_ARGS)]
        private static void CCRemoveItem(ConCommandArgs args)
        {
            NetworkUser player = args.sender;
            if (args.Count == 0)
            {
                Log.MessageNetworked(Lang.REMOVEITEM_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }
            if (args.sender == null)
            {
                if (args.Count < 3)
                {
                    Log.Message(Lang.DS_REQUIREFULLQUALIFY, LogLevel.Error);
                    return;
                }
            }
            int iCount = 1;
            if (args.Count >= 2)
            {
                int.TryParse(args[1], out iCount);
            }

            Inventory inventory = args.sender?.master.inventory;
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
                }

                inventory = (player == null) ? inventory : player.master.inventory;
            }

            if (args[0].ToUpper() == Lang.ALL)
            {

                if (inventory)
                {
                    var tempObj = new GameObject();
                    inventory.CopyItemsFrom(tempObj.AddComponent<Inventory>());
                    UnityEngine.Object.Destroy(tempObj);
                    Log.MessageNetworked("Removing inventory", args);
                }

                return;
            }
            var item = StringFinder.Instance.GetItemFromPartial(args[0]);
            if (item != ItemIndex.None)
            {
                if (args[1].ToUpper() == Lang.ALL)
                {
                    if (inventory != null)
                    {
                        iCount = inventory.GetItemCount(item);
                    }
                }

                if (inventory != null)
                {
                    inventory.RemoveItem(item, iCount);
                }
            }
            else
            {
                Log.MessageNetworked(Lang.OBJECT_NOTFOUND + args[0] + ":" + item, args, LogLevel.MessageClientOnly);
            }

            if (player)
            {
                Log.MessageNetworked($"Removed {iCount} {item} from {player.masterController.GetDisplayName()}", args);
            }
        }

        [ConCommand(commandName = "remove_equip", flags = ConVarFlags.ExecuteOnServer, helpText = "Removes the equipment from the specified player. " + Lang.REMOVEEQUIP_ARGS)]
        private static void CCRemoveEquipment(ConCommandArgs args)
        {
            NetworkUser player = args.sender;
            Inventory inventory = player.master.inventory;
            if (args.Count >= 1)
            {
                player = Util.GetNetUserFromString(args.userArgs);
                if (player == null)
                {
                    Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                    return;
                }

                inventory = player.master.inventory;
            }
            inventory.SetEquipmentIndex(EquipmentIndex.None);

            Log.MessageNetworked($"Removed current Equipment from {player.masterController.GetDisplayName()}", args);
        }
    }
}
