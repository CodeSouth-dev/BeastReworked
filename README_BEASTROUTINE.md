# BeastRoutine - Complete Skeleton

## Overview
A clean, phase-based automation framework for hunting beasts and smuggler's caches in Path of Exile.

## Architecture

### **No State Machine** - Uses Priority-Based Phase Selection

The routine evaluates game state each tick and executes the highest priority phase. This provides:
- ✅ Natural integration with DreamPoeBot's Logic() system
- ✅ Self-healing (re-evaluates every tick, can't get stuck)
- ✅ Clean, maintainable code
- ✅ Easy to extend

## Project Structure

```
Beasts/
├── Core/                                    # Main routine logic
│   ├── BeastRoutine.cs                     # IRoutine implementation (entry point)
│   ├── MechanicHunterState.cs              # State enum for tracking
│   ├── GameContext.cs                      # Perception data container
│   └── 3rdparty.json                       # DreamPoeBot plugin manifest
│
├── Perception/                              # Game state awareness
│   ├── IPerceptionModule.cs                # Base interface
│   ├── PerceptionManager.cs                # Orchestrates all perception
│   ├── BeastDetector.cs                    # Finds and prioritizes beasts
│   ├── CacheDetector.cs                    # Finds smuggler's caches
│   ├── CombatPerception.cs                 # Enemy detection & targeting
│   ├── LootPerception.cs                   # Item value calculation (poe.ninja integration)
│   └── PlayerPerception.cs                 # Player state monitoring
│
├── Services/                                # External API integrations
│   ├── PoeNinjaModels.cs                   # Data models for poe.ninja API
│   └── PoeNinjaService.cs                  # Price fetching and caching service
│
├── Phases/                                  # Execution phases (behavior modes)
│   ├── IPhase.cs                           # Base phase interface
│   ├── ExplorationPhase.cs                 # Roam zone looking for mechanics
│   ├── ApproachTargetPhase.cs              # Navigate to beast/cache
│   ├── ClearAreaPhase.cs                   # Kill guards around mechanics
│   ├── CaptureBeastPhase.cs                # Kill beast to capture (Einhar auto-captures)
│   ├── OpenCachePhase.cs                   # Interact with cache
│   ├── CombatPhase.cs                      # Normal combat
│   ├── LootingPhase.cs                     # Pick up items
│   └── ExitPhase.cs                        # Portal out
│
├── Configuration/                           # Settings classes
│   ├── BeastRoutineSettings.cs             # Main settings container
│   ├── BeastCaptureSettings.cs             # Beast filtering/capture config
│   ├── CacheSettings.cs                    # Cache detection config
│   ├── CombatSettings.cs                   # Combat behavior config
│   ├── LootSettings.cs                     # Loot filtering config
│   └── ExitConditions.cs                   # When to leave zone
│
└── GUI/                                     # WPF interface
    ├── BeastRoutineGui.xaml                # GUI layout
    ├── BeastRoutineGui.xaml.cs             # Code-behind
    └── BeastRoutineGui.g.cs                # Auto-generated XAML connector
```

## How It Works

### Main Loop Flow

```
DreamPoeBot calls Logic() every tick
    ↓
BeastRoutine.Logic()
    ↓
1. PerceptionManager builds GameContext
    - Scan for beasts
    - Scan for caches
    - Check enemies
    - Check loot
    - Check player state
    ↓
2. DeterminePhase() selects highest priority phase
    - Exit (inventory full, low resources)
    - Capture Beast (beast at low HP, ready to kill)
    - Open Cache (in range, no enemies)
    - Clear Area (enemies blocking mechanic)
    - Approach Target (mechanic detected)
    - Looting (valuable items)
    - Combat (random enemies)
    - Exploration (default: roam zone)
    ↓
3. Phase.Execute() performs action
    ↓
4. Return LogicResult.Provided
```

### Phase Priority (Highest to Lowest)

1. **Exit** - Critical conditions (inventory full, low resources)
2. **Capture Beast** - Beast ready (low HP, finish killing to capture)
3. **Open Cache** - Cache in range, no enemies
4. **Clear Area** - Enemies blocking mechanic
5. **Approach Target** - Mechanic detected but not in range
6. **Looting** - Valuable items on ground
7. **Combat** - Random enemies while exploring
8. **Exploration** - Default: roam looking for mechanics

## Files Created

### Core (3 files)
- BeastRoutine.cs - Main routine class
- MechanicHunterState.cs - State tracking enum
- GameContext.cs - Data containers

### Perception (7 files)
- IPerceptionModule.cs
- PerceptionManager.cs
- BeastDetector.cs
- CacheDetector.cs
- CombatPerception.cs
- LootPerception.cs
- PlayerPerception.cs

### Phases (9 files)
- IPhase.cs
- ExplorationPhase.cs
- ApproachTargetPhase.cs
- ClearAreaPhase.cs
- CaptureBeastPhase.cs
- OpenCachePhase.cs
- CombatPhase.cs
- LootingPhase.cs
- ExitPhase.cs

### Configuration (6 files)
- BeastRoutineSettings.cs
- BeastCaptureSettings.cs
- CacheSettings.cs
- CombatSettings.cs
- LootSettings.cs (now with poe.ninja integration)
- ExitConditions.cs

### Services (2 files)
- PoeNinjaModels.cs
- PoeNinjaService.cs

### GUI (3 files)
- BeastRoutineGui.xaml
- BeastRoutineGui.xaml.cs
- BeastRoutineGui.g.cs

**Total: 30 files** (28 original + 2 new service files)

## What's Implemented

✅ Complete architecture
✅ All classes with proper structure
✅ Interface definitions
✅ Phase selection logic
✅ Perception framework
✅ Settings system
✅ GUI with all controls
✅ **poe.ninja integration for intelligent loot filtering** (NEW!)

## What Needs Implementation (TODOs)

The skeleton is complete, but some functionality needs actual API calls:

### 1. **Beast Capture** (CaptureBeastPhase.cs)
```csharp
// Note: Einhar captures beasts automatically when killed
// Just need to kill the beast - no nets required
// The phase handles combat until beast is dead
```

### 2. **Beast Detection** (BeastDetector.cs)
```csharp
// TODO: Verify beast detection logic
// May need to refine:
// - How to detect if monster is a beast
// - BestiaryItemisedComponent check
// - Beast family/genus filtering
```

### 3. **Inventory Management** (PlayerPerception.cs)
```csharp
// TODO: Implement inventory checking
// - CheckInventoryFull()
```

### 4. **Portal/Exit Logic** (ExitPhase.cs)
```csharp
// TODO: Implement portal creation
// - Use portal scroll
// - Click on portal
// - Enter town
```

### 5. **Exploration Algorithm** (ExplorationPhase.cs)
```csharp
// TODO: Improve exploration
// Could add:
// - Grid-based tracking
// - Zone layout analysis
// - Minimap integration
```

## How to Test

1. **Build the project**
   ```bash
   # In Visual Studio or from command line
   msbuild Beasts.sln /p:Configuration=Release
   ```

2. **Copy to DreamPoeBot**
   ```
   Copy Beasts/bin/Release/Beasts.dll to DreamPoeBot/Plugins/
   Copy Core/3rdparty.json to the same location
   ```

3. **Configure in DreamPoeBot**
   - Start DreamPoeBot
   - Go to Settings → Routines
   - Select "BeastRoutine"
   - Configure settings in the GUI
   - Set Primary Skill Slot (which skill to use for combat)
   - Enable Beast Capture and/or Cache farming

4. **Test in Game**
   - Enter a map with beasts/caches
   - Start the bot
   - Watch the log for phase transitions

## Configuration Guide

### Beast Capture Settings
- **Enable Beast Farming**: Turn on/off beast hunting
- **Capture Unique/Rare/Magic/Normal**: Which rarities to capture
- **Capture HP Threshold**: HP% to finish killing beast for capture (default 10%)
- **Detection Range**: How far to scan for beasts (default 80)

### Cache Settings
- **Enable Cache Farming**: Turn on/off cache hunting
- **Detection Range**: How far to scan for caches (default 80)

### Combat Settings
- **Combat Range**: Max distance to engage enemies (default 60)
- **Max Melee Range**: Attack range for melee skills (default 30)
- **Primary Skill Slot**: Which skill bar slot to use (1-8)
- **Always Attack in Place**: Prevent movement while attacking

### Loot Settings
- **Pickup Currency/Blueprints/Contracts/etc**: What to loot
- **Loot Range**: Max distance to pick up items (default 40)

#### poe.ninja Integration (NEW!)
The bot now integrates with poe.ninja API to filter loot based on real-time market prices:

- **Use poe.ninja Pricing**: Enable/disable dynamic price-based filtering (default: true)
- **League**: Set your league - use the league name from poe.ninja (e.g., "Keepers", "Standard", "Hardcore"). Default: "Keepers"
- **Minimum Chaos Value**: Only pick up items worth at least this many chaos (default: 1.0)
- **Cache Refresh Minutes**: How often to update prices from poe.ninja (default: 60)

**Filtering Options:**
- **Filter Currency**: Apply poe.ninja filtering to currency (default: true)
- **Filter Uniques**: Apply poe.ninja filtering to unique items (default: true)
- **Filter Divination Cards**: Apply poe.ninja filtering to div cards (default: true)
- **Filter Other Items**: Apply filtering to scarabs, fossils, essences, etc. (default: true)

**How it works:**
1. On routine start, fetches current pricing data from poe.ninja
2. Caches prices locally (refreshes every hour by default)
3. Checks each item against poe.ninja database
4. Only picks up items that meet the minimum chaos value threshold
5. Falls back to basic filters if item not found in poe.ninja database

**Supported Item Types:**
- Currency & Fragments
- Unique items (weapons, armour, accessories, jewels, flasks, maps)
- Divination cards
- Scarabs, Fossils, Resonators, Essences
- Oils, Incubators, Delirium Orbs
- Omens, Vials, Allflame Embers

### Exit Conditions
- **Exit on Inventory Full**: Leave when no space
- **Exit on Low Resources**: Leave when HP/flasks low

## Next Steps

### Priority 1: Get Basic Combat Working
1. Test if combat works with current skill slot setup
2. Verify movement works (PlayerMoverManager.Current.MoveTowards)
3. Test exploration (does it roam the zone?)

### Priority 2: Implement Beast Detection
1. Verify `IsBeast()` logic works
2. Test beast priority selection
3. Check distance calculations

### Priority 3: Implement Beast Capture
1. Verify beast is captured automatically when killed
2. Test HP threshold detection
3. Ensure combat continues until beast is dead

### Priority 4: Implement Cache Opening
1. Verify cache detection metadata
2. Test InteractWith() on caches
3. Implement loot pickup

### Priority 5: Polish
1. Improve exploration algorithm
2. Add flask usage
3. Add kiting logic
4. Improve exit conditions

## Debugging Tips

The routine logs extensively. Look for:
```
[BeastRoutine] Starting...
[Phase Transition] Exploration → ApproachTarget
[BeastDetector] Found beast: Red Beast (Rarity: Rare, Distance: 45)
[Combat] Engaging Rare Monster (Rarity: Rare, Distance: 25)
```

Enable debug logging to see perception data:
- What beasts/caches are detected
- What enemies are nearby
- Distance calculations
- Movement decisions

## Questions to Answer

1. **What `logic.Id` values exist besides "hook_combat"?**
   - Check if there's "hook_task", "hook_movement", etc.

2. **How to detect beasts accurately?**
   - Is `BestiaryItemisedComponent` the right way?
   - Are there other identifiers?

3. **How does the framework handle zones?**
   - Do we need to manually enter maps?
   - Can we automate waypoint usage?

## Summary

You now have a **complete, production-ready skeleton** for a mechanic farming routine!

The architecture is:
- ✅ Clean and maintainable
- ✅ Easy to extend
- ✅ Well-documented
- ✅ Following best practices

All that's left is filling in the API-specific details (marked with TODO comments) once you verify how the DreamPoeBot API works.
