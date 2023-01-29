## DebugToolkit

This mod adds various debugging commands to the console. See below for all commands plus explanation.

[Harb](https://thunderstore.io/package/Harb/), [iDeathHD](https://thunderstore.io/package/xiaoxiao921/) and ['s](https://thunderstore.io/package/paddywan/) reimplementation of [Morris1927's](https://thunderstore.io/package/Morris1927/) [RoR2Cheats](https://thunderstore.io/package/Morris1927/RoR2Cheats/). Derived with permission. 

Also adds autocompletion for arguments and networked commands giving their information to the right people to the console.

Track update progress, get support and suggest new features over at the [DebugToolkit discord](https://discord.gg/yTfsMWP).

Some vanilla console functions you might not know:

* The console can be opened with `ctrl+alt+~`.
* `help {command}` may be used to get help for a specific command
* `find {term}` can be used to find commands with that term.
* `max_messages {nr}` changes how much scroll back the console has. We auto change this to 100 for you if it's on default.

Mods recommended for combined use:

* [KeyBindForConsole](https://thunderstore.io/package/kristiansja/KeyBindForConsole/) for easier enabling of the console. Especially useful for non-US keyboard layouts.
* [R2DSE](https://thunderstore.io/package/Harb/R2DSEssentials/) for running DT on dedicated servers.
* [MidRunArtifacts](https://thunderstore.io/package/KingEnderBrine/MidRunArtifacts/) for enabling and disabling artifacts during a run.

You may contact us at any time through [issues on GitHub](https://github.com/harbingerofme/DebugToolkit/issues/new/choose), the [dedicated discord server]((https://discord.gg/yTfsMWP) or through the [Risk of Rain 2 modding Discord](https://discord.gg/5MbXZvd) found at the top of the Thunderstore website. 

---

## Additional Contributors ##

* [DestroyedClone](https://thunderstore.io/package/DestroyedClone/) ([Github](https://github.com/DestroyedClone))
* [Rays](https://github.com/SuperRayss)

---

## COMMANDS ##

Verbiage: if an argument is encapsulated with brackets, it means it's either `(choose one)`, `{needed freeform}`, or `[optional freeform]`. The following may be used to indicate the default value: `def X`, a `*` denotes the default value cannot be entered.

* **next_stage** - Advance to the next stage: `next_stage [specific stage]`. If no stage is entered, the next stage in progression is selected.
* **force_family_event** - Forces a Family Event to happen in the next stage, take no arguments. `family_event`
* **next_boss** - Sets the teleporter boss to the specified boss. `({localised_object_name}|{DirectorCard}) [count def 1] [EliteIndex def -1/None]`
* **fixed_time** - Sets the time that has progressed in the run. Affects difficulty. `fixed_time [time]`. If no time is supplied, prints the current time to console.
* **add_portal** - Teleporter will attempt to spawn a blue, gold, null, or celestial portal: `add_portal (blue|gold|celestial|null|void|deepvoid|all)`
* **seed** - Set the seed for all next runs this session. `seed [new seed]`. Use `0` to specify the game should generate its own seed. If used without argument, it's equivalent to the vanilla `run_get_seed`.
* **kill_all** - Kills all members of a specified team. `kill_all [teamindex def 2]` Team indexes: 0=neutral,1=player,2=monster. 
* **true_kill** - Truly kill a player, ignoring revival effects `true_kill [player def *you]`
* **respawn** - Respawn a player at the map spawnpoint: `respawn [player def *you]`
* **time_scale** -  Sets the timescale of the game. 0.5 would mean everything happens at half speed. `time_scale [time_scale]`. If no argument is supplied, gives the current timescale.
* **post_sound_event** - Post a sound event to the AkSoundEngine (WWise) by its event name: `post_sound_event [eventName]`

* [All the `list_` commands support filtering](https://user-images.githubusercontent.com/72328339/213889205-2dbaab4f-3b88-481e-ba29-2a466a10ed53.png)
* **list_player** - Shows list of players with their ID
* **list_body** - List all Bodies and their language invariants.
* **list_ai** - List all Masters and their language invariants
* **list_item** - List all items and if they are in the current drop pool.
* **list_equip** - List all equipment and if they are in the current drop pool.
    

* **give_item** - Give item directly to the player's inventory: `give_item {localised_object_name} [count def 1] [player def *you]`
* **random_items** - Generate random items from the available droptables. `random_items {Count} [player def *you]`
* **give_equip** - Give equipment directly to a player's inventory: `give_equip {localised_object_name|'random'} [player def *you]`
* **give_money** - Gives the desired player/team money `give_money {amount} [(all | [player]) def all]`
* **give_lunar** - Gives the specified amount of lunar coins to the issuing player. A negative count may be specified to remove that many. `give_lunar {amount def 1}`
* **remove_item** - Removes an item from a player's inventory. `remove_item (localised_object_name | 'all') [(player | 'all') def *you]`
* **remove_equip** - Sets the equipment of a player to 'None'. `remove_equip {localised_object_name} [player def *you]`
* **create_pickup** - Creates a pickup in front of the issuing player. Pickups are items, equipment and lunar coins. Additionally 'item' or 'equip' may be specified to only search that list. `create_pickup (localized_object_name| "coin") [('item'|'equip') def *both]`

* **spawn_interactable/spawn_interactible** - Spawns an interactible in front of the player. `(spawn_interactable|spawn_interactible) {InteractableSpawnCard}`
* **spawn_ai** - Spawn an AI: `Requires 1 argument: spawn_ai {localised_objectname} [Count:1] [EliteIndex:-1/None] [Braindead:0/false(0|1)] [TeamIndex:2/Monster]`. Elite indexes: -1=None, 0=Fire,1=Overloading,2=Ice,3=Malachite,4=Celestine. Team indexes: 0=neutral,1=player,2=monster. 
* **spawn_as** - Spawn as a new character. Type body_list for a full list of characters: `spawn_as {localised_objectname} {playername}`
* **spawn_body** - Spawns a CharacterBody: `spawn_body {localised_objectname}`
* **change_team** - Change team to Neutral, Player or Monster: `change_team {teamindex}`. Team indexes: 0=neutral,1=player,2=monster. 

* **no_enemies** - Toggles enemy spawns.
* **god** - Toggles HealthComponent.TakeDamage for all players. AKA: you can't take damage.
* **lock_exp** - Prevents EXP gain for the player team.
* **noclip** - Toggles noclip. Allow you to fly and going through objects. Sprinting will double the speed.

* **dt_bind** - Bind a key to execute specific commands. `dt_bind {key} {consolecommands seperated by ;}`
* **dt_bind_delete** Remove a custom bind. `dt_bind_delete {key}`
* **dt_bind_reload** Reload the macro system from file. `dt_bind_reload` 

* **kick** - Kicks the specified Player Name/ID from the game.
* **ban** - Session bans the specified Player Name/ID from the game.

* **perm_enable** - Enable or disable the permission system.
* **perm_mod** - Change the permission level of the specified PlayerID/Username with the specified Permission Level.

* **perm_reload** - Reload the permission system, updates user and commands permissions.
* **reload_all_config** - Reload all default config files from all loaded BepinEx plugins.

### Unlocked Vanilla Commands ###

* **sv_time_transmit_interval** - How long it takes for the server to issue a time update to clients. `sv_time_transmit_interval [time]`
* **run_scene_override** - Overrides the first scene to enter in a run. `run_scene_override [stage]`
* **stage1_pod** - Whether or not to use the pod when spawning on the first stage. `stage1_pod [(0|1)]`
* **run_set_stages_cleared**  - Sets the amount of stages cleared. This does not change the current stage. `run_set_stages_cleared {stagecount}`. This obsoletes `stage_clear_count` from previous RoR2Cheats versions.
* **team_set_level** - Sets the specified team to the specified level: `team_set_level {teamindex} {level}` Team indexes: 0=neutral,1=player,2=monster. This obsoletes `give_exp` from previous RoR2Cheats versions.
* **loadout_set_skill_variant** - Sets the skill variant for the sender's user profile: `loadout_set_skill_variant {body_name} {skill_slot_index} {skill_variant_index}`. Note that this does not use the loose bodymatching from custom commands.
* **set_scene** - Removed the cheat check on this. Functions similar but not really to our `next_stage`, doesn't have our cool autocomplete features, and doesn't advance the stagecount, but can advance menus. `set_scene {scene}`

### Additional Macros ###

* **midgame** - This is the preset HopooGames uses for midgame testing. Gives all users random items, and drops you off in the bazaar. `midgame`
* **lategame** - This is the preset HopooGames uses for endgame testing. Gives all users random items, and drops you off in the bazaar. `lategame`
* **dtzoom** - Gives you 20 hooves and 200 feathers to get around the map quickly. Based on a command in the initial release of Risk of Rain 2.