using System.Collections.Generic;
using UnityEngine;
using RoR2;
using System.Text.RegularExpressions;
using System.Collections;
using System;
using System.Text;

namespace RoR2Cheats
{
    public class Alias
    {
        private static readonly Dictionary<string, string[]> BodyAlias = new Dictionary<string, string[]>();
        private static readonly Dictionary<string, string[]> MasterAlias = new Dictionary<string, string[]>();
        private static readonly Dictionary<string, string[]> ItemAlias = new Dictionary<string, string[]>();
        private static readonly Dictionary<string, string[]> EquipAlias = new Dictionary<string, string[]>();
        private static Alias instance;
        private static List<DirectorCard> spawnCards = new List<DirectorCard>();

        public static Alias Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Alias();
                }

                return instance;
            }
        }

        /// <summary>
        /// Initialises the various alias lists and creates the DirectorCard cache
        /// </summary>
        private Alias()
        {
            BodyAlias.Add("ToolbotBody", new string[] { "MULT", "MUL-T", "ShoppingTrolly" });
            BodyAlias.Add("MercBody", new string[] { "Mercenary","Ninja"});
            BodyAlias.Add("MageBody", new string[] { "Artificer"});
            BodyAlias.Add("HANDBody", new string[] { "HAN-D"});
            BodyAlias.Add("TreebotBody", new string[] { "Treebot", "REX", "PlantBot", "Shrub"});

            MasterAlias.Add("DroneBackupMaster", new string[] { "DroneBackup", "BackupDrone" });
            MasterAlias.Add("DroneMissileMaster", new string[] { "DroneMissile", "MissileDrone" });
            MasterAlias.Add("LemurianBruiserMasterFire", new string[] { "LemurianBruiserFire" });
            MasterAlias.Add("LemurianBruiserMasterIce", new string[] { "LemurianBruiserIce" });
            MasterAlias.Add("LemurianBruiserMasterPoison", new string[] { "LemurianBruiserPoison", "LemurianBruiserBlight", "LemurianBruisermalechite" });
            MasterAlias.Add("MercMonsterMaster", new string[] { "MercMonster" });

            Debug.Log($"CardCount: {Resources.LoadAll<CharacterSpawnCard>("SpawnCards/CharacterSpawnCards").Length}");

            foreach (CharacterSpawnCard csc in Resources.LoadAll<CharacterSpawnCard>("SpawnCards/CharacterSpawnCards"))
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
                spawnCards.Add(dCard);
            }
        }

        /// <summary>
        /// Returns a prepared list of available DirectorCards.
        /// </summary>
        public List<DirectorCard> DirectorCards
        {
            get
            {
                return spawnCards;
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

            foreach(DirectorCard dc in spawnCards)
            {
                if (/*dc.spawnCard.name.ToUpper().Equals(name.ToUpper()) || */dc.spawnCard.name.ToUpper().Replace("csc", String.Empty).Equals(name.ToUpper()))
                {
                    return dc;
                }
            }
            name = GetMasterName(name);
            foreach (DirectorCard dc in spawnCards)
            {
                if (dc.spawnCard.prefab.name.ToUpper().Equals(name.ToUpper()))
                {
                    return dc;
                }
            }
            throw new Exception($"Card {name} not found");
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
                foreach(string alias in dictEnt.Value)
                {
                    if (alias.ToUpper().Equals(name.ToUpper()))
                    {
                        name = dictEnt.Key.ToString();
                    }
                }
            }
            int i = 0;
            foreach(var body in BodyCatalog.allBodyPrefabBodyBodyComponents)
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
                if ((int.TryParse(name, out int iName) && i==iName) || master.name.ToUpper().Equals(name.ToUpper()) || master.name.ToUpper().Replace("MASTER", string.Empty).Equals(name.ToUpper()))
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
            return Regex.Replace(Language.GetString(baseToken), @"[ '-]", string.Empty);
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
                if (Enum.GetName(typeof(TEnum),num ).ToUpper().Contains(name.ToUpper()))
                {
                    return (TEnum)num;
                }
            }
            return default;
        }
    }
}