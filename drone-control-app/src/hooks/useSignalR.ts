import { useState, useEffect, useCallback, useRef } from 'react';
import * as signalR from '@microsoft/signalr';
import type { DroneState, FlightPath, Vector3D, DroneStatus, FlightMode } from '../types';

// ========== SignalR DTOs (from backend) ==========

export interface DroneStateDto {
  droneId: string;
  status: string;
  flightMode: string;
  position: Vector3D;
  velocity: Vector3D;
  altitudeAGL: number;
  groundSpeed: number;
  batteryPercent: number;
  distanceFromHome: number;
  distanceTraveled: number;
  flightTimeSec: number;
  isArmed: boolean;
  currentMissionId: string | null;
  currentWaypointIndex: number;
  totalWaypoints: number;
  timestampUtc: string;
}

export interface FlightPathDto {
  droneId: string;
  waypoints: Array<{ position: Vector3D; time: number; speed: number }>;
  totalDistance: number;
  totalDuration: number;
  distance?: number;
  eta?: number;
}

export interface MissionUpdate {
  droneId: string;
  missionId: string;
  status: string;
  progress: number;
  timestamp: string;
}

export interface Alert {
  droneId: string;
  alertType: string;
  message: string;
  severity: 'info' | 'warning' | 'error' | 'critical';
  timestamp: string;
}

// ========== Mappers ==========

const mapDroneState = (dto: DroneStateDto): DroneState => ({
  droneId: dto.droneId,
  status: dto.status as DroneStatus,
  flightMode: dto.flightMode as FlightMode,
  position: dto.position,
  velocity: dto.velocity ?? { x: 0, y: 0, z: 0 },
  altitudeAGL: dto.altitudeAGL ?? dto.position.z,
  groundSpeed: dto.groundSpeed ?? 0,
  batteryPercent: dto.batteryPercent,
  distanceFromHome: dto.distanceFromHome ?? 0,
  distanceTraveled: dto.distanceTraveled ?? 0,
  flightTimeSec: dto.flightTimeSec ?? 0,
  isArmed: dto.isArmed,
  currentMissionId: dto.currentMissionId,
  currentWaypointIndex: dto.currentWaypointIndex ?? 0,
  totalWaypoints: dto.totalWaypoints ?? 0,
  timestampUtc: dto.timestampUtc ?? new Date().toISOString()
});

const mapFlightPath = (dto: FlightPathDto): FlightPath => ({
  droneId: dto.droneId,
  waypoints: dto.waypoints.map(wp => ({
    position: wp.position ?? wp,  // Handle both formats
    time: wp.time ?? 0,
    speed: wp.speed ?? 0
  })),
  totalDistance: dto.totalDistance,
  totalDuration: dto.totalDuration
});

// ========== Hook Options ==========

interface UseSignalROptions {
  hubUrl?: string;
  autoConnect?: boolean;
  onDroneStateUpdated?: (state: DroneState) => void;
  onFlightPathUpdated?: (path: FlightPath) => void;
  onMissionUpdated?: (mission: MissionUpdate) => void;
  onAlertReceived?: (alert: Alert) => void;
}

// ========== Hook ==========

export const useSignalR = (options: UseSignalROptions = {}) => {
  const {
    hubUrl = '/droneHub',
    autoConnect = true,
    onDroneStateUpdated,
    onFlightPathUpdated,
    onMissionUpdated,
    onAlertReceived
  } = options;

  const [connectionState, setConnectionState] = useState<signalR.HubConnectionState>(
    signalR.HubConnectionState.Disconnected
  );
  const [error, setError] = useState<string | null>(null);
  const connectionRef = useRef<signalR.HubConnection | null>(null);

  // Create connection
  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Information)
      .build();

    connectionRef.current = connection;

    // Connection state changes
    connection.onreconnecting((err) => {
      console.log('SignalR reconnecting...', err);
      setConnectionState(signalR.HubConnectionState.Reconnecting);
      setError('Reconnecting...');
    });

    connection.onreconnected((connectionId) => {
      console.log('SignalR reconnected:', connectionId);
      setConnectionState(signalR.HubConnectionState.Connected);
      setError(null);
    });

    connection.onclose((err) => {
      console.log('SignalR closed:', err);
      setConnectionState(signalR.HubConnectionState.Disconnected);
      if (err) setError('Connection lost');
    });

    // Auto connect
    if (autoConnect) {
      connection.start()
        .then(() => {
          console.log('SignalR connected');
          setConnectionState(signalR.HubConnectionState.Connected);
          setError(null);
        })
        .catch((err) => {
          console.error('SignalR connection error:', err);
          setError('Failed to connect');
        });
    }

    // Cleanup
    return () => {
      connection.stop();
    };
  }, [hubUrl, autoConnect]);

  // Update event handlers when callbacks change
  useEffect(() => {
    const connection = connectionRef.current;
    if (!connection) return;

    // Remove old handlers
    connection.off('DroneStateUpdated');
    connection.off('FlightPathUpdated');
    connection.off('MissionUpdated');
    connection.off('AlertReceived');

    // Add new handlers with mapping
    connection.on('DroneStateUpdated', (dto: DroneStateDto) => {
      const mapped = mapDroneState(dto);
      onDroneStateUpdated?.(mapped);
    });

    connection.on('FlightPathUpdated', (dto: FlightPathDto) => {
      const mapped = mapFlightPath(dto);
      onFlightPathUpdated?.(mapped);
    });

    connection.on('MissionUpdated', (mission: MissionUpdate) => {
      onMissionUpdated?.(mission);
    });

    connection.on('AlertReceived', (alert: Alert) => {
      onAlertReceived?.(alert);
    });
  }, [onDroneStateUpdated, onFlightPathUpdated, onMissionUpdated, onAlertReceived]);

  // Manual connect
  const connect = useCallback(async () => {
    const connection = connectionRef.current;
    if (!connection || connection.state === signalR.HubConnectionState.Connected) return;

    try {
      await connection.start();
      setConnectionState(signalR.HubConnectionState.Connected);
      setError(null);
    } catch (err) {
      setError('Failed to connect');
    }
  }, []);

  // Manual disconnect
  const disconnect = useCallback(async () => {
    const connection = connectionRef.current;
    if (!connection) return;

    await connection.stop();
    setConnectionState(signalR.HubConnectionState.Disconnected);
  }, []);

  // Subscribe to drone group
  const subscribeToDrone = useCallback(async (droneId: string) => {
    const connection = connectionRef.current;
    if (!connection || connection.state !== signalR.HubConnectionState.Connected) return;

    try {
      await connection.invoke('SubscribeToDrone', droneId);
      console.log(`Subscribed to drone: ${droneId}`);
    } catch (err) {
      console.error('Failed to subscribe:', err);
    }
  }, []);

  // Unsubscribe from drone group
  const unsubscribeFromDrone = useCallback(async (droneId: string) => {
    const connection = connectionRef.current;
    if (!connection || connection.state !== signalR.HubConnectionState.Connected) return;

    try {
      await connection.invoke('UnsubscribeFromDrone', droneId);
    } catch (err) {
      console.error('Failed to unsubscribe:', err);
    }
  }, []);

  return {
    connectionState,
    isConnected: connectionState === signalR.HubConnectionState.Connected,
    error,
    connect,
    disconnect,
    subscribeToDrone,
    unsubscribeFromDrone
  };
};