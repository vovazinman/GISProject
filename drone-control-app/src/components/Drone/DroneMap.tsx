import { useState, useEffect } from 'react';
import { MapContainer, TileLayer, Marker, Polyline, Popup, Circle, useMapEvents, useMap } from 'react-leaflet';
import L from 'leaflet';
import { useMapClick, FlightMode } from '../../hooks/useMapClick';
import { useDroneRealtime } from '../../hooks/useDroneRealtime';

// ========== Icons ==========

// Rotatable drone icon
const createDroneIcon = (heading: number = 0) => L.divIcon({
  className: '',
  html: `
    <div style="transform: rotate(${heading}deg); width: 32px; height: 32px;">
      <img src="https://cdn-icons-png.flaticon.com/512/2169/2169325.png" 
           width="32" height="32" 
           style="filter: drop-shadow(0 2px 3px rgba(0,0,0,0.3));"/>
    </div>
  `,
  iconSize: [32, 32],
  iconAnchor: [16, 16],
  popupAnchor: [0, -16]
});

// Destination marker icon
const destinationIcon = new L.Icon({
  iconUrl: 'https://cdn-icons-png.flaticon.com/512/684/684908.png',
  iconSize: [32, 32],
  iconAnchor: [16, 32],
  popupAnchor: [0, -32]
});

// ========== Types ==========

interface Props {
  droneId: string;
  initialPosition?: { lat: number; lng: number };
}

// ========== Helper Components ==========

// Auto-follow drone
function FollowDrone({ position, follow }: { position: { lat: number; lng: number } | null; follow: boolean }) {
  const map = useMap();
  
  useEffect(() => {
    if (follow && position) {
      map.panTo([position.lat, position.lng], { animate: true });
    }
  }, [map, position, follow]);
  
  return null;
}

// Map click handler - SIMPLE VERSION
function ClickHandler({ onMapClick }: { onMapClick: (lat: number, lng: number) => void }) {
  debugger; 

  useMapEvents({
    click: (e) => {
       debugger;
      console.log('👆 CLICK!', e.latlng);
      onMapClick(e.latlng.lat, e.latlng.lng);
    }
  });
  return null;
}

// ========== Main Component ==========

