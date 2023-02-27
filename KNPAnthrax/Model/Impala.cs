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
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NetTopologySuite.Operation.Distance;
using Position = Mars.Interfaces.Environments.Position;

namespace KNPAnthrax.Model;


public class Impala : IAgent<AnimalLayer>, IPositionable
{

    #region Properties and Fields

    /// <summary>
    ///     Enables the agent's state at the end of the current tick to be persisted to a CSV output file.
    /// </summary>
    public bool StoreTickResult { get; set; }
    
    /// <summary>
    ///     The latitude of the current geo-referenced position of the agent.
    /// </summary>
    [PropertyDescription(Name = "Latitude")]
    public double Latitude { get; set; }

    /// <summary>
    ///     The longitude of the current geo-referenced position of the agent.
    /// </summary>
    [PropertyDescription(Name = "Longitude")]
    public double Longitude { get; set; }
    
    /// <summary>
    ///     The current energy level of the agent.
    /// </summary>
    [PropertyDescription(Name = "Energy")]
    public double Energy { get; set; }
    
    /// <summary>
    ///     The minimum energy level of the agent upon being spawned.
    /// </summary>
    [PropertyDescription(Name = "SpawnMinEnergy")]
    public double SpawnMinEnergy { get; set; }
    
    /// <summary>
    ///     The maximum energy level of the agent upon being spawned.
    /// </summary>
    [PropertyDescription(Name = "SpawnMaxEnergy")]
    public double SpawnMaxEnergy { get; set; }
    
    /// <summary>
    ///     The agent's current state.
    /// </summary>
    [PropertyDescription(Name = "State")]
    public AnimalState State { get; set; }

    /// <summary>
    ///     The probability of being spawned on "woodland" land type.
    /// </summary>
    [PropertyDescription(Name = "SpawnWoodlandProbability")]
    public double SpawnWoodlandProbability { get; set; }

    /// <summary>
    ///     The probability of being spawned on "savanna" land type.
    /// </summary>
    [PropertyDescription(Name = "SpawnSavannaProbability")]
    public double SpawnSavannaProbability { get; set; }
    
    /// <summary>
    ///     The minimum movement distance in meters per tick.
    /// </summary>
    [PropertyDescription(Name = "MinMovementPerTickInM")]
    public double MinMovementPerTickInM { get; set; }

    /// <summary>
    ///     The maximum movement distance in meters per tick.
    /// </summary>
    [PropertyDescription(Name = "MaxMovementPerTickInM")]
    public double MaxMovementPerTickInM { get; set; }
    
    /// <summary>
    ///     The maximum distance in meters allowed between the agent and the nearest water source. 
    /// </summary>
    [PropertyDescription(Name = "MaxDistanceFromWaterInM")]
    public double MaxDistanceFromWaterInM { get; set; }
    
    /// <summary>
    ///     The probability of becoming infected with Anthrax when in a cell that contains Anthrax.
    /// </summary>
    [PropertyDescription(Name = "AnthraxInfectionProbability")]
    public double AnthraxInfectionProbability { get; set; }

    /// <summary>
    ///     The minimum number of ticks that an Anthrax infection lasts (from exposure to death).
    /// </summary>
    [PropertyDescription(Name = "MinInfectionDurationInTicks")]
    public int MinInfectionDurationInTicks { get; set; }

    /// <summary>
    ///     The maximum number of ticks that an Anthrax infection lasts (from exposure to death).
    /// </summary>
    [PropertyDescription(Name = "MaxInfectionDurationInTicks")]
    public int MaxInfectionDurationInTicks { get; set; }

    /// <summary>
    ///     Enables the agent's movement trajectory to be written to a GeoJSON file.
    /// </summary>
    [PropertyDescription(Name = "OutputAgentTrack")]
    public bool OutputAgentTrack { get; set; }
    
    /// <summary>
    ///     The layer on which the agent lives.
    /// </summary>
    private AnimalLayer Layer { get; set; }
    
    /// <summary>
    ///     The perimeter of the simulation environment.
    /// </summary>
    [PropertyDescription(Name = "LandscapeLayer")]
    public LandscapeLayer LandscapeLayer { get; set; }
    
    /// <summary>
    ///     A layer that holds water sources in the form of vector features.
    /// </summary>
    [PropertyDescription(Name = "WaterLayer")]
    public WaterLayer WaterLayer { get; set; }
    
    /// <summary>
    ///     A grid-based layer that holds Anthrax infection sites.
    /// </summary>
    [PropertyDescription(Name = "AnthraxLayer")]
    public AnthraxLayer AnthraxLayer { get; set; }
    
    /// <summary>
    ///     The perimeter of the simulation environment.
    /// </summary>
    [PropertyDescription(Name = "Perimeter")]
    public Perimeter Perimeter { get; set; }

