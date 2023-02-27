using System.Collections.Generic;
using System.Linq;
using Mars.Components.Environments;
using Mars.Components.Layers;
using Mars.Core.Data;
using Mars.Interfaces.Annotations;
using Mars.Interfaces.Data;
using Mars.Interfaces.Environments;
using Mars.Interfaces.Layers;
using NetTopologySuite.Geometries;

namespace KNPAnthrax.Model;

public class AnimalLayer : AbstractLayer
{
    /// <summary>
    /// The AnimalLayer registers agents in the runtime system. This allows the tick methods of the agents
    /// to be executed later. Also, the expansion of the simulation area is calculated using the raster
    /// layers described in config.json. With this bounding box, an environment is created.
    /// </summary>
    /// <param name="layerInitData"></param>
    /// <param name="registerAgentHandle"></param>
    /// <param name="unregisterAgentHandle"></param>
    /// <returns>true if the agents where registered</returns>
    public override bool InitLayer(LayerInitData layerInitData, RegisterAgent registerAgentHandle = null,
        UnregisterAgent unregisterAgentHandle = null)
    {
        base.InitLayer(layerInitData, registerAgentHandle, unregisterAgentHandle);

        // Calculate and spatial extent of the environment
        var baseExtent = new Envelope(Fence.Extent.ToEnvelope());

        // Create one GeoHashEnvironment per agent type with the calculated extent
        KuduEnvironment = GeoHashEnvironment<Kudu>.BuildByBBox(new BoundingBox(baseExtent), 1000);
        ImpalaEnvironment = GeoHashEnvironment<Impala>.BuildByBBox(new BoundingBox(baseExtent), 1000);

        // Spawn the number of agents of each type specified in the simulation configuration
        var agentManager = layerInitData.Container.Resolve<IAgentManager>();
        Kudus = agentManager.Spawn<Kudu, AnimalLayer>().ToList();
        Impalas = agentManager.Spawn<Impala, AnimalLayer>().ToList();
        
        return Kudus.Count > 0 || Impalas.Count > 0;
    }

    #region Properties and Fields

    /// <summary>
    ///     A collection of Kudu agents.
    /// </summary>
    public List<Kudu> Kudus { get; set; }
    
    /// <summary>
    ///     A collection of Impala agents.
    /// </summary>
    public List<Impala> Impalas { get; set; }

    /// <summary>
    ///     The perimeter of the simulation environment.
    /// </summary>
    [PropertyDescription(Name = "Perimeter")]
    public Perimeter Fence { get; set; }

    /// <summary>
    ///     A spatial environment for Kudu agents to move in.
    /// </summary>
    public GeoHashEnvironment<Kudu> KuduEnvironment { get; set; }
    
    /// <summary>
    ///     A spatial environment for Impala agents to move in.
    /// </summary>
    public GeoHashEnvironment<Impala> ImpalaEnvironment { get; set; }

    #endregion
}