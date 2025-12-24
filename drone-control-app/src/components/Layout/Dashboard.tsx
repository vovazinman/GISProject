/**
 * Dashboard - פריסה ראשית
 */

import React from 'react';
import { DroneMap } from '../Map';
import { DroneStatus, ControlButtons } from '../Drone';
import { ChatPanel } from '../Chat';
import type { DroneState, FlightPath, ChatResponse } from '../../types';

interface DashboardProps {
  drone: DroneState | null;
  flightPath?: FlightPath | null;
  isConnected: boolean;
  onCommandExecuted?: (response: ChatResponse) => void;
}

export const Dashboard: React.FC<DashboardProps> = ({ 
  drone, 
  flightPath, 
  isConnected,
  onCommandExecuted 
}) => {
  return (
    <div className="dashboard">
      {/* Header */}
      <header className="dashboard-header">
        <div className="header-left">
          <h1>🚁 Drone Control Center</h1>
        </div>
        <div className="header-right">
          <span className={`connection-status ${isConnected ? 'connected' : 'disconnected'}`}>
            <span className="status-dot"></span>
            {isConnected ? 'Connected' : 'Disconnected'}
          </span>
        </div>
      </header>

      {/* Main */}
      <main className="dashboard-main">
        {/* Map */}
        <section className="map-section">
          <DroneMap drone={drone} flightPath={flightPath} />
        </section>

        {/* Side Panel */}
        <aside className="side-panel">
          <DroneStatus drone={drone} />
          <ControlButtons 
            droneId={drone?.droneId}
            droneStatus={drone?.status}
          />
          <ChatPanel 
            droneId={drone?.droneId}
            onCommandExecuted={onCommandExecuted}
          />
        </aside>
      </main>
    </div>
  );
};

export default Dashboard;