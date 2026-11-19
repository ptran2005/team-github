using System;

// Everything that gets written to a save file. To save something new, add a field here
// (with a default value) and fill it in from CafeProgress. Old save files still load.
[Serializable]
public class SaveData
{
    public int version = 1;
    public int day = 1;
    public int money;
    public int customersServed;
    public int customersLost;
    public string lastSaved; // local time, for showing on a save slot button
}
