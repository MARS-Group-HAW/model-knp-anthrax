# KNP Anthrax Model



## Running

You need to install the [.NET SDK 6.0](https://dotnet.microsoft.com/en-us/download), either manually or within a IDE like [Rider](https://www.jetbrains.com/de-de/rider/).

Manually build the simulation with `$ dotnet build` and run it with `$ dotnet run` with terminal in the `KNPAnthrax/` folder.

In Rider you can just open the `KNPAnthrax.sln` file and hit the "Run"-Button in the top-right area of the IDE.

For configuration the model you need to change the values inside the `config.json` file. See the following sections for configuration options for the simulation/agents.



## Simulation

`startPoint` and `endPoint` define start/end the simulation run.

Tick-Lenght: 8h (`deltaT` + `detleTUnit`)



## Agents

### Kudu

Landscapetypes: Woodland

| Key                                                          | Value                                                | Comment                                                      |
| ------------------------------------------------------------ | ---------------------------------------------------- | ------------------------------------------------------------ |
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




## Layers

### LandscapeLayer

Provides landtype classification. Input data are manually mapped againts `Woodland` and `Savanna` categories.

### Perimter

Simulation area, derived from union of landscape layer input.

### WaterLayer

POIS and rivers. GeoJSON can contain POINTs or LINESTRINGs. Derived from various watersources.

> **Warning**
>
> All geometries **must** be inside the Perimter shape, or the simulation will break.

### AnthraxLayer

Raster data with 1x1km resolution containing Anthrax carcasses from KNP northern area.





## Outputs

- `Kudu_trips.geojson` and `Impala_trips.geojson` contain time-series movement data of each agent type. Can be visualized with [kepler.gl](https://kepler.gl), though due to the 8h hour resulation you can't really see much)
- `LandscapeLayer_types.geojson` contains the mapping of input Landscape-Shape file to the repsective landtype in the model (Woodland, Savanna)
- `AnthraxLayer_Start.geojson` the input raster data of the Anthrax distriubtion (basically the input asc/GeoTIFF converted to GeoJSON)
- `AnthraxLayer_End.geojson` the anthrax densitiy after each animal lleft it's trace
- `KuduMovement.geojson` and `ImpalaMovement.geojson` heatmaps of the movement of all the Kudus/Imapals. Each agent increments the cell it stands on after it's tick with `0.1`.



Outputs can be visualized with [kepler.gl](https://kepler.gl/). It's a browser based visualization tool and needs to be reconfigured each time. For convenience you can use the Jupyter Notebook with a preset configured to load some data (see folder `Analysis/` ). Due to complicatated dependencies you can use the [Docker Container](https://www.docker.com/products/docker-desktop/) for starting a Jupyter Hub with all needed dependencies.