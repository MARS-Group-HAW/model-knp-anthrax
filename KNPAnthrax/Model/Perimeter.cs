using System.Linq;
using Mars.Components.Layers;
using NetTopologySuite.Geometries;
using Position = Mars.Interfaces.Environments.Position;

namespace KNPAnthrax.Model;

/// <summary>
///     A raster layer that ingests an .asc raster file with the walkable area by the agents.
/// </summary>
public class Perimeter : VectorLayer
{
    /// <summary>
    ///     Checks for the coordinate whether this point is inside the perimeter (the defined polygon).
    /// </summary>
    /// <param name="coordinate">The coordinate to check</param>
    /// <returns>
    ///     Returns true if the coordinate is inside the perimeter
    /// </returns>
    public bool IsPointInside(Position coordinate)
    {
        var p = new Point(coordinate.X, coordinate.Y);
        return Features.First().VectorStructured.Geometry.Covers(p);
        
        // First, check if the coordinate is inside the area of the .asc file.
        // The comparison with "1" ensures that it's not a "non-walkable" cell (the area outside of our polygon).
        // It's the NoData value that was set previously in QGIS.
        //return Extent.Contains(coordinate.X, coordinate.Y) && GetValue(coordinate) != NoDataValue.GetValueOrDefault();
    }
}