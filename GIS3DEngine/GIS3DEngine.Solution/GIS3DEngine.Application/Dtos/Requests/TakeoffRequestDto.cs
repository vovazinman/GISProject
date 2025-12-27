using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GIS3DEngine.Application.Dtos.Requests
{
    public record TakeoffRequestDto
    {
        public double Altitude { get; init; } = 30;
    }
}
