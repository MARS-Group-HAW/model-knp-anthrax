using System;
using System.Collections.Generic;
using System.IO;
using Mars.Components.Environments;
using Mars.Components.Layers;
using Mars.Interfaces.Annotations;
using Mars.Interfaces.Data;
using Mars.Interfaces.Environments;
using Mars.Interfaces.Layers;
using Mars.Numerics.Distances;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Npgsql.Internal.TypeHandlers.GeometricHandlers;

namespace KNPAnthrax.Model;

public class KuduMovement : RasterLayer, ISteppedActiveLayer
{
    [PropertyDescription(Name = "Perimeter")]
    public Perimeter Fence { get; set; }
    
    public override bool InitLayer(LayerInitData layerInitData, RegisterAgent registerAgentHandle = null,
        UnregisterAgent unregisterAgent = null)
    {
        var init = base.InitLayer(layerInitData, registerAgentHandle, unregisterAgent);
        return init;
    }
    
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
        if (GetCurrentTick() % 1 == 0 ||  GetCurrentTick() == 1 || GetCurrentTick() == Context.MaxTicks)
        {
            Console.WriteLine($"{GetCurrentTick()}/{Context.MaxTicks}");
        }
        
        if (GetCurrentTick() == Context.MaxTicks)
        {
            ToGeoJSON();
        }
    }
}