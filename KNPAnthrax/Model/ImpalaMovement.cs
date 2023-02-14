using System;
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
    [PropertyDescription(Name = "Perimeter")]
    public Perimeter Fence { get; set; }

        /// <summary>
    /// 
    /// </summary>
    public void ToGeoJSON()
    {
        var featureCollection = new FeatureCollection();
        var gf = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(4326);

        for (var x = 0; x < Width; x++)
        {
            for (var y = 0; y < Height; y++)
            {
                var value = this[x, y];
                

                
                if (value == 0)
                {
                    continue;
                }
                
                Console.WriteLine(value);

                
                // p4      p3
                // + ---- +
                // |      |
                // |      |
                // + ---- + 
                // p1      p2
                var p = gf.CreatePolygon(new[] {
                    new Coordinate(LowerLeft.X + CellWidth * x, LowerLeft.Y + CellHeight * y), // p1
                    new Coordinate(LowerLeft.X + CellWidth * x + CellWidth, LowerLeft.Y + CellHeight * y), // p2
                    new Coordinate(LowerLeft.X + CellWidth * x + CellWidth, LowerLeft.Y + CellHeight * y + CellHeight), // p3
                    new Coordinate( LowerLeft.X + CellWidth * x, LowerLeft.Y + CellHeight * y + CellHeight), // p4
                    new Coordinate(LowerLeft.X + CellWidth * x, LowerLeft.Y + CellHeight * y), // p1
                });
                var at = new AttributesTable();
                at.Add("density", value);
                featureCollection.Add(new Feature(p, at));
            }
        }
        
        var write = new GeoJsonWriter().Write(featureCollection);
        File.WriteAllText($"{this.GetType().Name}.geojson", write);
    }

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
            ToGeoJSON();
        }
    }
}