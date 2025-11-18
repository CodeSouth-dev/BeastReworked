# API Compatibility Fixes Based on Reference Code

## Key Findings from Reference Code Analysis

### ✅ CORRECT API PATTERNS (from ExampleRoutine, ExamplePlugin, ObjectExplorer):

#### 1. **Using Directives** (ALL CORRECT)
```csharp
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Bot.Pathfinding;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Coroutine;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using DreamPoeBot.Loki.RemoteMemoryObjects;  // IMPORTANT: This namespace exists!
```

#### 2. **Type Names**
- ✅ `Npc` (not `NPC`) - Use `.OfType<Npc>()`
- ✅ `Portal` - Exists, use `.OfType<Portal>()`
- ✅ `Monster` - Correct
- ✅ `DatWorldAreaWrapper` - For CurrentWorldArea
- ✅ `InventorySlot` enum - Exists in GameData namespace
- ✅ `Rarity` enum - Exists in GameData namespace

#### 3. **InventorySlot Usage**
```csharp
// CORRECT:
LokiPoe.InstanceInfo.GetPlayerInventoryItemsBySlot(InventorySlot.Main)
LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Main)
LokiPoe.InstanceInfo.GetPlayerInventoryItemsBySlot(InventorySlot.Flasks)
```

#### 4. **Item API**
```csharp
// CORRECT - Item properties that exist:
item.CanUse          // bool
item.CurrentCharges  // int
item.Class           // string (e.g., ItemClasses.LifeFlask)
item.Name            // string
item.FullName        // string
item.IsInstantRecovery  // bool
item.LocalStats      // Dictionary<StatTypeGGG, int>
item.Components.ModsComponent.Rarity  // Rarity enum

// WRONG - These don't exist:
item.Use()           // ❌ Method doesn't exist
item.IsEquipped      // ❌ Property doesn't exist
```

#### 5. **Rarity Enum**
```csharp
// CORRECT:
if (monster.Rarity == Rarity.Unique) { }
if (monster.Rarity == Rarity.Rare) { }
if (monster.Rarity == Rarity.Magic) { }
if (monster.Rarity == Rarity.Normal) { }

// For items:
var mods = item.Components.ModsComponent;
var rarity = mods == null ? Rarity.Normal : mods.Rarity;
```

#### 6. **Object Retrieval Patterns**
```csharp
// CORRECT for NPCs:
var npcs = LokiPoe.ObjectManager.Objects.OfType<Npc>().ToList();

// CORRECT for Monsters:
var monsters = LokiPoe.ObjectManager.Objects.OfType<Monster>().ToList();
// OR:
var monsters = LokiPoe.ObjectManager.GetObjectsByType<Monster>();

// CORRECT for Portals:
var portals = LokiPoe.ObjectManager.Objects.OfType<Portal>().ToList();
```

#### 7. **WorldArea (DatWorldAreaWrapper)**
```csharp
// CORRECT:
var currentArea = LokiPoe.CurrentWorldArea;  // Returns DatWorldAreaWrapper

// Properties that EXIST (based on reference code usage):
currentArea.Name    // string
currentArea.Id      // string

// Properties that MAY NOT EXIST on DatWorldAreaWrapper:
// - IsHideout, IsTown, IsMap, IsCombatArea, IsBossArea, IsUniqueMapArea
// Need to find alternative detection methods
```

#### 8. **LokiPoe API Methods**
```csharp
// CORRECT:
LokiPoe.ProcessHookManager.ClearAllKeyStates();  // Not LokiPoe.Input.ClearAllKeyStates()
LokiPoe.ObjectManager.Objects
LokiPoe.ObjectManager.GetObjectsByType<T>()
LokiPoe.InstanceInfo.GetPlayerInventoryItemsBySlot()
LokiPoe.InstanceInfo.MinimapIcons
LokiPoe.Me.EquippedItems
LokiPoe.Me.AvailableSkills
LokiPoe.Me.Auras

// WRONG:
LokiPoe.GameController      // ❌ Doesn't exist
LokiPoe.Input.ClearAllKeyStates()  // ❌ Wrong location
```

#### 9. **Combat & Targeting**
```csharp
// CORRECT:
CombatTargeting.Targets<Monster>().FirstOrDefault()
CombatTargeting.Update()
Blacklist.Add(objectId, TimeSpan, reason)
ExilePather.PathDistance(from, to, includeRooms, includeClosedDoors)
```

#### 10. **String.Contains()**
```csharp
// WRONG - 2 parameter version doesn't exist:
str.Contains("text", StringComparison.OrdinalIgnoreCase)  // ❌

// CORRECT - Use 1 parameter:
str.ToLower().Contains("text")  // ✅
// OR:
str.IndexOf("text", StringComparison.OrdinalIgnoreCase) >= 0  // ✅
```

---

## FIXES NEEDED

### Priority 1: Type Name Changes
1. Change `NPC` → `Npc` everywhere
2. Add `using DreamPoeBot.Loki.RemoteMemoryObjects;` where needed

### Priority 2: InventorySlot enum
- Already correct in code, just needs proper using directive

### Priority 3: Item.Use() replacements
- Need to find correct method to use items (check Flask usage in reference code)

### Priority 4: String.Contains() fixes
- Replace all 2-parameter Contains() with alternative

### Priority 5: WorldArea property alternatives
- Find how to detect hideout/town/map without IsHideout property

### Priority 6: Item.IsEquipped replacement
- Check if item is in equipped slots instead

### Priority 7: Monster.IsBoss replacement
- Find alternative boss detection method
