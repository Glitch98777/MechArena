using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public enum GameState { Menu, Hangar, Battle, Result, Crate }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public UIManager UI { get; private set; }
    public GameState State { get; private set; }
    public float ArenaRadius = 34f;

    public readonly List<Mech> Mechs = new List<Mech>();
    public Mech Player { get; private set; }
    public PlayerController PlayerCtrl { get; private set; }

    MechData[] catalog;
    int browseChassis;
    Wep browseWeapon;
    AbilityType browseAbility;
    public bool Paused { get; private set; }

    Camera cam;
    Mech preview;
    Crate crate;
    float shakeAmp, shakeTimer;
    float lookYaw, lookPitch;
    public float CameraYaw => lookYaw;
    public void AddLook(Vector2 delta) { lookYaw += delta.x * 0.18f; lookPitch -= delta.y * 0.12f; lookPitch = Mathf.Clamp(lookPitch, -26f, 34f); }
    static readonly AbilityType[] FairBotAbilities = { AbilityType.Dash, AbilityType.Overdrive, AbilityType.Heal, AbilityType.Slam };
    GameObject arena;
    Material floorMat;
    public string CurrentMap { get; private set; } = "";
    static readonly string[] MapNames = { "GRID", "FOUNDRY", "CANYON", "NEXUS" };

    // player squad (mid-battle hotbar)
    int[] squadChassis = new int[3];
    Wep[] squadWeapon = new Wep[3];
    AbilityType[] squadAbility = new AbilityType[3];
    float[] squadHpFrac = new float[3];
    bool[] squadDead = new bool[3];
    int activeSlot;
    float switchCd;
    int battleKills, battleEarnings;
    readonly List<int> chassisByStrength = new List<int>();   // weakest -> strongest
    readonly List<Wep> botWeapons = new List<Wep>();          // non-legendary, weakest -> strongest

    public MechData BrowseChassis => catalog[browseChassis];
    public WeaponDef BrowseWeapon => WeaponDef.Get(browseWeapon);
    public bool BrowseWeaponOwned => SaveSystem.IsWeaponUnlocked(browseWeapon);
    public bool BrowseChassisOwned => SaveSystem.IsChassisUnlocked(browseChassis);
    public int BrowseChassisPrice => catalog[browseChassis].price;
    public int PlayerRank => Rank.LevelOf(SaveSystem.XP);
    public int BrowseChassisReqRank => catalog[browseChassis].reqRank;
    public bool BrowseChassisRankOK => PlayerRank >= catalog[browseChassis].reqRank;
    public int BrowseWeaponReqRank => BrowseWeapon.reqRank;
    public bool BrowseWeaponRankOK => PlayerRank >= BrowseWeapon.reqRank;
    public AbilityDef BrowseAbility => AbilityDef.Get(browseAbility);
    public bool BrowseAbilityIsSignature => browseAbility == catalog[browseChassis].ability;
    public bool BrowseAbilityOwned => BrowseAbilityIsSignature || SaveSystem.IsAbilityUnlocked(browseAbility);
    public bool BrowseEquipped => browseChassis == SaveSystem.SelectedChassis && browseWeapon == SaveSystem.SelectedWeapon;
    public int Credits => SaveSystem.Credits;
    public string LastReward { get; private set; } = "";
    public string SquadSlotName(int i) => catalog[Mathf.Clamp(SaveSystem.GetSquadChassis(i), 0, catalog.Length - 1)].name;
    public bool SquadSlotIsBrowse(int i) => SaveSystem.GetSquadChassis(i) == browseChassis;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Application.targetFrameRate = 60;
        catalog = MechData.Catalog();
        BuildDifficultyTables();
        browseChassis = Mathf.Clamp(SaveSystem.SelectedChassis, 0, catalog.Length - 1);
        browseWeapon = SaveSystem.SelectedWeapon;

        SetupEnvironment();
        UI = gameObject.AddComponent<UIManager>();
        UI.Build();
        GoMenu();
    }

    // ---------------- environment ----------------
    void SetupEnvironment()
    {
        var camGO = new GameObject("MainCamera");
        camGO.tag = "MainCamera";
        cam = camGO.AddComponent<Camera>();
        camGO.AddComponent<AudioListener>();
        new GameObject("Sfx").AddComponent<SfxManager>();   // procedural audio
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.03f, 0.05f, 0.09f);
        cam.fieldOfView = 52f;

        var sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.95f, 0.88f);
        sun.intensity = 1.5f;
        sun.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(52f, -35f, 0f);

        var rim = new GameObject("Rim").AddComponent<Light>();
        rim.type = LightType.Directional;
        rim.color = new Color(0.35f, 0.55f, 0.95f);
        rim.intensity = 0.6f;
        rim.transform.rotation = Quaternion.Euler(-12f, 150f, 0f);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.22f, 0.3f, 0.45f);
        RenderSettings.ambientEquatorColor = new Color(0.16f, 0.18f, 0.24f);
        RenderSettings.ambientGroundColor = new Color(0.05f, 0.05f, 0.07f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.03f, 0.05f, 0.09f);
        RenderSettings.fogStartDistance = 40f;
        RenderSettings.fogEndDistance = 120f;

        SetupPostFX();
        BuildFloor();
        BuildArena(0);
    }

    // Global bloom / vignette so emissive cores, lasers, and lightning glow.
    void SetupPostFX()
    {
        var camData = cam.GetUniversalAdditionalCameraData();
        if (camData != null) { camData.renderPostProcessing = true; camData.antialiasing = AntialiasingMode.FastApproximateAntialiasing; }

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        var bloom = profile.Add<Bloom>(true);
        bloom.intensity.Override(1.15f);
        bloom.threshold.Override(0.85f);
        bloom.scatter.Override(0.62f);
        var vig = profile.Add<Vignette>(true);
        vig.intensity.Override(0.34f);
        vig.smoothness.Override(0.5f);
        var col = profile.Add<ColorAdjustments>(true);
        col.postExposure.Override(0.15f);
        col.saturation.Override(12f);
        col.contrast.Override(8f);

        var volGO = new GameObject("PostFX");
        var vol = volGO.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 1f;
        vol.profile = profile;
    }

    public void ShakeCam(float amp) { shakeAmp = Mathf.Max(shakeAmp, amp); shakeTimer = 0.35f; }
    public void ActivatePlayerAbility() { if (Player != null) Player.ActivateAbility(); }
    public void SetPaused(bool p) { Paused = p; Time.timeScale = p ? 0f : 1f; UI.ShowPause(p); }

    void BuildFloor()
    {
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.localScale = new Vector3(30, 1, 30);
        Shader sh = Shader.Find("Universal Render Pipeline/Lit"); if (sh == null) sh = Shader.Find("Standard");
        floorMat = new Material(sh);
        var tex = GridTexture();
        if (floorMat.HasProperty("_BaseMap")) { floorMat.SetTexture("_BaseMap", tex); floorMat.SetTextureScale("_BaseMap", new Vector2(60, 60)); }
        floorMat.mainTexture = tex; floorMat.mainTextureScale = new Vector2(60, 60);
        if (floorMat.HasProperty("_Smoothness")) floorMat.SetFloat("_Smoothness", 0.3f);
        floor.GetComponent<MeshRenderer>().sharedMaterial = floorMat;
    }

    // Randomised arena: recolours the floor/ring and lays out obstacles per variant.
    void BuildArena(int variant)
    {
        if (arena != null) Destroy(arena);
        arena = new GameObject("Arena");
        CurrentMap = MapNames[Mathf.Clamp(variant, 0, MapNames.Length - 1)];

        Color ringCol, floorTint;
        switch (variant)
        {
            case 1: ringCol = new Color(1f, 0.55f, 0.2f); floorTint = new Color(0.16f, 0.12f, 0.1f); break;   // FOUNDRY
            case 2: ringCol = new Color(0.7f, 0.4f, 1f);  floorTint = new Color(0.13f, 0.1f, 0.16f); break;   // CANYON
            case 3: ringCol = new Color(0.3f, 1f, 0.6f);  floorTint = new Color(0.08f, 0.14f, 0.11f); break;  // NEXUS
            default: ringCol = new Color(0.2f, 0.7f, 1f); floorTint = new Color(0.1f, 0.12f, 0.16f); break;   // GRID
        }
        if (floorMat != null)
        {
            floorMat.color = floorTint;
            if (floorMat.HasProperty("_BaseColor")) floorMat.SetColor("_BaseColor", floorTint);
        }

        // boundary ring
        var ringMat = MechBuilder.EmissiveMat(ringCol, 1.5f);
        int posts = 44;
        for (int i = 0; i < posts; i++)
        {
            float a = (i / (float)posts) * Mathf.PI * 2f;
            var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(p.GetComponent<Collider>());
            p.transform.SetParent(arena.transform, false);
            p.transform.position = new Vector3(Mathf.Cos(a) * ArenaRadius, 0.9f, Mathf.Sin(a) * ArenaRadius);
            p.transform.localScale = new Vector3(0.35f, 1.8f, 0.35f);
            p.GetComponent<MeshRenderer>().sharedMaterial = ringMat;
        }

        // obstacles / cover
        Material coverA = MechBuilder.Mat(new Color(0.22f, 0.24f, 0.3f), 0.6f, 0.4f);
        Material coverB = MechBuilder.Mat(floorTint * 1.8f + new Color(0.1f, 0.1f, 0.1f), 0.5f, 0.4f);
        Material edge = MechBuilder.EmissiveMat(ringCol, 0.8f);

        if (variant == 0) // GRID: symmetric tall pillars
        {
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3f;
                Cover(new Vector3(Mathf.Cos(a) * 14f, 0, Mathf.Sin(a) * 14f), new Vector3(1.4f, 5f, 1.4f), coverA, edge);
            }
        }
        else if (variant == 1) // FOUNDRY: scattered crate clusters
        {
            for (int i = 0; i < 10; i++)
            {
                Vector3 pos = RandomArenaPoint(7f, 26f);
                float h = Random.Range(1.4f, 2.6f);
                Cover(pos, new Vector3(Random.Range(1.6f, 2.6f), h, Random.Range(1.6f, 2.6f)), coverB, edge);
            }
        }
        else if (variant == 2) // CANYON: big cover blocks
        {
            for (int i = 0; i < 5; i++)
            {
                Vector3 pos = RandomArenaPoint(8f, 24f);
                Cover(pos, new Vector3(Random.Range(3f, 5f), Random.Range(3f, 4.5f), Random.Range(1.6f, 2.4f)), coverA, edge);
            }
        }
        else // NEXUS: ring of short posts + centre obelisk
        {
            Cover(Vector3.zero, new Vector3(2.2f, 6.5f, 2.2f), coverA, edge);
            for (int i = 0; i < 10; i++)
            {
                float a = i * Mathf.PI / 5f;
                Cover(new Vector3(Mathf.Cos(a) * 11f, 0, Mathf.Sin(a) * 11f), new Vector3(1.2f, 2.4f, 1.2f), coverB, edge);
            }
        }
    }

    void Cover(Vector3 groundPos, Vector3 size, Material body, Material edge)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.layer = ObstacleLayer;                       // keep collider for line-of-sight blocking
        g.transform.SetParent(arena.transform, false);
        g.transform.position = groundPos + Vector3.up * size.y * 0.5f;
        g.transform.localScale = size;
        g.GetComponent<MeshRenderer>().sharedMaterial = body;
        var strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(strip.GetComponent<Collider>());
        strip.transform.SetParent(arena.transform, false);
        strip.transform.position = groundPos + Vector3.up * (size.y - 0.12f);
        strip.transform.localScale = new Vector3(size.x * 1.02f, 0.12f, size.z * 1.02f);
        strip.GetComponent<MeshRenderer>().sharedMaterial = edge;
    }

    Vector3 RandomArenaPoint(float minR, float maxR)
    {
        for (int tries = 0; tries < 12; tries++)
        {
            float ang = Random.value * Mathf.PI * 2f;
            float r = Random.Range(minR, maxR);
            Vector3 p = new Vector3(Mathf.Cos(ang) * r, 0, Mathf.Sin(ang) * r);
            if (Vector3.Distance(p, new Vector3(0, 0, -12f)) > 5f) return p; // keep player spawn clear
        }
        return new Vector3(Random.Range(-20f, 20f), 0, Random.Range(0f, 20f));
    }

    Texture2D GridTexture()
    {
        int S = 256; var t = new Texture2D(S, S);
        Color baseC = new Color(0.09f, 0.11f, 0.15f);
        Color line = new Color(0.18f, 0.26f, 0.36f);
        for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
        {
            bool grid = (x < 3) || (y < 3);
            t.SetPixel(x, y, grid ? line : baseC);
        }
        t.wrapMode = TextureWrapMode.Repeat; t.Apply();
        return t;
    }

    // ---------------- transitions ----------------
    public void GoMenu()
    {
        CleanupBattle(); ClearCrate();
        Time.timeScale = 1f; Paused = false;
        State = GameState.Menu;
        BuildArena(0);
        SetPreviewCam();
        int m0 = Mathf.Clamp(SaveSystem.GetSquadChassis(0), 0, catalog.Length - 1);
        SpawnPreview(m0, SaveSystem.GetChassisWeapon(m0),
            (AbilityType)SaveSystem.GetChassisAbility(m0, (int)catalog[m0].ability));
        UI.ShowMenu();
    }

    public void GoHangar()
    {
        CleanupBattle(); ClearCrate();
        State = GameState.Hangar;
        browseChassis = Mathf.Clamp(SaveSystem.GetSquadChassis(0), 0, catalog.Length - 1);
        LoadBrowseLoadout();
        SetPreviewCam();
        SpawnPreview(browseChassis, browseWeapon, browseAbility);
        UI.RefreshHangar();
        UI.ShowHangar();
    }

    // Load the chassis's saved loadout into the browse selectors.
    void LoadBrowseLoadout()
    {
        browseWeapon = SaveSystem.GetChassisWeapon(browseChassis);
        browseAbility = (AbilityType)SaveSystem.GetChassisAbility(browseChassis, (int)catalog[browseChassis].ability);
    }

    public void CycleMech(int dir)
    {
        browseChassis = (browseChassis + dir + catalog.Length) % catalog.Length;
        LoadBrowseLoadout();                              // show this mech's remembered loadout
        SpawnPreview(browseChassis, browseWeapon, browseAbility);
        UI.RefreshHangar();
    }

    public void CycleWeapon(int dir)
    {
        var all = WeaponDef.All;
        int idx = System.Array.IndexOf(all, browseWeapon);
        browseWeapon = all[(idx + dir + all.Length) % all.Length];
        if (SaveSystem.IsWeaponUnlocked(browseWeapon)) SaveSystem.SetChassisWeapon(browseChassis, browseWeapon); // owned = equip now
        SpawnPreview(browseChassis, browseWeapon, browseAbility);
        UI.RefreshHangar();
    }

    public void CycleAbility(int dir)
    {
        var all = AbilityDef.All;
        int idx = System.Array.IndexOf(all, browseAbility);
        browseAbility = all[(idx + dir + all.Length) % all.Length];
        if (BrowseAbilityOwned) SaveSystem.SetChassisAbility(browseChassis, browseAbility);   // owned = equip now
        SpawnPreview(browseChassis, browseWeapon, browseAbility);
        UI.RefreshHangar();
    }

    public void ConfirmSelection()
    {
        if (!SaveSystem.IsWeaponUnlocked(browseWeapon))
        {
            // try to buy
            int price = BrowseWeapon.price;
            if (SaveSystem.Credits >= price) { SaveSystem.AddCredits(-price); SaveSystem.UnlockWeapon(browseWeapon); }
            else { UI.RefreshHangar(); return; }
        }
        SaveSystem.SelectedChassis = browseChassis;
        SaveSystem.SelectedWeapon = browseWeapon;
        UI.RefreshHangar();
    }

    // ---------------- crate ----------------
    public void GoCrate()
    {
        CleanupBattle();
        State = GameState.Crate;
        LastReward = "";
        cam.transform.position = new Vector3(0, 2.4f, -5.2f);
        cam.transform.LookAt(new Vector3(0, 1.1f, 0));
        ClearPreview();
        ClearCrate();
        crate = Crate.Build(Vector3.zero, new Color(1f, 0.8f, 0.3f));
        UI.ShowCrate(SaveSystem.CrateAvailable());
    }

    public void OpenCrate()
    {
        if (crate == null || !SaveSystem.CrateAvailable()) return;
        SfxManager.I?.Crate();
        UI.SetCrateOpening();
        crate.Open(() =>
        {
            int credits = Random.Range(150, 351);
            SaveSystem.AddCredits(credits);
            string extra = "";
            var locked = new List<Wep>();
            foreach (var w in WeaponDef.All) if (!SaveSystem.IsWeaponUnlocked(w)) locked.Add(w);
            if (locked.Count > 0 && Random.value < 0.45f)
            {
                var w = locked[Random.Range(0, locked.Count)];
                SaveSystem.UnlockWeapon(w);
                extra = "\nUNLOCKED: " + WeaponDef.Get(w).name;
            }
            SaveSystem.MarkCrateClaimed();
            LastReward = "+" + credits + " CREDITS" + extra;
            UI.ShowCrateReward(LastReward);
        });
    }

    public void DoBackup()
    {
        bool ok = SaveSystem.BackupToFile(out string path);
        UI.SetDataStatus(ok ? "Backup saved to:\n" + path : "Backup failed.");
    }

    public void DoRestore()
    {
        bool ok = SaveSystem.RestoreFromFile(out string path);
        if (ok)
        {
            UI.SetDataStatus("Restored from:\n" + path);
            UI.RefreshMenuStats();
            int m0 = Mathf.Clamp(SaveSystem.GetSquadChassis(0), 0, catalog.Length - 1);
            SpawnPreview(m0, SaveSystem.GetChassisWeapon(m0), (AbilityType)SaveSystem.GetChassisAbility(m0, (int)catalog[m0].ability));
        }
        else UI.SetDataStatus("No backup found in Download or app storage.");
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void SetPreviewCam()
    {
        cam.transform.position = new Vector3(4.2f, 2.7f, -5.8f);
        cam.transform.LookAt(new Vector3(0, 1.6f, 0));
    }

    void ClearPreview() { if (preview != null) Destroy(preview.gameObject); preview = null; }
    void ClearCrate() { if (crate != null) Destroy(crate.gameObject); crate = null; }

    void SpawnPreview(int chassis, Wep weapon, AbilityType ability)
    {
        ClearPreview();
        preview = MechBuilder.Build(catalog[chassis], WeaponDef.Get(weapon), 0);
        preview.SetAbility(ability);
        preview.transform.position = Vector3.zero;
        preview.transform.rotation = Quaternion.Euler(0, 155f, 0);
    }

    // ---------------- battle ----------------
    public void StartBattle()
    {
        CleanupBattle(); ClearPreview(); ClearCrate();
        State = GameState.Battle;
        Mechs.Clear();
        BuildArena(Random.Range(0, MapNames.Length));

        for (int i = 0; i < 3; i++)
        {
            squadChassis[i] = Mathf.Clamp(SaveSystem.GetSquadChassis(i), 0, catalog.Length - 1);
            squadWeapon[i] = SaveSystem.GetChassisWeapon(squadChassis[i]);
            squadAbility[i] = (AbilityType)SaveSystem.GetChassisAbility(squadChassis[i], (int)catalog[squadChassis[i]].ability);
            squadHpFrac[i] = 1f; squadDead[i] = false;
        }
        activeSlot = 0; switchCd = 0f; lookYaw = 0f; lookPitch = 0f;
        battleKills = 0; battleEarnings = 0;
        SpawnPlayerMech(0, new Vector3(0, 0, -12f), Quaternion.identity);

        float power = PlayerPower();
        int botCount = power < 0.3f ? 2 : (power < 0.7f ? 3 : 4);   // fewer, weaker foes early
        float hpMult = Mathf.Lerp(0.55f, 1.25f, power);            // low HP early, tanky late
        for (int i = 0; i < botCount; i++)
        {
            var data = catalog[chassisByStrength[PickScaled(chassisByStrength.Count, power)]];   // worse mechs early
            var wep = WeaponDef.Get(botWeapons[PickScaled(botWeapons.Count, power)]);            // weaker weapons early
            var bot = MechBuilder.Build(data, wep, 1, new Color(0.95f, 0.35f, 0.3f));
            bot.DamageMultiplier = 0.4f;   // enemies hit softer
            bot.SetAbility(FairBotAbilities[Random.Range(0, FairBotAbilities.Length)]);  // only fair abilities
            float a = (i / (float)botCount) * Mathf.PI * 2f;
            bot.transform.position = new Vector3(Mathf.Cos(a) * 16f, 0, Mathf.Sin(a) * 16f + 6f);
            bot.transform.rotation = Quaternion.Euler(0, 180f, 0);
            bot.EnableHealthBar();
            bot.SetHealthScale(hpMult);
            bot.OnDeath += OnMechDeath;
            bot.gameObject.AddComponent<BotController>().Bind(bot, i);
            Mechs.Add(bot);
        }

        string[] names = new string[3]; Color[] cols = new Color[3];
        for (int i = 0; i < 3; i++) { names[i] = catalog[squadChassis[i]].name; cols[i] = catalog[squadChassis[i]].accentColor; }
        UI.BuildHotbar(names, cols);

        UI.SetPlayerHp(1f);
        UI.SetEnemyCount(botCount);
        UI.ShowBattle();
    }

    void SpawnPlayerMech(int slot, Vector3 pos, Quaternion rot)
    {
        var mech = MechBuilder.Build(catalog[squadChassis[slot]], WeaponDef.Get(squadWeapon[slot]), 0);
        mech.transform.position = pos; mech.transform.rotation = rot;
        mech.SetHealthFraction(squadHpFrac[slot]);
        mech.SetAbility(squadAbility[slot]);
        mech.OnDeath += OnMechDeath;
        Player = mech;
        PlayerCtrl = mech.gameObject.AddComponent<PlayerController>();
        PlayerCtrl.Bind(mech);
        Mechs.Add(mech);
    }

    public void SwitchMech(int slot)
    {
        if (State != GameState.Battle || Player == null) return;
        if (slot < 0 || slot >= 3 || slot == activeSlot || squadDead[slot] || switchCd > 0f) return;
        switchCd = 2.5f;
        squadHpFrac[activeSlot] = Player.Health / Player.Data.maxHealth;
        Vector3 pos = Player.transform.position; Quaternion rot = Player.transform.rotation;
        Player.OnDeath -= OnMechDeath;
        Mechs.Remove(Player);
        Destroy(Player.gameObject);
        Fx.Blob(pos + Vector3.up * 1.4f, catalog[squadChassis[slot]].accentColor, 0.5f, 2.6f, 0.3f);
        SfxManager.I?.Switch();
        activeSlot = slot;
        SpawnPlayerMech(slot, pos, rot);
        ShakeCam(0.22f);
        UI.SetBattleLoadout(Player);
    }

    // Assign the currently-browsed chassis+weapon to a squad slot (buying chassis/weapon if needed).
    public void AssignSquadSlot(int slot)
    {
        if (!SaveSystem.IsChassisUnlocked(browseChassis))
        {
            if (PlayerRank < catalog[browseChassis].reqRank) { UI.RefreshHangar(); return; }   // rank-gated
            int cp = catalog[browseChassis].price;
            if (SaveSystem.Credits >= cp) { SaveSystem.AddCredits(-cp); SaveSystem.UnlockChassis(browseChassis); SaveSystem.AddXP(120); }
            else { UI.RefreshHangar(); return; }
        }
        if (!SaveSystem.IsWeaponUnlocked(browseWeapon))
        {
            if (PlayerRank < BrowseWeapon.reqRank) { UI.RefreshHangar(); return; }              // rank-gated
            int wp = BrowseWeapon.price;
            if (SaveSystem.Credits >= wp) { SaveSystem.AddCredits(-wp); SaveSystem.UnlockWeapon(browseWeapon); SaveSystem.AddXP(120); }
            else { UI.RefreshHangar(); return; }
        }
        if (!BrowseAbilityIsSignature && !SaveSystem.IsAbilityUnlocked(browseAbility))
        {
            int ap = BrowseAbility.price;
            if (SaveSystem.Credits >= ap) { SaveSystem.AddCredits(-ap); SaveSystem.UnlockAbility(browseAbility); SaveSystem.AddXP(120); }
            else { UI.RefreshHangar(); return; }
        }
        SaveSystem.SetChassisWeapon(browseChassis, browseWeapon);
        SaveSystem.SetChassisAbility(browseChassis, browseAbility);
        SaveSystem.SetSquadChassis(slot, browseChassis);
        UI.RefreshHangar();
    }

    void OnMechDeath(Mech m)
    {
        Mechs.Remove(m);
        if (State != GameState.Battle) return;

        if (m == Player)
        {
            squadDead[activeSlot] = true; squadHpFrac[activeSlot] = 0f;
            Vector3 pos = m.transform.position;
            int next = -1;
            for (int i = 0; i < 3; i++) if (!squadDead[i]) { next = i; break; }
            if (next >= 0)
            {
                activeSlot = next;
                SpawnPlayerMech(next, pos, Quaternion.identity);
                Fx.Blob(pos + Vector3.up * 1.2f, Color.white, 0.6f, 3f, 0.3f);
                UI.SetBattleLoadout(Player);
                UI.FlashKill("MECH DOWN - DEPLOYING " + catalog[squadChassis[next]].name);
            }
            else EndBattle(false);
            return;
        }

        battleKills++;
        int r = 40;                        // bounty per kill
        SaveSystem.AddCredits(r); battleEarnings += r;
        UI.FlashKill(m.Data.name + " DESTROYED   +" + r);
        int enemies = CountAlive(1);
        UI.SetEnemyCount(enemies);
        if (enemies <= 0) EndBattle(true);
    }

    int CountAlive(int team)
    {
        int n = 0;
        foreach (var mm in Mechs) if (mm != null && mm.Alive && mm.Team == team) n++;
        return n;
    }

    void EndBattle(bool win)
    {
        State = GameState.Result;
        int endReward = win ? 150 : 20;    // big win bonus vs small salvage on defeat
        SaveSystem.AddCredits(endReward); battleEarnings += endReward;

        int xpGain = battleKills * 12 + (win ? 60 : 20);
        int lvlBefore = Rank.LevelOf(SaveSystem.XP);
        SaveSystem.AddXP(xpGain);
        int lvlAfter = Rank.LevelOf(SaveSystem.XP);

        string flavor = win ? "All hostiles destroyed" : "Your squad was wiped out";
        string sub = flavor + "\nKILLS  " + battleKills + "     +" + battleEarnings + "cr     +" + xpGain + " XP";
        if (win) SfxManager.I?.Win(); else SfxManager.I?.Lose();
        string rankUp = "";
        if (lvlAfter > lvlBefore) { rankUp = Rank.Tier(lvlAfter) + "   " + lvlAfter; StartCoroutine(Celebrate()); SfxManager.I?.RankUp(); }
        UI.ShowResult(win, sub, rankUp);
    }

    // Fireworks + shake when the player ranks up (bloom makes the bursts glow through the result overlay).
    IEnumerator Celebrate()
    {
        Vector3 c = cam.transform.position + cam.transform.forward * 15f;
        for (int wave = 0; wave < 6; wave++)
        {
            for (int k = 0; k < 5; k++)
            {
                Vector3 p = c + new Vector3(Random.Range(-9f, 9f), Random.Range(-3f, 7f), Random.Range(-5f, 5f));
                Color col = Color.HSVToRGB(Random.value, 0.7f, 1f);
                Fx.Blob(p, col, 0.3f, 3.8f, 0.55f);
            }
            ShakeCam(0.18f);
            yield return new WaitForSeconds(0.32f);
        }
    }

    void CleanupBattle()
    {
        foreach (var m in Mechs) if (m != null) Destroy(m.gameObject);
        Mechs.Clear();
        foreach (var p in FindObjectsByType<Projectile>(FindObjectsSortMode.None)) Destroy(p.gameObject);
        Player = null; PlayerCtrl = null;
    }

    public Mech NearestEnemy(Mech from)
    {
        Mech best = null; float bd = float.MaxValue;
        foreach (var m in Mechs)
        {
            if (m == null || !m.Alive || m.Team == from.Team || m.Cloaked) continue;
            float d = (m.transform.position - from.transform.position).sqrMagnitude;
            if (d < bd) { bd = d; best = m; }
        }
        return best;
    }

    // ---- difficulty scaling (enemies scale to the player's rank + hangar) ----
    void BuildDifficultyTables()
    {
        chassisByStrength.Clear();
        for (int i = 0; i < catalog.Length; i++) chassisByStrength.Add(i);
        chassisByStrength.Sort((a, b) => ChassisStrength(catalog[a]).CompareTo(ChassisStrength(catalog[b])));

        botWeapons.Clear();
        foreach (var w in WeaponDef.All) if (WeaponDef.Get(w).reqRank == 0) botWeapons.Add(w);   // bots never get legendaries
        botWeapons.Sort((a, b) => WeaponStrength(WeaponDef.Get(a)).CompareTo(WeaponStrength(WeaponDef.Get(b))));
    }
    static float ChassisStrength(MechData d) => d.maxHealth * 0.5f + d.moveSpeed * 8f + d.reqRank * 25f + d.price * 0.04f;
    static float WeaponStrength(WeaponDef w) => w.damage * w.fireRate + w.splashRadius * 6f;

    // 0 (fresh account) .. 1 (maxed rank + everything unlocked)
    float PlayerPower()
    {
        float rankPower = Mathf.Clamp01(Rank.LevelOf(SaveSystem.XP) / 18f);
        int owned = 0, total = 0;
        for (int c = 0; c < catalog.Length; c++) { total++; if (SaveSystem.IsChassisUnlocked(c)) owned++; }
        foreach (var w in WeaponDef.All) { total++; if (SaveSystem.IsWeaponUnlocked(w)) owned++; }
        float gearPower = total > 0 ? owned / (float)total : 0f;
        return Mathf.Clamp01(0.55f * rankPower + 0.45f * gearPower);
    }

    // pick an index into a weakest->strongest list, within a window around the player's power
    int PickScaled(int count, float power)
    {
        int hi = Mathf.Clamp(Mathf.RoundToInt(power * (count - 1)) + 1, 0, count - 1);
        int lo = Mathf.Clamp(hi - 3, 0, hi);
        return Random.Range(lo, hi + 1);
    }

    // ---- line of sight ----
    public const int ObstacleLayer = 6;
    public static int ObstacleMask => 1 << ObstacleLayer;
    public bool HasLOS(Vector3 a, Vector3 b) => !Physics.Linecast(a, b, ObstacleMask);

    // Nearest enemy with a clear shot (no obstacle between the two cockpits).
    public Mech NearestVisibleEnemy(Mech from)
    {
        Mech best = null; float bd = float.MaxValue;
        Vector3 fc = from.transform.position + Vector3.up * 1.4f;
        foreach (var m in Mechs)
        {
            if (m == null || !m.Alive || m.Team == from.Team || m.Cloaked) continue;
            if (!HasLOS(fc, m.transform.position + Vector3.up * 1.4f)) continue;
            float d = (m.transform.position - from.transform.position).sqrMagnitude;
            if (d < bd) { bd = d; best = m; }
        }
        return best;
    }

    void Update()
    {
        if (switchCd > 0f) switchCd -= Time.deltaTime;
        if ((State == GameState.Menu || State == GameState.Hangar) && preview != null)
            preview.transform.Rotate(0, 18f * Time.deltaTime, 0, Space.World);
        if (State == GameState.Crate && crate != null)
            crate.transform.Rotate(0, 12f * Time.deltaTime, 0, Space.World);
        if (State == GameState.Battle && Player != null && Player.Alive)
        {
            UI.SetPlayerHp(Player.Health / Player.Data.maxHealth);
            UI.SetAbilityCharge(Player.AbilityCharge, Player.AbilityReady);
            UI.SetShield(Player.Shielded ? Player.ShieldRemaining : 0f);
            squadHpFrac[activeSlot] = Player.Health / Player.Data.maxHealth;
            UI.UpdateHotbar(activeSlot, squadHpFrac, squadDead);
        }
    }

    void LateUpdate()
    {
        if ((State == GameState.Battle || State == GameState.Result) && Player != null)
        {
            Vector3 focus = Player.transform.position + Vector3.up * 1.5f;
            float pitch = Mathf.Clamp(38f + lookPitch, 12f, 78f);   // drag adjusts yaw/pitch around the player
            Quaternion q = Quaternion.Euler(pitch, lookYaw, 0f);
            Vector3 desired = focus + q * (Vector3.back * 17f);
            cam.transform.position = Vector3.Lerp(cam.transform.position, desired, Time.deltaTime * 5f);
            cam.transform.LookAt(focus);
        }

        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            float k = shakeAmp * Mathf.Clamp01(shakeTimer / 0.35f);
            cam.transform.position += Random.insideUnitSphere * k;
            if (shakeTimer <= 0f) shakeAmp = 0f;
        }
    }
}
