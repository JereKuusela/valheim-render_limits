# Render Limits

Allows changing how far away zones are rendered, loaded and generated.

Install on the server and optionally on clients (modding [guide](https://youtu.be/L9ljm2eKLrk)).

## Possible uses

- If you have a powerful computer, you can increase the limits for a higher draw distance (at the cost of performance).
- If you want to take pretty picture of your entire base, you can temporarily increase the loaded area.
- If you have performance issues, you can try reducing distant areas.

## Configuration

The config can be found in the \<GameDirectory\>\BepInEx\config\ folder after the first start up. Or changed with the `render_config` command. The mod reloads the file when it changes. On a server, joined players get the new values at once.

Each zone is a 64x64 meter area.

On multiplayer, the mod must be installed on the server. The mod replaces the game simulation distance setting and the `-simulationdistance` server option. If a client has higher values than the server, the client gets the server values. If a client has lower values, the client keeps its own. Clients without the mod use the game setting, limited by the server.

### 1. Zones

- Active zones (default: `1`): Amounts of zones that are active around the player. Creatures are active in this area.
  - The server uses its own value for all players.
- Loaded zones (default: `2`): Amounts of zones loaded around the player. Structures are visible in this area.
- Generated zones (default: `4`): Amounts of zones generated around the player. Large static objects like trees are visible in this area.
- Real terrain visibility (default: `0`): Visibility in meters. If 0, the game decides from loaded area.
- Force active (default: none): Zones that are always active. Use the `force_active` command to modify this.
  - The list is saved for each world. Lists from older versions are ignored and must be added again.
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

- Maximum generated zones (default: `10`): Maximum generated zones. Loaded zones and active zones are also limited by it.
  - This prevents a typo from freezing the game or the server. Increase it only if your computer can handle it.
- Allow force active (default: `false`): Whether clients can have force active zones.
  - The client sends its zones to the server. The server keeps them active and gives the client the objects in them.
  - The server can refuse the list. Then the client does not use force active.
  - Force active can be used to collapse buildings by only loading a part of them.
- Maximum force active zones (default: `64`): Maximum force active zones for each client.

## Credits

Thanks for Azumatt for creating the mod icon!

Sources: [GitHub](https://github.com/JereKuusela/valheim-render_limits)

Donations: [Buy me a computer](https://www.buymeacoffee.com/jerekuusela)
