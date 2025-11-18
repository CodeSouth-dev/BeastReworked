# Poe.Ninja Loot Filtering - Quick Start Guide

## TL;DR

**The bot now only loots valuable items using real-time poe.ninja prices!**

- ✅ **Blueprints & Contracts**: ALWAYS looted (mandatory)
- ✅ **Valuable items**: Only loots items worth 5+ chaos
- ❌ **Trash items**: Automatically skipped

## Quick Setup (3 Steps)

### Step 1: Configure Your League
Open your bot settings and set:
```
PoeNinjaLeague = "Settlers"  // Or "Standard", "Hardcore", etc.
```

### Step 2: Set Your Chaos Threshold (Optional)
```
MinimumChaosValue = 5.0  // Default - only loot 5c+ items
```

**Recommendations:**
- **Conservative**: 10.0 (only high-value items)
- **Balanced**: 5.0 (good mix)
- **Aggressive**: 2.0 (more items)

### Step 3: Start the Bot!
That's it! The bot will:
1. Auto-fetch prices from poe.ninja on startup
2. Only loot items worth ≥ your threshold
3. ALWAYS loot blueprints/contracts (mandatory)

## What Gets Looted?

### ALWAYS Looted (Mandatory)
✅ Blueprints  
✅ Contracts  
✅ Rogue's Markers  

### Looted If Valuable (≥ MinimumChaosValue)
✅ Currency (Divine, Chaos, Exalted, etc.)  
✅ Unique Items (only if worth enough)  
✅ Divination Cards (The Doctor, House of Mirrors, etc.)  
✅ Maps (if configured)  
✅ Scarabs & Fragments  

### NEVER Looted
❌ Worthless uniques (Lifesprig, Goldrim, etc.)  
❌ Rare items (no accurate price data)  
❌ Magic items  
❌ Low-value currency (Transmutes, Whetstones, etc. if < threshold)  

## Example: What Happens?

### Example 1: Divine Orb (Worth ~200c)
```
Divine Orb drops
→ Check poe.ninja: 200 chaos
→ 200c >= 5c? YES
→ ✅ LOOTED
```

### Example 2: Lifesprig (Worth ~0.5c)
```
Lifesprig drops
→ Check poe.ninja: 0.5 chaos
→ 0.5c >= 5c? NO
→ ❌ SKIPPED
```

### Example 3: Blueprint (Mandatory)
```
Blueprint drops
→ Is Blueprint? YES
→ ✅ ALWAYS LOOTED (price check skipped)
```

## Advanced Configuration

### Disable poe.ninja (Not Recommended)
```
UsePoeNinjaFiltering = false
```
Falls back to basic rarity-based filtering (picks up all currency, uniques, etc.)

### Adjust Item Categories
```
PickupCurrency = true           // Currency (filtered by value)
PickupUniques = true            // Uniques (filtered by value)
PickupMaps = true               // Maps (filtered by value)
PickupDivinationCards = true    // Div cards (filtered by value)
PickupRares = false             // NOT RECOMMENDED (no price data)
```

### Fallback Settings
```
AlwaysPickupUnknownCurrency = true  // Pickup currency even if not in poe.ninja
```

## Troubleshooting

### "Not looting valuable items"
- Lower your `MinimumChaosValue` (try 2.0 or 3.0)
- Check if `UsePoeNinjaFiltering = true`
- Verify your league name is correct

### "Looting too much trash"
- Increase your `MinimumChaosValue` (try 10.0 or 15.0)
- Make sure `PickupRares = false`

### "poe.ninja API error"
- Bot will use fallback filtering automatically
- Check your internet connection
- Verify league name is spelled correctly

## Performance

- **Minimal Impact**: Prices cached for 1 hour
- **Fast Lookups**: O(1) dictionary lookups
- **Network**: ~500KB data per hour
- **No Lag**: Async background refresh

## Summary

**Default Settings (Recommended)**
```csharp
UsePoeNinjaFiltering = true
MinimumChaosValue = 5.0
PoeNinjaLeague = "Settlers"  // Or your league
PickupBlueprints = true      // Mandatory
PickupContracts = true       // Mandatory
PickupMarkers = true
PickupCurrency = true
PickupUniques = true
PickupMaps = true
PickupDivinationCards = true
PickupRares = false
AlwaysPickupUnknownCurrency = true
```

**Result**: Bot loots all blueprints/contracts + items worth 5+ chaos. Simple! 🎯

