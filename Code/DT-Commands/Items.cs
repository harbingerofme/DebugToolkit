using RoR2;
using RoR2.Artifacts;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static DebugToolkit.Log;

namespace DebugToolkit.Commands
{
    class Items
    {
        private static readonly Dictionary<ItemTier, List<PickupIndex>> availableDropLists = new Dictionary<ItemTier, List<PickupIndex>>();
        private static BasicPickupDropTable droptable;
        private static GameObject potentialPrefab;

        [ConCommand(commandName = "list_itemtier", flags = ConVarFlags.None, helpText = Lang.LISTITEMTIER_HELP)]
        private static void CCListItemTier(ConCommandArgs args)
        {
            var sb = new StringBuilder();
            var arg = args.Count > 0 ? args[0] : "";
            var indices = StringFinder.Instance.GetItemTiersFromPartial(arg);
            foreach (var index in indices)
            {
                sb.AppendLine($"[{(int)index}]{ItemTierCatalog.GetItemTierDef(index).name}");
            }
            var s = sb.Length > 0 ? sb.ToString().TrimEnd('\n') : string.Format(Lang.NOMATCH_ERROR, "item tiers", arg);
            Log.MessageNetworked(s, args, LogLevel.MessageClientOnly);
        }

        [ConCommand(commandName = "list_item", flags = ConVarFlags.None, helpText = Lang.LISTITEM_HELP)]
        private static void CCListItem(ConCommandArgs args)
        {
            var sb = new StringBuilder();
            var arg = args.Count > 0 ? args[0] : "";
            var indices = StringFinder.Instance.GetItemsFromPartial(arg);
            foreach (var index in indices)
            {
                var definition = ItemCatalog.GetItemDef(index);
                var realName = Language.currentLanguage.GetLocalizedStringByToken(definition.nameToken);
                bool enabled = Run.instance && Run.instance.IsItemAvailable(index);
                sb.AppendLine($"[{(int)index}]{definition.name} \"{realName}\" (enabled={enabled})");
            }
            var s = sb.Length > 0 ? sb.ToString().TrimEnd('\n') : string.Format(Lang.NOMATCH_ERROR, "items", arg);
            Log.MessageNetworked(s, args, LogLevel.MessageClientOnly);
        }

        [ConCommand(commandName = "list_equip", flags = ConVarFlags.None, helpText = Lang.LISTEQUIP_HELP)]
        private static void CCListEquip(ConCommandArgs args)
        {
            var sb = new StringBuilder();
            var arg = args.Count > 0 ? args[0] : "";
            var indices = StringFinder.Instance.GetEquipsFromPartial(arg);
            foreach (var index in indices)
            {
                var definition = EquipmentCatalog.GetEquipmentDef(index);
                var realName = Language.currentLanguage.GetLocalizedStringByToken(definition.nameToken);
                var enabled = Run.instance && Run.instance.IsEquipmentAvailable(index);
                sb.AppendLine($"[{(int)index}]{definition.name} \"{realName}\" (enabled={enabled})");
            }
            var s = sb.Length > 0 ? sb.ToString().TrimEnd('\n') : string.Format(Lang.NOMATCH_ERROR, "equipment", arg);
            Log.MessageNetworked(s, args, LogLevel.MessageClientOnly);
        }

        [ConCommand(commandName = "dump_inventories", flags = ConVarFlags.None, helpText = Lang.DUMPINVENTORIES_HELP)]
        private static void CCDumpInventories(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            var sb = new StringBuilder();
            foreach (var body in CharacterBody.readOnlyInstancesList)
            {
                var inventory = body.inventory;
                if (!inventory)
                {
                    continue;
                }
                sb.AppendLine($"--- {body.name} {body.corePosition}");
                foreach (var itemIndex in inventory.itemAcquisitionOrder)
                {
                    int count = inventory.GetItemCount(itemIndex);
                    if (count != 0)
                    {
                        var itemDef = ItemCatalog.GetItemDef(itemIndex);
                        var colorHexString = ColorCatalog.GetColorHexString(itemDef.colorIndex);
                        var name = itemDef.nameToken != "" ? Language.GetString(itemDef.nameToken) : itemDef.name;
                        sb.AppendLine($"<color=#{colorHexString}>{name}</color> {count}");
                    }
                }
                for (var slot = 0; slot < inventory.GetEquipmentSlotCount(); slot++)
                {
                    var equipmentDef = inventory.GetEquipment((uint)slot).equipmentDef;
                    var colorHexString = ColorCatalog.GetColorHexString(ColorCatalog.ColorIndex.Equipment);
                    var name = (equipmentDef != null) ? Language.GetString(equipmentDef.nameToken) : "<NONE>";
                    sb.AppendLine($"<color=#{colorHexString}>{name}</color>");
                }
                sb.AppendLine();
            }
            Log.MessageNetworked(sb.ToString().TrimEnd('\n'), args, LogLevel.MessageClientOnly);
        }

