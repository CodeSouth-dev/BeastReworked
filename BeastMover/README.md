# BeastMover v1.3.0

A custom player mover for the Beast bot with **movement skill support** and optimized pathfinding.

## Features

- ✅ **Movement Skills**: Automatically uses Whirling Blades, Frostblink, Flame Dash, Shield Charge, Leap Slam, Dash, etc!
- ✅ **Smart Detection**: Checks skill availability, cooldowns, and mana before using
- ✅ **Portal Avoidance**: Prevents premature portal navigation until exit phase
- ✅ **Optimized Pathfinding**: Uses `PathExistsBetween` and `IsWalkable` for better performance
- ✅ **Direct Path Fallback**: Automatically tries direct paths when normal pathfinding fails
- ✅ **Proper Logging**: Uses `Logger.GetLoggerInstanceForType()` so logs appear in DreamPoeBot
- ✅ **Stuck Detection**: Automatically detects and recovers from stuck situations
- ✅ **Path Regeneration**: Periodically refreshes paths for optimal routing
- ✅ **JSON Configuration**: Easy to configure via `BeastMover.json` settings file
- ✅ **Clean Logs**: Minimal spam - only errors and important events logged

## Supported Movement Skills

BeastMover will automatically detect and use these movement skills if they're on your skill bar:

- **Whirling Blades** (highest priority - very fast!)
- **Frostblink**
- **Flame Dash**
- **Shield Charge**
- **Leap Slam**
- **Dash**
- **Lightning Warp**
- **Blink Arrow**
- **Phase Run** (activates buff, then uses basic movement)
- **Bodyswap**

Skills are checked in priority order. The first available skill will be used!

## How It Works

`BeastMover` provides intelligent movement for the Beast bot:

1. **Path Generation**: Uses `ExilePather.FindPath()` to create efficient paths
2. **Movement Skill Check**: Checks if any movement skills are available and ready
3. **Skill Usage**: If a skill is ready and distance > 30m, uses the movement skill
4. **Fallback to Walking**: If no skills available, falls back to basic click-to-move
5. **Direct Path Fallback**: If pathfinding fails, tries `PathExistsBetween()` for direct routing
6. **Stuck Recovery**: Regenerates path if player hasn't moved in 2 seconds

## Configuration

Edit `%USERPROFILE%\Documents\dreamboeat\DreamPoeBot\Settings\<CharacterName>\BeastMover.json`:

```json
{
  "PathRefreshRate": 1000,
  "DebugLogging": false,
  "StuckThreshold": 3,
  "StuckDistance": 5.0,
  "SingleUseDistance": 30,
  "AllowPortalMovement": false
}
```

### Settings Explained

- **PathRefreshRate**: How often to regenerate paths (ms). Lower = more responsive but more CPU
- **DebugLogging**: Enable detailed movement logs (useful for debugging, but very spammy!)
- **StuckThreshold**: Number of stuck checks before forcing path regeneration
- **StuckDistance**: Distance in meters to consider as "not moving"
- **SingleUseDistance**: Distance threshold for using movement skills (30m default)
- **AllowPortalMovement**: Allow movement toward portals. Keep false to prevent premature map exits. Only set to true when you want the bot to exit maps (inventory full or map completed).

## Installation

1. Place `BeastMover` folder in `DreamPoeBot/3rdParty/_CONFIGS_/Production/`
2. Restart DreamPoeBot
3. Select "BeastMover" as your Player Mover
4. Make sure you have movement skills on your skill bar (Q, W, E, R, T)!

## Movement Skill Priority

BeastMover prioritizes movement skills based on speed and reliability:

1. **Whirling Blades** - Fastest, no targeting needed
2. **Frostblink** - Instant teleport
3. **Flame Dash** - Fast blink
4. **Shield Charge** - Charge skills
5. **Leap Slam** - Can jump over obstacles
6. **Dash** - Dash skills
7. **Lightning Warp** - Warp skills
8. **Blink Arrow** - Arrow blinks
9. **Phase Run** - Buff only
10. **Bodyswap** - Requires corpses

## Logging

All movement logs use the proper `Logger.GetLoggerInstanceForType()` so they appear in DreamPoeBot's log window:

```
[BeastMover] Initialized - Version 1.2.0
[BeastMover] Cannot path from {526, 2141} to {457, 2228}
```

## Troubleshooting

**Movement skills not being used:**
- Check that skills are bound to your skill bar (Q/W/E/R/T)
- Verify you have enough mana (needs > 10%)
- Make sure skills are not on cooldown
- Ensure distance to target is > 30m (configurable via `SingleUseDistance`)

**Bot getting stuck:**
- Try lowering `StuckThreshold` to 2 for more aggressive recovery
- Enable `DebugLogging` to see detailed movement logs (WARNING: very verbose!)
- Check if your character has movement skills available

**Pathfinding errors:**
- `Cannot path` errors are normal - the bot tries a direct path fallback
- If you see many errors, the destination may be unreachable
- Try increasing `PathRefreshRate` or lowering `StuckThreshold`

**Skills not ready:**
- Cooldowns - wait for skills to come off cooldown
- Mana - make sure you have > 10% mana
- Skill availability - check skill bar bindings

**Bot goes to portal too early:**
- Set `AllowPortalMovement` to `false` to prevent portal navigation (default)
- Only set `AllowPortalMovement` to `true` when you want the bot to exit maps
- The bot will only exit when ExitPhase conditions are met (inventory full or map completed)
- If you see "Avoiding portal movement" in logs, the bot is correctly avoiding premature exits

## Performance Tips

- **Default `PathRefreshRate` (1000ms)** is good for most cases
- **Lower to 500ms** if bot seems sluggish or unresponsive
- **Raise to 2000ms** if experiencing lag or high CPU usage
- **Disable `DebugLogging`** unless actively debugging - it generates a LOT of logs
- **Single Use Distance (30m)** - increase if you want more frequent skill usage

## Comparison with SkillPlayerMover

| Feature | BeastMover v1.2.0 | SkillPlayerMover |
|---------|-------------------|------------------|
| Movement Skills | ✅ Yes (10 skills) | ✅ Yes (more) |
| Skill Priority | ✅ Hardcoded (speed-based) | ✅ Configurable |
| Path Optimization | ✅ Yes | ✅ Yes |
| GUI Configuration | ❌ No (JSON only) | ✅ Yes |
| Complexity | 🟢 Simple | 🟡 Advanced |
| Best For | 🎯 Beast bot | 🗺️ General use |

## Version History

### v1.3.0
- 🔒 **Added portal avoidance control** with `AllowPortalMovement` setting
- Prevents premature map exits - only allows portal movement when explicitly enabled
- Perfect for inventory full or map completed exit conditions
- Keeps bot in maps longer for better farming

### v1.2.0
- ✨ **Added movement skill support!** (Whirling Blades, Frostblink, Flame Dash, etc.)
- Skill detection based on availability, cooldowns, and mana
- Automatic fallback to basic movement when skills unavailable
- Simple implementation without external helper dependencies

### v1.1.0
- Added optimized pathfinding with `PathExistsBetween` and `IsWalkable`
- Added direct path fallback when pathfinding fails
- Removed verbose logging spam
- Simplified to basic movement only (no skill dependencies)
- Better stuck detection and recovery

### v1.0.0
- Initial release with basic movement
- Proper DreamPoeBot logging integration
- JSON configuration support
