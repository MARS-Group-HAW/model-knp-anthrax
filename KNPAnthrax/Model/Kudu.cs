using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mars.Common;
using Mars.Common.Core.Random;
using Mars.Components.Layers;
using Mars.Interfaces.Agents;
using Mars.Interfaces.Annotations;
using Mars.Interfaces.Environments;
using Mars.Interfaces.Layers;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Position = Mars.Interfaces.Environments.Position;

namespace KNPAnthrax.Model;


public class Kudu : IAgent<AnimalLayer>, IPositionable
{
    
    /// <summary>
    ///     The latitude of the current geo-referenced position of the agent
    /// </summary>
    [PropertyDescription(Name = "Latitude")]
    public double Latitude { get; set; }

    /// <summary>
    ///     The longitude of the current geo-referenced position of the agent
    /// </summary>
    [PropertyDescription(Name = "Longitude")]
    public double Longitude { get; set; }
    
    /// <summary>
    ///     Energy level of Agent
    /// </summary>
    [PropertyDescription(Name = "Energy")]
    public double Energy { get; set; }
    
    /// <summary>
    ///     Energy level of Agent
    /// </summary>
    [PropertyDescription(Name = "State")]
    public AnimalState State { get; set; }

    
    /// <summary>
    ///     The layer on which these agents live
    /// </summary>
    private AnimalLayer Layer { get; set; }
    
    /// <summary>
    ///     The perimeter of the simulation environment
    /// </summary>
    [PropertyDescription(Name = "LandscapeLayer")]
    public LandscapeLayer LandscapeLayer { get; set; }
    
    [PropertyDescription(Name = "WaterLayer")]
    public WaterLayer WaterLayer { get; set; }
    
    [PropertyDescription(Name = "AnthraxLayer")]
    public AnthraxLayer AnthraxLayer { get; set; }
    
    /// <summary>
    ///     The perimeter of the simulation environment
    /// </summary>
    [PropertyDescription(Name = "Perimeter")]
    public Perimeter Perimeter { get; set; }

    private List<LandscapeType> _preferredLandTypes = new() { LandscapeType.Woody };
    
    
    public void Init(AnimalLayer layer)
    {
        Layer = layer;
        Energy = RandomHelper.NextDouble(RandomHelper.Random, 50, 100);
        State = AnimalState.RandomMove;
        
        Position = Position.CreateGeoPosition(Longitude, Latitude);
        Console.WriteLine($"I'm a kudu @ {Position}!");
    }


    private List<Position> _positions = new List<Position>();
    
    public void Tick()
    {
        // Movement
        if (State == AnimalState.RandomMove)
        {
            var moved = false;
            do
            {
                var bearing = RandomHelper.NextDouble(RandomHelper.Random, 0, 360);
                var distance = RandomHelper.NextDouble(RandomHelper.Random, 750, 1250); // todo: konfigurarierbar?
                var target = Position.CalculateRelativePosition(bearing, distance);
                
                // in case we are on the wrong land type! Quickly move to the nearest comfortable area.
                // this can happen after visiting an water source, or after initialization.
                if (!_preferredLandTypes.Contains(LandscapeLayer.GetTypeForPosition(Position)))
                {
                    var nearestPreferredFeature = LandscapeLayer.FindNearestLandAreaOfType(Position, _preferredLandTypes.First());  // TODO replace First() call with some land type decision logic
                    var posInPreferredArea =nearestPreferredFeature.VectorStructured.Geometry.RandomPositionFromGeometry();
                    bearing = Position.GetBearing(posInPreferredArea);
                }

                // is target still in area of KNP?
                if (!Perimeter.IsPointInside(target))
                {
                    //Console.WriteLine("outside perimeter");
                    continue;
                }

                // is target of same category as our current position?
                if (!LandscapeLayer.IsTargetPositionOfSameCategory(Position, target))
                {
                    //Console.WriteLine("wrong cateogry");
                    continue;
                }

                Position = Layer.KuduEnvironment.MoveTowards(this, bearing, distance);
                moved = true;
            } while (!moved);
            
            if (Energy < 15)
            {
                State = AnimalState.SearchForWater;
            }
            
        } else if (State == AnimalState.SearchForWater)
        {
            // Energy is low, so look for water
            var waterSources = WaterLayer.Explore(Position.PositionArray, 50000).ToList();
            if (waterSources.Any())
            {
                // Explore() is not sorted by distance, so we need to sort them first.
                IVectorFeature nearestWaterSource = new VectorFeature();
                var nearestWaterDistance = Double.MaxValue;
                foreach (var w in waterSources)
                {
                    var loc = (Point) w.VectorStructured.Geometry;
                    var t = new Position(loc.X, loc.Y);
                    var d = t.DistanceInMTo(Position);

                    if (d < nearestWaterDistance)
                    {
                        nearestWaterDistance = d;
                        nearestWaterSource = w;
                    }
                }
                
                // Get coordinates of the nearest water source...
                //var nearestWaterSource = waterSources.First();
                var waterSourceLocation = (Point) nearestWaterSource.VectorStructured.Geometry;
                var target = new Position(waterSourceLocation.X, waterSourceLocation.Y);
                
                // Math.Min() of random move distance and distance to water source, so we can reach the sight!
                var distance = Math.Min(target.DistanceInMTo(Position), RandomHelper.NextDouble(RandomHelper.Random, 750, 1250)); // todo: konfigurarierbar?
                
                // ... and change the agent's bearing such that it looks in the direction of the water source
                var bearing = Position.GetBearing(target);
                Position = Layer.KuduEnvironment.MoveTowards(this, bearing, distance);

                
                // If the agent in close the the water source, increase its energy and change bearing
                if (target.DistanceInMTo(Position) < 20)
                {
                    State = AnimalState.Drinking;
                }
            }
            else
            {
                Console.WriteLine("No water in area");
            }
        }
        else if (State == AnimalState.Drinking)
        {
            Energy += 50;
            if (Energy > 101)
            {
                State = AnimalState.RandomMove;
            }
        }
        
        // Energy drops each tick
        Energy -= 1;
        _positions.Add(Position.Copy());
        
        // Infection
        if (AnthraxLayer.GetValue(Position) > 0)
        {  
            // todo: Anthrax logic could go here…
            Console.WriteLine($"This Kudu is on an anthrax site @ {Position} -> Anthrax Case Count: {AnthraxLayer.GetValue(Position)} ({Layer.Context.CurrentTick})");
        }


        // On last tick export movement of this agent as GeoJSON LineString
        if (Layer.GetCurrentTick() == Layer.Context.MaxTicks)
        {
            var featureCollection = new FeatureCollection();
            List<Coordinate> coors = new List<Coordinate>();

            foreach (var p in _positions)
            {
                coors.Add(new Coordinate(p.X, p.Y));
            }
            
            var ls = new LineString(coors.ToArray());
            featureCollection.Add(new Feature(ls, new AttributesTable()));
            var write = new GeoJsonWriter().Write(featureCollection);
            File.WriteAllText($"Kudu_path_{ID}.geojson", write);
        }
    }

    public Guid ID { get; set; }
    public Position Position { get; set; }
}