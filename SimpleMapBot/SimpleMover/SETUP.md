# BeastMover Setup Guide

## Installation

BeastMover is already included in your ClaudeBeasts2.0 project. DreamPoeBot will automatically detect it on startup.

## Configuration Steps

### 1. Enable BeastMover in DreamPoeBot

1. **Launch DreamPoeBot**
2. **Go to Settings** (gear icon)
3. **Navigate to "Player Mover" section**
4. **Select "BeastMover"** from the dropdown menu
5. **Click "Apply" or "Save"**
6. **Restart the bot** for changes to take effect

### 2. Verify It's Working

After restarting, you should see in the logs:
```
[BeastMover] Initialized
[BeastMover] Enabled
```

### 3. Test Movement

1. Start the bot in your hideout
2. Watch as it moves to the map device
3. Verify smooth movement to stash when needed

## Troubleshooting

### BeastMover Not Showing in Dropdown

**Problem**: BeastMover doesn't appear in the player mover list

**Solution**:
1. Make sure `BeastMover` folder is in the same directory as `SkillPlayerMover`
2. Check that `3rdparty.json` exists in BeastMover folder
3. Restart DreamPoeBot completely
4. Check bot logs for any loading errors

### Bot Uses Wrong Mover

**Problem**: Bot still uses SkillPlayerMover after selecting BeastMover

**Solution**:
1. **Important**: You must restart the bot after changing player mover
2. Check that "BeastMover" is actually selected (not just highlighted)
3. Save settings before restarting

### Movement Issues

**Problem**: Bot doesn't move or moves incorrectly

**Solution**:
1. Check bot logs for BeastMover errors:
   - Look for `[BeastMover] Error` messages
   - Look for `ExilePather.FindPath failed` messages
2. Verify your hideout has clear paths to stash and map device
3. Try switching back to SkillPlayerMover temporarily to see if it's a mover issue

### Stuck Detection Triggers Too Often

**Problem**: Bot keeps saying "Player appears stuck"

**Solution**:
- This is normal if hideout has obstacles
- BeastMover will automatically regenerate paths
- Consider redesigning hideout for clearer paths
- Or use SkillPlayerMover which can dash through obstacles

## Reverting to SkillPlayerMover

If you want to go back to SkillPlayerMover:

1. Go to Settings → Player Mover
2. Select "SkillPlayerMover"
3. Restart bot
4. Configure your movement skills in SkillPlayerMover settings

## Performance Notes

- **BeastMover** is slower but more reliable (walking only)
- **SkillPlayerMover** is faster but requires configured movement skills
- For hideout-only bots (like Beast bot), BeastMover is recommended
- For mapping/combat, SkillPlayerMover may be better

## Support

If you encounter issues:
1. Check the logs in `Logs/` folder
2. Look for `[BeastMover]` entries
3. Verify the bot is actually using BeastMover (check initialization messages)
4. Test in a clean hideout with no obstacles

