/**
 * API Service - Communication layer with Backend
 * Handles all HTTP requests to the WebAPI
 */

import { config } from '../config';
import type {
  DroneState,
  CreateDroneRequest,
  GoToRequest,
  CommandResponse,
  FlightPath,
  ChatRequest,
  ChatResponse,
  MissionPlanRequest,
  MissionPlanResponse,
  MissionInfo,
  ApiTestResponse
} from '../types';

const API_BASE = config.api.baseUrl;

/**
 * Helper function for API calls with error handling
 */
async function fetchApi<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
  const url = `${API_BASE}${endpoint}`;
  
  console.log(`[API] ${options.method || 'GET'} ${url}`);

  const response = await fetch(url, {
    headers: {
      'Content-Type': 'application/json',
      ...options.headers
    },
    ...options
  });

  if (!response.ok) {
    const error = await response.json().catch(() => ({ error: 'Request failed' }));
    console.error(`[API] Error:`, error);
    throw new Error(error.error || error.message || `HTTP ${response.status}`);
  }

  return response.json() as Promise<T>;
}

// ========== Drone API ==========

export const droneApi = {
  /** Get all drones */
  getAll: (): Promise<DroneState[]> => 
    fetchApi<DroneState[]>('/api/drone'),

  /** Get specific drone by ID */
  get: (id: string): Promise<DroneState> => 
    fetchApi<DroneState>(`/api/drone/${id}`),

  /** Create new drone */
  create: (data: CreateDroneRequest): Promise<DroneState> => 
    fetchApi<DroneState>('/api/drone', {
      method: 'POST',
      body: JSON.stringify(data)
    }),

  /** Arm the drone */
  arm: (id: string): Promise<CommandResponse> => 
    fetchApi<CommandResponse>(`/api/drone/${id}/arm`, { method: 'POST' }),

  /** Disarm the drone */
  disarm: (id: string): Promise<CommandResponse> => 
    fetchApi<CommandResponse>(`/api/drone/${id}/disarm`, { method: 'POST' }),

  /** Takeoff to specified altitude */
  takeoff: (id: string, altitude: number = 30): Promise<CommandResponse> => 
    fetchApi<CommandResponse>(`/api/drone/${id}/takeoff?altitude=${altitude}`, { method: 'POST' }),

  /** Land the drone */
  land: (id: string): Promise<CommandResponse> => 
    fetchApi<CommandResponse>(`/api/drone/${id}/land`, { method: 'POST' }),

  /** Return to launch/home position */
  rtl: (id: string): Promise<CommandResponse> => 
    fetchApi<CommandResponse>(`/api/drone/${id}/rtl`, { method: 'POST' }),

  /** Emergency stop */
  emergency: (id: string): Promise<CommandResponse> => 
    fetchApi<CommandResponse>(`/api/drone/${id}/emergency`, { method: 'POST' }),

  /** Reset from emergency state */
  reset: (id: string): Promise<CommandResponse> => 
    fetchApi<CommandResponse>(`/api/drone/${id}/reset`, { method: 'POST' }),

  /** Go to specific position */
  goTo: (id: string, request: GoToRequest): Promise<CommandResponse> => 
    fetchApi<CommandResponse>(`/api/drone/${id}/goto`, {
      method: 'POST',
      body: JSON.stringify(request)
    }),

  /** Get current flight path */
  getPath: (id: string): Promise<FlightPath> => 
    fetchApi<FlightPath>(`/api/drone/${id}/path`),

  /** Trigger simulation update */
  simulate: (id: string, deltaTime: number = 0.1): Promise<DroneState> => 
    fetchApi<DroneState>(`/api/drone/${id}/simulate?deltaTime=${deltaTime}`, { method: 'POST' })
};

// ========== Chat API ==========

export const chatApi = {
  /** Send chat message to AI (no command execution) */
  send: (message: string, droneId?: string): Promise<ChatResponse> => 
    fetchApi<ChatResponse>('/api/chat', {
      method: 'POST',
      body: JSON.stringify({ message, droneId } as ChatRequest)
    }),

  /** Send natural language command and execute it */
  command: (message: string, droneId?: string): Promise<ChatResponse> => 
    fetchApi<ChatResponse>('/api/chat/command', {
      method: 'POST',
      body: JSON.stringify({ message, droneId } as ChatRequest)
    }),

  /** Test API connection */
  test: (): Promise<ApiTestResponse> => 
    fetchApi<ApiTestResponse>('/api/chat/test')
};

// ========== Mission API ==========

export const missionApi = {
  /** Get all missions */
  getAll: (): Promise<MissionInfo[]> => 
    fetchApi<MissionInfo[]>('/api/mission'),

  /** Get specific mission */
  get: (id: string): Promise<MissionInfo> => 
    fetchApi<MissionInfo>(`/api/mission/${id}`),

  /** Plan mission using AI */
  plan: (droneId: string, description: string): Promise<MissionPlanResponse> => 
    fetchApi<MissionPlanResponse>('/api/mission/plan', {
      method: 'POST',
      body: JSON.stringify({ droneId, description } as MissionPlanRequest)
    }),

  /** Start a mission */
  start: (missionId: string, droneId: string): Promise<CommandResponse> => 
    fetchApi<CommandResponse>(`/api/mission/${missionId}/start?droneId=${droneId}`, { method: 'POST' }),

  /** Stop a mission */
  stop: (missionId: string, droneId: string): Promise<CommandResponse> => 
    fetchApi<CommandResponse>(`/api/mission/${missionId}/stop?droneId=${droneId}`, { method: 'POST' }),

  /** Pause a mission */
  pause: (missionId: string, droneId: string): Promise<CommandResponse> => 
    fetchApi<CommandResponse>(`/api/mission/${missionId}/pause?droneId=${droneId}`, { method: 'POST' }),

  /** Resume a paused mission */
  resume: (missionId: string, droneId: string): Promise<CommandResponse> => 
    fetchApi<CommandResponse>(`/api/mission/${missionId}/resume?droneId=${droneId}`, { method: 'POST' })
};

export default { droneApi, chatApi, missionApi };