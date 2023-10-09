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

You may contact us at any time through [issues on GitHub](https://github.com/harbingerofme/DebugToolkit/issues/new/choose), the [dedicated discord server](https://discord.gg/yTfsMWP) or through the [Risk of Rain 2 modding Discord](https://discord.gg/5MbXZvd) found at the top of the Thunderstore website. 

---

## Additional Contributors ##

* [DestroyedClone](https://thunderstore.io/package/DestroyedClone/) ([Github](https://github.com/DestroyedClone))
* [Rays](https://github.com/SuperRayss)
* [HeyImNoop](https://thunderstore.io/package/Heyimnoob/)
* [SChinchi](https://github.com/SChinchi)

---

## COMMANDS ##

Verbiage:
- The brackets encapsulating arguments mean `{needed}`, `(choose one)`, and `[optional]`.
- If an optional argument has a default value, it will be indicated with `:`. Any preceeding optional arguments from the one entered become necessary, but empty double quotes can be used as a placeholder for the default value. If an optional argument is required from a dedicated server, it will be preceeded by `*`.
- Player, body, AI, item, equipment, team, elite, interactable, and director card values can be declared by either their ID, or their string name. The latter can be written in freeform and it will be matched to the first result that contains the string. See the related `list_` commands for options and which result would take precedence if there are multiple matches.

Commands:

* **next_stage** - Advance to the next stage. `next_stage [specific_stage]`. If no stage is entered, the next stage in progression is selected.
* **force_family_event** - Forces a Family Event to happen in the next stage, takes no arguments. `force_family_event`
* **next_boss** - Sets the next teleporter/simulacrum boss to the specified boss. Get a list of potential boss with `list_directorcards`. `next_boss {director_card} [count:1] [elite:None]`
* **fixed_time** - Sets the time that has progressed in the run. Affects difficulty. `fixed_time [time]`. If no time is supplied, prints the current time to console.
* **next_wave** - Advance to the next Simulacrum wave. `next_wave`
* **force_wave** - Set the next wave prefab. `force_wave [wave_prefab]`. If no input is supplied, prints all available options and clears any previous selection.
* **set_run_waves_cleared** - Set the Simulacrum waves cleared. Must be positive. `set_run_waves_cleared {wave}`
* **add_portal** - Teleporter will attempt to spawn after the teleporter completion. `add_portal {portal ('blue'|'gold'|'celestial'|'null'|'void'|'deepvoid'|'all')}`. The `null` portal doesn't require a teleporter and will spawn in front of the player.
* **seed** - Set the seed for all next runs this session. `seed [new_seed]`. Use `0` to specify the game should generate its own seed. If used without argument, it's equivalent to the vanilla `run_get_seed`.
* **kill_all** - Kills all members of a specified team. `kill_all [team:Monster]`.
* **true_kill** - Truly kill a player, ignoring revival effects. `true_kill *[player:<self>]`
* **respawn** - Respawn a player at the map spawnpoint. `respawn *[player:<self>]`
* **teleport_on_cursor** -  Teleport you to where your cursor is currently aiming at. `teleport_on_cursor`
* **time_scale** -  Sets the timescale of the game. 0.5 would mean everything happens at half speed. `time_scale [time_scale]`. If no argument is supplied, gives the current timescale.
* **post_sound_event** - Post a sound event to the AkSoundEngine (WWise) by its event name: `post_sound_event {event_name}`

List Commands:

* [All the `list_` commands support filtering](https://user-images.githubusercontent.com/72328339/213889205-2dbaab4f-3b88-481e-ba29-2a466a10ed53.png)
* **list_player** - List all Players and their ID.
* **list_body** - List all Bodies and their language invariants.
* **list_ai** - List all Masters and their language invariants.
* **list_elite** - List all Elites and their language invariants.
* **list_team** - List all Teams and their language invariants.
* **list_buff** - List all Buffs and if they are stackable.
* **list_dot** - List all DoT effects.
* **list_itemtier** - List all Item Tiers.
* **list_item** - List all Items, their language invariants, and if they are in the current drop pool.
* **list_equip** - List all Equipment, their language invariants, and if they are in the current drop pool.
* **list_interactables** List all Interactables.
* **list_directorcards** List all Director Cards. Mainly used for the `next_boss` command.
* **list_skins** List all Body Skins and the language invariant of the current one in use.

Dump Commands:

* **dump_buffs** - List the buffs/debuffs of all spawned bodies.
* **dump_inventories** - List the inventory items and equipment of all spawned bodies.
* **dump_state** - List the current stats, entity state, and skill cooldown of a specified body. `dump_state *[target (player|'pinged'):<self>]`
* **dump_stats** - List the base stats of a specific body.

Buff Commands:

* **give_buff** - Gives a buff to a character. Duration of 0 means permanent: `give_buff {buff} [count:1] [duration:0] *[target (player|'pinged'):<self>]`
* **give_dot** - Gives a DoT stack to a character: `give_dot {dot} [count:1] *[target (player|'pinged'):<self>] *[attacker (player|'pinged'):<self>]`
* **remove_buff** - Removes a buff from a character. Timed buffs prioritise the longest expiration stack: `remove_buff {buff} [count:1] [timed (0|1):0/false] *[target (player|'pinged'):<self>]`
* **remove_buff_stacks** - Resets a buff for a character: `remove_buff_stacks {buff} [timed (0|1):0/false] *[target (player|'pinged'):<self>]`
* **remove_all_buffs** - Resets all buffs for a character: `remove_all_buffs [timed (0|1):0/false] *[target (player|'pinged'):<self>]`
* **remove_dot** - Removes a DoT stack with the longest expiration from a character: `remove_dot {dot} [count:1] *[target (player|'pinged'):<self>]`
* **remove_dot_stacks** - Removes all stacks of a DoT effect from a character: `remove_dot_stacks {dot} *[target (player|'pinged'):<self>]`
* **remove_all_dots** - Removes all DoT effects from a character: `remove_all_dot *[target (player|'pinged'):<self>]`

***Note:*** _The game does not protect against negative buff stacks, which can lead to unintended behaviors. Gaining a timed buff gives a permanent stack which is taken away upon expiration._ `give_buff bleed 5 10; remove_buff_stacks bleed` _will lead to -5 bleed stacks when the timed buff expires. Having a DoT active is also similar to a timed buff._

Item Commands:

* **give_item** - Give an item directly to a character's inventory. A negative amount is an alias for `remove_item`: `give_item {item} [count:1] *[target (player|'pinged'):<self>]`
* **random_items** - Generate random items from the available item tiers. The tier argument must be encapsulated by double quotes if there is a comma. `random_items {count} [tiers ('all'|Any comma-separated tier names):'all'] *[target (player|'pinged'):<self>]`
* **give_equip** - Give an equipment directly to a character's inventory: `give_equip {(equip|'random')} *[target (player|'pinged'):<self>]`
* **give_money** - Gives the desired player/team money `give_money {amount} [target ('all'|player):'all']`
* **give_lunar** - Gives the specified amount of lunar coins to the issuing player. A negative count may be specified to remove that many. `give_lunar [amount:1]`
* **remove_item** - Removes an item from a character's inventory. A negative amount is an alias for `give_item`: `remove_item {item} [count:1] *[target (player|'pinged'):<self>]`
* **remove_item_stacks** - Removes all item stacks from a character's inventory. `remove_item_stacks {item} *[target (player|'pinged'):<self>]`
* **remove_all_items** - Removes all items from a character's inventory. `remove_all_items *[target (player|'pinged'):<self>]`
* **remove_equip** - Sets the equipment of a character to 'None'. `remove_equip *[target (player|'pinged'):<self>]`
* **create_pickup** - Creates a pickup in front of a player. Pickups are items, equipment, or coins. When the pickup is an item or equipment, the search argument 'item' or 'equip' may be specified to only search that list. `create_pickup {object (item|equip|'lunarcoin'|'voidcoin')} [search ('item'|'equip'|'both'):'both'] *[player:<self>]`

Spawn Commands:

* **spawn_interactable/spawn_interactible** - Spawns an interactible in front of the player. `(spawn_interactable|spawn_interactible) {interactable}`
* **spawn_ai** - Spawn an AI. `spawn_ai {ai} [count:1] [elite:None] [braindead (0|1):0/false] [team:Monster]`.
* **spawn_as** - Spawn as a new character. `spawn_as {body} *[player:<self>]`
* **spawn_body** - Spawns a CharacterBody with no AI, inventory, or team alliance: `spawn_body {body}`
* **change_team** - Change a player's team. `change_team {team} *[player:<self>]`.

Cheat Commands:

* **no_enemies** - Toggles enemy spawns.
* **god** - Toggles HealthComponent.TakeDamage for all players. AKA: you can't take damage.
* **buddha** / **budha** / **buda** / **budda** - Turns damage taken `NonLethal` for all players. AKA: you can't die.
* **lock_exp** - Prevents EXP gain for the player team.
* **noclip** - Toggles noclip. Allow you to fly and going through objects. Sprinting will double the speed.

Bind Commands:

* **dt_bind** - Bind a key to execute specific commands. `dt_bind {key} {<consolecommands seperated by ;>}`
* **dt_bind_delete** Remove a custom bind. `dt_bind_delete {key}`
* **dt_bind_reload** Reload the macro system from file. `dt_bind_reload` 

Server Related Commands:

* **kick** - Kicks the specified Player Name/ID from the game.
* **ban** - Session bans the specified Player Name/ID from the game.

* **perm_enable** - Enable or disable the permission system.
* **perm_mod** - Change the permission level of the specified PlayerID/Username with the specified Permission Level.

Reload Commands:

* **perm_reload** - Reload the permission system, updates user and commands permissions.
* **reload_all_config** - Reload all default config files from all loaded BepinEx plugins.

### Unlocked Vanilla Commands ###

* **sv_time_transmit_interval** - How long it takes for the server to issue a time update to clients. `sv_time_transmit_interval [time]`
* **run_scene_override** - Overrides the first scene to enter in a run. `run_scene_override [scene]`
* **stage1_pod** - Whether or not to use the pod when spawning on the first stage. `stage1_pod [(0|1)]`
* **run_set_stages_cleared**  - Sets the amount of stages cleared. This does not change the current stage. `run_set_stages_cleared {stage_count}`. This obsoletes `stage_clear_count` from previous RoR2Cheats versions.
* **team_set_level** - Sets the specified team to the specified level: `team_set_level {team} {level}`. This obsoletes `give_exp` from previous RoR2Cheats versions.
* **loadout_set_skill_variant** - Sets the skill variant for the sender's user profile: `loadout_set_skill_variant {body_name} {skill_slot_index} {skill_variant_index}`. Note that this does not use the loose bodymatching from custom commands.
* **set_scene** - Removed the cheat check on this. Functions similar but not really to our `next_stage`, doesn't have our cool autocomplete features, and doesn't advance the stage count, but can advance menus. `set_scene {scene}`

### Additional Macros ###

* **midgame** - This is the preset HopooGames uses for midgame testing. Gives all users random items, and drops you off in the bazaar. `midgame`
* **lategame** - This is the preset HopooGames uses for endgame testing. Gives all users random items, and drops you off in the bazaar. `lategame`
* **dtzoom** - Gives you 20 hooves and 200 feathers to get around the map quickly. Based on a command in the initial release of Risk of Rain 2.
