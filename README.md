**We are moving towards a new name. This mod will be named "DebugToolkit" in the future.**

With the recent release of Lodington's RoRCheats, ambiguity exists between the mods. We are renaming to [DebugToolkit](https://thunderstore.io/package/Harb/DebugToolkit/) from version `3.2` onwards. We decided that the flexibility of our mod is more aimed towards developers who want to test extreme scenarios quickly, while Lodington's lends itself better to cheating. **This package will become deprecated once we release 3.2**. [This link will take you to that modpage once it releases.](https://thunderstore.io/package/Harb/DebugToolkit/)

# RoR2Cheats #

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

You may contact us at any time through issues on GitHub, or through the Risk of Rain 2 modding Discord found at the top of the Thunderstore website.

---

## COMMANDS ##

Verbiage: if an argument is encapsulated with brackets, it means it's either `(choose one)`, `{needed freeform}`, or `[optional freeform]`. The following may be used to indicate the default value: `def X`, a `*` denotes the default value cannot be entered.

* **next_stage** - Advance to the next stage: `next_stage [specific stage]`. If no stage is entered, the next stage in progression is selected.
* **force_family_event** - Forces a Family Event to happen in the next stage, take no arguments. `family_event`
* **next_boss** - Sets the teleporter boss to the specified boss. `({localised_object_name}|{DirectorCard}) [count def 1] [EliteIndex def -1/None]`
* **fixed_time** - Sets the time that has progressed in the run. Affects difficulty. `fixed_time [time]`. If no time is supplied, prints the current time to console.
* **add_portal** - Teleporter will attempt to spawn a blue, gold, or celestial portal: `add_portal (blue|gold|celestial)`
* **seed** - Set the seed for all next runs this session. `seed [new seed]`. Use `0` to specify the game should generate its own seed. If used without argument, it's equivalent to the vanilla `run_get_seed`.
* **kill_all** - Kills all members of a specified team. `kill_all [teamindex def 2]` Team indexes: 0=neutral,1=player,2=monster. 
* **true_kill** - Truly kill a player, ignoring revival effects `true_kill [player def *you]`
* **respawn** - Respawn a player at the map spawnpoint: `respawn [player def *you]`
* **time_scale** -  Sets the timescale of the game. 0.5 would mean everything happens at half speed. `time_scale [time_scale]`. If no argument is supplied, gives the current timescale.

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

### Commands slated for deletion

These commands will be removed once we hit **3.2**. Speak out now or be forever silenced.

* **give_exp** - Use `team_set_level` instead.
* You can now use language invariants, so the following lists have been invalidated.
    * **list_items**
    * **list_equipments**
* **stage_clear_count** - Use `run_set_stages_cleared` instead.

---

## Changelog ##

### 3.1.0 ###

* "DebugToolkit"
* **General**
    * **iDeathHD** has been added to the team behind RoR2Cheats/DebugToolkit, their expertise has been of amazing use and we're excited to have a joint venture in this.
    * You may have noticed *MiniRPCLib* is a new dependency. This is to network cheats better over the network. Functionally nothing has changed.
    * A secret new convar is added: `ror2cheats_debug`. Only available for those people who read the changelog. ❤️
    * Various commands have had their ingame descriptions updated to better inform what args are expected.
    * *Modders stuff:* Hooks that do not permanently need to exists have been been made to only exist for as long as they are needed.
    * *Modders stuff:* All hooks, temporary or not, have been added to the readme to help resolve mod conflicts.
* **New Commands**
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
    * Several issues with argument matching are now resolved better.
    * Special characters are now handled better (or rather, are ignored completely) when matching arguments.

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

* `IL.RoR2.Console.Awake` - We do this to 'free' `run_set_stages_cleared`.
* `On.RoR2.Console.InitConVar` - We do this to 'free' the vanilla convars and change some vanilla descriptions.
* `On.RoR2.Console.RunCmd` -  We do this to log clients sending a command to the Host.

This mod hooks the following methods when prompted:

* `On.RoR2.PreGameController.Awake` - We use this to change the seed if needed.
* `On.RoR2.CombatDirector.SetNextSpawnAsBoss` - We use this for `set_boss`.
* `IL.RoR2.ClassicStageInfo.Awake` - We hook this to set a family event.
* `On.RoR2.Stage.Start` - We hook this to remove the IL hook on ClassicStageInfo.