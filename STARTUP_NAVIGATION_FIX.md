# Startup Navigation Fix - COMPLETE REDESIGN

## Issue
On bot startup, the bot was opening the **Heist Locker** instead of navigating to the **Stash** or **Map Device** first. This caused confusion and delayed the map opening workflow.

**Even worse**: The bot had contracts in inventory and was trying to go to heist locker on startup, which is completely wrong - it should NEVER stash items on startup, only restock what's needed to run maps.

## Root Cause
The `PreparationPhase` was incorrectly designed to handle BOTH:
1. **Stashing items** (including blueprints/contracts → Heist Locker)
2. **Restocking consumables** (portal scrolls)

This was fundamentally wrong. The phase was being used for two completely different purposes:
- **Startup preparation** (restock portal scrolls to start mapping)
- **Mid-run stashing** (deposit loot after returning from map)

When the bot started with contracts in inventory:
1. Enter `PreparationPhase` (because missing portal scrolls)
2. Analyze inventory for items to stash
3. Find contracts in inventory
4. Navigate to **Heist Locker** to deposit contracts ❌ WRONG!
5. Never actually deposit them because workflow was broken
6. Get stuck in wrong location

**The fundamental problem**: PreparationPhase should NEVER handle stashing. That's what `ExitAndStashPhase` is for!

## Solution: Complete Phase Redesign

### 1. Redefined PreparationPhase Purpose
**PreparationPhase is now STARTUP ONLY** - it does NOT handle stashing at all.

- **Old behavior**: Stash items + restock consumables
- **New behavior**: ONLY restock portal scrolls on startup
- **Stashing responsibility**: Moved to `ExitAndStashPhase` (where it belongs)

### 2. Removed All Stashing Logic
Completely removed inventory analysis for stashing:

```csharp
private async Task<PhaseResult> AnalyzeInventory(GameContext context)
{
    Log.Info("[Preparation] PreparationPhase is for STARTUP ONLY - going directly to restock portal scrolls");

    // PreparationPhase should NEVER stash items
    // Stashing is handled by ExitAndStashPhase after returning from maps
    // This phase ONLY restocks portal scrolls for startup
    
    _itemsToStash.Clear();
    _blueprintsToStash.Clear();
    
    // Go directly to restock - no inventory analysis needed
    _currentStep = PrepPhaseStep.Restock;
    _stepAttempts = 0;
    return PhaseResult.InProgress("Restocking portal scrolls for startup");
}
```

**Result**: Bot NEVER looks at inventory for stashing - goes **directly to stash** for portal scrolls.

### 3. Removed Deprecated Workflow Steps
Removed these steps from the execute switch:
- `MoveToStash` (deprecated)
- `DepositToStash` (deprecated)
- `MoveToHeistLocker` (deprecated)
- `DepositToHeistLocker` (deprecated)

Simplified the workflow enum:
```csharp
private enum PrepPhaseStep
{
    AnalyzeInventory,     // Validate startup (always goes to Restock)
    Restock,              // Withdraw portal scrolls from stash
    Complete              // Done - ready for map opening
}
```

**Result**: Clean, simple workflow with no confusion about what this phase does.

### 4. Implemented Automatic Portal Scroll Withdrawal
Previously, the `Restock()` method only logged a warning and didn't actually withdraw portal scrolls. Now it:

1. Finds the stash chest in hideout
2. Navigates to stash
3. Opens stash UI
4. Searches first 10 tabs for portal scrolls
5. Withdraws a stack using `FastMove()`
6. Closes stash UI
7. Completes preparation phase

**Result**: Bot now **automatically restocks portal scrolls** from stash on startup.

### 5. Clarified Phase Responsibilities
Updated documentation to clarify:
- **PreparationPhase**: Only handles portal scrolls restocking on startup
- **OpenMapPhase**: Handles withdrawing maps and scarabs from stash as needed

## Testing Scenarios

### Scenario 1: Fresh Startup (No Portal Scrolls, Has Contracts)
**Before**: Bot → Analyzes inventory → Finds contracts → Heist Locker ❌ WRONG!  
**After**: Bot → Stash → Withdraw portal scrolls → Map Device ✓ (ignores contracts)

### Scenario 2: Fresh Startup (No Portal Scrolls, Empty Inventory)
**Before**: Bot → Stash/Heist Locker confusion  
**After**: Bot → Stash → Withdraw portal scrolls → Map Device ✓

### Scenario 3: Fresh Startup (Has Portal Scrolls)
**Before**: Bot → Map Device (correct)  
**After**: Bot → Map Device ✓ (PreparationPhase doesn't execute)

### Scenario 4: Returning from Map (Full Inventory with Contracts)
**Before**: PreparationPhase would handle this (wrong phase)  
**After**: ExitAndStashPhase handles this ✓ (correct phase)

## Code Changes

### Files Modified
1. `Beasts/Phases/PreparationPhase.cs`
   - **REDESIGNED** `AnalyzeInventory()` - now skips all stashing logic
   - **REMOVED** stashing workflow steps from execute switch
   - **SIMPLIFIED** workflow enum (3 steps instead of 7)
   - **DEPRECATED** old stashing methods (MoveToStash, DepositToStash, MoveToHeistLocker, DepositToHeistLocker)
   - **IMPLEMENTED** automatic portal scroll withdrawal in `Restock()`
   - **UPDATED** documentation to clarify this is STARTUP ONLY phase

### Behavior Changes
- **Startup navigation**: NEVER goes to heist locker, always goes to stash for portal scrolls ✓
- **Stashing**: Completely removed from PreparationPhase, now handled by ExitAndStashPhase ✓
- **Portal scroll handling**: Automatically withdraws from stash ✓
- **Phase purpose**: Clear single responsibility - startup preparation only ✓

## Impact
- ✅ **Faster startup**: No unnecessary detours to heist locker
- ✅ **Better UX**: Bot navigates to correct location immediately
- ✅ **Automated restocking**: No manual portal scroll withdrawal needed
- ✅ **Clearer workflow**: Each phase has well-defined responsibilities

## Related Issues
This fix resolves multiple critical issues:
- ✅ Bot opening heist locker on startup (completely removed)
- ✅ Bot trying to stash contracts/blueprints on startup (phase no longer handles stashing)
- ✅ Bot getting stuck with wrong UI open (clear navigation path now)
- ✅ Bot requiring manual portal scroll restocking (now automatic)
- ✅ Confusing phase responsibilities (now clearly defined)

## Architecture Improvement
This fix improves the overall bot architecture by:
1. **Single Responsibility Principle**: Each phase has ONE clear purpose
   - `PreparationPhase`: Startup portal scroll restocking only
   - `ExitAndStashPhase`: Post-map stashing only
   - `OpenMapPhase`: Map device interaction only
2. **Clear Phase Priority**: No overlap between phase responsibilities
3. **Predictable Behavior**: Bot always does the same thing in the same situation

## Future Enhancements
Potential improvements for future updates:
1. Add configurable stash tab index for portal scrolls (currency tab)
2. Support for bulk portal scroll withdrawal (withdraw multiple stacks)
3. Smart detection of currency tab type (specialized vs regular)
4. Automatic map/scarab restocking in PreparationPhase (currently handled by OpenMapPhase)

