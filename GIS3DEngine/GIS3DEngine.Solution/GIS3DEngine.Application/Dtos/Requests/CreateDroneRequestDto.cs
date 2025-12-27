using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GIS3DEngine.Application.Dtos.Requests
{
    /// <summary>
    /// Create drone request
    /// </summary>
    public record CreateDroneRequestDto
    {
        public string? Id { get; set; }
        public string? SpecsType { get; set; }  // "mavic3", "phantom4", "matrice300"
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
    }
}
