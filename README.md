# KNP Anthrax Model

Model for simulating movement of Kudus & Impalas in the norhtern part of the Kruger National Park (KNP), regaring the distribution of Anthrax pathogen.

| Simulation Area: Norhtern KNP                  | Kudu & Imapal movments                                       | Anthrax densitiy                               |
| ---------------------------------------------- | ------------------------------------------------------------ | ---------------------------------------------- |
| ![Simulation Area](./docs/Simulation_Area.png) | ![Kudu and Imapala movement](./docs/Impala_and_Kudu_Movement.png) | ![Anthrax density](./docs/Anthrax_Density.png) |

## Running

You need to install the [.NET SDK 6.0](https://dotnet.microsoft.com/en-us/download), either manually or within a IDE like [Rider](https://www.jetbrains.com/de-de/rider/).

Manually build the simulation with `$ dotnet build` and run it with `$ dotnet run` with terminal in the `KNPAnthrax/` folder.

In Rider you can just open the `KNPAnthrax.sln` file and hit the "Run"-Button in the top-right area of the IDE.

For configuration the model you need to change the values inside the `config.json` file. See the following sections for configuration options for the simulation/agents.

If you can't install .NET you can also use the provided [self-contained boxes](https://github.com/MARS-Group-HAW/knp-anthrax-model/releases), choose you platform and run the `KNPAnthrax` execuatabe. You can configure the model with the `config.json` but wont be able run/test changes in the source-code.

## Simulation

`startPoint` and `endPoint` define start/end the simulation run.

Tick-Lenght: 8h (`deltaT` + `detleTUnit`)

Each agent starts with given Energy level. It drops `1` each tick. When it falls below `15` it will search for the nearest water source to trink and wait there until it's at `100` again. Otherwise each agent will randomly walk arround, with constraints (landtype, proximity to water source).



## Agents

### Kudu

Landscapetypes: Woodland

| Key                                                          | Value                                                | Comment                                                      |
| ------------------------------------------------------------ | ---------------------------------------------------- | ------------------------------------------------------------ |
| `count`                                                      | `int`                                                | Amount of agents, ⚠️ in the file `Resources/impala.csv` you need as much rows. |
| `SpawnMinEnergy`, `SpawnMaxEnergy`                           | `int`                                                | Defines a range in which the energy at spawning will be. This allows for more dynamic animal behaviour, so not all animals need water at the exact same time. |
| `SpawnWoodlandProbability`, `SpawnSavannaProbability`        | percentage between 0 and 1 each, sum needs to be 1.0 | Probabillity the agent will spawn in Woodland/Savanna.       |
| `MinMovementPerTickInM`, `MaxMovementPerTickInM`             | `int` in metes                                       | Distance the agent moves per tick will between these values. |
| `MovementOnWoodland`                                         | 0 to 1                                               | no effect, since the Kudu prefereses Woodland                |
| `MovementOnSavanna`                                          | 0 to 1                                               | If the agent would reach a Savanna patch, the probabillity it would walk on it. |
| `MaxDistanceFromWaterInM`                                    | `int` in meters                                      | Max distance the kudu allows to be away from a water source. |
| `AnthraxInfectionProbability`                                | 0 to 1                                               | Probabillity multiplied with the value from the Anthrax-Layer. |
| `MinInfectionDurationInTicks`, `MaxInfectionDurationInTicks` | `int` each                                           | Range of number of ticks an infected agent will die/leave a trace on the Antrax-Layer. |
| `OutputAgentTrack`                                           | `bool`                                               | If set to `true` for each herd/agent a single GeoJSON conatinings it's track over the runtime of the simulation will be created. |

### Impala

Landscapetypes: Woodland, Savanna

| Key                                                          | Value                                                | Comment                                                      |
| ------------------------------------------------------------ | ---------------------------------------------------- | ------------------------------------------------------------ |
| `count`                                                      | `int`                                                | Amount of agents, ⚠️ in the file `Resources/kudu.csv` you need as much rows. |
| `SpawnMinEnergy`, `SpawnMaxEnergy`                           | `int`                                                | Defines a range in which the energy at spawning will be. This allows for more dynamic animal behaviour, so not all animals need water at the exact same time. |
| `SpawnWoodlandProbability`, `SpawnSavannaProbability`        | percentage between 0 and 1 each, sum needs to be 1.0 | Probabillity the agent will spawn in Woodland/Savanna.       |
| `MinMovementPerTickInM`, `MaxMovementPerTickInM`             | `int` in metes                                       | Distance the agent moves per tick will between these values. |
| ⚠️ no Woodland/Savanna                                        |                                                      | No options are given due to the near equal disitribution.    |
| `MaxDistanceFromWaterInM`                                    | `int` in meters                                      | Max distance the kudu allows to be away from a water source. |
| `AnthraxInfectionProbability`                                | 0 to 1                                               | Probabillity multiplied with the value from the Anthrax-Layer. |
| `MinInfectionDurationInTicks`, `MaxInfectionDurationInTicks` | `int` each                                           | Range of number of ticks an infected agent will die/leave a trace on the Antrax-Layer. |
| `OutputAgentTrack`                                           | `bool`                                               | If set to `true` for each herd/agent a single GeoJSON conatinings it's track over the runtime of the simulation will be created. |




## Layers

### LandscapeLayer

Provides landtype classification. Input data are manually mapped againts `Woodland` and `Savanna` categories.

### Perimeter

Simulation area, derived from union of landscape layer input.

### WaterLayer

POIS and rivers. GeoJSON can contain POINTs or LINESTRINGs. Derived from various watersources.

> **Warning**
>
> All geometries **must** be inside the Perimter shape, or the simulation will break.

### AnthraxLayer

Raster data with 1x1km resolution containing Anthrax carcasses from KNP northern area.

## Outputs

When using Rider, outputs will be in the folder `KNPAnthrax/bin/Debug/net6.0/`, in case you use .NET directyl they will be in the same folder.

- `Kudu_trips.geojson` and `Impala_trips.geojson` contain time-series movement data of each agent type. Can be visualized with [kepler.gl](https://kepler.gl), though due to the 8h hour resulation you can't really see much)
- `LandscapeLayer_types.geojson` contains the mapping of input Landscape-Shape file to the repsective landtype in the model (Woodland, Savanna)
- `AnthraxLayer_Start.geojson` the input raster data of the Anthrax distriubtion (basically the input asc/GeoTIFF converted to GeoJSON)
- `AnthraxLayer_End.geojson` the anthrax densitiy after each animal lleft it's trace
- `KuduMovement.geojson` and `ImpalaMovement.geojson` heatmaps of the movement of all the Kudus/Imapals. Each agent increments the cell it stands on after it's tick with `0.1`.



Outputs can be visualized with [kepler.gl](https://kepler.gl/). It's a browser based visualization tool and needs to be reconfigured each time. For convenience you can use the Jupyter Notebook with a preset configured to load some data (see folder `Analysis/` ). Due to complicatated dependencies you can use the [Docker Container](https://www.docker.com/products/docker-desktop/) for starting a Jupyter Hub with all needed dependencies (start it with `$ ./notebookdocker.sh`).