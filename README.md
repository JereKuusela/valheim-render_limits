# Render Limits

This client side mod allows changing how far away zones are rendered, loaded and generated.

Install on the client (modding [guide](https://youtu.be/L9ljm2eKLrk)).

# Posssible uses

- If you have a powerful computer, you can increase the limits for a higher draw distance (at the cost of performance).
- If you want to take pretty picture of your entire base, you can temporarily increase the loaded area.
- If you have performance issues, you can try reducing distant and loaded areas.

# Configuration

The config can be found in the \<GameDirectory\>\BepInEx\config\ folder after the first start up. Or changed with the `render_config` command.

The game world is split to "zones" of 64x64 meters. By default the current zone and all adjacent zones consist of the active area. This is the area where things happen and where creatures are visible.

Around that is the loaded area. Here objects exist in the world but are frozen. Static objects like structures are visible here. Real terrain is visible here.

Finally there is the distant area that is two zones around the loaded area. Here most objects are instantly destroyed after being generated. Big static objects like trees are visible here.

- Active area (key: `active_area`, default: `2`): Amounts of zones that are active around the player. Creatures are visible in this area. Minimum value is 1.
- Distant area (key: `distant_area`, default: `5`): Amounts of zones generated around the player. Big static objects like trees are visible in this area. Minimum value is Loaded area.
- Force active (key: `force_active`): Zones that are always active. Internal value. Use the `force_active` command to modify this.
- Loaded area (key: `loaded_area`, default: `3`): Amounts of zones loaded around the player. Structures are visible in this area. Minimum value is Active area.
- Lod bias (key: `lod_bias`, default: `5`): Level of detail. Higher values show smaller distance objects.
- Clutter visibility area (key: `clutter_visibility`, default: `45`): How many meters away the clutter like grass is shown. This is based on the camera position which works bit weird for smaller values.
- Spawn limit (key: `spawn_limit`, default: `200`): How many meters away the spawn limits are checked. 0 for all loaded objects (base game behavior).
- Real terrain visibility (key: `real_terrain_visibility`, default: `180`): Visibility in meters. Higher values move the low quality terrain further away but may show unloaded areas as void.

Note: The default value for spawn limit modifies how the game works. For example enemies in dungeons will no longer count towards the main world spawns. Also the 200 meters is not exactly the same as the "loaded area" check (but very close).

This "fix" must done, otherwise increasing active or loaded are would significantly reduce enemy spawns.

# Commands

`force_active [add/remove/toggle] [around=0]`

- `force_active`: Toggles the force active of the current zone.
- `force_active add`: Adds force active to the current zone.
- `force_active remove`: Removes force active from the current zone.
- `force_active toggle 1`: Toggles the force active of the current zone and adjacent zones (9 zones).
- `force_active add 2`: Adds force active to the current zone, adjacent zones and their adjacent zones (25 zones).
