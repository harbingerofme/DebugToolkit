using System.Collections.Generic;
using UnityEngine;
using RoR2;
using System.Text.RegularExpressions;
using System.Collections;
using System;
using System.Text;
using System.Linq;

namespace RoR2Cheats
{
    public class Alias
    {
        private static readonly Dictionary<string, string[]> BodyAlias = new Dictionary<string, string[]>();
        private static readonly Dictionary<string, string[]> MasterAlias = new Dictionary<string, string[]>();
        private static readonly Dictionary<string, string[]> ItemAlias = new Dictionary<string, string[]>();
        private static readonly Dictionary<string, string[]> EquipAlias = new Dictionary<string, string[]>();
        private static Alias instance;
        private static List<DirectorCard> characterSpawnCard = new List<DirectorCard>();
        private static List<InteractableSpawnCard> interactableSpawnCards = new List<InteractableSpawnCard>();

        public static Alias Instance
        {
            get => instance ?? (instance = new Alias());
        }

        public static void EnsureInstance()
        {
            var _ = instance;
        }

        /// <summary>
        /// Initialises the various alias lists and creates the SpawnCard cache's
        /// </summary>
        private Alias()
        {
            BodyAlias.Add("ToolbotBody", new string[] { "MULT", "MUL-T", "ShoppingTrolly" });
            BodyAlias.Add("MercBody", new string[] { "Mercenary", "Ninja" });
            BodyAlias.Add("MageBody", new string[] { "Artificer", "Arti" });
            BodyAlias.Add("EngiBody", new string[] { "Engineer" });
            BodyAlias.Add("HANDBody", new string[] { "HAN-D" });
            BodyAlias.Add("TreebotBody", new string[] { "Treebot", "REX", "PlantBot", "Shrub" });
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


            var allCSC = Resources.LoadAll<CharacterSpawnCard>("SpawnCards/CharacterSpawnCards");
            Log.MessageInfo($"Loading all CSC's: {allCSC.Length}");
            foreach (CharacterSpawnCard csc in allCSC)
            {
                var dCard = new DirectorCard
                {
                    spawnCard = csc,
                    cost = 600,
                    allowAmbushSpawn = true,
                    forbiddenUnlockable = "",
                    minimumStageCompletions = 0,
                    preventOverhead = true,
                    spawnDistance = DirectorCore.MonsterSpawnDistance.Standard,
                };
                characterSpawnCard.Add(dCard);
            }
            var allISC = Resources.LoadAll<InteractableSpawnCard>("SpawnCards/InteractableSpawnCard");
            Log.MessageInfo($"Loading all ISC's: {allISC.Length}");
            interactableSpawnCards = allISC.OfType<InteractableSpawnCard>().ToList();
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

        //public TEnum GetIndexFromPartial<TEnum>(string name)
        //{
        //    string langInvar;
        //    Dictionary<string, string[]> aliasList;
        //    object catalog;
        //    if (typeof(TEnum) == typeof(ItemIndex))
        //    {
        //        aliasList = ItemAlias;
        //    }
        //    else if (typeof(TEnum) == typeof(EquipmentIndex))
        //    {
        //        aliasList = EquipAlias;
        //    }
        //    else
        //    {
        //        Log.Message("Invalid type");
        //        throw new Exception("Invalid Type");
        //    }

        //    foreach (KeyValuePair<string, string[]> dictEnt in aliasList)
        //    {
        //        foreach (string alias in dictEnt.Value)
        //        {
        //            if (alias.ToUpper().Equals(name.ToUpper()))
        //            {
        //                name = dictEnt.Key.ToString();
        //            }
        //        }
        //    }
        //    TEnum foundObject;
        //    if (Enum.TryParse(name, true, out foundObject) && EquipmentCatalog.IsIndexValid(foundObject))
        //    {
        //        //catalogmod
        //        Log.MessageInfo("RETURNED EXACT MATCH!");
        //        return foundObject;
        //    }
        //    return default;
        //}

        //public bool TryParse<T>(string name, out T result, Predicate<T> isValid) where T : struct
        //{
        //    if (Enum.TryParse(name, out result) && isValid(result))
        //        return true;
        //    return false;
        //}

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
                        name = dictEnt.Key.ToString();
                    }
                }
            }

            if (Enum.TryParse(name, true, out EquipmentIndex foundEquip) && EquipmentCatalog.IsIndexValid(foundEquip))
            {
                //catalogmod
                Log.MessageInfo("RETURNED EXACT MATCH!");
                return foundEquip;
            }

            StringBuilder s = new StringBuilder();
            foreach (var equip in EquipmentCatalog.allEquipment)
            {
                langInvar = GetLangInvar("EQUIPMENT_" + equip.ToString().ToUpper() + "_NAME");
                s.AppendLine(equip.ToString() + ":" + langInvar + ":" + name.ToUpper());
                if (equip.ToString().ToUpper().Contains(name.ToUpper()) || langInvar.ToUpper().Contains(name.ToUpper()))
                {
                    Log.MessageInfo(s);
                    return equip;
                }
            }
            Log.MessageInfo(s);
            return EquipmentIndex.None;
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
                        name = dictEnt.Key.ToString();

                    }
                }
            }
            if (Enum.TryParse(name, true, out ItemIndex foundItem) && ItemCatalog.IsIndexValid(foundItem))
            {
                Log.MessageInfo("RETURNED EXACT MATCH!");
                return foundItem;
            }

            StringBuilder s = new StringBuilder();
            foreach (var item in ItemCatalog.allItems)
            {
                langInvar = GetLangInvar("ITEM_" + item.ToString().ToUpper() + "_NAME");
                s.AppendLine(item.ToString() + ":" + langInvar + ":" + name.ToUpper());
                if (item.ToString().ToUpper().Contains(name.ToUpper()) || langInvar.ToUpper().Contains(name.ToUpper()))
                {
                    Log.MessageInfo(s);
                    return item;
                }
            }
            Log.MessageInfo(s);
            return ItemIndex.None;
        }

        /// <summary>
        /// This is probably horrible and going to break.
        /// </summary>
        /// <param name="name">The partial name to query, priority given to exact csc match, fails over to GetMasterName</param>
        /// <returns>The matched DirectorCard or throws exception.</returns>
        public DirectorCard GetDirectorCardFromPartial(string name)
        {

            foreach (DirectorCard dc in characterSpawnCard)
            {
                if (dc.spawnCard.name.ToUpper().Replace("CSC", String.Empty).Equals(name.ToUpper()))
                {
                    return dc;
                }
            }
            name = GetMasterName(name).ToUpper();//.Replace("MASTER", string.Empty)
            foreach (DirectorCard dc in characterSpawnCard)
            {
                if (dc.spawnCard.prefab.name.ToUpper().Equals(name))
                {
                    return dc;
                }
            }
            throw new Exception($"DirectorCard {name} not found. ");
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
                if (isc.name.ToUpper().Replace("ISC", String.Empty).Equals(name.ToUpper().Replace("ISC", string.Empty)) || isc.name.ToUpper().Replace("isc", String.Empty).Contains(name.ToUpper()))
                {
                    return isc;
                }
            }
            throw new Exception($"InteractableSpawnCard {name} not found.");
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
                        name = dictEnt.Key.ToString();
                    }
                }
            }
            int i = 0;
            foreach (var body in BodyCatalog.allBodyPrefabBodyBodyComponents)
            {
                if ((int.TryParse(name, out int iName) && i == iName) || body.name.ToUpper().Equals(name.ToUpper()) || body.name.ToUpper().Replace("BODY", string.Empty).Equals(name.ToUpper()))
                {
                    Log.MessageInfo("MATCHED EXACT!");
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
                    Log.MessageInfo(s);
                    return body.name;
                }
            }
            Log.MessageInfo(s);
            return null;
        }

        /// <summary>
        /// Returns MasterName when provided with a partial/invariant.
        /// </summary>
        /// <param name="name">Matches in order: (int)Index, Exact Alias, Exact name_MASTER, Exact name, Partial name, Partial Invariant</param>
        /// <returns>Returns the matched Master name string or returns null.</returns>
        public string GetMasterName(string name)
        {
            string langInvar;
            foreach (KeyValuePair<string, string[]> dictEnt in MasterAlias)
            {
                foreach (string alias in dictEnt.Value)
                {
                    if (alias.ToUpper().Equals(name.ToUpper()))
                    {
                        name = dictEnt.Key.ToString();
                    }
                }
            }
            int i = 0;
            foreach (var master in MasterCatalog.allAiMasters)
            {
                if ((int.TryParse(name, out int iName) && i == iName) || master.name.ToUpper().Equals(name.ToUpper()) || master.name.ToUpper().Replace("MASTER", string.Empty).Equals(name.ToUpper()))
                {
                    Log.MessageInfo("MATCHED EXACT!");
                    return master.name;
                }
                i++;
            }
            StringBuilder s = new StringBuilder();
            foreach (var master in MasterCatalog.allAiMasters)
            {

                langInvar = GetLangInvar(master.bodyPrefab.GetComponent<CharacterBody>().baseNameToken);
                s.AppendLine(master.name + ":" + langInvar + ":" + name.ToUpper());
                if (master.name.ToUpper().Contains(name.ToUpper()) || langInvar.ToUpper().Contains(name.ToUpper()))
                {
                    Log.MessageInfo(s);
                    return master.name;
                }
            }
            Log.MessageInfo(s);
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
            var array = (TEnum[])Enum.GetValues(typeof(TEnum));
            if (int.TryParse(name, out int index))
            {
                return (index < array.Length) ? array[index] : default;
            }

            foreach (TEnum num in array)
            {
                if (Enum.GetName(typeof(TEnum), num).ToUpper().Contains(name.ToUpper()))
                {
                    return (TEnum)num;
                }
            }
            return default;
        }
    }
}