    /// <summary>
    ///     A grid-based layer that is used to store movement locations of Impala agents.
    /// </summary>
    [PropertyDescription(Name = "ImpalaMovement")]
    public ImpalaMovement ImpalaMovement { get; set; }
    
    /// <summary>
    ///     A collection of land types that the agent prefer to be on.
    /// </summary>
    private readonly List<LandscapeType> _preferredLandTypes = new(){ LandscapeType.Woodland, LandscapeType.Savanna };
    
    /// <summary>
    ///     A counter to keep track of the number of infections this agent has had.
    /// </summary>
    public int InfectedCounter { get; set; }
    
    /// <summary>
    ///     A collection that tracks during which ticks the agent should leave Anthrax traces on the AnthraxLayer.
    /// </summary>
    private readonly List<long> _leaveAnthraxTraceTicks = new();
    
    /// <summary>
    ///     The total number of infections of the agent throughout the simulation.
    /// </summary>
    public int InfectedTotalCounter { get; set; }

    /// <summary>
    ///     A unique identifier of the agent.
    /// </summary>
    public Guid ID { get; set; }
    
    /// <summary>
    ///     The current position of the agent.
    /// </summary>
    public Position Position { get; set; }
    
    /// <summary>
    ///     A collection of positions that make up the agent's movement trajectory.
    /// </summary>
    private readonly List<Position> _positions = new();
    
    #endregion

    #region Initialization

