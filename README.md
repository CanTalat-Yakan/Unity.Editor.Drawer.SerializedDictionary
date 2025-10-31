# Unity Essentials

This module is part of the Unity Essentials ecosystem and follows the same lightweight, editor-first approach.
Unity Essentials is a lightweight, modular set of editor utilities and helpers that streamline Unity development. It focuses on clean, dependency-free tools that work well together.

All utilities are under the `UnityEssentials` namespace.

```csharp
using UnityEssentials;
```

## Installation

Install the Unity Essentials entry package via Unity's Package Manager, then install modules from the Tools menu.

- Add the entry package (via Git URL)
    - Window → Package Manager
    - "+" → "Add package from git URL…"
    - Paste: `https://github.com/CanTalat-Yakan/UnityEssentials.git`

- Install or update Unity Essentials packages
    - Tools → Install & Update UnityEssentials
    - Install all or select individual modules; run again anytime to update

---

# Serialized Dictionary Drawer

> Quick overview: A strongly-typed, serializable dictionary with a smooth inspector experience. Edit key–value pairs in a reorderable list, support nested types, and tune the key/value width split with an attribute.

This package provides a generic `SerializedDictionary<TKey, TValue>` you can use as a field, plus a custom inspector drawer that renders entries as a reorderable list with add/remove, drag-to-reorder, and an adjustable size field. Keys and values can be complex/nested types. Duplicate keys are handled safely during deserialization.

![screenshot](Documentation/Screenshot.png)

## Features
- Inspector UI
  - Reorderable list with add/remove controls and drag-to-reorder
  - Foldout header with an inline size field to grow/shrink the list
  - Adjustable key/value width via `[KeyValueSplitWeight(keyWeight)]`
  - Supports nested/complex property types for both keys and values
- Runtime API
  - Generic `SerializedDictionary<TKey, TValue>` implements `IDictionary<TKey,TValue>`
  - Helpers: `TryAdd`, `TryGetValue(defaultValue, out value)`, `CopyFrom`, `AddFrom`
  - Access the underlying `Dictionary` via the `Dictionary` property
- Serialization safety
  - Converts to a serializable list on save and reconstructs the dictionary on load
  - If duplicates appear, the entry is reassigned to a “default key” for common key types (string, int, float, Color) or `default(TKey)`

## Requirements
- Unity Editor 6000.0+ (Editor-only GUI; runtime container lives in Runtime)
- Depends on the Unity Essentials Inspector Hooks module (drawer uses its utilities)

Tip: Prefer unique, non-null keys. If you change key values manually in the inspector and duplicates occur, they’ll be remapped to a default key on deserialize.

## Usage
Define and edit in the Inspector

```csharp
using UnityEngine;
using UnityEssentials;

public class EnemyConfig : MonoBehaviour
{
    // Key = enemy id (string), Value = spawn count (int)
    [KeyValueSplitWeight(1.2f)]
    public SerializedDictionary<string, int> Spawns;
}
```

Work with it at runtime

```csharp
void Start()
{
    // Add or set
    Spawns["orc"] = 3;
    Spawns.TryAdd("goblin", 5);

    // Read with default
    Spawns.TryGetValue("dragon", 0, out var dragonCount); // returns 0 if missing

    // Iterate
    foreach (var (key, value) in Spawns.Dictionary)
        Debug.Log($"{key}: {value}");
}
```

Nested types

```csharp
[System.Serializable]
public struct Loot
{
    public string ItemId;
    public int Count;
}

public class LootTable : MonoBehaviour
{
    // Key can be an enum, object reference, or struct; Value can be a struct or nested type
    public SerializedDictionary<string, Loot> Drops;
}
```

## How It Works
- Container: `SerializedDictionary<TKey,TValue>`
  - Internally stores entries in a serialized `List<SerializedKeyValuePair<TKey,TValue>>`
  - OnBeforeSerialize: writes the current dictionary to the list
  - OnAfterDeserialize: rebuilds the dictionary from the list
  - Duplicate keys: if a key already exists, the new entry is assigned to a “default key” (string.Empty, 0, 0f, Color.black, or `default(TKey)`)
- Inspector drawer
  - Renders `_entries` as a `ReorderableList`
  - Foldout header + inline size field; add/remove and drag reordering supported
  - Draws key/value side-by-side; key width is controlled by `[KeyValueSplitWeight]` on the field
  - Supports nested properties (iterates children for complex values)

## Notes and Limitations
- Keys must be unique and non-null; the drawer won’t enforce uniqueness while editing
- Duplicate handling: on deserialize, duplicates are remapped to a default key; for custom key types, this falls back to `default(TKey)`
- Serialization scope: both key and value types must be serializable by Unity
- Multi-object editing follows Unity’s standard rules for property drawers
- The UI depends on the Inspector Hooks utilities; ensure that module is present

## Files in This Package
- `Runtime/SerializedDictionary.cs` – Generic container, pair struct, and `[KeyValueSplitWeight]`
- `Runtime/SerializedDictionaryEditor.cs` – Serialization callbacks and duplicate-key handling
- `Editor/SerializedDictionaryDrawer.cs` – Reorderable inspector UI and key/value layout
- `Runtime/UnityEssentials.SerializedDictionary.asmdef` – Runtime assembly definition

## Tags
unity, unity-editor, dictionary, serialized, key-value, propertydrawer, reorderable-list, inspector, tools, workflow
