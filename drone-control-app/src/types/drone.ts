/**
 * Drone Types - Types for drones in the Drone Control App
 * 
 * These types correspond to the DTOs in the C# backend
 */

// ========== Enums ==========

/**
 * drone status like in Backend
 * DroneStatus enum like in C#
 */
export enum DroneStatus {
  Offline = 'Offline',
  Ready = 'Ready',
  Armed = 'Armed',
  TakingOff = 'TakingOff',
  Flying = 'Flying',
  Hovering = 'Hovering',
  Landing = 'Landing',
  Landed = 'Landed',
  Returning = 'Returning',
  Emergency = 'Emergency',
  Error = 'Error'
}

/**
 * flying mode like in Backend
 *  * FlightMode enum like in C#
 */
export enum FlightMode {
  Manual = 'Manual',
  Stabilize = 'Stabilize',
  Loiter = 'Loiter',
  Auto = 'Auto',
  Guided = 'Guided',
  ReturnToLaunch = 'ReturnToLaunch',
  Land = 'Land',
  Takeoff = 'Takeoff'
}

// ========== Interfaces ==========

/**
 * Vector3D - for position and velocity
 */
export interface Vector3D {
  x: number;
  y: number;
  z: number;
}

/**
 * Drone State - received from the API
 * Matches DroneStateDto in C#
 */
export interface DroneState {
  droneId: string;
  status: DroneStatus;
  flightMode: FlightMode;
  position: Vector3D;
  velocity: Vector3D;
  altitudeAGL: number;
  groundSpeed: number;
  batteryPercent: number;
  distanceFromHome: number;
  distanceTraveled: number;
  flightTimeSec: number;
  isArmed: boolean;
  currentMissionId: string | null;
  currentWaypointIndex: number;
  totalWaypoints: number;
  timestampUtc: string;
}

/**
 * Dron Specifications metadata
 */
export interface DroneSpecifications {
  model: string;
  maxSpeedMs: number;
  maxAltitudeM: number;
  maxFlightTimeMinutes: number;
  batteryCapacityMah: number;
}

/**
 * Create Drone Request
 */
export interface CreateDroneRequest {
  id?: string;
  specsType?: 'mavic3' | 'phantom4' | 'matrice300';
  x: number;
  y: number;
  z: number;
}

/**
 * GoTo Request
 */
export interface GoToRequest {
  x: number;
  y: number;
  z: number;
  speed?: number;
}

/**
 * Command Response
 */
export interface CommandResponse {
  success: boolean;
  message: string;
  newState?: DroneState;
}

/**
 * Waypoint
 */
export interface Waypoint {
  position: Vector3D;
  time: number;
  speed: number;
}

/**
 * FlightPath 
 */
export interface FlightPath {
  droneId: string;
  waypoints: Waypoint[];
  totalDistance: number;
  totalDuration: number;
}