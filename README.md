# ⚙️ MECH ARENA

A fast, touchscreen **3D mech battler** for Android. Build a squad, deploy into randomized arenas, and blast waves of AI mechs. Everything — mechs, weapons, UI, effects, and sound — is **generated procedurally in code** (no art or audio assets), so the whole game is a compact set of C# scripts running on Unity 6 + URP.

> Built for the **Pixel 6a** and any modern **arm64** Android phone.

---

## 📲 Install

1. Go to the [**Releases**](../../releases) page.
2. Download the latest **`MechArena.apk`**.
3. On your phone, tap the APK to install (allow *Install unknown apps* if prompted).
4. If you have an older copy, uninstall it first only if it was signed with a different key.

*APK is release-signed (v2), arm64-v8a, min Android 7.0.*

---

## 🎮 Features

- **18 mechs** — from free starters to glowing **Legendary** and **Mythic** chassis, each with distinct silhouettes (head/shoulder styles), colors, stats, and a signature ability.
- **17 weapons** — Blaster, Cannon, Scatter, Rocket, Lance, Minigun, Railgun, Plasma, Flak, Arc, plus overpowered legendaries (Oblivion, Inferno, Stormcaller, Annihilator, Voidripper, Singularity, Apocalypse). Each has its own projectile behavior and arm model.
- **10 abilities** you can buy and equip on any mech — Shield, Blink dash, Tesla (insta-kill), Slam AoE, Phantom strike, Repair, Overdrive, Cloak, Barrage, EMP.
- **3-mech battle squad** with a **mid-fight hotbar** — swap mechs on the fly; each keeps its own health, and a downed mech auto-deploys the next.
- **Rank progression** with tiers (Recruit → … → Legend) and a home-screen badge; XP from battles and unlocks.
- **Legendary/Mythic gear** gated by both **rank** and **credits**, scaling gradually so it's a reachable grind.
- **Scaling difficulty** — enemy mechs, weapons, health, and count scale to your rank + hangar, so early fights are gentle and it ramps up fairly.
- **Line-of-sight cover** — pillars and blocks stop shots; use them tactically.
- **Drag-to-look camera** (right side of screen) with protected fire/ability zones.
- **Daily reward crate** with an open animation.
- **Backup / Restore** — export your whole save to a JSON file and restore it any time.
- **Procedural SFX + ambience** — weapon-specific fire, hits, explosions, ability sounds, UI clicks, victory/defeat stingers, rank-up jingle.
- **Polish** — bloom, camera shake, fireworks on rank-up, kill feed, pause menu, rematch.

---

## 🕹️ Controls

- **Left joystick** — move (camera-relative)
- **FIRE** (bottom-right) — auto-aims the nearest enemy you have a clear shot at
- **ABILITY** (left of FIRE) — signature ability, with a radial cooldown
- **Hotbar** (bottom-center) — tap to swap squad mechs
- **Drag right side** — look around

---

## 🛠️ Build from source

Requires **Unity 6000.3.5f1** (URP) with Android Build Support.

- Open the project, or build headless:
  `Unity.exe -batchmode -quit -projectPath . -buildTarget Android -executeMethod BuildScript.PerformAndroidBuild`
- Output: `Build/MechArena.apk` (IL2CPP / arm64, release-signed).
- All gameplay lives in `Assets/Scripts/`; the scene (`Assets/Scenes/Main.unity`) holds a single `Bootstrap` object that generates everything at runtime.

See [CHANGELOG.md](CHANGELOG.md) for version history.
