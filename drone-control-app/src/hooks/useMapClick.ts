import { useCallback, useState } from 'react';

// Origin point - adjust to your simulation area
const ORIGIN_LAT = 32.0853;  // Tel Aviv
const ORIGIN_LNG = 34.7818;

// Conversion constants
const METERS_PER_DEG_LAT = 111320;
const metersPerDegLng = (lat: number) => 111320 * Math.cos(lat * Math.PI / 180);

export type FlightMode = 'Direct' | 'Safe';

export interface GoToResult {
  success: boolean;
  droneId: string;
  destination: { x: number; y: number; z: number };
  distance: number;
  eta: number;
  mode: string;
  message?: string;
}

export interface Destination {
  lat: number;
  lng: number;
}

export const useMapClick = (droneId: string, defaultAltitude: number = 30) => {
  const [isFlying, setIsFlying] = useState(false);
  const [destination, setDestination] = useState<Destination | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [lastResult, setLastResult] = useState<GoToResult | null>(null);

  // Convert lat/lng to local XY coordinates (meters)
  const latLngToLocal = useCallback((lat: number, lng: number) => ({
    x: (lng - ORIGIN_LNG) * metersPerDegLng(ORIGIN_LAT),
    y: (lat - ORIGIN_LAT) * METERS_PER_DEG_LAT
  }), []);

  // Convert local XY (meters) to lat/lng
  const localToLatLng = useCallback((x: number, y: number) => ({
    lat: ORIGIN_LAT + y / METERS_PER_DEG_LAT,
    lng: ORIGIN_LNG + x / metersPerDegLng(ORIGIN_LAT)
  }), []);

  // Fly to a location
  const flyTo = useCallback(async (
    lat: number,
    lng: number,
    altitude: number = defaultAltitude,
    speed: number = 15,
    mode: FlightMode = 'Safe'
  ): Promise<GoToResult | null> => {
    console.log('🚀 flyTo called:', { droneId, lat, lng, altitude, speed, mode });
    
    setIsFlying(true);
    setError(null);
    setDestination({ lat, lng });

    try {
      const { x, y } = latLngToLocal(lat, lng);

      const response = await fetch(`/api/drone/${droneId}/goto`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ x, y, z: altitude, speed, mode })
      });

      const data = await response.json();

      if (!response.ok) {
        throw new Error(data.error || data.message || 'Failed to send goto command');
      }

      setLastResult(data);
      return data;

    } catch (err) {
      const message = err instanceof Error ? err.message : 'Unknown error';
      setError(message);
      setDestination(null);
      return null;

    } finally {
      setIsFlying(false);
    }
  }, [droneId, defaultAltitude, latLngToLocal]);

  // Clear destination and reset state
  const clearDestination = useCallback(() => {
    setDestination(null);
    setLastResult(null);
    setError(null);
  }, []);

  return {
    // Actions
    flyTo,
    clearDestination,
    
    // Converters
    latLngToLocal,
    localToLatLng,
    
    // State
    isFlying,
    destination,
    error,
    lastResult
  };
};