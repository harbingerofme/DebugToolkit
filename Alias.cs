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
            if(Enum.TryParse(name, true, out EquipmentIndex foundEquip) && EquipmentCatalog.IsIndexValid(foundEquip))
            {
                //catalogmod
                Log.MessageInfo("RETURNED EXACT MATCH!");
                return foundEquip.ToString();
            }

            StringBuilder s = new StringBuilder();
            foreach (var equip in EquipmentCatalog.allEquipment)
            {
                langInvar = GetLangInvar("EQUIPMENT_" + equip.ToString().ToUpper() + "_NAME");
                s.AppendLine(equip.ToString() + ":" + langInvar + ":" + name.ToUpper());
                if (equip.ToString().ToUpper().Contains(name.ToUpper()) || langInvar.ToUpper().Contains(name.ToUpper()))
                {
                    Log.MessageInfo(s);
                    return equip.ToString();
                }
            }
            Log.MessageInfo(s);
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
            if (Enum.TryParse(name, true, out ItemIndex foundItem) && ItemCatalog.IsIndexValid(foundItem))
            {
                Log.MessageInfo("RETURNED EXACT MATCH!");
                return foundItem.ToString();
            }

            StringBuilder s = new StringBuilder();
            foreach (var item in ItemCatalog.allItems)
            {
                langInvar = GetLangInvar("ITEM_" + item.ToString().ToUpper() + "_NAME");
                s.AppendLine(item.ToString() + ":" + langInvar + ":" + name.ToUpper());
                if (item.ToString().ToUpper().Contains(name.ToUpper()) || langInvar.ToUpper().Contains(name.ToUpper()))
                {
                    Log.MessageInfo(s);
                    return item.ToString();
                }
            }
            Log.MessageInfo(s);
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