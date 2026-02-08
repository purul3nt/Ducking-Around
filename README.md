# 🦆 Ducking Around

A casual arcade game made for **Supercell's Global AI Game Hack**. Guide your cursor to zap ducks in the hot tub before time runs out—then spend your gold on upgrades and watch the pool grow as you hit milestone after milestone.

![Ducking Around](https://img.shields.io/badge/Supercell-Global%20AI%20Game%20Hack-00D9FF?style=for-the-badge)
![Unity](https://img.shields.io/badge/Unity-2022+-black?style=flat-square&logo=unity)

---

## 🎮 How to Play

- **Move the breaker** with your mouse over the water. Ducks inside the breaker ring take damage.
- **Eliminate ducks** before the session timer hits zero. Each duck sucked into the crocodile earns you gold.
- **Spend gold** between sessions on upgrades: more session time, bigger breaker, more damage, special ducks (Fire, Electro, Lazer), and more.
- **Hit milestones** (100, 200, 400, 800… ducks killed) to zoom out, grow the pool, and unlock more space.

---

## ▶️ Running the Game

**Windows (build):**

1. Open the `build` folder.
2. Double-click **`Ducking Around.exe`** to launch the game.

**From source (Unity):**

1. Open the project in **Unity 2022** or later.
2. Add the main game scene to **File → Build Settings** if needed.
3. Press **Play** in the Editor, or build to the `build` folder via **File → Build and Run**.

---

## ✨ Features

- **Session-based gameplay** — Race the clock, earn gold, then upgrade and restart.
- **Progressive difficulty** — Pool and camera expand at exponential milestones (100, 200, 400, 800…) so the arena grows with you.
- **Upgrade tree** — Unlock session time, breaker size/damage/speed, duck count, crits, and special duck spawns (Fire, Electro, Lazer).
- **Juice** — Floating damage numbers, camera shake on big hits, gold/timer pulses, crocodile pulse on duck capture, summary bounce-in, and music/SFX.

---

## 🛠 Tech

- **Engine:** Unity (C#)
- **Rendering:** URP
- **UI:** TextMeshPro

**Credits:** Assets were made with [Rodin](https://rodin.io); music with [Suno AI](https://suno.com); code was assisted by [Cursor](https://cursor.com) and GPT.

---

## 📁 Project Structure

| Folder / file   | Description |
|-----------------|-------------|
| `build/`        | Windows build; run **Ducking Around.exe** from here. |
| `Assets/`       | Scenes, scripts, art, prefabs, and settings. |
| `Assets/Scripts/` | Core game logic (GameManager, BreakerController, Duck, UIManager, upgrades, etc.). |
| `ProjectSettings/` | Unity project configuration. |
| `Packages/`     | Unity packages and dependencies. |

---

## 📜 License

This project was created for **Supercell's Global AI Game Hack**. Use and distribution are subject to the hackathon rules and license.

---

**Ducking Around** — *Made for Supercell Global AI Game Hack*
