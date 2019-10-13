using System;
using System.Collections.Generic;
using RoR2;
using System.Linq;
using UnityEngine;


namespace RoR2Cheats
{
    public class Character
    {
        private static Dictionary<string, string[]> DictAlias = new Dictionary<string, string[]>();
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
            Debug.Log("Added aliases to CharacterDict");
            DictAlias.Add("ToolbotBody", new string[] { "MULT", "MUL-T" });
            DictAlias.Add("MercBody", new string[] { "Mercenary" });
            DictAlias.Add("MageBody", new string[] { "Artificer" });
            DictAlias.Add("HANDBody", new string[] { "HAN-D" });
            DictAlias.Add("TreebotBody", new string[] { "Treebot", "REX" });
        }

        public string GetMatch(string name)
        {
            foreach (KeyValuePair<string, string[]> dictEnt in DictAlias)
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
            return $"No match found for {name}";
        }
    }
}