# Render Limits

Allows changing how far away zones are rendered, loaded and generated.

Install on the server and optionally on clients (modding [guide](https://youtu.be/L9ljm2eKLrk)).

## Possible uses

- If you have a powerful computer, you can increase the limits for a higher draw distance (at the cost of performance).
- If you want to take pretty picture of your entire base, you can temporarily increase the loaded area.
- If you have performance issues, you can try reducing distant areas.

## Configuration

The config can be found in the \<GameDirectory\>\BepInEx\config\ folder after the first start up. Or changed with the `render_config` command.

Each zone is a 64x64 meter area.

On multiplayer, the mod must be installed on the server. Clients can optionally install the mod to change the setting. Clients without the mod will use the default game settings.

### 1. Zones

- Active zones (default: `1`): Amounts of zones that are active around the player. Creatures are active in this area.
- Loaded zones (default: `2`): Amounts of zones loaded around the player. Structures are visible in this area.
- Generated zones (default: `4`): Amounts of zones generated around the player. Large static objects like trees are visible in this area.
- Real terrain visibility (default: `0`): Visibility in meters. If 0, automatically calculated from loaded area.
- Force active (default: none): Zones that are always active. Use the `force_active` command to modify this.
  - Note: When using this near buildings, make sure to include the whole building.
  - Otherwise it might collapse because only a part of it is loaded.

### 2. Quality

- Clutter visibility (default: `45`): Visibility in meters.
- Lod bias (default: none): Level of detail limit (increase to show smaller objects, vanilla is from 1 to 5 but even 100 works).
- Pixel light count (default: none): Light detail (bigger the better, vanilla is from 2 to 8).
- Shadow cascades (default: none): Improves shadows near the camera (bigger the better, vanilla is from 2 to 4).
- Shadow quality (default: none): 0: off, 1: hard only, 2: all.
- Shadow distance (default: none): Max distance for shadows in meters (vanilla is from 80 to 150).
- Shadow resolution (default: none): Shadow quality. From 0 to 3.

### 3. Server limits

- Maximum generated zones (default: `10`): Maximum generated zones that can be received from clients.
  - This prevents players from freezing the server by requesting too high generated area.
- Allow force active (default: `false`): Whether clients can have force active areas.
  - Disabled by default because the implementation has some issues.
  - Currently force active is not server specific. Players can accidentally have wrong settings for different servers.
  - Force active can be used to collapse buildings by only loading a part of them.

## Commands

`force_active [add/remove/toggle] [around=0]`

- `force_active`: Toggles the force active of the current zone.
- `force_active add`: Adds force active to the current zone.
- `force_active remove`: Removes force active from the current zone.
- `force_active toggle 1`: Toggles the force active of the current zone and adjacent zones (9 zones).
- `force_active add 2`: Adds force active to the current zone, adjacent zones and their adjacent zones (25 zones).
