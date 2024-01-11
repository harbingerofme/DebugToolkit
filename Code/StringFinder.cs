﻿using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace DebugToolkit
{
    /// <summary>
    /// DO NOT INSTANTIATE THIS CLASS YOURSELF! Instead use the static Instance property.
    /// </summary>
    public sealed class StringFinder
    {
        private static readonly Dictionary<string, string[]> BodyAlias = new Dictionary<string, string[]>();
        private static readonly Dictionary<string, string[]> MasterAlias = new Dictionary<string, string[]>();
        private static readonly Dictionary<string, string[]> BuffAlias = new Dictionary<string, string[]>();
        private static readonly Dictionary<string, string[]> DotAlias = new Dictionary<string, string[]>();
        private static readonly Dictionary<string, string[]> ItemAlias = new Dictionary<string, string[]>();
        private static readonly Dictionary<string, string[]> EquipAlias = new Dictionary<string, string[]>();
        private static readonly Dictionary<string, string[]> SkinAlias = new Dictionary<string, string[]>();
        private static StringFinder instance;
        private static readonly List<DirectorCard> characterSpawnCard = new List<DirectorCard>();
        private static readonly List<InteractableSpawnCard> interactableSpawnCards = new List<InteractableSpawnCard>();
        private static readonly string NAME_NOTFOUND = "???";

        public static EliteIndex EliteIndex_NotFound = (EliteIndex)(-2);
        public static ItemTier ItemTier_NotFound = (ItemTier)(-1);
        public static TeamIndex TeamIndex_NotFound = (TeamIndex)(-2);

        public static StringFinder Instance
        {
            get => instance ?? (instance = new StringFinder());
        }

        /// <summary>
        /// Initialises the various alias lists and creates the SpawnCard cache's
        /// </summary>
        private StringFinder()
        {
            BodyAlias.Add("ToolbotBody", new string[] { "MULT", "MUL-T", "ShoppingTrolly" });
            BodyAlias.Add("MercBody", new string[] { "Mercenary", "Ninja" });
            BodyAlias.Add("MageBody", new string[] { "Artificer", "Arti" });
            BodyAlias.Add("EngiBody", new string[] { "Engineer" });
            BodyAlias.Add("HANDBody", new string[] { "HAN-D" });
            BodyAlias.Add("TreebotBody", new string[] { "Treebot", "REX", "PlantBot", "Shrub" });
            BodyAlias.Add("CrocoBody", new string[] { "barney" });
            BodyAlias.Add("RoboBallBossBody", new string[] { "SCU", "roboboss" });
            BodyAlias.Add("SuperRoboBallBossBody", new string[] { "AWU" });

            MasterAlias.Add("LemurianBruiserMasterFire", new string[] { "LemurianBruiserFire", "BruiserFire" });
            MasterAlias.Add("LemurianBruiserMasterIce", new string[] { "LemurianBruiserIce", "BruiserIce" });
            MasterAlias.Add("LemurianBruiserMasterHaunted", new string[] { "LemurianBruiserHaunted", "BruiserHaunter" });
            MasterAlias.Add("LemurianBruiserMasterPoison", new string[] { "LemurianBruiserPoison", "LemurianBruiserBlight", "LemurianBruisermalechite" });
            MasterAlias.Add("MercMonsterMaster", new string[] { "MercMonster" });
            MasterAlias.Add("RoboBallBossMaster", new string[] { "SCU", "roboboss" });
            MasterAlias.Add("SuperRoboBallBossMaster", new string[] { "AWU" });

            ItemAlias.Add("Syringe", new string[] { "drugs" });

            GatherCSCs();
            GatherISCs();
        }

        private static void GatherCSCs()
        {
            GatherAddressableAssets<CharacterSpawnCard>("/csc", (asset) =>
            {
                characterSpawnCard.Add(new DirectorCard
                {
                    spawnCard = asset,
                    forbiddenUnlockableDef = null,
                    minimumStageCompletions = 0,
                    preventOverhead = true,
                    spawnDistance = DirectorCore.MonsterSpawnDistance.Standard,
                });
            });
        }

        private void GatherISCs()
        {
            GatherAddressableAssets<InteractableSpawnCard>("/isc", (asset) => interactableSpawnCards.Add(asset));
            On.RoR2.ClassicStageInfo.Start += AddCurrentStageIscsToCache;
        }

        private static void GatherAddressableAssets<T>(string filterKey, Action<T> onAssetLoaded)
        {
            RoR2Application.onLoad += () =>
            {
                foreach (var resourceLocator in Addressables.ResourceLocators)
                {
                    foreach (var key in resourceLocator.Keys)
                    {
                        var keyString = key.ToString();
                        if (keyString.Contains(filterKey))
                        {
                            var iscLoadRequest = Addressables.LoadAssetAsync<T>(keyString);

                            iscLoadRequest.Completed += (completedAsyncOperation) =>
                            {
                                if (completedAsyncOperation.Status == AsyncOperationStatus.Succeeded)
                                {
                                    onAssetLoaded(completedAsyncOperation.Result);
                                }
                            };
                        }
                    }
                }
            };
        }

        // There is no real good way to query for all custom iscs afaik
        // So let's lazily add them as the player encounter stages
        private void AddCurrentStageIscsToCache(On.RoR2.ClassicStageInfo.orig_Start orig, ClassicStageInfo self)
        {
            orig(self);
            var iscsOfCurrentStage =
                self.interactableCategories.categories.
                SelectMany(category => category.cards).
                Select(directorCard => directorCard.spawnCard).
                Where(spawnCard => interactableSpawnCards.All(existingIsc => existingIsc.name != spawnCard.name)).
                Cast<InteractableSpawnCard>();
            interactableSpawnCards.AddRange(iscsOfCurrentStage);
        }

        /// <summary>
        /// Returns a prepared list of available DirectorCards.
        /// </summary>
        public List<DirectorCard> DirectorCards
        {
            get
            {
                return characterSpawnCard;
            }
        }

        /// <summary>
        /// Returns a prepared list of available InteractableSpawnCards
        /// </summary>
        public List<InteractableSpawnCard> InteractableSpawnCards
        {
            get
            {
                return interactableSpawnCards;
            }
        }

        /// <summary>
        /// Returns a BuffIndex when provided with an index or partial/invariant.
        /// </summary>
        /// <param name="name">Matches either the exact (int)Index or Partial Invariant</param>
        /// <returns>Returns the BuffIndex if a match is found, or returns BuffIndex.None</returns>
        public BuffIndex GetBuffFromPartial(string name)
        {
            return GetBuffsFromPartial(name).DefaultIfEmpty(BuffIndex.None).First();
        }

        /// <summary>
        /// Returns an iterator of BuffIndex's when provided with an index or partial/invariant.
        /// </summary>
        /// <param name="name">Matches either the exact (int)Index or Partial Invariant</param>
        /// <returns>Returns an iterator with all BuffIndex's matched</returns>
        public IEnumerable<BuffIndex> GetBuffsFromPartial(string name)
        {
            if (TextSerialization.TryParseInvariant(name, out int i))
            {
                var index = (BuffIndex)i;
                if (BuffCatalog.GetBuffDef(index) != null)
                {
                    yield return index;
                }
                yield break;
            }
            name = name.ToUpperInvariant();
            foreach (var buff in BuffCatalog.buffDefs)
            {
                if (buff.name.ToUpper().Contains(name))
                {
                    yield return buff.buffIndex;
                }
            }
        }

        /// <summary>
        /// Returns a DotIndex when provided with an index or partial/invariant.
        /// </summary>
        /// <param name="name">Matches either the exact (int)Index or Partial Invariant</param>
        /// <returns>Returns the DotIndex if a match is found, or returns DotIndex.None</returns>
        public DotController.DotIndex GetDotFromPartial(string name)
        {
            return GetDotsFromPartial(name).DefaultIfEmpty(DotController.DotIndex.None).First();
        }

        /// <summary>
        /// Returns an iterator of DotIndex's when provided with an index or partial/invariant.
        /// </summary>
        /// <param name="name">Matches either the exact (int)Index or Partial Invariant</param>
        /// <returns>Returns an iterator with all DotIndex's matched</returns>
        public IEnumerable<DotController.DotIndex> GetDotsFromPartial(string name)
        {
            if (TextSerialization.TryParseInvariant(name, out int i))
            {
                var index = (DotController.DotIndex)i;
                if (DotController.GetDotDef(index) != null)
                {
                    yield return index;
                }
                yield break;
            }
            name = name.ToUpperInvariant();
            foreach (var dot in Enum.GetValues(typeof(DotController.DotIndex)).Cast<DotController.DotIndex>())
            {
                if (dot >= DotController.DotIndex.Bleed && dot < DotController.DotIndex.Count)
                {
                    if (dot.ToString().ToUpper().Contains(name))
                    {
                        yield return dot;
                    }
                }
            }
        }

        /// <summary>
        /// Returns an EquipmentIndex when provided with an index or partial/invariant.
        /// </summary>
        /// <param name="name">Matches either the exact (int)Index or Partial Invariant</param>
        /// <returns>Returns the EquiptmentIndex if a match is found, or returns EquipmentIndex.None</returns>
        public EquipmentIndex GetEquipFromPartial(string name)
        {
            return GetEquipsFromPartial(name).DefaultIfEmpty(EquipmentIndex.None).First();
        }

        /// <summary>
        /// Returns an iterator of EquipmentIndex's when provided with an index or partial/invariant.
        /// </summary>
        /// <param name="name">Matches either the exact (int)Index or Partial Invariant</param>
        /// <returns>Returns an iterator with all EquipmentIndex's matched</returns>
        public IEnumerable<EquipmentIndex> GetEquipsFromPartial(string name)
        {
            if (TextSerialization.TryParseInvariant(name, out int i))
            {
                var index = (EquipmentIndex)i;
                if (EquipmentCatalog.GetEquipmentDef(index) != null)
                {
                    yield return index;
                }
                yield break;
            }
            name = name.ToUpperInvariant();
            foreach (var equip in EquipmentCatalog.equipmentDefs)
            {
                var langInvar = GetLangInvar(equip.nameToken).ToUpper();
                if (equip.name.ToUpper().Contains(name) || langInvar.Contains(RemoveSpacesAndAlike(name)))
                {
                    yield return equip.equipmentIndex;
                }
            }
        }

        /// <summary>
        /// Returns an ItemTierIndex when provided with an index or partial/invariant.
        /// </summary>
        /// <param name="name">Matches either the (int)Index or Partial Invariant</param>
        /// <returns>Returns the ItemTierIndex if a match is found, or returns -1</returns>
        public ItemTier GetItemTierFromPartial(string name)
        {
            return GetItemTiersFromPartial(name).DefaultIfEmpty(ItemTier_NotFound).First();
        }

        /// <summary>
        /// Returns an iterator of ItemTierIndex's when provided with an index or partial/invariant.
        /// </summary>
        /// <param name="name">Matches either the exact (int)Index or Partial Invariant</param>
        /// <returns>Returns an iterator with all ItemTierIndex's matched</returns>
        public IEnumerable<ItemTier> GetItemTiersFromPartial(string name)
        {
            var allItemTierDefs = ItemTierCatalog.allItemTierDefs.OrderBy(t => t.tier).ToList();
            if (TextSerialization.TryParseInvariant(name, out int i))
            {
                var index = (ItemTier)i;
                foreach (var tierDef in allItemTierDefs)
                {
                    if (tierDef.tier == index)
                    {
                        yield return index;
                        break;
                    }
                }
                yield break;
            }
            name = name.ToUpperInvariant();
            foreach (var tierDef in allItemTierDefs)
            {
                if (tierDef.name.ToUpper().Contains(name))
                {
                    yield return tierDef.tier;
                }
            }
        }

        /// <summary>
        /// Returns an ItemIndex when provided with an index or partial/invariant.
        /// </summary>
        /// <param name="name">Matches either the exact (int)Index or Partial Invariant</param>
        /// <returns>Returns the ItemIndex if a match is found, or returns ItemIndex.None</returns>
        public ItemIndex GetItemFromPartial(string name)
        {
            return GetItemsFromPartial(name).DefaultIfEmpty(ItemIndex.None).First();
        }

        /// <summary>
        /// Returns an iterator of ItemIndex's when provided with an index or partial/invariant.
        /// </summary>
        /// <param name="name">Matches either the exact (int)Index or Partial Invariant</param>
        /// <returns>Returns an iterator with all ItemIndex's matched</returns>
        public IEnumerable<ItemIndex> GetItemsFromPartial(string name)
        {
            if (TextSerialization.TryParseInvariant(name, out int i))
            {
                var index = (ItemIndex)i;
                if (ItemCatalog.IsIndexValid(index))
                {
                    yield return index;
                }
                yield break;
            }
            name = name.ToUpperInvariant();
            foreach (var item in ItemCatalog.allItemDefs)
            {
                var langInvar = GetLangInvar(item.nameToken).ToUpper();
                if (item.name.ToUpper().Contains(name) || langInvar.Contains(RemoveSpacesAndAlike(name)))
                {
                    yield return item.itemIndex;
                }
            }
        }

        /// <summary>
        /// Returns a NetworkUser when provided with an index or partial/invariant.
        /// </summary>
        /// <param name="name">Matches either the exact (int)Index or Partial Invariant</param>
        /// <returns>Returns the NetworkUser if a match is found, or returns null</returns>
        public NetworkUser GetPlayerFromPartial(string name)
        {
            return GetPlayersFromPartial(name).DefaultIfEmpty(null).First();
        }

        /// <summary>
        /// Returns an iterator of NetworkUsers when provided with an index or partial/invariant.
        /// </summary>
        /// <param name="name">Matches either the exact (int)Index or Partial Invariant</param>
        /// <returns>Returns an iterator with all NetworkUsers matched</returns>
        public IEnumerable<NetworkUser> GetPlayersFromPartial(string name)
        {
            if (TextSerialization.TryParseInvariant(name, out int i))
            {
                if (i >= 0 && i < NetworkUser.readOnlyInstancesList.Count)
                {
                    yield return NetworkUser.readOnlyInstancesList[i];
                }
                yield break;
            }
            name = name.ToUpperInvariant();
            foreach (var user in NetworkUser.readOnlyInstancesList)
            {
                if (user.userName.ToUpper().Contains(name))
                {
                    yield return user;
                }
            }
        }

        /// <summary>
        /// Returns a TeamIndex when provided with an index or partial/invariant.
        /// </summary>
        /// <param name="name">Matches either the exact (int)Index or Partial Invariant</param>
        /// <returns>Returns the TeamIndex if a match is found, or returns -2</returns>
        public TeamIndex GetTeamFromPartial(string name)
        {
            return GetTeamsFromPartial(name).DefaultIfEmpty(TeamIndex_NotFound).First();
        }

        /// <summary>
        /// Returns an iterator of TeamIndex's when provided with an index or partial/invariant.
        /// </summary>
        /// <param name="name">Matches either the exact (int)Index or Partial Invariant</param>
        /// <returns>Returns an iterator with all TeamIndex's matched</returns>
        public IEnumerable<TeamIndex> GetTeamsFromPartial(string name)
        {
            if (TextSerialization.TryParseInvariant(name, out int i))
            {
                var index = (TeamIndex)i;
                if (index >= TeamIndex.None && index < TeamIndex.Count)
                {
                    yield return index;
                }
                yield break;
            }
            name = name.ToUpperInvariant();
            foreach (var team in Enum.GetValues(typeof(TeamIndex)).Cast<TeamIndex>().OrderBy(t => t))
            {
                if (team >= TeamIndex.None && team < TeamIndex.Count)
                {
                    if (team.ToString().ToUpper().Contains(name))
                    {
                        yield return team;
                    }
                }
            }
        }

        /// <summary>
        /// Returns an EliteIndex when provided with an index or partial/invariant.
        /// </summary>
        /// <param name="name">Matches either the exact (int)Index or Partial Invariant</param>
        /// <returns>Returns the EliteIndex if a match is found, or returns -2</returns>
        public EliteIndex GetEliteFromPartial(string name)
        {
            return GetElitesFromPartial(name).DefaultIfEmpty(EliteIndex_NotFound).First();
        }

        /// <summary>
        /// Returns an iterator of EliteIndex's when provided with an index or partial/invariant.
        /// </summary>
        /// <param name="name">Matches either the exact (int)Index or Partial Invariant</param>
        /// <returns>Returns an iterator with all EliteIndex's matched</returns>
        public IEnumerable<EliteIndex> GetElitesFromPartial(string name)
        {
            if (TextSerialization.TryParseInvariant(name, out int i))
            {
                var index = (EliteIndex)i;
                if (index == EliteIndex.None)
                {
                    yield return index;
                }
                else if (EliteCatalog.GetEliteDef(index) != null)
                {
                    yield return index;
                }
                yield break;
            }
            name = name.ToUpperInvariant();
            if ("NONE".Contains(name))
            {
                yield return EliteIndex.None;
            }
            foreach (var elite in EliteCatalog.eliteDefs)
            {
                var langInvar = GetLangInvar(elite.modifierToken).Replace("{0}", "").ToUpper();
                if (elite.name.ToUpper().Contains(name) || langInvar.Contains(name))
                {
                    yield return elite.eliteIndex;
                }
            }
        }

        /// <summary>
        /// Returns a DirectorCard when provided with an index or partial/invariant.
        /// </summary>
        /// <param name="name">Matches either the exact (int)Index or Partial Invariant</param>
        /// <returns>Returns the DirectorCard if a match is found, or returns null</returns>
        public DirectorCard GetDirectorCardFromPartial(string name)
        {
            return GetDirectorCardsFromPartial(name).DefaultIfEmpty(null).First();
        }

        /// <summary>
        /// Returns an iterator of DirectorCards when provided with an index or partial/invariant.
        /// </summary>
        /// <param name="name">Matches either the exact (int)Index or Partial Invariant</param>
        /// <returns>Returns an iterator with all DirectorCards matched</returns>
        public IEnumerable<DirectorCard> GetDirectorCardsFromPartial(string name)
        {
            if (TextSerialization.TryParseInvariant(name, out int i))
            {
                if (i >= 0 && i < characterSpawnCard.Count)
                {
                    yield return characterSpawnCard[i];
                }
                yield break;
            }
            name = name.ToUpperInvariant();
            foreach (var dc in characterSpawnCard)
            {
                if (dc.spawnCard.name.ToUpper().Contains(name))
                {
                    yield return dc;
                }
            }
        }

        /// <summary>
        /// Returns an InteractableSpawnCard when provided with an index or partial/invariant.
        /// </summary>
        /// <param name="name">Matches either the exact (int)Index or Partial Invariant</param>
        /// <returns>Returns the InteractableSpawnCard if a match is found, or returns null</returns>
        public InteractableSpawnCard GetInteractableSpawnCardFromPartial(string name)
        {
            return GetInteractableSpawnCardsFromPartial(name).DefaultIfEmpty(null).First();
        }

        /// <summary>
        /// Returns an iterator of InteractableSpawnCards when provided with an index or partial/invariant.
        /// </summary>
        /// <param name="name">Matches either the exact (int)Index or Partial Invariant</param>
        /// <returns>Returns an iterator with all InteractableSpawnCards matched</returns>
        public IEnumerable<InteractableSpawnCard> GetInteractableSpawnCardsFromPartial(string name)
        {
            if (TextSerialization.TryParseInvariant(name, out int i))
            {
                if (i >= 0 && i < interactableSpawnCards.Count)
                {
                    yield return interactableSpawnCards[i];
                }
                yield break;
            }
            name = name.ToUpperInvariant();
            foreach (var isc in interactableSpawnCards)
            {
                var langInvar = GetLangInvar(GetInteractableName(isc.prefab)).ToUpper();
                if (isc.name.ToUpper().Contains(name) || langInvar.Contains(name))
                {
                    yield return isc;
                }
            }
        }

        /// <summary>
        /// Returns a BodyIndex when provided with an index or partial/invariant.
        /// </summary>
        /// <param name="name">Matches either the exact (int)Index or Partial Invariant</param>
        /// <returns>Returns the BodyIndex if a match is found, or returns BodyIndex.None</returns>
        public BodyIndex GetBodyFromPartial(string name)
        {
            return GetBodiesFromPartial(name).DefaultIfEmpty(BodyIndex.None).First();
        }

        /// <summary>
        /// Returns an iterator of BodyIndex's when provided with an index or partial/invariant.
        /// </summary>
        /// <param name="name">Matches either the exact (int)Index or Partial Invariant</param>
        /// <returns>Returns an iterator with all BodyIndex's matched</returns>
        public IEnumerable<BodyIndex> GetBodiesFromPartial(string name)
        {
            if (TextSerialization.TryParseInvariant(name, out int i))
            {
                var index = (BodyIndex)i;
                if (BodyCatalog.GetBodyPrefab(index) != null)
                {
                    yield return index;
                }
                yield break;
            }
            name = name.ToUpperInvariant();
            foreach (var body in BodyCatalog.allBodyPrefabBodyBodyComponents)
            {
                var langInvar = GetLangInvar(body.baseNameToken).ToUpper();
                if (body.name.ToUpper().Contains(name) || langInvar.Contains(RemoveSpacesAndAlike(name)))
                {
                    yield return body.bodyIndex;
                }
            }
        }

        /// <summary>
        /// Returns a MasterIndex when provided with an index or partial/invariant.
        /// </summary>
        /// <param name="name">Matches either the exact (int)Index or Partial Invariant</param>
        /// <returns>Returns the MasterIndex if a match is found, or returns MasterIndex.none</returns>
        public MasterCatalog.MasterIndex GetAiFromPartial(string name)
        {
            return GetAisFromPartial(name).DefaultIfEmpty(MasterCatalog.MasterIndex.none).First();
        }

        /// <summary>
        /// Returns an iterator of MasterIndex's when provided with an index or partial/invariant.
        /// </summary>
        /// <param name="name">Matches either the exact (int)Index or Partial Invariant</param>
        /// <returns>Returns an iterator with all MasterIndex's matched</returns>
        public IEnumerable<MasterCatalog.MasterIndex> GetAisFromPartial(string name)
        {
            if (TextSerialization.TryParseInvariant(name, out int i))
            {
                var index = (MasterCatalog.MasterIndex)i;
                if (MasterCatalog.GetMasterPrefab(index) != null)
                {
                    yield return index;
                }
                yield break;
            }
            name = name.ToUpperInvariant();
            foreach (var ai in MasterCatalog.allAiMasters)
            {
                var langInvar = GetLangInvar(GetMasterName(ai)).ToUpper();
                if (ai.name.ToUpper().Contains(name) || langInvar.Contains(name))
                {
                    yield return ai.masterIndex;
                }
            }
        }

        /// <summary>
        /// Returns a special char stripped language invariant when provided with a BaseNameToken.
        /// </summary>
        /// <param name="baseToken">The BaseNameToken to query for a Language Invariant.</param>
        /// <returns>Returns the LanguageInvariant for the BaseNameToken, or returns an empty string for a null input.</returns>
        public static string GetLangInvar(string baseToken)
        {
            if (baseToken == null)
            {
                return "";
            }
            return RemoveSpacesAndAlike(Language.GetString(baseToken));
        }

        public static string RemoveSpacesAndAlike(string input)
        {
            return Regex.Replace(input, @"[ '-]", string.Empty);
        }

        public static string GetInteractableName(GameObject prefab)
        {
            if (prefab == null)
            {
                return NAME_NOTFOUND;
            }
            var display = prefab.GetComponent<IDisplayNameProvider>();
            if (display != null)
            {
                return display.GetDisplayName();
            }
            var multishop = prefab.GetComponent<MultiShopController>();
            if (multishop != null && multishop.terminalPrefab)
            {
                display = multishop.terminalPrefab.GetComponent<IDisplayNameProvider>();
                if (display != null)
                {
                    return display.GetDisplayName();
                }
            }
            return NAME_NOTFOUND;
        }

        public static string GetMasterName(CharacterMaster master)
        {
            if (master == null)
            {
                return NAME_NOTFOUND;
            }
            var body = master?.bodyPrefab.GetComponent<CharacterBody>();
            if (body != null)
            {
                return body.GetDisplayName();
            }
            return NAME_NOTFOUND;
        }

        /// <summary>
        /// Will match an (int)TEnum, or a TEnum.ToString partial with a specific TEnum
        /// </summary>
        /// <typeparam name="TEnum">The Enum type desired to match against</typeparam>
        /// <param name="name">The (int)TEnum, or a TEnum.ToString partial</param>
        /// <returns>Returns the match TEnum.</returns>
        public static TEnum GetEnumFromPartial<TEnum>(string name)
        {
            TryGetEnumFromPartial<TEnum>(name, out TEnum result);
            return result;
        }

        public static bool TryGetEnumFromPartial<TEnum>(string name, out TEnum result)
        {
            if (typeof(TEnum).GetEnumUnderlyingType() == typeof(int))
            {
                if (int.TryParse(name, out int index))
                {
                    if (Enum.IsDefined(typeof(TEnum), index))
                    {
                        result = (TEnum)Enum.ToObject(typeof(TEnum), index);
                        return true;
                    }
                    result = default;
                    return false;
                }
            }
            else if (typeof(TEnum).GetEnumUnderlyingType() == typeof(sbyte))
            {
                if (sbyte.TryParse(name, out sbyte index))
                {
                    if (Enum.IsDefined(typeof(TEnum), index))
                    {
                        result = (TEnum)Enum.ToObject(typeof(TEnum), index);
                        return true;
                    }
                    result = default;
                    return false;
                }
            }

            var array = Enum.GetValues(typeof(TEnum));
            foreach (TEnum num in array)
            {
                if (Enum.GetName(typeof(TEnum), num).ToUpper().Contains(name.ToUpper()))
                {
                    result = (TEnum)num;
                    return true;
                }
            }
            result = default;
            return false;
        }
    }
}
