using System.Collections.Generic;
using UnityEngine;
using RoR2;
using System.Text.RegularExpressions;
using System.Collections;
using System;

namespace RoR2Cheats
{
    public class Alias
    {
        private static readonly Dictionary<string, string[]> BodyAlias = new Dictionary<string, string[]>();
        private static readonly Dictionary<string, string[]> MasterAlias = new Dictionary<string, string[]>();
        private static readonly Dictionary<string, string[]> ItemAlias = new Dictionary<string, string[]>();
        private static readonly Dictionary<string, string[]> EquipAlias = new Dictionary<string, string[]>();
        private static Alias instance;

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
            BodyAlias.Add("MercBody", new string[] { "Mercenary","Ninja",  });
            BodyAlias.Add("MageBody", new string[] { "Artificer" });
            BodyAlias.Add("HANDBody", new string[] { "HAN-D" });
            BodyAlias.Add("TreebotBody", new string[] { "Treebot", "REX", "PlantBot", "Shrub", "" });

            MasterAlias.Add("DroneBackupMaster", new string[] { "DroneBackup", "BackupDrone" });
            MasterAlias.Add("DroneMissileMaster", new string[] { "DroneMissile", "MissileDrone" });
            MasterAlias.Add("LemurianBruiserMasterFire", new string[] { "LemurianBruiserFire" });
            MasterAlias.Add("LemurianBruiserMasterIce", new string[] { "LemurianBruiserIce" });
            MasterAlias.Add("LemurianBruiserMasterPoison", new string[] { "LemurianBruiserPoison", "LemurianBruiserBlight", "LemurianBruisermalechite" });
            MasterAlias.Add("MercMonsterMaster", new string[] { "MercMonster" });
        }

        public string GetEquipName(string name)
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
            if(Enum.TryParse(name, true, out EquipmentIndex foundEquip))
            {
#if DEBUG
                Debug.Log("RETURNED EXACT MATCH!");
#endif
                return foundEquip.ToString();
            }
            foreach (var equip in RoR2.EquipmentCatalog.allEquipment)
            {
                langInvar = GetLangInvar("EQUIPMENT_" + equip.ToString().ToUpper() + "_NAME");
#if DEBUG
                Debug.Log(equip.ToString() + ":" + langInvar + ":" + name.ToUpper());
#endif
                if (equip.ToString().ToUpper().Contains(name.ToUpper()) || langInvar.ToUpper().Contains(name.ToUpper()))
                {
                    return equip.ToString();
                }
            }
            return null;
        }

        public string GetItemName(string name)
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
            if (Enum.TryParse(name, true, out EquipmentIndex foundItem))
            {
#if DEBUG
                Debug.Log("RETURNED EXACT MATCH!");
#endif
                return foundItem.ToString();
            }
            foreach (var item in RoR2.ItemCatalog.allItems)
            {
                langInvar = GetLangInvar("ITEM_" + item.ToString().ToUpper() + "_NAME");
#if DEBUG
                Debug.Log(item.ToString() + ":" + langInvar + ":" + name.ToUpper());
#endif
                if (item.ToString().ToUpper().Contains(name.ToUpper()) || langInvar.ToUpper().Contains(name.ToUpper()))
                {
                    return item.ToString();
                }
            }
            return null;
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
            foreach(var body in RoR2.BodyCatalog.allBodyPrefabBodyBodyComponents)
            {
                if ((int.TryParse(name, out int iName) && i == iName) || body.name.ToUpper().Equals(name.ToUpper()) || body.name.ToUpper().Replace("BODY", string.Empty).Equals(name.ToUpper()))
                {
#if DEBUG
                    Debug.Log("MATCHED EXACT!");
#endif
                    return body.name;
                }
                i++;
            }
            foreach(var body in RoR2.BodyCatalog.allBodyPrefabBodyBodyComponents)
            {
                langInvar = GetLangInvar(body.baseNameToken);
#if DEBUG
                Debug.Log(body.name + ":" + langInvar + ":" + name.ToUpper());
#endif
                if (body.name.ToUpper().Contains(name.ToUpper()) || langInvar.ToUpper().Contains(name.ToUpper()))
                {
                    return body.name;
                }
            }
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
            foreach (var master in RoR2.MasterCatalog.allAiMasters)
            {
                if ((int.TryParse(name, out int iName) && i==iName) || master.name.ToUpper().Equals(name.ToUpper()) || master.name.ToUpper().Replace("MASTER", string.Empty).Equals(name.ToUpper()))
                {
# if DEBUG
                    Debug.Log("MATCHED EXACT!");
#endif
                    return master.name;
                }
                i++;
            }
            foreach (var master in RoR2.MasterCatalog.allAiMasters)
            {

                langInvar = GetLangInvar(master.bodyPrefab.GetComponent<CharacterBody>().baseNameToken); 
#if DEBUG
                Debug.Log(master.name + ":" + langInvar + ":" + name.ToUpper());
#endif
                if (master.name.ToUpper().Contains(name.ToUpper()) || langInvar.ToUpper().Contains(name.ToUpper()))
                {
                    return master.name;
                }
            }
            return null;
        }

        public static string GetLangInvar(string baseToken)
        {
            return Regex.Replace(Language.GetString(baseToken), @"[ '-]", string.Empty);
        }
        //public static string GetStringFromPartial<T>(string name)
        //{
        //    foreach (string eVal in Enum.GetNames(typeof(T)))
        //    {
        //        if (eVal.ToUpper().Contains(name.ToUpper()))
        //        {
        //            return eVal;
        //        }
        //    }
        //    return null;
        //}
        public static T GetEnumFromPartial<T>(string name)
        {
            var array = (T[])Enum.GetValues(typeof(T));
            if (int.TryParse(name, out int index))
            {
                return array[index];
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