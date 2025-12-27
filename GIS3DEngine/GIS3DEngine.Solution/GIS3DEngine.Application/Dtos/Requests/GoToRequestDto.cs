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
        public double X { get; init; }
        public double Y { get; init; }
        public double Z { get; init; }
        public double Speed { get; init; } = 15;
        public FlightMode Mode { get; init; } = FlightMode.Direct;
    }
}
