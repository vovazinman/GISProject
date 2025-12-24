/**
 * useSignalR - Custom Hook לחיבור Real-time
 * 
 * מתחבר ל-SignalR Hub ומקשיב לאירועים.
 * TypeScript מבטיח שה-handlers מקבלים את הטיפוסים הנכונים.
 */

import { useEffect, useRef, useState, useCallback } from 'react';
import * as signalR from '@microsoft/signalr';
import type { DroneState, FlightPath } from '../types';

// ========== Types ==========

interface SignalRHandlers {
  onDroneStateUpdated?: (state: DroneState) => void;
  onFlightPathUpdated?: (path: FlightPath) => void;
  onReceiveMessage?: (user: string, message: string) => void;
  onMissionUpdated?: (data: { missionId: string; status: string; progress: number }) => void;
  onAlertReceived?: (data: { droneId: string; alertType: string; message: string }) => void;
  onChatChunk?: (chunk: string) => void;
}

interface UseSignalRReturn {
  connection: signalR.HubConnection | null;
  isConnected: boolean;
  connectionError: string | null;
  subscribeToDrone: (droneId: string) => Promise<void>;
  unsubscribeFromDrone: (droneId: string) => Promise<void>;
}

// ========== Hook ==========

export function useSignalR(handlers: SignalRHandlers = {}): UseSignalRReturn {
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const [isConnected, setIsConnected] = useState(false);
  const [connectionError, setConnectionError] = useState<string | null>(null);

  useEffect(() => {
    // יצירת החיבור
    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/droneHub')
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Information)
      .build();

    connectionRef.current = connection;

    // רישום לאירועים
    if (handlers.onDroneStateUpdated) {
      connection.on('DroneStateUpdated', handlers.onDroneStateUpdated);
    }

    if (handlers.onFlightPathUpdated) {
      connection.on('FlightPathUpdated', handlers.onFlightPathUpdated);
    }

    if (handlers.onReceiveMessage) {
      connection.on('ReceiveMessage', handlers.onReceiveMessage);
    }

    if (handlers.onMissionUpdated) {
      connection.on('MissionUpdated', handlers.onMissionUpdated);
    }

    if (handlers.onAlertReceived) {
      connection.on('AlertReceived', handlers.onAlertReceived);
    }

    if (handlers.onChatChunk) {
      connection.on('ChatChunk', handlers.onChatChunk);
    }

    // מצבי חיבור
    connection.onclose((error) => {
      console.log('SignalR: Disconnected', error);
      setIsConnected(false);
      setConnectionError(error?.message || 'Disconnected');
    });

    connection.onreconnecting((error) => {
      console.log('SignalR: Reconnecting...', error);
      setIsConnected(false);
    });

    connection.onreconnected((connectionId) => {
      console.log('SignalR: Reconnected!', connectionId);
      setIsConnected(true);
      setConnectionError(null);
    });

    // התחברות
    const startConnection = async () => {
      try {
        await connection.start();
        console.log('SignalR: Connected!');
        setIsConnected(true);
        setConnectionError(null);
      } catch (err) {
        const error = err as Error;
        console.error('SignalR: Connection failed', error);
        setConnectionError(error.message);
        setIsConnected(false);
        setTimeout(startConnection, 5000);
      }
    };

    startConnection();

    // ניקוי
    return () => {
      connection.stop();
    };
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  // מתודות
  const subscribeToDrone = useCallback(async (droneId: string) => {
    if (connectionRef.current?.state === signalR.HubConnectionState.Connected) {
      await connectionRef.current.invoke('SubscribeToDrone', droneId);
    }
  }, []);

  const unsubscribeFromDrone = useCallback(async (droneId: string) => {
    if (connectionRef.current?.state === signalR.HubConnectionState.Connected) {
      await connectionRef.current.invoke('UnsubscribeFromDrone', droneId);
    }
  }, []);

  return {
    connection: connectionRef.current,
    isConnected,
    connectionError,
    subscribeToDrone,
    unsubscribeFromDrone
  };
}

export default useSignalR;