using GIS3DEngine.Drones.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GIS3DEngine.Application.Dtos.Requests
{
    /// <summary>
    /// Go to request
    /// </summary>
    public record GoToRequestDto
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double Speed { get; set; } = 15;
        public string? Mode { get; set; } = "Direct";  // "Direct" || "Safe"
    }
}
