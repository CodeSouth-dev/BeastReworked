# Quick Fix Reference - DreamPoeBot API

## Copy-Paste Solutions for Common Issues

---

## 1. CORRECT Using Directives

**Copy this to the top of every .cs file:**

```csharp
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Bot.Pathfinding;
using DreamPoeBot.Loki.Coroutine;
using Beasts.Core;
using Beasts.Configuration;
using log4net;
```

---

## 2. WorldArea / DatWorldAreaWrapper

### In Class Fields/Properties:
```csharp
// ✅ CORRECT
public DatWorldAreaWrapper CurrentArea { get; set; }

// ❌ WRONG
public WorldArea CurrentArea { get; set; }
```

### In Method Parameters:
```csharp
// ✅ CORRECT (WorldArea is fine here)
private bool IsInHideout(WorldArea area)
{
    if (area == null) return false;
    return area.IsHideout || area.Name?.Contains("Hideout") == true;
}
```

### Getting Current Area:
```csharp
// ✅ CORRECT
var currentArea = LokiPoe.CurrentWorldArea;  // Returns DatWorldAreaWrapper

// All these properties WORK:
bool isHideout = currentArea.IsHideout;
bool isTown = currentArea.IsTown;
bool isMap = currentArea.IsMap;
bool isCombatArea = currentArea.IsCombatArea;
bool isBossArea = currentArea.IsBossArea;
bool isUniqueMap = currentArea.IsUniqueMapArea;
string name = currentArea.Name;
string id = currentArea.Id;
```

---

## 3. NPC Pattern

### ❌ WRONG:
```csharp
var npc = LokiPoe.ObjectManager.GetObjectsByType<NPC>()
    .FirstOrDefault(n => n.Distance < 100f);
```

### ✅ CORRECT:
```csharp
var npc = LokiPoe.ObjectManager.Objects
    .OfType<NPC>()
    .Where(n => n.IsValid &&
               n.Distance < 300f &&
               (n.Name.Contains("Faustus", System.StringComparison.OrdinalIgnoreCase) ||
                n.Metadata.Contains("Faustus")))
    .OrderBy(n => n.Distance)
    .FirstOrDefault();
```

---

## 4. Item.Use() Pattern

### ✅ CORRECT (with PlayerAction wrapper):
```csharp
var useResult = await PlayerAction.TakeAction(() =>
{
    return item.Use();
}, "UseItem");

if (!useResult)
{
    Log.Warn("Failed to use item");
    return PhaseResult.InProgress("Retrying item use...");
}
```

### Examples from Working Code:

**Portal Scroll:**
```csharp
var portalScroll = LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Main)
    ?.Items
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

**Map:**
```csharp
var useResult = await PlayerAction.TakeAction(() =>
{
    return mapItem.Use();
}, "UseMapOnDevice");
```

**Scarab:**
```csharp
var useResult = await PlayerAction.TakeAction(() =>
{
    return scarab.Use();
}, "UseScarab");
```

---

## 5. InventorySlot

### ✅ CORRECT:
```csharp
// Get main inventory
var inventory = LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Main);

if (inventory == null)
{
    Log.Error("Cannot access inventory");
    return;
}

// Iterate items
foreach (var item in inventory.Items)
{
    if (item == null || !item.IsValid)
        continue;

    // Do something with item
}

// Find specific item
var map = inventory.Items
    .FirstOrDefault(item => item != null &&
                          item.IsValid &&
                          item.Class != null &&
                          item.Class.Contains("Map", System.StringComparison.OrdinalIgnoreCase));
```

---

## 6. Rarity Enum

### ✅ CORRECT:
```csharp
// Check monster rarity
if (monster.Rarity == Rarity.Unique)
{
    Log.Info("Found unique monster");
}

// Switch on rarity
switch (monster.Rarity)
{
    case Rarity.Unique:
        priority += 100;
        break;
    case Rarity.Rare:
        priority += 50;
        break;
    case Rarity.Magic:
        priority += 25;
        break;
    case Rarity.Normal:
        priority += 10;
        break;
}

// Get item rarity from ModsComponent
var modsComponent = item.Components.ModsComponent;
var rarity = modsComponent == null ? Rarity.Normal : modsComponent.Rarity;
```

---

## 7. Monster.IsBoss

### ✅ CORRECT (Property EXISTS!):
```csharp
var bosses = LokiPoe.ObjectManager.GetObjectsByType<Monster>()
    .Where(m => m.IsValid &&
               !m.IsDead &&
               m.IsHostile &&
               (m.Rarity == Rarity.Unique || m.IsBoss))  // IsBoss WORKS!
    .OrderBy(m => m.Distance)
    .ToList();
```

---

## 8. Item.IsEquipped

### ✅ CORRECT (Property EXISTS!):
```csharp
// Never stash equipped items
if (item.IsEquipped)
{
    return true;  // Skip this item
}

// Or in filter
var itemsToStash = inventory.Items
    .Where(item => item != null &&
                  item.IsValid &&
                  !item.IsEquipped)  // IsEquipped WORKS!
    .ToList();
