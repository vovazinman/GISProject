using GIS3DEngine.Core.Primitives;
using GIS3DEngine.Core.Interfaces;

namespace GIS3DEngine.Core.Animation;

/// <summary>
/// Waypoint for flight path definition.
/// </summary>
public class Waypoint
{
    public Vector3D Position { get; }
    public double Time { get; }
    public double? Speed { get; }
    public WaypointType Type { get; }

    public Waypoint(Vector3D position, double time, double? speed = null, WaypointType type = WaypointType.Smooth)
    {
        Position = position;
        Time = time;
        Speed = speed;
        Type = type;
    }
}

/// <summary>
/// Time-based animation path with interpolation.
/// </summary>
public class FlightPath : IAnimationPath
{
    private readonly List<Waypoint> _waypoints;
    private readonly InterpolationType _interpolationType;
    private readonly bool _isLooping;
    private readonly double _tension;

    public IReadOnlyList<Waypoint> Waypoints => _waypoints.AsReadOnly();
    public InterpolationType InterpolationType => _interpolationType;
    public bool IsLooping => _isLooping;
    public double Tension => _tension;

    public double TotalDuration => _waypoints.Count > 0 ? _waypoints[^1].Time : 0;

    public double TotalDistance
    {
        get
        {
            double distance = 0;
            for (int i = 1; i < _waypoints.Count; i++)
            {
                distance += Vector3D.Distance(_waypoints[i - 1].Position, _waypoints[i].Position);
            }
            return distance;
        }
    }

    public bool IsClosed =>
        _waypoints.Count > 1 &&
        Vector3D.Distance(_waypoints[0].Position, _waypoints[^1].Position) < 1e-6;

    private FlightPath(List<Waypoint> waypoints, InterpolationType interpolationType, bool isLooping, double tension)
    {
        _waypoints = waypoints;
        _interpolationType = interpolationType;
        _isLooping = isLooping;
        _tension = tension;
    }

    /// <summary>
    /// Creates a linear flight path.
    /// </summary>
    public static FlightPath CreateLinear(IEnumerable<Waypoint> waypoints, bool isLooping = false) =>
        new(waypoints.OrderBy(w => w.Time).ToList(), InterpolationType.Linear, isLooping, 0.5);

    /// <summary>
    /// Creates a smooth spline flight path.
    /// </summary>
    public static FlightPath CreateSpline(IEnumerable<Waypoint> waypoints, bool isLooping = false, double tension = 0.5) =>
        new(waypoints.OrderBy(w => w.Time).ToList(), InterpolationType.CatmullRom, isLooping, tension);

    /// <summary>
    /// Creates a flight path with automatic timing based on speed.
    /// </summary>
    public static FlightPath CreateWithSpeed(IEnumerable<Vector3D> positions, double speed, InterpolationType interpolationType = InterpolationType.Linear)
    {
        var waypoints = new List<Waypoint>();
        var posList = positions.ToList();
        double time = 0;

        for (int i = 0; i < posList.Count; i++)
        {
            waypoints.Add(new Waypoint(posList[i], time, speed));
            if (i < posList.Count - 1)
            {
                time += Vector3D.Distance(posList[i], posList[i + 1]) / speed;
            }
        }

        return new FlightPath(waypoints, interpolationType, false, 0.5);
    }

    /// <summary>
    /// Creates a circular orbit path.
    /// </summary>
    public static FlightPath CreateOrbit(Vector3D center, double radius, double altitude, double duration, int segments = 36)
    {
        var waypoints = new List<Waypoint>();
        var angleStep = 2 * Math.PI / segments;
        var timeStep = duration / segments;

        for (int i = 0; i <= segments; i++)
        {
            var angle = i * angleStep;
            var position = new Vector3D(
                center.X + radius * Math.Cos(angle),
                center.Y + radius * Math.Sin(angle),
                center.Z + altitude
            );
            waypoints.Add(new Waypoint(position, i * timeStep));
        }

        return new FlightPath(waypoints, InterpolationType.CatmullRom, true, 0.5);
    }

    /// <summary>
    /// Creates a figure-eight path.
    /// </summary>
    public static FlightPath CreateFigureEight(Vector3D center, double size, double altitude, double duration, int segments = 72)
    {
        var waypoints = new List<Waypoint>();
        var timeStep = duration / segments;

        for (int i = 0; i <= segments; i++)
        {
            var t = (double)i / segments * 2 * Math.PI;
            var position = new Vector3D(
                center.X + size * Math.Sin(t),
                center.Y + size * Math.Sin(t) * Math.Cos(t),
                center.Z + altitude
            );
            waypoints.Add(new Waypoint(position, i * timeStep));
        }

        return new FlightPath(waypoints, InterpolationType.CatmullRom, true, 0.5);
    }

