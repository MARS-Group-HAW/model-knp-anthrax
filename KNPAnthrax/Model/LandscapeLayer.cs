using System;
using System.Collections.Generic;
using System.Linq;
using Mars.Common;
using Mars.Components.Environments;
using Mars.Components.Layers;
using Mars.Core.Data;
using Mars.Interfaces.Annotations;
using Mars.Interfaces.Data;
using Mars.Interfaces.Environments;
using Mars.Interfaces.Layers;
using NetTopologySuite.Geometries;
using ServiceStack;
using Position = Mars.Interfaces.Environments.Position;

namespace KNPAnthrax.Model;

public class LandscapeLayer : VectorLayer
{

    public bool IsTargetPositionOfSameCategory(Position currentPosition, Position targetPosition)
    {
        // determine the current feature we are on!
        var currentFeature = FeatureOnPosition(currentPosition);
        var targetFeature = FeatureOnPosition(targetPosition);

        // todo: LSCAP_ID != our land type category defined by sunday!
        return currentFeature.VectorStructured.Attributes["LSCAP_ID"]
            .Equals(targetFeature.VectorStructured.Attributes["LSCAP_ID"]);

        // todo:
        // 1. find "our" land category
        // 2. find all connected areas with the same category originating on our current position
        // 3. build union of these geometries
        // 4. is target position inside union!
    }

    public Position FindNewGoalInSameAreaType(Position currentPosition)
    {
        // determine the current feature we are on!
        var currentFeature = FeatureOnPosition(currentPosition);
        
        // todo:
        // 1. find "our" land category
        // 2. find all connected areas with the same category originating on our current position
        // 3. build union of these geometries
        // 4. find random position on this 

        return currentFeature.VectorStructured.Geometry.RandomPositionFromGeometry();
    }
    
    
    public IVectorFeature FeatureOnPosition(Position p)
    {
        var g = new Point(p.X, p.Y);
        foreach (var f in Features)
        {
            if (f.VectorStructured.Geometry.Contains(g))
            {
                return f;
                //return Convert.ToInt32(f.VectorStructured.Attributes["LSCAP_ID"]);
            }
        }

        throw new ArgumentException($"Position {p} is not covered by the provided Landscape Area");
    }
    
    /// <summary>
    ///     Obtains a random POI that is of the given category (e.g., "restaurant").
    /// </summary>
    /// <param name="category">The given category</param>
    /// <returns>A POI of the given category, if any exist</returns>
    /// <exception cref="ArgumentException">Thrown if no POI of the given category exists</exception>
    public IVectorFeature GetRandomPoiForCategory(string category)
    {
        // Shuffle all available features randomly so each POI has a chance of being selected.
        var shuffledFeatures = Features.OrderBy(_ => new Random().Next()).ToList();

        foreach (var feature in shuffledFeatures)
        {
            if ((string)feature.VectorStructured.Attributes["fclass"] == category)
            {
                return feature;
            }
        }

        // If we reach this code, no POI with this category is available, so abort the simulation.
        throw new ArgumentException($"No POIs found with category '{category}");
    }
    
}