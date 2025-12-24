/**
 * API Service - שירות לתקשורת עם ה-Backend
 * 
 * כל הקריאות ל-WebAPI עוברות דרך כאן.
 * TypeScript מבטיח שאנחנו שולחים ומקבלים את הטיפוסים הנכונים.
 */

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

const API_BASE = '/api';

// ========== Helper Functions ==========

/**
 * פונקציית עזר לקריאות API עם Type Safety
 */
async function fetchApi<T>(url: string, options: RequestInit = {}): Promise<T> {
  const response = await fetch(`${API_BASE}${url}`, {
    headers: {
      'Content-Type': 'application/json',
      ...options.headers
    },
    ...options
  });

  if (!response.ok) {
    const error = await response.json().catch(() => ({ error: 'Request failed' }));
    throw new Error(error.error || error.message || `HTTP ${response.status}`);
  }

  return response.json() as Promise<T>;
}

// ========== Drone API ==========

export const droneApi = {
  /**
   * קבל את כל הרחפנים
   */
  getAll: (): Promise<DroneState[]> => 
    fetchApi<DroneState[]>('/drone'),

  /**
   * קבל רחפן ספציפי
   */
  get: (id: string): Promise<DroneState> => 
    fetchApi<DroneState>(`/drone/${id}`),

  /**
   * צור רחפן חדש
   */
  create: (data: CreateDroneRequest): Promise<DroneState> => 
    fetchApi<DroneState>('/drone', {
      method: 'POST',
      body: JSON.stringify(data)
    }),

  /**
   * זיין את הרחפן (Arm)
   */
  arm: (id: string): Promise<CommandResponse> => 
    fetchApi<CommandResponse>(`/drone/${id}/arm`, { method: 'POST' }),

  /**
   * פרק זיון (Disarm)
   */
  disarm: (id: string): Promise<CommandResponse> => 
    fetchApi<CommandResponse>(`/drone/${id}/disarm`, { method: 'POST' }),

  /**
   * המראה
   */
  takeoff: (id: string, altitude: number = 30): Promise<CommandResponse> => 
    fetchApi<CommandResponse>(`/drone/${id}/takeoff?altitude=${altitude}`, { method: 'POST' }),

  /**
   * נחיתה
   */
  land: (id: string): Promise<CommandResponse> => 
    fetchApi<CommandResponse>(`/drone/${id}/land`, { method: 'POST' }),

  /**
   * חזרה הביתה
   */
  rtl: (id: string): Promise<CommandResponse> => 
    fetchApi<CommandResponse>(`/drone/${id}/rtl`, { method: 'POST' }),

  /**
   * עצירת חירום
   */
  emergency: (id: string): Promise<CommandResponse> => 
    fetchApi<CommandResponse>(`/drone/${id}/emergency`, { method: 'POST' }),

  /**
   * טוס לנקודה
   */
  goTo: (id: string, request: GoToRequest): Promise<CommandResponse> => 
    fetchApi<CommandResponse>(`/drone/${id}/goto`, {
      method: 'POST',
      body: JSON.stringify(request)
    }),

  /**
   * קבל מסלול טיסה
   */
  getPath: (id: string): Promise<FlightPath> => 
    fetchApi<FlightPath>(`/drone/${id}/path`),

  /**
   * עדכון סימולציה
   */
  simulate: (id: string, deltaTime: number = 0.1): Promise<DroneState> => 
    fetchApi<DroneState>(`/drone/${id}/simulate?deltaTime=${deltaTime}`, { method: 'POST' })
};

// ========== Chat API ==========

export const chatApi = {
  /**
   * שלח הודעת צ'אט ל-AI (לא מבצע פקודות)
   */
  send: (message: string, droneId?: string): Promise<ChatResponse> => 
    fetchApi<ChatResponse>('/chat', {
      method: 'POST',
      body: JSON.stringify({ message, droneId } as ChatRequest)
    }),

  /**
   * שלח פקודה בשפה טבעית ובצע אותה
   */
  command: (message: string, droneId?: string): Promise<ChatResponse> => 
    fetchApi<ChatResponse>('/chat/command', {
      method: 'POST',
      body: JSON.stringify({ message, droneId } as ChatRequest)
    }),

  /**
   * בדיקת חיבור ל-API
   */
  test: (): Promise<ApiTestResponse> => 
    fetchApi<ApiTestResponse>('/chat/test')
};

// ========== Mission API ==========

export const missionApi = {
  /**
   * קבל את כל המשימות
   */
  getAll: (): Promise<MissionInfo[]> => 
    fetchApi<MissionInfo[]>('/mission'),

  /**
   * קבל משימה ספציפית
   */
  get: (id: string): Promise<MissionInfo> => 
    fetchApi<MissionInfo>(`/mission/${id}`),

  /**
   * תכנון משימה עם AI
   */
  plan: (droneId: string, description: string): Promise<MissionPlanResponse> => 
    fetchApi<MissionPlanResponse>('/mission/plan', {
      method: 'POST',
      body: JSON.stringify({ droneId, description } as MissionPlanRequest)
    }),

  /**
   * התחל משימה
   */
  start: (missionId: string, droneId: string): Promise<CommandResponse> => 
    fetchApi<CommandResponse>(`/mission/${missionId}/start?droneId=${droneId}`, { method: 'POST' }),

  /**
   * עצור משימה
   */
  stop: (missionId: string, droneId: string): Promise<CommandResponse> => 
    fetchApi<CommandResponse>(`/mission/${missionId}/stop?droneId=${droneId}`, { method: 'POST' }),

  /**
   * השהה משימה
   */
  pause: (missionId: string, droneId: string): Promise<CommandResponse> => 
    fetchApi<CommandResponse>(`/mission/${missionId}/pause?droneId=${droneId}`, { method: 'POST' }),

  /**
   * המשך משימה
   */
  resume: (missionId: string, droneId: string): Promise<CommandResponse> => 
    fetchApi<CommandResponse>(`/mission/${missionId}/resume?droneId=${droneId}`, { method: 'POST' })
};

// ========== Default Export ==========

export default { droneApi, chatApi, missionApi };