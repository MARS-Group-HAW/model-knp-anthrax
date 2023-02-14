using System;
using System.Drawing;
using System.Linq;
using Mars.Components.Layers;
using Mars.Interfaces.Layers;
using NetTopologySuite.Geometries;
using Point = NetTopologySuite.Geometries.Point;
using Position = Mars.Interfaces.Environments.Position;

namespace KNPAnthrax.Model;

/// <summary>
///     The WaterLayer extends the VectorLayer. This enable the WaterLayer to hold VectorFeatures. The provided
///     VectorFeatures represent water spots in the Addo Elephant National Park to which Elephant agents can move.
/// </summary>
public class WaterLayer : VectorLayer
{

    public double GetDistanceToNearestWaterSource(Position p)
    {
        var g = new Point(p.X, p.Y);
        var nearestWaterDistance = Double.MaxValue;
        foreach (var f in Features)
        {
            var d = f.VectorStructured.Geometry.Distance(g);
            
            if (d < nearestWaterDistance)
            {
                nearestWaterDistance = d;
            }
        }

        return nearestWaterDistance;
    }

    public IVectorFeature GetNearestWaterSource(Position p)
    {
        //Console.WriteLine($"total: {Features.Count}");
        var waterSources = Explore(p.PositionArray, 50000).ToList();
        //Console.WriteLine($"explored: {waterSources.Count}");
        if (waterSources.Any())
        {
            var g = new Point(p.X, p.Y);

            IVectorFeature nearestWaterSource = new VectorFeature();
            var nearestWaterDistance = Double.MaxValue;
            foreach (var f in waterSources)
            {
                var d = f.VectorStructured.Geometry.Distance(g);
            
                if (d < nearestWaterDistance)
                {
                    nearestWaterDistance = d;
                    nearestWaterSource = f;
                }
            }

            return nearestWaterSource;
        }

        Console.WriteLine("no waters source available.");
        throw new ArgumentException("no waters source available.");
    }
    
}
