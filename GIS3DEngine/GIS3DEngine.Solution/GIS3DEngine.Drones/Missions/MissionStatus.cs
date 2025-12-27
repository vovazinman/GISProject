using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GIS3DEngine.Drones.Missions;

    /// <summary>
    /// Mission status tracking.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum MissionStatus
    {
        Created,
        Validated,
        Assigned,
        InProgress,
        Paused,
        Completed,
        Aborted,
        Failed,
        Running,
        Scheduled,
        Cancelled
    }

