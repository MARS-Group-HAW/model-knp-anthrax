using System;
using System.Collections.Generic;
using System.IO;
using Mars.Common;
using Mars.Common.Core.Collections;
using Mars.Common.Core.Random;
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
    #region Properties and Fields

    /// <summary>
    /// Mapping of provided shape file regions to our two land-type categories.
    /// </summary>
    private new static readonly Dictionary<int, LandscapeType> Mapping = new()
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

    #endregion

    #region Initialization

    /// <summary>
    ///     Initialization of the layer type.
    /// </summary>
    /// <param name="layerInitData">The initialization data provided by the simulation configuration</param>
    /// <param name="registerAgentHandle">The agent registration handle of the layer type</param>
    /// <param name="unregisterAgent">The agent un-registration handle of the layer type</param>
    /// <returns>A boolean stating if initialization of the layer types base class was successful</returns>
    public override bool InitLayer(
        LayerInitData layerInitData,
        RegisterAgent registerAgentHandle = null,
        UnregisterAgent unregisterAgent = null)
    {
        var baseInitSuccessful = base.InitLayer(layerInitData, registerAgentHandle, unregisterAgent);

        // Save a GeoJSON with the current mapping of landscape types
        var featureCollection = new FeatureCollection();
        foreach (var feature in Features)
        {
            //var attrs = f.VectorStructured.Attributes;
            //attrs.Add("ModelLandType", GetTypeForFeature(f).ToString());
            
            var attributes = new AttributesTable
            {
                { "LABEL", feature.VectorStructured.Attributes["LABEL"] },
                { "ModelLandType", GetTypeForFeature(feature).ToString() },
                { "LSCAP_ID", feature.VectorStructured.Attributes["LSCAP_ID"] }
            };

            featureCollection.Add(new Feature(feature.VectorStructured.Geometry, attributes));
        }
        var featureCollectionAsGeoJson = new GeoJsonWriter().Write(featureCollection);
        File.WriteAllText("LandscapeLayer_types.geojson", featureCollectionAsGeoJson);

        return baseInitSuccessful;
    }

    #endregion

    #region Methods
    
    /// <summary>
    ///     Returns the landscape type for the given feature based on the ID of the feature.
    /// </summary>
    /// <param name="feature">The given feature</param>
    /// <returns>The landscape type of the given feature</returns>
    /// <exception cref="ApplicationException">Thrown if the given feature is not within the mapping</exception>
    private static LandscapeType GetTypeForFeature(IVectorFeature feature)
    {
        var featureId = Convert.ToInt32(feature.VectorStructured.Attributes["LSCAP_ID"]);

        if (Mapping.ContainsKey(featureId))
        {
            return Mapping[featureId];
        }

        throw new ApplicationException($"No Landscape Mapping found for LSCAP_ID = {featureId}");
    }

    /// <summary>
    ///     Gets the landscape type of the feature that contains the given position.
    /// </summary>
    /// <param name="p">The given position</param>
    /// <returns>The identified landscape type</returns>
    public LandscapeType GetTypeForPosition(Position p)
    {
        try
        {
            var f = FeatureOnPosition(p);
            return GetTypeForFeature(f);
        }
        catch (ArgumentException e)
        {
            //Console.WriteLine($"Tried moving to position with no land type coverage {p}");
            return LandscapeType.Unknown;
        }
    }
    
    /// <summary>
    ///     Finds the feature that is nearest to the given position and of the given landscape type.
    /// </summary>
    /// <param name="position">The given position</param>
    /// <param name="landscapeType">The given landscape type</param>
    /// <returns>The identified feature</returns>
    public IVectorFeature FindNearestLandAreaOfType(Position position, LandscapeType landscapeType)
    {
        var positionAsPoint = new Point(position.X, position.Y);
        var distanceToNearestFeature = double.MaxValue;
        IVectorFeature nearestFeature = new VectorFeature();
        
        foreach (var feature in Features)
        {
            if (landscapeType == GetTypeForFeature(feature))
            {
                var distanceToFeature = feature.VectorStructured.Geometry.Distance(positionAsPoint);

                if (distanceToFeature < distanceToNearestFeature)
                {
                    nearestFeature = feature;
                    distanceToNearestFeature = distanceToFeature;
                }
            }
        }

        return nearestFeature;
    }
    
    public bool IsTargetPositionOfSameCategory(Position currentPosition, Position targetPosition)
    {
        // determine the current feature we are on!
        try
        {
            var currentFeature = FeatureOnPosition(currentPosition);
            var targetFeature = FeatureOnPosition(targetPosition);

            return GetTypeForFeature(currentFeature).Equals(GetTypeForFeature(targetFeature));
        }
        catch (ArgumentException e)
        {
            return false;
        }

        // todo:
        // 1. find "our" land category
        // 2. find all connected areas with the same category originating on our current position
        // 3. build union of these geometries
        // 4. is target position inside union!
    }
    
    /// <summary>
    ///     Returns the feature that contains the given position.
    /// </summary>
    /// <param name="position">The given position</param>
    /// <returns>The identified feature</returns>
    /// <exception cref="ArgumentException">Thrown if the given position is not within any of the features</exception>
    private IVectorFeature FeatureOnPosition(Position position)
    {
        var positionAsPoint = new Point(position.X, position.Y);
        foreach (var feature in Features)
        {
            // feature.VectorStructured.Geometry.Contains(positionAsPoint)) fails if positionAsPoint is exactly ON the
            // boundary of feature! Use Covers(positionAsPoint) instead.
            // See https://nettopologysuite.github.io/NetTopologySuite/api/NetTopologySuite.Geometries.Geometry.html#NetTopologySuite_Geometries_Geometry_Contains_NetTopologySuite_Geometries_Geometry_
            if (feature.VectorStructured.Geometry.Covers(positionAsPoint))
            {
                return feature;
            }
        }
        //Console.WriteLine($"Position {p} is not covered by the provided Landscape Areas");
        throw new ArgumentException($"Position {position} is not covered by the provided Landscape Areas");
    }

    /// <summary>
    ///     Gets a random position within a feature that is classified as one of the given landscape types.
    /// </summary>
    /// <param name="landscapeTypes">The given landscape types</param>
    /// <returns>An identified position</returns>
    /// <exception cref="ArgumentException">Thrown if there exist none of the given landscape types</exception>
    public Position GetRandomPositionForLandscapeType(List<LandscapeType> landscapeTypes)
    {
        // Shuffle all available features to get randomness
        var shuffledFeatures = Features.ShuffleEnumerable(RandomHelper.Random);

        foreach (var feature in shuffledFeatures)
        {
            var featureType = GetTypeForFeature(feature);
            if (landscapeTypes.Contains(featureType))
            {
                return feature.VectorStructured.Geometry.RandomPositionFromGeometry();
            }
        }
        
        throw new ArgumentException($"No shapes are available for the given types {landscapeTypes}");
    }
    
    #endregion
}