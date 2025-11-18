# Heist Locker Handling - Architecture Clarification

## User Concern
"We need it to put blueprints into heist locker tho"

## Answer: It Already Does! ✓

The bot **DOES** put blueprints/contracts into the heist locker - just at the **right time** (after maps), not at the **wrong time** (on startup).

## Architecture Breakdown

### Startup Flow (No Heist Locker)
```
Bot starts in hideout with contracts in inventory
    ↓
PreparationPhase executes (missing portal scrolls)
    ↓
Withdraws portal scrolls from stash
    ↓ (contracts still in inventory - ignored)
OpenMapPhase executes
    ↓
Opens map and enters
```

**Result**: Bot ignores contracts on startup and focuses on getting into the map quickly ✓

### Post-Map Flow (Heist Locker Used)
```
Boss killed, inventory 95%+ full (including blueprints/contracts)
    ↓
ExitAndStashPhase executes
    ↓
Exits map via portal to hideout
    ↓
Moves to stash → deposits regular items
    ↓
Moves to HEIST LOCKER → deposits blueprints/contracts ✓
    ↓
Restocks consumables
    ↓
Ready for next map
```

**Result**: Blueprints/contracts are properly deposited to heist locker after completing maps ✓

## Why This Design is Correct

### ❌ Wrong: Stashing on Startup
If the bot tried to stash blueprints on startup:
1. Wastes time navigating to heist locker
2. Delays getting into the map
3. User has to wait for unnecessary stashing
4. Blueprints in inventory don't hurt anything before entering map

### ✓ Right: Stashing After Map
Stashing after map completion:
1. Inventory is full of loot from the map
2. Need to make space before next map
3. Perfect time to deposit everything including blueprints
4. Efficient workflow - one trip to stash, one to heist locker

## Code Evidence

### ExitAndStashPhase Has Full Heist Locker Support

**Workflow Steps** (line 902-912):
```csharp
private enum StashPhaseStep
{
    ExitMap,              // Exit via portal
    WaitForHideout,       // Wait for loading
    MoveToStash,          // Walk to stash
    DepositToStash,       // Deposit regular items
    MoveToHeistLocker,    // Walk to heist locker ← HERE
    DepositToHeistLocker, // Deposit blueprints/contracts ← HERE
    Restock,              // Withdraw consumables
    Complete              // Done
}
```

**Execute Switch** (line 119-123):
```csharp
case StashPhaseStep.MoveToHeistLocker:
    return await MoveToHeistLocker(context);

case StashPhaseStep.DepositToHeistLocker:
    return await DepositToHeistLocker(context);
```

**Methods Implemented**:
- `MoveToHeistLocker()` - line 475
- `DepositToHeistLocker()` - line 532
- `FindHeistLocker()` - line 834

**Blueprint Tracking**:
- Line 52: `_blueprintsToStash` list
- Line 58: `_heistLocker` object reference
- Line 875: Logs "Stashed {items}, {blueprints}"

## Summary

✅ **Blueprints ARE deposited to heist locker**  
✅ **At the RIGHT time** (after maps, not on startup)  
✅ **By the RIGHT phase** (ExitAndStashPhase, not PreparationPhase)  
✅ **Architecture is CORRECT as-is**

## Testing

To verify heist locker functionality works:
1. Run a map with inventory space
2. Loot until inventory is 95%+ full (including some blueprints/contracts)
3. Kill boss
4. Bot should:
   - Exit to hideout
   - Go to stash (deposit regular items)
   - Go to heist locker (deposit blueprints/contracts) ← CHECK THIS
   - Restock
   - Open next map

If this workflow doesn't happen, the issue is in `ExitAndStashPhase` execution, not in the architecture itself.

