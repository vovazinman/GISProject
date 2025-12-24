/**
 * ControlButtons - כפתורי שליטה ידניים
 */

import React, { useState } from 'react';
import { 
  Power, 
  Plane, 
  ArrowDown, 
  Home, 
  AlertOctagon,
  Loader2 
} from 'lucide-react';
import { useDroneApi } from '../../hooks';
import type { DroneStatus } from '../../types';

interface ControlButtonsProps {
  droneId?: string;
  droneStatus?: DroneStatus | string;
  onAction?: (action: string, result: unknown) => void;
}

export const ControlButtons: React.FC<ControlButtonsProps> = ({ 
  droneId, 
  droneStatus,
  onAction 
}) => {
  const [altitude, setAltitude] = useState(30);
  const { loading, arm, disarm, takeoff, land, rtl, emergency } = useDroneApi();

  const handleAction = async (action: string): Promise<void> => {
    if (!droneId) return;

    let result;
    switch (action) {
      case 'arm':
        result = await arm(droneId);
        break;
      case 'disarm':
        result = await disarm(droneId);
        break;
      case 'takeoff':
        result = await takeoff(droneId, altitude);
        break;
      case 'land':
        result = await land(droneId);
        break;
      case 'rtl':
        result = await rtl(droneId);
        break;
      case 'emergency':
        result = await emergency(droneId);
        break;
    }

    if (result && onAction) {
      onAction(action, result);
    }
  };

  // מצב כפתורים
  const isFlying = ['Flying', 'Hovering', 'TakingOff'].includes(droneStatus as string);
  const isArmed = droneStatus === 'Armed' || isFlying;
  const canTakeoff = isArmed && !isFlying;
  const canLand = isFlying;

  return (
    <div className="control-buttons">
      <h3>Manual Controls</h3>

      {/* Altitude Slider */}
      <div className="altitude-control">
        <label>Takeoff Altitude: {altitude}m</label>
        <input
          type="range"
          min="10"
          max="100"
          value={altitude}
          onChange={(e) => setAltitude(Number(e.target.value))}
        />
      </div>

      {/* Buttons */}
      <div className="button-grid">
        <button
          className={`ctrl-btn ${isArmed ? 'armed' : ''}`}
          onClick={() => handleAction(isArmed ? 'disarm' : 'arm')}
          disabled={!!loading || isFlying}
        >
          {(loading === 'arm' || loading === 'disarm') ? (
            <Loader2 size={18} className="spin" />
          ) : (
            <Power size={18} />
          )}
          {isArmed ? 'Disarm' : 'Arm'}
        </button>

        <button
          className="ctrl-btn takeoff"
          onClick={() => handleAction('takeoff')}
          disabled={!!loading || !canTakeoff}
        >
          {loading === 'takeoff' ? <Loader2 size={18} className="spin" /> : <Plane size={18} />}
          Takeoff
        </button>

        <button
          className="ctrl-btn land"
          onClick={() => handleAction('land')}
          disabled={!!loading || !canLand}
        >
          {loading === 'land' ? <Loader2 size={18} className="spin" /> : <ArrowDown size={18} />}
          Land
        </button>

        <button
          className="ctrl-btn rtl"
          onClick={() => handleAction('rtl')}
          disabled={!!loading || !isFlying}
        >
          {loading === 'rtl' ? <Loader2 size={18} className="spin" /> : <Home size={18} />}
          Return Home
        </button>

        <button
          className="ctrl-btn emergency"
          onClick={() => handleAction('emergency')}
          disabled={!!loading}
        >
          {loading === 'emergency' ? <Loader2 size={18} className="spin" /> : <AlertOctagon size={18} />}
          EMERGENCY
        </button>
      </div>
    </div>
  );
};

export default ControlButtons;