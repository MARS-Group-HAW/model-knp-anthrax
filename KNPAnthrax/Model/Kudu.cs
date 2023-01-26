using System;
using Mars.Common;
using Mars.Common.Core.Random;
using Mars.Interfaces.Agents;
using Mars.Interfaces.Annotations;
using Mars.Interfaces.Environments;
using Mars.Numerics;

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
    ///     The layer on which these agents live
    /// </summary>
    private AnimalLayer Layer { get; set; }
    
    /// <summary>
    ///     The perimeter of the simulation environment
    /// </summary>
    [PropertyDescription(Name = "LandscapeLayer")]
    public LandscapeLayer LandscapeLayer { get; set; }
    
    /// <summary>
    ///     The perimeter of the simulation environment
    /// </summary>
    [PropertyDescription(Name = "Perimeter")]
    public Perimeter Perimeter { get; set; }
    
    public void Init(AnimalLayer layer)
    {
        Layer = layer;
        Position = Position.CreateGeoPosition(Longitude, Latitude);
        Console.WriteLine($"I'm a kudu @ {Position}!");
    }

    public void Tick()
    {
        var moved = false;
        do
        {
            var bearing = RandomHelper.NextDouble(RandomHelper.Random, 0, 360);
            var distance = RandomHelper.NextDouble(RandomHelper.Random, 750, 1250); // todo: konfigurarierbar?
            var target = Position.CalculateRelativePosition(bearing, distance);
            
            // is target still in area of KNP?
            if (!Perimeter.IsPointInside(target))
            {
                Console.WriteLine("outside perimeter");
                continue;
            }
            
            // is target of same category as our current position?
            if (!LandscapeLayer.IsTargetPositionOfSameCategory(Position, target))
            {
                Console.WriteLine("wrong cateogry");
                continue;
            }
            
            Position = Layer.KuduEnvironment.MoveTowards(this, bearing, distance);
            moved = true;
        } while (!moved);
        
        Console.WriteLine("-----\n");
    }

    public Guid ID { get; set; }
    public Position Position { get; set; }
}