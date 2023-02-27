using System;
using System.Linq;
using Mars.Components.Layers;
using Mars.Interfaces.Layers;
using Point = NetTopologySuite.Geometries.Point;
using Position = Mars.Interfaces.Environments.Position;

namespace KNPAnthrax.Model;

/// <summary>
///     The WaterLayer extends the VectorLayer. This enable the WaterLayer to hold VectorFeatures. The provided
///     VectorFeatures represent water spots in the KNP to which agents can move.
/// </summary>
public class WaterLayer : VectorLayer
{

    #region Methods

    /// <summary>
    ///     Gets the distance in meters to the water source that is nearest to the given position.
    /// </summary>
    /// <param name="p">The given position</param>
    /// <returns>The distance in meters to the nearest water source</returns>
    public double GetDistanceToNearestWaterSource(Position p)
    {
        var positionAsPoint = new Point(p.X, p.Y);
        var distanceToNearestWaterSource = double.MaxValue;
        foreach (var feature in Features)
        {
            var distanceToFeature = feature.VectorStructured.Geometry.Distance(positionAsPoint);
            
            if (distanceToFeature < distanceToNearestWaterSource)
            {
                distanceToNearestWaterSource = distanceToFeature;
            }
        }
        return distanceToNearestWaterSource;
    }
    
    /// <summary>
    ///     Gets the water source that is nearest to the given position.
    /// </summary>
    /// <param name="p">The given position</param>
    /// <returns>The nearest water source</returns>
    public IVectorFeature GetNearestWaterSource(Position p)
    {
        //Console.WriteLine($"total: {Features.Count}");
        var waterSources = Explore(p.PositionArray, 50000).ToList();
        //Console.WriteLine($"explored: {waterSources.Count}");
        
        if (!waterSources.Any())
        {
            Console.WriteLine("no waters source available.");
            throw new ArgumentException("no waters source available.");
        }
        
        var positionAsPoint = new Point(p.X, p.Y);
        var nearestWaterSource = new VectorFeature();
        var distanceToNearestWaterSource = double.MaxValue;
        foreach (var feature in waterSources)
        {
            var distanceToFeature = feature.VectorStructured.Geometry.Distance(positionAsPoint);
            
            if (distanceToFeature < distanceToNearestWaterSource)
            {
                distanceToNearestWaterSource = distanceToFeature;
                nearestWaterSource = feature;
            }
        }
        return nearestWaterSource;
    }

    #endregion

}
