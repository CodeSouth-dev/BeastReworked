# BeastMover - Custom Player Mover

## What is BeastMover?

BeastMover is a custom player mover plugin specifically designed for the Beast bot. It provides simpler, more reliable movement than SkillPlayerMover, especially in hideouts.

## Why Create BeastMover?

The Beast bot was experiencing movement issues where it would:
- Get stuck at the map device
- Not move to stash when needed
- Have problems with PlayerMoverManager integration

BeastMover solves these issues by providing a dedicated, optimized movement solution.

## Key Features

✅ **Simple & Reliable** - No complex configuration needed
✅ **Hideout Optimized** - Works perfectly with stash and map device
✅ **Stuck Detection** - Automatically detects and recovers from stuck situations
✅ **Integrated** - Works seamlessly with all Beast bot phases
✅ **No Skills Required** - Pure walking movement, no movement skills needed

## How to Use

1. **Enable in Bot Settings**:
   ```
   Settings → Player Mover → Select "BeastMover" → Restart Bot
   ```

2. **That's it!** - No configuration needed

## File Structure

```
BeastMover/
├── BeastMover.cs        # Main IPlayerMover implementation
├── 3rdparty.json        # Plugin metadata for DreamPoeBot
├── README.md            # Detailed documentation
└── SETUP.md            # Setup and troubleshooting guide
```

## Integration with Beast Bot

All Beast bot phases automatically use the configured player mover:
- `OpenMapPhase` - Uses it for moving to map device and stash
- `PreparationPhase` - Uses it for moving to stash for portal scrolls
- `ExitAndStashPhase` - Uses it for returning from maps
- All other phases - Automatic integration via `PlayerMoverManager`

## Technical Details

- **Implements**: `IPlayerMover` interface
- **Uses**: `ExilePather` for pathfinding
- **Path Refresh**: Every 1000ms or when needed
- **Stuck Check**: Every 2000ms with auto-recovery
- **Move Range**: 15f in hideout/town, 20f in maps

## When to Use BeastMover vs SkillPlayerMover

| Scenario | Recommended Mover |
|----------|-------------------|
| Beast bot (hideout only) | **BeastMover** ✅ |
| Mapping with combat | SkillPlayerMover |
| Need speed (movement skills) | SkillPlayerMover |
| Need reliability | **BeastMover** ✅ |
| Minimal configuration | **BeastMover** ✅ |

## Troubleshooting

See `BeastMover/SETUP.md` for detailed troubleshooting steps.

Quick fixes:
- **Not moving?** - Check that BeastMover is selected and bot is restarted
- **Still stuck?** - Check hideout has clear paths to stash/device
- **Want speed?** - Use SkillPlayerMover with configured movement skills

## Development Notes

BeastMover wraps around the core movement logic and integrates with:
- `PlayerMoverManager` - Bot's player mover management system
- `ExilePather` - Core pathfinding engine
- `LokiPoe.MyPosition` - Player position tracking
- `PlayerMover.MoveTowards()` - Low-level movement command

All Beast bot phases use `PlayerMoverManager.Current.MoveTowards()` which delegates to the active player mover (BeastMover when configured).

