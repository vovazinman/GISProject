/**
 * DroneStatus - פאנל סטטוס הרחפן
 */

import React from 'react';
import { 
  Battery, 
  Navigation, 
  Gauge, 
  MapPin, 
  Clock,
  Wifi,
  AlertTriangle
} from 'lucide-react';
import type { DroneState, DroneStatus as DroneStatusEnum } from '../../types';

interface DroneStatusProps {
  drone: DroneState | null;
}

export const DroneStatus: React.FC<DroneStatusProps> = ({ drone }) => {
  if (!drone) {
    return (
      <div className="drone-status empty">
        <div className="empty-message">
          <Navigation size={48} />
          <p>No drone selected</p>
        </div>
      </div>
    );
  }

  const getBatteryColor = (percent: number): string => {
    if (percent > 50) return 'var(--color-success)';
    if (percent > 20) return 'var(--color-warning)';
    return 'var(--color-danger)';
  };

  const getStatusColor = (status: DroneStatusEnum | string): string => {
    const colors: Record<string, string> = {
      'Flying': 'var(--color-success)',
      'Hovering': 'var(--color-success)',
      'TakingOff': 'var(--color-primary)',
      'Landing': 'var(--color-warning)',
      'Armed': 'var(--color-warning)',
      'Ready': 'var(--color-muted)',
      'Landed': 'var(--color-muted)',
      'Emergency': 'var(--color-danger)',
      'Error': 'var(--color-danger)'
    };
    return colors[status] || 'var(--color-muted)';
  };

  const formatFlightTime = (seconds: number): string => {
    const mins = Math.floor(seconds / 60);
    const secs = Math.floor(seconds % 60);
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  };

  return (
    <div className="drone-status">
      <div className="status-header">
        <h3>
          <Navigation size={20} />
          {drone.droneId}
        </h3>
        <span 
          className="status-badge"
          style={{ backgroundColor: getStatusColor(drone.status) }}
        >
          {drone.status}
        </span>
      </div>

      <div className="status-grid">
        {/* Battery */}
        <div className="status-item">
          <div className="item-icon">
            <Battery size={18} style={{ color: getBatteryColor(drone.batteryPercent) }} />
          </div>
          <div className="item-content">
            <span className="item-value">{drone.batteryPercent?.toFixed(0)}%</span>
            <span className="item-label">Battery</span>
          </div>
          <div 
            className="battery-bar"
            style={{ 
              width: `${drone.batteryPercent}%`,
              backgroundColor: getBatteryColor(drone.batteryPercent)
            }}
          />
        </div>

        {/* Altitude */}
        <div className="status-item">
          <div className="item-icon"><Navigation size={18} /></div>
          <div className="item-content">
            <span className="item-value">{drone.altitudeAGL?.toFixed(1)} m</span>
            <span className="item-label">Altitude</span>
          </div>
        </div>

        {/* Speed */}
        <div className="status-item">
          <div className="item-icon"><Gauge size={18} /></div>
          <div className="item-content">
            <span className="item-value">{drone.groundSpeed?.toFixed(1)} m/s</span>
            <span className="item-label">Speed</span>
          </div>
        </div>

        {/* Flight Time */}
        <div className="status-item">
          <div className="item-icon"><Clock size={18} /></div>
          <div className="item-content">
            <span className="item-value">{formatFlightTime(drone.flightTimeSec || 0)}</span>
            <span className="item-label">Flight Time</span>
          </div>
        </div>

        {/* Distance from Home */}
        <div className="status-item">
          <div className="item-icon"><MapPin size={18} /></div>
          <div className="item-content">
            <span className="item-value">{drone.distanceFromHome?.toFixed(0)} m</span>
            <span className="item-label">From Home</span>
          </div>
        </div>

        {/* Waypoint */}
        {drone.totalWaypoints > 0 && (
          <div className="status-item">
            <div className="item-icon"><Wifi size={18} /></div>
            <div className="item-content">
              <span className="item-value">
                {drone.currentWaypointIndex + 1} / {drone.totalWaypoints}
              </span>
              <span className="item-label">Waypoint</span>
            </div>
          </div>
        )}
      </div>

      {/* Position */}
      <div className="position-display">
        <span className="position-label">Position:</span>
        <code>
          X: {drone.position?.x?.toFixed(1)} | 
          Y: {drone.position?.y?.toFixed(1)} | 
          Z: {drone.position?.z?.toFixed(1)}
        </code>
      </div>

      {/* Low Battery Warning */}
      {drone.batteryPercent < 20 && (
        <div className="status-warning">
          <AlertTriangle size={16} />
          <span>Low battery! Return to home recommended.</span>
        </div>
      )}
    </div>
  );
};

export default DroneStatus;