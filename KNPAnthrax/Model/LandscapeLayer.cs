using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mars.Common;
using Mars.Components.Environments;
using Mars.Components.Layers;
using Mars.Core.Data;
using Mars.Interfaces.Annotations;
using Mars.Interfaces.Data;
using Mars.Interfaces.Environments;
using Mars.Interfaces.Layers;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Position = Mars.Interfaces.Environments.Position;

namespace KNPAnthrax.Model;

public class LandscapeLayer : VectorLayer
{
    public new static Dictionary<int, LandscapeTypes> Mapping = new()
    {
        {8,  LandscapeTypes.Woody},
        {9,  LandscapeTypes.Plain},
        {10, LandscapeTypes.Woody},
        {11, LandscapeTypes.Woody},
        {12, LandscapeTypes.Plain}, // "open savanna"?!
        
        {15, LandscapeTypes.Plain}, // "high tree savanna"?!
        {16, LandscapeTypes.Woody},
        
        {20, LandscapeTypes.Plain}, // "shrub veld/woodland"?!
        {21, LandscapeTypes.Woody},
        
        {24, LandscapeTypes.Woody},
        {25, LandscapeTypes.Plain}, // open savanna
        {26, LandscapeTypes.Woody}, // Tree savanna
        {27, LandscapeTypes.Woody}, // open tree/ woody
        {28, LandscapeTypes.Woody}, // Open tree savanna
        
        {31, LandscapeTypes.Woody}, // Woody/ high tree
        
        {33, LandscapeTypes.Plain}, // shrub savanna/ few trees
        
        {35, LandscapeTypes.Woody}, // high tree savanna
        
        // Types not in excel
        {23, LandscapeTypes.Unknown},
        {34, LandscapeTypes.Unknown},
        {22, LandscapeTypes.Unknown},
        {32, LandscapeTypes.Unknown},
        {7, LandscapeTypes.Unknown},

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

    public LandscapeTypes GetTypeForFeature(IVectorFeature f)
    {
        var id = Convert.ToInt32(f.VectorStructured.Attributes["LSCAP_ID"]);

        if (Mapping.ContainsKey(id))
        {
            return Mapping[id];
        }

        throw new ApplicationException($"No Landscape Mapping found for LSCAP_ID = {id}");
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