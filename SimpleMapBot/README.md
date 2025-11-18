# SimpleMapBot

A straightforward map clearing bot for Path of Exile that focuses on the essentials: running maps, killing monsters, and looting items.

## Features

- **Automated Map Running**: Takes maps from stash, opens map device, and enters
- **Zone Exploration**: Explores maps using intelligent pathfinding
- **Combat**: Kills monsters using BeastCombatRoutine (set zoom mode OFF)
- **Smart Looting**: Picks up currency, cards, maps, and valuable items with poe.ninja price checking
- **Auto Return**: Returns to hideout when inventory full, low health, or map complete
- **Movement Skills**: Uses movement skills efficiently via SimpleMover (v1.5.0)
- **Poe.ninja Integration**: Filters loot by chaos value using live poe.ninja prices

## Components

### Core Bot
- **SimpleMapBot**: Main bot logic with task-based state machine

### Tasks (Execution Order)
1. **ReturnToHideoutTask**: Returns to hideout when needed (inventory full, low health)
2. **TakeMapFromStashTask**: Gets a suitable map from specified stash tab
3. **OpenMapDeviceTask**: Opens map device and places map
4. **EnterMapTask**: Enters the map portal
5. **ExploreAndClearTask**: Explores zone, kills monsters, loots items

### Movement & Combat
- **SimpleMover**: Efficient movement with movement skill support (Whirling Blades, Flame Dash, etc.)
- **BeastCombatRoutine**: Combat handling (use with `EnableZoomMode = false`)

## Configuration

Settings are stored in JSON at: `Settings/[BotName]/SimpleMapBot.json`

### Key Settings

**Map Settings:**
- `MapStashTab`: Name of stash tab containing maps (default: "Maps")
- `MinMapTier` / `MaxMapTier`: Tier range to run (default: 1-16)
- `RunWhiteMaps` / `RunMagicMaps` / `RunRareMaps`: Which rarities to run

**Exploration:**
- `MaxMapTimeSeconds`: Max time in map before returning (default: 60s)
- `TargetExplorationPercent`: Target % of zone to explore (default: 80%)

**Combat:**
- `EnableCombat`: Enable fighting monsters (default: true)
- `CombatLeashRange`: How far to chase enemies (default: 40)

**Looting:**
- `EnableLooting`: Enable item pickup (default: true)
- `MaxLootRange`: Max distance to pick up loot (default: 50)
- `PickupCurrency` / `PickupDivinationCards` / `PickupMaps`: What to pick up
- `UsePoeNinjaFiltering`: Enable poe.ninja price checking (default: true)
- `PoeNinjaLeague`: League for price data (default: "Standard")
- `MinItemValueChaos`: Minimum chaos value to pick up (default: 5c)

**Return Conditions:**
- `ReturnWhenInventoryFull`: Return when inventory full (default: true)
- `ReturnHealthPercent`: Return if health drops below % (default: 20)

## Setup

1. **Place your maps** in a stash tab (name it "Maps" or configure `MapStashTab`)
2. **Configure SimpleMapBot.json** with your preferences
3. **Select SimpleMover** as your player mover
4. **Select BeastCombatRoutine** as your combat routine
   - Set `EnableZoomMode = false` in BeastCombatRoutine settings
5. **Start the bot** in your hideout

## Requirements

- Maps in configured stash tab
- Portal skill or portal scrolls in inventory
- Movement skills on skillbar (optional but recommended)
- Combat skills configured in BeastCombatRoutine
- In-game loot filter configured (bot respects game filter)

## Tips

- Start with white/magic low-tier maps to test
- Adjust `MaxMapTimeSeconds` based on your clear speed
- Enable `DebugLogging` to troubleshoot issues
- Use `DryRunMode` to test logic without entering maps

## Differences from BeastRoutine

SimpleMapBot is a simplified version focused on core mapping:

**What SimpleMapBot Does:**
- ✓ Runs maps continuously
- ✓ Kills all monsters
- ✓ Loots valuable items with poe.ninja price checking
- ✓ Returns to hideout when done

**What SimpleMapBot Doesn't Do:**
- ✗ Beast capture mechanics
- ✗ Scarab/fragment management
- ✗ Selective combat (fights everything)
- ✗ Cache detection
- ✗ Advanced perception modules

SimpleMapBot is perfect for learning bot development or running basic map farms. Once you master this, BeastRoutine adds advanced mechanics on top of the same foundation.

## Version

**v1.0.0** - Initial release
- Core mapping functionality
- Task-based state machine
- SimpleMover integration (v1.5.0)
- BeastCombatRoutine integration
