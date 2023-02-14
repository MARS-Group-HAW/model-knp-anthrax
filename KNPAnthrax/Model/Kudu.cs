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
using NetTopologySuite.Operation.Distance;
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
    ///   
    /// </summary>
    [PropertyDescription(Name = "SpawnWoodlandProbability")]
    public double SpawnWoodlandProbability { get; set; }

    /// <summary>
    ///    
    /// </summary>
    [PropertyDescription(Name = "SpawnSavannaProbability")]
    public double SpawnSavannaProbability { get; set; }
    
    /// <summary>
    ///   
    /// </summary>
    [PropertyDescription(Name = "MinMovementPerTickInM")]
    public double MinMovementPerTickInM { get; set; }

    /// <summary>
    ///    
    /// </summary>
    [PropertyDescription(Name = "MaxMovementPerTickInM")]
    public double MaxMovementPerTickInM { get; set; }
    
    /// <summary>
    ///   
    /// </summary>
    [PropertyDescription(Name = "MovementOnWoodland")]
    public double MovementOnWoodland { get; set; }

    /// <summary>
    ///    
    /// </summary>
    [PropertyDescription(Name = "MovementOnSavanna")]
    public double MovementOnSavanna { get; set; }
    
    /// <summary>
    ///    
    /// </summary>
    [PropertyDescription(Name = "MaxDistanceFromWaterInM")]
    public double MaxDistanceFromWaterInM { get; set; }
    
    
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

    [PropertyDescription(Name = "KuduMovement")]
    public KuduMovement KuduMovement { get; set; }
    
    private List<LandscapeType> _preferredLandTypes = new() { LandscapeType.Woodland };
    
    
    
    
    public void Init(AnimalLayer layer)
    {
        Layer = layer;
        Energy = RandomHelper.NextDouble(RandomHelper.Random, 50, 100);
        State = AnimalState.RandomMove;

        if ((SpawnWoodlandProbability + SpawnSavannaProbability) != 1.0)
        {
            throw new ArgumentException("Spawning probabilities must add up to 100.");
        }

        // no position set from kudu.csv -> choose random position according to land-type spawn probability
        if (Latitude == 0.0 || Longitude == 0.0)
        {
            if (RandomHelper.SmallerThan(SpawnWoodlandProbability))
            {
                // spawn on Woodland
                Position = LandscapeLayer.GetRandomPositionForLandscapeType(new List<LandscapeType>() {LandscapeType.Woodland});
            }
            else
            {
                // Spawn on Savanna   
                Position = LandscapeLayer.GetRandomPositionForLandscapeType(new List<LandscapeType>() {LandscapeType.Savanna});
            }
        }
        else
        {
            // position defined in csv file
            Position = Position.CreateGeoPosition(Longitude, Latitude);
        }

        //Console.WriteLine($"I'm a kudu @ {Position}!");
    }


    /// <summary>
    /// Generates a random movement distance for one tick.
    /// </summary>
    /// <returns></returns>
    private double GetDistance()
    {
        return RandomHelper.NextDouble(RandomHelper.Random, MinMovementPerTickInM, MaxMovementPerTickInM);
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
                var distance = GetDistance();
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
                var targetType = LandscapeLayer.GetTypeForPosition(target);
                if (targetType == LandscapeType.Woodland)
                {
                    // every thing is fine.
                } else if (targetType == LandscapeType.Savanna)
                {
                    if (RandomHelper.SmallerThan(MovementOnSavanna))
                    {
                        // it's our low probability we allow the animal on the savanna area
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    // unknown land type, try again!
                    continue;
                }

                // if the agent is to far from a water source walk towards it!
                var nearestWaterSource = WaterLayer.GetNearestWaterSource(Position);
                var g = new Point(Position.X, Position.Y);
                //var d = nearestWaterSource.VectorStructured.Geometry.Distance(g);
                var ps = DistanceOp.NearestPoints(g, nearestWaterSource.VectorStructured.Geometry);
                var wPos = new Position(ps[1].X, ps[1].Y);
                var d = GeoPositionExtension.DistanceInMTo(Position, wPos);
                
                if (d > MaxDistanceFromWaterInM)
                {
                    bearing = Position.GetBearing(wPos);
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
                    var myPositionAsPoint = new Point(Position.X, Position.Y);
                    var d = w.VectorStructured.Geometry.Distance(myPositionAsPoint);
                    
                    //var loc = (Point) w.VectorStructured.Geometry;
                    //var t = new Position(loc.X, loc.Y);
                    //var d = t.DistanceInMTo(Position);

                    if (d < nearestWaterDistance)
                    {
                        nearestWaterDistance = d;
                        nearestWaterSource = w;
                    }
                }
                
                // Get coordinates of the nearest water source...
                //var nearestWaterSource = waterSources.First();
                // @todo: RandomPositionFromGeometry() is needed since die direct way for LINESTRINGs would be the 
                // requires the nearest POINT on the target geoemtry, but we don't have that from the API. So we just
                // use a random position somewhere on the linestring, so at least we go in the right direction...
                var waterSourceLocation = nearestWaterSource.VectorStructured.Geometry.RandomPositionFromGeometry();
                var target = new Position(waterSourceLocation.X, waterSourceLocation.Y);
                
                // Math.Min() of random move distance and distance to water source, so we can reach the sight!
                var distance = Math.Min(target.DistanceInMTo(Position), GetDistance());
                
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
        
        if (AnthraxLayer.IsInRaster(Position) && AnthraxLayer.GetValue(Position) > 0)
        {  
            // todo: Anthrax logic could go here…
            //Console.WriteLine($"This Kudu is on an anthrax site @ {Position} -> Anthrax Case Count: {AnthraxLayer.GetValue(Position)} ({Layer.Context.CurrentTick})");
        }
        
        // Leave Movement trace for heatmap
        if (KuduMovement.IsInRaster(Position))
        {
            KuduMovement[Position] += 0.1;
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