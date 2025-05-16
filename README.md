# Unity Essentials

**Unity Essentials** is a lightweight, modular utility namespace designed to streamline development in Unity. 
It provides a collection of foundational tools, extensions, and helpers to enhance productivity and maintain clean code architecture.

## 📦 This Package

This package is part of the **Unity Essentials** ecosystem.  
It integrates seamlessly with other Unity Essentials modules and follows the same lightweight, dependency-free philosophy.

## 🌐 Namespace

All utilities are under the `UnityEssentials` namespace. This keeps your project clean, consistent, and conflict-free.

```csharp
using UnityEssentials;
```


# SerializeDictionary
Each example is immediately usable in the Unity Editor and demonstrates a different TKey/TValue combination or UI feature.


## Usage Example 



1. Basic Usage in a MonoBehaviour
```csharp

using UnityEngine;
using UnityEssentials;

public class InventorySystem : MonoBehaviour
{
    [SerializeField]
    private SerializeDictionary<string, int> _stringInt = new();
    [SerializeField]
    private SerializeDictionary<string, Color> _stringColor = new();
    [SerializeField]
    private SerializeDictionary<string, SomeEnum> _stringEnum = new();
}
```
Stores item names with integer quantities.



2. Custom Struct Key and Value

```csharp
using UnityEngine;
using UnityEssentials;

[System.Serializable]
public struct ItemID
{
    public int Id;
    public string Name;
}

[System.Serializable]
public struct ItemData
{
    public int MaxStack;
    public float Weight;
}

public class ItemDatabase : MonoBehaviour
{
    [SerializeField]
    private SerializeDictionary<ItemID, ItemData> _itemDatabase = new();
}
```
Uses custom structs as keys and values.



3. Enum as Key with ScriptableObject as Value

```csharp
using UnityEngine;
using UnityEssentials;

public enum WeaponSlot
{
    Primary,
    Secondary,
    Melee
}

[CreateAssetMenu]
public class WeaponData : ScriptableObject
{
    public string WeaponName;
    public int Damage;
}

public class WeaponManager : MonoBehaviour
{
    [SerializeField]
    private SerializeDictionary<WeaponSlot, WeaponData> _equippedWeapons = new();
}
```

Manages weapons per slot using an enum.

4. Serializable Class as Key

```csharp
using UnityEngine;
using UnityEssentials;

[System.Serializable]
public class EnemyType
{
    public string Name;
    public int Tier;

    public override int GetHashCode() => name.GetHashCode() ^ tier.GetHashCode();
    public override bool Equals(object obj)
    {
        if (obj is not EnemyType other) return false;
        return name == other.name && tier == other.tier;
    }
}

public class EnemySpawnSystem : MonoBehaviour
{
    [SerializeField]
    private SerializeDictionary<EnemyType, int> _spawnChances = new();
}
```
Handles spawn chances per enemy type. Requires overriding Equals and GetHashCode.


5. Nested Dictionary

```csharp
using UnityEngine;
using UnityEssentials;

public class GameStats : MonoBehaviour
{
    [SerializeField]
    private SerializeDictionary<string, SerializeDictionary<string, int>> _playerStats = new();
}
```
Keeps per-player dictionaries of stat names to stat values.



6. Dictionary of Vector3 Keys

```csharp
using UnityEngine;
using UnityEssentials;

public class PositionTracker : MonoBehaviour
{
    [SerializeField]
    private SerializeDictionary<Vector3, string> _locationTags = new();
}
```
Associates world positions with named tags.



7. Dictionary of GameObject References

```csharp
using UnityEngine;
using UnityEssentials;

public class ObjectLinker : MonoBehaviour
{
    [SerializeField]
    private SerializeDictionary<string, GameObject> _namedReferences = new();
}
```
Stores named references to scene objects.



8. Usage with SplitWeightAttribute

```csharp
using UnityEngine;
using UnityEssentials;

public class WeightedDictionaryExample : MonoBehaviour
{
    [SplitWeight(2f)]
    [SerializeField]
    private SerializeDictionary<string, string> _translations = new();
}
```
Expands the key field width in the inspector using the SplitWeightAttribute.
