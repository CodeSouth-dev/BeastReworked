# BeastMover Logging Guide

## ✅ BeastMover Logs ARE Visible!

Unlike Beast bot phases, BeastMover uses the proper logger that shows up in DreamPoeBot:

```csharp
public static readonly ILog Log = Logger.GetLoggerInstanceForType();
```

All logging statements (`Log.InfoFormat`, `Log.WarnFormat`, `Log.ErrorFormat`) will appear in the bot logs.

## What You'll See in the Logs

### On Bot Startup
```
[BeastMover] Initialized - Version 1.0.0
[BeastMover] Enabled and ready for movement
```

### During Normal Movement
```
[BeastMover] Generating path from (2345, 6789) to (3456, 7890) (distance: 54.3m)
[BeastMover] Path generated with 15 waypoints
[BeastMover] Moving towards (2360, 6805) (current: (2345, 6789), distance: 54.3m, waypoints left: 15)
[BeastMover] Already at destination (3.2m away)
```

### When Stuck is Detected
```
[BeastMover] Stuck check #1: Only moved 2.3m in 2 seconds
[BeastMover] Stuck check #2: Only moved 1.8m in 2 seconds
[BeastMover] Stuck check #3: Only moved 0.5m in 2 seconds
[BeastMover] Player appears stuck at (2345, 6789), clearing path and regenerating
[BeastMover] Generating path from (2345, 6789) to (3456, 7890) (distance: 54.1m)
[BeastMover] Movement recovered, moved 18.7m
```

### Error Cases
```
[BeastMover] ExilePather.FindPath failed from (2345, 6789) to (3456, 7890)
[BeastMover] Received 0,0 as position, returning false
```

## How to Use Logs for Debugging

### Problem: Bot Not Moving

**Check for initialization:**
- Look for `[BeastMover] Initialized` - if not present, BeastMover isn't loaded
- Look for `[BeastMover] Enabled` - if not present, BeastMover isn't active

**Check for movement attempts:**
- Look for `[BeastMover] Generating path` - if present, pathfinding is working
- Look for `[BeastMover] Moving towards` - if present, movement commands are being sent
- If NO movement logs appear, the bot isn't calling the mover (phase issue)
- If logs appear but character doesn't move, it's a game/client issue

### Problem: Bot Getting Stuck

**Check stuck detection:**
- Look for `[BeastMover] Stuck check #N` - stuck detection is working
- If stuck checks appear but no recovery, increase stuck threshold
- If no stuck checks appear, the mover isn't being called regularly

**Check path regeneration:**
- Look for `Player appears stuck at...` followed by `Generating path`
- If regeneration happens but still stuck, it's a pathing issue (obstacles/walls)
- Consider redesigning hideout or using SkillPlayerMover with dash skills

### Problem: Paths Failing

**Check pathfinding errors:**
- Look for `ExilePather.FindPath failed`
- This usually means target is unreachable or on unwalkable terrain
- Check hideout layout - ensure clear paths between stash and map device
- Check target coordinates - they should be valid positions

### Problem: Too Much Logging

**Reduce log frequency:**
Currently BeastMover logs every movement tick. If this is too verbose:
1. Comment out the `Log.InfoFormat` in the main movement loop (line ~164)
2. Keep initialization, stuck detection, and error logs
3. Rebuild the plugin

**Which logs to keep:**
- ✅ Keep: Initialization, errors, stuck detection
- ⚠️ Optional: Path generation
- ❌ Remove if too verbose: Every movement tick

## Comparing with SkillPlayerMover Logs

BeastMover uses the same logging pattern as SkillPlayerMover:

**SkillPlayerMover:**
```
[SkillPlayerMover] Using skill Flame Dash
[SkillPlayerMover] Adjustments being made!
```

**BeastMover:**
```
[BeastMover] Generating path from X to Y
[BeastMover] Moving towards X
```

Both will show up in the same log view, making it easy to see which mover is active.

## Log Files Location

Logs are typically found in:
```
DreamPoeBot/Logs/
```

Look for files named like:
```
Log_2025-11-18.txt
```

Use text search (Ctrl+F) to find `[BeastMover]` entries.

