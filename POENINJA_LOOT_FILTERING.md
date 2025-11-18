# Poe.Ninja Loot Filtering Integration

## Overview
The bot now uses the **poe.ninja API** to intelligently filter loot based on real-time market prices. This ensures you only pick up valuable items, maximizing efficiency and inventory space.

## Key Features

### ✅ Smart Price-Based Filtering
- Queries poe.ninja API for current item prices
- Only loots items above your configured chaos value threshold
- Automatically refreshes prices every hour
- Works for: Currency, Uniques, Divination Cards, Maps, Fragments

### ✅ Mandatory Loot (Always Picked Up)
These items are ALWAYS looted regardless of price:
- **Blueprints** (highest priority - core purpose of the bot)
- **Contracts** (second priority)
- **Rogue's Markers** (Heist currency)

### ✅ Fallback System
If an item isn't found in poe.ninja database:
- **Currency**: Always picked up (usually valuable)
- **Uniques**: Picked up (might be valuable)
- **Other items**: Skipped (too risky without price data)

## Configuration

### LootSettings Properties

```csharp
// Enable/disable poe.ninja filtering
UsePoeNinjaFiltering = true  // Default: true

// Minimum chaos value to loot (5c = only loot items worth 5 chaos or more)
MinimumChaosValue = 5.0      // Default: 5.0

// League for price data
PoeNinjaLeague = "Standard"  // Options: "Standard", "Hardcore", "Settlers", etc.

// Mandatory loot (always picked up)
PickupBlueprints = true      // Cannot disable - core purpose
PickupContracts = true
PickupMarkers = true

// Value-filtered loot (only if above MinimumChaosValue)
PickupCurrency = true
PickupUniques = true
PickupMaps = true
PickupDivinationCards = true
PickupRares = false          // NOT recommended - no accurate price data

// Fallback settings
AlwaysPickupUnknownCurrency = true  // Pickup currency even if not in poe.ninja
```

### Recommended Settings

**Conservative (High Value Only)**
```csharp
MinimumChaosValue = 10.0  // Only loot 10c+ items
PickupRares = false       // Skip all rares
```

**Balanced (Good Mix)**
```csharp
MinimumChaosValue = 5.0   // Loot 5c+ items (default)
PickupRares = false       // Skip all rares
```

**Aggressive (More Loot)**
```csharp
MinimumChaosValue = 2.0   // Loot 2c+ items
PickupRares = false       // Still skip rares
```

## How It Works

### 1. Startup Initialization
```
Bot starts → LootPerception.Update() called
    ↓
PoeNinjaService.Initialize(league)
    ↓
Fetches price data from poe.ninja API
    ↓
Caches prices for 1 hour
```

### 2. Loot Detection Flow
```
Item appears on ground
    ↓
Is it Blueprint/Contract/Marker? → YES → ALWAYS LOOT ✓
    ↓ NO
Check poe.ninja for price
    ↓
Price >= MinimumChaosValue? → YES → LOOT ✓
    ↓ NO
Price < MinimumChaosValue? → YES → SKIP ❌
    ↓
Price unknown? → Check fallback rules → Maybe loot
```

### 3. Priority System
Items are looted in this order:
1. **Blueprints** (10,000 priority)
2. **Contracts** (9,000 priority)
3. **High-value items** (sorted by chaos value)
4. **Medium-value items**
5. **Low-value items** (if above threshold)

## Supported Item Categories

| Category | poe.ninja Coverage | Notes |
|----------|-------------------|-------|
| Currency | ✅ Excellent | Divine, Chaos, Exalted, etc. |
| Fragments | ✅ Excellent | Scarabs, Invitations, etc. |
| Divination Cards | ✅ Excellent | The Doctor, House of Mirrors, etc. |
| Unique Items | ✅ Excellent | All uniques with market data |
| Maps | ✅ Good | Tier 1-16 maps |
| Rares | ❌ None | No price data (skip recommended) |

## API Details

### Endpoints Used
- `currencyoverview?league={league}&type=Currency`
- `currencyoverview?league={league}&type=Fragment`
- `itemoverview?league={league}&type=DivinationCard`
- `itemoverview?league={league}&type=UniqueWeapon`
- `itemoverview?league={league}&type=UniqueArmour`
- `itemoverview?league={league}&type=UniqueAccessory`
- `itemoverview?league={league}&type=UniqueFlask`
- `itemoverview?league={league}&type=UniqueJewel`
- `itemoverview?league={league}&type=Map`

