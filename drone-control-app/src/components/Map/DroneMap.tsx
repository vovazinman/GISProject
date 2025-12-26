/**
 * DroneMap - Leaflet map with drone position
 * Supports current location detection
 */

import React, { useEffect, useState } from 'react';
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

const currentLocationIcon = L.divIcon({
  html: '<div class="current-location-marker">📍</div>',
  className: 'current-location-icon-wrapper',
  iconSize: [24, 24],
  iconAnchor: [12, 12]
});

// ========== Map Updater ==========

interface MapUpdaterProps {
  center: [number, number];
  follow: boolean;
}

const MapUpdater: React.FC<MapUpdaterProps> = ({ center, follow }) => {
  const map = useMap();
  
  useEffect(() => {
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
  homePosition,
  followDrone = true 
}) => {
  // Current location state
  const [currentLocation, setCurrentLocation] = useState<[number, number] | null>(null);
  const [locationError, setLocationError] = useState<string | null>(null);
  const [isLoadingLocation, setIsLoadingLocation] = useState(true);

  // Default location (Tel Aviv) if geolocation unavailable
  const defaultLocation: [number, number] = [32.0853, 34.7818];

  // Get current location on mount
  useEffect(() => {
    if (!navigator.geolocation) {
      setLocationError('Geolocation not supported');
      setCurrentLocation(defaultLocation);
      setIsLoadingLocation(false);
      return;
    }

    navigator.geolocation.getCurrentPosition(
      (position) => {
        const loc: [number, number] = [
          position.coords.latitude, 
          position.coords.longitude
        ];
        setCurrentLocation(loc);
        setIsLoadingLocation(false);
        console.log('📍 Current location:', loc);
      },
      (error) => {
        console.warn('⚠️ Geolocation error:', error.message);
        setLocationError(error.message);
        setCurrentLocation(defaultLocation);
        setIsLoadingLocation(false);
      },
      { 
        enableHighAccuracy: true, 
        timeout: 10000,
        maximumAge: 60000 
      }
    );
  }, []);

  // Base location for coordinate conversion
  const baseLocation = currentLocation || defaultLocation;

  /**
   * Convert local coordinates to LatLng
   */
  const toLatLng = (pos: Vector3D | undefined): [number, number] => {
    if (!pos) return baseLocation;
    
    const metersPerDegreeLat = 111320;
    const metersPerDegreeLng = 111320 * Math.cos(baseLocation[0] * Math.PI / 180);
    
    return [
      baseLocation[0] + (pos.y / metersPerDegreeLat),
      baseLocation[1] + (pos.x / metersPerDegreeLng)
    ];
  };

  const dronePosition = drone?.position ? toLatLng(drone.position) : null;
  const homeLatLng = homePosition ? toLatLng(homePosition) : baseLocation;
  const isFlying = ['Flying', 'Hovering', 'TakingOff'].includes(drone?.status as string);

  // Flight path positions
  const pathPositions: [number, number][] = flightPath?.waypoints?.map(wp => 
    toLatLng(wp.position)
  ) || [];

  // Loading state
  if (isLoadingLocation) {
    return (
      <div className="drone-map">
        <div className="map-loading">
          <div className="loading-spinner">📍</div>
          <p>Getting your location...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="drone-map">
      <MapContainer
        center={baseLocation}
        zoom={17}
        style={{ height: '100%', width: '100%' }}
      >
        <TileLayer
          attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
        />

        {dronePosition && (
          <MapUpdater center={dronePosition} follow={followDrone} />
        )}

        {/* Current Location Marker */}
        {currentLocation && (
          <Marker position={currentLocation} icon={currentLocationIcon}>
            <Popup>
              <strong>📍 Your Location</strong>
              {locationError && <p style={{color: 'orange'}}>Using default location</p>}
            </Popup>
          </Marker>
        )}

        {/* Home Position */}
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

        {/* Drone Marker */}
        {dronePosition && drone && (
          <>
            <Marker position={dronePosition} icon={createDroneIcon(isFlying)}>
              <Popup>
                <strong>🚁 {drone.droneId}</strong><br />
                Status: {drone.status}<br />
                Altitude: {drone.altitudeAGL?.toFixed(1)}m<br />
                Speed: {drone.groundSpeed?.toFixed(1)}m/s<br />
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

      {/* Info Overlay */}
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

      {/* Location Error Warning */}
      {locationError && (
        <div className="location-warning">
          ⚠️ Using default location (Tel Aviv)
        </div>
      )}
    </div>
  );
};

export default DroneMap;