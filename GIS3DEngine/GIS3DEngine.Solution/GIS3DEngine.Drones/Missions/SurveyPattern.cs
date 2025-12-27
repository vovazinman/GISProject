using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GIS3DEngine.Drones.Missions;



/// <summary>
/// Survey pattern types.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SurveyPattern
{
    /// <summary>Back and forth parallel lines.</summary>
    Lawnmower,
    /// <summary>Spiral from outside to center.</summary>
    SpiralInward,
    /// <summary>Spiral from center outward.</summary>
    SpiralOutward,
    /// <summary>Grid pattern (crosshatch).</summary>
    Grid,
    /// <summary>Circular orbits.</summary>
    Circular
}