```

---

## 9. Contains() String Method

### ❌ WRONG:
```csharp
// 3 arguments - doesn't work in C# .NET 4.8
if (item.Name.Contains("Map", true, CultureInfo.InvariantCulture))  // ERROR!
```

### ✅ CORRECT:
```csharp
// Option 1: Case-insensitive with StringComparison
if (item.Name.Contains("Map", System.StringComparison.OrdinalIgnoreCase))

// Option 2: Simple case-sensitive
if (item.Metadata.Contains("PortalScroll"))

// Option 3: ToLower approach
if (item.Name.ToLower().Contains("map"))
```

**Working Examples:**
```csharp
// From OpenMapPhase.cs
item.Class.Contains("Map", System.StringComparison.OrdinalIgnoreCase)
item.Name.Contains("Scarab", System.StringComparison.OrdinalIgnoreCase)
item.Metadata.Contains("PortalScroll")
npc.Name.Contains("Faustus", System.StringComparison.OrdinalIgnoreCase)
```

---

## 10. Portal Type

### ✅ CORRECT:
```csharp
// Find portal
var portal = LokiPoe.ObjectManager.GetObjectsByType<Portal>()
    .FirstOrDefault(p => p != null && p.IsValid && p.Distance < 50f);

if (portal == null)
{
    Log.Warn("Portal not found");
    return PhaseResult.InProgress("Looking for portal...");
}

// Interact with portal
var interactResult = await PlayerAction.TakeAction(() =>
{
    return portal.Interact();
}, "EnterPortal");
```

---

## 11. Logger

### ✅ CORRECT:
```csharp
// At top of class
private static readonly ILog Log = Logger.GetLoggerInstanceForType();

// Usage
Log.Debug("Debug message");
Log.DebugFormat("Debug {0}", value);

Log.Info("Info message");
Log.InfoFormat("Info {0}", value);

Log.Warn("Warning message");
Log.WarnFormat("Warning {0}", value);

Log.Error("Error message");
Log.Error("Error message", exception);
Log.ErrorFormat("Error {0}", value);
```

---

## 12. Stash Pattern

### ✅ CORRECT:
```csharp
// Find stash
var stash = LokiPoe.ObjectManager.GetObjectsByType<Stash>()
    .FirstOrDefault(s => s != null && s.IsValid && s.Distance < 100f);

// Open stash
var interactResult = await PlayerAction.TakeAction(() =>
{
    return stash.Interact();
}, "OpenStash");

// Check if stash UI is open
if (LokiPoe.InGameState.StashUi.IsOpened)
{
    // Move items to stash
    var fastMoveResult = await PlayerAction.TakeAction(() =>
    {
        return LokiPoe.InGameState.StashUi.FastMoveFromInventory(
            item.LocationTopLeft.X,
            item.LocationTopLeft.Y
        );
    }, "FastMoveToStash");
}

// Close stash
LokiPoe.InGameState.StashUi.Close();
```

---

## 13. Monster Properties

### ✅ CORRECT:
```csharp
var monsters = LokiPoe.ObjectManager.GetObjectsByType<Monster>()
    .Where(m => m.IsValid &&
               !m.IsDead &&
               m.IsHostile &&
               m.Distance < 100f)
    .ToList();

foreach (var monster in monsters)
{
    string name = monster.Name;                    // ✅
    string metadata = monster.Metadata;            // ✅
    float distance = monster.Distance;             // ✅
    Vector2i position = monster.Position;          // ✅
    Rarity rarity = monster.Rarity;                // ✅
    bool isBoss = monster.IsBoss;                  // ✅
    float hp = monster.HealthPercentTotal;         // ✅
    uint id = monster.Id;                          // ✅
    bool isDead = monster.IsDead;                  // ✅
    bool isHostile = monster.IsHostile;            // ✅
    bool isValid = monster.IsValid;                // ✅
}
```

---

## 14. Item Properties

### ✅ CORRECT:
```csharp
var inventory = LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Main);

foreach (var item in inventory.Items)
{
    if (item == null || !item.IsValid)
        continue;

    // All these properties WORK:
    string name = item.Name;                       // ✅
    string fullName = item.FullName;               // ✅
    string metadata = item.Metadata;               // ✅
    string itemClass = item.Class;                 // ✅
    bool isEquipped = item.IsEquipped;             // ✅
    bool isValid = item.IsValid;                   // ✅
    Vector2i topLeft = item.LocationTopLeft;       // ✅
    Vector2i size = item.Size;                     // ✅
    int stackCount = item.StackCount;              // ✅
    var components = item.Components;              // ✅
    var stats = item.Stats;                        // ✅

    // For flasks
    int charges = item.CurrentCharges;             // ✅
    bool canUse = item.CanUse;                     // ✅
    bool instant = item.IsInstantRecovery;         // ✅

    // Use item
    var result = await PlayerAction.TakeAction(() =>
    {
        return item.Use();                         // ✅ Use() EXISTS!
    }, "UseItem");
}
```

---

## 15. Generic Object Finding

### ✅ CORRECT:
```csharp
// For generic objects (map device, etc.)
var mapDevice = LokiPoe.ObjectManager.Objects
    .Where(obj => obj.IsValid &&
                 obj.Distance < 200f &&
                 (obj.Metadata.Contains("MapDevice") ||
                  obj.Name.Contains("Map Device", System.StringComparison.OrdinalIgnoreCase)))
    .OrderBy(obj => obj.Distance)
    .FirstOrDefault();