    public Vector3D GetPositionAtTime(double time)
    {
        if (_waypoints.Count == 0)
            return Vector3D.Zero;
        if (_waypoints.Count == 1)
            return _waypoints[0].Position;

        // Handle looping
        if (_isLooping && time > TotalDuration)
            time %= TotalDuration;

        // Clamp time
        time = Math.Max(0, Math.Min(time, TotalDuration));

        // Find segment
        int i = FindSegmentIndex(time);
        var w0 = _waypoints[i];
        var w1 = _waypoints[Math.Min(i + 1, _waypoints.Count - 1)];

        // Calculate local t
        var segmentDuration = w1.Time - w0.Time;
        var t = segmentDuration > 0 ? (time - w0.Time) / segmentDuration : 0;

        return _interpolationType switch
        {
            InterpolationType.CatmullRom => CatmullRomInterpolate(i, t),
            _ => Vector3D.Lerp(w0.Position, w1.Position, t)
        };
    }

    public Vector3D GetVelocityAtTime(double time)
    {
        const double delta = 0.001;
        var p0 = GetPositionAtTime(time);
        var p1 = GetPositionAtTime(time + delta);
        return (p1 - p0) / delta;
    }

    public Vector3D GetDirectionAtTime(double time)
    {
        var velocity = GetVelocityAtTime(time);
        return velocity.Magnitude > 1e-10 ? velocity.Normalized : Vector3D.UnitX;
    }

    /// <summary>
    /// Gets the index of the current waypoint at a given time.
    /// </summary>
    public int GetCurrentWaypointIndex(double time)
    {
        if (_waypoints.Count == 0)
            return 0;

        // Handle looping
        if (_isLooping && time > TotalDuration)
        {
            time = time % TotalDuration;
        }

        // Find the waypoint we've passed
        for (int i = _waypoints.Count - 1; i >= 0; i--)
        {
            if (time >= _waypoints[i].Time)
                return i;
        }

        return 0;
    }

    /// <summary>
    /// Gets the next waypoint from current time.
    /// </summary>
    public Waypoint? GetNextWaypoint(double time)
    {
        var currentIndex = GetCurrentWaypointIndex(time);
        if (currentIndex < _waypoints.Count - 1)
            return _waypoints[currentIndex + 1];

        return _isLooping ? _waypoints[0] : null;
    }

    /// <summary>
    /// Gets progress through the path (0.0 to 1.0).
    /// </summary>
    public double GetProgress(double time)
    {
        if (TotalDuration <= 0)
            return 0;

        if (_isLooping)
            return (time % TotalDuration) / TotalDuration;

        return Math.Clamp(time / TotalDuration, 0, 1);
    }

    public double GetSpeedAtTime(double time) => GetVelocityAtTime(time).Magnitude;

    public double GetAltitudeAtTime(double time) => GetPositionAtTime(time).Z;

    /// <summary>
    /// Gets heading in radians (0 = North/+Y, π/2 = East/+X).
    /// </summary>
    public double GetHeadingAtTime(double time)
    {
        var dir = GetDirectionAtTime(time);
        return Math.Atan2(dir.X, dir.Y);
    }

    /// <summary>
    /// Gets pitch in radians (positive = climbing).
    /// </summary>
    public double GetPitchAtTime(double time)
    {
        var velocity = GetVelocityAtTime(time);
        var horizontalSpeed = Math.Sqrt(velocity.X * velocity.X + velocity.Y * velocity.Y);
        return Math.Atan2(velocity.Z, horizontalSpeed);
    }

    /// <summary>
    /// Gets the cumulative distance traveled at a given time.
    /// </summary>
    public double GetDistanceAtTime(double time)
    {
        if (_waypoints.Count < 2 || time <= 0)
            return 0;

        double distance = 0;
        var prevPos = _waypoints[0].Position;
        var sampleCount = (int)(time * 100);

        for (int i = 1; i <= sampleCount; i++)
        {
            var t = time * i / sampleCount;
            var pos = GetPositionAtTime(t);
            distance += Vector3D.Distance(prevPos, pos);
            prevPos = pos;
        }

        return distance;
    }

    /// <summary>
    /// Samples the path at regular intervals.
    /// </summary>
    public IReadOnlyList<Vector3D> SamplePath(int sampleCount)
    {
        var samples = new List<Vector3D>();
        for (int i = 0; i <= sampleCount; i++)
        {
            var t = TotalDuration * i / sampleCount;
            samples.Add(GetPositionAtTime(t));
        }
        return samples;
    }

    private int FindSegmentIndex(double time)
    {
        for (int i = 0; i < _waypoints.Count - 1; i++)
        {
            if (time >= _waypoints[i].Time && time <= _waypoints[i + 1].Time)
                return i;
        }
        return _waypoints.Count - 2;
    }

