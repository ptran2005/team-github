using UnityEngine;
using UnityEngine.InputSystem;
using System;

// The player's running totals (money, day, customers) and the link to the save file.
// Loads the current save slot when the scene starts and saves after every customer.
public class CafeProgress : MonoBehaviour
{
    [SerializeField] private bool loadOnStart = true;
    [SerializeField] private bool autosave = true;
    [Tooltip("F5 saves, F9 loads. Handy for testing")]
    [SerializeField] private bool debugKeys = true;

    public static CafeProgress Instance { get; private set; }
    public SaveData Data { get; private set; } = new SaveData();
    public int Slot => SaveSystem.CurrentSlot;

    public event Action Changed;

    private void Awake()
    {
        Instance = this;
        if (loadOnStart) Load();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!debugKeys) return;
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.f5Key.wasPressedThisFrame) Save();
        else if (keyboard.f9Key.wasPressedThisFrame) Load();
    }

    private void OnApplicationQuit()
    {
        if (autosave) WriteSave(false);
    }

    public void RecordCustomerServed(int moneyEarned)
    {
        Data.money += moneyEarned;
        Data.customersServed++;
        Changed?.Invoke();
        if (autosave) WriteSave(false);
    }

    public void RecordCustomerLost()
    {
        Data.customersLost++;
        Changed?.Invoke();
        if (autosave) WriteSave(false);
    }

    // For the day/clock system to call when a day ends
    public void NextDay()
    {
        Data.day++;
        Changed?.Invoke();
        if (autosave) WriteSave(false);
    }

    [ContextMenu("Save Now")]
    public void Save() => WriteSave(true);

    [ContextMenu("Load")]
    public void Load()
    {
        SaveData loaded = SaveSystem.Load(Slot);
        Data = loaded ?? new SaveData();
        Changed?.Invoke();
        if (loaded != null) CafeHUD.ShowMessage($"Loaded slot {Slot + 1}: day {Data.day}, ${Data.money}");
    }

    private void WriteSave(bool announce)
    {
        Data.lastSaved = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        if (SaveSystem.Save(Slot, Data) && announce) CafeHUD.ShowMessage($"Saved to slot {Slot + 1}");
    }

    [ContextMenu("Start Over (deletes this slot)")]
    public void StartOver()
    {
        SaveSystem.Delete(Slot);
        Data = new SaveData();
        Changed?.Invoke();
    }

#if UNITY_EDITOR
    [ContextMenu("Show Save Files")]
    private void ShowSaveFolder()
    {
        UnityEditor.EditorUtility.RevealInFinder(Application.persistentDataPath);
    }
#endif
}
