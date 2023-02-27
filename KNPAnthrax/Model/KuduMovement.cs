using System;
using System.IO;
using Mars.Components.Layers;
using Mars.Interfaces.Annotations;
using Mars.Interfaces.Data;
using Mars.Interfaces.Layers;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace KNPAnthrax.Model;

public class KuduMovement : RasterLayer, ISteppedActiveLayer
{
    #region Properties and Fields

    /// <summary>
    ///     The perimeter of the simulation environment.
    /// </summary>
    [PropertyDescription(Name = "Perimeter")]
    public Perimeter Fence { get; set; }

    #endregion

    #region Initialization

    /// <summary>
    ///     Initialization of the layer type.
    /// </summary>
    /// <param name="layerInitData">The initialization data provided by the simulation configuration</param>
    /// <param name="registerAgentHandle">The agent registration handle of the layer type</param>
    /// <param name="unregisterAgent">The agent un-registration handle of the layer type</param>
    /// <returns>A boolean stating if initialization of the layer types base class was successful</returns>
    public override bool InitLayer(LayerInitData layerInitData, RegisterAgent registerAgentHandle = null,
        UnregisterAgent unregisterAgent = null)
    {
        var baseInitSuccessful = base.InitLayer(layerInitData, registerAgentHandle, unregisterAgent);
        return baseInitSuccessful;
    }
    
    #endregion
    
    #region Tick
    
    public void Tick()
    {
    }

    public void PreTick()
    {
    }

    public void PostTick()
    {
        if (GetCurrentTick() % 1 == 0 ||  GetCurrentTick() == 1 || GetCurrentTick() == Context.MaxTicks)
        {
            Console.WriteLine($"{GetCurrentTick()}/{Context.MaxTicks}");
        }
        
        if (GetCurrentTick() == Context.MaxTicks)
        {
            WriteMovementHeatMapToGeoJson();
        }
    }
    
    #endregion

    #region Methods

    /// <summary>
    ///     Writes the movement heat map of Kudu agents to a GeoJSON file.
    /// </summary>
    private void WriteMovementHeatMapToGeoJson()
    {
        var featureCollection = new FeatureCollection();
        var geometryFactory = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(4326);

        for (var x = 0; x < Width; x++)
        {
            for (var y = 0; y < Height; y++)
            {
                var gridCellValue = this[x, y];
                
                if (gridCellValue == 0)
                {
                    continue;
                }
                
                // p4      p3
                // + ---- +
                // |      |
                // |      |
                // + ---- + 
                // p1      p2
                var polygon = geometryFactory.CreatePolygon(new[] {
                    new Coordinate(LowerLeft.X + CellWidth * x, LowerLeft.Y + CellHeight * y), // p1
                    new Coordinate(LowerLeft.X + CellWidth * x + CellWidth, LowerLeft.Y + CellHeight * y), // p2
                    new Coordinate(LowerLeft.X + CellWidth * x + CellWidth, LowerLeft.Y + CellHeight * y + CellHeight), // p3
                    new Coordinate( LowerLeft.X + CellWidth * x, LowerLeft.Y + CellHeight * y + CellHeight), // p4
                    new Coordinate(LowerLeft.X + CellWidth * x, LowerLeft.Y + CellHeight * y), // p1
                });
                var attributesTable = new AttributesTable { { "density", gridCellValue } };
                featureCollection.Add(new Feature(polygon, attributesTable));
            }
        }
        
        var featureCollectionAsGeoJson = new GeoJsonWriter().Write(featureCollection);
        File.WriteAllText($"{GetType().Name}.geojson", featureCollectionAsGeoJson);
    }
    
    #endregion
}