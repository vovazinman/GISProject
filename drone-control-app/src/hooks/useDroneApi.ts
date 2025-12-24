/**
 * useDroneApi - Hook לפעולות על רחפן
 * 
 * מספק ממשק נוח לשליטה ברחפן עם loading states.
 */

import { useState, useCallback } from 'react';
import { droneApi } from '../services/api';
import type { DroneState, CommandResponse, GoToRequest } from '../types';

interface UseDroneApiReturn {
  loading: string | null;  // איזה פעולה בטעינה
  error: string | null;
  
  // פעולות
  arm: (droneId: string) => Promise<CommandResponse | null>;
  disarm: (droneId: string) => Promise<CommandResponse | null>;
  takeoff: (droneId: string, altitude?: number) => Promise<CommandResponse | null>;
  land: (droneId: string) => Promise<CommandResponse | null>;
  rtl: (droneId: string) => Promise<CommandResponse | null>;
  emergency: (droneId: string) => Promise<CommandResponse | null>;
  goTo: (droneId: string, request: GoToRequest) => Promise<CommandResponse | null>;
  
  // שליפת נתונים
  getDrone: (droneId: string) => Promise<DroneState | null>;
  getAllDrones: () => Promise<DroneState[] | null>;
}

export function useDroneApi(): UseDroneApiReturn {
  const [loading, setLoading] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  /**
   * Wrapper לפעולות עם loading state
   */
  const withLoading = useCallback(async <T>(
    action: string,
    fn: () => Promise<T>
  ): Promise<T | null> => {
    setLoading(action);
    setError(null);
    
    try {
      const result = await fn();
      return result;
    } catch (err) {
      const error = err as Error;
      setError(error.message);
      console.error(`Action ${action} failed:`, error);
      return null;
    } finally {
      setLoading(null);
    }
  }, []);

  // פעולות
  const arm = useCallback((droneId: string) => 
    withLoading('arm', () => droneApi.arm(droneId)), [withLoading]);

  const disarm = useCallback((droneId: string) => 
    withLoading('disarm', () => droneApi.disarm(droneId)), [withLoading]);

  const takeoff = useCallback((droneId: string, altitude: number = 30) => 
    withLoading('takeoff', () => droneApi.takeoff(droneId, altitude)), [withLoading]);

  const land = useCallback((droneId: string) => 
    withLoading('land', () => droneApi.land(droneId)), [withLoading]);

  const rtl = useCallback((droneId: string) => 
    withLoading('rtl', () => droneApi.rtl(droneId)), [withLoading]);

  const emergency = useCallback((droneId: string) => 
    withLoading('emergency', () => droneApi.emergency(droneId)), [withLoading]);

  const goTo = useCallback((droneId: string, request: GoToRequest) => 
    withLoading('goto', () => droneApi.goTo(droneId, request)), [withLoading]);

  // שליפת נתונים
  const getDrone = useCallback((droneId: string) => 
    withLoading('getDrone', () => droneApi.get(droneId)), [withLoading]);

  const getAllDrones = useCallback(() => 
    withLoading('getAllDrones', () => droneApi.getAll()), [withLoading]);

  return {
    loading,
    error,
    arm,
    disarm,
    takeoff,
    land,
    rtl,
    emergency,
    goTo,
    getDrone,
    getAllDrones
  };
}

export default useDroneApi;