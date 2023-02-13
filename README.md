# KNP Anthrax Model



## Agents

### Kudu




### Impala


## Layers

### LandscapeLayer

Provides landtype classification. Input data are manually mapped againts `Woodland` and `Savanna` categories.

### Perimter

Simulation area, derived from union of landscape layer input.

### WaterLayer

POIS and rivers. GeoJSON can contain POINTs or LINESTRINGs. Derived from various watersources.

:: **Warning**
> All geometries ***must** be inside the Perimter shape, or the simulation will break.

### AnthraxLayer

Raster data with 5x5km resolution containing Anthrax carcasses from KNP northern area.