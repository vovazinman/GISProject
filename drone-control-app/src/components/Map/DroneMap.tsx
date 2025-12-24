/**
 * DroneMap - מפת Leaflet עם מיקום הרחפן
 */

import React from 'react';
import { 
  MapContainer, 
  TileLayer, 
  Marker, 
  Popup, 
  Polyline, 
  Circle,
  useMap 
} from 'react-leaflet';
import L from 'leaflet';
import type { DroneState, FlightPath, Vector3D } from '../../types';

// ========== Custom Icons ==========

const createDroneIcon = (isFlying: boolean): L.DivIcon => {
  return L.divIcon({
    html: `<div class="drone-marker ${isFlying ? 'flying' : ''}">🚁</div>`,
    className: 'drone-icon-wrapper',
    iconSize: [40, 40],
    iconAnchor: [20, 20]
  });
};

const homeIcon = L.divIcon({
  html: '<div class="home-marker">🏠</div>',
  className: 'home-icon-wrapper',
  iconSize: [30, 30],
  iconAnchor: [15, 15]
});

const waypointIcon = L.divIcon({
  html: '<div class="waypoint-marker">📍</div>',
  className: 'waypoint-icon-wrapper',
  iconSize: [20, 20],
  iconAnchor: [10, 10]
});

// ========== Map Updater ==========

interface MapUpdaterProps {
  center: [number, number];
  follow: boolean;
}

const MapUpdater: React.FC<MapUpdaterProps> = ({ center, follow }) => {
  const map = useMap();
  
  React.useEffect(() => {
    if (follow && center) {
      map.setView(center, map.getZoom());
    }
  }, [center, follow, map]);
  
  return null;
};

// ========== Main Component ==========

interface DroneMapProps {
  drone: DroneState | null;
  flightPath?: FlightPath | null;
  homePosition?: Vector3D;
  followDrone?: boolean;
}

export const DroneMap: React.FC<DroneMapProps> = ({ 
  drone, 
  flightPath, 
  homePosition = { x: 0, y: 0, z: 0 },
  followDrone = true 
}) => {
  /**
   * המרת קואורדינטות מקומיות ל-LatLng
   * Base: Tel Aviv (32.0853, 34.7818)
   */
  const toLatLng = (pos: Vector3D | undefined): [number, number] => {
    if (!pos) return [32.0853, 34.7818];
    
    const baseLat = 32.0853;
    const baseLng = 34.7818;
    const metersPerDegree = 111320;
    
    return [
      baseLat + (pos.y / metersPerDegree),
      baseLng + (pos.x / metersPerDegree)
    ];
  };

  const dronePosition = drone?.position ? toLatLng(drone.position) : null;
  const homeLatLng = toLatLng(homePosition);
  const isFlying = ['Flying', 'Hovering', 'TakingOff'].includes(drone?.status as string);

  // מסלול טיסה
  const pathPositions: [number, number][] = flightPath?.waypoints?.map(wp => 
    toLatLng(wp.position)
  ) || [];

  return (
    <div className="drone-map">
      <MapContainer
        center={homeLatLng}
        zoom={16}
        style={{ height: '100%', width: '100%' }}
      >
        <TileLayer
          attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
        />

        {dronePosition && (
          <MapUpdater center={dronePosition} follow={followDrone} />
        )}

        {/* Home */}
        <Marker position={homeLatLng} icon={homeIcon}>
          <Popup><strong>🏠 Home Position</strong></Popup>
        </Marker>

        {/* Flight Path */}
        {pathPositions.length > 1 && (
          <>
            <Polyline
              positions={pathPositions}
              color="#3b82f6"
              weight={3}
              opacity={0.7}
              dashArray="10, 5"
            />
            {pathPositions.map((pos, idx) => (
              <Marker key={idx} position={pos} icon={waypointIcon}>
                <Popup>Waypoint {idx + 1}</Popup>
              </Marker>
            ))}
          </>
        )}

        {/* Drone */}
        {dronePosition && drone && (
          <>
            <Marker position={dronePosition} icon={createDroneIcon(isFlying)}>
              <Popup>
                <strong>🚁 {drone.droneId}</strong><br />
                Status: {drone.status}<br />
                Altitude: {drone.altitudeAGL?.toFixed(1)}m<br />
                Battery: {drone.batteryPercent?.toFixed(0)}%
              </Popup>
            </Marker>
            <Circle
              center={dronePosition}
              radius={10}
              pathOptions={{
                color: isFlying ? '#22c55e' : '#6b7280',
                fillColor: isFlying ? '#22c55e' : '#6b7280',
                fillOpacity: 0.2
              }}
            />
          </>
        )}
      </MapContainer>

      {/* Overlay */}
      <div className="map-overlay">
        <div className="map-info">
          {drone && (
            <>
              <span>Alt: {drone.altitudeAGL?.toFixed(1)}m</span>
              <span>Speed: {drone.groundSpeed?.toFixed(1)}m/s</span>
            </>
          )}
        </div>
      </div>
    </div>
  );
};

export default DroneMap;