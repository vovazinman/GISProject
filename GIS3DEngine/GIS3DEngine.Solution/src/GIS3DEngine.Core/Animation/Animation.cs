using GIS3DEngine.Core.Primitives;
using GIS3DEngine.Core.Interfaces;
using GIS3DEngine.Core.Flights;

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