        [ConCommand(commandName = "give_item", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.GIVEITEM_HELP)]
        [ConCommand(commandName = "remove_item", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.REMOVEITEM_HELP)]
        [AutoCompletion(typeof(ItemCatalog), "itemDefs", "nameToken")]
        private static void CCGiveItem(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            bool isDedicatedServer = args.sender == null;
            if (args.Count == 0 || (isDedicatedServer && (args.Count < 3 || args[2] == Lang.DEFAULT_VALUE)))
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

            if (!TryParseTarget(args, 2, out var inventory, out var targetName))
            {
                return;
            }

            var item = StringFinder.Instance.GetItemFromPartial(args[0]);
            if (item == ItemIndex.None)
            {
                Log.MessageNetworked(Lang.OBJECT_NOTFOUND + args[0] + ":" + item, args, LogLevel.MessageClientOnly);
                return;
            }
            var amount = (args.commandName == "give_item" ? 1 : -1) * iCount;
            if (amount > 0)
            {
                if (Run.instance.IsItemExpansionLocked(item))
                {
                    Log.MessageNetworked("Additional content enabled is required to grant this item.", args, LogLevel.MessageClientOnly);
                    return;
                }
                inventory.GiveItem(item, amount);
                Log.MessageNetworked($"Gave {amount} {item} to {targetName}", args);
            }
            else if (amount < 0)
            {
                amount = Math.Min(-amount, inventory.GetItemCount(item));
                inventory.RemoveItem(item, amount);
                Log.MessageNetworked($"Removed {amount} {item} from {targetName}", args);
            }
            else
            {
                Log.MessageNetworked("Nothing happened", args);
            }
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
            if (args.Count == 0 || (isDedicatedServer && (args.Count < 3 || args[2] == Lang.DEFAULT_VALUE)))
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.RANDOMITEM_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }

            var droptable = ParseDroptable(args, 1, false);
            if (droptable == null)
            {
                return;
            }
            var weightedSelection = droptable.selector;
            if (weightedSelection.Count == 0)
            {
                Log.MessageNetworked("No items found to draw from.", args, LogLevel.MessageClientOnly);
                return;
            }

            if (!TryParseTarget(args, 2, out var inventory, out var targetName))
            {
                return;
            }

            if (!TextSerialization.TryParseInvariant(args[0], out int iCount))
            {
                Log.MessageNetworked(String.Format(Lang.PARSE_ERROR, "count", "int"), args, LogLevel.MessageClientOnly);
                return;
            }
            if (iCount > 0)
            {
                for (int i = 0; i < iCount; i++)
                {
                    var pickupIndex = weightedSelection.Evaluate(UnityEngine.Random.value);
                    var pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
                    inventory.GiveItem((pickupDef != null) ? pickupDef.itemIndex : ItemIndex.None, 1);
                }
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
            if (args.Count == 0 || (isDedicatedServer && (args.Count < 2 || args[1] == Lang.DEFAULT_VALUE)))
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.GIVEEQUIP_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }

            if (!TryParseTarget(args, 1, out var inventory, out var targetName))
            {
                return;
            }

