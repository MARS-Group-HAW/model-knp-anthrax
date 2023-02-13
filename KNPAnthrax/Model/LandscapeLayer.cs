using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mars.Components.Layers;
using Mars.Interfaces.Data;
using Mars.Interfaces.Layers;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Position = Mars.Interfaces.Environments.Position;

namespace KNPAnthrax.Model;

public class LandscapeLayer : VectorLayer
{
    /// <summary>
    /// Mapping of provided shape file regions to our two land-type categories.
    /// </summary>
    public new static readonly Dictionary<int, LandscapeType> Mapping = new()
    {
        {8,  LandscapeType.Woodland},
        {9,  LandscapeType.Savanna},
        {10, LandscapeType.Woodland},
        {11, LandscapeType.Woodland},
        {12, LandscapeType.Savanna},
        
        {15, LandscapeType.Savanna},
        {16, LandscapeType.Woodland},
        
        {20, LandscapeType.Woodland},
        {21, LandscapeType.Woodland},
        {22, LandscapeType.Woodland},
        {23, LandscapeType.Woodland},
        {24, LandscapeType.Woodland},
        {25, LandscapeType.Savanna},
        {26, LandscapeType.Savanna},
        {27, LandscapeType.Woodland},
        {28, LandscapeType.Savanna},
        
        {31, LandscapeType.Woodland},
        {32, LandscapeType.Woodland},
        {33, LandscapeType.Savanna},
        
        {35, LandscapeType.Savanna},
        
        // Types not in excel
        {7, LandscapeType.Unknown},
        {34, LandscapeType.Unknown},
    };

    public override bool InitLayer(
        LayerInitData layerInitData,
        RegisterAgent registerAgentHandle = null,
        UnregisterAgent unregisterAgent = null)
    {
        var parent =  base.InitLayer(layerInitData, registerAgentHandle, unregisterAgent);

        // Save a GeoJSON with the current mapping of landscape types
        var featureCollection = new FeatureCollection();
        foreach (var f in Features)
        {
            //var attrs = f.VectorStructured.Attributes;
            //attrs.Add("ModelLandType", GetTypeForFeature(f).ToString());
            
            var attrs = new AttributesTable();
            attrs.Add("LABEL", f.VectorStructured.Attributes["LABEL"]);
            attrs.Add("ModelLandType", GetTypeForFeature(f).ToString());
            attrs.Add("LSCAP_ID", f.VectorStructured.Attributes["LSCAP_ID"]);

            featureCollection.Add(new Feature(f.VectorStructured.Geometry, attrs));
        }
        var write = new GeoJsonWriter().Write(featureCollection);
        File.WriteAllText("LandscapeLayer_types.geojson", write);

        return parent;
    }

    public LandscapeType GetTypeForFeature(IVectorFeature f)
    {
        var id = Convert.ToInt32(f.VectorStructured.Attributes["LSCAP_ID"]);

        if (Mapping.ContainsKey(id))
        {
            return Mapping[id];
        }

        throw new ApplicationException($"No Landscape Mapping found for LSCAP_ID = {id}");
    }

    public LandscapeType GetTypeForPosition(Position p)
    {
        var f = FeatureOnPosition(p);
        return GetTypeForFeature(f);
    }
    
    public IVectorFeature FindNearestLandAreaOfType(Position p, LandscapeType type)
    {
        var g = new Point(p.X, p.Y);

        var minDistanceToFeature = Double.MaxValue;
        IVectorFeature nearestFeature = new VectorFeature();
        foreach (var vf in Features)
        {
            if (type == GetTypeForFeature(vf))
            {
                var d = vf.VectorStructured.Geometry.Distance(g);

                if (d < minDistanceToFeature)
                {
                    nearestFeature = vf;
                    minDistanceToFeature = d;
                }
            }
        }

        return nearestFeature;
    }
    
    public bool IsTargetPositionOfSameCategory(Position currentPosition, Position targetPosition)
    {
        // determine the current feature we are on!
        var currentFeature = FeatureOnPosition(currentPosition);
        var targetFeature  = FeatureOnPosition(targetPosition);

        return GetTypeForFeature(currentFeature)
            .Equals(GetTypeForFeature(targetFeature));

        // todo:
        // 1. find "our" land category
        // 2. find all connected areas with the same category originating on our current position
        // 3. build union of these geometries
        // 4. is target position inside union!
    }
    
    public IVectorFeature FeatureOnPosition(Position p)
    {
        var g = new Point(p.X, p.Y);
        foreach (var f in Features)
        {
            if (f.VectorStructured.Geometry.Contains(g))
            {
                return f;
            }
        }

        throw new ArgumentException($"Position {p} is not covered by the provided Landscape Areas");
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