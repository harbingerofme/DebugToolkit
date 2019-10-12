using System;
using System.Collections.Generic;

namespace RoR2Cheats
{
    public class Character
    {

        public string body;
        public string master;
        public List<string> aliases;

        public Character(string _body, string _master, string[] _aliases)
        {
            body = _body;
            master = _master;
            aliases = new List<string>(_aliases);
        }

        public bool IsMatch(string name)
        {

            if (body.Equals(name, StringComparison.OrdinalIgnoreCase) || master.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            foreach (var alias in aliases)
            {
                if (name.Equals(alias, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }


            return false;
        }

        public static Character GetCharacter(string name)
        {
            foreach (var character in characters)
            {
                if (character.IsMatch(name))
                    return character;
            }

            return new Character(name, name.Remove("Body") + "Master", new string[] { "" });
        }

        public static List<Character> characters = new List<Character>() {
            new Character("AssassinBody", "AssassinMaster", new string[] {"Assassin"}),
            new Character("CommandoBody", "CommandoMaster", new string[] {"Commando"}),
            new Character("HuntressBody", "HuntressMaster", new string[] {"Huntress"}),
            new Character("EngiBody", "EngiMaster", new string[] {"Engi", "Engineer"}),
            new Character("ToolbotBody", "ToolbotMaster", new string[] {"Toolbot", "MULT", "MUL-T"}),
            new Character("MercBody", "MercMaster", new string[] {"Merc", "Mercenary"}),
            new Character("MageBody", "MageMaster", new string[] {"Mage", "Artificer"}),
            new Character("BanditBody", "BanditMaster", new string[] {"Bandit"}),
            new Character("SniperBody", "SniperMaster", new string[] {"Sniper"}),
            new Character("HANDBody", "HANDMaster", new string[] {"HAND", "HAN-D"}),
            new Character("TreebotBody", "TreebotMaster", new string[] {"Treebot", "REX"}),
            new Character("LoaderBody", "LoaderMaster", new string[] {"Loader", "Load"}),

            new Character("AncientWispBody", "AncientWispMaster", new string[] { "AncientWisp" }),
            new Character("ArchWispBody", "ArchWispMaster", new string[] { "ArchWisp" }),
            new Character("BeetleGuardAllyBody", "BeetleGuardAllyMaster", new string[] { "BeetleGuardAlly"}),
            new Character("BeetleGuardBody", "BeetleGuardMaster", new string[] { "BeetleGuard"}),
            new Character("BeetleBody", "BeetleMaster", new string[] { "Beetle"}),
            new Character("BeetleQueen2Body", "BeetleQueenMaster", new string[] { "BeetleQueen"}),
            new Character("BellBody", "BellMaster", new string[] { "Bell"}),
            new Character("BisonBody", "BisonMaster", new string[] { "Bison"}),
            new Character("ClayBossBody", "ClayBossMaster", new string[] { "ClayBoss"}),
            new Character("ClayBody", "ClaymanMaster", new string[] { "Clayman"}),
            new Character("ClayBruiserBody", "ClayBruiserMaster", new string[] { "ClayBruiser"}),
            new Character("CommandoBody", "CommandoMaster", new string[] { "Commando"}),
            new Character("CommandoBody", "CommandoMonsterMaster", new string[] { "CommandoMonster"}),
            new Character("CommandoPerformanceTestBody", "CommandoMonsterMaster", new string[] { "CommandoPerformanceTest"}),
            new Character("Drone1Body", "Drone1Master", new string[] { "Drone1"}),
            new Character("Drone2Body", "Drone2Master", new string[] { "Drone2"}),
            new Character("BackupDroneBody", "DroneBackupMaster", new string[] { "DroneBackup"}),
            new Character("MissileDroneBody", "DroneMissileMaster", new string[] { "DroneMissile"}),
            new Character("ElectricWormBody", "ElectricWormMaster", new string[] { "ElectricWorm"}),
            new Character("EngiBeamTurretBody", "EngiBeamTurretMaster", new string[] { "EngiBeamTurret"}),
            new Character("EngiTurretBody", "EngiTurretMaster", new string[] { "EngiTurret"}),
            new Character("EquipmentDroneBody", "EquipmentDroneMaster", new string[] { "EquipmentDrone"}),
            new Character("FlameDroneBody", "FlameDroneMaster", new string[] { "FlameDrone"}),
            new Character("GolemBody", "GolemMaster", new string[] { "Golem"}),
            new Character("GravekeeperBody", "GravekeeperMaster", new string[] { "Gravekeeper"}),
            new Character("GreaterWispBody", "GreaterWispMaster", new string[] { "GreaterWisp"}),
            new Character("HermitCrabBody", "HermitCrabMaster", new string[] { "HermitCrab"}),
            new Character("ImpBossBody", "ImpBossMaster", new string[] { "ImpBoss"}),
            new Character("ImpBody", "ImpMaster", new string[] { "Imp"}),
            new Character("JellyfishBody", "JellyfishMaster", new string[] { "Jellyfish"}),
            new Character("LemurianBruiserBody", "LemurianBruiserMaster", new string[] { "LemurianBruiser"}),
            new Character("LemurianBruiserBody", "LemurianBruiserMasterFire", new string[] { "LemurianBruiserFire"}),
            new Character("LemurianBruiserBody", "LemurianBruiserMasterIce", new string[] { "LemurianBruiserIce"}),
            new Character("LemurianBruiserBody", "LemurianBruiserMasterPoison", new string[] { "LemurianBruiserPoison"}),
            new Character("LemurianBody", "LemurianMaster", new string[] { "Lemurian"}),
            new Character("MagmaWormBody", "MagmaWormMaster", new string[] { "MagmaWorm"}),
            new Character("MegaDroneBody", "MegaDroneMaster", new string[] { "MegaDrone"}),
            new Character("MercBody", "MercMonsterMaster", new string[] { "MercMonster"}),
            new Character("ShopkeeperBody", "ShopkeeperMaster", new string[] { "Shopkeeper"}),
            new Character("SpectatorBody", "SpectatorMaster", new string[] { "Spectator"}),
            new Character("SquidTurretBody", "SquidTurretMaster", new string[] { "SquidTurret"}),
            new Character("TitanGoldBody", "TitanGoldMaster", new string[] { "TitanGold"}),
            new Character("TitanBody", "TitanMaster", new string[] { "Titan"}),
            new Character("Turret1Body", "Turret1Master", new string[] { "Turret1"}),
            new Character("UrchinTurretBody", "UrchinTurretMaster", new string[] { "UrchinTurret"}),
            new Character("VagrantBody", "VagrantMaster", new string[] { "Vagrant"}),
            new Character("WispBody", "WispMaster", new string[] { "Wisp" })

        };
    }
}