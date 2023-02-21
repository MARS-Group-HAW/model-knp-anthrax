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
    public bool StoreTickResult { get; set; }
    
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
    
    [PropertyDescription(Name = "SpawnMinEnergy")]
    public double SpawnMinEnergy { get; set; }
    
    [PropertyDescription(Name = "SpawnMaxEnergy")]
    public double SpawnMaxEnergy { get; set; }
    
    /// <summary>
    ///     
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
    ///    
    /// </summary>
    [PropertyDescription(Name = "AnthraxInfectionProbability")]
    public double AnthraxInfectionProbability { get; set; }

    /// <summary>
    ///    
    /// </summary>
    [PropertyDescription(Name = "MinInfectionDurationInTicks")]
    public int MinInfectionDurationInTicks { get; set; }

    /// <summary>
    ///    
    /// </summary>
    [PropertyDescription(Name = "MaxInfectionDurationInTicks")]
    public int MaxInfectionDurationInTicks { get; set; }
    
    [PropertyDescription(Name = "OutputAgentTrack")]
    public bool OutputAgentTrack { get; set; }
    
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
    public int InfectedCounter { get; set; }
    private List<long> _LeaveAnthraxTraceTicks = new();
    
    public int InfectedTotalCounter { get; set; }

    
    public void Init(AnimalLayer layer)
    {
        InfectedCounter = 0;
        InfectedTotalCounter = 0;
        Layer = layer;
        Energy = RandomHelper.NextDouble(RandomHelper.Random, SpawnMinEnergy, SpawnMaxEnergy);
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
        StoreTickResult = false;
        if (Layer.Context.CurrentTick == Layer.Context.MaxTicks || Layer.Context.CurrentTick == 1)
        {
            // in the first/last sim tick store the agent data regardless of anthrax infections so, we have 
            // the full set of agents in the output.
            StoreTickResult = true;
        }
        
        // Movement
        if (State == AnimalState.RandomMove)
        {
            var moved = false;
            do
            {
                var bearing = RandomHelper.NextDouble(RandomHelper.Random, 0, 360);
                var distance = GetDistance();
                var target = Position.CalculateRelativePosition(bearing, distance);
                
                // is target still in area of KNP?
                if (!Perimeter.IsPointInside(target))
                {
                    continue;
                }
                
                // in case we are on the wrong land type! Quickly move to the nearest comfortable area.
                // this can happen after visiting an water source, or after initialization.
                if (!_preferredLandTypes.Contains(LandscapeLayer.GetTypeForPosition(Position)))
                {
                    var nearestPreferredFeature = LandscapeLayer.FindNearestLandAreaOfType(Position, _preferredLandTypes.First());  // TODO replace First() call with some land type decision logic
                    var posInPreferredArea =nearestPreferredFeature.VectorStructured.Geometry.RandomPositionFromGeometry();
                    bearing = Position.GetBearing(posInPreferredArea);
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
            var beta = AnthraxLayer.GetValue(Position) * AnthraxInfectionProbability;

            if (RandomHelper.SmallerThan(beta))
            {
                StoreTickResult = true;
                InfectedCounter += 1;
                InfectedTotalCounter += 1;
                
                var DeathOccuresInTicks = RandomHelper.NextInteger(RandomHelper.Random, MinInfectionDurationInTicks,
                    MaxInfectionDurationInTicks + 1);
                var x = Layer.Context.CurrentTick + DeathOccuresInTicks;

                _LeaveAnthraxTraceTicks.Add(x);
            }
        }

        // Leave Anthrax trail on the Map / animal dies
        if (InfectedCounter > 0)
        {
            if (_LeaveAnthraxTraceTicks.Contains(Layer.Context.CurrentTick))
            {
                StoreTickResult = true;
                InfectedCounter -= 1;
                _LeaveAnthraxTraceTicks.Remove(Layer.Context.CurrentTick);

                if (AnthraxLayer.IsInRaster(Position))
                {
                    AnthraxLayer[Position] += 1;
                }
            }
        }

        // Leave Movement trace for heatmap
        if (KuduMovement.IsInRaster(Position))
        {
            KuduMovement[Position] += 0.1;
        }

        // On last tick export movement of this agent as GeoJSON LineString
        if (OutputAgentTrack && Layer.GetCurrentTick() == Layer.Context.MaxTicks)
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