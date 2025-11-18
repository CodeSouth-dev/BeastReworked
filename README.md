# BeastRoutine - Autonomous Beast & Cache Farming Bot

> **NOTE:** BeastPlayerMover has been removed. Use the community **SkillPlayerMover** instead for movement handling.

## Overview
A production-ready, phase-based automation system for hunting beasts and smuggler's caches in Path of Exile. Built as a 2-component system: main bot (IBot) and combat routine (IRoutine). Use SkillPlayerMover (community mover) for movement.

## System Components

### 1. **BeastRoutine** (Main Bot - IBot)
Located in: `Beasts/`
- Autonomous bot that runs independently in DreamPoeBot
- Phase-based state management for clean task execution
- Perception system for game state awareness
- Flask management and loot filtering with poe.ninja integration
- ✅ **Has GUI** for configuring all bot settings

### 2. **BeastCombatRoutine** (Combat Handler - IRoutine)
Located in: `BeastCombatRoutine/`
- Context-aware combat (only fights near objectives)
- Zoom mode when not engaging (skips random encounters)
- Controlled by messages from BeastRoutine phases
- ✅ **Has GUI** - Configure PrimarySkillSlot (0-12)

### 3. **BeastPlayerMover** (Movement Handler - IPlayerMover)
Located in: `BeastPlayerMover/`
- Movement skill support (Whirling Blades, Shield Charge, Dash, Flame Dash, etc.)
- Intelligent pathfinding with ExilePather integration
- Stuck detection and blacklisting
- ✅ **Has GUI** - Configure all movement skills with checkboxes and distance sliders

## Recent Changes & Fixes

### Latest Updates (Nov 2025)
- ✅ Fixed all compilation errors in CombatSettings
- ✅ Added missing properties for combat behavior (skill slots, attack modes, movement skills)
- ✅ Fixed BeastPlayerMover MouseManager errors
- ✅ Fixed IPlayerMover usage (MoveTo → MoveTowards)
- ✅ Proper async/await patterns
- ✅ Added GUI support for BeastCombatRoutine (skill slot selector)
- ✅ Added GUI support for BeastPlayerMover (movement skill configuration)

### Architecture
```
┌─────────────────────────────────────────┐
│         BeastRoutine (IBot)             │
│  ┌───────────────────────────────────┐  │
│  │   Phase-Based State Controller    │  │
│  │  - CaptureBeast, Combat, Loot,    │  │
│  │    Exploration, Exit, etc.        │  │
│  └───────────────────────────────────┘  │
│              ↓        ↓                  │
│      ┌───────────┐  ┌──────────────┐    │
│      │ Perception│  │ Flask Mgmt   │    │
│      └───────────┘  └──────────────┘    │
└──────────┬──────────────────┬───────────┘
           │                  │
           ↓                  ↓
┌──────────────────┐  ┌─────────────────┐
│ BeastCombatRoutine│  │ BeastPlayerMover│
│    (IRoutine)     │  │  (IPlayerMover) │
│                   │  │                 │
│ - Context-aware   │  │ - Movement      │
│   combat          │  │   skills        │
│ - Zoom mode       │  │ - Pathfinding   │
└──────────────────┘  └─────────────────┘
```

## Project Structure

```
Beasts/
├── Beasts/                                  # Main bot (IBot)
│   ├── Core/
│   │   ├── BeastRoutine.cs                 # Main bot class
│   │   ├── StateController.cs              # Phase orchestration
│   │   ├── PhaseRegistry.cs                # Phase registration
│   │   ├── GameContext.cs                  # Game state data
│   │   ├── Logger.cs                       # Logging utilities
│   │   └── 3rdparty.json                   # DreamPoeBot manifest
│   │
│   ├── Perception/                         # Game state awareness
│   │   ├── PerceptionManager.cs            # Orchestrates perception
│   │   ├── BeastDetector.cs                # Beast detection
│   │   ├── CacheDetector.cs                # Cache detection
│   │   ├── CombatPerception.cs             # Enemy detection
│   │   ├── LootPerception.cs               # Loot filtering
│   │   └── PlayerPerception.cs             # Player state
│   │
│   ├── Phases/                             # Execution phases
│   │   ├── IPhase.cs                       # Phase interface
│   │   ├── ExplorationPhase.cs             # Map exploration
│   │   ├── CaptureBeastPhase.cs            # Beast killing/capture
│   │   ├── CombatPhase.cs                  # Combat handler
│   │   ├── LootingPhase.cs                 # Item pickup
│   │   └── ExitPhase.cs                    # Portal/exit logic
│   │
│   ├── Services/                           # External integrations
│   │   ├── PoeNinjaService.cs              # Price fetching
│   │   ├── PoeNinjaModels.cs               # API models
│   │   └── FlaskManager.cs                 # Flask automation
│   │
│   ├── Configuration/                      # Settings
│   │   ├── BeastRoutineSettings.cs         # Main settings
│   │   ├── BeastCaptureSettings.cs         # Beast filters
│   │   ├── CacheSettings.cs                # Cache config
│   │   ├── CombatSettings.cs               # Combat behavior
│   │   ├── LootSettings.cs                 # Loot filters
│   │   └── ExitConditions.cs               # Exit triggers
│   │
│   └── GUI/
│       └── BeastRoutineGui.cs              # Settings GUI
│
├── BeastCombatRoutine/                     # Combat routine (IRoutine)
│   ├── BeastCombatRoutine.cs               # Main combat class
│   ├── BeastCombatRoutineSettings.cs       # Settings (skill slot)
│   └── 3rdparty.json                       # DreamPoeBot manifest
│
└── BeastPlayerMover/                       # Movement (IPlayerMover)
    ├── BeastPlayerMover.cs                 # Main mover class
    ├── BeastPlayerMoverSettings.cs         # Movement config
    ├── Helpers/
    │   ├── SkillsHelpers.cs                # Movement skill logic
    │   ├── MoveHelper.cs                   # Range calculations
    │   └── MovementSkillConfig.cs          # Skill defaults
    └── 3rdparty.json                       # DreamPoeBot manifest
```