export function DroneMap({ droneId, initialPosition }: Props) {
  // Flight parameters
  const [altitude, setAltitude] = useState(30);
  const [speed, setSpeed] = useState(15);
  const [mode, setMode] = useState<FlightMode>('Safe');
  const [followDrone, setFollowDrone] = useState(true);

  // Real-time data from SignalR
  const { 
    isConnected,
    droneState,
    position: realtimePosition,
    flightPath: realtimePath,
    latestAlert,
    clearAlerts
  } = useDroneRealtime(droneId);

  // Map click hook
  const { 
    flyTo, 
    clearDestination,
    isFlying, 
    destination, 
    error, 
    lastResult 
  } = useMapClick(droneId, altitude);

  // Use realtime position or initial position
  const dronePosition = realtimePosition 
    ? { lat: realtimePosition.lat, lng: realtimePosition.lng }
    : initialPosition ?? { lat: 32.0853, lng: 34.7818 };

  const handleMapClick = async (lat: number, lng: number) => {
  console.log('🎯 handleMapClick called!', { lat, lng });  // ← הוסף
  setFollowDrone(false);
  const result = await flyTo(lat, lng, altitude, speed, mode);
  console.log('✈️ Fly result:', result);
  if (result) {
    console.log('Flying to:', result);
  }
};

  // Build flight path for display
  const displayPath: [number, number][] = realtimePath 
    ? realtimePath.waypoints.map(wp => [wp.lat, wp.lng] as [number, number])
    : destination 
      ? [[dronePosition.lat, dronePosition.lng], [destination.lat, destination.lng]]
      : [];

  return (
    <div style={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
      {/* Connection Status */}
      <div style={styles.connectionBar}>
        <span style={{ 
          color: isConnected ? '#22c55e' : '#ef4444',
          fontWeight: 500 
        }}>
          {isConnected ? '🟢 Connected' : '🔴 Disconnected'}
        </span>
        
        {droneState && (
          <>
            <span>Status: <strong>{droneState.status}</strong></span>
            <span>🔋 {droneState.batteryPercent.toFixed(0)}%</span>
            <span>⚡ {droneState.speed.toFixed(1)} m/s</span>
            <span>🧭 {droneState.heading.toFixed(0)}°</span>
            <span>📍 Alt: {droneState.position.altitude.toFixed(1)}m</span>
          </>
        )}
      </div>

      {/* Alert Banner */}
      {latestAlert && (
        <div style={{
          ...styles.alertBar,
          background: latestAlert.severity === 'critical' ? '#fee2e2' : 
                      latestAlert.severity === 'error' ? '#fef2f2' :
                      latestAlert.severity === 'warning' ? '#fffbeb' : '#eff6ff'
        }}>
          <span>
            {latestAlert.severity === 'critical' ? '🚨' : 
             latestAlert.severity === 'error' ? '❌' :
             latestAlert.severity === 'warning' ? '⚠️' : 'ℹ️'}
            {' '}{latestAlert.message}
          </span>
          <button onClick={clearAlerts} style={styles.dismissButton}>✕</button>
        </div>
      )}

      {/* Control Panel */}
      <div style={styles.controlPanel}>
        <div style={styles.controlGroup}>
          <label style={styles.label}>
            🎚️ Alt:
            <input 
              type="number" 
              value={altitude} 
              onChange={(e) => setAltitude(Number(e.target.value))}
              min={5} max={500} step={5}
              style={styles.input}
            /> m
          </label>

          <label style={styles.label}>
            ⚡ Speed:
            <input 
              type="number" 
              value={speed} 
              onChange={(e) => setSpeed(Number(e.target.value))}
              min={1} max={50} step={1}
              style={styles.input}
            /> m/s
          </label>

          <label style={styles.label}>
            🛤️
            <select 
              value={mode} 
              onChange={(e) => setMode(e.target.value as FlightMode)}
              style={styles.select}
            >
              <option value="Safe">Safe</option>
              <option value="Direct">Direct</option>
            </select>
          </label>

          <label style={styles.checkboxLabel}>
            <input 
              type="checkbox" 
              checked={followDrone}
              onChange={(e) => setFollowDrone(e.target.checked)}
            />
            Follow Drone
          </label>
        </div>

        <div style={styles.statusGroup}>
          {isFlying && <span style={styles.statusFlying}>⏳ Sending...</span>}
          {error && <span style={styles.statusError}>❌ {error}</span>}
          {destination && !isFlying && (
            <button onClick={clearDestination} style={styles.clearButton}>
              ✕ Clear
            </button>
          )}
        </div>
      </div>

      {/* Flight Info */}
      {(lastResult || realtimePath) && (
        <div style={styles.infoBar}>
          <span>✈️ <strong>In Flight</strong></span>
          <span>📏 {(realtimePath?.distance ?? lastResult?.distance ?? 0).toFixed(0)}m</span>
          <span>⏱️ ETA: {(realtimePath?.eta ?? lastResult?.eta ?? 0).toFixed(0)}s</span>
        </div>
      )}

      {/* Map */}
      <MapContainer 
        center={[dronePosition.lat, dronePosition.lng]} 
        zoom={17} 
        style={{ flex: 1, minHeight: '400px' }}
      >
        <TileLayer 
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
          attribution='&copy; OpenStreetMap'
        />
        
        <ClickHandler onMapClick={handleMapClick} />
        <FollowDrone position={dronePosition} follow={followDrone} />
        
        {/* Drone Marker with rotation */}
        <Marker 
          position={[dronePosition.lat, dronePosition.lng]} 
          icon={createDroneIcon(droneState?.heading ?? 0)}
        >
          <Popup>
            <strong>🚁 {droneId}</strong><br/>
            Status: {droneState?.status ?? 'Unknown'}<br/>
            Battery: {droneState?.batteryPercent?.toFixed(0) ?? '--'}%<br/>
            Altitude: {droneState?.position?.altitude?.toFixed(1) ?? '--'}m
          </Popup>
        </Marker>

        {/* Accuracy circle */}
        <Circle 
          center={[dronePosition.lat, dronePosition.lng]}
          radius={3}
          pathOptions={{ color: '#3b82f6', fillColor: '#3b82f6', fillOpacity: 0.3 }}
        />

        {/* Destination Marker */}
        {destination && (
          <Marker position={[destination.lat, destination.lng]} icon={destinationIcon}>
            <Popup>
              <strong>📍 Destination</strong><br/>
              Distance: {lastResult?.distance?.toFixed(0) ?? '--'}m<br/>
              ETA: {lastResult?.eta?.toFixed(0) ?? '--'}s
            </Popup>
          </Marker>
        )}

        {/* Flight Path */}
        {displayPath.length >= 2 && (
          <Polyline 
            positions={displayPath}
            pathOptions={{ 
              color: mode === 'Safe' ? '#22c55e' : '#3b82f6',
              weight: 3,
              dashArray: '8, 8',
              opacity: 0.8
            }}
          />
        )}
      </MapContainer>

      {/* Instructions */}
      <div style={styles.instructions}>
        💡 Click on map to fly drone • Real-time updates {isConnected ? 'active' : 'inactive'}
      </div>
    </div>
  );
}

// ========== Styles ==========

const styles: Record<string, React.CSSProperties> = {
  connectionBar: {
    padding: '8px 16px',
    background: '#1e293b',
    color: 'white',
    display: 'flex',
    gap: '20px',
    fontSize: '13px',
    alignItems: 'center'
  },
  alertBar: {
    padding: '10px 16px',
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    fontSize: '14px'
  },
  dismissButton: {
    background: 'transparent',
    border: 'none',
    cursor: 'pointer',
    fontSize: '16px',
    opacity: 0.6
  },
  controlPanel: {
    padding: '10px 16px',
    background: '#f8fafc',
    borderBottom: '1px solid #e2e8f0',
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    gap: '12px',
    flexWrap: 'wrap'
  },
  controlGroup: {
    display: 'flex',
    gap: '16px',
    alignItems: 'center',
    flexWrap: 'wrap'
  },
  label: {
    display: 'flex',
    alignItems: 'center',
    gap: '6px',
    fontSize: '13px'
  },
  checkboxLabel: {
    display: 'flex',
    alignItems: 'center',
    gap: '4px',
    fontSize: '13px',
    cursor: 'pointer'
  },
  input: {
    width: '55px',
    padding: '4px 6px',
    border: '1px solid #d1d5db',
    borderRadius: '4px',
    fontSize: '13px'
  },
  select: {
    padding: '4px 8px',
    border: '1px solid #d1d5db',
    borderRadius: '4px',
    fontSize: '13px'
  },
  statusGroup: {
    display: 'flex',
    alignItems: 'center',
    gap: '10px'
  },
  statusFlying: { color: '#3b82f6', fontSize: '13px' },
  statusError: { color: '#ef4444', fontSize: '13px' },
  clearButton: {
    padding: '4px 10px',
    background: '#fee2e2',
    color: '#dc2626',
    border: 'none',
    borderRadius: '4px',
    cursor: 'pointer',
    fontSize: '12px'
  },
  infoBar: {
    padding: '8px 16px',
    background: '#ecfdf5',
    display: 'flex',
    gap: '20px',
    fontSize: '13px',
    color: '#065f46'
  },
  instructions: {
    padding: '8px 16px',
    background: '#fefce8',
    fontSize: '12px',
    color: '#854d0e',
    textAlign: 'center'
  }
};