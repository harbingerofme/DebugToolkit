using RoR2;
using System;
using System.Text;
using static DebugToolkit.Log;

namespace DebugToolkit.Commands
{
    class Drones
    {
        [ConCommand(commandName = "list_drone", flags = ConVarFlags.None, helpText = Lang.LISTDRONE_HELP)]
        [AutoComplete(Lang.LISTQUERY_ARGS)]
        private static void CCListDrone(ConCommandArgs args)
        {
            var sb = new StringBuilder();
            var arg = args.Count > 0 ? args[0] : "";
            var indices = StringFinder.Instance.GetDronesFromPartial(arg);
            foreach (var index in indices)
            {
                var definition = DroneCatalog.GetDroneDef(index);
                var realName = Language.currentLanguage.GetLocalizedStringByToken(definition.nameToken);
                bool enabled = Run.instance && Run.instance.IsDroneAvailable(index);
                sb.AppendLine($"[{(int)index}]{definition.name} \"{realName}\" (enabled={enabled})");
            }
            var s = sb.Length > 0 ? sb.ToString().TrimEnd('\n') : string.Format(Lang.NOMATCH_ERROR, "drone", arg);
            Log.MessageNetworked(s, args, LogLevel.MessageClientOnly);
        }

        [ConCommand(commandName = "give_drone", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.GIVEDRONE_HELP)]
        [AutoComplete(Lang.GIVEDRONE_ARGS)]
        private static void CCGiveDrone(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            bool isDedicatedServer = args.sender == null;
            if (args.Count == 0 || (isDedicatedServer && (args.Count < 4 || args[3] == Lang.DEFAULT_VALUE)))
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.GIVEITEM_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }

            int dCount = 1;
            if (args.Count > 1 && args[1] != Lang.DEFAULT_VALUE && !TextSerialization.TryParseInvariant(args[1], out dCount))
            {
                Log.MessageNetworked(String.Format(Lang.PARSE_ERROR, "count", "int"), args, LogLevel.MessageClientOnly);
                return;
            }
            if (dCount < 0)
            {
                Log.MessageNetworked(string.Format(Lang.NEGATIVE_ARG, "count"), args, LogLevel.MessageClientOnly);
                return;
            }
            int tier = 1;
            if (args.Count > 2 && args[2] != Lang.DEFAULT_VALUE && !TextSerialization.TryParseInvariant(args[2], out tier))
            {
                Log.MessageNetworked(String.Format(Lang.PARSE_ERROR, "droneTier", "int"), args, LogLevel.MessageClientOnly);
                return;
            }
            if (tier < 0)
            {
                Log.MessageNetworked(string.Format(Lang.NEGATIVE_ARG, "droneTier"), args, LogLevel.MessageClientOnly);
                return;
            }
            
            var target = Buffs.ParseTarget(args, 3);
            if (target.failMessage != null)
            {
                Log.MessageNetworked(target.failMessage, args, LogLevel.MessageClientOnly);
                return;
            }
    
