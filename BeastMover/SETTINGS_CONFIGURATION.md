# BeastMover Settings Configuration

## No GUI (For Now)

BeastMover currently runs **without a GUI** to avoid XAML compilation issues. Settings are still fully functional via JSON configuration!

## How to Configure

Settings are automatically created in:
```
DreamPoeBot/Settings/{CharacterName}/BeastMover.json
```

### Default Settings

When you first run BeastMover, it will create a JSON file with these defaults:

```json
{
  "PathRefreshRate": 1000,
  "DebugLogging": true,
  "StuckThreshold": 3,
  "StuckDistance": 5.0
}
```

### Settings Explained

#### PathRefreshRate (default: 1000)
- **Type**: Integer (milliseconds)
- **Description**: How often to recalculate paths
- **Recommended**: 1000ms (1 second)
- **Lower values**: More responsive but more CPU usage
- **Higher values**: Less CPU but slower to react

#### DebugLogging (default: true)
- **Type**: Boolean
- **Description**: Enable detailed movement logs
- **true**: Shows all movement, pathfinding, and stuck detection logs
- **false**: Only shows warnings and errors
- **Recommended**: Keep enabled until bot works perfectly, then disable for cleaner logs

#### StuckThreshold (default: 3)
- **Type**: Integer
- **Description**: Number of stuck checks before regenerating path
- **Recommended**: 3 checks (6 seconds total)
- **Lower values**: More aggressive recovery but may cause path spam
- **Higher values**: Less aggressive, may stay stuck longer

#### StuckDistance (default: 5.0)
- **Type**: Float (meters)
- **Description**: Distance moved to be considered "not stuck"
- **Recommended**: 5.0 meters
- **Lower values**: More sensitive stuck detection
- **Higher values**: Less sensitive, only detects severe stucks

## How to Edit Settings

### Option 1: Before First Run
1. Navigate to `DreamPoeBot/Settings/{CharacterName}/`
2. Create `BeastMover.json` with your desired settings
3. Start the bot

### Option 2: After First Run
1. Stop the bot
2. Navigate to `DreamPoeBot/Settings/{CharacterName}/`
3. Open `BeastMover.json` in a text editor
4. Edit the values
5. Save and restart the bot

### Example: Disable Debug Logging

```json
{
  "PathRefreshRate": 1000,
  "DebugLogging": false,
  "StuckThreshold": 3,
  "StuckDistance": 5.0
}
```

### Example: More Aggressive Stuck Detection

```json
{
  "PathRefreshRate": 1000,
  "DebugLogging": true,
  "StuckThreshold": 2,
  "StuckDistance": 3.0
}
```

### Example: Performance Mode (Less CPU)

```json
{
  "PathRefreshRate": 2000,
  "DebugLogging": false,
  "StuckThreshold": 5,
  "StuckDistance": 10.0
}
```

## Settings Take Effect When

- ✅ **Immediately**: DebugLogging (next log message)
- ✅ **Next path generation**: PathRefreshRate
- ✅ **Next stuck check**: StuckThreshold, StuckDistance
- ⚠️ **Restart recommended**: For guaranteed effect

## Verifying Settings Are Loaded

Check logs for:
```
[BeastMover] Initialized - Version 1.0.0
[BeastMover] Enabled and ready for movement
```

If debug logging is enabled, you'll see:
```
[BeastMover] Generating path from X to Y
[BeastMover] Moving towards X
[BeastMover] Stuck check #N
```

If debug logging is disabled, you'll only see:
```
[BeastMover] Player appears stuck at X, clearing path
[BeastMover] No move skill found!
```

## Future: GUI Coming

Once XAML compilation issues are resolved, BeastMover will have a full GUI like SkillPlayerMover. For now, JSON configuration is simple and effective!

## Troubleshooting

**Settings not taking effect:**
- Make sure file is named exactly `BeastMover.json`
- Check JSON syntax (no trailing commas, proper quotes)
- Restart the bot completely

**File not created:**
- Start the bot once with BeastMover enabled
- It will auto-create the file with defaults
- Then you can edit it

**Invalid values:**
- PathRefreshRate: Must be positive integer (500-5000 recommended)
- DebugLogging: Must be `true` or `false` (no quotes)
- StuckThreshold: Must be positive integer (1-10 recommended)
- StuckDistance: Must be positive decimal (1.0-20.0 recommended)