// For typed objects (monsters, chests, etc.)
var monsters = LokiPoe.ObjectManager.GetObjectsByType<Monster>()
    .Where(m => m.IsValid && m.Distance < 100f)
    .ToList();

var chests = LokiPoe.ObjectManager.GetObjectsByType<Chest>()
    .Where(c => c.IsValid && c.Distance < 100f)
    .ToList();

var loot = LokiPoe.ObjectManager.GetObjectsByType<WorldItem>()
    .Where(item => item.IsValid && item.Distance < 50f)
    .ToList();

// For NPCs - SPECIAL PATTERN!
var npcs = LokiPoe.ObjectManager.Objects
    .OfType<NPC>()  // Use .OfType<NPC>() not GetObjectsByType<NPC>()
    .Where(n => n.IsValid && n.Distance < 300f)
    .ToList();
```

---

## 16. Async/Await Patterns

### ✅ CORRECT:
```csharp
// Use Coroutine.Sleep, NOT Task.Delay
await Coroutine.Sleep(500);

// Finish current action before interactions
await Coroutines.FinishCurrentAction();

// Interact with objects
var interactResult = await Coroutines.InteractWith(networkObject);

// Or use PlayerAction wrapper
var result = await PlayerAction.TakeAction(() =>
{
    return obj.Interact();
}, "InteractAction");
```

---

## 17. Combat Patterns

### ✅ CORRECT:
```csharp
// Use skill on target
var combatResult = await PlayerAction.TakeAction(() =>
{
    return LokiPoe.InGameState.SkillBarHud.UseAt(
        skillSlot,      // int - slot number (0-12)
        false,          // bool - use shift modifier
        targetPosition  // Vector2i - target position
    );
}, "UseSkill");

// Or use combat logic
await CombatLogic.Current.Fight();
```

---

## 18. Movement

### ✅ CORRECT:
```csharp
// Move towards position
var moveResult = PlayerMoverManager.Current.MoveTowards(targetPosition);

if (!moveResult)
{
    Log.Warn("Failed to move");
}

// Move towards object
PlayerMoverManager.Current.MoveTowards(monster.Position);
```

---

## 19. Blacklist

### ✅ CORRECT:
```csharp
// Add to blacklist
Blacklist.Add(objectId, TimeSpan.FromMinutes(10), "Reason");

// Check blacklist
if (Blacklist.Contains(objectId))
{
    continue;  // Skip this object
}

// Example with monster
if (!Blacklist.Contains(monster.Id))
{
    // Do something with monster

    // Then blacklist it
    Blacklist.Add(monster.Id, TimeSpan.FromMinutes(10), "Already processed");
}
```

---

## 20. Complete Working Example

### Finding and Capturing a Beast:
```csharp
using System.Linq;
using System.Threading.Tasks;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using log4net;

public class BeastCaptureExample
{
    private static readonly ILog Log = Logger.GetLoggerInstanceForType();

    public async Task<bool> CaptureNearbyBeast()
    {
        // Find beasts
        var beasts = LokiPoe.ObjectManager.GetObjectsByType<Monster>()
            .Where(m => m.IsValid &&
                       !m.IsDead &&
                       m.IsHostile &&
                       m.Distance < 80f &&
                       m.Metadata.Contains("Bestiary") &&
                       !Blacklist.Contains(m.Id))
            .OrderBy(m => m.Distance)
            .ToList();

        if (!beasts.Any())
        {
            Log.Debug("No beasts found");
            return false;
        }

        var beast = beasts.First();

        Log.InfoFormat("Found beast: {0} (Rarity: {1}, HP: {2:F1}%, Distance: {3:F1})",
            beast.Name, beast.Rarity, beast.HealthPercentTotal, beast.Distance);

        // Attack beast
        var combatResult = await PlayerAction.TakeAction(() =>
        {
            return LokiPoe.InGameState.SkillBarHud.UseAt(
                0,           // Primary skill slot
                false,       // No shift modifier
                beast.Position
            );
        }, "AttackBeast");

        if (!combatResult)
        {
            Log.Warn("Failed to attack beast");
            return false;
        }

        // Check if beast is dead (Einhar will capture it)
        if (beast.IsDead || !beast.IsValid)
        {
            Log.Info("Beast killed - Einhar will capture it");
            Blacklist.Add(beast.Id, TimeSpan.FromMinutes(10), "Captured");
            return true;
        }

        return false;
    }
}
```

---

**End of Quick Reference**

Use this as a copy-paste reference when fixing API compatibility issues.
All patterns shown here are from WORKING CODE in your project.
