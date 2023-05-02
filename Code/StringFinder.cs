using R2API.Utils;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        private static List<InteractableSpawnCard> interactableSpawnCards = new List<InteractableSpawnCard>();

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
        /// Returns a BuffIndex when provided with a partial/invariant.
        /// </summary>
        /// <param name="name">Matches in order: (int)Index, Exact Alias, Exact Index, Partial Index, Partial Invariant</param>
        /// <returns>Returns the BuffIndex if a match is found, or returns BuffIndex.None</returns>
        public BuffIndex GetBuffFromPartial(string name)
        {
            foreach (KeyValuePair<string, string[]> dictEnt in BuffAlias)
            {
                foreach (string alias in dictEnt.Value)
                {
                    if (alias.ToUpper().Equals(name.ToUpper()))
                    {
                        name = dictEnt.Key;
                    }
                }
            }

            if (Enum.TryParse(name, true, out BuffIndex buffIndex) && BuffCatalog.GetBuffDef(buffIndex) != null)
            {
                return buffIndex;
            }

            foreach (var buff in BuffCatalog.buffDefs)
            {
                if (buff.name.ToUpper().Contains(name.ToUpper()))
                {
                    return buff.buffIndex;
                }
            }
            return BuffIndex.None;
        }

        /// <summary>
        /// Returns an array of BuffIndex's when provided with a partial/invariant.
        /// </summary>
        /// <param name="name">Matches in order: (int)Index, Exact Alias, Exact Index, Partial Index, Partial Invariant</param>
        /// <returns>Returns an array with all BuffIndex's matched</returns>
        public BuffIndex[] GetBuffsFromPartial(string name)
        {
            var set = new HashSet<BuffIndex>();
            foreach (KeyValuePair<string, string[]> dictEnt in BuffAlias)
            {
                foreach (string alias in dictEnt.Value)
                {
                    if (alias.ToUpper().Equals(name.ToUpper()))
                    {
                        name = dictEnt.Key;
                        var buffIndex = BuffCatalog.FindBuffIndex(name);
                        if (buffIndex != BuffIndex.None)
                        {
                            set.Add(buffIndex);
                        }
                    }
                }
            }

            foreach (var buff in BuffCatalog.buffDefs)
            {
                if (buff.name.ToUpper().Contains(name.ToUpper()))
                {
                    set.Add(buff.buffIndex);
                }
            }
            return set.ToArray();
        }

        /// <summary>
        /// Returns a DotIndex when provided with a partial/invariant.
        /// </summary>
        /// <param name="name">Matches in order: (int)Index, Exact Alias, Exact Index, Partial Index, Partial Invariant</param>
        /// <returns>Returns the DotIndex if a match is found, or returns DotIndex.None</returns>
        public DotController.DotIndex GetDotFromPartial(string name)
        {
            foreach (KeyValuePair<string, string[]> dictEnt in DotAlias)
            {
                foreach (string alias in dictEnt.Value)
                {
                    if (alias.ToUpper().Equals(name.ToUpper()))
                    {
                        name = dictEnt.Key;
                    }
                }
            }

            if (Enum.TryParse(name, true, out DotController.DotIndex dotIndex) && DotController.GetDotDef(dotIndex) != null)
            {
                return dotIndex;
            }

            foreach (var dot in Enum.GetValues(typeof(DotController.DotIndex)).Cast<DotController.DotIndex>())
            {
                if (dot >= DotController.DotIndex.Bleed && dot < DotController.DotIndex.Count)
                {
                    var dotName = dot.ToString();
                    if (dotName.ToUpper().Contains(name.ToUpper()))
                    {
                        return dot;
                    }
                }
            }
            return DotController.DotIndex.None;
        }

        /// <summary>
        /// Returns an array of DotIndex's when provided with a partial/invariant.
        /// </summary>
        /// <param name="name">Matches in order: (int)Index, Exact Alias, Exact Index, Partial Index, Partial Invariant</param>
        /// <returns>Returns an array with all the DotIndex's matched</returns>
        public DotController.DotIndex[] GetDotsFromPartial(string name)
        {
            var set = new HashSet<DotController.DotIndex>();
            foreach (KeyValuePair<string, string[]> dictEnt in DotAlias)
            {
                foreach (string alias in dictEnt.Value)
                {
                    if (alias.ToUpper().Equals(name.ToUpper()))
                    {
                        name = dictEnt.Key;
                        if (Enum.TryParse(name, true, out DotController.DotIndex dotIndex) && DotController.GetDotDef(dotIndex) != null)
                        {
                            set.Add(dotIndex);
                        }
                    }
                }
            }

            foreach (var dot in Enum.GetValues(typeof(DotController.DotIndex)).Cast<DotController.DotIndex>())
            {
                if (dot >= DotController.DotIndex.Bleed && dot < DotController.DotIndex.Count)
                {
                    var dotName = dot.ToString();
                    if (dotName.ToUpper().Contains(name.ToUpper()))
                    {
                        set.Add(dot);
                    }
                }
            }
            return set.ToArray();
        }

        /// <summary>
        /// Returns an EquipmentIndex when provided with a partial/invariant.
        /// </summary>
        /// <param name="name">Matches in order: (int)Index, Exact Alias, Exact Index, Partial Index, Partial Invariant</param>
        /// <returns>Returns the EquiptmentIndex if a match is found, or returns EquiptmentIndex.None</returns>
        public EquipmentIndex GetEquipFromPartial(string name)
        {
            string langInvar;
            foreach (KeyValuePair<string, string[]> dictEnt in EquipAlias)
            {
                foreach (string alias in dictEnt.Value)
                {
                    if (alias.ToUpper().Equals(name.ToUpper()))
                    {
                        name = dictEnt.Key;
                    }
                }
            }

            if (Enum.TryParse(name, true, out EquipmentIndex foundEquip) && EquipmentCatalog.IsIndexValid(foundEquip))
            {
                return foundEquip;
            }

            foreach (var equip in typeof(EquipmentCatalog).GetFieldValue<EquipmentDef[]>("equipmentDefs"))
            {
                langInvar = GetLangInvar(equip.nameToken.ToUpper());
                if (equip.name.ToUpper().Contains(name.ToUpper()) || langInvar.ToUpper().Contains(name.ToUpper()) || langInvar.ToUpper().Contains(RemoveSpacesAndAlike(name.ToUpper())))
                {
                    return equip.equipmentIndex;
                }
            }
            return EquipmentIndex.None;
        }

        /// <summary>
        /// Returns an array of EquipmentIndex's when provided with a partial/invariant.
        /// </summary>
        /// <param name="name">Matches in order: (int)Index, Exact Alias, Exact Index, Partial Index, Partial Invariant</param>
        /// <returns>Returns the EquipmentIndex if a match is found, or returns EquiptmentIndex.None</returns>
        public EquipmentIndex[] GetEquipsFromPartial(string name)
        {
            var set = new HashSet<EquipmentIndex>();
            string langInvar;
            foreach (KeyValuePair<string, string[]> dictEnt in EquipAlias)
            {
                foreach (string alias in dictEnt.Value)
                {
                    if (alias.ToUpper().Equals(name.ToUpper()))
                    {
                        name = dictEnt.Key;
                        if (Enum.TryParse(name, true, out EquipmentIndex foundEquip) && EquipmentCatalog.IsIndexValid(foundEquip))
                        {
                            set.Add(foundEquip);
                        }
                    }
                }
            }


            foreach (var equip in typeof(EquipmentCatalog).GetFieldValue<EquipmentDef[]>("equipmentDefs"))
            {
                langInvar = GetLangInvar(equip.nameToken.ToUpper());
                if (equip.name.ToUpper().Contains(name.ToUpper()) || langInvar.ToUpper().Contains(name.ToUpper()) || langInvar.ToUpper().Contains(RemoveSpacesAndAlike(name.ToUpper())))
                {
                    set.Add(equip.equipmentIndex);
                }
            }
            //if (set.Count == 0) set.Add(EquipmentIndex.None);
            return set.ToArray();
        }

        /// <summary>
        /// Returns an ItemTier when provided with a partial/invariant.
        /// </summary>
        /// <param name="name">Matches in order: (int)Index, Partial Invariant</param>
        /// <returns>Returns the ItemTier if a match is found, or returns -1</returns>
        public ItemTier GetItemTierFromPartial(string name)
        {
            // Not using Enum.TryParse because we're trying to avoid ItemTier.AssignedAtRuntime
            if (int.TryParse(name, out int index))
            {
                // They are not ordered by tier value!
                foreach (var tierDef in ItemTierCatalog.allItemTierDefs)
                {
                    if ((ItemTier)index == tierDef.tier)
                    {
                        return tierDef.tier;
                    }
                }
            }

            foreach (var tierDef in ItemTierCatalog.allItemTierDefs)
            {
                if (tierDef.name.ToUpperInvariant().Contains(name.ToUpperInvariant()))
                {
                    return tierDef.tier;
                }
            }
            return (ItemTier)(-1);
        }

        /// <summary>
        /// Returns an array of ItemTier's when provided with a partial/invariant.
        /// </summary>
        /// <param name="name">Matches in order: (int)Index, Partial Invariant</param>
        /// <returns>Returns the array of ItemTier indices for any matches found</returns>
        public ItemTier[] GetItemTiersFromPartial(string name)
        {
            var set = new HashSet<ItemTier>();
            foreach (var tierDef in ItemTierCatalog.allItemTierDefs)
            {
                if (int.TryParse(name, out int index))
                {
                    if (index == (int)tierDef.tier)
                    {
                        set.Add(tierDef.tier);
                    }
                }
                else if (tierDef.name.ToUpperInvariant().Contains(name.ToUpperInvariant()))
                {
                    set.Add(tierDef.tier);
                }
            }
            return set.OrderBy(t => t).ToArray();
        }

        /// <summary>
        /// Returns an ItemIndex when provided with a partial/invariant.
        /// </summary>
        /// <param name="name">Matches in order: (int)Index, Exact Alias, Exact Index, Partial Index, Partial Invariant</param>
        /// <returns>Returns the ItemIndex if a match is found, or returns ItemIndex.None</returns>
        public ItemIndex GetItemFromPartial(string name)
        {
            string langInvar;
            foreach (KeyValuePair<string, string[]> dictEnt in ItemAlias)
            {
                foreach (string alias in dictEnt.Value)
                {
                    if (alias.ToUpper().Equals(name.ToUpper()))
                    {
                        name = dictEnt.Key;

                    }
                }
            }
            if (Enum.TryParse(name, true, out ItemIndex foundItem) && ItemCatalog.IsIndexValid(foundItem))
            {
                return foundItem;
            }

            foreach (var item in typeof(ItemCatalog).GetFieldValue<ItemDef[]>("itemDefs"))
            {
                langInvar = GetLangInvar(item.nameToken.ToUpper());
                if (item.name.ToUpper().Contains(name.ToUpper()) || langInvar.ToUpper().Contains(name.ToUpper()) || langInvar.ToUpper().Contains(RemoveSpacesAndAlike(name.ToUpper())))
                {
                    return item.itemIndex;
                }
            }
            return ItemIndex.None;
        }

        /// <summary>
        /// Returns an array of ItemIndex's when provided with a partial/invariant.
        /// </summary>
        /// <param name="name">Matches in order: (int)Index, Exact Alias, Exact Index, Partial Index, Partial Invariant</param>
        /// <returns>Returns the ItemIndex if a match is found, or returns ItemIndex.None</returns>
        public ItemIndex[] GetItemsFromPartial(string name)
        {
            var set = new HashSet<ItemIndex>();
            string langInvar;
            foreach (KeyValuePair<string, string[]> dictEnt in ItemAlias)
            {
                foreach (string alias in dictEnt.Value)
                {
                    if (alias.ToUpper().Equals(name.ToUpper()))
                    {
                        name = dictEnt.Key;
                        if (Enum.TryParse(name, true, out ItemIndex foundItem) && ItemCatalog.IsIndexValid(foundItem))
                        {
                            set.Add(foundItem);
                        }
                    }
                }
            }

            foreach (var item in typeof(ItemCatalog).GetFieldValue<ItemDef[]>("itemDefs"))
            {
                langInvar = GetLangInvar(item.nameToken.ToUpper());
                if (item.name.ToUpper().Contains(name.ToUpper()) || langInvar.ToUpper().Contains(name.ToUpper()) || langInvar.ToUpper().Contains(RemoveSpacesAndAlike(name.ToUpper())))
                {
                    set.Add(item.itemIndex);
                }
            }
            //if (set.Count == 0) set.Add(ItemIndex.None);
            return set.ToArray();
        }

        /// <summary>
        /// Returns an SkinIndex when provided with a partial/invariant.
        /// </summary>
        /// <param name="name">Matches in order: (int)Index, Exact Alias, Exact Index, Partial Index, Partial Invariant</param>
        /// <returns>Returns the SkinIndex if a match is found, or returns SkinIndex.None</returns>
        public SkinIndex GetSkinFromPartial(string name)
        {
            string langInvar;
            string nameUpperInvar = name.ToUpperInvariant();
            foreach (KeyValuePair<string, string[]> dictEnt in SkinAlias)
            {
                foreach (string alias in dictEnt.Value)
                {
                    if (alias.ToUpperInvariant().Equals(nameUpperInvar))
                    {
                        name = dictEnt.Key;
                    }
                }
            }
            if (Enum.TryParse(name, true, out SkinIndex foundSkin) && foundSkin < (SkinIndex)SkinCatalog.skinCount)
            {
                return foundSkin;
            }

            foreach (var skin in typeof(SkinCatalog).GetFieldValue<SkinDef[]>("allSkinDefs"))
            {
                langInvar = GetLangInvar(skin.nameToken.ToUpperInvariant());
                var langInvarUpper = langInvar.ToUpperInvariant();
                if (skin.name.ToUpperInvariant().Contains(nameUpperInvar) || langInvarUpper.Contains(nameUpperInvar) || langInvarUpper.Contains(RemoveSpacesAndAlike(nameUpperInvar)))
                {
                    return skin.skinIndex;
                }
            }
            return SkinIndex.None;
        }

        public DirectorCard GetDirectorCardFromPartial(string masterNameUpper)
        {
            var nameUpper = masterNameUpper.ToUpperInvariant();

            foreach (DirectorCard dc in characterSpawnCard)
            {
                var spawnCardNameUpper = dc.spawnCard.name.ToUpperInvariant();

                if (nameUpper == spawnCardNameUpper)
                {
                    return dc;
                }

                if (spawnCardNameUpper.Replace("CSC", String.Empty).Equals(nameUpper))
                {
                    return dc;
                }
            }

            var masterName = GetMasterName(masterNameUpper);
            if (masterName != null)
            {
                masterNameUpper = masterName.ToUpper();
                foreach (DirectorCard dc in characterSpawnCard)
                {
                    var spawnCardPrefabNameUpper = dc.spawnCard.prefab.name.ToUpperInvariant();

                    if (masterNameUpper == spawnCardPrefabNameUpper)
                    {
                        return dc;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Returns an InteractableSpawnCard given a partial spawncard name
        /// </summary>
        /// <param name="name">Matches a specific spawncard prior to matching a partial.</param>
        /// <returns>Returns a InteractableSpawncard or throws exception.</returns>
        public InteractableSpawnCard GetInteractableSpawnCard(string name)
        {
            foreach (InteractableSpawnCard isc in interactableSpawnCards)
            {
                if (isc.name.ToUpper().Replace("ISC", String.Empty).Equals(name.ToUpper().Replace("ISC", string.Empty)) || isc.name.ToUpper().Replace("ISC", String.Empty).Contains(name.ToUpper()))
                {
                    return isc;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns an array of InteractableSpawnCards given a partial spawncard name
        /// </summary>
        /// <param name="name">Matches a specific spawncard prior to matching a partial.</param>
        /// <returns>Returns a InteractableSpawncard or throws exception.</returns>
        public InteractableSpawnCard[] GetInteractableSpawnCards(string name)
        {
            HashSet<InteractableSpawnCard> set = new HashSet<InteractableSpawnCard>();
            foreach (InteractableSpawnCard isc in interactableSpawnCards)
            {
                if (isc.name.ToUpper().Replace("ISC", String.Empty).Equals(name.ToUpper().Replace("ISC", string.Empty)) || isc.name.ToUpper().Replace("ISC", String.Empty).Contains(name.ToUpper()))
                {
                    set.Add(isc);
                }
            }

            return set.ToArray();
        }

        /// <summary>
        /// Returns BodyName when provided with a partial/invariant.
        /// </summary>
        /// <param name="name">Matches in order: (int)Index, Exact Alias, Exact name_BODY, Exact name, Partial name, Partial Invariant</param>
        /// <returns>Returns the matched body name string or returns null.</returns>
        public string GetBodyName(string name)
        {
            string langInvar;
            foreach (KeyValuePair<string, string[]> dictEnt in BodyAlias)
            {
                foreach (string alias in dictEnt.Value)
                {
                    if (alias.ToUpper().Equals(name.ToUpper()))
                    {
                        name = dictEnt.Key;
                    }
                }
            }
            int i = 0;
            foreach (var body in BodyCatalog.allBodyPrefabBodyBodyComponents)
            {
                if (int.TryParse(name, out int iName) && i == iName || body.name.ToUpper().Equals(name.ToUpper()) || body.name.ToUpper().Replace("BODY", string.Empty).Equals(name.ToUpper()))
                {
                    return body.name;
                }
                i++;
            }
            StringBuilder s = new StringBuilder();
            foreach (var body in BodyCatalog.allBodyPrefabBodyBodyComponents)
            {
                langInvar = GetLangInvar(body.baseNameToken);
                s.AppendLine(body.name + ":" + langInvar + ":" + name.ToUpper());
                if (body.name.ToUpper().Contains(name.ToUpper()) || langInvar.ToUpper().Contains(name.ToUpper()))
                {
                    return body.name;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns MasterName when provided with a partial/invariant.
        /// </summary>
        /// <param name="name">Matches in order: (int)Index, Exact Alias, Exact name_MASTER, Exact name, Partial name, Partial Invariant</param>
        /// <returns>Returns the matched Master name string or returns null.</returns>
        public string GetMasterName(string name)
        {
            foreach (KeyValuePair<string, string[]> dictEnt in MasterAlias)
            {
                foreach (string alias in dictEnt.Value)
                {
                    if (alias.ToUpper().Equals(name.ToUpper()))
                    {
                        name = dictEnt.Key;
                    }
                }
            }
            int i = 0;
            foreach (var master in MasterCatalog.allAiMasters)
            {
                if (int.TryParse(name, out int iName) && i == iName || master.name.ToUpper().Equals(name.ToUpper()) || master.name.ToUpper().Replace("MASTER", string.Empty).Equals(name.ToUpper()))
                {
                    return master.name;
                }
                i++;
            }
            StringBuilder s = new StringBuilder();
            foreach (var master in MasterCatalog.allAiMasters)
            {

                var langInvar = GetLangInvar(master.bodyPrefab.GetComponent<CharacterBody>().baseNameToken);
                s.AppendLine(master.name + ":" + langInvar + ":" + name.ToUpper());
                if (master.name.ToUpper().Contains(name.ToUpper()) || langInvar.ToUpper().Contains(name.ToUpper()))
                {
                    return master.name;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns a special char stripped language invariant when provided with a BaseNameToke.
        /// </summary>
        /// <param name="baseToken">The BaseNameToken to query for a Language Invariant.</param>
        /// <returns>Returns the LanguageInvariant for the BaseNameToken.</returns>
        public static string GetLangInvar(string baseToken)
        {
            return RemoveSpacesAndAlike(Language.GetString(baseToken));
        }

        public static string RemoveSpacesAndAlike(string input)
        {
            return Regex.Replace(input, @"[ '-]", string.Empty);
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
