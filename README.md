# Your Journey

**Your Journey** is a companion app that plays custom scenarios and campaigns for the
*Journeys in Middle-earth* board game. You write the scenarios in the
[JiME Scenario Editor](https://github.com/openfantasymap/JiMEditor). This app then runs them
at the table, the way the official app runs the published adventures. It lays out the map
tiles, reads out story text, runs events and stat tests, spawns and tracks enemies, and runs
the Shadow phase (threat, darkness, enemy activations).

It is built with Unity **6000.0.84f1 (Unity 6 LTS)** and runs on **Windows, macOS (Intel and
Apple Silicon) and Linux**.

Originally created by [GlowPuff](https://github.com/GlowPuff/your-journey). This fork adds
cross-platform support and automated builds.

> **Status:** early alpha. Battle-map scenarios are not supported yet.

---

## Download & install

Get the latest build from the [Releases page](https://github.com/openfantasymap/your-journey/releases):

| Platform | File | How to run |
|---|---|---|
| Windows 10/11 (x64) | `YourJourney-windows-x64.zip` | Unzip, run `YourJourney.exe` |
| macOS 11+ (Intel & Apple Silicon) | `YourJourney-macos-universal.zip` | Unzip, move `YourJourney.app` to *Applications* (see below) |
| Linux (x64) | `YourJourney-linux-x64.tar.gz` | `tar xzf …`, run `./YourJourney.x86_64` |

**macOS first launch:** the app is not notarized by Apple, so Gatekeeper blocks the first launch. Either
right-click the app, choose *Open*, then *Open*, or clear the quarantine flag once:

```bash
xattr -dr com.apple.quarantine /Applications/YourJourney.app
```

## Where your files live

The player and the editor share one data folder, **`Your Journey`**, inside your Documents folder:

| OS | Folder |
|---|---|
| Windows | `%USERPROFILE%\Documents\Your Journey` |
| macOS | `~/Documents/Your Journey` |
| Linux | `$XDG_DOCUMENTS_DIR/Your Journey`, default `~/Documents/Your Journey` |

To use a different folder, set the `JIME_DATA_DIR` environment variable to its full path. The
editor reads the same variable.

```
Your Journey/
├── My Scenario.jime              standalone scenarios (JSON) saved by the editor
├── My Campaign.zip               campaign packages exported by the editor
├── <campaign-guid>/              campaigns, unpacked automatically on startup
│   ├── <campaign-guid>.json      campaign metadata (name, story, scenario order, triggers)
│   └── Chapter 1.jime …          the campaign's scenarios
└── Saves/
    ├── SAVE0.sav … SAVE5.sav     the six save slots
    └── TEMP.sav                  quick-save (S / L in game)
```

* **Standalone scenario:** save it in the editor, or copy the `.jime` file into the folder.
* **Campaign:** in the editor use *Package Campaign*, then drop the `.zip` into the folder. On
  startup the app unpacks every zip into a folder named after the campaign's GUID. Zips that
  Finder or Explorer re-compressed also work, because extra `__MACOSX` entries and sub-folders
  are ignored.

## Playing

1. **New Game**, then pick a scenario or campaign, name your game, choose heroes and difficulty.
2. The app shows the intro text and places the first chapter's tiles. Follow the prompts to set
   up the physical board.
3. Click tokens on the tiles to trigger events: exploration text, stat tests, decisions,
   dialogs, fights and so on. Use the Party panel and the enemy buttons during combat.
4. **End Turn** starts the Shadow phase: threat rises, darkness spreads, and enemies move and
   attack.
5. The game auto-saves to its slot after every Shadow phase and when you quit to the title. Use **Load Game** to continue.

### Controls

| Action | Mouse | Trackpad / keyboard |
|---|---|---|
| Pan the board | left-drag | one-finger drag |
| Rotate the view | right-drag | **Ctrl/⌥/⌘ + drag**, or **Q / E** |
| Zoom | scroll wheel | two-finger scroll, or **+ / −** |
| Centre on a tile | double-click a tile | double-tap |
| Re-centre on the map | **Space** | **Space** |
| Quick save / quick load | **S** / **L** | **S** / **L** |

---

## How it works

```
JiME Editor ──(.jime JSON / campaign .zip)──▶ Documents/Your Journey ──▶ Your Journey (Unity)
                                                                        │
                        title scene: pick journey, heroes, save slot ◀──┤
                        gameboard scene: tiles, events, enemies, shadow ◀┘
```

* **`Scenes/title.unity`**: title screen, journey browser, hero selection, save slots, campaign screen
  (`Assets/Scripts/Title`).
* **`Scenes/gameboard.unity`**: the game itself (`Assets/Scripts/Engine`).
  * `Engine.cs` owns the game loop and wires the managers together.
  * `Managers/`: `ChapterManager` (tile reveal), `InteractionManager` (events), `TriggerManager`
    (scenario triggers), `ObjectiveManager`, `MonsterManager`/`FightManager` (enemies, combat),
    `ShadowPhaseManager` (threat and darkness), `TileManager` (hex-tile placement).
  * `Interactions/`: one class per event type (text, stat test, decision, branch, dialog,
    darkness, reward, replace token, persistent, conditional, multi-event, enemy).
  * `Models/`: the scenario data model, shared in shape with the editor.
  * `Panels/`: UI panels for text, stat tests, combat, lore/XP and so on.
* **`Bootstrap.cs`**: static state carried between the two scenes (selected scenario, heroes, campaign).
* **`AppPaths.cs`**: resolves the data folder the same way on every OS.
* **`Engine/Managers/FileManager.cs`**: reads `.jime` scenarios and campaigns, and unpacks
  campaign zips.
* **`Engine/GameState.cs`**: serializes the whole game state (party, triggers, objectives,
  monsters, tiles, camera) to `Saves/*.sav`.
* **`Assets/Editor/BuildScript.cs`**: the command-line and CI build entry point.

Third-party: [DOTween](http://dotween.demigiant.com/) (in `Assets/Demigiant`),
Newtonsoft.Json (`com.unity.nuget.newtonsoft-json`), Post Processing v2, ProBuilder.

## Developing

1. Install **Unity Hub**, then editor **6000.0.84f1** with *Mac*, *Windows (Mono)* and *Linux (Mono)*
   Build Support as needed. On Apple Silicon the editor runs natively.
2. Open the `YourJourney/` folder in Unity Hub (*Add project from disk*).
3. Open `Assets/Scenes/title.unity` and press Play. Put a `.jime` file in your data folder first.

> **Upgrading from Unity 2019.3.** The project was moved from 2019.3 to Unity 6 by updating
> `ProjectVersion.txt` and `Packages/manifest.json`. On the first open, Unity re-imports everything,
> upgrades ProjectSettings and ProBuilder meshes, and may run the API updater. Commit the resulting
> changes to `ProjectSettings/` and `Assets/`.

### Building locally

From the editor use *File > Build Profiles*. From the command line (paths shown for macOS):

```bash
/Applications/Unity/Hub/Editor/6000.0.84f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -quit -projectPath YourJourney \
  -executeMethod BuildScript.Build \
  -customBuildTarget StandaloneOSX \
  -customBuildPath build/StandaloneOSX/YourJourney.app \
  -buildVersion 1.0.0 -logFile -
```

`-customBuildTarget` can be `StandaloneWindows64`, `StandaloneOSX` (universal Intel + Apple
Silicon) or `StandaloneLinux64`.

### Compile check without a Unity license

`.github/scripts/compile-check.sh` compiles every script against the pinned editor's assemblies and
package versions. It uses the official GameCI editor image and needs no license:

```bash
docker run --rm -v "$PWD":/proj unityci/editor:ubuntu-6000.0.84f1-base-3 \
  bash /proj/.github/scripts/compile-check.sh
```

## Building on GitHub

`.github/workflows/build.yml` does the following:

| Trigger | Jobs |
|---|---|
| any push or pull request | **Compile check** (no license needed) |
| push to `master`, manual run | compile check, then **Windows / macOS / Linux builds** (downloadable as workflow artifacts) |
| tag `v*` (e.g. `v0.16.0`) | all of the above, then a **GitHub Release** with the three downloads |

Builds use [GameCI](https://game.ci/) (`game-ci/unity-builder`) on Ubuntu runners. The macOS app
is then ad-hoc code-signed and zipped on a macOS runner, which Apple Silicon needs in order to
run it. Tags with a hyphen (`v1.0.0-beta.1`) are published as pre-releases.

### One-time setup: Unity license secrets

Unity needs a license even for headless builds. Add these under *Settings > Secrets and variables >
Actions*:

| Secret | Value |
|---|---|
| `UNITY_EMAIL` | your Unity ID email |
| `UNITY_PASSWORD` | your Unity ID password |
| `UNITY_LICENSE` | **Personal license:** the full contents of your `Unity_lic.ulf` file |
| `UNITY_SERIAL` | **Pro/Plus license:** the serial key (use instead of `UNITY_LICENSE`) |

To get `Unity_lic.ulf` for a Personal license, activate Unity Hub once on your machine. The file
is at:

* macOS: `/Library/Application Support/Unity/Unity_lic.ulf`
* Windows: `C:\ProgramData\Unity\Unity_lic.ulf`
* Linux: `~/.local/share/unity3d/Unity/Unity_lic.ulf`

See the [GameCI activation guide](https://game.ci/docs/github/activation) for details.

### Making a release

```bash
git tag v0.16.0
git push origin v0.16.0
```

The version (without the `v`) is written into the players' `bundleVersion`.

## Related

* **Scenario editor:** [openfantasymap/JiMEditor](https://github.com/openfantasymap/JiMEditor)
* Original project and discussion: [GlowPuff/your-journey](https://github.com/GlowPuff/your-journey),
  [BoardGameGeek thread](https://boardgamegeek.com/thread/2488415/custom-scenario-editor-and-companion-app-create-yo)

## License

See [LICENSE](LICENSE). *Journeys in Middle-earth* is a trademark of Fantasy Flight Games. This is
an unofficial fan project.
