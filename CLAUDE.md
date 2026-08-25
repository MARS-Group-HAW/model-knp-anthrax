# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

An agent-based simulation of Kudu and Impala movement in the northern Kruger National Park (KNP), used to model the spread/density of Anthrax infection sites. Built on the [MARS Framework](https://mars-group-haw.github.io/index.html) (`Mars.Life.Simulations` NuGet package), a .NET agent-based modeling framework that handles the simulation loop, spatial environments, and layer/agent config wiring.

## Commands

All commands run from the `KNPAnthrax/` directory (the actual .NET project; the repo root only has the solution file, docs, and the analysis notebooks).

```bash
cd KNPAnthrax
dotnet build                    # build
dotnet run                      # run using config.json in this dir
dotnet run -sm config.json      # equivalent, explicit config (see run.sh)
```

There are no automated tests in this repository.

To build self-contained, distributable release "boxes" for macOS/Windows/Linux (zipped, with `Resources/` and `config.json` bundled):

```bash
./build.sh              # builds all models listed in the script
./build.sh KNPAnthrax    # build just this one
```

Simulation output (CSV, GeoJSON heatmaps/tracks) is written to `KNPAnthrax/bin/Debug/net10.0/` when run via `dotnet run`, or next to the executable when run from a published box. See the README's "Outputs" section for the full list of output files and what they contain.

### Analysis

`Analysis/` holds Jupyter notebooks for visualizing output (kepler.gl-based). Run via Docker (`./notebookdocker.sh` builds/runs a Jupyter Lab container on `localhost:8888`); `Analysis/map_config.py` stores the kepler.gl map configuration.

## Architecture

The whole model is assembled in `Program.cs`: it builds a `ModelDescription` (registering all layers and agent types), loads `config.json` into a `SimulationConfig`, and starts the MARS simulation. All model code lives in `KNPAnthrax/Model/`.

**Layers** (`AbstractLayer`/`VectorLayer`/`RasterLayer` subclasses) provide the spatial environment and static/derived data agents read from:
- `AnimalLayer` — the layer agents register on; computes the simulation bounding box from `Perimeter` and builds a `GeoHashEnvironment<T>` per species (`KuduEnvironment`, `ImpalaEnvironment`) that agents move within. Spawns agents via `IAgentManager`.
- `LandscapeLayer` (vector) — classifies KNP into `Woodland`/`Savanna`/`Unknown` via a hardcoded `LSCAP_ID -> LandscapeType` mapping; also dumps `LandscapeLayer_types.geojson` on init.
- `Perimeter` (vector) — the walkable simulation boundary polygon; `IsPointInside` gates all agent movement.
- `WaterLayer` (vector) — water sources (points/linestrings) agents seek when low on energy; every feature must lie within `Perimeter` or the sim breaks.
- `AnthraxLayer` (raster) — per-cell Anthrax density; agents roll infection against cell values and increment them when an infected agent "dies". Periodically dumped to GeoJSON (`AnthraxLayer_Start`/`_Tick_N`/`_End`) per `WriteHeatMapToFileEveryXTicks`.
- `KuduMovement` / `ImpalaMovement` (raster, structurally identical) — pure movement-density heatmaps, incremented by each agent's tick and dumped to GeoJSON at the last tick.

**Agents** (`Kudu`, `Impala`, implementing `IAgent<AnimalLayer>` + `IPositionable`) are near-duplicate state machines over `AnimalState` (`RandomMove -> SearchForWater -> Drinking -> RandomMove`):
- `RandomMove`: if off preferred land type, beeline to the nearest patch of it; otherwise sample a bearing as a normal distribution around the previous bearing (`GetBearingBasedOnPreviousBearing`), reject candidate moves that leave `Perimeter` or land on a disallowed type (Kudu additionally allows Savanna only with probability `MovementOnSavanna`; Impala accepts both types unconditionally), and steer toward water if farther than `MaxDistanceFromWaterInM`. Drops to `SearchForWater` when `Energy < 15`.
- `SearchForWater`: `Explore()`s `WaterLayer` within 50km, picks the true nearest feature (results aren't distance-sorted), and moves toward it; switches to `Drinking` within 20m.
- `Drinking`: `Energy += 50`/tick until > 101, then back to `RandomMove`.
- Every tick: energy -= 1, optional trajectory point recorded (`OutputAgentTrack`), infection roll against `AnthraxLayer` value * `AnthraxInfectionProbability`, scheduled "death"/trace tick (`MinInfectionDurationInTicks`..`MaxInfectionDurationInTicks` later) that increments the `AnthraxLayer` cell, and a movement-heatmap increment. `StoreTickResult` gates whether the CSV writer emits a row for this tick (always on first/last tick or when an infection event occurred, to keep output small); it is wired via `config.json`'s `outputFilter` for each agent, not consumed directly by MARS elsewhere.
- Agents can spawn either from `Resources/{kudus,impalas}.csv` (fixed Lat/Long per row — row count must match `config.json`'s `count`) or, if Lat/Long is `0.0`, at a random position on a land type chosen by `SpawnWoodlandProbability`/`SpawnSavannaProbability` (which must sum to `1.0`).

Since `Kudu.cs` and `Impala.cs` (and `KuduMovement.cs`/`ImpalaMovement.cs`) are structurally parallel, a behavioral change to one species' movement/infection logic usually needs the equivalent change made in the other file too — check both.

## Configuration (`config.json`)

Drives everything: simulation start/end/tick length (`globals`), which layers to load and from which `Resources/` file (`layers`), and per-species agent parameters (`agents[].mapping`, matched to `[PropertyDescription(Name=...)]`-annotated properties on `Kudu`/`Impala`). See the README's "Configuration" section for the full parameter reference (spawn energy/probabilities, movement distance/preference, water-seeking distance, infection probability/duration, `OutputAgentTrack`). Changing `count` for a species requires the matching CSV in `Resources/` to have the same number of rows.
