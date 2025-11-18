# DreamPoeBot API Compatibility Guide
**Generated:** 2025-11-16
**Project:** Beasts Routine
**Purpose:** Complete reference for fixing API compatibility issues

---

## Table of Contents
1. [Required Using Directives](#required-using-directives)
2. [Type Corrections](#type-corrections)
3. [Property Access Patterns](#property-access-patterns)
4. [Method Replacements](#method-replacements)
5. [String Operations](#string-operations)
6. [Object Retrieval Patterns](#object-retrieval-patterns)
7. [Common Pitfalls](#common-pitfalls)

---

## Required Using Directives

### Standard DreamPoeBot Imports
```csharp
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Bot.Pathfinding;
using DreamPoeBot.Loki.Coroutine;
using log4net;
```

### Namespace-Specific Types

| Type | Namespace | Example |
|------|-----------|---------|
| `Logger` | `DreamPoeBot.Common` | `Logger.GetLoggerInstanceForType()` |
| `InventorySlot` | `DreamPoeBot.Loki.Game.GameData` | `InventorySlot.Main` |
| `Rarity` | `DreamPoeBot.Loki.Game.GameData` | `Rarity.Unique` |
| `DatWorldAreaWrapper` | `DreamPoeBot.Loki.Game.GameData` | `LokiPoe.CurrentWorldArea` |
| `Monster`, `Item`, `WorldItem`, `Chest`, `NPC`, `Portal`, `Stash` | `DreamPoeBot.Loki.Game.Objects` | Various object types |
| `ItemClasses` | `DreamPoeBot.Loki.Game.GameData` | `ItemClasses.LifeFlask` |
| `StatTypeGGG` | `DreamPoeBot.Loki.Game.GameData` | For item statistics |

---

## Type Corrections

### 1. WorldArea Type

**ISSUE:** Code uses `WorldArea` but should use `DatWorldAreaWrapper`

**WRONG:**
```csharp
WorldArea area = LokiPoe.CurrentWorldArea;
```

**CORRECT:**
```csharp
// In GameContext.cs - use DatWorldAreaWrapper
public DatWorldAreaWrapper CurrentArea { get; set; }

// In method parameters - use WorldArea (it's an alias)
private bool IsInHideout(WorldArea area)
{
    if (area == null) return false;
    return area.IsHideout || area.Name?.Contains("Hideout") == true;
}

// Access via LokiPoe
var currentArea = LokiPoe.CurrentWorldArea; // Returns DatWorldAreaWrapper
```

**Available Properties:**
```csharp
currentArea.Name           // string - area name
currentArea.Id             // string - unique area ID
currentArea.IsMap          // bool - is this a map
currentArea.IsHideout      // bool - is this a hideout
currentArea.IsTown         // bool - is this a town
currentArea.IsCombatArea   // bool - is this a combat area
currentArea.IsBossArea     // bool - is this a boss area
currentArea.IsUniqueMapArea // bool - is this a unique map
```

### 2. NPC Type

**ISSUE:** NPC is not a NetworkObject - it's `DreamPoeBot.Loki.Components.NPC`

**WRONG:**
```csharp
var faustus = LokiPoe.ObjectManager.GetObjectsByType<NPC>()
    .FirstOrDefault(npc => npc.Distance < 100f);  // ERROR: Distance doesn't exist on NPC
```

**CORRECT:**
```csharp
// Use .OfType<NPC>() pattern from ObjectManager
var faustus = LokiPoe.ObjectManager.Objects
    .OfType<NPC>()
    .Where(npc => npc.IsValid &&
                 npc.Distance < 300f &&  // Distance available via base class
                 (npc.Name.Contains("Faustus") || npc.Metadata.Contains("Faustus")))
    .OrderBy(npc => npc.Distance)
    .FirstOrDefault();
```

**Available NPC Properties (via inheritance):**
```csharp
npc.Name       // string - NPC name
npc.Metadata   // string - metadata path
npc.Distance   // float - distance from player
npc.Position   // Vector2i - position
npc.IsValid    // bool - is object valid
```

**NPC Interaction:**
```csharp
// Use PlayerAction wrapper for interactions
var interactResult = await PlayerAction.TakeAction(() =>
{
    return npc.Interact();
}, "InteractWithNPC");
```

### 3. InventorySlot Type

**CORRECT USAGE:**
```csharp
// Access player inventory
var inventory = LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Main);

// Available inventory slots (from working code):
InventorySlot.Main        // Main player inventory (12x5 grid)
// Other slots likely include: Helm, BodyArmour, Gloves, Boots, Weapon1, Weapon2, Flask, etc.
```

### 4. Rarity Enum

**CORRECT USAGE:**
```csharp
// Check monster rarity
if (monster.Rarity == Rarity.Unique) { }
if (monster.Rarity == Rarity.Rare) { }
if (monster.Rarity == Rarity.Magic) { }
if (monster.Rarity == Rarity.Normal) { }

// Check item rarity (via ModsComponent)
var mods = item.Components.ModsComponent;
var rarity = mods == null ? Rarity.Normal : mods.Rarity;
```

### 5. Portal Type

**CORRECT USAGE:**
```csharp
// Get portal objects
var portal = LokiPoe.ObjectManager.GetObjectsByType<Portal>()
    .FirstOrDefault(p => p != null && p.IsValid && p.Distance < 50f);

// Interact with portal
var result = await PlayerAction.TakeAction(() =>
{
    return portal.Interact();
}, "EnterPortal");
```

### 6. Stash Type

**CORRECT USAGE:**
```csharp
// Find stash
var stash = LokiPoe.ObjectManager.GetObjectsByType<Stash>()
    .FirstOrDefault(s => s != null && s.IsValid && s.Distance < 100f);

// Interact with stash
var interactResult = await PlayerAction.TakeAction(() =>
{
    return stash.Interact();
}, "OpenStash");

// Check if stash UI is open
if (LokiPoe.InGameState.StashUi.IsOpened) { }

// Move items to stash
var fastMoveResult = await PlayerAction.TakeAction(() =>
{
    return LokiPoe.InGameState.StashUi.FastMoveFromInventory(item.LocationTopLeft.X, item.LocationTopLeft.Y);
}, "FastMoveToStash");

// Close stash
LokiPoe.InGameState.StashUi.Close();
```

---

## Property Access Patterns

### Monster Properties

**WORKING PROPERTIES:**
```csharp
monster.Name                // string - monster name
monster.Metadata            // string - metadata path
monster.Distance            // float - distance from player
monster.Position            // Vector2i - position
monster.IsValid             // bool - is object valid
monster.IsDead              // bool - is monster dead
monster.IsHostile           // bool - is monster hostile
monster.Rarity              // Rarity enum - monster rarity
monster.IsBoss              // bool - is this a boss (WORKS!)
monster.HealthPercentTotal  // float - HP percentage (0-100)
monster.Id                  // uint - unique object ID
```

### Item Properties

**WORKING PROPERTIES:**
```csharp
item.Name                   // string - base item name
item.FullName               // string - full item name (with prefixes/suffixes)
item.Metadata               // string - metadata path
item.Class                  // string - item class (e.g., "LifeFlask")
item.IsValid                // bool - is item valid
item.IsEquipped             // bool - is item equipped (WORKS!)
item.LocationTopLeft        // Vector2i - inventory position
item.Size                   // Vector2i - item size in slots
item.StackCount             // int - stack count
item.Components             // ItemComponents - component data
item.Stats                  // IEnumerable<string> - item stats
item.CurrentCharges         // int - flask charges
item.CanUse                 // bool - can item be used
item.IsInstantRecovery      // bool - is instant flask
```

**Item.Use():**
```csharp
// Item.Use() EXISTS and WORKS!
// But use via PlayerAction wrapper for safety:
var useResult = await PlayerAction.TakeAction(() =>
{
    return item.Use();
}, "UseItem");
```

**Item Components:**
```csharp
// Access ModsComponent for rarity
var modsComponent = item.Components.ModsComponent;
if (modsComponent != null)
{
    var rarity = modsComponent.Rarity;  // Returns Rarity enum
}

// Access MapComponent for map tier
var mapComponent = item.Components.MapComponent;
if (mapComponent != null && mapComponent.Tier > 0)
{
    int tier = mapComponent.Tier;
}

// Access LocalStats
var localStats = item.Components.LocalStats;
if (localStats != null && localStats.ContainsKey(StatTypeGGG.SomeStat))
{
    // Check for specific stats
}
```

---

## Method Replacements

### 1. PlayerAction.TakeAction()

**ISSUE:** `PlayerAction` doesn't exist as a static class

**SOLUTION:** Based on working code, it DOES exist and is used extensively

**CORRECT USAGE:**
```csharp
// Using items
var useResult = await PlayerAction.TakeAction(() =>
{
    return item.Use();
}, "UseItem");

// Interacting with objects
var interactResult = await PlayerAction.TakeAction(() =>
{
    return npc.Interact();
}, "InteractWithNPC");

// Using skills
var skillResult = await PlayerAction.TakeAction(() =>
{
    return LokiPoe.InGameState.SkillBarHud.UseAt(
        slotNumber,
        false,
        targetPosition
    );
}, "UseSkill");
```

### 2. Logger

**CORRECT USAGE:**
```csharp
// Import: using log4net;
private static readonly ILog Log = Logger.GetLoggerInstanceForType();

// Logging methods
Log.Debug("Debug message");
Log.Info("Info message");
Log.InfoFormat("Formatted {0}", value);
Log.Warn("Warning message");
Log.WarnFormat("Formatted {0}", value);
Log.Error("Error message");
Log.Error("Error message", exception);
Log.DebugFormat("Formatted debug {0}", value);
```

### 3. Object Interaction

**PATTERN:**
```csharp
// Always finish current action first
await Coroutines.FinishCurrentAction();

// Then interact
var result = await Coroutines.InteractWith(networkObject);

// Or via PlayerAction
var result = await PlayerAction.TakeAction(() =>
{
    return networkObject.Interact();
}, "ActionName");
```

---

## String Operations

### Contains() Method

**ISSUE:** Using wrong number of arguments

**WRONG:**
```csharp
// Too many arguments - this is JavaScript/TypeScript pattern
if (item.Name.Contains("Map", true, CultureInfo.InvariantCulture))  // ERROR!
```

**CORRECT:**
```csharp
// Option 1: Case-sensitive (default)
if (item.Name.Contains("Map"))  // Simple contains

// Option 2: Case-insensitive with StringComparison
if (item.Name.Contains("Map", System.StringComparison.OrdinalIgnoreCase))

// Option 3: Case-insensitive with ToLower
if (item.Name.ToLower().Contains("map"))

// Working examples from codebase:
item.Class.Contains("Map", System.StringComparison.OrdinalIgnoreCase)
item.Name.Contains("Scarab", System.StringComparison.OrdinalIgnoreCase)
item.Metadata.Contains("PortalScroll")  // Case-sensitive is fine for metadata
```

---

## Object Retrieval Patterns

### GetObjectsByType<T>() Pattern

```csharp
// Monsters
var monsters = LokiPoe.ObjectManager.GetObjectsByType<Monster>()
    .Where(m => m.IsValid && !m.IsDead && m.Distance < 100f)
    .ToList();

// Chests
var chests = LokiPoe.ObjectManager.GetObjectsByType<Chest>()
    .Where(c => c.IsValid && c.Distance < 100f)
    .ToList();

// World Items (loot)
var loot = LokiPoe.ObjectManager.GetObjectsByType<WorldItem>()
    .Where(item => item.IsValid && item.Distance < 50f)
    .ToList();

// Portals
var portal = LokiPoe.ObjectManager.GetObjectsByType<Portal>()
    .FirstOrDefault(p => p.IsValid && p.Distance < 50f);

// Stash
var stash = LokiPoe.ObjectManager.GetObjectsByType<Stash>()
    .FirstOrDefault(s => s.IsValid && s.Distance < 100f);
```

### OfType<T>() Pattern (for NPCs)

```csharp
// NPC requires .OfType<NPC>() pattern
var npc = LokiPoe.ObjectManager.Objects
    .OfType<NPC>()
    .Where(n => n.IsValid && n.Distance < 300f)
    .FirstOrDefault();
```

### Generic Objects (NetworkObject)

```csharp
// For generic objects (map device, stash, etc.)
var mapDevice = LokiPoe.ObjectManager.Objects
    .Where(obj => obj.IsValid &&
                 obj.Distance < 200f &&
                 (obj.Metadata.Contains("MapDevice") ||
                  obj.Name.Contains("Map Device", System.StringComparison.OrdinalIgnoreCase)))
    .OrderBy(obj => obj.Distance)
    .FirstOrDefault();
```

### AreaTransition Pattern

```csharp
// Cast to AreaTransition when checking transitions
(obj is AreaTransition transition &&
 (transition.Metadata.Contains("boss") ||
  transition.TransitionText.ToLower().Contains("boss")))
```

---

## Common Pitfalls

### 1. Location Detection

**WORKING PATTERN:**
```csharp
// Get current area
var currentArea = LokiPoe.CurrentWorldArea;  // Returns DatWorldAreaWrapper

// Check location
private bool IsInHideout(WorldArea area)
{
    if (area == null) return false;
    return area.IsHideout || area.Name?.Contains("Hideout") == true;
}

private bool IsInMap(WorldArea area)
{
    if (area == null) return false;
    return area.IsCombatArea && !area.IsTown && !IsInHideout(area);
}

private bool IsInTown(WorldArea area)
{
    if (area == null) return false;
    return area.IsTown;
}
```

### 2. Boss Detection

```csharp
// Monsters have .IsBoss property - it WORKS!
var bosses = LokiPoe.ObjectManager.GetObjectsByType<Monster>()
    .Where(m => m.IsValid &&
               !m.IsDead &&
               m.IsHostile &&
               (m.Rarity == Rarity.Unique || m.IsBoss))  // IsBoss exists!
    .OrderBy(m => m.Distance)
    .ToList();

// Check boss area
private bool DetectBossRoom()
{
    var currentArea = LokiPoe.CurrentWorldArea;
    if (currentArea == null) return false;

    string areaName = currentArea.Name.ToLower();

    return areaName.Contains("arena") ||
           areaName.Contains("sanctum") ||
           currentArea.IsBossArea ||      // Property exists!
           currentArea.IsUniqueMapArea;   // Property exists!
}
```

### 3. Inventory Operations

```csharp
// Get inventory
var inventory = LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Main);
if (inventory == null)
    return;

// Iterate items
foreach (var item in inventory.Items)
{
    if (item == null || !item.IsValid)
        continue;

    // Access properties
    int x = item.LocationTopLeft.X;
    int y = item.LocationTopLeft.Y;
    int width = item.Size.X;
    int height = item.Size.Y;
}

// Find specific items
var portalScroll = inventory.Items
    .FirstOrDefault(item => item != null &&
                          item.IsValid &&
                          item.Metadata != null &&
                          (item.Metadata.Contains("PortalScroll") ||
                           item.Name.Contains("Portal Scroll")));
```

### 4. Async/Await Patterns

```csharp
// Always await Coroutine operations
await Coroutine.Sleep(500);  // NOT Task.Delay!
await Coroutines.FinishCurrentAction();
await Coroutines.InteractWith(obj);

// PlayerAction.TakeAction is async
var result = await PlayerAction.TakeAction(() =>
{
    return item.Use();
}, "ActionName");
```

### 5. Movement

```csharp
// Move towards position
PlayerMoverManager.Current.MoveTowards(targetPosition);

// Check if movement succeeded
var moveResult = PlayerMoverManager.Current.MoveTowards(targetPosition);
if (!moveResult)
{
    Log.Warn("Failed to move");
}
```

### 6. Combat

```csharp
// Use skill on target
var combatResult = await PlayerAction.TakeAction(() =>
{
    return LokiPoe.InGameState.SkillBarHud.UseAt(
        skillSlot,      // int - skill slot number
        false,          // bool - use shift modifier
        targetPosition  // Vector2i - target position
    );
}, "UseSkill");

// Or use CombatLogic
await CombatLogic.Current.Fight();
```

### 7. Blacklist

```csharp
// Add to blacklist
Blacklist.Add(monster.Id, TimeSpan.FromMinutes(10), "Reason");

// Check blacklist
if (Blacklist.Contains(monster.Id))
{
    // Skip this monster
}
```

---

## Quick Reference Table

| Old/Wrong | Correct | Notes |
|-----------|---------|-------|
| `WorldArea area` (in fields) | `DatWorldAreaWrapper area` | In GameContext or fields |
| `WorldArea area` (in params) | `WorldArea area` | Method parameters use alias |
| `var area = ...CurrentWorldArea` | Returns `DatWorldAreaWrapper` | But can be used as WorldArea |
| `npc.Distance` (direct) | Use `.OfType<NPC>()` pattern | NPC needs special handling |
| `item.Use()` | `await PlayerAction.TakeAction(() => item.Use(), "Use")` | Wrapper recommended |
| `Contains(str, true, culture)` | `Contains(str, StringComparison.OrdinalIgnoreCase)` | C# .NET 4.8 syntax |
| `Monster.IsBoss` | Works! | Property exists and works |
| `Item.IsEquipped` | Works! | Property exists and works |
| `InventorySlot.???` | `InventorySlot.Main` | From GameData namespace |
| `Rarity.???` | `Rarity.Unique/Rare/Magic/Normal` | From GameData namespace |

---

## Working Code Examples

### Example 1: Find and Use Portal Scroll
```csharp
var inventory = LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Main);
var portalScroll = inventory?.Items
    .FirstOrDefault(item => item != null &&
                          item.IsValid &&
                          item.Metadata != null &&
                          (item.Metadata.Contains("PortalScroll") ||
                           item.Name.Contains("Portal Scroll")));

if (portalScroll != null)
{
    var useResult = await PlayerAction.TakeAction(() =>
    {
        return portalScroll.Use();
    }, "UsePortalScroll");
}
```

### Example 2: Find and Interact with NPC
```csharp
var faustus = LokiPoe.ObjectManager.Objects
    .OfType<NPC>()
    .Where(npc => npc.IsValid &&
                 npc.Distance < 300f &&
                 (npc.Name.Contains("Faustus", System.StringComparison.OrdinalIgnoreCase) ||
                  npc.Metadata.Contains("Faustus")))
    .OrderBy(npc => npc.Distance)
    .FirstOrDefault();

if (faustus != null && faustus.Distance <= 20f)
{
    await Coroutines.FinishCurrentAction();
    var interactResult = await Coroutines.InteractWith(faustus);
}
```

### Example 3: Check Boss and Rarity
```csharp
var bosses = LokiPoe.ObjectManager.GetObjectsByType<Monster>()
    .Where(m => m.IsValid &&
               !m.IsDead &&
               m.IsHostile &&
               (m.Rarity == Rarity.Unique || m.IsBoss))
    .OrderBy(m => m.Distance)
    .ToList();

foreach (var boss in bosses)
{
    Log.InfoFormat("Boss: {0}, Rarity: {1}, HP: {2:F1}%",
        boss.Name, boss.Rarity, boss.HealthPercentTotal);
}
```

### Example 4: Area Detection
```csharp
var currentArea = LokiPoe.CurrentWorldArea;

bool isHideout = currentArea.IsHideout ||
                 currentArea.Name?.Contains("Hideout") == true;

bool isMap = currentArea.IsMap;

bool isTown = currentArea.IsTown;

bool isBossArea = currentArea.IsBossArea ||
                  currentArea.IsUniqueMapArea;
```

---

## Summary of Key Findings

### ✅ WORKING (Contrary to initial report):
- `Item.Use()` - EXISTS and works
- `Monster.IsBoss` - EXISTS and works
- `Item.IsEquipped` - EXISTS and works
- `InventorySlot` - EXISTS in `DreamPoeBot.Loki.Game.GameData`
- `Rarity` - EXISTS in `DreamPoeBot.Loki.Game.GameData`
- `Portal` - EXISTS as object type
- `Logger` - EXISTS via `DreamPoeBot.Common`
- `PlayerAction` - EXISTS and is used extensively

### ⚠️ NEEDS ATTENTION:
- `WorldArea` vs `DatWorldAreaWrapper` - Use DatWorldAreaWrapper in fields, WorldArea in method params
- `NPC` - Requires `.OfType<NPC>()` pattern, not `.GetObjectsByType<NPC>()`
- `Contains()` - Use 1 or 2 argument version, not 3
- WorldArea properties (`.IsHideout`, `.IsBossArea`, etc.) - All EXIST and work!

### 🔧 BEST PRACTICES:
1. Always use `using DreamPoeBot.Loki.Game.GameData;` for types
2. Wrap `Item.Use()` and interactions in `PlayerAction.TakeAction()` for safety
3. Use `StringComparison.OrdinalIgnoreCase` for case-insensitive string operations
4. Check `IsValid` before accessing object properties
5. Use `await Coroutines.FinishCurrentAction()` before interactions
6. Use `await Coroutine.Sleep()` not `Task.Delay()`

---

**End of Guide**
