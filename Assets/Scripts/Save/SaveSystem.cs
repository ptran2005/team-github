using UnityEngine;
using System;
using System.IO;

// Reads and writes save slots as JSON files in Application.persistentDataPath
// (on Windows: C:\Users\<you>\AppData\LocalLow\<company>\<product>).
public static class SaveSystem
{
    public const int SlotCount = 3;

    // The menu sets this before loading the game scene; CafeProgress loads from it
    public static int CurrentSlot { get; set; }

    public static string GetPath(int slot)
    {
        return Path.Combine(Application.persistentDataPath, $"save_slot{slot + 1}.json");
    }

    public static bool HasSave(int slot)
    {
        return IsValid(slot) && File.Exists(GetPath(slot));
    }

    public static bool Save(int slot, SaveData data)
    {
        if (!IsValid(slot) || data == null) return false;
        try
        {
            File.WriteAllText(GetPath(slot), JsonUtility.ToJson(data, true));
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Couldn't save slot {slot + 1}: {e.Message}");
            return false;
        }
    }

    // Returns null if the slot is empty or the file can't be read
    public static SaveData Load(int slot)
    {
        if (!HasSave(slot)) return null;
        try
        {
            return JsonUtility.FromJson<SaveData>(File.ReadAllText(GetPath(slot)));
        }
        catch (Exception e)
        {
            Debug.LogError($"Save slot {slot + 1} is unreadable, starting fresh: {e.Message}");
            return null;
        }
    }

    public static void Delete(int slot)
    {
        if (HasSave(slot)) File.Delete(GetPath(slot));
    }

    private static bool IsValid(int slot)
    {
        if (slot >= 0 && slot < SlotCount) return true;
        Debug.LogError($"There's no save slot {slot + 1} (slots go from 1 to {SlotCount}).");
        return false;
    }
}