## Installation

1. **Build the solution**:
   ```bash
   dotnet build Beasts.sln /p:Configuration=Release
   # or use Visual Studio/Rider
   ```

2. **Copy to DreamPoeBot** (3rdparty system automatically loads from source):
   ```
   DreamPoeBot/3rdParty/_CONFIGS_/Production/
   ├── Beasts/                      # Copy entire Beasts/ folder
   ├── BeastCombatRoutine/          # Copy entire BeastCombatRoutine/ folder
   └── BeastPlayerMover/            # Copy entire BeastPlayerMover/ folder
   ```

3. **Configure in DreamPoeBot**:
   - **Bot**: Select "BeastRoutine"
   - **Routine**: Select "BeastCombatRoutine"
   - **PlayerMover**: Select "BeastPlayerMover"
   - Configure settings in BeastRoutine GUI

## Configuration

### Main Bot Settings (BeastRoutine GUI)

#### Beast Capture
- **Enable Beast Farming**: Hunt and capture beasts
- **Capture by Rarity**: Unique/Rare/Magic/Normal
- **Detection Range**: How far to scan (default: 80)
- **Filter Mode**: Whitelist/Blacklist specific beast families

#### Cache Settings
- **Enable Cache Farming**: Hunt smuggler's caches
- **Detection Range**: Scan range (default: 80)

#### Combat Settings
- **Combat Range**: Max engagement distance (default: 60)
- **Max Melee Range**: Melee attack range (default: 30)
- **Primary Skill Slot**: Which skill to use (0-12)
- **Movement Skill Slot**: Movement skill slot (default: 1)
- **Attack While Moving**: Enable kiting (default: false)
- **Fast Combat Mode**: Optimized attack loop (default: true)

#### Flask Settings
- **Use Life/Mana Flasks**: Auto-use at HP%/Mana% threshold
- **Use Utility Flasks**: Quicksilver, Granite, Jade, etc.
- **Use Offensive Flasks**: Diamond, Silver, Sulphur on rares/bosses
- **Use Tincture**: Auto-activate tinctures

#### Loot Settings with poe.ninja Integration
- **Use poe.ninja Pricing**: Enable dynamic price filtering
- **League**: Your league name (e.g., "Settlers", "Standard")
- **Minimum Chaos Value**: Only pickup items worth X chaos
- **Filter Currency/Uniques/Div Cards**: Apply filtering by category

### Combat Routine Settings (BeastCombatRoutine)
**GUI Available**: Configure via DreamPoeBot UI when BeastCombatRoutine is selected as Routine

Settings:
- **Primary Skill Slot**: Which skill slot to use for attacking
  - 0 = Q, 1 = W, 2 = E, 3 = R, 4 = T
  - 5-12 = Additional skill slots (if available)

Alternatively, edit `BeastCombatRoutineSettings.json`:
```json
{
  "PrimarySkillSlot": 0
}
```

### Player Mover Settings (BeastPlayerMover)
**GUI Available**: Configure via DreamPoeBot UI when BeastPlayerMover is selected as PlayerMover

Settings in GUI:
- **Basic Configuration**:
  - MoveRange: Distance filter for path points (default: 33)
  - SingleUseDistance: Distance for single-click vs hold (default: 18)
  - Min Mana %: Mana threshold for movement skills
  - Use Blood Magic: Enable if using Blood Magic support

- **Movement Skills** (each with enable checkbox + min/max distance):
  - Whirling Blades
  - Shield Charge
  - Leap Slam
  - Dash
  - Flame Dash
  - Frostblink
  - Lightning Warp
  - Blink Arrow
  - Phase Run
  - Withering Step

