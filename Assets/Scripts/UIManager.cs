using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Builds and manages all screens (menu / hangar / crate / battle / result) with legacy uGUI.
public class UIManager : MonoBehaviour
{
    static readonly Color Bg      = new Color(0.04f, 0.05f, 0.08f, 1f);
    static readonly Color Panel   = new Color(0.10f, 0.12f, 0.17f, 0.94f);
    static readonly Color PanelHi = new Color(0.14f, 0.17f, 0.23f, 0.96f);
    static readonly Color Accent  = new Color(0.24f, 0.78f, 1f, 1f);
    static readonly Color Accent2 = new Color(1f, 0.58f, 0.22f, 1f);
    static readonly Color Gold    = new Color(1f, 0.82f, 0.32f, 1f);
    static readonly Color TextCol = new Color(0.93f, 0.96f, 0.99f, 1f);
    static readonly Color Dim     = new Color(0.6f, 0.66f, 0.72f, 1f);
    static readonly Color Danger  = new Color(0.95f, 0.32f, 0.32f, 1f);

    Font font;
    Transform canvas;
    GameObject menuPanel, hangarPanel, cratePanel, battlePanel, resultPanel;

    Joystick joystick;
    Text[] creditTexts = new Text[3];
    GameObject crateBadge;
    Image rankCircle;
    Text rankLevel, rankTier, rankXpText;
    RectTransform rankXpFill;

    // hangar refs
    Text hName, hRole, wName, wDesc, hStatus, hAbility, aName, aDesc, selectLabel;
    Image selectImg;
    GameObject pausePanel, dataPanel;
    Text dataStatus;
    RectTransform[] statFills = new RectTransform[4];
    Text[] squadLabels = new Text[3];
    Image[] squadBg = new Image[3];

    // battle hotbar
    GameObject hotbarRoot;
    Image[] hotBg = new Image[3];
    RectTransform[] hotFill = new RectTransform[3];
    Text[] hotName = new Text[3];

    // battle refs
    RectTransform playerHpFill;
    Text enemyCountText, weaponHud, mapLabel, abilityLabel, killText, shieldText;
    Image abilityBg, abilityFill;
    GameObject shieldInd;

    // crate refs
    GameObject crateOpenBtn, crateRewardPanel, crateClaimedMsg;
    Text crateRewardText;

    Text resultTitle, resultSub, rankBanner;

    public bool FireHeld { get; private set; }
    public Vector2 MoveAxis => joystick != null ? joystick.Value : Vector2.zero;

