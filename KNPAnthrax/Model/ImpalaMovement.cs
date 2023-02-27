using System.IO;
using Mars.Components.Layers;
using Mars.Interfaces.Annotations;
using Mars.Interfaces.Layers;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace KNPAnthrax.Model;

public class ImpalaMovement : RasterLayer, ISteppedActiveLayer
{

    #region Properties and Fields

    /// <summary>
    ///     The perimeter of the simulation environment.
    /// </summary>
    [PropertyDescription(Name = "Perimeter")]
    public Perimeter Fence { get; set; }

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
        if (GetCurrentTick() == Context.MaxTicks)
        {
            WriteMovementHeatMapToGeoJson();
        }
    }
    
    #endregion

    #region Methods
    
    /// <summary>
    /// 
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