Alternatively, edit `BeastPlayerMoverSettings.json`:
```json
{
  "PathRefreshRateMs": 32,
  "MoveRange": 33,
  "SingleUseDistance": 18,
  "SkillConfigs": {
    "Whirling Blades": { "Enabled": true, "MinDistance": 10, "MaxDistance": 50 },
    "Shield Charge": { "Enabled": true, "MinDistance": 10, "MaxDistance": 50 },
    "Dash": { "Enabled": true, "MinDistance": 10, "MaxDistance": 40 },
    "Flame Dash": { "Enabled": true, "MinDistance": 10, "MaxDistance": 50 }
  }
}
```

## How It Works

### Main Loop
```
DreamPoeBot calls BeastRoutine.Tick()
    ↓
MainCoroutine() executes
    ↓
1. Perception scans game state
   - Detect beasts (BeastDetector)
   - Detect caches (CacheDetector)
   - Find enemies (CombatPerception)
   - Check loot (LootPerception)
   - Monitor player (PlayerPerception)
    ↓
2. StateController selects active phase
   Priority order:
   - Exit (inventory full, low resources)
   - CaptureBeast (beast nearby)
   - Combat (enemies near objective)
   - Looting (valuable items)
   - Exploration (default: roam zone)
    ↓
3. Phase.Execute() performs action
   - Sends messages to BeastCombatRoutine (EnableCombat/DisableCombat)
   - Uses BeastPlayerMover for movement (MoveTowards)
   - Manages flasks via FlaskManager
    ↓
4. Repeat each tick (30ms)
```

### Combat Flow
```
CombatPhase detects enemy near beast/cache
    ↓
Sends "EnableCombat" message to BeastCombatRoutine
    ↓
BeastCombatRoutine.DoCombat()
   - Only engages if _shouldEngage = true
   - Uses PrimarySkillSlot to attack
   - Respects leash range (only fights near objective)
    ↓
Phase complete → Sends "DisableCombat"
    ↓
BeastCombatRoutine enters zoom mode (ignores enemies)
```

### Movement Flow
```
Phase calls PlayerMoverManager.Current.MoveTowards(position)
    ↓
BeastPlayerMover.MoveTowards()
   - Generates path with ExilePather
   - Checks if movement skill available
   - If distance > SingleUseDistance:
      → Try movement skill (Whirling Blades, etc.)
      → Fall back to walking if skill unavailable
   - Handles stuck detection
    ↓
Returns true if movement initiated
```

## Phase Priority

Phases are evaluated in priority order each tick:

1. **ExitPhase** - Critical exit conditions
   - Inventory full
   - Low resources
   - Exit conditions met

2. **CaptureBeastPhase** - Beast detected nearby
   - Enables combat at beast position
   - Fights until beast is dead
   - Einhar auto-captures when beast dies

3. **CombatPhase** - Enemies near objective
   - Only fights when near beast/cache
   - Uses movement skills for repositioning
   - Fast combat mode for high APS builds

4. **LootingPhase** - Valuable items on ground
   - poe.ninja price filtering
   - Pickup based on chaos value threshold

5. **ExplorationPhase** - Default roaming
   - Explores map looking for mechanics
   - Uses ExilePather for pathfinding

## Features

### ✅ Implemented
- Complete phase-based architecture
- Beast detection and capture
- Cache detection
- Context-aware combat (zoom mode)
- Movement skill support (all major skills)
- Flask automation (life, mana, utility, offensive)
- Tincture support
- poe.ninja integration for loot filtering
- Intelligent pathfinding with stuck detection
- **Settings GUIs for all 3 components** (BeastRoutine, BeastCombatRoutine, BeastPlayerMover)

### 🚧 Future Enhancements
- Advanced exploration algorithms
- Map automation (stash, vendor, re-enter)
- Delve integration
- Better beast family filtering

## Debugging

Enable debug logging to see detailed output:

```
[BeastRoutine] State: Exploration
[Perception] Found beast: Red Beast (Distance: 45, Rarity: Rare)
[StateController] Phase transition: Exploration → CaptureBeast
[CaptureBeast] Engaging beast: Red Beast (HP: 100%)
[Combat] Fighting Red Beast (3 attacks)
[CaptureBeast] Beast killed - Einhar will capture it
[StateController] Phase transition: CaptureBeast → Exploration
```

## Troubleshooting

### Bot not fighting anything
- Check that BeastRoutine is selected as Bot
- Check that BeastCombatRoutine is selected as Routine
- Verify PrimarySkillSlot is set to correct skill

### Movement not working
- Check that BeastPlayerMover is selected as PlayerMover
- Verify Move skill is bound to skillbar
- Check movement skill settings in JSON

### Not picking up loot
- Disable poe.ninja filtering to test
- Check MinimumChaosValue threshold
- Verify league name matches poe.ninja exactly

### Compilation errors
- Make sure all using statements include DreamPoeBot.BotFramework
- Verify CombatSettings has all required properties
- Check that IPlayerMover uses MoveTowards (not MoveTo)

## Contributing

The codebase is well-structured for contributions:
- Add new phases in `Phases/`
- Add new perception modules in `Perception/`
- Add new services in `Services/`
- Settings changes in `Configuration/`

## License

[Your License Here]

## Credits

Built for DreamPoeBot framework for Path of Exile automation.
