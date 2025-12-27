using GIS3DEngine.Core.Animation;
using GIS3DEngine.Core.Flights;
using GIS3DEngine.Core.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GIS3DEngine.Drones.Missions;

/// <summary>
/// Package delivery mission.
/// </summary>
public class DeliveryMission : DroneMission
{
    public override MissionType Type => MissionType.Delivery;

    /// <summary>Pickup location.</summary>
    public Vector3D PickupLocation { get; set; }

    /// <summary>Delivery location.</summary>
    public Vector3D DeliveryLocation { get; set; }

    /// <summary>Package weight in kg.</summary>
    public double PackageWeightKg { get; set; } = 1.0;

    /// <summary>Hover time at pickup (seconds).</summary>
    public double PickupHoverSec { get; set; } = 30.0;

    /// <summary>Hover time at delivery (seconds).</summary>
    public double DeliveryHoverSec { get; set; } = 30.0;

    /// <summary>Delivery altitude (lower for drop).</summary>
    public double DeliveryAltitude { get; set; } = 15.0;

    /// <summary>Return to home after delivery.</summary>
    public bool ReturnAfterDelivery { get; set; } = true;

    public double HoverTimeAtPickup { get; set; } = 10;    // seconds
    public double HoverTimeAtDelivery { get; set; } = 10;  // seconds

    public override FlightPath GenerateFlightPath()
    {
        var waypoints = new List<Waypoint>();
        var time = 0.0;

        // Start at home
        waypoints.Add(new Waypoint(HomePosition, time));
        time += 5;

        // Fly to cruise altitude
        var cruiseStart = HomePosition + new Vector3D(0, 0, Altitude);
        time += Altitude / 5; // Climb rate
        waypoints.Add(new Waypoint(cruiseStart, time));

        // Fly to pickup
        var pickupCruise = new Vector3D(PickupLocation.X, PickupLocation.Y, HomePosition.Z + Altitude);
        var toPickup = Vector3D.Distance(cruiseStart, pickupCruise);
        time += toPickup / Speed;
        waypoints.Add(new Waypoint(pickupCruise, time));

        // Descend for pickup
        var pickupPoint = new Vector3D(PickupLocation.X, PickupLocation.Y, PickupLocation.Z + 5);
        time += (Altitude - 5) / 3; // Descent rate
        waypoints.Add(new Waypoint(pickupPoint, time));

        // Hover for pickup
        time += PickupHoverSec;
        waypoints.Add(new Waypoint(pickupPoint, time));

        // Climb back up
        time += (Altitude - 5) / 5;
        waypoints.Add(new Waypoint(pickupCruise, time));

        // Fly to delivery
        var deliveryCruise = new Vector3D(DeliveryLocation.X, DeliveryLocation.Y, HomePosition.Z + Altitude);
        var toDelivery = Vector3D.Distance(pickupCruise, deliveryCruise);
        time += toDelivery / Speed;
        waypoints.Add(new Waypoint(deliveryCruise, time));

        // Descend for delivery
        var deliveryPoint = new Vector3D(DeliveryLocation.X, DeliveryLocation.Y, DeliveryLocation.Z + DeliveryAltitude);
        time += (Altitude - DeliveryAltitude) / 3;
        waypoints.Add(new Waypoint(deliveryPoint, time));

        // Hover for delivery
        time += DeliveryHoverSec;
        waypoints.Add(new Waypoint(deliveryPoint, time));

        if (ReturnAfterDelivery)
        {
            // Climb and return
            time += (Altitude - DeliveryAltitude) / 5;
            waypoints.Add(new Waypoint(deliveryCruise, time));

            var toHome = Vector3D.Distance(deliveryCruise, cruiseStart);
            time += toHome / Speed;
            waypoints.Add(new Waypoint(cruiseStart, time));

            // Land
            time += Altitude / 3;
            waypoints.Add(new Waypoint(HomePosition, time));
        }

        EstimatedDurationSec = time;
        EstimatedDistanceM = CalculateTotalDistance(waypoints);

        return FlightPath.CreateSpline(waypoints);
    }

    private static double CalculateTotalDistance(List<Waypoint> waypoints)
    {
        double total = 0;
        for (int i = 1; i < waypoints.Count; i++)
        {
            total += Vector3D.Distance(waypoints[i - 1].Position, waypoints[i].Position);
        }
        return total;
    }
}

