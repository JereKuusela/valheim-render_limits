# Render Limits

This client side mods allows changing how far away zones are rendered, loaded and generated.

Can be installed on the server for syncing the client configs but not mandatory.

# Manual Installation:

1. Install the [BepInExPack Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim).
2. Download the zip and extract the DLL file to the \<GameDirectory\>\BepInEx\plugins\ folder.
3. Optionally also install the [Configuration manager](https://github.com/BepInEx/BepInEx.ConfigurationManager/releases/tag/v16.4).

# Configuration

The config can be found in the \<GameDirectory\>\BepInEx\config\ folder after the first start up. Or changed with the `render_config` command.

The game world is split to "zones" of 64x64 meters. By default the current zone and all adjacent zones consist of the active area. This is the area where things happen.

Around that is the loaded area. Here objects are already loaded the world but frozen. Finally there is the generated area that is two zones around the loaded area. Here the objects are instantly destroyed after being generated.

- Active area (key: `active_area`, default: `2`): Amounts of zones that are active around the player. Minimum value is 1.
- Generated area (key: `generated_area`, default: `5`): Amounts of zones generated around the player. Minimum value is Loaded area.
- Loaded area (key: `loaded_area`, default: `3`): Amounts of zones loaded around the player. Minimum value is Active area.
- Real terrain visibility (key: `real_terrain_visibility`, default: `180`): Visibility in meters. Higher values move the low quality terrain further away but may show unloaded areas as void.

# Changelog

- v1.0
	- Initial release
