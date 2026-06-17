# Screen Antics: Johnny Castaway — Modern

A modern, up-res'd reimplementation of Sierra/Dynamix's 1992 *Screen Antics: Johnny
Castaway* screensaver, built in C# / .NET 10 with SkiaSharp. It replaces the original
16-bit engine — which no longer runs on 64-bit Windows — with a clean managed runtime that
interprets the game's original animation scripts and renders them with AI-upscaled art at
modern resolutions.

> Johnny is a hapless castaway stranded on a tiny island with one palm tree. Over a simulated
> day he fishes, builds sandcastles, gets visited, dreams, and (rarely) finds a message in a
> bottle — the "world's first story-telling screen saver."

## Features

- **Faithful engine** — a TTM animation-bytecode virtual machine (sprite compositing, save-under,
  delays) and an ADS scene-director (conditionals, weighted randomness, sequence chaining) that
  reproduce the original gags (walk → cast → reel → catch …) — never the same way twice.
- **A simulated day** — a 16-"hour" in-game clock driven by your local time and a configurable
  start-of-day, choosing time-appropriate vignettes (fishing by day, sleeping by night) with
  seasonal awareness.
- **Up-res'd art** — sprites and backgrounds upscaled 4× (Real-ESRGAN) and scaled to any monitor.
- **Sound** — the original sound effects, triggered in sync by the animation scripts (off by default).
- **A real screensaver** — borderless full-screen `/s`, `/p` preview, a `/c` settings dialog
  (start-of-day, sound), **multi-monitor**, and a one-command installer that registers
  `JohnnyCastaway.scr` into Windows Screen Saver settings.

## Architecture

```
content bundle (scripts.json + 4× art + audio)        ← generated from original game data
        │
   DayClock → Scheduler → AdsDirector → TtmVm → SkiaSharp renderer → screen
                                          └→ NAudio (sound effects)
```

- `JohnnyCastaway.Engine` — runtime: content loader, `TtmVm`, `AdsDirector`, `DayClock`,
  `Scheduler`, `ScenePlayer`, `ContentLocator`, settings + audio interfaces.
- `JohnnyCastaway.ScreenSaver` — the WinForms `.scr` host (rendering, settings dialog, audio,
  multi-monitor).
- `JohnnyCastaway.Cli` — a headless renderer (`--render <TTM> <seq> out.png`) for testing.
- `JohnnyCastaway.Tests` — xUnit (54 tests).

## Building & running

```
dotnet build
dotnet test
dotnet run --project src/JohnnyCastaway.ScreenSaver -- /s   # run the screensaver (needs content/, below)
```

Install as a real screensaver (see `installer/`):

```powershell
installer\build.ps1      # publish a self-contained JohnnyCastaway.scr + bundle content
installer\install.ps1    # copy to %LOCALAPPDATA%\JohnnyCastaway and register it
installer\uninstall.ps1  # remove and unregister
```

Requires the .NET 10 SDK.

## Content & assets (not included)

This repository contains **source code only**. It does **not** include the game's graphics,
sounds, or scripts — those are © 1992 Sierra On-Line / Dynamix. To run it you must supply a
`content/` bundle generated from your own copy of the original *Johnny Castaway* data
(`RESOURCE.001` / `RESOURCE.MAP`): `content/scripts.json`, `content/manifest.json`, and the
art/audio under `content/sprites`, `content/backgrounds`, `content/audio`. The runtime locates
this bundle next to the installed `.scr` (or in the repo root during development). The tooling
that decodes the original game into this bundle is not distributed (it derives from GPL ScummVM
and would embed copyrighted assets).

## Credits & license

- The DGDS format decoders and the TTM/ADS interpreter logic are derived from the
  **[ScummVM](https://www.scummvm.org/) `dgds` engine** (GPLv3). Because this work is a
  derivative of GPLv3 code, **this project is licensed under GPLv3** (see `LICENSE`).
- AI up-res via **Real-ESRGAN** (anime-tuned model).
- Original game © 1992 Sierra On-Line / Dynamix. This project is an unaffiliated,
  non-commercial fan reimplementation.
