import { useState, useEffect, useCallback } from 'react';
import { useSignalR, DroneStateDto, FlightPathUpdate, Alert } from './useSignalR';

// Coordinate conversion
const ORIGIN_LAT = 32.0853;
const ORIGIN_LNG = 34.7818;
const METERS_PER_DEG_LAT = 111320;
const metersPerDegLng = (lat: number) => 111320 * Math.cos(lat * Math.PI / 180);

const localToLatLng = (x: number, y: number) => ({
  lat: ORIGIN_LAT + y / METERS_PER_DEG_LAT,
  lng: ORIGIN_LNG + x / metersPerDegLng(ORIGIN_LAT)
});

export interface DronePosition {
  lat: number;
  lng: number;
  altitude: number;
}

export interface DroneRealtimeState {
  droneId: string;
  position: DronePosition;
  status: string;
  speed: number;
  heading: number;
  batteryPercent: number;
  isArmed: boolean;
  flightMode: string;
}

export interface FlightPathData {
  waypoints: DronePosition[];
  distance: number;
  eta: number;
}

export const useDroneRealtime = (droneId: string) => {
  const [droneState, setDroneState] = useState<DroneRealtimeState | null>(null);
  const [flightPath, setFlightPath] = useState<FlightPathData | null>(null);
  const [alerts, setAlerts] = useState<Alert[]>([]);

  // Convert backend state to frontend format
  const handleDroneStateUpdated = useCallback((state: DroneStateDto) => {
    if (state.droneId !== droneId) return;

    const position = localToLatLng(state.position.x, state.position.y);
    
    setDroneState({
      droneId: state.droneId,
      position: {
        lat: position.lat,
        lng: position.lng,
        altitude: state.position.z
      },
      status: state.status,
      speed: state.speed,
      heading: state.heading,
      batteryPercent: state.batteryPercent,
      isArmed: state.isArmed,
      flightMode: state.flightMode
    });
  }, [droneId]);

  // Convert flight path waypoints
  const handleFlightPathUpdated = useCallback((path: FlightPathUpdate) => {
    if (path.droneId !== droneId) return;

    const waypoints = path.waypoints.map(wp => {
      const pos = localToLatLng(wp.x, wp.y);
      return { lat: pos.lat, lng: pos.lng, altitude: wp.z };
    });

    setFlightPath({
      waypoints,
      distance: path.distance,
      eta: path.eta
    });
  }, [droneId]);

  // Handle alerts
  const handleAlertReceived = useCallback((alert: Alert) => {
    if (alert.droneId !== droneId) return;
    
    setAlerts(prev => [alert, ...prev].slice(0, 10)); // Keep last 10
  }, [droneId]);

  const { 
    isConnected, 
    connectionState, 
    error,
    subscribeToDrone,
    unsubscribeFromDrone 
  } = useSignalR({
    onDroneStateUpdated: handleDroneStateUpdated,
    onFlightPathUpdated: handleFlightPathUpdated,
    onAlertReceived: handleAlertReceived
  });

  // Subscribe to drone on connect
  useEffect(() => {
    if (isConnected && droneId) {
      subscribeToDrone(droneId);
    }

    return () => {
      if (isConnected && droneId) {
        unsubscribeFromDrone(droneId);
      }
    };
  }, [isConnected, droneId, subscribeToDrone, unsubscribeFromDrone]);

  // Clear latest alert
  const clearAlerts = useCallback(() => {
    setAlerts([]);
  }, []);

  return {
    // Connection
    isConnected,
    connectionState,
    connectionError: error,
    
    // Drone data
    droneState,
    position: droneState?.position ?? null,
    
    // Flight
    flightPath,
    
    // Alerts
    alerts,
    latestAlert: alerts[0] ?? null,
    clearAlerts
  };
};