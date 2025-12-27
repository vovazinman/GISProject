using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GIS3DEngine.Application.Dtos.Responses
{
    /// <summary>
    /// Command response
    /// </summary>
    public record CommandResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public DroneStateDto? NewState { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
