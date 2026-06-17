# Screen Antics: Johnny Castaway — Modern

A modern, up-res'd reimplementation of Sierra/Dynamix's 1992 *Screen Antics: Johnny
Castaway* screensaver, built in C# / .NET 10 with SkiaSharp. It replaces the original
16-bit engine — which no longer runs on 64-bit Windows — with a clean managed runtime that
interprets the game's original animation scripts and renders them with AI-upscaled art at
modern resolutions.

> Johnny is a hapless castaway stranded on a tiny island with one palm tree. Over a simulated
> day he fishes, builds sandcastles, gets visited, dreams, and (rarely) finds a message in a
> bottle — the "world's first story-telling screen saver."

## Status

Work in progress. Implemented so far:

- ✅ **Content pipeline** — the original game's decoded animation (TTM) and scene-director
  (ADS) scripts serialized to a clean JSON bundle.
- ✅ **TTM virtual machine** — a faithful animation-bytecode interpreter (sprite compositing,
  GETPUT save-under, delays, sequence chaining) rendered with SkiaSharp.
- ✅ **ADS director** — sequences vignettes with conditionals, weighted randomness, and
  chaining (walk → cast → reel → catch …), so gags play coherently and never the same way
  twice.
- ✅ **Day clock + scheduler** — a 16-"hour" in-game day driven by your local clock and a
  configurable start-of-day, selecting time-appropriate vignettes (fishing by day, sleeping
  by night, …).
- ✅ **Screensaver shell** — a borderless full-screen `/s` host with exit-on-input and `/p`
  preview, scaling the art to any monitor.

Planned next: sound effects, a settings dialog, multi-monitor, and a packaged installer that
registers `JohnnyCastaway.scr` into Windows Screen Saver settings.

## Architecture

```
content bundle (scripts.json + 4× art + audio)        ← generated from original game data
        │
   DayClock → Scheduler → AdsDirector → TtmVm → SkiaSharp renderer → screen
```

- `JohnnyCastaway.Engine` — the runtime: content bundle loader, `TtmVm`, `AdsDirector`,
  `DayClock`, `Scheduler`, `ScenePlayer`.
- `JohnnyCastaway.ScreenSaver` — the WinForms `.scr` host.
- `JohnnyCastaway.Cli` — a headless renderer (`--render <TTM> <seq> out.png`) for testing.
- `JohnnyCastaway.Tests` — xUnit.

## Building

```
dotnet build
dotnet test
dotnet run --project src/JohnnyCastaway.ScreenSaver -- /s   # run the screensaver
```

Requires the .NET 10 SDK.

## Content & assets (not included)

This repository contains **source code only**. It does **not** include the game's graphics,
sounds, or scripts — those are © 1992 Sierra On-Line / Dynamix. To run it you must supply a
content bundle generated from your own copy of the original *Johnny Castaway* data
(`RESOURCE.001` / `RESOURCE.MAP`). The runtime reads `content/scripts.json` + `manifest.json`
and the art/audio referenced therein.

## Credits & license

- The DGDS format decoders and the TTM/ADS interpreter logic are derived from the
  **[ScummVM](https://www.scummvm.org/) `dgds` engine** (GPLv3). Because this work is a
  derivative of GPLv3 code, **this project is licensed under GPLv3** (see `LICENSE`).
- AI up-res via **Real-ESRGAN** (anime-tuned model).
- Original game © 1992 Sierra On-Line / Dynamix. This project is an unaffiliated,
  non-commercial fan reimplementation.