            var equip = EquipmentIndex.None;
            if (args[0].ToUpperInvariant() == Lang.RANDOM)
            {
                inventory.GiveRandomEquipment();
                equip = inventory.GetEquipmentIndex();
            }
            else
            {
                equip = StringFinder.Instance.GetEquipFromPartial(args[0]);
                if (equip == EquipmentIndex.None)
                {
                    Log.MessageNetworked(Lang.OBJECT_NOTFOUND + args[0] + ":" + equip, args, LogLevel.MessageClientOnly);
                    return;
                }
                inventory.SetEquipmentIndex(equip);
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
        [AutoCompletion(typeof(EquipmentCatalog), "equipmentDefs", "nameToken")]
        [AutoCompletion(typeof(ItemCatalog), "itemDefs", "nameToken")]
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
                switch (args[1].ToUpperInvariant())
                {
                    case Lang.BOTH:
                        break;
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

            switch (args[0].ToUpperInvariant())
            {
                case Lang.COIN_LUNAR:
                    final = PickupCatalog.FindPickupIndex("LunarCoin.Coin0");
                    break;
                case Lang.COIN_VOID:
                    final = PickupCatalog.FindPickupIndex("MiscPickupIndex.VoidCoin");
                    break;
                default:
                    if (searchEquip)
                    {
                        equipment = StringFinder.Instance.GetEquipFromPartial(args[0]);
                    }
                    if (searchItem)
                    {
                        item = StringFinder.Instance.GetItemFromPartial(args[0]);
                    }
                    if (item == ItemIndex.None && equipment == EquipmentIndex.None)
                    {
                        Log.MessageNetworked(Lang.CREATEPICKUP_NOTFOUND, args, LogLevel.MessageClientOnly);
                        return;
                    }
                    else if (item != ItemIndex.None && equipment != EquipmentIndex.None)
                    {
                        Log.MessageNetworked(string.Format(Lang.CREATEPICKUP_AMBIGIOUS_2, item, equipment), args, LogLevel.MessageClientOnly);
                        return;
                    }
                    else if (equipment != EquipmentIndex.None)
                    {
                        final = PickupCatalog.FindPickupIndex(equipment);
                    }
                    else
                    {
                        final = PickupCatalog.FindPickupIndex(item);
                    }
                    break;
            }

            Log.MessageNetworked(string.Format(Lang.CREATEPICKUP_SUCCESS_1, final), args);
            PickupDropletController.CreatePickupDroplet(final, transform.position, transform.forward * 40f);
        }

        [ConCommand(commandName = "create_potential", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.CREATEPOTENTIAL_HELP)]
        [AutoCompletion(typeof(ItemTierCatalog), "itemTierDefs")]
        private static void CCCreatePotential(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            if (args.Count < 3 && args.sender == null)
            {
                Log.Message(Lang.INSUFFICIENT_ARGS + Lang.CREATEPOTENTIAL_ARGS, LogLevel.MessageClientOnly);
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

            var iCount = 3;
            if (args.Count > 1 && args[1] != Lang.DEFAULT_VALUE)
            {
                if (!TextSerialization.TryParseInvariant(args[1], out iCount))
                {
                    Log.MessageNetworked(String.Format(Lang.PARSE_ERROR, "count", "int"), args, LogLevel.MessageClientOnly);
                    return;
                }
            }
            if (iCount <= 0)
            {
                Log.MessageNetworked("'count' should be a non-zero positive integer.", args, LogLevel.MessageClientOnly);
                return;
            }

            var droptable = ParseDroptable(args, 0, true);
            if (droptable == null)
            {
                return;
            }
            var firstItemTier = ItemTier.Tier1;
            if (args.Count > 0 && args[0] != Lang.DEFAULT_VALUE && args[0].ToUpperInvariant() != Lang.ALL)
            {
                firstItemTier = StringFinder.Instance.GetItemTierFromPartial(args[0].Split(',')[0].Split(':')[0]);
            }

            PickupDropletController.CreatePickupDroplet(new GenericPickupController.CreatePickupInfo
            {
                pickerOptions = PickupPickerController.GenerateOptionsFromDropTable(iCount, droptable, RoR2Application.rng),
                prefabOverride = potentialPrefab,
                position = transform.position,
                rotation = Quaternion.identity,
                pickupIndex = PickupCatalog.FindPickupIndex(firstItemTier)
            }, transform.position, transform.forward * 40f);
            Log.MessageNetworked(string.Format(Lang.CREATEPICKUP_SUCCESS_2, Math.Min(iCount, droptable.selector.Count)), args);
        }

        [ConCommand(commandName = "remove_item_stacks", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.REMOVEITEMSTACKS_HELP)]
        [AutoCompletion(typeof(ItemCatalog), "itemDefs", "nameToken")]
        private static void CCRemoveItemStacks(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            bool isDedicatedServer = args.sender == false;
            if (args.Count == 0 || (isDedicatedServer && (args.Count < 2 || args[1] == Lang.DEFAULT_VALUE)))
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.REMOVEITEMSTACKS_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }

            if (!TryParseTarget(args, 1, out var inventory, out var targetName))
            {
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
            if (isDedicatedServer && (args.Count < 1 || args[0] == Lang.DEFAULT_VALUE))
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.REMOVEALLITEMS_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }

            if (!TryParseTarget(args, 0, out var inventory, out var targetName))
            {
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
            if (isDedicatedServer && (args.Count < 1 || args[0] == Lang.DEFAULT_VALUE))
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.REMOVEEQUIP_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }

            if (!TryParseTarget(args, 0, out var inventory, out var targetName))
            {
                return;
            }

            inventory.SetEquipmentIndex(EquipmentIndex.None);
            Log.MessageNetworked($"Removed current Equipment from {targetName}", args);
        }

        [ConCommand(commandName = "restock_equip", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.RESTOCKEQUIP_HELP)]
        private static void CCRestockEquip(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            bool isDedicatedServer = args.sender == null;
            if (isDedicatedServer && (args.Count < 2 || args[1] == Lang.DEFAULT_VALUE))
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.RESTOCKEQUIP_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }
            var iCount = 1;
            if (args.Count > 0 && !TextSerialization.TryParseInvariant(args[0], out iCount))
            {
                Log.MessageNetworked(String.Format(Lang.PARSE_ERROR, "count", "int"), args, LogLevel.MessageClientOnly);
                return;
            }
            if (iCount < 0)
            {
                Log.MessageNetworked(string.Format(Lang.NEGATIVE_ARG, "count"), args, LogLevel.MessageClientOnly);
                return;
            }
            if (!TryParseTarget(args, 1, out var inventory, out var targetName))
            {
                return;
            }

            var currentSlot = inventory.activeEquipmentSlot;
            var chargesBefore = inventory.GetEquipment(currentSlot).charges;
            inventory.RestockEquipmentCharges(currentSlot, iCount);
            var chargesAfter = inventory.GetEquipment(currentSlot).charges;
            Log.MessageNetworked($"Restocked {chargesAfter - chargesBefore} for the current equipment of {targetName}", args);
        }