    private Vector3D CatmullRomInterpolate(int index, double t)
    {
        var p0 = _waypoints[Math.Max(0, index - 1)].Position;
        var p1 = _waypoints[index].Position;
        var p2 = _waypoints[Math.Min(_waypoints.Count - 1, index + 1)].Position;
        var p3 = _waypoints[Math.Min(_waypoints.Count - 1, index + 2)].Position;

        var t2 = t * t;
        var t3 = t2 * t;

        var tension = _tension;
        var s = (1 - tension) / 2;

        var b0 = -s * t3 + 2 * s * t2 - s * t;
        var b1 = (2 - s) * t3 + (s - 3) * t2 + 1;
        var b2 = (s - 2) * t3 + (3 - 2 * s) * t2 + s * t;
        var b3 = s * t3 - s * t2;

        return new Vector3D(
            p0.X * b0 + p1.X * b1 + p2.X * b2 + p3.X * b3,
            p0.Y * b0 + p1.Y * b1 + p2.Y * b2 + p3.Y * b3,
            p0.Z * b0 + p1.Z * b1 + p2.Z * b2 + p3.Z * b3
        );
    }
}

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

/// <summary>
/// Linear interpolator.
/// </summary>
public class LinearInterpolator : IInterpolator
{
    public Vector3D Interpolate(Vector3D start, Vector3D end, double t) =>
        Vector3D.Lerp(start, end, t);

    public double Interpolate(double start, double end, double t) =>
        start + (end - start) * t;
}

/// <summary>
/// Smooth step interpolator.
/// </summary>
public class SplineInterpolator : IInterpolator
{
    public Vector3D Interpolate(Vector3D start, Vector3D end, double t)
    {
        t = SmoothStep(t);
        return Vector3D.Lerp(start, end, t);
    }

    public double Interpolate(double start, double end, double t)
    {
        t = SmoothStep(t);
        return start + (end - start) * t;
    }

    private static double SmoothStep(double t) =>
        t * t * (3 - 2 * t);
}

/// <summary>
/// Common easing functions.
/// </summary>
public static class Easing
{
    public static double Linear(double t) => t;
    public static double EaseInQuad(double t) => t * t;
    public static double EaseOutQuad(double t) => t * (2 - t);
    public static double EaseInOutQuad(double t) => t < 0.5 ? 2 * t * t : -1 + (4 - 2 * t) * t;
    public static double EaseInCubic(double t) => t * t * t;
    public static double EaseOutCubic(double t) => (--t) * t * t + 1;
    public static double EaseInOutCubic(double t) => t < 0.5 ? 4 * t * t * t : (t - 1) * (2 * t - 2) * (2 * t - 2) + 1;
    public static double EaseInSine(double t) => 1 - Math.Cos(t * Math.PI / 2);
    public static double EaseOutSine(double t) => Math.Sin(t * Math.PI / 2);
    public static double EaseInOutSine(double t) => -(Math.Cos(Math.PI * t) - 1) / 2;
}

/// <summary>
/// Time controller for synchronized animation.
/// </summary>
public class TimeController
{
    private readonly List<FlyingObject> _objects = new();
    private DateTime _lastUpdateTime;

    public double GlobalTime { get; private set; }
    public double TimeScale { get; set; } = 1.0;
    public bool IsRunning { get; private set; }

    public IReadOnlyList<FlyingObject> Objects => _objects.AsReadOnly();

    public void AddObject(FlyingObject obj)
    {
        if (!_objects.Contains(obj))
            _objects.Add(obj);
    }

    public void RemoveObject(FlyingObject obj)
    {
        _objects.Remove(obj);
    }

    public void Start()
    {
        IsRunning = true;
        _lastUpdateTime = DateTime.Now;
        foreach (var obj in _objects)
            obj.Play();
    }

    public void Stop()
    {
        IsRunning = false;
        foreach (var obj in _objects)
            obj.Stop();
    }

    public void Reset()
    {
        GlobalTime = 0;
        foreach (var obj in _objects)
        {
            obj.Stop();
            obj.SetTime(0);
        }
    }

    public void SetTimeScale(double scale)
    {
        TimeScale = Math.Clamp(scale, 0.1, 10.0);
    }

    /// <summary>
    /// Updates all objects using real time delta.
    /// </summary>
    public void Update()
    {
        var now = DateTime.Now;
        var deltaTime = (now - _lastUpdateTime).TotalSeconds * TimeScale;
        _lastUpdateTime = now;

        Update(deltaTime);
    }

    /// <summary>
    /// Updates all objects with specified delta time.
    /// </summary>
    public void Update(double deltaTime)
    {
        if (!IsRunning)
            return;

        GlobalTime += deltaTime;
        foreach (var obj in _objects)
        {
            obj.Update(deltaTime);
        }
    }
}
