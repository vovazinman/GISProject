/**
 * App - Main Application Component
 */
import 'leaflet/dist/leaflet.css';
import React, { useState, useEffect, useCallback } from 'react';
import { Dashboard } from './components';
import { useSignalR } from './hooks';
import { droneApi } from './services/api';
import type { DroneState, FlightPath, ChatResponse } from './types';

const App: React.FC = () => {
  const [drone, setDrone] = useState<DroneState | null>(null);  
  const [error, setError] = useState<string | null>(null);

  // SignalR - now receives mapped types directly!
  const { isConnected } = useSignalR({
    onDroneStateUpdated: (state: DroneState) => {
      console.log('Drone state updated:', state);
      setDrone(state);  // ✅ Already correct type
    },
    onFlightPathUpdated: (path: FlightPath) => {
      console.log('Flight path updated:', path);  
    },
    onAlertReceived: (alert) => {
      console.log('Alert:', alert);
    }
  });

  // Load drone
  useEffect(() => {
    loadDrone();
  }, []);

  const loadDrone = async (): Promise<void> => {
    try {
      const drones = await droneApi.getAll();
      
      if (drones.length > 0) {
        setDrone(drones[0]);
      } else {
        const newDrone = await droneApi.create({
          id: 'drone-1',
          specsType: 'mavic3',
          x: 0, y: 0, z: 0
        });
        setDrone(newDrone);
      }
    } catch (err) {
      const error = err as Error;
      console.error('Failed to load drone:', error);
      setError(error.message);
    }
  };

  const handleCommandExecuted = useCallback((response: ChatResponse) => {
    console.log('Command executed:', response);
  }, []);

  if (error) {
    return (
      <div className="error-screen">
        <h2>⚠️ Error</h2>
        <p>{error}</p>
        <button onClick={() => { setError(null); loadDrone(); }}>
          Retry
        </button>
      </div>
    );
  }

  return (
    <Dashboard
      drone={drone}
      isConnected={isConnected}
      onCommandExecuted={handleCommandExecuted}
    />
  );
};

export default App;