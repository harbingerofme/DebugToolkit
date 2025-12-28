using RoR2;
using RoR2.Artifacts;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static DebugToolkit.Log;
using static DebugToolkit.Util;

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

        [ConCommand(commandName = "list_drone", flags = ConVarFlags.None, helpText = Lang.LISTDRONE_HELP)]
        [AutoComplete(Lang.LISTQUERY_ARGS)]
        private static void CCListDrone(ConCommandArgs args)
        {
            var sb = new StringBuilder();
            var arg = args.Count > 0 ? args[0] : "";
            var indices = StringFinder.Instance.GetDronesFromPartial(arg);
            foreach (var index in indices)
            {
                var definition = DroneCatalog.GetDroneDef(index);
                var realName = Language.currentLanguage.GetLocalizedStringByToken(definition.nameToken);
                bool enabled = Run.instance && Run.instance.IsDroneAvailable(index);
                sb.AppendLine($"[{(int)index}]{definition.name} \"{realName}\" (enabled={enabled})");
            }
            var s = sb.Length > 0 ? sb.ToString().TrimEnd('\n') : string.Format(Lang.NOMATCH_ERROR, "drone", arg);
            Log.MessageNetworked(s, args, LogLevel.MessageClientOnly);
        }

        [ConCommand(commandName = "list_pickups", flags = ConVarFlags.None, helpText = Lang.LISTDRONE_HELP)]
        [AutoComplete(Lang.LISTQUERY_ARGS)]
        private static void CCListPickup(ConCommandArgs args)
        {
            var sb = new StringBuilder();
            var arg = args.Count > 0 ? args[0] : "";
            var indices = StringFinder.Instance.GetPickupsFromPartial(arg);
            foreach (var index in indices)
            {
                var definition = PickupCatalog.GetPickupDef(index);
                var realName = Language.currentLanguage.GetLocalizedStringByToken(definition.nameToken);
                bool enabled = Run.instance && Run.instance.IsPickupAvailable(index);
                sb.AppendLine($"[{index.value}]{definition.internalName} \"{realName}\" (enabled={enabled})");
            }
            var s = sb.Length > 0 ? sb.ToString().TrimEnd('\n') : string.Format(Lang.NOMATCH_ERROR, "pickups", arg);
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
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            bool isDedicatedServer = args.sender == null;
            if (args.Count == 0 || (isDedicatedServer && (args.Count < 4 || args[3] == Lang.DEFAULT_VALUE)))
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

            var type = ParseItemType(args, 2);
            if (type <= ItemType.None || type >= ItemType.Count)
            {
                Log.MessageNetworked(String.Format(Lang.INVALID_ARG_VALUE, "type"), args, LogLevel.MessageClientOnly);
                return;
            }

            var target = ParseTarget(args, 3);
            if (target.failMessage != null)
            {
                Log.MessageNetworked(target.failMessage, args, LogLevel.MessageClientOnly);
                return;
            }

            var item = StringFinder.Instance.GetItemFromPartial(args[0]);
            if (item == ItemIndex.None)
            {
                Log.MessageNetworked(string.Format(Lang.OBJECT_NOTFOUND, "item", args[0]), args, LogLevel.MessageClientOnly);
                return;
            }
            var itemDef = ItemCatalog.GetItemDef(item);
            var name = itemDef.name;

            name = getItemTypeName(type) + name;

            var amount = (args.commandName == "give_item" ? 1 : -1) * iCount;
            var inventory = target.inventory;
            if (amount > 0)
            {
                if (Run.instance.IsItemExpansionLocked(item))
                {
                    Log.MessageNetworked(string.Format(Lang.EXPANSION_LOCKED, "item", Util.GetExpansion(itemDef.requiredExpansion)), args, LogLevel.MessageClientOnly);
                    return;
                }
                GiveItem(inventory, item, amount, type);
                Log.MessageNetworked(string.Format(Lang.GIVEITEM, amount, name, target.name), args);
            }
            else if (amount < 0)
            {
                amount = Math.Min(-amount, GetItemCount(inventory, item, type));
                RemoveItem(inventory, item, amount, type);
                Log.MessageNetworked(string.Format(Lang.REMOVEOBJECT, amount, name, target.name), args);
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

        public static string getItemTypeName(ItemType type)
        {
            switch (type)
            {
                case ItemType.Permanent:
                    return "";
                case ItemType.Temp:
                    return "<color=#53E9FF>Temporary </color>"; //Saturated by 50% Temp color
                    //return "<style=cIsTemporary>temporary</color>"; //Bit hard to see in the console
            }
            return type.ToString();
        }

        [ConCommand(commandName = "give_drone", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.GIVEDRONE_HELP)]
        [AutoComplete(Lang.GIVEDRONE_ARGS)]
        private static void CCGiveDrone(ConCommandArgs args)
        {
            //give_drone {name} {amount} {tier}
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            bool isDedicatedServer = args.sender == null;
            if (args.Count == 0 || (isDedicatedServer && (args.Count < 4 || args[3] == Lang.DEFAULT_VALUE)))
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.GIVEITEM_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }

            int amount = 1;
            if (args.Count > 1 && args[1] != Lang.DEFAULT_VALUE && !TextSerialization.TryParseInvariant(args[1], out amount))
            {
                Log.MessageNetworked(String.Format(Lang.PARSE_ERROR, "count", "int"), args, LogLevel.MessageClientOnly);
                return;
            }
            int tier = 1;
            if (args.Count > 2 && args[2] != Lang.DEFAULT_VALUE && !TextSerialization.TryParseInvariant(args[2], out tier))
            {
                Log.MessageNetworked(String.Format(Lang.PARSE_ERROR, "tier", "int"), args, LogLevel.MessageClientOnly);
                return;
            }
            if (tier < 0)
            {
                Log.MessageNetworked(string.Format(Lang.NEGATIVE_ARG, "count"), args, LogLevel.MessageClientOnly);
                return;
            }
            var target = Buffs.ParseTarget(args, 3);
            if (target.failMessage != null)
            {
                Log.MessageNetworked(target.failMessage, args, LogLevel.MessageClientOnly);
                return;
            }
            if (!target.body)
            {
                Log.MessageNetworked("No target body found", args, LogLevel.MessageClientOnly);
                return;
            }

            var drone = StringFinder.Instance.GetDroneFromPartial(args[0]);
            if (drone == DroneIndex.None)
            {
                Log.MessageNetworked(string.Format(Lang.OBJECT_NOTFOUND, "drone", args[0]), args, LogLevel.MessageClientOnly);
                return;
            }
            DroneDef droneDef = DroneCatalog.GetDroneDef(drone);
 
            if (droneDef == null)
            {
                Log.MessageNetworked(string.Format(Lang.OBJECT_NOTFOUND, "drone", args[0]), args, LogLevel.MessageClientOnly);
                return;
            }
            if (Run.instance.IsDroneExpansionLocked(drone))
            {
                Log.MessageNetworked(string.Format(Lang.EXPANSION_LOCKED, "drone", Util.GetExpansion(droneDef.requiredExpansion)), args, LogLevel.MessageClientOnly);
                return;
            }
            if (amount > 50)
            {
                amount = 50;
                Log.MessageNetworked($"Limited to 50, please don't spawn too many things at once.", args, LogLevel.MessageClientOnly);
            }
            if (amount > 0)
            {
                for (int i = 0; i < amount; i++) 
                {
                    CharacterMaster newlySpawnedDrone = new MasterSummon
                    {
                        masterPrefab = droneDef.masterPrefab,
                        position = target.body.transform.position,
                        rotation = target.body.transform.rotation,
                        summonerBodyObject = target.body.gameObject,
                        ignoreTeamMemberLimit = true,
                        useAmbientLevel = true,
                        enablePrintController = true
                    }.Perform();
                    if (tier > 1)
                    {
                        newlySpawnedDrone.inventory.GiveItemPermanent(DLC3Content.Items.DroneUpgradeHidden, (tier - 1));
                    } 
                }
                var name = droneDef.name;
                Log.MessageNetworked(string.Format(Lang.GIVEDRONE, amount, name, target.name, tier), args);
            }
            else
            {
                Log.MessageNetworked("Nothing happened", args);
            }
        
        }

 
        [ConCommand(commandName = "random_items", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.RANDOMITEM_HELP)]
        [AutoComplete(Lang.RANDOMITEM_ARGS)]
        private static void CCRandomItemsTiered(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            bool isDedicatedServer = args.sender == null;
            if (args.Count == 0 || (isDedicatedServer && (args.Count < 4 || args[3] == Lang.DEFAULT_VALUE)))
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

            var type = ParseItemType(args, 2);
            if (type <= ItemType.None || type >= ItemType.Count)
            {
                Log.MessageNetworked(String.Format(Lang.INVALID_ARG_VALUE, "type"), args, LogLevel.MessageClientOnly);
                return;
            }

            var target = ParseTarget(args, 3);
            if (target.failMessage != null)
            {
                Log.MessageNetworked(target.failMessage, args, LogLevel.MessageClientOnly);
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
                    var uniquePickup = weightedSelection.Evaluate(UnityEngine.Random.value);
                    var pickupDef = PickupCatalog.GetPickupDef(uniquePickup.pickupIndex);
                    var item = pickupDef?.itemIndex ?? ItemIndex.None;
                    GiveItem(target.inventory, item, 1, type);
                }
                if (target.devotionController)
                {
                    target.devotionController.UpdateAllMinions(false);
                }
                Log.MessageNetworked($"Generated {iCount} {getItemTypeName(type)}items for {target.name}!", args);
            }
            else
            {
                Log.MessageNetworked("'count' should be a non-zero positive integer.", args, LogLevel.MessageClientOnly);
            }
        }

        [ConCommand(commandName = "give_equip", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.GIVEEQUIP_HELP)]
        [AutoComplete(Lang.GIVEEQUIP_ARGS)]
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

            var target = ParseTarget(args, 1);
            if (target.failMessage != null)
            {
                Log.MessageNetworked(target.failMessage, args, LogLevel.MessageClientOnly);
                return;
            }

            var inventory = target.inventory;
            var equip = EquipmentIndex.None;
            if (args[0].ToUpperInvariant() == Lang.RANDOM)
            {
                inventory.GiveRandomEquipment();
                equip = inventory.GetEquipmentIndex();
            }
            else if (args[0] == "-1")
            {
                inventory.SetEquipmentIndex(EquipmentIndex.None, true);
                Log.MessageNetworked($"Removed current Equipment from {target.name}", args);
                return;
            }
            else
            {
                equip = StringFinder.Instance.GetEquipFromPartial(args[0]);
                if (equip == EquipmentIndex.None)
                {
                    Log.MessageNetworked(string.Format(Lang.OBJECT_NOTFOUND, "equip", args[0]), args, LogLevel.MessageClientOnly);
                    return;
                }
                if (Run.instance.IsEquipmentExpansionLocked(equip))
                {
                    Log.MessageNetworked(string.Format(Lang.EXPANSION_LOCKED, "equipment", Util.GetExpansion(EquipmentCatalog.GetEquipmentDef(equip).requiredExpansion)), args, LogLevel.MessageClientOnly);
                    return;
                }
                inventory.SetEquipmentIndex(equip, false);
            }
            var name = EquipmentCatalog.GetEquipmentDef(equip).name;

            Log.MessageNetworked($"Gave {name} to {target.name}", args);
        }

        [ConCommand(commandName = "random_equip", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.RANDOMEQUIP_HELP)]
        [AutoComplete(Lang.RANDOMEQUIP_ARGS)]
        private static void CCGiveRandomEquipment(ConCommandArgs args)
        {
            DebugToolkit.InvokeCMD(args.sender, "give_equip", "RANDOM");
        }


        [ConCommand(commandName = "create_pickup", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.CREATEPICKUP_HELP)]
        [AutoComplete(Lang.CREATEPICKUP_ARGS)]
        private static void CCCreatePickup(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            if (args.Count == 0 || (args.Count < 4 && args.sender == null))
            {
                Log.Message(Lang.INSUFFICIENT_ARGS + Lang.CREATEPICKUP_ARGS, LogLevel.MessageClientOnly);
                return;
            }
            NetworkUser player = args.sender;
            if (args.Count > 3)
            {
                player = Util.GetNetUserFromString(args.userArgs, 3);
                if (player == null)
                {
                    Log.MessageNetworked(Lang.PLAYER_NOTFOUND, args, LogLevel.MessageClientOnly);
                    return;
                }
            }
            var body = player.GetCurrentBody();
            if (body == null)
            {
                // We could possibly use `player.master.deathFootPosition` instead
                Log.MessageNetworked("Can't spawn an object with relation to a dead player.", args, LogLevel.MessageClientOnly);
                return;
            }

            ItemType type = ItemType.Permanent;
            if (args.Count > 2 && args[2] != Lang.DEFAULT_VALUE)
            {
                type = ParseItemType(args, 2);
                if (type <= ItemType.None || type >= ItemType.Count)
                {
                    Log.MessageNetworked(String.Format(Lang.INVALID_ARG_VALUE, "type"), args, LogLevel.MessageClientOnly);
                    return;
                }
            }

            bool searchEquip = true, searchItem = true, searchDrone = true, skip = false;
            if (args.Count > 1 && args[1] != Lang.DEFAULT_VALUE)
            {
                switch (args[1].ToUpperInvariant())
                {
                    case Lang.ALL:
                        break;
                    case Lang.PICKUP:
                        skip = true;
                        break;
                    case Lang.ITEM:
                        searchEquip = false;
                        searchDrone = false;
                        break;
                    case Lang.EQUIP:
                        searchItem = false;
                        searchDrone = false;
                        break;
                    case Lang.DRONE:
                        searchItem = false;
                        searchEquip = false;
                        break;
                    default:
                        Log.MessageNetworked(String.Format(Lang.INVALID_ARG_VALUE, "search"), args, LogLevel.MessageClientOnly);
                        return;
                }
            }
            PickupIndex final = PickupIndex.none;
            DroneIndex drone = DroneIndex.None;
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
                    if (skip)
                    {
                        int? pickup2 = args.TryGetArgInt(0);
                        if (pickup2 != null)
                        {
                            final.value = (int)pickup2;
                        }
                        else
                        {
                            final = StringFinder.Instance.GetPickupFromPartial(args[0]);
                        }
                    }
                    else
                    {      
                        if (searchEquip)
                        {
                            equipment = StringFinder.Instance.GetEquipFromPartial(args[0]);
                        }
                        if (searchItem)
                        {
                            item = StringFinder.Instance.GetItemFromPartial(args[0]);
                        }
                        if (searchDrone)
                        {
                            drone = StringFinder.Instance.GetDroneFromPartial(args[0]);
                        }
                        if (item == ItemIndex.None && equipment == EquipmentIndex.None && drone == DroneIndex.None)
                        {
                            Log.MessageNetworked(Lang.CREATEPICKUP_NOTFOUND, args, LogLevel.MessageClientOnly);
                            return;
                        }
                        else if(item != ItemIndex.None && equipment != EquipmentIndex.None||
                                drone != DroneIndex.None && equipment != EquipmentIndex.None ||
                                item != ItemIndex.None && drone != DroneIndex.None )
                        {
                            var def1 = ItemCatalog.GetItemDef(item);
                            var def2 = EquipmentCatalog.GetEquipmentDef(equipment);
                            var def3 = DroneCatalog.GetDroneDef(drone);
                            string foundResults = string.Empty;
                            if (def1)
                            {
                                foundResults += $"{item}|{def1.name}|{Language.GetString(def1.nameToken)}";
                            }
                            if (def2)
                            {
                                foundResults += $"{equipment}|{def2.name}|{Language.GetString(def2.nameToken)}";
                            }
                            if (def3)
                            {
                                foundResults += $"{drone}|{def3.name}|{Language.GetString(def3.nameToken)}";
                            }
                            Log.MessageNetworked(string.Format(Lang.CREATEPICKUP_AMBIGIOUS_4, foundResults), args, LogLevel.MessageClientOnly);
                            return;
                        }
                        else if (equipment != EquipmentIndex.None)
                        {
                            final = PickupCatalog.FindPickupIndex(equipment);
                        }
                        else if (drone != DroneIndex.None)
                        {
                            final = PickupCatalog.FindPickupIndex(drone);
                        }
                        else if (item != ItemIndex.None)
                        {
                            final = PickupCatalog.FindPickupIndex(item);
                        }
                        else
                        {
                        final.value = args.TryGetArgInt(0).GetValueOrDefault(-1);
                        }
                    }
                    break;
            }

            Log.MessageNetworked(string.Format(Lang.CREATEPICKUP_SUCCESS_1, final), args);
            PickupDropletController.CreatePickupDroplet(new GenericPickupController.CreatePickupInfo
            {
                pickup = new UniquePickup
                {
                    pickupIndex = final,
                    decayValue = type == ItemType.Temp ? 1f : 0f,
                    //upgradeValue 
                },
            }, body.transform.position, body.inputBank.aimDirection * 30f);
        }

        [ConCommand(commandName = "create_potential", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.CREATEPOTENTIAL_HELP)]
        [AutoComplete(Lang.CREATEPOTENTIAL_ARGS)]
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
            var body = player.GetCurrentBody();
            if (body == null)
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
                position = body.transform.position,
                rotation = Quaternion.identity,
                pickup = new UniquePickup(PickupCatalog.FindPickupIndex(firstItemTier))
            }, body.transform.position, body.inputBank.aimDirection * 30f);
            Log.MessageNetworked(string.Format(Lang.CREATEPICKUP_SUCCESS_2, Math.Min(iCount, droptable.selector.Count)), args);
        }

        [ConCommand(commandName = "remove_item_stacks", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.REMOVEITEMSTACKS_HELP)]
        [AutoComplete(Lang.REMOVEITEMSTACKS_ARGS)]
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

            var target = ParseTarget(args, 1);
            if (target.failMessage != null)
            {
                Log.MessageNetworked(target.failMessage, args, LogLevel.MessageClientOnly);
                return;
            }

            var inventory = target.inventory;
            var item = StringFinder.Instance.GetItemFromPartial(args[0]);
            if (item == ItemIndex.None)
            {
                Log.MessageNetworked(string.Format(Lang.OBJECT_NOTFOUND, "item", args[0]), args, LogLevel.MessageClientOnly);
                return;
            }
            var itemDef = ItemCatalog.GetItemDef(item);
            var name = itemDef.name;
            int count = inventory.GetItemCountPermanent(item) + inventory.GetItemCountTemp(item);
            if (Run.instance.IsItemExpansionLocked(item))
            {
                Log.MessageNetworked(string.Format(Lang.EXPANSION_LOCKED, "item", Util.GetExpansion(itemDef.requiredExpansion)), args, LogLevel.MessageClientOnly);
                return;
            }
            inventory.RemoveItemPermanent(item, count);
            inventory.RemoveItemTemp(item, count);
            if (target.devotionController)
            {
                target.devotionController.UpdateAllMinions(false);
            }
            Log.MessageNetworked(string.Format(Lang.REMOVEOBJECT, count, name, target.name), args);
        }

        [ConCommand(commandName = "remove_all_items", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.REMOVEALLITEMS_HELP)]
        [AutoComplete(Lang.REMOVEALLITEMS_ARGS)]
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

            var target = ParseTarget(args, 0);
            if (target.failMessage != null)
            {
                Log.MessageNetworked(target.failMessage, args, LogLevel.MessageClientOnly);
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
            Log.MessageNetworked($"Reseting inventory for {target.name}", args);
        }

        [ConCommand(commandName = "remove_equip", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.REMOVEEQUIP_HELP)]
        [AutoComplete(Lang.REMOVEEQUIP_ARGS)]
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

            var target = ParseTarget(args, 0);
            if (target.failMessage != null)
            {
                Log.MessageNetworked(target.failMessage, args, LogLevel.MessageClientOnly);
                return;
            }

            target.inventory.SetEquipmentIndex(EquipmentIndex.None, true);
            Log.MessageNetworked($"Removed current Equipment from {target.name}", args);
        }

        [ConCommand(commandName = "restock_equip", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.RESTOCKEQUIP_HELP)]
        [AutoComplete(Lang.RESTOCKEQUIP_ARGS)]
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
            var target = ParseTarget(args, 1);
            if (target.failMessage != null)
            {
                Log.MessageNetworked(target.failMessage, args, LogLevel.MessageClientOnly);
                return;
            }

            var inventory = target.inventory;
            var currentSlot = inventory.activeEquipmentSlot;
            var currentSet = inventory.activeEquipmentSet[inventory.activeEquipmentSlot];
            var chargesBefore = inventory.GetEquipment(currentSlot, currentSet).charges;
            inventory.RestockEquipmentCharges(currentSlot, currentSet, iCount);
            var chargesAfter = inventory.GetEquipment(currentSlot, currentSet).charges;
            Log.MessageNetworked($"Restocked {chargesAfter - chargesBefore} for the current equipment of {target.name}", args);
        }

        internal enum ItemType
        {
            None,
            Permanent,
            Temp,
            Channeled,
            Count,
        }

        private static int GetItemCount(Inventory inventory, ItemIndex itemIndex, ItemType type)
        {
            switch (type)
            {
                case ItemType.Permanent:
                    return inventory.GetItemCountPermanent(itemIndex);
                case ItemType.Temp:
                    return inventory.GetItemCountTemp(itemIndex);
                case ItemType.Channeled:
                    return inventory.GetItemCountChanneled(itemIndex);
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
                case ItemType.Channeled:
                    inventory.GiveItemChanneled(itemIndex, count);
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
                case ItemType.Channeled:
                    inventory.RemoveItemChanneled(itemIndex, count);
                    break;
                default:
                    Log.Message(Lang.NOMESSAGE, LogLevel.Warning);
                    break;
            }
        }

        private static ItemType ParseItemType(ConCommandArgs args, int index)
        {
            if (args.Count > index && args[index] != Lang.DEFAULT_VALUE)
            {
                return Enum.TryParse(args[index], true, out ItemType itemType) ? itemType : ItemType.None;
            }
            return ItemType.Permanent;
        }

        public static CommandTarget ParseTarget(ConCommandArgs args, int index)
        {
            string failMessage = null;
            Inventory inventory = null;
            string targetName = null;
            DevotionInventoryController devotionController = null;
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
                        failMessage = Lang.NOTINASIMULACRUMRUN_ERROR;
                    }
                    else
                    {
                        inventory = run.enemyInventory;
                        targetName = inventory?.gameObject.name;
                    }
                }
                else if (targetArg == Lang.VOIDFIELDS)
                {
                    var mission = ArenaMissionController.instance;
                    if (!mission)
                    {
                        failMessage = Lang.NOTINVOIDFIELDS_ERROR;
                    }
                    else
                    {
                        inventory = mission.inventory;
                        targetName = inventory?.gameObject.name;
                    }
                }
                else if (targetArg == Lang.DEVOTION)
                {
                    if (args.sender == null)
                    {
                        failMessage = String.Format(Lang.DS_INVALIDARG, "devotion");
                    }
                    else
                    {
                        var target = args.senderMaster;
                        if (target == null)
                        {
                            failMessage = Lang.PLAYER_NOTFOUND;
                        }
                        else
                        {
                            devotionController = GetDevotionController(target);
                            inventory = devotionController._devotionMinionInventory;
                            var player = target.playerCharacterMasterController?.networkUser;
                            targetName = (player?.masterController.GetDisplayName() ?? target.gameObject.name) + "'s Devotion Inventory";
                        }
                    }
                }
                else
                {
                    var isDedicatedServer = args.sender == null;
                    var target = Util.GetTargetFromArgs(args, index);
                    if (target == null && !isDedicatedServer && targetArg == Lang.PINGED)
                    {
                        failMessage = Lang.PINGEDBODY_NOTFOUND;
                    }
                    else if (target == null)
                    {
                        failMessage = Lang.PLAYER_NOTFOUND;
                    }
                    else
                    {
                        inventory = target.inventory;
                        var player = target.playerCharacterMasterController?.networkUser;
                        targetName = target.bodyInstanceObject ? RoR2.Util.GetBestBodyName(target.bodyInstanceObject) : player?.masterController.GetDisplayName() ?? target.gameObject.name;
                    }
                }
            }
            else
            {
                var target = args.senderMaster;
                if (target == null)
                {
                    failMessage = Lang.PLAYER_NOTFOUND;
                }
                else
                {
                    inventory = target.inventory;
                    var player = target.playerCharacterMasterController?.networkUser;
                    targetName = player?.masterController.GetDisplayName() ?? target.gameObject.name;
                }
            }
            if (failMessage == null && inventory == null)
            {
                failMessage = Lang.INVENTORY_ERROR;
            }
            if (failMessage != null)
            {
                return new CommandTarget
                {
                    failMessage = failMessage
                };
            }
            return new CommandTarget
            {
                name = targetName,
                inventory = inventory,
                devotionController = devotionController
            };
        }

        private static BasicPickupDropTable ParseDroptable(ConCommandArgs args, int index, bool canDropBeReplaced)
        {
            droptable.selector.Clear();
            droptable.canDropBeReplaced = canDropBeReplaced;
            if (args.Count < index + 1 || args[index] == Lang.DEFAULT_VALUE)
            {
                droptable.Add(availableDropLists[ItemTier.Tier1], 1f);
                droptable.Add(availableDropLists[ItemTier.Tier2], 1f);
                droptable.Add(availableDropLists[ItemTier.Tier3], 1f);
                droptable.Add(availableDropLists[ItemTier.Boss], 1f);
            }
            else if (args[index].ToUpperInvariant() == Lang.ALL)
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
                        Log.MessageNetworked(string.Format(Lang.OBJECT_NOTFOUND, "item tier", data[0]), args, LogLevel.MessageClientOnly);
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
