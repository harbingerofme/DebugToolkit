## DebugToolkit

This mod adds various debugging commands to the console. See below for all commands plus explanation.

[Harb](https://thunderstore.io/package/Harb/), [iDeathHD](https://thunderstore.io/package/xiaoxiao921/) and ['s](https://thunderstore.io/package/paddywan/) reimplementation of [Morris1927's](https://thunderstore.io/package/Morris1927/) [RoR2Cheats](https://thunderstore.io/package/Morris1927/RoR2Cheats/). Derived with permission. 

Also adds autocompletion for arguments and networked commands giving their information to the right people to the console. Read [here](https://github.com/harbingerofme/DebugToolkit/tree/master/Code/AutoCompletion) to see how you can use the autocompletion feature for your own mod.

Track update progress, get support and suggest new features over at the [DebugToolkit discord](https://discord.gg/yTfsMWP).

Some vanilla console functions you might not know:

* The console can be opened with `ctrl+alt+~`.
  * **DebugToolkit automatically enables the console so there is no need to do this.** In fact doing this would lock the console and you would have to press the combination to unlock it again. Just press `~` to toggle the window.
  * We have also added `Page Up` as an alternative to the default key since the latter is language dependent. Configurable.
* `help {command}` may be used to get help for a specific command.
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
* [RandomlyAwesome](https://github.com/yekoc)

---

## COMMANDS ##

Command Parsing:
- Multiple commands can be combined with `;`.
- Only alphanumeric characters and the symbols `._-:` are allowed. Any other character requires being surrounded by quotes in order to be parsed literally. Both `"` and `'` work, while `\"` and `\'` also work when nesting quotes.

Verbiage:
- The brackets encapsulating arguments mean `{needed}`, `(choose one)`, and `[optional]`.
- If an optional argument has a default value, it will be indicated with `:`. Any preceeding optional arguments from the one entered become necessary, but empty double quotes (`""`) can be used as a placeholder for the default value. If an optional argument is required from a dedicated server, it will be preceeded by `*`.
- Player, body, AI, item, equipment, team, elite, interactable, and director card values can be declared by either their ID, or their string name. The latter can be written in freeform and it will be matched to the first result that contains the string. See the related `list_` commands for options and which result would take precedence if there are multiple matches.

Commands:

* **next_stage** - Advance to the next stage. `next_stage [specific_stage]`. If no stage is entered, the next stage in progression is selected.
* **force_family_event** - Forces a Family Event to happen in the next stage, takes no arguments. `force_family_event`
* **next_boss** - Sets the next teleporter/simulacrum boss to the specified boss. Get a list of potential boss with `list_directorcards`. `next_boss {director_card} [count:1] [elite:None]`
* **fixed_time** - Sets the time that has progressed in the run. Affects difficulty. `fixed_time [time]`. If no time is supplied, prints the current time to console.
* **stop_timer** - Pause/unpause the run timer. `pause_timer [enable (0|1)]`. If no argument is supplied, toggles the current state. Only works for "Stage" and "TimedIntermission" stages.
* **next_wave** - Advance to the next Simulacrum wave. `next_wave`
* **force_wave** - Set the next wave prefab. `force_wave [wave_prefab]`. If no input is supplied, prints all available options and clears any previous selection.
* **run_set_waves_cleared** - Set the Simulacrum waves cleared. Must be positive. `set_run_waves_cleared {wave}`
* **add_portal** - Add a portal to the current Teleporter on completion. `add_portal {portal ('blue'|'celestial'|'gold'|'green'|'void'|'all')}`.
* **charge_zone** - Set the charge of all active holdout zones. `charge_zone {charge}`. The value is a float between 0 and 100.
* **seed** - Set the seed for all next runs this session. `seed [new_seed]`. Use `0` to specify the game should generate its own seed. If used without argument, it's equivalent to the vanilla `run_get_seed`.
* **set_artifact** - Enable/disable an Artifact. `set_artifact {artifact (artifact|'all')} [enable (0|1)]`. If enable isn't supplied, it will toggle the artifact's current state. However, it is required when using "all".
* **kill_all** - Kills all members of a specified team. `kill_all [team:Monster]`.
* **true_kill** - Truly kill a player, ignoring revival effects. `true_kill *[player:<self>]`
* **respawn** - Respawn a player at the map spawnpoint. `respawn *[player:<self>]`
* **hurt** - Deal generic damage to a target. `hurt {amount} *[target (player|'pinged'):<self>]*`
* **heal** - Heal a target. `heal {amount} *[target (player|'pinged'):<self>]*`
* **teleport_on_cursor** -  Teleport you to where your cursor is currently aiming at. `teleport_on_cursor`
* **time_scale** -  Sets the timescale of the game. 0.5 would mean everything happens at half speed. `time_scale [time_scale]`. If no argument is supplied, gives the current timescale.
* **post_sound_event** - Post a sound event to the AkSoundEngine (WWise) either by its event name or event ID. `post_sound_event {sound_event (event_name|event_id)}`
* **delay** - Execute any commands after a delay in seconds. `delay {delay} {<consolecommands separated by ;>}`

List Commands:

* [All the `list_` commands support filtering](https://user-images.githubusercontent.com/57867641/295963274-169b2fd9-a5ea-41df-8dba-2632f75ddbd4.png). A number for the unique index or any string for partial matching.
* **list_player** - List all Players and their ID.
* **list_body** - List all Bodies and their language invariants.
* **list_ai** - List all Masters and their language invariants.
* **list_elite** - List all Elites and their language invariants.
* **list_team** - List all Teams and their language invariants.
* **list_artifact** - List all Artifacts and their language invariants.
* **list_buff** - List all Buffs and if they are stackable.
* **list_dot** - List all DoT effects.
* **list_itemtier** - List all Item Tiers.
* **list_item** - List all Items, their language invariants, and if they are in the current drop pool.
* **list_equip** - List all Equipment, their language invariants, and if they are in the current drop pool.
* **list_interactables/list_interactibles** List all Interactables.
* **list_directorcards** List all Director Cards. Mainly used for the `next_boss` command.
* **list_scene** List all Scenes, their language invariants, and if are they an offline scene.
* **list_skins** List all Body Skins and the language invariant of the current one in use.
* **list_survivor** List all Survivors and their body/ai names.

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

Item Commands:

* **give_item** - Give an item directly to a target's inventory. A negative amount is an alias for `remove_item`: `give_item {item} [count:1] *[target (player|'pinged'|'evolution'|'simulacrum'|'voidfields'|'devotion'):<self>]`
* **random_items** - Generate random items from the available item tiers. `random_items {count} [droptable (droptable|'all'):'all'] *[target (player|'pinged'|'evolution'|'simulacrum'|'voidfields'|'devotion'):<self>]`
* **give_equip** - Give an equipment directly to a target's inventory: `give_equip {(equip|'random')} *[target (player|'pinged'|'evolution'|'simulacrum'|'voidfields'|'devotion'):<self>]`
* **give_money** - Gives the desired player/team money. A negative amount can remove that many without underflowing. `give_money {amount} [target ('all'|player):'all']`
* **give_lunar** - Gives the specified amount of lunar coins to the issuing player. A negative count may be specified to remove that many. `give_lunar [amount:1]`
* **remove_item** - Removes an item from a target's inventory. A negative amount is an alias for `give_item`: `remove_item {item} [count:1] *[target (player|'pinged'|'evolution'|'simulacrum'|'voidfields'|'devotion'):<self>]`
* **remove_item_stacks** - Removes all item stacks from a target's inventory. `remove_item_stacks {item} *[target (player|'pinged'|'evolution'|'simulacrum'|'voidfields'|'devotion'):<self>]`
* **remove_all_items** - Removes all items from a target's inventory. `remove_all_items *[target (player|'pinged'|'evolution'|'simulacrum'|'voidfields'|'devotion'):<self>]`
* **remove_equip** - Sets the equipment of a target to 'None'. `remove_equip *[target (player|'pinged'|'evolution'|'simulacrum'|'voidfields'|'devotion'):<self>]`
* **restock_equip** - Restock charges for the current equipment. `restock_equip [count:1] *[target (player|'pinged'|'evolution'|'simulacrum'|'voidfields'|'devotion'):<self>]`
* **create_pickup** - Creates a pickup in front of a player. Pickups are items, equipment, or coins. When the pickup is an item or equipment, the search argument 'item' or 'equip' may be specified to only search that list. `create_pickup {object (item|equip|'lunarcoin'|'voidcoin')} [search ('item'|'equip'|'both'):'both'] *[player:<self>]`
* **create_potential** - Creates a potential in front of a player. The first item tier defined in the droptable decides the color of the droplet and what items will be available with the Artifact of Command. `create_potential [droptable (droptable|'all'):'all'] [count:3] *[player:<self>]`

***Note:*** Some commands support a weighted item selection, referred to as _droptable_. The syntax for it is `<itemtier:weight tokens separated by comma>`. The weight should be a positive float and is an optional argument with a default value of 1.0. If a comma or decimal point is used, the whole argument must be surrounded in double quotes. The keyword `all` uses all available item tiers with a default weight. For example, any of the following are valid inputs: `tier1`, `"tier1:5,tier2,tier3:0.4"`, `all`.

Spawn Commands:

* **spawn_interactable/spawn_interactible** - Spawns an interactible in front of the player. `(spawn_interactable|spawn_interactible) {interactable}`
* **spawn_portal** - Spawns a portal in front of the player. `spawn_portal {portal ('artifact'|'blue'|'celestial'|'deepvoid'|'gold'|'green'|'null'|'void')}`.
* **spawn_ai** - Spawn an AI. `spawn_ai {ai} [count:1] [elite:None] [braindead (0|1):0/false] [team:Monster]`.
* **spawn_as** - Spawn as a new character. `spawn_as {body} *[player:<self>]`
* **spawn_body** - Spawns a CharacterBody with no AI, inventory, or team alliance: `spawn_body {body}`
* **change_team** - Change a player's team. `change_team {team} *[player:<self>]`.

Profile Commands:

* **prevent_profile_writing** - Prevent saving the user profile to avoid bogus data. Enable before doing something and keep it until the end of the session. `prevent_profile_writing [enable (0|1)]`. If no argument is supplied, prints the current state. Disabled by default.

Cheat Commands:

* **no_enemies** - Prevents enemy spawns. `no_enemies [enable (0|1)]`. If no argument is supplied, toggles the current state.
* **god** - Prevents players from taking any damage. `god [enable (0|1)]`. If no argument is supplied, toggles the current state.
* **buddha** / **budha** / **buda** / **budda** - Turns damage taken `NonLethal` for all players. AKA: you can't die. `buddha [enable (0|1)]`. If no argument is supplied, toggles the current state.
* **lock_exp** - Prevents EXP gain for the player team. `lock_exp [enable (0|1)]`. If no argument is supplied, toggles the current state.
* **noclip** - Toggles noclip. Allow you to fly and going through objects. Sprinting will double the speed. `noclip [enable (0|1)]`. If no argument is supplied, toggles the current state.

Bind Commands:

* **dt_bind** - Bind a key to execute specific commands. `dt_bind {key} [<consolecommands separated by ;>]`. See [here](https://docs.unity3d.com/Manual/class-InputManager.html) for a list of possible key names. Alt, Ctrl, and Shift can also be used for key combinations, e.g. `"left shift+left ctrl+x"`. If no commands are provided, it prints information about the key.
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
