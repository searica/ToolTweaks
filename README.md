# ToolTweaks
Simple mod that lets you customize stamina cost, durability drain, and usage delay for tools. Will work with tools added by other mods (so long as other mod authors set the item type to Tool). This mod is very similar to existing mods like FastTools but instead of using Transpiler patches it relies on Prefix/Postfix patches to avoid causing visual bugs when using tools like the Hoe.

**Server-Side Info**: This mod does work as a client-side only mod and only needs to be installed on the server if you wish to enforce configuration settings.

## Instructions
If you are using a mod manager for Thunderstore simply install the mod from there. If you are not using a mod manager then, you need a modded instance of Valheim (BepInEx) and the Jotunn plugin installed.

## Configuration
Changes made to the configuration settings will be reflected in-game immediately (no restart required) and they will also sync to clients if the mod is on the server. The mod also has a built in file watcher so you can edit settings via an in-game configuration manager (changes applied upon closing the in-game configuration manager) or by changing values in the file via a text editor or mod manager.

### Global Section:


<div class="header">
	<h3>Global Section</h3>
  These settings control the main features of the mod and how verbose it's output to the log is.
</div>
<table>
	<tbody>
		<tr>
			<th align="center">Setting</th>
			<th align="center">Server Sync</th>
			<th align="center">Description</th>
		</tr>
		<tr>
			<td align="center"><b>Verbosity</b></td>
			<td align="center">No</td>
			<td align="left">
				Low will log basic information about the mod. Medium will log information that is useful for troubleshooting. High will log a lot of information, do not set it to this without good reason as it will slow down your game.
				<ul>
					<li>Acceptable values: Low, Medium, High</li>
					<li>Default value: Low</li>
				</ul>
			</td>
		</tr>
		<tr>
			<td align="center"><b>Usage Delay</b></td>
			<td align="center">Yes</td>
			<td align="left">
				Set the time delay between tool uses for both placement and removal. Vanilla default is 0.4s for placement and 0.25s for removal.
				<ul>
					<li>Acceptable values: (0.05, 2)</li>
					<li>Default value: 0.25</li>
				</ul>
			</td>
		</tr>
		<tr>
			<td align="center"><b>Stamina Cost Multiplier</b></td>
			<td align="center">Yes</td>
			<td align="left">
				Change the stamina cost for using tools. Setting to 0.5 means stamina costs are reduced to 50%. Setting to 2 means stamina costs are increased to 200%.
				<ul>
					<li>Acceptable values: (0.0, 2)</li>
					<li>Default value: 0.5</li>
				</ul>
			</td>
		</tr>
		<tr>
			<td align="center"><b>Durability Drain Multiplier</b></td>
			<td align="center">Yes</td>
			<td align="left">
				Change the amount of durability drained each time a tool is used. Setting to 0.5 means durability is drained 50% as much. Setting to 2 means durability drain is increased to 200%.
				<ul>
					<li>Acceptable values: (0.0, 2)</li>
					<li>Default value: 0.5</li>
				</ul>
			</td>
		</tr>
	</tbody>
</table>

## Known Issues
None so far, tell me if you find any.

## Compatibility
Should be compatible with most mods and is fully compatible with AdvancedTerrainModifiers.

## Donations/Tips
My mods will always be free to use but if you feel like saying thanks you can tip/donate.

| My Ko-fi: | [![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/searica) |
|-----------|---------------|

## Source Code
Source code is available on Github.

| Github Repository: | <button style="font-size:20px"><img height="18" src="https://github.githubassets.com/favicons/favicon-dark.svg"></img><a href="https://github.com/searica/ToolTweaks"> ToolTweaks</button> |
|-----------|---------------|

### Contributions
If you would like to provide suggestions, make feature requests, or reports bugs and compatibility issues you can either open an issue on the Github repository or tag me (@searica) with a message on my discord [Searica's Mods](https://discord.gg/sFmGTBYN6n).

I'm a grad student and have a lot of personal responsibilities on top of that so I can't promise I will respond quickly, but I do intend to maintain and improve the mod in my free time.

### Credits
This mod was inspired by FastTools by CrystalFerrai.

## Shameless Self Plug (Other Mods By Me)
If you like this mod you might like some of my other ones.

#### Building Mods
- [More Vanilla Build Prefabs](https://valheim.thunderstore.io/package/Searica/More_Vanilla_Build_Prefabs/)
- [Extra Snap Points Made Easy](https://valheim.thunderstore.io/package/Searica/Extra_Snap_Points_Made_Easy/)
- [AdvancedTerrainModifiders](https://valheim.thunderstore.io/package/Searica/AdvancedTerrainModifiders/)
- [BuildRestrictionTweaksSync](https://valheim.thunderstore.io/package/Searica/BuildRestrictionTweaksSync/)

#### Gameplay Mods
- [CameraTweaks](https://valheim.thunderstore.io/package/Searica/CameraTweaks/)
- [DodgeShortcut](https://valheim.thunderstore.io/package/Searica/DodgeShortcut/)
- [FortifySkillsRedux](https://valheim.thunderstore.io/package/Searica/FortifySkillsRedux/)
- [ProjectileTweaks](https://github.com/searica/ProjectileTweaks/)
- [SkilledCarryWeight](https://github.com/searica/SkilledCarryWeight/)
- [SafetyStatus](https://valheim.thunderstore.io/package/Searica/SafetyStatus/)