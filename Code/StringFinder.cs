using RoR2;
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
        private static StringFinder instance;
        private static readonly List<DirectorCard> characterSpawnCard = new List<DirectorCard>();
        private static readonly List<InteractableSpawnCard> interactableSpawnCards = new List<InteractableSpawnCard>();
        internal static readonly string NAME_NOTFOUND = "???";

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
            GatherCSCs();
            GatherISCs();
        }

        private static void GatherCSCs()
        {
            RoR2Application.onLoad += () =>
            {
                //I imagine this would fail to get modded MultiCSC
                var CSCList = Resources.FindObjectsOfTypeAll(typeof(CharacterSpawnCard)) as CharacterSpawnCard[];

                foreach (var resourceLocator in Addressables.ResourceLocators)
                {
                    foreach (var key in resourceLocator.Keys)
                    {
                        var keyString = key.ToString();
                        if (keyString.Contains("/csc"))
                        {
                            characterSpawnCard.Add(new DirectorCard
                            {
                                spawnCard = Addressables.LoadAssetAsync<CharacterSpawnCard>(keyString).WaitForCompletion(),
                                preventOverhead = true,
                            });
                        }
                    }
                }

                var filteredList =
                CSCList.Where(spawnCard => spawnCard is CharacterSpawnCard && characterSpawnCard.All(existingCSC => existingCSC.spawnCard != spawnCard));
                foreach (var card in filteredList)
                {
                    characterSpawnCard.Add(new DirectorCard
                    {
                        spawnCard = card,
                        preventOverhead = true,
                    });
                }
            };


        }

        private void GatherISCs()
        {
            RoR2Application.onLoad += () =>
            {
                //Doing this first means only getting loaded spawn cards.
                //Which for vanilla isn't a lot of them (38)
                //But itll find any modded ones and mod-loaded ones.
                var ISCList = Resources.FindObjectsOfTypeAll(typeof(InteractableSpawnCard)) as InteractableSpawnCard[];
     
                //Unless there's some issue with WaitForFinished()
                //Have do it this way to get vanillaInteractables first then moddedInteractables, without jumbling up the order
                foreach (var resourceLocator in Addressables.ResourceLocators)
                {
                    foreach (var key in resourceLocator.Keys)
                    {
                        var keyString = key.ToString();
                        if (keyString.Contains("/isc"))
                        {
                            interactableSpawnCards.Add(Addressables.LoadAssetAsync<InteractableSpawnCard>(keyString).WaitForCompletion());
                        }
                    }
                }

                var filteredList =
                ISCList.Where(spawnCard => spawnCard is InteractableSpawnCard && interactableSpawnCards.All(existingIsc => existingIsc != spawnCard));
                interactableSpawnCards.AddRange(filteredList);
            };
        }

        private static void GatherAddressableAssets<T>(string filterKey, Action<T> onAssetLoaded)
        {
            RoR2Application.onLoadFinished += () =>
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
            if (self.interactableCategories != null)
            {
                var iscsOfCurrentStage =
                    self.interactableCategories.categories.
                    SelectMany(category => category.cards).
                    Select(directorCard => directorCard.spawnCard).
                    Where(spawnCard => spawnCard is InteractableSpawnCard && interactableSpawnCards.All(existingIsc => existingIsc.name != spawnCard.name)).
                    Cast<InteractableSpawnCard>();
                interactableSpawnCards.AddRange(iscsOfCurrentStage);
            }
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
        /// Returns an ArtifactIndex when provided with an index or partial/invariant.
        /// </summary>
        /// <param name="name">Matches either the exact (int)Index or Partial Invariant</param>
        /// <returns>Returns the ArtifactIndex if a match is found, or returns ArtifactIndex.None</returns>
        public ArtifactIndex GetArtifactFromPartial(string name)
        {
            return GetArtifactsFromPartial(name).DefaultIfEmpty(ArtifactIndex.None).First();
        }

        /// <summary>
        /// Returns an iterator of ArtifactIndex's when provided with an index or partial/invariant.
        /// </summary>
        /// <param name="name">Matches either the exact (int)Index or Partial Invariant</param>
        /// <returns>Returns an iterator with all ArtifactIndex's matched</returns>
        public IEnumerable<ArtifactIndex> GetArtifactsFromPartial(string name)
        {
            if (TextSerialization.TryParseInvariant(name, out int i))
            {
                var index = (ArtifactIndex)i;
                if (ArtifactCatalog.GetArtifactDef(index) != null)
                {
                    yield return index;
                }
                yield break;
            }
            name = name.ToUpperInvariant();
            foreach (var artifact in ArtifactCatalog.artifactDefs)
            {
                var langInvar = GetLangInvar(artifact.nameToken).ToUpper();
                if (artifact.cachedName.ToUpper().Contains(name) || langInvar.Contains(name))
                {
                    yield return artifact.artifactIndex;
                }
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
            var matches = new List<MatchSimilarity>();
            foreach (var buff in BuffCatalog.buffDefs)
            {
                if (buff.name.ToUpper().Contains(name))
                {
                    matches.Add(new MatchSimilarity
                    {
                        similarity = GetSimilarity(buff.name, name),
                        item = buff.buffIndex
                    });
                }
            }
            foreach (var match in matches.OrderByDescending(m => m.similarity))
            {
                yield return (BuffIndex)match.item;
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
            var matches = new List<MatchSimilarity>();
            foreach (var dot in Enum.GetValues(typeof(DotController.DotIndex)).Cast<DotController.DotIndex>())
            {
                if (DotController.GetDotDef(dot) != null)
                {
                    if (dot.ToString().ToUpper().Contains(name))
                    {
                        matches.Add(new MatchSimilarity
                        {
                            similarity = GetSimilarity(dot.ToString(), name),
                            item = dot
                        });
                    }
                }
            }
            foreach (var match in matches.OrderByDescending(m => m.similarity))
            {
                yield return (DotController.DotIndex)match.item;
            }
        }

        /// <summary>
        /// Returns an DifficultyIndex when provided with an index or partial/invariant.
        /// </summary>
        /// <param name="name">Matches either the exact (int)Index or Partial Invariant</param>
        /// <returns>Returns the DifficultyIndex if a match is found, or returns DifficultyIndex.Invalid</returns>
        public DifficultyIndex GetDifficultyFromPartial(string name)
        {
            return GetDifficultiesFromPartial(name).DefaultIfEmpty(DifficultyIndex.Invalid).First();
        }

        /// <summary>
        /// Returns an iterator of DifficultyIndex's when provided with an index or partial/invariant.
        /// </summary>
        /// <param name="name">Matches either the exact (int)Index or Partial Invariant</param>
        /// <returns>Returns an iterator with all DifficultyIndex's matched</returns>
        public IEnumerable<DifficultyIndex> GetDifficultiesFromPartial(string name)
        {
            //Vanilla has no DifficultyDef -> Index
            //Modded difficulties are also not stored in DifficultyCatalog usually.
            if (TextSerialization.TryParseInvariant(name, out int i))
            {
                var index = (DifficultyIndex)i;
                if (DifficultyCatalog.GetDifficultyDef(index) != null)
                {
                    yield return index;
                }
                yield break;
            }
            name = name.ToUpperInvariant();
            foreach (var dict in R2API.DifficultyAPI.difficultyDefinitions)
            {
                var langInvar = GetLangInvar(dict.Value.nameToken).ToUpper();
                if (dict.Value.nameToken.ToUpper().Contains(name) || langInvar.Contains(name))
                {
                    yield return dict.Key;
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
            var matches = new List<MatchSimilarity>();
            foreach (var equip in EquipmentCatalog.equipmentDefs)
            {
                var langInvar = GetLangInvar(equip.nameToken).ToUpper();
                if (equip.name.ToUpper().Contains(name) || langInvar.Contains(name))
                {
                    matches.Add(new MatchSimilarity
                    {
                        similarity = Math.Max(GetSimilarity(equip.name, name), GetSimilarity(langInvar, name)),
                        item = equip.equipmentIndex
                    });
                }
            }
            foreach (var match in matches.OrderByDescending(m => m.similarity))
            {
                yield return (EquipmentIndex)match.item;
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
            var matches = new List<MatchSimilarity>();
            foreach (var tierDef in allItemTierDefs)
            {
                if (tierDef.name.ToUpper().Contains(name))
                {
                    matches.Add(new MatchSimilarity
                    {
                        similarity = GetSimilarity(tierDef.name, name),
                        item = tierDef.tier
                    });
                }
            }
            foreach (var match in matches.OrderByDescending(m => m.similarity))
            {
                yield return (ItemTier)match.item;
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
        /// Returns an DroneIndex when provided with an index or partial/invariant.
        /// </summary>
        /// <param name="name">Matches either the exact (int)Index or Partial Invariant</param>
        /// <returns>Returns the DroneIndex if a match is found, or returns DroneIndex.None</returns>
        public DroneIndex GetDroneFromPartial(string name)
        {
            return GetDronesFromPartial(name).DefaultIfEmpty(DroneIndex.None).First();
        }

        /// <summary>
        /// Returns an PickupIndex when provided with an index or partial/invariant.
        /// </summary>
        /// <param name="name">Matches either the exact (int)Index or Partial Invariant</param>
        /// <returns>Returns the PickupIndex if a match is found, or returns PickupIndex.none</returns>
        public PickupIndex GetPickupFromPartial(string name)
        {
            return GetPickupsFromPartial(name).DefaultIfEmpty(PickupIndex.none).First();
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
            var matches = new List<MatchSimilarity>();
            foreach (var item in ItemCatalog.allItemDefs)
            {
                var langInvar = GetLangInvar(item.nameToken).ToUpper();
                if (item.name.ToUpper().Contains(name) || langInvar.Contains(name))
                {
                    matches.Add(new MatchSimilarity
                    {
                        similarity = Math.Max(GetSimilarity(item.name, name), GetSimilarity(langInvar, name)),
                        item = item.itemIndex
                    });
                }
            }
            foreach (var match in matches.OrderByDescending(m => m.similarity))
            {
                yield return (ItemIndex)match.item;
            }
        }


        /// <summary>
        /// Returns an iterator of DroneIndex's when provided with an index or partial/invariant.
        /// </summary>
        /// <param name="name">Matches either the exact (int)Index or Partial Invariant</param>
        /// <returns>Returns an iterator with all DroneIndex's matched</returns>
        public IEnumerable<DroneIndex> GetDronesFromPartial(string name)
        {
            if (TextSerialization.TryParseInvariant(name, out int i))
            {
                var index = (DroneIndex)i;
                //if (DroneCatalog.IsIndexValid(index))
                if (index < (DroneIndex)DroneCatalog.droneCount)
                {
                    yield return index;
                }
                yield break;
            }
            name = name.ToUpperInvariant();
            var matches = new List<MatchSimilarity>();
            foreach (var drone in DroneCatalog.allDroneDefs)
            {
                var langInvar = GetLangInvar(drone.nameToken).ToUpper();
                if (drone.name.ToUpper().Contains(name) || langInvar.Contains(name))
                {
                    matches.Add(new MatchSimilarity
                    {
                        similarity = Math.Max(GetSimilarity(drone.name, name), GetSimilarity(langInvar, name)),
                        item = drone.droneIndex
                    });
                }
            }
            foreach (var match in matches.OrderByDescending(m => m.similarity))
            {
                yield return (DroneIndex)match.item;
            }
        }


        /// <summary>
        /// Returns an iterator of PickupIndex's when provided with an index or partial/invariant.
        /// </summary>
        /// <param name="name">Matches either the exact (int)Index or Partial Invariant</param>
        /// <returns>Returns an iterator with all PickupIndex's matched</returns>
        public IEnumerable<PickupIndex> GetPickupsFromPartial(string name)
        {
            if (TextSerialization.TryParseInvariant(name, out int i))
            {
                var index = i;
                if (index < PickupCatalog.pickupCount)
                {
                    yield return PickupIndex.none;
                }
                yield break;
            }
            name = name.ToUpperInvariant();
            var matches = new List<MatchSimilarity>();
            foreach (var pickup in PickupCatalog.allPickups)
            {
                var langInvar = GetLangInvar(pickup.nameToken).ToUpper();
                if (pickup.internalName.ToUpper().Contains(name) || langInvar.Contains(name))
                {
                    //yield return pickup.pickupIndex;
                    matches.Add(new MatchSimilarity
                    {
                        similarity = Math.Max(GetSimilarity(pickup.internalName, name), GetSimilarity(langInvar, name)),
                        item = pickup.pickupIndex
                    });
                }
            }
            foreach (var match in matches.OrderByDescending(m => m.similarity))
            {
                yield return (PickupIndex)match.item;
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
            var matches = new List<MatchSimilarity>();
            foreach (var user in NetworkUser.readOnlyInstancesList)
            {
                if (user.userName.ToUpper().Contains(name))
                {
                    matches.Add(new MatchSimilarity
                    {
                        similarity = GetSimilarity(user.userName, name),
                        item = user
                    });
                }
            }
            foreach (var match in matches.OrderByDescending(m => m.similarity))
            {
                yield return (NetworkUser)match.item;
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
                if (index == TeamIndex.None || TeamCatalog.GetTeamDef(index) != null)
                {
                    yield return index;
                }
                yield break;
            }
            name = name.ToUpperInvariant();
            var matches = new List<MatchSimilarity>();
            foreach (var team in Enum.GetValues(typeof(TeamIndex)).Cast<TeamIndex>().OrderBy(t => t))
            {
                if (team == TeamIndex.None || TeamCatalog.GetTeamDef(team) != null)
                {
                    if (team.ToString().ToUpper().Contains(name))
                    {
                        matches.Add(new MatchSimilarity
                        {
                            similarity = GetSimilarity(team.ToString(), name),
                            item = team
                        });
                    }
                }
            }
            foreach (var match in matches.OrderByDescending(m => m.similarity))
            {
                yield return (TeamIndex)match.item;
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
            var matches = new List<MatchSimilarity>();
            if ("NONE".Contains(name))
            {
                matches.Add(new MatchSimilarity
                {
                    similarity = GetSimilarity("None", name),
                    item = EliteIndex.None
                });
            }
            foreach (var elite in EliteCatalog.eliteDefs)
            {
                var langInvar = GetLangInvar(elite.modifierToken).ToUpper();
                if (elite.name.ToUpper().Contains(name) || langInvar.Contains(name))
                {
                    matches.Add(new MatchSimilarity
                    {
                        similarity = Math.Max(GetSimilarity(elite.name, name), GetSimilarity(langInvar, name)),
                        item = elite.eliteIndex
                    });
                }
            }
            foreach (var match in matches.OrderByDescending(m => m.similarity))
            {
                yield return (EliteIndex)match.item;
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
            var matches = new List<MatchSimilarity>();
            foreach (var dc in characterSpawnCard)
            {
                if (dc.spawnCard.name.ToUpper().Contains(name))
                {
                    matches.Add(new MatchSimilarity
                    {
                        similarity = GetSimilarity(dc.spawnCard.name, name),
                        item = dc
                    });
                }
            }
            foreach (var match in matches.OrderByDescending(m => m.similarity))
            {
                yield return (DirectorCard)match.item;
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
            var matches = new List<MatchSimilarity>();
            foreach (var isc in interactableSpawnCards)
            {
                var langInvar = GetLangInvar(GetInteractableName(isc.prefab)).ToUpper();
                if (isc.name.ToUpper().Contains(name) || langInvar.Contains(name))
                {
                    matches.Add(new MatchSimilarity
                    {
                        similarity = Math.Max(GetSimilarity(isc.name, name), GetSimilarity(langInvar, name)),
                        item = isc
                    });
                }
            }
            foreach (var match in matches.OrderByDescending(m => m.similarity))
            {
                yield return (InteractableSpawnCard)match.item;
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
            var matches = new List<MatchSimilarity>();
            foreach (var body in BodyCatalog.allBodyPrefabBodyBodyComponents)
            {
                var langInvar = GetLangInvar(body.baseNameToken).ToUpper();
                if (body.name.ToUpper().Contains(name) || langInvar.Contains(name))
                {
                    matches.Add(new MatchSimilarity
                    {
                        similarity = Math.Max(GetSimilarity(body.name, name), GetSimilarity(langInvar, name)),
                        item = body.bodyIndex
                    });
                }
            }
            foreach (var match in matches.OrderByDescending(m => m.similarity))
            {
                yield return (BodyIndex)match.item;
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
            var matches = new List<MatchSimilarity>();
            foreach (var ai in MasterCatalog.allAiMasters)
            {
                var langInvar = GetLangInvar(GetMasterName(ai)).ToUpper();
                if (ai.name.ToUpper().Contains(name) || langInvar.Contains(name))
                {
                    matches.Add(new MatchSimilarity
                    {
                        similarity = Math.Max(GetSimilarity(ai.name, name), GetSimilarity(langInvar, name)),
                        item = ai.masterIndex
                    });
                }
            }
            foreach (var match in matches.OrderByDescending(m => m.similarity))
            {
                yield return (MasterCatalog.MasterIndex)match.item;
            }
        }

        /// <summary>
        /// Returns a SurvivorIndex when provided with an index or partial/invariant.
        /// </summary>
        /// <param name="name">Matches either the exact (int)Index or Partial Invariant</param>
        /// <returns>Returns the SurvivorIndex if a match is found, or returns SurvivorIndex.None</returns>
        public SurvivorIndex GetSurvivorFromPartial(string name)
        {
            return GetSurvivorsFromPartial(name).DefaultIfEmpty(SurvivorIndex.None).First();
        }

        /// <summary>
        /// Returns an iterator of SurvivorIndex's when provided with an index or partial/invariant.
        /// </summary>
        /// <param name="name">Matches either the exact (int)Index or Partial Invariant</param>
        /// <returns>Returns an iterator with all SurvivorIndex's matched</returns>
        public IEnumerable<SurvivorIndex> GetSurvivorsFromPartial(string name)
        {
            if (TextSerialization.TryParseInvariant(name, out int i))
            {
                var index = (SurvivorIndex)i;
                if (SurvivorCatalog.GetSurvivorDef(index) != null)
                {
                    yield return index;
                }
                yield break;
            }
            name = name.ToUpperInvariant();
            var matches = new List<MatchSimilarity>();
            foreach (var survivor in SurvivorCatalog.allSurvivorDefs)
            {
                var langInvar = GetLangInvar(survivor.displayNameToken).ToUpper();
                if (survivor.cachedName.ToUpper().Contains(name) || langInvar.Contains(name))
                {
                    matches.Add(new MatchSimilarity
                    {
                        similarity = Math.Max(GetSimilarity(survivor.cachedName, name), GetSimilarity(langInvar, name)),
                        item = survivor.survivorIndex
                    });
                }
            }
            foreach (var match in matches.OrderByDescending(m => m.similarity))
            {
                yield return (SurvivorIndex)match.item;
            }
        }

        /// <summary>
        /// Returns a SceneIndex when provided with an index or partial/invariant.
        /// </summary>
        /// <param name="name">Matches either the exact (int)Index or Partial Invariant</param>
        /// <returns>Returns the SceneIndex if a match is found, or returns SceneIndex.Invalid</returns>
        public SceneIndex GetSceneFromPartial(string name, bool includeOffline)
        {
            return GetScenesFromPartial(name, includeOffline).DefaultIfEmpty(SceneIndex.Invalid).FirstOrDefault();
        }

        /// <summary>
        /// Returns an iterator of SceneIndex's when provided with an index or partial/invariant.
        /// </summary>
        /// <param name="name">Matches either the exact (int)Index or Partial Invariant</param>
        /// <returns>Returns an iterator with all SceneIndex's matched</returns>
        public IEnumerable<SceneIndex> GetScenesFromPartial(string name, bool includeOffline)
        {
            if (TextSerialization.TryParseInvariant(name, out int i))
            {
                var index = (SceneIndex)i;
                var scene = SceneCatalog.GetSceneDef(index);
                if (scene != null && (includeOffline || !scene.isOfflineScene))
                {
                    yield return index;
                }
                yield break;
            }
            name = name.ToUpperInvariant();
            var matches = new List<MatchSimilarity>();
            foreach (var scene in SceneCatalog.allSceneDefs)
            {
                var langInvar = GetLangInvar(scene.nameToken).ToUpper();
                if ((scene.cachedName.ToUpper().Contains(name) || langInvar.Contains(name)) && (includeOffline || !scene.isOfflineScene))
                {
                    matches.Add(new MatchSimilarity
                    {
                        similarity = Math.Max(GetSimilarity(scene.cachedName, name), GetSimilarity(langInvar, name)),
                        item = scene.sceneDefIndex
                    });
                }
            }
            foreach (var match in matches.OrderByDescending(m => m.similarity))
            {
                yield return (SceneIndex)match.item;
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
            return Regex.Replace(input, @"[ '(),-]|\{\d+\}", string.Empty);
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
            var body = master.bodyPrefab?.GetComponent<CharacterBody>();
            if (body != null)
            {
                return body.GetDisplayName();
            }
            return NAME_NOTFOUND;
        }

        private struct MatchSimilarity
        {
            public int similarity;
            public object item;
        }

        private static int GetSimilarity(string s, string partial)
        {
            if (string.IsNullOrEmpty(partial) || !s.ToUpper().Contains(partial))
            {
                return int.MinValue;
            }
            var offset = s.StartsWith(partial, StringComparison.InvariantCultureIgnoreCase) ? 1000 : 0;
            return offset + partial.Length - s.Length;
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
