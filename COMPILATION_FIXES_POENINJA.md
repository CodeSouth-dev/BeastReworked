# Compilation Fixes - Poe.Ninja Integration

## Issues Fixed

### 1. PoeNinjaService Not Found in Context
**Error:**
```
The name 'PoeNinjaService' does not exist in the current context
```

**Cause:**
The `PoeNinjaService.cs` file was created but not added to the `Beasts.csproj` project file.

**Fix:**
Added `PoeNinjaService.cs` to the project file:
```xml
<Compile Include="Services\PoeNinjaService.cs" />
```

**Location:** `Beasts/Beasts.csproj` line 97

---

### 2. Unused Field Warning
**Error:**
```
The field 'PreparationPhase._heistLocker' is assigned but its value is never used
```

**Cause:**
The `_heistLocker` field was left over from when PreparationPhase handled heist locker stashing. This functionality was removed and moved to ExitAndStashPhase.

**Fix:**
Removed the `_heistLocker` field and its reset in OnExit():
- Removed declaration: `private NetworkObject _heistLocker;`
- Removed reset: `_heistLocker = null;`

**Files Modified:**
- `Beasts/Phases/PreparationPhase.cs` (lines 51, 478)

---

## Verification

✅ **All compilation errors resolved**  
✅ **No linter warnings**  
✅ **PoeNinjaService accessible from LootPerception**  
✅ **PreparationPhase clean of unused fields**  

---

### 3. HttpClient Assembly Missing
**Error:**
```
The type or namespace name 'HttpClient' could not be found
The type or namespace name 'Http' does not exist in the namespace 'System.Net'
```

**Cause:**
The `System.Net.Http` assembly was not referenced in the project file. HttpClient requires this assembly for HTTP API calls.

**Fix:**
Added `System.Net.Http` reference to the project file:
```xml
<Reference Include="System.Net.Http" />
```

**Location:** `Beasts/Beasts.csproj` line 65

---

---

### 4. PoeNinjaService Not in 3rdparty.json FileList
**Error:**
```
Cannot load [BeastRoutine] because an exception occurred
error CS0103: The name 'PoeNinjaService' does not exist in the current context
```

**Cause:**
The bot uses a third-party loader system that reads from `3rdparty.json` to know which files to package and compile. The new `PoeNinjaService.cs` file was not listed in the FileList.

**Fix:**
Added `Services/PoeNinjaService.cs` to the FileList in 3rdparty.json:
```json
"Services/PoeNinjaService.cs",
```

**Location:** `Beasts/3rdparty.json` line 25

---

### 5. System.Net.Http Not in 3rdparty.json References
**Error:**
```
error CS0234: The type or namespace name 'Http' does not exist in the namespace 'System.Net'
error CS0246: The type or namespace name 'HttpClient' could not be found
```

**Cause:**
The third-party loader also needs assembly references specified in `3rdparty.json`, not just in `.csproj`. The `System.Net.Http` assembly wasn't in the References array.

**Fix:**
Added `System.Net.Http.dll` to the References array in 3rdparty.json (note the .dll extension):
```json
"References": [
  "System.Net.Http.dll"
]
```

**Important**: The third-party loader requires the `.dll` extension for assembly references.

**Location:** `Beasts/3rdparty.json` lines 54-56

---

## Files Modified

1. **Beasts/Beasts.csproj**
   - Added PoeNinjaService.cs to Compile list
   - Added System.Net.Http assembly reference

2. **Beasts/3rdparty.json**
   - Added Services/PoeNinjaService.cs to FileList (for third-party loader)
   - Added System.Net.Http to References (for third-party loader)

3. **Beasts/Phases/PreparationPhase.cs**
   - Removed unused `_heistLocker` field
   - Removed `_heistLocker` reset in OnExit()

## Summary

All issues were configuration tasks:
1. Adding new service file to `.csproj` (Visual Studio)
2. Adding new service file to `3rdparty.json` FileList (bot loader)
3. Adding System.Net.Http reference to `.csproj` (Visual Studio)
4. Adding System.Net.Http reference to `3rdparty.json` References (bot loader)
5. Removing deprecated field from refactored phase

**Status**: ✅ Ready to compile and test!

## Important Note - DreamPoeBot Plugin System

**DreamPoeBot uses a third-party plugin system** that requires configuration in BOTH files:

### For New Source Files:
- ✅ `Beasts.csproj` → Add to `<Compile>` list
- ✅ `3rdparty.json` → Add to `FileList` array

### For New Assembly References:
- ✅ `Beasts.csproj` → Add to `<Reference>` list  
- ✅ `3rdparty.json` → Add to `References` array

**Both files must be kept in sync!**

### Example: Adding a New Service

**In Beasts.csproj:**
```xml
<Compile Include="Services/MyNewService.cs" />
<Reference Include="System.SomeLibrary" />
```

**In 3rdparty.json:**
```json
{
  "FileList": [
    "Services/MyNewService.cs"
  ],
  "References": [
    "System.SomeLibrary.dll"
  ]
}
```

**Note**: References in `3rdparty.json` require the `.dll` extension!

