/**
 * Dashboard - פריסה ראשית
 */

import React from 'react';
import { DroneMap } from '../Drone/DroneMap';
import { DroneStatus, ControlButtons } from '../Drone';
import { ChatPanel } from '../Chat';
import type { DroneState, ChatResponse } from '../../types';

interface DashboardProps {
  drone: DroneState | null; 
  isConnected: boolean;
  onCommandExecuted?: (response: ChatResponse) => void;
}

export const Dashboard: React.FC<DashboardProps> = ({ 
  drone,   
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
          <DroneMap 
                droneId={drone?.droneId ?? 'drone-1'} 
                initialPosition={drone?.position ? 
                  {
                    lat: 32.0853 + (drone.position.y / 111320),
                    lng: 34.7818 + (drone.position.x / (111320 * Math.cos(32.0853 * Math.PI / 180)))
                    } : undefined}
/>
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