        private static bool TryParseTarget(ConCommandArgs args, int index, out Inventory inventory, out string targetName)
        {
            inventory = null;
            targetName = null;
            if (args.Count > index && args[index] != Lang.DEFAULT_VALUE)
            {
                var targetArg = args[index].ToUpperInvariant();
                if (targetArg == Lang.EVOLUTION)
                {
                    inventory = MonsterTeamGainsItemsArtifactManager.monsterTeamInventory;
                    targetName = inventory?.gameObject.name;
                }
                else if (targetArg == Lang.SIMULACRUM)
                {
                    var run = Run.instance as InfiniteTowerRun;
                    if (!run)
                    {
                        Log.MessageNetworked(Lang.NOTINASIMULACRUMRUN_ERROR, args, LogLevel.MessageClientOnly);
                        return false;
                    }
                    inventory = run.enemyInventory;
                    targetName = inventory?.gameObject.name;
                }
                else if (targetArg == Lang.VOIDFIELDS)
                {
                    var mission = ArenaMissionController.instance;
                    if (!mission)
                    {
                        Log.MessageNetworked(Lang.NOTINVOIDFIELDS_ERROR, args, LogLevel.MessageClientOnly);
                        return false;
                    }
                    inventory = mission.inventory;
                    targetName = inventory?.gameObject.name;
                }
                else
                {
                    var isDedicatedServer = args.sender == null;
                    var target = Util.GetTargetFromArgs(args.userArgs, index, isDedicatedServer);
                    if (target == null && !isDedicatedServer && targetArg == Lang.PINGED)
                    {
                        Log.MessageNetworked(Lang.PINGEDBODY_NOTFOUND, args, LogLevel.MessageClientOnly);
                        return false;
                    }
                    if (target == null)
                    {
                        Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                        return false;
                    }
                    inventory = target.inventory;
                    var player = target.playerCharacterMasterController?.networkUser;
                    targetName = player?.masterController.GetDisplayName() ?? target.gameObject.name;
                }
            }
            else
            {
                var target = args.senderMaster;
                if (target == null)
                {
                    Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                    return false;
                }
                inventory = target.inventory;
                var player = target.playerCharacterMasterController?.networkUser;
                targetName = player?.masterController.GetDisplayName() ?? target.gameObject.name;
            }
            if (inventory == null)
            {
                Log.MessageNetworked(Lang.INVENTORY_ERROR, args, LogLevel.MessageClientOnly);
                return false;
            }
            return true;
        }

