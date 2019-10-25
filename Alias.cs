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

        public List<DirectorCard> GetDirectorCards()
        {
            return spawnCards;
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
        /// <param name="name"></param>
        /// <returns></returns>
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

        public static string GetLangInvar(string baseToken)
        {
            return Regex.Replace(Language.GetString(baseToken), @"[ '-]", string.Empty);
        }

        public static T GetEnumFromPartial<T>(string name)
        {
            var array = (T[])Enum.GetValues(typeof(T));
            if (int.TryParse(name, out int index))
            {
                return (index < array.Length) ? array[index] : default;
            }

            foreach (T num in array)
            {
                if (Enum.GetName(typeof(T),num ).ToUpper().Contains(name.ToUpper()))
                {
                    return (T)num;
                }
            }
            return default;
        }
    }
}