##Debug Toolkit

This mod adds various debugging commands to the console. See below for all commands plus explanation.

Also adds autocompletion for arguments and networked commands giving their information to the right people to the console.

Some vanilla console functions you might not know:

* The console can be opened with `ctrl+alt+~`.
* `help {command}` may be used to get help
* `find {term}` can be used to find commands with that term.
* `max_messages {nr}` changes how much scroll back the console has. We auto change this to 100 for you.

[Harb](https://thunderstore.io/package/Harb/), [iDeathHD](https://thunderstore.io/package/xiaoxiao921/) and ['s](https://thunderstore.io/package/paddywan/) reimplementation of [Morris1927's](https://thunderstore.io/package/Morris1927/) [RoR2Cheats](https://thunderstore.io/package/Morris1927/RoR2Cheats/). Derived with permission. 

Mods recommended for combined use:

* [KeyBindForConsole](https://thunderstore.io/package/kristiansja/KeyBindForConsole/)
* [SimpleMacros](https://thunderstore.io/package/recursiveGecko/SimpleMacros/)
* [MacroCommands] (https://thunderstore.io/package/JackPendarvesRead/MacroCommands/)

You may contact us at any time through issues on GitHub, or through the Risk of Rain 2 modding Discord found at the top of the Thunderstore website.

---

## COMMANDS ##

Verbiage: if an argument is encapsulated with brackets, it means it's either `(choose one)`, `{needed freeform}`, or `[optional freeform]`. The following may be used to indicate the default value: `def X`, a `*` denotes the default value cannot be entered.

* **next_stage** - Advance to the next stage: `next_stage [specific stage]`. If no stage is entered, the next stage in progression is selected.
* **force_family_event** - Forces a Family Event to happen in the next stage, take no arguments. `family_event`
* ~~**next_boss** - Sets the teleporter boss to the specified boss. `({localised_object_name}|{DirectorCard}) [count def 1] [EliteIndex def -1/None]`~~ *next_boss has been disabled due to a directorcard update. To expedite testing, this feature ahs been temporarily disabled.*
* **fixed_time** - Sets the time that has progressed in the run. Affects difficulty. `fixed_time [time]`. If no time is supplied, prints the current time to console.
* **add_portal** - Teleporter will attempt to spawn a blue, gold, or celestial portal: `add_portal (blue|gold|celestial|all)`
* **seed** - Set the seed for all next runs this session. `seed [new seed]`. Use `0` to specify the game should generate its own seed. If used without argument, it's equivalent to the vanilla `run_get_seed`.
* **kill_all** - Kills all members of a specified team. `kill_all [teamindex def 2]` Team indexes: 0=neutral,1=player,2=monster. 
* **true_kill** - Truly kill a player, ignoring revival effects `true_kill [player def *you]`
* **respawn** - Respawn a player at the map spawnpoint: `respawn [player def *you]`
* **time_scale** -  Sets the timescale of the game. 0.5 would mean everything happens at half speed. `time_scale [time_scale]`. If no argument is supplied, gives the current timescale.
* **post_sound_event** - Post a sound event to the AkSoundEngine (WWise) by its event name: `post_sound_event [eventName]`

* **player_list** - Shows list of players with their ID
* **list_body** - List all Bodies and their language invariants.
* **list_ai** - List all Masters and their language invariants

* **give_item** - Give item directly to the player's inventory: `give_item {localised_object_name} [count def 1] [player def *you]`
* **give_equip** - Give equipment directly to a player's inventory: `give_equip {localised_object_name} [player def *you]`
* **give_money** - Gives the desired player/team money `give_money {amount} [(all | [player]) def all]`
* **give_lunar** - Gives the specified amount of lunar coins to the issuing player. A negative count may be specified to remove that many. `give_lunar {amount def 1}`
* **remove_item** - Removes an item from a player's inventory. `remove_item (localised_object_name | 'all') [(player | 'all') def *you]`
* **remove_equip** - Sets the equipment of a player to 'None'. `remove_equip {localised_object_name} [player def *you]`
* **create_pickup** - Creates a pickup in front of the issuing player. Pickups are items, equipment and lunar coins. Additionally 'item' or 'equip' may be specified to only search that list. `create_pickup (localized_object_name| "coin") [('item'|'equip') def *both]`

* **spawn_interactible** - Spawns an interactible in front of the player. `spawn_interactable {InteractableSpawnCard}`
* **spawn_ai** - Spawn an AI: `spawn_ai {localised_objectname} [eliteIndex def -1/None] [teamIndex def 0] [braindead def 1]`. Elite indexes: 0=Fire,1=Overloading,2=Ice,3=Malachite,4=Celestine. Team indexes: 0=neutral,1=player,2=monster. 
* **spawn_as** - Spawn as a new character. Type body_list for a full list of characters: `spawn_as {localised_objectname} {playername}`
* **spawn_body** - Spawns a CharacterBody: `spawn_body {localised_objectname}`
* **change_team** - Change team to Neutral, Player or Monster: `change_team {teamindex}`. Team indexes: 0=neutral,1=player,2=monster. 

* **no_enemies** - Toggles enemy spawns.
* **god** - Toggles HealthComponent.TakeDamage for all players. AKA: you can't take damage.
* **noclip** - Toggles noclip. Allow you to fly and going through objects. Sprinting will double the speed.

* **kick** - Kicks the specified Player Name/ID from the game.
* **ban** - Session bans the specified Player Name/ID from the game.

### Unlocked Vanilla Commands ###

* **sv_time_transmit_interval** - How long it takes for the server to issue a time update to clients. `sv_time_transmit_interval [time]`
* **run_scene_override** - Overrides the first scene to enter in a run. `run_scene_override [stage]`
* **stage1_pod** - Whether or not to use the pod when spawning on the first stage. `stage1_pod [(0|1)]`
* **run_set_stages_cleared**  - Sets the amount of stages cleared. This does not change the current stage. `run_set_stages_cleared {stagecount}`. This obsoletes `stage_clear_count` from previous RoR2Cheats versions.
* **team_set_level** - Sets the specified team to the specified level: `team_set_level {teamindex} {level}` Team indexes: 0=neutral,1=player,2=monster. This obsoletes `give_exp` from previous RoR2Cheats versions.
* **loadout_set_skill_variant** - Sets the skill variant for the sender's user profile: `loadout_set_skill_variant {body_name} {skill_slot_index} {skill_variant_index}`. Note that this does not use the loose bodymatching from custom commands.
* **set_scene** - Removed the cheat check on this. Functions similar but not really to our `next_stage`, doesn't have our cool autocomplete features, and doesn't advance the stagecount, but can advance menus. `set_scene {scene}`


---

## Changelog ##

### 3.2.0 ###

* "Dedicated Servers"
* **General**
	* Commands are now safe to run with a dedicated server as the sender. Want to know how you can issue commands duriung runtime as a server? [R2DSE](https://github.com/harbingerofme/R2DS-Essentials)
		* Some commands have not yet been implemented this way and merely do nothing, expect fixes for that in `3.2.X` updates.
		* When a player is needed for a command, dedicated servers will have to fully qualify their command.
* **Fixes**
	* `Noclip` now disables the out of bounds teleport. Go take a look at that collossus head!

### 3.1.0 ###

* "DebugToolkit"
* **General**
	* **3.1.2:** Disabled `next_boss` so that everyone can use it to update their mods.
	* **3.1.1:** Now known as DebugToolkit on Thunderstore.
	* **3.1.1:** Removed obsoleted commands.
	* **Paddywan** has left the modding community. We thank them for their contributions to this project and wish them the best of luck in future endavours.
    * **iDeathHD** has joined the team behind RoR2Cheats/DebugToolkit, their expertise has been of amazing use and we're excited to have a joint venture in this.
    * You may have noticed *MiniRPCLib* is a new dependency. This is to network cheats better over the network. Functionally nothing has changed.
    * A secret new convar is added: `debugtoolkit_debug`. Only available for those people who read the changelog. ❤️
    * Various commands have had their ingame descriptions updated to better inform what args are expected.
	* added the *"all"* overload to `add_portal`.
    * *Modders stuff:* Hooks that do not permanently need to exists have been been made to only exist for as long as they are needed.
    * *Modders stuff:* All hooks, temporary or not, have been added to the readme to help resolve mod conflicts.
* **New Commands**
	* **3.1.1:** `post_sound_event` Sounds be hard. This should help.
    * `next_boss` We've worked hard on this. We hope you find use for it. *(And with 'we', Harb means the other contributor who isn't Harb nor iDeathHD.)*
    * `give_lunar` Editing your save is an unnessecary task. This command is restricted to the issuing player to prevent grieving.
    * `remove_item` While this functionality could already be achieved with *give_item* and a negative amount, this was not obvious.
    * `remove_equip` While this functionality could already be achieved with `give_equip None, this was not obvious.
    * `create_pickup` A lot of custom item mods also need to test their descriptions. Maybe you have an on pickup hook.
    * `force_family_event` We initially tried it being able to do any family, but this proved to be hard. So instead we force an event to happen next stage.
    * `noclip` Fly freely through the map.
    * `kick` Not much to say there are better ways to resolve your issues with players. 
    * `ban`You can talk it out.
    * `spawn_interactible` Implemented with full range of interactles, not limited to types. Accepts InteractableSpawnCard partial as parameter.
* **Fixes**
    * `Spawn_as` now temporarily disables arriving in the pod to prevent not being able to get out of a pod.
    * Clients now see the output of commands with the ExecuteOnServer flag. *This change only applies to commands created by this mod.*
    * Host now sees the input of commands with the ExecuteOnServer flag. *This change applies to all commands, even that of other mods.*
    * `set_team` is now smarter in detecting which team you want.
	* `add_portal` now sets better values to get the vanilla orb messages to appear in chat.
    * Several issues with argument matching are now resolved better.
    * Special characters are now handled better (or rather, are ignored completely) when matching arguments.
	* `set_scene` is now no longer denying access to you because you don't have cheats enabled. (you do, after all.)

### 3.0.0 ###

* "Initial Release"
* **General**
    * Reworked almost every command to be more maintainable. This should have no impact on current users.
    * FOV is now outside the scope of this project and has thus been removed
* **New Features**
    * Object & Character names can now be queried using partial language invariants. For example: `give_item uku` will give one ukulele under "en".
    * Several vanilla cheats have now been unlocked.
* **Fixes**
    * No longer forcefully changes FOV settings.
    * No longer hooks stuff for disabling enemies, improving mod inter compatibility.
    * `seed` is now networked.
    * Elites now spawn with correct stats, and should be update-proofed.

### < 3.0.0 ###

See [the old package](https://thunderstore.io/package/Morris1927/)

---

## Hooks ##

This mod always hooks the following methods:

* `On.RoR2.Console.InitConVar` - We do this to 'free' the vanilla convars and change some vanilla descriptions.
* `On.RoR2.Console.RunCmd` -  We do this to log clients sending a command to the Host.
* `IL.RoR2.Console.Awake` - We do this to 'free' `run_set_stages_cleared`.
* `IL.RoR2.Networking.GameNetworkManager.CCSetScene` - We do this to 'free' `set_scene`.


This mod hooks the following methods when prompted:

* `On.RoR2.PreGameController.Awake` - We use this to change the seed if needed.
* `On.RoR2.CombatDirector.SetNextSpawnAsBoss` - We use this for `set_boss`.
* `On.RoR2.Stage.Start` - We hook this to remove the IL hook on ClassicStageInfo.
* `IL.RoR2.ClassicStageInfo.Awake` - We hook this to set a family event.
* NoClip requires 3 hooks:
	* `On.RoR2.Networking.GameNetworkManager.Disconnect` 
	* These two trampoline hooks are initialized, but are only applied on prompt:
		1. UnityEngine.NetworkManager.DisableOnServerSceneChange
		2. UnityEngine.NetworkManager.DisableOnClientSceneChange
	