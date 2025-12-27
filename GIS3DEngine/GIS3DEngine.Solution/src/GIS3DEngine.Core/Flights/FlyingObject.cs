using GIS3DEngine.Core.Interfaces;
using GIS3DEngine.Core.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GIS3DEngine.Core.Flights
{
    /// <summary>
    /// Animated flying object that follows a path.
    /// </summary>
    public class FlyingObject : IFlyingObject
    {
        public string Id { get; }
        public string Name { get; set; }
        public FlyingObjectType Type { get; set; }
        public FlightPath? Path { get; private set; }

        public Vector3D Position { get; private set; }
        public Vector3D Velocity { get; private set; }
        public double Heading { get; private set; }
        public double Pitch { get; private set; }
        public double Roll { get; set; }

        public double CurrentTime { get; private set; }
        public double SpeedMultiplier { get; set; } = 1.0;
        public bool IsPlaying { get; private set; }
        public bool IsPaused { get; private set; }

        public double MaxSpeed { get; set; } = 100.0;
        public double Acceleration { get; set; } = 10.0;
        public double TurnRate { get; set; } = Math.PI / 4; // 45 degrees per second

        public event EventHandler<int>? WaypointReached;
        public event EventHandler? PathCompleted;

        private int _lastWaypointIndex = -1;

        public FlyingObject(string id, FlyingObjectType type, FlightPath? path = null)
        {
            Id = id;
            Name = id;
            Type = type;
            Path = path;
            Position = path?.GetPositionAtTime(0) ?? Vector3D.Zero;
        }

        public void SetPath(FlightPath path)
        {
            Path = path;
            CurrentTime = 0;
            Position = path.GetPositionAtTime(0);
            _lastWaypointIndex = -1;
        }

        public void Play()
        {
            IsPlaying = true;
            IsPaused = false;
        }

        public void Pause()
        {
            IsPaused = true;
        }

        public void Resume()
        {
            IsPaused = false;
        }

        public void Stop()
        {
            IsPlaying = false;
            IsPaused = false;
            CurrentTime = 0;
            if (Path != null)
                Position = Path.GetPositionAtTime(0);
            _lastWaypointIndex = -1;
        }

        public void SetTime(double time)
        {
            CurrentTime = Math.Max(0, time);
            if (Path != null)
            {
                Position = Path.GetPositionAtTime(CurrentTime);
                Velocity = Path.GetVelocityAtTime(CurrentTime);
                Heading = Path.GetHeadingAtTime(CurrentTime);
                Pitch = Path.GetPitchAtTime(CurrentTime);
            }
        }

        public void Update(double deltaTime)
        {
            if (!IsPlaying || IsPaused || Path == null)
                return;

            CurrentTime += deltaTime * SpeedMultiplier;

            // Check for path completion
            if (CurrentTime >= Path.TotalDuration)
            {
                if (Path.IsLooping)
                {
                    CurrentTime %= Path.TotalDuration;
                    _lastWaypointIndex = -1;
                }
                else
                {
                    CurrentTime = Path.TotalDuration;
                    Position = Path.GetPositionAtTime(CurrentTime);
                    IsPlaying = false;
                    PathCompleted?.Invoke(this, EventArgs.Empty);
                    return;
                }
            }

            // Update position and orientation
            Position = Path.GetPositionAtTime(CurrentTime);
            Velocity = Path.GetVelocityAtTime(CurrentTime);
            Heading = Path.GetHeadingAtTime(CurrentTime);
            Pitch = Path.GetPitchAtTime(CurrentTime);

            // Check for waypoint crossing
            CheckWaypointCrossing();
        }

        /// <summary>
        /// Moves toward a target position at specified speed.
        /// </summary>
        public void MoveToward(Vector3D target, double speed, double deltaTime)
        {
            var direction = target - Position;
            var distance = direction.Magnitude;

            if (distance < 1e-6)
                return;

            var moveDistance = Math.Min(speed * deltaTime, distance);
            Position = Position + direction.Normalized * moveDistance;
            Velocity = direction.Normalized * speed;
            Heading = Math.Atan2(direction.X, direction.Y);
            Pitch = Math.Atan2(direction.Z, Math.Sqrt(direction.X * direction.X + direction.Y * direction.Y));
        }

        private void CheckWaypointCrossing()
        {
            if (Path == null)
                return;

            for (int i = 0; i < Path.Waypoints.Count; i++)
            {
                var wp = Path.Waypoints[i];
                if (i > _lastWaypointIndex && CurrentTime >= wp.Time)
                {
                    _lastWaypointIndex = i;
                    WaypointReached?.Invoke(this, i);
                }
            }
        }
    }
}
