
## Changelog ##

### 3.9 ###

* **3.9.0**
    * This update is basically carried by [SChinchi](https://github.com/SChinchi) and [RandomlyAwesome](https://github.com/yekoc), thanks!
    * Added `buddha` (aliases: `budha`, `buddha`, `buda`, `budda`): Become immortal. Instead of refusing damage you just refuse to take lethal damage. It works by giving all damage a player takes `DamageType.NonLethal`.
    * Added `dump_buffs`, `dump_inventories`, `dump_stats ` and `dump_state `: [Image showcasing the new commands](https://user-images.githubusercontent.com/57867641/273559529-cc505004-4f21-474a-84ed-672227183f65.png). The buff/inventory ones are useful for keeping track of who has what. The latter is also helpful to verify which characters get any NoTier items. The stats one is mostly for wiki work, while the state one to get current stats during gameplay and/or skill cooldowns, entity state and AI values.
    * Added various simulacrum commands: `next_wave`, `set_run_waves_cleared`, `force_wave ` and `dump_state `: `next_wave` (similar to `next_stage` for a classic run). `set_run_waves_cleared` {wave} (similar to `set_run_stages_cleared`). `force_wave` [wave_prefab] (similar to `force_family`). They add control to fast-track progression to certain wave numbers, e.g., transition waves, and force certain wave augments.
    * Updated `create_pickup`: Add autocomplete support, fix equipment not found without the 'equip' search. Add void coin support; useful for the Released From The Void mod. Explicit arg strings should be prioritised before partial search terms to avoid ambiguity.
    * Updated `give_equip`: Explicit arg strings should be prioritised before partial search terms to avoid ambiguity.
    * Updated `spawn_interactable`: Fix floating interactables when on ground.
    * Updated `midgame` and `lategame`: The previous update changed the command signature for `random_items` to include tiers. They now correctly include tier 1 to 3.
    * Updated `spawn_ai`: Fix spawns not properly getting UseAmbientLevel item, Evolution items, and artifact effects such as Swarms.

### 3.8 ###

* **3.8.2**
    * Updated `give_item` and `remove_item`: A negative amount is an alias for the other command and vice versa. Also simplified the code internally. Thanks [SChinchi](https://github.com/SChinchi)
    * Some changelog entries were missing for the `3.8.0` update, they have been retroactively added.

* **3.8.1**
    * Updated `random_items`: Can now specify which tiers of items you want to include. By default, all tiers are included.
    * Added: `list_itemtier`
    * Updated `next_boss`: Should work correctly for any master. Work consistently in simulacrum.
    * Fix commands (`spawn_ai` `next_boss`) that had an elite type arg. Specifying `-1` / `None` now works correctly.
    * Fix networked commands like `noclip` or `time_scale` not working in all custom gamemodes.

* **3.8.0**
    * This update is basically carried by [SChinchi](https://github.com/SChinchi), thank you!
    * Added: `give_buff`
    * Added: `give_dot`
    * Added: `remove_buff`
    * Added: `remove_buff_stacks`
    * Added: `remove_all_buffs`
    * Added: `remove_dot`
    * Added: `remove_dot_stacks`
    * Added: `remove_all_dot`
    * Added: `list_buff`
    * Added: `list_dot`
    * Details of how to use these commands are listed in the README file.
    * Many commands have a much better description of how they work and the errors printed in the console in case of misuse should also be clearer.
    * Ping functionality has been added for some commands, refer to the README for a complete list and look for the `pinged` arg.
    * Updated `remove_item`: Split into `remove_item_stacks` and `remove_all_items`.
    * Fixed `next_boss`: It just works.
    * Fixed `list_interactable`: Should now show all possible entries.
    * Fixed `list_directorcards`: Should now show all possible entries.
    * Fixed `force_family_event`: Wasn't working since SOTV DCCS pool changes.

### 3.7 ###

* "Survivors of the Void"
* **General**
    * Updated to work with the new game update.
* **3.7.1**
    * Fixed: `scene_list` was not prodiving all scenes. Thank you [DestroyedClone](https://thunderstore.io/package/DestroyedClone/)!
    * Fixed: `spawn_as` made people softlock.
    * Added: Support for void portals. Thank you [DestroyedClone](https://thunderstore.io/package/DestroyedClone/)!
    * Fixed: `set_scene` was broken.
    * Added: `set_scene` now also allows numbers from `scene_list` to be passed.
    * 'Fixed': `set_scene` and `next_stage` allowed players to visit DLC scenes when the DLC was not enabled.
* **3.7.2**
    * Added split assembly support Thank you [HeyImNoop](https://thunderstore.io/package/Heyimnoob/)
    * Started work on better interactible loading. Thank you [HeyImNoop](https://thunderstore.io/package/Heyimnoob/)
* **3.7.3**
    * Added filtering to listing commands (list_item, etc). Thank you [DestroyedClone](https://thunderstore.io/package/DestroyedClone/)

### 3.6 ###

* "DebugToolkit"
* **General**
    * Microsoft.CSharp.dll dependency is gone.
* **Additions**
    * `spawn_interactable` Now support custom interactables.
    * `spawn_interactible` Does the same as `spawn_interactable`.
    * `lock_exp` toggles EXP gain, note that this applies to all players. Thank you [DestroyedClone](https://thunderstore.io/package/DestroyedClone/)!
*  **Fixes**
    * Description of `add_portal` now mention the null portal, also you can now spawn it in teleporter-less stages.

### 3.5 ###

* "Anniversary Update"
*  **Fixes**
    * Updated for anniversary update.
    * **3.5.1** Updated for the update to the anniversary update.

### 3.4 ###

* "Full Release"
* **General**
    * **3.4.1** Made some internal classes public.
    * **3.4.1** Now supports custom stages too.
    * **3.4.1** Now has keybinds and macros.
    * **3.4.1** We are considering moving the permissions module to it's own seperate mod. Please give your feedback.
    * **3.4.2** Harb was a dumb dumb and didn't put the Microsoft assembly in the zip.
    * Updated for game version `1.0`.
    * ~~We are considering adding macros and keybinds to the base mod. [We would like your input on this.](https://github.com/harbingerofme/DebugToolkit/issues/101)~~
* **Additions**
    * **3.4.1** `dt_bind` You can now bind keys to macros. 
    * `list_item` When we removed this command, we forgot that people can forget. As the game now counts over 100 items, there's value in adding these lists back. They have been improved for more user readability.
    * `list_equip` (see list_item)
* **Fixes**
    * **3.4.1** Fixed an issue introduced in Gameversion 1.0.1 where `list_item` and `list_equip` wouldn't work.
    * **3.4.2** Fixed needing to have a config file so a config file could be generated.
    * **3.4.2** Fixed not having the Microsoft assembly in the zip.

### 3.3 ###

* "Artifacts"
* **General**
    * **3.3.2** Improved networking
    * **3.3.1** Removed minirpclib as a dependency. [Learn how for your own mod here](https://github.com/risk-of-thunder/R2Wiki/wiki/Networking-with-Weaver---The-Unity-Way)
    * Updated for artifacts.
    * `give_money` is now compatible with ShareSuite's money sharing.
    * We did a lot of cleanup behind the scenes. You won't notice this (hopefully), but it makes everything easier to maintain.
    * We've added some predefined macros for testing. We still recommend the macro plugins linked in our description if you want to define your own macros.
* **Additions**
    * **3.3.2** Permission System can now be enabled on the RoR2 Assembly Console Commands.
    * `random_items` generates an inventory of items for you.
    * `give_equip random` now gives you a random equipment from your available drop pool.
    * `midgame` and `lategame` allow you to test your mod in a typical mid/endgame scenario. The presets come from HopooGames.
    * `dtzoom` gives you some items to make you move around the map faster. This is a macro that was present in the original release of RoR2.
* **Fixes**
    * **3.3.2** Fix noclip failing to work in any subsequent run after it was activated.
    * **3.3.1** Fix incorrect parsing on some arguments in commands. Thank you Violet for reporting these!
    * **3.3.1** Removed double embbeded dependency. This shaves off about half the file size!
    * Fix a faulty ILHook in CCSetScene
    * Fix `spawn_as`.

### 3.2 ###

* "Dedicated Servers"
* **General**
    * Commands are now safe to run with a dedicated server as the sender. Want to know how you can issue commands during runtime as a server? [R2DSE](https://github.com/harbingerofme/R2DS-Essentials)
        * Some commands have not yet been implemented this way and merely do nothing, expect fixes for that in `3.2.X` updates.
        * When a player is needed for a command, dedicated servers will have to fully qualify their command.
* **Additions**
    * **3.2.1** reworked `spawn_ai` so that it can now spawn multiple enemies. As a result the command arguments have been shuffled around a bit.
    * Added the null portal to `add_portal`
    * `reload_all_config` makes a best effort to reload all plugins' configurations.
    * Permission system:
        * There's a config for the permission system located in your `bepinex/config/` folder after running the new version once.
        * The permission system is by default DISABLED.
* **Fixes**
    * **3.2.2** Fix seeveral premature game ends with noclip enabled crashing outside stuff.
    * **3.2.2** DT now correctly handles nonpostive enums. this fixes an annoying persistent issue in `spawn_ai`.
    * **3.2.1** `spawn_ai` now parses braindead if it's an integer.
    * **3.2.1** Fix `spawn_as` crashing dedicated servers.
    * **3.2.1** fixed_time now actually sets/displays the ingame time. Thank you [@Rayss](https://github.com/SuperRayss).
    * `Noclip` now disables the out of bounds teleport. Go take a look at that collossus head!
    * `add_portal` now gives a nicer error message with all available portals
    * Reenabled `next_boss`. Behaviour might still be weird.
* **Known Bugs**
    * Some commands work improperly when connected to a dedicated server with this enabled. Due to the amount of testing required basically doubling because of dedicated servers, we ask you to report these issues as you spot them.
* Other things:
    * Want to get bleeding edge builds? Check out our [Discord](https://discord.gg/yTfsMWP)!

### 3.1 ###

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

### 3.0 ###

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

### < 3.0 ###

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
* `On.RoR2.ExperienceManager.AwardExperience` - We use this for `lock_exp`.
* `On.RoR2.Stage.Start` - We hook this to remove the IL hook on ClassicStageInfo.
* `IL.RoR2.ClassicStageInfo.Awake` - We hook this to set a family event.
* NoClip requires 3 hooks:
    * `On.RoR2.Networking.GameNetworkManager.Disconnect` 
    * These two trampoline hooks are initialized, but are only applied on prompt:
        1. UnityEngine.NetworkManager.DisableOnServerSceneChange
        2. UnityEngine.NetworkManager.DisableOnClientSceneChange
    
