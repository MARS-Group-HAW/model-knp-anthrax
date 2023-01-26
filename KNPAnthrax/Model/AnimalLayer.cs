using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mars.Components.Environments;
using Mars.Components.Layers;
using Mars.Core.Data;
using Mars.Interfaces.Annotations;
using Mars.Interfaces.Data;
using Mars.Interfaces.Environments;
using Mars.Interfaces.Layers;
using MongoDB.Bson;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace KNPAnthrax.Model;

public class AnimalLayer : AbstractLayer
{
    /// <summary>
    /// The LandscapeLayer registers the Elephants in the runtime system. In this way, the tick methods
    /// of the agents can be executed later. Then the expansion of the simulation area is calculated using
    /// the raster layers described in config.json. An environment is created with this bounding box.
    /// </summary>
    /// <param name="layerInitData"></param>
    /// <param name="registerAgentHandle"></param>
    /// <param name="unregisterAgentHandle"></param>
    /// <returns>true if the agents where registered</returns>
    public override bool InitLayer(LayerInitData layerInitData, RegisterAgent registerAgentHandle,
        UnregisterAgent unregisterAgentHandle)
    {
        base.InitLayer(layerInitData, registerAgentHandle, unregisterAgentHandle);

        // Calculate and expand extent
        var baseExtent = new Envelope(Fence.Extent.ToEnvelope());

        // Create GeoHashEnvironment with the calculated extent
        Environment = GeoHashEnvironment<Elephant>.BuildByBBox(new BoundingBox(baseExtent), 1000);
        KuduEnvironment = GeoHashEnvironment<Kudu>.BuildByBBox(new BoundingBox(baseExtent), 1000);

        var agentManager = layerInitData.Container.Resolve<IAgentManager>();
        Elephants = agentManager.Spawn<Elephant, AnimalLayer>().ToList();
        Kudus = agentManager.Spawn<Kudu, AnimalLayer>().ToList();
        
        return Kudus.Count > 0;
    }

    #region Properties and Fields

    public List<Elephant> Elephants { get; set; }

    public List<Kudu> Kudus { get; set; }

    
    [PropertyDescription(Name = "Perimeter")]
    public Perimeter Fence { get; set; }

    public GeoHashEnvironment<Elephant> Environment { get; set; }

    public GeoHashEnvironment<Kudu> KuduEnvironment { get; set; }

    #endregion
}