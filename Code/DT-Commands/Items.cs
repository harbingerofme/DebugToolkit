using RoR2;
using RoR2.Artifacts;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static DebugToolkit.Log;

namespace DebugToolkit.Commands
{
    class Items
    {
        private static readonly Dictionary<ItemTier, List<PickupIndex>> availableDropLists = new Dictionary<ItemTier, List<PickupIndex>>();
        private static BasicPickupDropTable droptable;
        private static GameObject potentialPrefab;

        [ConCommand(commandName = "list_itemtier", flags = ConVarFlags.None, helpText = Lang.LISTITEMTIER_HELP)]
        [AutoComplete(Lang.LISTQUERY_ARGS)]
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
        [AutoComplete(Lang.LISTQUERY_ARGS)]
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
        [AutoComplete(Lang.LISTQUERY_ARGS)]
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
            if (!ArgumentParser.AssertInARun(args))
            {
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
                    int count = inventory.GetItemCountEffective(itemIndex);
                    if (count != 0)
                    {
                        var itemDef = ItemCatalog.GetItemDef(itemIndex);
#pragma warning disable CS0618 // Type or member is obsolete
                        var colorHexString = ColorCatalog.GetColorHexString(itemDef.colorIndex);
#pragma warning restore CS0618 // Type or member is obsolete
                        var name = itemDef.nameToken != "" ? Language.GetString(itemDef.nameToken) : itemDef.name;
                        sb.AppendLine($"<color=#{colorHexString}>{name}</color> {count}");
                    }
                }
                for (uint slot = 0; slot < inventory.GetEquipmentSlotCount(); slot++)
                {
                    for (uint set = 0; set < inventory.GetEquipmentSetCount(slot); set++)
                    {
                        var equipmentDef = inventory.GetEquipment(slot, set).equipmentDef;
                        var colorHexString = ColorCatalog.GetColorHexString(ColorCatalog.ColorIndex.Equipment);
                        var name = (equipmentDef != null) ? Language.GetString(equipmentDef.nameToken) : "<NONE>";
                        sb.AppendLine($"<color=#{colorHexString}>{name}</color>");
                    }
                }
                sb.AppendLine();
            }
            Log.MessageNetworked(sb.ToString().TrimEnd('\n'), args, LogLevel.MessageClientOnly);
        }

        [ConCommand(commandName = "give_item", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.GIVEITEM_HELP)]
        [ConCommand(commandName = "remove_item", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.REMOVEITEM_HELP)]
        [AutoComplete(Lang.GIVEITEM_ARGS)]
        private static void CCGiveItem(ConCommandArgs args)
        {
            if (!ArgumentParser.AssertInARun(args) ||
                !ArgumentParser.AssertRequiredArguments(args, Lang.GIVEITEM_ARGS, 1, 4) ||
                !ArgumentParser.TryParseItem(args, 0, out var itemDef) ||
                !ArgumentParser.TryParseOptionalInt(args, 1, "count", 1, out var count) ||
                !TryParseItemType(args, 2, out var itemType) ||
                !TryParseInventoryTarget(args, 3, out var target))
            {
                return;
            }

            var name = itemDef.name;
            if (itemType != ItemType.Permanent)
            {
                name = $"{itemType} {name}";
            }
            var amount = (args.commandName == "give_item" ? 1 : -1) * count;
            var inventory = target.inventory;
            if (amount > 0)
            {
                GiveItem(inventory, itemDef.itemIndex, amount, itemType);
                Log.MessageNetworked(string.Format(Lang.GIVEOBJECT_WITH_STACKS, amount, name, target.name), args);
            }
            else if (amount < 0)
            {
                amount = Math.Min(-amount, GetItemCount(inventory, itemDef.itemIndex, itemType));
                RemoveItem(inventory, itemDef.itemIndex, amount, itemType);
                Log.MessageNetworked(string.Format(Lang.REMOVEOBJECT_WITH_STACKS, amount, name, target.name), args);
            }
            else
            {
                Log.MessageNetworked("Nothing happened", args);
            }
            if (target.devotionController)
            {
                target.devotionController.UpdateAllMinions(false);
            }
        }

        [ConCommand(commandName = "random_items", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.RANDOMITEM_HELP)]
        [AutoComplete(Lang.RANDOMITEM_ARGS)]
        private static void CCRandomItems(ConCommandArgs args)
        {
            if (!ArgumentParser.AssertInARun(args) ||
                !ArgumentParser.AssertRequiredArguments(args, Lang.RANDOMITEM_ARGS, 1, 4) ||
                // Not optional technically
                !ArgumentParser.TryParseOptionalInt(args, 0, "count", default, out var count, min: 1) ||
                !TryParseDroptable(args, 1, false) ||
                !TryParseItemType(args, 2, out var itemType) ||
                !TryParseInventoryTarget(args, 3, out var target))
            {
                return;
            }

            var weightedSelection = droptable.selector;
            if (weightedSelection.Count == 0)
            {
                Log.MessageNetworked("No items found to draw from.", args, LogLevel.MessageClientOnly);
                return;
            }

            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    var uniquePickup = weightedSelection.Evaluate(UnityEngine.Random.value);
                    var pickupDef = PickupCatalog.GetPickupDef(uniquePickup.pickupIndex);
                    var item = pickupDef?.itemIndex ?? ItemIndex.None;
                    GiveItem(target.inventory, item, 1, itemType);
                }
                if (target.devotionController)
                {
                    target.devotionController.UpdateAllMinions(false);
                }
                if (itemType == ItemType.Permanent)
                {
                    Log.MessageNetworked($"Generated {count} items for {target.name}!", args);
                }
                else
                {
                    Log.MessageNetworked($"Generated {count} {itemType} items for {target.name}!", args);
                }
            }
        }

        [ConCommand(commandName = "give_equip", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.GIVEEQUIP_HELP)]
        [AutoComplete(Lang.GIVEEQUIP_ARGS)]
        private static void CCGiveEquipment(ConCommandArgs args)
        {
            if (!ArgumentParser.AssertInARun(args) ||
                !ArgumentParser.AssertRequiredArguments(args, Lang.GIVEEQUIP_ARGS, 1, 2) ||
                !ArgumentParser.TryParseEquipmentOrRandom(args, 0, out var equipmentDef) ||
                !TryParseInventoryTarget(args, 1, out var target))
            {
                return;
            }
            target.inventory.SetEquipmentIndex(equipmentDef.equipmentIndex, false);
            Log.MessageNetworked(string.Format(Lang.GIVEOBJECT, equipmentDef.name, target.name), args);
        }

        [ConCommand(commandName = "give_equip_extra", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.GIVEEQUIPEXTRA_HELP)]
        [AutoComplete(Lang.GIVEEQUIPEXTRA_ARGS)]
        private static void CCGiveEquipmentExtra(ConCommandArgs args)
        {
            if (!ArgumentParser.AssertInARun(args) ||
                !ArgumentParser.AssertRequiredArguments(args, Lang.GIVEEQUIPEXTRA_ARGS, 3, 4) ||
                !ArgumentParser.TryParseEquipmentOrRandom(args, 0, out var equipmentDef) ||
                // Slot and set not optional technically
                !ArgumentParser.TryParseOptionalUInt(args, 1, "slot", default, out var slot) ||
                !ArgumentParser.TryParseOptionalUInt(args, 2, "set", default, out var set) ||
                !TryParseInventoryTarget(args, 3, out var target))
            {
                return;
            }
            var inventory = target.inventory;
            // We need to call this first to properly resize sets due to the ExtraEquipment item,
            // or else the command may allocate extra sets beyond what the item accounts for.
            // This is only an issue if we're combining give_item and give_equip_extra in the console,
            // where Inventory.FixedUpdate doesn't get a chance to run in the between.
            inventory.UpdateEquipmentSetCount();
            inventory.SetEquipmentIndexForSlot(equipmentDef.equipmentIndex, slot, set);
            Log.MessageNetworked($"Gave {equipmentDef.name} to {target.name} in position ({slot}, {set})", args);
        }

        [ConCommand(commandName = "create_pickup", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.CREATEPICKUP_HELP)]
        [AutoComplete(Lang.CREATEPICKUP_ARGS)]
        private static void CCCreatePickup(ConCommandArgs args)
        {
            if (!ArgumentParser.AssertInARun(args) ||
                !ArgumentParser.AssertRequiredArguments(args, Lang.CREATEPICKUP_ARGS, 1, 4) ||
                // The object and search arguments are parsed later due to complex logic.
                !TryParseItemType(args, 1, out var itemType) ||
                !ArgumentParser.TryParsePlayerOrDefault(args, 3, out var master, requireLiving: true))
            {
                return;
            }

            bool searchEquip = true, searchItem = true;
            if (args.Count > 2 && args[2] != Lang.DEFAULT_VALUE)
            {
                switch (args[2].ToUpperInvariant())
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

            var body = master.GetBody();
            PickupDropletController.CreatePickupDroplet(new GenericPickupController.CreatePickupInfo
            {
                pickup = new UniquePickup
                {
                    pickupIndex = final,
                    decayValue = itemType == ItemType.Temp ? 1f : 0f,
                },
            }, body.transform.position, body.inputBank.aimDirection * 30f);
            Log.MessageNetworked(string.Format(Lang.CREATEPICKUP_SUCCESS_1, final), args);
        }

        [ConCommand(commandName = "create_potential", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.CREATEPOTENTIAL_HELP)]
        [AutoComplete(Lang.CREATEPOTENTIAL_ARGS)]
        private static void CCCreatePotential(ConCommandArgs args)
        {
            if (!ArgumentParser.AssertInARun(args) ||
                !ArgumentParser.AssertRequiredArguments(args, Lang.CREATEPOTENTIAL_ARGS, 0, 3) ||
                !TryParseDroptable(args, 0, true) ||
                !ArgumentParser.TryParseOptionalInt(args, 1, "count", 3, out var count, min: 1) ||
                !ArgumentParser.TryParsePlayerOrDefault(args, 2, out var master, requireLiving: true))
            {
                return;
            }

            var firstItemTier = ItemTier.Tier1;
            if (args.Count > 0 && args[0] != Lang.DEFAULT_VALUE && args[0].ToUpperInvariant() != Lang.ALL)
            {
                firstItemTier = StringFinder.Instance.GetItemTierFromPartial(args[0].Split(',')[0].Split(':')[0]);
            }

            var body = master.GetBody();
            PickupDropletController.CreatePickupDroplet(new GenericPickupController.CreatePickupInfo
            {
                pickerOptions = PickupPickerController.GenerateOptionsFromDropTable(count, droptable, RoR2Application.rng),
                prefabOverride = potentialPrefab,
                position = body.transform.position,
                rotation = Quaternion.identity,
                pickup = new UniquePickup(PickupCatalog.FindPickupIndex(firstItemTier))
            }, body.transform.position, body.inputBank.aimDirection * 30f);
            Log.MessageNetworked(string.Format(Lang.CREATEPICKUP_SUCCESS_2, Math.Min(count, droptable.selector.Count)), args);
        }

        [ConCommand(commandName = "remove_item_stacks", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.REMOVEITEMSTACKS_HELP)]
        [AutoComplete(Lang.REMOVEITEMSTACKS_ARGS)]
        private static void CCRemoveItemStacks(ConCommandArgs args)
        {
            if (!ArgumentParser.AssertInARun(args) ||
                !ArgumentParser.AssertRequiredArguments(args, Lang.REMOVEITEMSTACKS_ARGS, 1, 2) ||
                !ArgumentParser.TryParseItem(args, 0, out var itemDef) ||
                !TryParseInventoryTarget(args, 1, out var target))
            {
                return;
            }

            var inventory = target.inventory;
            int count = inventory.GetItemCountPermanent(itemDef) + inventory.GetItemCountTemp(itemDef);
            inventory.RemoveItemPermanent(itemDef, count);
            inventory.RemoveItemTemp(itemDef.itemIndex, count);
            if (target.devotionController)
            {
                target.devotionController.UpdateAllMinions(false);
            }
            Log.MessageNetworked(string.Format(Lang.REMOVEOBJECT_WITH_STACKS, count, itemDef.name, target.name), args);
        }

        [ConCommand(commandName = "remove_all_items", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.REMOVEALLITEMS_HELP)]
        [AutoComplete(Lang.REMOVEALLITEMS_ARGS)]
        private static void CCRemoveAllItems(ConCommandArgs args)
        {
            if (!ArgumentParser.AssertInARun(args) ||
                !ArgumentParser.AssertRequiredArguments(args, Lang.REMOVEALLITEMS_ARGS, 0, 1) ||
                !TryParseInventoryTarget(args, 0, out var target))
            {
                return;
            }

            using (new Inventory.InventoryChangeScope(target.inventory))
            {
                // CleanInventory does not reset temp items, so we have to do it ourselves
                target.inventory.CleanInventory();
                using (HG.CollectionPool<ItemIndex, List<ItemIndex>>.RentCollection(out var itemList))
                {
                    target.inventory.tempItemsStorage.GetNonZeroIndices(itemList);
                    foreach (var itemIndex in itemList)
                    {
                        target.inventory.ResetItemTemp(itemIndex);
                    }
                }
            }
            if (target.devotionController)
            {
                target.devotionController.UpdateAllMinions(false);
            }
            Log.MessageNetworked(string.Format(Lang.RESETOBJECT, "all items", target.name), args);
        }

        [ConCommand(commandName = "remove_equip", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.REMOVEEQUIP_HELP)]
        [AutoComplete(Lang.REMOVEEQUIP_ARGS)]
        private static void CCRemoveEquipment(ConCommandArgs args)
        {
            if (!ArgumentParser.AssertInARun(args) ||
                !ArgumentParser.AssertRequiredArguments(args, Lang.REMOVEEQUIP_ARGS, 0, 1) ||
                !TryParseInventoryTarget(args, 0, out var target))
            {
                return;
            }
            target.inventory.SetEquipmentIndex(EquipmentIndex.None, true);
            Log.MessageNetworked(string.Format(Lang.REMOVEOBJECT, "current Equipment", target.name), args);
        }

        [ConCommand(commandName = "remove_equip_extra", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.REMOVEEQUIPEXTRA_HELP)]
        [AutoComplete(Lang.REMOVEEQUIPEXTRA_ARGS)]
        private static void CCRemoveEquipmentExtra(ConCommandArgs args)
        {
            if (!ArgumentParser.AssertInARun(args) ||
                !ArgumentParser.AssertRequiredArguments(args, Lang.REMOVEEQUIPEXTRA_ARGS, 2, 3) ||
                // Slot and set not optional technically
                !ArgumentParser.TryParseOptionalUInt(args, 0, "slot", default, out var slot) ||
                !ArgumentParser.TryParseOptionalUInt(args, 1, "set", default, out var set) ||
                !TryParseInventoryTarget(args, 2, out var target))
            {
                return;
            }

            var inventory = target.inventory;
            if (inventory._equipmentStateSlots.Length < slot || inventory._equipmentStateSlots[slot].Length < set)
            {
                Log.MessageNetworked("Unassigned equipment slot/set. Nothing to remove.", args, LogLevel.MessageClientOnly);
                return;
            }
            inventory.SetEquipmentIndexForSlot(EquipmentIndex.None, slot, set);
            Log.MessageNetworked($"Removed equipment from {target.name} in position ({slot}, {set})", args);
        }

        [ConCommand(commandName = "restock_equip", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.RESTOCKEQUIP_HELP)]
        [AutoComplete(Lang.RESTOCKEQUIP_ARGS)]
        private static void CCRestockEquip(ConCommandArgs args)
        {
            if (!ArgumentParser.AssertInARun(args) ||
                !ArgumentParser.AssertRequiredArguments(args, Lang.RESTOCKEQUIP_ARGS, 0, 2) ||
                !ArgumentParser.TryParseOptionalInt(args, 0, "count", 1, out var count, min: 0) ||
                !TryParseInventoryTarget(args, 1, out var target))
            {
                return;
            }

            var inventory = target.inventory;
            var currentSlot = inventory.activeEquipmentSlot;
            var currentSet = inventory.activeEquipmentSet[inventory.activeEquipmentSlot];
            var chargesBefore = inventory.GetEquipment(currentSlot, currentSet).charges;
            inventory.RestockEquipmentCharges(currentSlot, currentSet, count);
            var chargesAfter = inventory.GetEquipment(currentSlot, currentSet).charges;
            Log.MessageNetworked($"Restocked {chargesAfter - chargesBefore} for the current equipment of {target.name}", args);
        }

        internal enum ItemType
        {
            None = -1,
            Permanent,
            Temp,
            // If you're here because you're adding a new ItemType, update all the following accordingly! :)
            // GetItemCount, GiveItem, RemoveItem, create_pickup, remove_item_stacks, and dump_build.
        }

        private static int GetItemCount(Inventory inventory, ItemIndex itemIndex, ItemType type)
        {
            switch (type)
            {
                case ItemType.Permanent:
                    return inventory.GetItemCountPermanent(itemIndex);
                case ItemType.Temp:
                    return inventory.GetItemCountTemp(itemIndex);
                default:
                    Log.Message(Lang.NOMESSAGE, LogLevel.Warning);
                    return 0;
            }
        }

        private static void GiveItem(Inventory inventory, ItemIndex itemIndex, int count, ItemType type)
        {
            switch (type)
            {
                case ItemType.Permanent:
                    inventory.GiveItemPermanent(itemIndex, count);
                    break;
                case ItemType.Temp:
                    inventory.GiveItemTemp(itemIndex, count);
                    break;
                default:
                    Log.Message(Lang.NOMESSAGE, LogLevel.Warning);
                    break;
            }
        }

        private static void RemoveItem(Inventory inventory, ItemIndex itemIndex, int count, ItemType type)
        {
            switch (type)
            {
                case ItemType.Permanent:
                    inventory.RemoveItemPermanent(itemIndex, count);
                    break;
                case ItemType.Temp:
                    inventory.RemoveItemTemp(itemIndex, count);
                    break;
                default:
                    Log.Message(Lang.NOMESSAGE, LogLevel.Warning);
                    break;
            }
        }

        private static bool TryParseItemType(ConCommandArgs args, int index, out ItemType itemType)
        {
            itemType = ItemType.Permanent;
            if (args.Count > index && args[index] != Lang.DEFAULT_VALUE)
            {
                if (!StringFinder.TryGetEnumFromPartial(args[index], out itemType) || itemType == ItemType.None)
                {
                    Log.MessageNetworked(string.Format(Lang.INVALID_ARG_VALUE, "item_type"), args, LogLevel.MessageClientOnly);
                    return false;
                }
            }
            return true;
        }

        private static bool TryParseInventoryTarget(ConCommandArgs args, int index, out Util.CommandTarget target)
        {
            target = default;
            if (args.Count > index && args[index] != Lang.DEFAULT_VALUE)
            {
                var targetArg = args[index].ToUpperInvariant();
                switch (targetArg)
                {
                    case Lang.EVOLUTION:
                        {
                            target.inventory = MonsterTeamGainsItemsArtifactManager.monsterTeamInventory;
                            target.name = target.inventory.gameObject.name;
                            return true;
                        }
                    case Lang.SIMULACRUM:
                        {
                            var run = Run.instance as InfiniteTowerRun;
                            if (!run)
                            {
                                Log.MessageNetworked(Lang.NOTINASIMULACRUMRUN_ERROR, args, LogLevel.MessageClientOnly);
                                return false;
                            }
                            target.inventory = run.enemyInventory;
                            target.name = target.inventory.gameObject.name;
                            return true;
                        }
                    case Lang.VOIDFIELDS:
                        {
                            var mission = ArenaMissionController.instance;
                            if (!mission)
                            {
                                Log.MessageNetworked(Lang.NOTINVOIDFIELDS_ERROR, args, LogLevel.MessageClientOnly);
                                return false;
                            }
                            target.inventory = mission.inventory;
                            target.name = target.inventory.gameObject.name;
                            return true;
                        }
                    case Lang.DEVOTION:
                        {
                            if (args.sender == null)
                            {
                                Log.MessageNetworked(string.Format(Lang.DS_INVALIDARG, "devotion"), args, LogLevel.MessageClientOnly);
                                return false;
                            }
                            var targetMaster = args.senderMaster;
                            if (targetMaster == null)
                            {
                                Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                                return false;
                            }
                            target.devotionController = GetDevotionController(targetMaster);
                            target.inventory = target.devotionController._devotionMinionInventory;
                            var player = targetMaster.playerCharacterMasterController;
                            target.name = (player?.GetDisplayName() ?? targetMaster.gameObject.name) + "'s Devotion Inventory";
                            return true;
                        }
                    default:
                        // All that is left is PINGED and player which are handled below.
                        break;
                }
            }
            return ArgumentParser.TryParsePlayerOrPingedTarget(args, index, out target);
        }

        private static bool TryParseDroptable(ConCommandArgs args, int index, bool canDropBeReplaced)
        {
            droptable.selector.Clear();
            droptable.canDropBeReplaced = canDropBeReplaced;
            if (args.Count > index && args[index].ToUpperInvariant() == Lang.ALL)
            {
                foreach (var itemTier in StringFinder.Instance.GetItemTiersFromPartial(""))
                {
                    droptable.Add(availableDropLists[itemTier], 1f);
                }
            }
            else
            {
                var droptableArg = Lang.DROPTABLE_DEFAULT;
                if (args.Count > index && args[index].ToUpperInvariant() != Lang.DEFAULT_VALUE)
                {
                    droptableArg = args[index];
                }
                foreach (var tierData in droptableArg.Split(','))
                {
                    var data = tierData.Split(':');
                    var itemTier = StringFinder.Instance.GetItemTierFromPartial(data[0]);
                    if (itemTier == StringFinder.ItemTier_NotFound)
                    {
                        Log.MessageNetworked(string.Format(Lang.OBJECT_NOTFOUND, "item tier", data[0], "list_itemtier"), args, LogLevel.MessageClientOnly);
                        return false;
                    }
                    float weight = 1f;
                    if (data.Length > 1 && !TextSerialization.TryParseInvariant(data[1], out weight))
                    {
                        Log.MessageNetworked(string.Format(Lang.PARSE_ERROR, "droptable weight", "float"), args, LogLevel.MessageClientOnly);
                        return false;
                    }
                    if (weight < 0f)
                    {
                        Log.MessageNetworked(string.Format(Lang.NEGATIVE_ARG, "droptable weight"), args, LogLevel.MessageClientOnly);
                        return false;
                    }
                    droptable.Add(availableDropLists[itemTier], weight);
                }
            }
            return true;
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

        private static DevotionInventoryController GetDevotionController(CharacterMaster master)
        {
            DevotionInventoryController controller = null;
            foreach (var thisController in DevotionInventoryController.InstanceList)
            {
                if (thisController.SummonerMaster == master)
                {
                    controller = thisController;
                    break;
                }
            }
            if (controller == null)
            {
                GameObject gameObject = GameObject.Instantiate(Addressables.LoadAssetAsync<GameObject>("RoR2/CU8/LemurianEgg/DevotionMinionInventory.prefab").WaitForCompletion());
                controller = gameObject.GetComponent<DevotionInventoryController>();
                controller.GetComponent<TeamFilter>().teamIndex = TeamIndex.Player;
                controller.SummonerMaster = master;
                NetworkServer.Spawn(gameObject);
            }
            return controller;
        }
    }
}
