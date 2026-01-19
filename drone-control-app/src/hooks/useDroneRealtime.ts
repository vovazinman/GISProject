import { useState, useEffect, useCallback } from 'react';
import { useSignalR, DroneStateDto, FlightPathDto, Alert } from './useSignalR';

// Coordinate conversion
const METERS_PER_DEG_LAT = 111320;
const metersPerDegLng = (lat: number) => 111320 * Math.cos(lat * Math.PI / 180);

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

export const useDroneRealtime = (
  droneId: string, 
  origin: { lat: number; lng: number } = { lat: 32.0853, lng: 34.7818 }
  ) => {
  const [droneState, setDroneState] = useState<DroneRealtimeState | null>(null);
  const [flightPath, setFlightPath] = useState<FlightPathData | null>(null);
  const [alerts, setAlerts] = useState<Alert[]>([]);

  // Convert local XY (meters) to lat/lng using origin
  const localToLatLng = useCallback((x: number, y: number) => ({
    lat: origin.lat + y / METERS_PER_DEG_LAT,
    lng: origin.lng + x / metersPerDegLng(origin.lat)
  }), [origin.lat, origin.lng]);

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
      speed: state.groundSpeed ?? 0,        
      heading: state.velocity?.x !== undefined 
        ? Math.atan2(state.velocity.y, state.velocity.x) * 180 / Math.PI 
        : 0,                                 
      batteryPercent: state.batteryPercent,
      isArmed: state.isArmed,
      flightMode: state.flightMode
    });
  }, [droneId, localToLatLng]);

  // Convert flight path waypoints
  const handleFlightPathUpdated = useCallback((path: FlightPathDto) => { 
    if (path.droneId !== droneId) return;

    const waypoints = path.waypoints.map((wp: { position: { x: number; y: number; z: number } }) => {  // ✅ תוקן
      const pos = localToLatLng(wp.position.x, wp.position.y);
      return { lat: pos.lat, lng: pos.lng, altitude: wp.position.z };
    });

    setFlightPath({
      waypoints,
      distance: path.distance ?? path.totalDistance,
      eta: path.eta ?? path.totalDuration
    });
  }, [droneId, localToLatLng]);

  // Handle alerts
  const handleAlertReceived = useCallback((alert: Alert) => {
    if (alert.droneId !== droneId) return;
    
    setAlerts(prev => [alert, ...prev].slice(0, 10));
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

  // Clear alerts
  const clearAlerts = useCallback(() => {
    setAlerts([]);
  }, []);

  return {
    isConnected,
    connectionState,
    connectionError: error,
    droneState,
    position: droneState?.position ?? null,
    flightPath,
    alerts,
    latestAlert: alerts[0] ?? null,
    clearAlerts
  };
};