using System;
using System.Collections.Generic;
using UnityEngine;

// Lightweight persistence via PlayerPrefs: credits, unlocked weapons, selections, daily crate.
public static class SaveSystem
{
    public static int Credits
    {
        get => PlayerPrefs.GetInt("credits", 0);
        set { PlayerPrefs.SetInt("credits", Mathf.Max(0, value)); PlayerPrefs.Save(); }
    }
    public static void AddCredits(int n) { Credits = Credits + n; }

    public static int XP
    {
        get => PlayerPrefs.GetInt("xp", 0);
        private set { PlayerPrefs.SetInt("xp", Mathf.Max(0, value)); PlayerPrefs.Save(); }
    }
    public static void AddXP(int n) { XP = XP + Mathf.Max(0, n); }

    public static int SelectedChassis
    {
        get => PlayerPrefs.GetInt("chassis", 0);
        set { PlayerPrefs.SetInt("chassis", value); PlayerPrefs.Save(); }
    }

    public static Wep SelectedWeapon
    {
        get => (Wep)PlayerPrefs.GetInt("weapon", (int)Wep.Blaster);
        set { PlayerPrefs.SetInt("weapon", (int)value); PlayerPrefs.Save(); }
    }

    public static bool IsWeaponUnlocked(Wep w)
    {
        if (w == Wep.Blaster) return true; // starter always owned
        return PlayerPrefs.GetInt("wep_" + (int)w, 0) == 1;
    }
    public static void UnlockWeapon(Wep w)
    {
        PlayerPrefs.SetInt("wep_" + (int)w, 1); PlayerPrefs.Save();
    }
    public static List<Wep> UnlockedWeapons()
    {
        var list = new List<Wep>();
        foreach (Wep w in Enum.GetValues(typeof(Wep)))
            if (IsWeaponUnlocked(w)) list.Add(w);
        return list;
    }

    // ---- Chassis unlocks (first 3 are free starters) ----
    public static bool IsChassisUnlocked(int i) => i < 3 || PlayerPrefs.GetInt("chx" + i, 0) == 1;
    public static void UnlockChassis(int i) { PlayerPrefs.SetInt("chx" + i, 1); PlayerPrefs.Save(); }

    // ---- Ability unlocks (a chassis's signature ability is free on that chassis) ----
    public static bool IsAbilityUnlocked(AbilityType a) => PlayerPrefs.GetInt("abx" + (int)a, 0) == 1;
    public static void UnlockAbility(AbilityType a) { PlayerPrefs.SetInt("abx" + (int)a, 1); PlayerPrefs.Save(); }

    // ---- Per-chassis loadout (a mech remembers its own weapon + ability) ----
    public static Wep GetChassisWeapon(int c) => (Wep)PlayerPrefs.GetInt("cw" + c, (int)Wep.Blaster);
    public static void SetChassisWeapon(int c, Wep w) { PlayerPrefs.SetInt("cw" + c, (int)w); PlayerPrefs.Save(); }
    public static int GetChassisAbility(int c, int def) => PlayerPrefs.GetInt("ca" + c, def);
    public static void SetChassisAbility(int c, AbilityType a) { PlayerPrefs.SetInt("ca" + c, (int)a); PlayerPrefs.Save(); }

    // ---- Squad (3 slots for the hotbar; each stores only a chassis, loadout comes from the chassis) ----
    public static int GetSquadChassis(int slot) => PlayerPrefs.GetInt("sqC" + slot, slot);   // default 0,1,2
    public static void SetSquadChassis(int slot, int c) { PlayerPrefs.SetInt("sqC" + slot, c); PlayerPrefs.Save(); }

    // ---- Daily crate ----
    // Stored as an ordinal day number so we don't parse dates.
    static int Today()
    {
        DateTime n = DateTime.Now.Date;
        return (n.Year * 372) + (n.Month * 31) + n.Day;
    }
    public static bool CrateAvailable() => PlayerPrefs.GetInt("crateDay", -1) != Today();
    public static void MarkCrateClaimed() { PlayerPrefs.SetInt("crateDay", Today()); PlayerPrefs.Save(); }

    // ---------- Backup / Restore (JSON) ----------
    [System.Serializable] class KV { public string k; public int v; }
    [System.Serializable] class Backup { public int version = 1; public List<KV> items = new List<KV>(); }

    static void AddKeys(System.Action<string> add)
    {
        add("credits"); add("xp"); add("crateDay"); add("chassis"); add("weapon");
        for (int i = 0; i < 40; i++) { add("chx" + i); add("cw" + i); add("ca" + i); }
        for (int i = 0; i < 40; i++) add("wep_" + i);
        for (int i = 0; i < 20; i++) add("abx" + i);
        for (int i = 0; i < 3; i++) add("sqC" + i);
    }

    public static string ExportJson()
    {
        var b = new Backup();
        AddKeys(k => { if (PlayerPrefs.HasKey(k)) b.items.Add(new KV { k = k, v = PlayerPrefs.GetInt(k, 0) }); });
        return JsonUtility.ToJson(b, true);
    }

    public static bool ImportJson(string json)
    {
        try
        {
            var b = JsonUtility.FromJson<Backup>(json);
            if (b == null || b.items == null || b.items.Count == 0) return false;
            foreach (var kv in b.items) if (!string.IsNullOrEmpty(kv.k)) PlayerPrefs.SetInt(kv.k, kv.v);
            PlayerPrefs.Save();
            return true;
        }
        catch { return false; }
    }

    public static string BackupPath => System.IO.Path.Combine(Application.persistentDataPath, "mecharena_backup.json");
    const string DownloadPath = "/storage/emulated/0/Download/mecharena_backup.json";

    public static bool BackupToFile(out string path)
    {
        path = BackupPath;
        try
        {
            string json = ExportJson();
            System.IO.File.WriteAllText(BackupPath, json);
            try { System.IO.File.WriteAllText(DownloadPath, json); path = DownloadPath; } catch { }  // bonus if storage allows
            return true;
        }
        catch { return false; }
    }

    public static bool RestoreFromFile(out string path)
    {
        path = "";
        foreach (var p in new[] { DownloadPath, BackupPath })
        {
            try { if (System.IO.File.Exists(p)) { if (ImportJson(System.IO.File.ReadAllText(p))) { path = p; return true; } } }
            catch { }
        }
        return false;
    }
}
