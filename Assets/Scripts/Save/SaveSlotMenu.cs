using UnityEngine;
using UnityEngine.SceneManagement;

// Optional: for New Game / Continue buttons on the main menu.
// Hook a Button's OnClick to ContinueSlot or NewGameInSlot and type the slot number (0, 1 or 2).
public class SaveSlotMenu : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "MainGame";

    public void ContinueSlot(int slot)
    {
        SaveSystem.CurrentSlot = slot;
        SceneManager.LoadScene(gameSceneName);
    }

    public void NewGameInSlot(int slot)
    {
        SaveSystem.Delete(slot);
        ContinueSlot(slot);
    }

    // e.g. "Day 3 - $120" for a slot button's label, or "Empty"
    public static string DescribeSlot(int slot)
    {
        SaveData data = SaveSystem.Load(slot);
        return data == null ? "Empty" : $"Day {data.day} - ${data.money}";
    }
}