            var drone = StringFinder.Instance.GetDroneFromPartial(args[0]);
            DroneDef droneDef = DroneCatalog.GetDroneDef(drone);
            if (drone == DroneIndex.None || !droneDef)
            {
                Log.MessageNetworked(string.Format(Lang.OBJECT_NOTFOUND, "drone", args[0]), args, LogLevel.MessageClientOnly);
                return;
            }
            if (Run.instance.IsDroneExpansionLocked(drone))
            {
                Log.MessageNetworked(string.Format(Lang.EXPANSION_LOCKED, "drone", Util.GetExpansion(droneDef.requiredExpansion)), args, LogLevel.MessageClientOnly);
                return;
            }
            if (dCount > 0)
            {
                for (int i = 0; i < dCount; i++) 
                {
                    CharacterMaster newlySpawnedDrone = new MasterSummon
                    {
                        masterPrefab = droneDef.masterPrefab,
                        position = target.body.transform.position,
                        rotation = target.body.transform.rotation,
                        summonerBodyObject = target.body.gameObject,
                        ignoreTeamMemberLimit = true,
                        useAmbientLevel = true,
                        enablePrintController = true
                    }.Perform();
                    if (tier > 1)
                    {
                        newlySpawnedDrone.inventory.GiveItemPermanent(DLC3Content.Items.DroneUpgradeHidden, (tier - 1));
                    } 
                }
                var name = droneDef.name;
                Log.MessageNetworked(string.Format(Lang.GIVEDRONE, dCount, name, target.name, tier), args);
            }
            else
            {
                Log.MessageNetworked("Nothing happened", args);
            }
        
        }

 
        [ConCommand(commandName = "remove_all_drones", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.REMOVEALLDRONES_HELP)]
        [AutoComplete(Lang.REMOVEALLDRONES_ARGS)]
        private static void CCRemoveDrones(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            bool isDedicatedServer = args.sender == null;
            if (isDedicatedServer && (args.Count < 1 || args[0] == Lang.DEFAULT_VALUE))
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.REMOVEALLDRONES_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }
 
            var target = Items.ParseTarget(args, 0);
            if (target.failMessage != null)
            {
                Log.MessageNetworked(target.failMessage, args, LogLevel.MessageClientOnly);
                return;
            }
 
      
            var owner = target.inventory.GetComponent<MinionOwnership.MinionGroup.MinionGroupDestroyer>();
            if (owner == null || owner.group == null || owner.group.memberCount == 0)
            {
                Log.MessageNetworked(string.Format(Lang.REMOVEDRONES, 0, target.name), args);
                return;
            }
            int removedAmount = 0;
            for (int i = 0; i < owner.group.memberCount; i++)
            {
                var master = owner.group.members[i].GetComponent<CharacterMaster>();
                if (master.bodyPrefab.GetComponent<CharacterBody>().IsDrone)
                {
                    master.DestroyBody();
                    UnityEngine.Object.Destroy(master.gameObject, 1f);
                    removedAmount++;
                }
            }
            Log.MessageNetworked(string.Format(Lang.REMOVEDRONES, removedAmount, target.name), args);
        }

        [ConCommand(commandName = "kill_all_minions", flags = ConVarFlags.ExecuteOnServer, helpText = Lang.KILL_ALL_MINIONS_HELP)]
        [AutoComplete(Lang.REMOVEALLDRONES_ARGS)]
        private static void CCKillMinions(ConCommandArgs args)
        {
            if (!Run.instance)
            {
                Log.MessageNetworked(Lang.NOTINARUN_ERROR, args, LogLevel.MessageClientOnly);
                return;
            }
            bool isDedicatedServer = args.sender == null;
            if (isDedicatedServer && (args.Count < 1 || args[0] == Lang.DEFAULT_VALUE))
            {
                Log.MessageNetworked(Lang.INSUFFICIENT_ARGS + Lang.PLAYER_ARGS, args, LogLevel.MessageClientOnly);
                return;
            }

            var target = Items.ParseTarget(args, 0);
            if (target.failMessage != null)
            {
                Log.MessageNetworked(target.failMessage, args, LogLevel.MessageClientOnly);
                return;
            }
 
            var owner = target.inventory.GetComponent<MinionOwnership.MinionGroup.MinionGroupDestroyer>();
            if (owner == null || owner.group == null || owner.group.memberCount == 0)
            {
                Log.MessageNetworked(string.Format(Lang.Kill_MINIONS, target.name), args);
                return;
            }
            for (int i = 0; i < owner.group.memberCount; i++)
            {
                var master = owner.group.members[i].GetComponent<CharacterMaster>();
                master.TrueKill();
            }
            Log.MessageNetworked(string.Format(Lang.Kill_MINIONS, target.name), args);
        }


    }
}
