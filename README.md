# Greety Greeter

A Dalamud plugin for Final Fantasy XIV that automatically greets players who enter a defined area.

## Features

- **Zone Definition** – Define a sphere (3 axis-circles rendered in-game) or an axis-aligned cube as your greeting zone. Set the zone center to your current position with one click and adjust size freely.
- **Player Memory** – Every player who enters the zone is recorded with first-seen and last-seen timestamps. Greetings only trigger again after a configurable cooldown (1 minute to 24 hours).
- **Greeting Presets** – Create named, sortable presets with any number of command lines. Each line has an individual delay. The `<t>` placeholder is replaced with `PlayerName@HomeWorld` — independently of FFXIV's own `<t>` target mechanic, so `/tell` works correctly without needing to target the player.
- **Tell Rate Limiter** – A global internal timer guarantees that `/tell` commands are never sent faster than 1.1 seconds apart, regardless of configuration.
- **Queue System** – Players who enter the zone are processed one by one. Sequences never overlap.
- **5 UI Languages** – English, Deutsch, Français, Русский, 日本語. Switchable at any time from the Settings window.

## Installation

Add this custom repository to Dalamud:

```
https://raw.githubusercontent.com/NeoGriever/GreetyGreeter/main/pluginmaster.json
```

## Commands

| Command | Action |
|---------|--------|
| `/gg` | Toggle the main panel |
| `/gg settings` | Open the settings window |

## Usage

1. Open `/gg settings` → **Zone** tab
2. Click **Capture My Position** to set the zone center to your current location
3. Choose **Sphere** or **Cube** and adjust the size
4. Enable **Show Zone Visual** to see the 3D overlay
5. Go to the **Presets** tab, create a preset, and add command lines
   - Use `<t>` as placeholder for the greeted player (`/tell <t> Hello!`)
6. Go to **Greet** tab, set the cooldown and select your active preset
7. Enable the zone with the `●` button in the main panel

## Placeholder Reference

| Token | Replaced with |
|-------|---------------|
| `<t>` | `PlayerName@HomeWorld` |

## License

AGPL-3.0-or-later

## Repository

[github.com/NeoGriever/GreetyGreeter](https://github.com/NeoGriever/GreetyGreeter)
