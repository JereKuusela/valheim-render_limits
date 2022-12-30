# Render Limits

This client side mod allows changing how far away zones are rendered, loaded and generated.

Can be installed on the server for syncing the client configs but not mandatory.

Install on the client (modding [guide](https://youtu.be/L9ljm2eKLrk)).

Install on the server to enforce some settings.

# Posssible uses

- If you have a powerful computer, you can increase the limits for a higher draw distance (at the cost of performance).
- If you want to take pretty picture of your entire base, you can temporarily increase the loaded area.
- If you have performance issues, you can try reducing distant and loaded areas.

# Configuration

The config can be found in the \<GameDirectory\>\BepInEx\config\ folder after the first start up. Or changed with the `render_config` command.

## Visibility settings

The game world is split to "zones" of 64x64 meters. By default the current zone and all adjacent zones consist of the active area. This is the area where things happen and where creatures are visible.

Around that is the loaded area. Here objects exist in the world but are frozen. Static objects like structures are visible here. Real terrain is visible here.

Finally there is the distant area that is two zones around the loaded area. Here most objects are instantly destroyed after being generated. Big static objects like trees are visible here.

- Active area (default: `2`): Amounts of zones that are active around the player. Creatures are visible in this area. Minimum value is 1.
- Distant area (default: `5`): Amounts of zones generated around the player. Big static objects like trees are visible in this area. Minimum value is Loaded area.
- Force active: Zones that are always active. Internal value. Use the `force_active` command to modify this.
- Loaded area (default: `3`): Amounts of zones loaded around the player. Structures are visible in this area. Minimum value is Active area.
- Clutter visibility area (default: `45`): How many meters away the clutter like grass is shown. This is based on the camera position which works bit weird for smaller values.
- Real terrain visibility (default: `180`): Visibility in meters. Higher values move the low quality terrain further away but may show unloaded areas as void.

## Quality settings

- Lod bias: Level of detail. Higher values show smaller distance objects.
- Pixel light count: Light detail (bigger the better, vanilla is from 2 to 8).
- Shadow cascades: Improves shadows near the camera (bigger the better, vanilla is from 2 to 4).
- Shadow quality: 0: off, 1: hard only, 2: all.
- Shadow distance: Max distance for shadows in meters (vanilla is from 80 to 150).
- Shadow resolution: Shadow level of detail. From 0 to 3.

## Synced settings

These settings are enforced if installed on the server. They can only be changed by admins.

- Spawn limit (default: `200`): How many meters away the spawn limits are checked. 0 for all loaded objects (base game behavior).

Note: The default value for spawn limit modifies how the game works. For example enemies in dungeons will no longer count towards the main world spawns. Also the 200 meters is not exactly the same as the "loaded area" check (but very close).

This "fix" must done, otherwise increasing active or loaded are would significantly reduce enemy spawns.

# Commands

`force_active [add/remove/toggle] [around=0]`

- `force_active`: Toggles the force active of the current zone.
- `force_active add`: Adds force active to the current zone.
- `force_active remove`: Removes force active from the current zone.
- `force_active toggle 1`: Toggles the force active of the current zone and adjacent zones (9 zones).
- `force_active add 2`: Adds force active to the current zone, adjacent zones and their adjacent zones (25 zones).

# Credits

Thanks for Azumatt for creating the mod icon!

Sources: [GitHub](https://github.com/JereKuusela/valheim-render_limits)

Donations: [Buy me a computer](https://www.buymeacoffee.com/jerekuusela)

# Changelog

- v1.6
	- Adds new settings for light and shadows.
	- Changes the config sync to be always active but only affect the Spawn limit setting.

- v1.5
	- Fixes the black screen.

- v1.4
	- Adds a new command `force_active` to force areas to stay active.

- v1.3
	- Adds a new setting `lod_bias` to change how far away smaller objects are shown.
	- Changes the mod GUID.

- v1.2
	- Fixes structure stability calculation not working on nearby zones.

- v1.1
	- Adds a new setting `clutter_visibility` to change how far grass is visible.
	- Fixes the spawn limit having less range than intended.