    public void Init(AnimalLayer layer)
    {
        InfectedCounter = 0;
        InfectedTotalCounter = 0;
        Layer = layer;
        Energy = RandomHelper.Random.NextDouble(SpawnMinEnergy, SpawnMaxEnergy);
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
    
    #endregion

    #region Tick

    public void Tick()
    {
        StoreTickResult = false;
        if (Layer.Context.CurrentTick == Layer.Context.MaxTicks || Layer.Context.CurrentTick == 1)
        {
            // in the first/last sim tick store the agent data regardless of anthrax infections so, we have 
            // the full set of agents in the output.
            StoreTickResult = true;
        }

        switch (State)
        {
            // Movement
            case AnimalState.RandomMove:
            {
                // in case we are on the wrong land type! Quickly move to the nearest comfortable area.
                // this can happen after visiting an water source, or after initialization.
                if (!_preferredLandTypes.Contains(LandscapeLayer.GetTypeForPosition(Position)))
                {
                    var nearestPreferredFeature = LandscapeLayer.FindNearestLandAreaOfType(Position, _preferredLandTypes.First());  // TODO replace First() call with some land type decision logic
                    var posInPreferredArea =nearestPreferredFeature.VectorStructured.Geometry.RandomPositionFromGeometry();
                    var bearing = Position.GetBearing(posInPreferredArea);
                    
                    // also update the distance, so we at most walk directly onto of the preferred landscape
                    // but never farther, which might lead us out of the perimeter!
                    var distance = Math.Min(GetDistance(), Position.DistanceInMTo(posInPreferredArea));
                    Position = Layer.ImpalaEnvironment.MoveTowards(this, bearing, distance);
                }
                else
                {
                    var moved = false;
                    do
                    {
                        var bearing = RandomHelper.Random.NextDouble(0, 360);
                        var distance = GetDistance();
                        var target = Position.CalculateRelativePosition(bearing, distance);
                
                        // is target still in area of KNP?
                        if (!Perimeter.IsPointInside(target))
                        {
                            continue;
                        }

                        // is target of same category as our current position?
                        if (!_preferredLandTypes.Contains(LandscapeLayer.GetTypeForPosition(target)))
                        {
                            continue;
                        }
                
                        // if the agent is to far from a water source walk towards it!
                        var nearestWaterSource = WaterLayer.GetNearestWaterSource(Position);
                        var positionAsPoint = new Point(Position.X, Position.Y);
                        //var d = nearestWaterSource.VectorStructured.Geometry.Distance(g);
                        var nearestPointInWaterSource = DistanceOp.NearestPoints(positionAsPoint, nearestWaterSource.VectorStructured.Geometry);
                        var pointAsPosition = new Position(nearestPointInWaterSource[1].X, nearestPointInWaterSource[1].Y);
                        var distanceToPointInWaterSource = Position.DistanceInMTo(pointAsPosition);
                
                        if (distanceToPointInWaterSource > MaxDistanceFromWaterInM)
                        {
                            bearing = Position.GetBearing(pointAsPosition);
                            distance = Math.Min(distanceToPointInWaterSource, distance);
                        }

                        Position = Layer.ImpalaEnvironment.MoveTowards(this, bearing, distance);
                        moved = true;
                    } while (!moved);
                }
            
                if (Energy < 15)
                {
                    State = AnimalState.SearchForWater;
                }

                break;
            }
            case AnimalState.SearchForWater:
            {
                // Energy is low, so look for water
                var waterSources = WaterLayer.Explore(Position.PositionArray, 50000).ToList();
                if (waterSources.Any())
                {
                    // Explore() is not sorted by distance, so we need to sort them first.
                    var nearestWaterSource = new VectorFeature();
                    var distanceToNearestWaterSource = double.MaxValue;
                    foreach (var waterSource in waterSources)
                    {
                        var positionAsPoint = new Point(Position.X, Position.Y);
                        var distanceToWaterSource = waterSource.VectorStructured.Geometry.Distance(positionAsPoint);
                    
                        //var loc = (Point) w.VectorStructured.Geometry;
                        //var t = new Position(loc.X, loc.Y);
                        //var d = t.DistanceInMTo(Position);

                        if (distanceToWaterSource < distanceToNearestWaterSource)
                        {
                            distanceToNearestWaterSource = distanceToWaterSource;
                            nearestWaterSource = waterSource;
                        }
                    }
                
                    // Get coordinates of the nearest water source...
                    //var nearestWaterSource = waterSources.First();
                
                    // @todo: RandomPositionFromGeometry() is needed since die direct way for LINESTRINGs would be the 
                    // requires the nearest POINT on the target geometry, but we don't have that from the API. So we just
                    // use a random position somewhere on the linestring, so at least we go in the right direction...
                    var waterSourceLocation = nearestWaterSource.VectorStructured.Geometry.RandomPositionFromGeometry();
                    var target = new Position(waterSourceLocation.X, waterSourceLocation.Y);
                
                    // Math.Min() of random move distance and distance to water source, so we can reach the sight!
                    var distance = Math.Min(target.DistanceInMTo(Position),  GetDistance());
                
                    // ... and change the agent's bearing such that it looks in the direction of the water source
                    var bearing = Position.GetBearing(target);
                    Position = Layer.ImpalaEnvironment.MoveTowards(this, bearing, distance);

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

                break;
            }
            case AnimalState.Drinking:
            {
                Energy += 50;
                if (Energy > 101)
                {
                    State = AnimalState.RandomMove;
                }

                break;
            }
            default:
                Console.WriteLine("Unknown agent state in Impala agent.");
                break;
        }
        
        // Energy drops each tick
        Energy -= 1;

        if (OutputAgentTrack)
        {
            _positions.Add(Position.Copy());
        }
        
        // Infection
        if (AnthraxLayer.IsInRaster(Position) && AnthraxLayer.GetValue(Position) > 0)
        {
            var beta = AnthraxLayer.GetValue(Position) * AnthraxInfectionProbability;

            if (RandomHelper.SmallerThan(beta))
            {
                StoreTickResult = true;
                InfectedTotalCounter += 1;
                InfectedCounter += 1;
                var deathOccursInTicks = RandomHelper.Random.NextInteger(MinInfectionDurationInTicks,
                    MaxInfectionDurationInTicks + 1);
                var x = Layer.Context.CurrentTick + deathOccursInTicks;
                _leaveAnthraxTraceTicks.Add(x);
            }
        }

        // Leave Anthrax trail on the Map / animal dies
        if (InfectedCounter > 0)
        {
            if (_leaveAnthraxTraceTicks.Contains(Layer.Context.CurrentTick))
            {
                StoreTickResult = true;
                InfectedCounter -= 1;
                _leaveAnthraxTraceTicks.Remove(Layer.Context.CurrentTick);

                if (AnthraxLayer.IsInRaster(Position))
                {
                    AnthraxLayer[Position] += 1;
                }
            }
        }
        
        // Leave Movement trace for heatmap
        if (ImpalaMovement.IsInRaster(Position))
        {
            ImpalaMovement[Position] += 0.1;
        }

        // On last tick export movement of this agent as GeoJSON LineString
        if (OutputAgentTrack && Layer.GetCurrentTick() == Layer.Context.MaxTicks)
        {
            var featureCollection = new FeatureCollection();
            var coordinates = new List<Coordinate>();

            foreach (var position in _positions)
            {
                coordinates.Add(new Coordinate(position.X, position.Y));
            }
            
            var lineString = new LineString(coordinates.ToArray());
            featureCollection.Add(new Feature(lineString, new AttributesTable()));
            var featureCollectionAsGeoJson = new GeoJsonWriter().Write(featureCollection);
            File.WriteAllText($"Impala_path_{ID}.geojson", featureCollectionAsGeoJson);
        }
    }
    
    #endregion
    
    #region Methods

    /// <summary>
    ///     Generates a random movement distance for the current tick.
    /// </summary>
    /// <returns>The movement distance the agent will traverse during the current tick.</returns>
    private double GetDistance()
    {
        return RandomHelper.Random.NextDouble(MinMovementPerTickInM, MaxMovementPerTickInM);
    }
    
    #endregion
}