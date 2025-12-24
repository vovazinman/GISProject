/**
 * API Types - Types for API interactions in the Drone Control App
 * 
 * These types correspond to the DTOs in the C# backend
 */

/**
 * Error Response
 */
export interface ErrorResponse {
  error: string;
  details?: string;
  statusCode: number;
}

/**
 * Mission Plan Request
 */
export interface MissionPlanRequest {
  droneId: string;
  description: string;
}

/**
 * Mission Plan Response
 */
export interface MissionPlanResponse {
  success: boolean;
  missionId: string;
  missionType: string;
  name: string;
  waypointCount: number;
  estimatedDurationMin: number;
  estimatedDistanceM: number;
  waypoints: Array<{
    x: number;
    y: number;
    z: number;
    time: number;
  }>;
  errorMessage?: string;
}

/**
 * Mission Info
 */
export interface MissionInfo {
  id: string;
  name: string;
  type: string;
  status: string;
  altitude: number;
  speed: number;
  estimatedDurationSec: number;
  estimatedDistanceM: number;
  createdAt: string;
}

/**
 * API Test Response
 */
export interface ApiTestResponse {
  status: string;
  configured: boolean;
  message?: string;
  error?: string;
}