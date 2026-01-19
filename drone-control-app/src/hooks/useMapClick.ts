import { useCallback, useState } from 'react';

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

export const useMapClick = (
  droneId: string, 
  defaultAltitude: number = 30, 
  origin: { lat: number; lng: number } ={lat: 32.0853, lng: 34.7818}
  ) => {
  const [isFlying, setIsFlying] = useState(false);
  const [destination, setDestination] = useState<Destination | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [lastResult, setLastResult] = useState<GoToResult | null>(null);

  // ✅ Convert lat/lng to local XY coordinates using the provided origin
  const latLngToLocal = useCallback((lat: number, lng: number) => {
    console.log('🔄 latLngToLocal:', { lat, lng, origin });
    return {
      x: (lng - origin.lng) * metersPerDegLng(origin.lat),
      y: (lat - origin.lat) * METERS_PER_DEG_LAT
    };
  }, [origin.lat, origin.lng]);

  // Convert lat/lng to local XY coordinates (meters)
   const localToLatLng = useCallback((x: number, y: number) => ({
    lat: origin.lat + y / METERS_PER_DEG_LAT,
    lng: origin.lng + x / metersPerDegLng(origin.lat)
  }), [origin.lat, origin.lng]);
 

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