    // ---------- sprites ----------
    static Sprite _circle, _rounded, _grad;
    public static Sprite CircleSprite()
    {
        if (_circle != null) return _circle;
        int S = 128; var t = new Texture2D(S, S, TextureFormat.RGBA32, false);
        var c = new Vector2(S * 0.5f, S * 0.5f);
        for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c);
            t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01((S * 0.5f - d) / 2f)));
        }
        t.Apply(); t.wrapMode = TextureWrapMode.Clamp;
        _circle = Sprite.Create(t, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f);
        return _circle;
    }
    static Sprite Rounded()
    {
        if (_rounded != null) return _rounded;
        int S = 48, rad = 14; var t = new Texture2D(S, S, TextureFormat.RGBA32, false);
        for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
        {
            float a = 1f; var p = new Vector2(x, y);
            Vector2[] cc = { new Vector2(rad, rad), new Vector2(S - rad, rad), new Vector2(rad, S - rad), new Vector2(S - rad, S - rad) };
            bool[] inc = { x < rad && y < rad, x > S - rad && y < rad, x < rad && y > S - rad, x > S - rad && y > S - rad };
            for (int i = 0; i < 4; i++) if (inc[i]) a = Mathf.Clamp01((rad - Vector2.Distance(p, cc[i])) + 0.5f);
            t.SetPixel(x, y, new Color(1, 1, 1, a));
        }
        t.Apply(); t.wrapMode = TextureWrapMode.Clamp;
        _rounded = Sprite.Create(t, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(rad, rad, rad, rad));
        return _rounded;
    }
    // Horizontal fade: opaque dark on the left, transparent on the right so the 3D mech shows through.
    static Sprite HGradient()
    {
        if (_grad != null) return _grad;
        int W = 256; var t = new Texture2D(W, 1, TextureFormat.RGBA32, false);
        Color dark = new Color(0.05f, 0.07f, 0.12f);
        for (int x = 0; x < W; x++)
        {
            float u = x / (float)W;
            float a = 1f - Mathf.Clamp01((u - 0.32f) / 0.24f); // opaque to ~0.32, faded out by ~0.56
            t.SetPixel(x, 0, new Color(dark.r, dark.g, dark.b, a));
        }
        t.Apply(); t.wrapMode = TextureWrapMode.Clamp;
        _grad = Sprite.Create(t, new Rect(0, 0, W, 1), new Vector2(0.5f, 0.5f), 100f);
        return _grad;
    }

    // ---------- rect helpers ----------
    RectTransform Rect(string n, Transform p, Vector2 anchor, Vector2 size, Vector2 pos)
    {
        var go = new GameObject(n, typeof(RectTransform));
        go.transform.SetParent(p, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size; rt.anchoredPosition = pos;
        return rt;
    }
    RectTransform Full(string n, Transform p)
    {
        var go = new GameObject(n, typeof(RectTransform));
        go.transform.SetParent(p, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }
    Image Img(RectTransform rt, Color c, bool rounded = false)
    {
        var img = rt.gameObject.AddComponent<Image>();
        img.color = c;
        if (rounded) { img.sprite = Rounded(); img.type = Image.Type.Sliced; }
        return img;
    }
    Text Label(Transform p, string s, int size, Color c, TextAnchor a, Vector2 anchor, Vector2 sz, Vector2 pos, FontStyle st = FontStyle.Normal)
    {
        var rt = Rect("T", p, anchor, sz, pos);
        var t = rt.gameObject.AddComponent<Text>();
        t.font = font; t.text = s; t.fontSize = size; t.color = c; t.alignment = a; t.fontStyle = st;
        t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }
    Button Btn(Transform p, string label, Vector2 size, Vector2 anchor, Vector2 pos, Color col, System.Action onClick, int fs = 30)
    {
        var rt = Rect("Btn_" + label, p, anchor, size, pos);
        var img = Img(rt, col, true);
        var b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        var cb = b.colors; cb.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
        cb.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f); cb.fadeDuration = 0.07f; b.colors = cb;
        var t = Label(rt, label, fs, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), size, Vector2.zero, FontStyle.Bold);
        t.rectTransform.anchorMin = Vector2.zero; t.rectTransform.anchorMax = Vector2.one; t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero;
        b.onClick.AddListener(() => { SfxManager.I?.Click(); onClick?.Invoke(); });
        return b;
    }
    RectTransform CreditPill(Transform p, int idx)
    {
        var pill = Rect("CreditPill", p, new Vector2(1, 1), new Vector2(210, 56), new Vector2(-170, -54));
        Img(pill, Panel, true);
        var coin = Rect("Coin", pill, new Vector2(0, 0.5f), new Vector2(28, 28), new Vector2(32, 0));
        var ci = coin.gameObject.AddComponent<Image>(); ci.sprite = CircleSprite(); ci.color = Gold;
        creditTexts[idx] = Label(pill, "0", 28, Gold, TextAnchor.MiddleRight, new Vector2(1, 0.5f), new Vector2(150, 40), new Vector2(-20, 0), FontStyle.Bold);
        return pill;
    }

    // ---------- build ----------
    public void Build()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        var go = new GameObject("UICanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var cv = go.GetComponent<Canvas>(); cv.renderMode = RenderMode.ScreenSpaceOverlay;
        var sc = go.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1600, 900); sc.matchWidthOrHeight = 0.5f;
        DontDestroyOnLoad(go); canvas = go.transform;

        if (FindObjectOfType<EventSystem>() == null)
            DontDestroyOnLoad(new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule)));

        BuildMenu(); BuildHangar(); BuildCrate(); BuildBattle(); BuildResult(); BuildPause(); BuildData();
        ShowMenu();
    }

    // ---------- MENU ----------
    void BuildMenu()
    {
        menuPanel = Full("MenuPanel", canvas).gameObject;
        var bg = Img(Full("Bg", menuPanel.transform), Color.white);
        bg.sprite = HGradient(); bg.type = Image.Type.Simple; bg.raycastTarget = false;

        // title block (upper-left)
        Label(menuPanel.transform, "MECH", 110, TextCol, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(700, 130), new Vector2(410, -70), FontStyle.Bold);
        Label(menuPanel.transform, "ARENA", 110, Accent, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(700, 130), new Vector2(420, -180), FontStyle.Bold);
        var underline = Rect("UL", menuPanel.transform, new Vector2(0, 1), new Vector2(360, 6), new Vector2(430, -290)); Img(underline, Accent2);
        Label(menuPanel.transform, "TACTICAL MECH COMBAT", 22, Dim, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(700, 30), new Vector2(432, -310));

        // buttons (lower-left column)
        Btn(menuPanel.transform, "PLAY", new Vector2(360, 84), new Vector2(0, 0), new Vector2(410, 360), Accent2, () => GameManager.Instance.StartBattle(), 40);
        Btn(menuPanel.transform, "HANGAR", new Vector2(360, 72), new Vector2(0, 0), new Vector2(410, 268), Accent, () => GameManager.Instance.GoHangar());
        var crateBtn = Btn(menuPanel.transform, "DAILY CRATE", new Vector2(360, 72), new Vector2(0, 0), new Vector2(410, 184), PanelHi, () => GameManager.Instance.GoCrate());
        crateBadge = Rect("Badge", crateBtn.transform, new Vector2(1, 1), new Vector2(26, 26), new Vector2(-6, -6)).gameObject;
        var bimg = crateBadge.AddComponent<Image>(); bimg.sprite = CircleSprite(); bimg.color = Gold;
        Btn(menuPanel.transform, "QUIT", new Vector2(360, 58), new Vector2(0, 0), new Vector2(410, 108), Panel, () => GameManager.Instance.QuitGame(), 24);
        Btn(menuPanel.transform, "BACKUP / RESTORE", new Vector2(360, 40), new Vector2(0, 0), new Vector2(410, 54), PanelHi, () => ShowData(true), 20);

        CreditPill(menuPanel.transform, 0);

        // rank badge (bottom-right): circle | (RANK + XP on top row) / tier / xp bar
        var rp = Rect("RankBadge", menuPanel.transform, new Vector2(1, 0), new Vector2(476, 140), new Vector2(-258, 152));
        Img(rp, new Color(0.10f, 0.12f, 0.17f, 0.94f), true);
        var circ = Rect("RCircle", rp, new Vector2(0, 0.5f), new Vector2(96, 96), new Vector2(70, 0));
        rankCircle = circ.gameObject.AddComponent<Image>(); rankCircle.sprite = CircleSprite(); rankCircle.color = Accent;
        rankLevel = Label(circ, "1", 44, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(96, 64), Vector2.zero, FontStyle.Bold);
        // text stacked + centered in the area right of the circle (x-offset +59 = centre of x118..476)
        Label(rp, "RANK", 15, Dim, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(340, 18), new Vector2(59, -13));
        rankTier = Label(rp, "RECRUIT", 28, TextCol, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(340, 34), new Vector2(59, -44), FontStyle.Bold);
        rankXpText = Label(rp, "0 / 120 XP", 15, Dim, TextAnchor.MiddleCenter, new Vector2(0.5f, 0), new Vector2(340, 18), new Vector2(59, 42));
        var xbg = Rect("xbg", rp, new Vector2(0, 0), new Vector2(300, 14), new Vector2(288, 22)); Img(xbg, new Color(0.04f, 0.05f, 0.07f, 1f), true);
        rankXpFill = Rect("xf", xbg, new Vector2(0, 0.5f), new Vector2(300, 14), Vector2.zero); rankXpFill.pivot = new Vector2(0, 0.5f); rankXpFill.anchoredPosition = new Vector2(-150, 0); Img(rankXpFill, Accent, true);
    }

    void UpdateRank()
    {
        int xp = SaveSystem.XP;
        int lvl = Rank.LevelOf(xp);
        int into = Rank.XpInto(xp);
        int need = Rank.XpForLevel(lvl);
        Color tc = Rank.TierColor(lvl);
        if (rankLevel) rankLevel.text = lvl.ToString();
        if (rankCircle) rankCircle.color = tc;
        if (rankTier) { rankTier.text = Rank.Tier(lvl); rankTier.color = tc; }
        if (rankXpText) rankXpText.text = into + " / " + need + " XP";
        if (rankXpFill) rankXpFill.sizeDelta = new Vector2(300f * Mathf.Clamp01(need > 0 ? into / (float)need : 0f), 14);
    }

    // ---------- HANGAR ----------
    void BuildHangar()
    {
        hangarPanel = Full("HangarPanel", canvas).gameObject;
        // No full-screen bg here — the 3D preview mech renders behind the UI.
        var top = Rect("Top", hangarPanel.transform, new Vector2(0.5f, 1), new Vector2(4000, 88), new Vector2(0, -44)); Img(top, new Color(0.07f, 0.08f, 0.11f, 0.97f));
        Btn(hangarPanel.transform, "< BACK", new Vector2(150, 56), new Vector2(0, 1), new Vector2(100, -44), Panel, () => GameManager.Instance.GoMenu(), 24);
        Label(hangarPanel.transform, "HANGAR", 42, TextCol, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(400, 60), new Vector2(0, -44), FontStyle.Bold);
        CreditPill(hangarPanel.transform, 1);

        // ---- CHASSIS selector (prominent, below top bar) ----
        Label(hangarPanel.transform, "CHASSIS", 20, Dim, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(200, 26), new Vector2(0, -120));
        Btn(hangarPanel.transform, "<", new Vector2(78, 78), new Vector2(0.5f, 1), new Vector2(-300, -172), Accent, () => GameManager.Instance.CycleMech(-1), 42);
        hName = Label(hangarPanel.transform, "NAME", 48, TextCol, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(460, 60), new Vector2(0, -172), FontStyle.Bold);
        Btn(hangarPanel.transform, ">", new Vector2(78, 78), new Vector2(0.5f, 1), new Vector2(300, -172), Accent, () => GameManager.Instance.CycleMech(1), 42);
        hRole = Label(hangarPanel.transform, "Role", 24, Accent, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(500, 34), new Vector2(0, -222));

        // ---- bottom card: weapon + ability selectors, stats, squad, battle ----
        var card = Rect("Card", hangarPanel.transform, new Vector2(0.5f, 0), new Vector2(1020, 362), new Vector2(0, 188)); Img(card, Panel, true);
        Color aCol = new Color(0.5f, 0.8f, 1f, 1f);

        // WEAPON selector (left column)
        Label(card, "WEAPON", 18, Dim, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(200, 24), new Vector2(70, -18));
        Btn(card, "<", new Vector2(50, 50), new Vector2(0, 1), new Vector2(88, -64), Accent, () => GameManager.Instance.CycleWeapon(-1), 28);
        wName = Label(card, "BLASTER", 27, Gold, TextAnchor.MiddleCenter, new Vector2(0, 1), new Vector2(220, 36), new Vector2(258, -64), FontStyle.Bold);
        Btn(card, ">", new Vector2(50, 50), new Vector2(0, 1), new Vector2(430, -64), Accent, () => GameManager.Instance.CycleWeapon(1), 28);
        wDesc = Label(card, "desc", 17, Dim, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(450, 24), new Vector2(70, -100));

        // ABILITY selector (left column)
        Label(card, "ABILITY", 18, Dim, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(200, 24), new Vector2(70, -138));
        Btn(card, "<", new Vector2(50, 50), new Vector2(0, 1), new Vector2(88, -184), aCol, () => GameManager.Instance.CycleAbility(-1), 28);
        aName = Label(card, "AEGIS", 27, aCol, TextAnchor.MiddleCenter, new Vector2(0, 1), new Vector2(220, 36), new Vector2(258, -184), FontStyle.Bold);
        Btn(card, ">", new Vector2(50, 50), new Vector2(0, 1), new Vector2(430, -184), aCol, () => GameManager.Instance.CycleAbility(1), 28);
        aDesc = Label(card, "desc", 17, Dim, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(450, 24), new Vector2(70, -220));

        hStatus = Label(card, "", 20, Accent2, TextAnchor.UpperRight, new Vector2(1, 1), new Vector2(280, 30), new Vector2(-36, -22), FontStyle.Bold);

        // stat bars (right column)
        string[] names = { "SPEED", "ARMOR", "DAMAGE", "R.O.F" };
        for (int i = 0; i < 4; i++)
        {
            float y = -70 - i * 40;
            Label(card, names[i], 18, Dim, TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(110, 24), new Vector2(585, y));
            var bg = Rect("bg" + i, card, new Vector2(0, 1), new Vector2(210, 15), new Vector2(818, y)); Img(bg, new Color(0.04f, 0.05f, 0.07f, 1f), true);
            var fill = Rect("f" + i, bg, new Vector2(0, 0.5f), new Vector2(105, 15), Vector2.zero); fill.pivot = new Vector2(0, 0.5f); fill.anchoredPosition = new Vector2(-105, 0);
            Img(fill, i == 1 ? Accent2 : Accent, true); statFills[i] = fill;
        }

        // squad slots + battle (bottom)
        Label(card, "ASSIGN TO SQUAD", 17, Dim, TextAnchor.MiddleLeft, new Vector2(0, 0), new Vector2(300, 22), new Vector2(188, 92));
        for (int i = 0; i < 3; i++)
        {
            int slot = i;
            var b = Btn(card, "1: NAME", new Vector2(196, 58), new Vector2(0, 0), new Vector2(120 + i * 206, 46), PanelHi, () => GameManager.Instance.AssignSquadSlot(slot), 22);
            squadBg[i] = b.GetComponent<Image>();
            squadLabels[i] = b.GetComponentInChildren<Text>();
        }
        Btn(card, "BATTLE", new Vector2(250, 62), new Vector2(1, 0), new Vector2(-150, 46), Accent2, () => GameManager.Instance.StartBattle());
    }

    public void RefreshHangar()
    {
        var gm = GameManager.Instance;
        var d = gm.BrowseChassis; var w = gm.BrowseWeapon; var ab = gm.BrowseAbility;
        hName.text = d.name; hRole.text = d.role;
        wName.text = w.name;
        if (gm.BrowseWeaponOwned) wDesc.text = w.desc;
        else if (!gm.BrowseWeaponRankOK) wDesc.text = w.desc + "   [REACH RANK " + gm.BrowseWeaponReqRank + "]";
        else wDesc.text = w.desc + "   [LOCKED  " + w.price + "cr]";
        aName.text = ab.name;
        aDesc.text = gm.BrowseAbilityOwned ? ab.desc : (ab.desc + "   [LOCKED  " + ab.price + "cr]");

        float[] vals = {
            Mathf.InverseLerp(2.5f, 7f, d.moveSpeed),
            Mathf.InverseLerp(80f, 220f, d.maxHealth),
            Mathf.InverseLerp(6f, 34f, w.damage),
            Mathf.InverseLerp(0.8f, 4.6f, w.fireRate)
        };
        for (int i = 0; i < 4; i++) if (statFills[i]) statFills[i].sizeDelta = new Vector2(210f * Mathf.Clamp01(vals[i]), 15);

        bool inSquad = gm.SquadSlotIsBrowse(0) || gm.SquadSlotIsBrowse(1) || gm.SquadSlotIsBrowse(2);
        if (!gm.BrowseChassisOwned)
        {
            if (!gm.BrowseChassisRankOK) { hStatus.text = "REACH RANK " + gm.BrowseChassisReqRank; hStatus.color = Danger; }
            else { hStatus.text = "LOCKED  " + gm.BrowseChassisPrice + "cr"; hStatus.color = Gold; }
        }
        else { hStatus.text = inSquad ? "IN SQUAD" : "OWNED"; hStatus.color = inSquad ? Accent2 : Dim; }
        for (int i = 0; i < 3; i++)
        {
            if (squadLabels[i]) squadLabels[i].text = (i + 1) + ":  " + gm.SquadSlotName(i);
            if (squadBg[i]) squadBg[i].color = gm.SquadSlotIsBrowse(i) ? new Color(Accent.r, Accent.g, Accent.b, 0.92f) : PanelHi;
        }
        UpdateCredits();
    }

    // ---------- battle hotbar (mid-battle mech swap) ----------
    public void BuildHotbar(string[] names, Color[] cols)
    {
        if (hotbarRoot != null) Destroy(hotbarRoot);
        hotbarRoot = Rect("Hotbar", battlePanel.transform, new Vector2(0.5f, 0), new Vector2(660, 116), new Vector2(0, 62)).gameObject;
        for (int i = 0; i < 3; i++)
        {
            int slot = i;
            var s = Rect("Hot" + i, hotbarRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(200, 100), new Vector2((i - 1) * 210, 0));
            var bg = Img(s, new Color(0.1f, 0.12f, 0.16f, 0.92f), true);
            var b = s.gameObject.AddComponent<Button>(); b.targetGraphic = bg;
            b.onClick.AddListener(() => GameManager.Instance.SwitchMech(slot));
            hotBg[i] = bg;
            var chip = Rect("chip", s, new Vector2(0, 0.5f), new Vector2(12, 62), new Vector2(20, 8)); var ci = Img(chip, cols[i], true); ci.raycastTarget = false;
            hotName[i] = Label(s, names[i], 21, TextCol, TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(150, 30), new Vector2(96, -18), FontStyle.Bold); hotName[i].raycastTarget = false;
            var hpb = Rect("hpb", s, new Vector2(0.5f, 0), new Vector2(172, 14), new Vector2(0, 22)); var hi = Img(hpb, new Color(0.04f, 0.05f, 0.07f, 1f), true); hi.raycastTarget = false;
            var f = Rect("hpf", hpb, new Vector2(0, 0.5f), new Vector2(172, 14), Vector2.zero); f.pivot = new Vector2(0, 0.5f); f.anchoredPosition = new Vector2(-86, 0);
            var fi = Img(f, new Color(0.3f, 0.9f, 0.45f, 1f), true); fi.raycastTarget = false; hotFill[i] = f;
        }
    }
    public void UpdateHotbar(int active, float[] fracs, bool[] dead)
    {
        for (int i = 0; i < 3; i++)
        {
            if (hotFill[i]) hotFill[i].sizeDelta = new Vector2(172f * Mathf.Clamp01(fracs[i]), 14);
            if (hotBg[i]) hotBg[i].color = dead[i] ? new Color(0.12f, 0.06f, 0.06f, 0.85f)
                : (i == active ? new Color(Accent.r, Accent.g, Accent.b, 0.9f) : new Color(0.1f, 0.12f, 0.16f, 0.92f));
            if (hotName[i]) hotName[i].color = dead[i] ? new Color(0.5f, 0.42f, 0.42f, 1f) : (i == active ? Color.white : TextCol);
        }
    }
    public void SetBattleLoadout(Mech m)
    {
        if (m == null) return;
        if (weaponHud) weaponHud.text = m.Weapon.name;
        if (abilityLabel) abilityLabel.text = m.Ability != null ? m.Ability.name : m.Data.abilityName;
        if (abilityFill) abilityFill.fillAmount = 0f;
    }

    // ---------- CRATE ----------
    void BuildCrate()
    {
        cratePanel = Full("CratePanel", canvas).gameObject;
        Img(Full("Bg", cratePanel.transform), new Color(0.03f, 0.04f, 0.07f, 1f));
        Btn(cratePanel.transform, "< BACK", new Vector2(150, 56), new Vector2(0, 1), new Vector2(100, -44), Panel, () => GameManager.Instance.GoMenu(), 24);
        Label(cratePanel.transform, "DAILY CRATE", 46, Gold, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(600, 60), new Vector2(0, -50), FontStyle.Bold);
        CreditPill(cratePanel.transform, 2);

        crateOpenBtn = Btn(cratePanel.transform, "OPEN", new Vector2(340, 84), new Vector2(0.5f, 0), new Vector2(0, 80), Gold, () => GameManager.Instance.OpenCrate(), 40).gameObject;
        crateClaimedMsg = Label(cratePanel.transform, "Already opened today — come back tomorrow!", 26, Dim, TextAnchor.MiddleCenter, new Vector2(0.5f, 0), new Vector2(900, 40), new Vector2(0, 110)).gameObject;

        crateRewardPanel = Rect("Reward", cratePanel.transform, new Vector2(0.5f, 0.5f), new Vector2(620, 300), new Vector2(0, 0)).gameObject;
        Img((RectTransform)crateRewardPanel.transform, PanelHi, true);
        Label(crateRewardPanel.transform, "REWARD", 30, Gold, TextAnchor.UpperCenter, new Vector2(0.5f, 1), new Vector2(600, 40), new Vector2(0, -26), FontStyle.Bold);
        crateRewardText = Label(crateRewardPanel.transform, "", 34, TextCol, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(560, 120), new Vector2(0, 6), FontStyle.Bold);
        Btn(crateRewardPanel.transform, "COLLECT", new Vector2(260, 66), new Vector2(0.5f, 0), new Vector2(0, 26), Accent, () => GameManager.Instance.GoMenu());
        crateRewardPanel.SetActive(false);
    }

    public void ShowCrate(bool available)
    {
        crateOpenBtn.SetActive(available);
        crateClaimedMsg.SetActive(!available);
        crateRewardPanel.SetActive(false);
        UpdateCredits();
        SetScreen(false, false, true, false, false);
    }
    public void SetCrateOpening() { crateOpenBtn.SetActive(false); crateClaimedMsg.SetActive(false); }
    public void ShowCrateReward(string text)
    {
        crateRewardText.text = text;
        crateRewardPanel.SetActive(true);
        UpdateCredits();
    }

    // ---------- BATTLE ----------
    void BuildBattle()
    {
        battlePanel = Full("BattlePanel", canvas).gameObject;

        // drag-to-look surface: right ~58% of the screen, BEHIND the buttons (added first = lowest, so buttons win the touch)
        var look = Rect("LookPad", battlePanel.transform, new Vector2(0.5f, 0.5f), new Vector2(10, 10), Vector2.zero);
        look.anchorMin = new Vector2(0.42f, 0f); look.anchorMax = new Vector2(1f, 1f); look.offsetMin = Vector2.zero; look.offsetMax = Vector2.zero;
        var li = look.gameObject.AddComponent<Image>(); li.color = new Color(1, 1, 1, 0);   // invisible but raycast-able
        look.gameObject.AddComponent<LookPad>();

        joystick = Joystick.Create(battlePanel.transform, new Vector2(210, 190), 260, new Color(1, 1, 1, 0.12f), new Color(Accent.r, Accent.g, Accent.b, 0.55f));

        var fire = Rect("Fire", battlePanel.transform, new Vector2(1, 0), new Vector2(210, 210), new Vector2(-190, 180));
        var fimg = fire.gameObject.AddComponent<Image>(); fimg.sprite = CircleSprite(); fimg.color = new Color(Danger.r, Danger.g, Danger.b, 0.85f);
        fire.gameObject.AddComponent<HoldButton>().OnHold = (h) => FireHeld = h;
        Label(fire, "FIRE", 34, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(210, 60), Vector2.zero, FontStyle.Bold);

        // ability button (left of fire) with radial cooldown overlay
        var ab = Rect("Ability", battlePanel.transform, new Vector2(1, 0), new Vector2(160, 160), new Vector2(-420, 210));
        abilityBg = ab.gameObject.AddComponent<Image>(); abilityBg.sprite = CircleSprite(); abilityBg.color = new Color(Accent.r, Accent.g, Accent.b, 0.9f);
        var abBtn = ab.gameObject.AddComponent<Button>(); abBtn.targetGraphic = abilityBg;
        abBtn.onClick.AddListener(() => GameManager.Instance.ActivatePlayerAbility());
        var cd = Rect("CD", ab, new Vector2(0.5f, 0.5f), new Vector2(160, 160), Vector2.zero);
        abilityFill = cd.gameObject.AddComponent<Image>(); abilityFill.sprite = CircleSprite(); abilityFill.color = new Color(0, 0, 0, 0.62f);
        abilityFill.type = Image.Type.Filled; abilityFill.fillMethod = Image.FillMethod.Radial360;
        abilityFill.fillOrigin = (int)Image.Origin360.Top; abilityFill.fillClockwise = false; abilityFill.fillAmount = 0f; abilityFill.raycastTarget = false;
        abilityLabel = Label(ab, "ABILITY", 20, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(150, 60), Vector2.zero, FontStyle.Bold);
        abilityLabel.raycastTarget = false;

        killText = Label(battlePanel.transform, "", 34, Gold, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(900, 44), new Vector2(0, -150), FontStyle.Bold);

        Label(battlePanel.transform, "HULL", 22, TextCol, TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(120, 30), new Vector2(90, -40));
        var hp = Rect("HpBg", battlePanel.transform, new Vector2(0, 1), new Vector2(340, 26), new Vector2(320, -40)); Img(hp, new Color(0.05f, 0.06f, 0.08f, 0.9f), true);
        playerHpFill = Rect("HpFill", hp, new Vector2(0, 0.5f), new Vector2(340, 26), Vector2.zero); playerHpFill.pivot = new Vector2(0, 0.5f); playerHpFill.anchoredPosition = new Vector2(-170, 0);
        Img(playerHpFill, new Color(0.3f, 0.9f, 0.4f, 1f), true);
        weaponHud = Label(battlePanel.transform, "", 20, Accent, TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(300, 26), new Vector2(160, -74), FontStyle.Bold);

        enemyCountText = Label(battlePanel.transform, "ENEMIES: 0", 30, Danger, TextAnchor.MiddleRight, new Vector2(1, 1), new Vector2(360, 40), new Vector2(-260, -54), FontStyle.Bold);
        mapLabel = Label(battlePanel.transform, "", 22, Accent, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(400, 26), new Vector2(0, -84), FontStyle.Bold);
        Btn(battlePanel.transform, "II", new Vector2(56, 56), new Vector2(0.5f, 1), new Vector2(0, -40), Panel, () => GameManager.Instance.SetPaused(true), 24);

        // shield timer indicator (red, top-center; shown only while Aegis is active)
        var sh = Rect("ShieldInd", battlePanel.transform, new Vector2(0.5f, 1), new Vector2(250, 46), new Vector2(0, -128));
        var shImg = Img(sh, new Color(0.62f, 0.12f, 0.12f, 0.94f), true); shImg.raycastTarget = false;
        shieldText = Label(sh, "SHIELD  15s", 24, new Color(1f, 0.78f, 0.78f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(250, 46), Vector2.zero, FontStyle.Bold);
        shieldText.raycastTarget = false;
        shieldInd = sh.gameObject; shieldInd.SetActive(false);
    }
    public void SetPlayerHp(float a) { if (playerHpFill) playerHpFill.sizeDelta = new Vector2(340f * Mathf.Clamp01(a), 26); }
    public void SetEnemyCount(int n) { if (enemyCountText) enemyCountText.text = "ENEMIES: " + n; }
    public void SetShield(float secs)
    {
        if (shieldInd == null) return;
        bool on = secs > 0.05f;
        if (shieldInd.activeSelf != on) shieldInd.SetActive(on);
        if (on && shieldText) shieldText.text = "SHIELD  " + Mathf.CeilToInt(secs) + "s";
    }
    public void SetAbilityCharge(float charge01, bool ready)
    {
        if (abilityFill) abilityFill.fillAmount = 1f - Mathf.Clamp01(charge01);
        if (abilityBg) abilityBg.color = ready ? new Color(Accent.r, Accent.g, Accent.b, 0.95f) : new Color(0.28f, 0.32f, 0.4f, 0.85f);
    }
    public void FlashKill(string t)
    {
        if (killText == null) return;
        StopAllCoroutines();
        killText.text = t;
        StartCoroutine(ClearKill());
    }
    IEnumerator ClearKill() { yield return new WaitForSeconds(1.3f); if (killText) killText.text = ""; }

    // ---------- RESULT ----------
    void BuildResult()
    {
        resultPanel = Full("ResultPanel", canvas).gameObject;
        Img(Full("Dim", resultPanel.transform), new Color(0, 0, 0, 0.5f));
        rankBanner = Label(resultPanel.transform, "", 48, Gold, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(1200, 60), new Vector2(0, 215), FontStyle.Bold);
        rankBanner.gameObject.SetActive(false);
        resultTitle = Label(resultPanel.transform, "VICTORY", 110, Accent, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(1200, 160), new Vector2(0, 100), FontStyle.Bold);
        resultSub = Label(resultPanel.transform, "", 30, TextCol, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(1200, 90), new Vector2(0, 20));
        Btn(resultPanel.transform, "REMATCH", new Vector2(300, 74), new Vector2(0.5f, 0.5f), new Vector2(-165, -130), Accent2, () => GameManager.Instance.StartBattle());
        Btn(resultPanel.transform, "MENU", new Vector2(300, 74), new Vector2(0.5f, 0.5f), new Vector2(165, -130), Accent, () => GameManager.Instance.GoMenu());
    }

    void BuildPause()
    {
        pausePanel = Full("PausePanel", canvas).gameObject;
        Img(Full("Dim", pausePanel.transform), new Color(0, 0, 0, 0.74f));
        Label(pausePanel.transform, "PAUSED", 92, TextCol, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(900, 130), new Vector2(0, 130), FontStyle.Bold);
        Btn(pausePanel.transform, "RESUME", new Vector2(360, 76), new Vector2(0.5f, 0.5f), new Vector2(0, 10), Accent, () => GameManager.Instance.SetPaused(false));
        Btn(pausePanel.transform, "QUIT TO MENU", new Vector2(360, 66), new Vector2(0.5f, 0.5f), new Vector2(0, -90), Panel, () => GameManager.Instance.GoMenu(), 26);
        pausePanel.SetActive(false);
    }
    public void ShowPause(bool show) { if (pausePanel) pausePanel.SetActive(show); }

    void BuildData()
    {
        dataPanel = Full("DataPanel", canvas).gameObject;
        Img(Full("Dim", dataPanel.transform), new Color(0, 0, 0, 0.7f));
        var card = Rect("DataCard", dataPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(780, 470), Vector2.zero); Img(card, PanelHi, true);
        Label(card, "SAVE DATA", 44, TextCol, TextAnchor.UpperCenter, new Vector2(0.5f, 1), new Vector2(700, 54), new Vector2(0, -24), FontStyle.Bold);
        Btn(card, "BACKUP TO STORAGE", new Vector2(580, 72), new Vector2(0.5f, 1), new Vector2(0, -110), Accent, () => GameManager.Instance.DoBackup(), 28);
        Btn(card, "RESTORE FROM STORAGE", new Vector2(580, 72), new Vector2(0.5f, 1), new Vector2(0, -196), Accent2, () => GameManager.Instance.DoRestore(), 28);
        dataStatus = Label(card, "Backs up everything: rank, credits, unlocks, squad, loadouts.", 20, Dim, TextAnchor.UpperCenter, new Vector2(0.5f, 1), new Vector2(700, 120), new Vector2(0, -290));
        Btn(card, "CLOSE", new Vector2(300, 64), new Vector2(0.5f, 0), new Vector2(0, 30), Panel, () => ShowData(false), 26);
        dataPanel.SetActive(false);
    }
    public void ShowData(bool show) { if (dataPanel) dataPanel.SetActive(show); }
    public void SetDataStatus(string s) { if (dataStatus) dataStatus.text = s; }
    public void RefreshMenuStats() { UpdateCredits(); UpdateRank(); }
    public void ShowResult(bool win, string sub, string rankUp)
    {
        resultTitle.text = win ? "VICTORY" : "DEFEAT";
        resultTitle.color = win ? Accent : Danger;
        resultSub.text = sub;
        if (rankBanner)
        {
            bool up = !string.IsNullOrEmpty(rankUp);
            rankBanner.gameObject.SetActive(up);
            if (up) { rankBanner.text = "RANK UP!   " + rankUp; StopCoroutine("PulseBanner"); StartCoroutine("PulseBanner"); }
        }
        SetScreen(false, false, false, false, true);
    }
    IEnumerator PulseBanner()
    {
        float t = 0f;
        while (t < 2.6f && rankBanner != null && rankBanner.gameObject.activeInHierarchy)
        {
            t += Time.unscaledDeltaTime;
            float s = 1f + Mathf.Sin(t * 11f) * 0.09f;
            rankBanner.rectTransform.localScale = new Vector3(s, s, 1f);
            rankBanner.color = Color.Lerp(Gold, Color.white, (Mathf.Sin(t * 8f) + 1f) * 0.5f);
            yield return null;
        }
        if (rankBanner) { rankBanner.rectTransform.localScale = Vector3.one; rankBanner.color = Gold; }
    }

    // ---------- switching ----------
    void UpdateCredits()
    {
        int c = SaveSystem.Credits;
        foreach (var t in creditTexts) if (t) t.text = c.ToString();
    }
    void SetScreen(bool menu, bool hangar, bool crate, bool battle, bool result)
    {
        if (menuPanel) menuPanel.SetActive(menu);
        if (hangarPanel) hangarPanel.SetActive(hangar);
        if (cratePanel) cratePanel.SetActive(crate);
        if (battlePanel) battlePanel.SetActive(battle);
        if (resultPanel) resultPanel.SetActive(result);
        if (pausePanel) pausePanel.SetActive(false);
        if (dataPanel) dataPanel.SetActive(false);
    }
    public void ShowMenu()
    {
        if (crateBadge) crateBadge.SetActive(SaveSystem.CrateAvailable());
        UpdateCredits();
        UpdateRank();
        SetScreen(true, false, false, false, false);
    }
    public void ShowHangar() { RefreshHangar(); SetScreen(false, true, false, false, false); }
    public void ShowBattle()
    {
        FireHeld = false;
        var pl = GameManager.Instance.Player;
        if (weaponHud && pl != null) weaponHud.text = pl.Weapon.name;
        if (abilityLabel && pl != null) abilityLabel.text = pl.Ability != null ? pl.Ability.name : pl.Data.abilityName;
        if (mapLabel) mapLabel.text = "ARENA: " + GameManager.Instance.CurrentMap;
        if (killText) killText.text = "";
        if (abilityFill) abilityFill.fillAmount = 0f;
        if (shieldInd) shieldInd.SetActive(false);
        SetScreen(false, false, false, true, false);
    }
}
