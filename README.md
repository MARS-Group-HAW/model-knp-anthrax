# MARS GeoRasterBlueprint

The MARS GeoRasterBlueprint is an simple MARS model that incorporates georeferenced raster data. The model's environment represents the Addo Elephant National Park. This area is modelled as a grid layer with georeferenced cells. `Elephant` agents can move within the perimeter of the Addo Elephant National Park, which is defined by the .asc grid file `addo.asc`.

The model also includes water sources in the form of vector data, which the `Elephant` agents can navigate to. Therefore, the model also illustrates the use of georeferenced vector data.

**Note:** this model intends to showcase some technical aspects of MARS only. It does not aspire to model elephant behavior accurately.

## How to get raster data

To create a georeferenced raster layer of your simulation area, please see the [MARS documentation](https://mars.haw-hamburg.de/articles/core/tutorials/create_vector_layer_raster_layer.html).

Simple Vector layers can be created with [QGIS](https://www.qgis.org/en/site/) or at [geojson.io](https://geojson.io/).

## Model Structure and Resources

The following components and resources make up the model:

- `Elephant`: This agent type models an elephant that moves around randomly in the Addo Elephant National Park. When its `Energy` is below a certain threshold, it queries the `WaterLayer` (see below) for a water source to navigate to. `Elephant` agents are parameterized with the elephants.csv file in the `Resources` folder.
- `LandscapeLayer`: This layer holds a grid of cells. Each cell is referenced by geocoordinates. The georeferenced data contained in the `Perimeter` (see below) are used to determine which cell is accessible (i.e., within the perimeter) and inaccessible (i.e., outside of the perimeter).
- `Perimeter`: This raster layer holds the perimeter of the Addo Elephant National Park. The georeferenced data are provided by the addo.asc file in the `Resources` folder.
- `WaterLayer`: This vector layer holds a set of vector features that represent water sources in the Addo Elephant National Park. The georeferenced data of the water sources are provided by the WaterSpots.geojson file in the `Resources` folder.

## Model configuration

The model can be configured via the `config.json` file in the root folder of the blueprint. For additional information, please see the [MARS documentation](https://mars.haw-hamburg.de/articles/core/model-configuration/sim_config_options.html).

## Visualizing the model output

The vector data and the movement of the `Elephant` agents is stored in a file named `Elephant_trips.geojson`, which is stored in GeoRasterBlueprint/bin/Debug/net6.0. The movement trajectories can be visualized in [kepler.gl](https://kepler.gl). Below is a screenshot of the visualization output.

<p align="center">
  <img src="img/elephants-on-raster.png" alt="Elephant agents in Addo Elephant National Park"/>
</p>