### Cache Behavior
- **Cache Duration**: 1 hour
- **Refresh Method**: Automatic background refresh
- **Network Failure**: Uses stale cache if available
- **Manual Refresh**: `PoeNinjaService.ForceRefresh()` (future feature)

## Example Scenarios

### Scenario 1: Divine Orb Drops
```
Divine Orb drops (worth ~200c in current league)
    ↓
Check poe.ninja: 200c
    ↓
200c >= 5c threshold? YES
    ↓
LOOT ✓ (logged as "Valuable item (200c): Divine Orb")
```

### Scenario 2: Worthless Unique
```
Lifesprig drops (worth ~0.5c)
    ↓
Check poe.ninja: 0.5c
    ↓
0.5c >= 5c threshold? NO
    ↓
SKIP ❌ (logged as "Low value (0.5c < 5c): Lifesprig")
```

### Scenario 3: Blueprint (Mandatory)
```
Blueprint drops
    ↓
Is Blueprint? YES
    ↓
ALWAYS LOOT ✓ (logged as "Blueprint (mandatory): Smuggler's Den Blueprint")
(Price check skipped - blueprints ALWAYS looted)
```

### Scenario 4: Unknown Item
```
Obscure unique "The Broken Crown" drops
    ↓
Check poe.ninja: Not found
    ↓
Use fallback: Is Unique? YES
    ↓
LOOT ✓ (logged as "Unknown unique (fallback pickup): The Broken Crown")
```

## Performance Considerations

### Network Usage
- **Initial Load**: ~10 HTTP requests (one per category)
- **Data Size**: ~500KB total per refresh
- **Refresh Frequency**: Once per hour
- **Impact**: Minimal (async background refresh)

### CPU Usage
- **Price Lookups**: O(1) dictionary lookups (very fast)
- **Item Scanning**: Same as before (no performance impact)
- **Memory**: ~1-2MB for price cache

### Recommendations
- **Good Internet**: Enable poe.ninja filtering
- **Slow/Unstable Internet**: Disable and use basic filters
- **Solo Self-Found**: Disable (prices not relevant)

## Troubleshooting

### Issue: Items Not Being Looted
**Check:**
1. Is `UsePoeNinjaFiltering = true`?
2. Is `MinimumChaosValue` too high? Try lowering it
3. Are items actually valuable? Check poe.ninja manually
4. Check logs for "Low value" messages

### Issue: Too Many Items Being Looted
**Solutions:**
1. Increase `MinimumChaosValue` (try 10c or 15c)
2. Disable `PickupRares` if enabled
3. Check if filtering is actually enabled

### Issue: poe.ninja API Failure
**Behavior:**
- Bot falls back to basic filtering automatically
- Uses stale cache if available
- Logs warning: "Failed to refresh price cache"
**Fix:**
- Check internet connection
- Verify league name is correct
- Wait for API to come back online (rare)

## Future Enhancements

Potential improvements for future versions:
1. **Auto-detect league** from game client
2. **Custom price thresholds per item category**
   - Currency: 1c
   - Uniques: 5c
   - Divination Cards: 10c
3. **Price trend analysis** (rising/falling prices)
4. **Bulk pricing** for stackable items
5. **User-defined price overrides**
6. **GUI for live price monitoring**

## Files Modified

### New Files
- `Beasts/Services/PoeNinjaService.cs` - API integration and caching

### Modified Files
- `Beasts/Configuration/LootSettings.cs` - Added poe.ninja settings
- `Beasts/Perception/LootPerception.cs` - Integrated price checking

## Summary

✅ **Implemented**: Full poe.ninja integration with smart filtering  
✅ **Mandatory Loot**: Blueprints/contracts always looted (cannot disable)  
✅ **Value Filtering**: Only loot items above configured chaos threshold  
✅ **Fallback System**: Handles unknown items gracefully  
✅ **Performance**: Minimal impact with 1-hour caching  
✅ **User Control**: Fully configurable via settings  

**Result**: Bot now intelligently loots only valuable items, maximizing efficiency and profits! 🎯