        private static BasicPickupDropTable ParseDroptable(ConCommandArgs args, int index, bool canDropBeReplaced)
        {
            droptable.selector.Clear();
            droptable.canDropBeReplaced = canDropBeReplaced;
            if (args.Count < index + 1 || args[index] == Lang.DEFAULT_VALUE || args[index].ToUpperInvariant() == Lang.ALL)
            {
                foreach (var itemTier in StringFinder.Instance.GetItemTiersFromPartial(""))
                {
                    droptable.Add(availableDropLists[itemTier], 1f);
                }
            }
            else
            {
                foreach (var tierData in args[index].Split(','))
                {
                    var data = tierData.Split(':');
                    var itemTier = StringFinder.Instance.GetItemTierFromPartial(data[0]);
                    if (itemTier == StringFinder.ItemTier_NotFound)
                    {
                        Log.MessageNetworked(Lang.OBJECT_NOTFOUND + data[0], args, LogLevel.MessageClientOnly);
                        return null;
                    }
                    float weight = 1f;
                    if (data.Length > 1 && !TextSerialization.TryParseInvariant(data[1], out weight))
                    {
                        Log.MessageNetworked(String.Format(Lang.PARSE_ERROR, "droptable weight", "float"), args, LogLevel.MessageClientOnly);
                        return null;
                    }
                    if (weight < 0f)
                    {
                        Log.MessageNetworked(string.Format(Lang.NEGATIVE_ARG, "droptable weight"), args, LogLevel.MessageClientOnly);
                        return null;
                    }
                    droptable.Add(availableDropLists[itemTier], weight);
                }
            }
            return droptable;
        }

        internal static void InitDroptableData()
        {
            droptable = ScriptableObject.CreateInstance<BasicPickupDropTable>();
            droptable.name = "dtDebugToolkit";
            potentialPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/OptionPickup/OptionPickup.prefab").WaitForCompletion();
        }

        internal static void CollectItemTiers(Run run)
        {
            availableDropLists.Clear();
            var customTiers = new Dictionary<ItemTier, List<PickupIndex>>();
            foreach (var itemTier in StringFinder.Instance.GetItemTiersFromPartial(""))
            {
                switch (itemTier)
                {
                    case ItemTier.Tier1:
                        availableDropLists[itemTier] = Run.instance.availableTier1DropList;
                        break;
                    case ItemTier.Tier2:
                        availableDropLists[itemTier] = Run.instance.availableTier2DropList;
                        break;
                    case ItemTier.Tier3:
                        availableDropLists[itemTier] = Run.instance.availableTier3DropList;
                        break;
                    case ItemTier.Lunar:
                        availableDropLists[itemTier] = Run.instance.availableLunarItemDropList;
                        break;
                    case ItemTier.Boss:
                        availableDropLists[itemTier] = Run.instance.availableBossDropList;
                        break;
                    case ItemTier.VoidTier1:
                        availableDropLists[itemTier] = Run.instance.availableVoidTier1DropList;
                        break;
                    case ItemTier.VoidTier2:
                        availableDropLists[itemTier] = Run.instance.availableVoidTier2DropList;
                        break;
                    case ItemTier.VoidTier3:
                        availableDropLists[itemTier] = Run.instance.availableVoidTier3DropList;
                        break;
                    case ItemTier.VoidBoss:
                        availableDropLists[itemTier] = Run.instance.availableVoidBossDropList;
                        break;
                    default:
                        customTiers[itemTier] = new List<PickupIndex>();
                        break;
                }
            }
            if (customTiers.Count > 0)
            {
                foreach (var itemIndex in ItemCatalog.allItems)
                {
                    var itemDef = ItemCatalog.GetItemDef(itemIndex);
                    if (run.availableItems.Contains(itemIndex) && itemDef.DoesNotContainTag(ItemTag.WorldUnique))
                    {
                        if (customTiers.TryGetValue(itemDef.tier, out var list))
                        {
                            list.Add(PickupCatalog.FindPickupIndex(itemIndex));
                        }
                    }
                }
                foreach (var tier in customTiers)
                {
                    availableDropLists[tier.Key] = tier.Value;
                }
            }
        }
    }
}
