/**
 * ControlButtons - Manual drone control buttons
 * Includes Emergency and Reset functionality
 */

import React, { useState } from 'react';
import { 
  Power, 
  Plane, 
  ArrowDown, 
  Home, 
  AlertOctagon,
  RefreshCw,
  Loader2 
} from 'lucide-react';
import { droneApi } from '../../services/api';
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
  const [loading, setLoading] = useState<string | null>(null);

  const handleAction = async (action: string): Promise<void> => {
    if (!droneId || loading) return;

    setLoading(action);
    
    try {
      let result;
      switch (action) {
        case 'arm':
          result = await droneApi.arm(droneId);
          break;
        case 'disarm':
          result = await droneApi.disarm(droneId);
          break;
        case 'takeoff':
          result = await droneApi.takeoff(droneId, altitude);
          break;
        case 'land':
          result = await droneApi.land(droneId);
          break;
        case 'rtl':
          result = await droneApi.rtl(droneId);
          break;
        case 'emergency':
          result = await droneApi.emergency(droneId);
          break;
        case 'reset':
          result = await droneApi.reset(droneId);
          break;
      }

      if (result && onAction) {
        onAction(action, result);
      }
    } catch (error) {
      console.error(`Action ${action} failed:`, error);
    } finally {
      setLoading(null);
    }
  };

  // Button states
  const isFlying = ['Flying', 'Hovering', 'TakingOff'].includes(droneStatus as string);
  const isArmed = droneStatus === 'Armed' || isFlying;
  const isEmergency = droneStatus === 'Emergency';
  const canTakeoff = isArmed && !isFlying && !isEmergency;
  const canLand = isFlying;

  return (
    <div className="control-buttons">
      <h3>Manual Controls</h3>

      {/* Emergency State Warning */}
      {isEmergency && (
        <div className="emergency-warning">
          ⚠️ EMERGENCY MODE - Press Reset to recover
        </div>
      )}

      {/* Altitude Slider */}
      <div className="altitude-control">
        <label>Takeoff Altitude: {altitude}m</label>
        <input
          type="range"
          min="10"
          max="100"
          value={altitude}
          onChange={(e) => setAltitude(Number(e.target.value))}
          disabled={isEmergency}
        />
      </div>

      {/* Control Buttons */}
      <div className="button-grid">
        {/* Arm/Disarm */}
        <button
          className={`ctrl-btn ${isArmed ? 'armed' : ''}`}
          onClick={() => handleAction(isArmed ? 'disarm' : 'arm')}
          disabled={!!loading || isFlying || isEmergency}
        >
          {(loading === 'arm' || loading === 'disarm') ? (
            <Loader2 size={18} className="spin" />
          ) : (
            <Power size={18} />
          )}
          {isArmed ? 'Disarm' : 'Arm'}
        </button>

        {/* Takeoff */}
        <button
          className="ctrl-btn takeoff"
          onClick={() => handleAction('takeoff')}
          disabled={!!loading || !canTakeoff}
        >
          {loading === 'takeoff' ? <Loader2 size={18} className="spin" /> : <Plane size={18} />}
          Takeoff
        </button>

        {/* Land */}
        <button
          className="ctrl-btn land"
          onClick={() => handleAction('land')}
          disabled={!!loading || !canLand}
        >
          {loading === 'land' ? <Loader2 size={18} className="spin" /> : <ArrowDown size={18} />}
          Land
        </button>

        {/* Return Home */}
        <button
          className="ctrl-btn rtl"
          onClick={() => handleAction('rtl')}
          disabled={!!loading || !isFlying}
        >
          {loading === 'rtl' ? <Loader2 size={18} className="spin" /> : <Home size={18} />}
          Return Home
        </button>

        {/* Emergency - only when NOT in emergency state */}
        {!isEmergency && (
          <button
            className="ctrl-btn emergency"
            onClick={() => handleAction('emergency')}
            disabled={!!loading}
          >
            {loading === 'emergency' ? <Loader2 size={18} className="spin" /> : <AlertOctagon size={18} />}
            EMERGENCY
          </button>
        )}

        {/* Reset - only when IN emergency state */}
        {isEmergency && (
          <button
            className="ctrl-btn reset"
            onClick={() => handleAction('reset')}
            disabled={!!loading}
          >
            {loading === 'reset' ? <Loader2 size={18} className="spin" /> : <RefreshCw size={18} />}
            RESET EMERGENCY
          </button>
        )}
      </div>
    </div>
  );
};

export default ControlButtons;