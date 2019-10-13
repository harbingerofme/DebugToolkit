using System;
using System.Collections.Generic;
using RoR2;
using System.Linq;
using UnityEngine;


namespace RoR2Cheats
{
    public class Character
    {
        private static Dictionary<string, string[]> BodyAlias = new Dictionary<string, string[]>();
        private static Dictionary<string, string[]> MasterAlias = new Dictionary<string, string[]>();
        private static Character instance;

        public static Character Instance
        {
            get
            {
                if (instance == null)
                    instance = new Character();
                return instance;
            }
        }

        private Character()
        {
            BodyAlias.Add("ToolbotBody", new string[] { "MULT", "MUL-T" });
            BodyAlias.Add("MercBody", new string[] { "Mercenary" });
            BodyAlias.Add("MageBody", new string[] { "Artificer" });
            BodyAlias.Add("HANDBody", new string[] { "HAN-D" });
            BodyAlias.Add("TreebotBody", new string[] { "Treebot", "REX" });

            MasterAlias.Add("DroneBackupMaster", new string[] { "DroneBackup", "BackupDrone" });
            MasterAlias.Add("DroneMissileMaster", new string[] { "DroneMissile", "MissileDrone" });
            MasterAlias.Add("LemurianBruiserMasterFire", new string[] { "LemurianBruiserFire" });
            MasterAlias.Add("LemurianBruiserMasterIce", new string[] { "LemurianBruiserIce" });
            MasterAlias.Add("LemurianBruiserMasterPoison", new string[] { "LemurianBruiserPoison", "LemurianBruiserBlight", "LemurianBruisermalechite" });
            MasterAlias.Add("MercMonsterMaster", new string[] { "MercMonster" });
        }

        public string GetBodyName(string name)
        {
            foreach (KeyValuePair<string, string[]> dictEnt in BodyAlias)
            {
                foreach(string alias in dictEnt.Value)
                {
                    if (alias.ToUpper().Contains(name.ToUpper()))
                        name = dictEnt.Key.ToString();
                }
            }
            //if(BodyCatalog.allBodyPrefabs.Any<>)
            foreach(var body in RoR2.BodyCatalog.allBodyPrefabs)
            {
                if (body.name.ToUpper().Contains(name.ToUpper())) return body.name;
            }
            return null;
        }

        internal string GetMasterName(string name)
        {
            foreach (KeyValuePair<string, string[]> dictEnt in MasterAlias)
            {
                foreach (string alias in dictEnt.Value)
                {
                    if (alias.ToUpper().Contains(name.ToUpper()))
                        name = dictEnt.Key.ToString();
                }
            }
            //if(BodyCatalog.allBodyPrefabs.Any<>)
            foreach (var master in RoR2.MasterCatalog.allMasters)
            {
                if (master.name.ToUpper().Contains(name.ToUpper())) return master.name;
            }
            return null;
        }
    }
}