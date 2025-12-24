/**
 * Chat Types - Types for chat functionality in the Drone Control App
 * 
 * These types correspond to the DTOs in the C# backend
 */

/**
 * Chat Request
 */
export interface ChatRequest {
  message: string;
  droneId?: string;
}

/**
 * Chat Response
 */
export interface ChatResponse {
  response: string;
  hasCommand: boolean;
  commandType?: string;
  commandExecuted?: boolean;
  commandResult?: string;
}

/**
 * Parsed Command - what the AI returns
 */
export interface ParsedCommand {
  command: string;
  params?: Record<string, unknown>;
  confidence?: number;
  executed?: boolean;
  result?: string;
}

/**
 * Command Result
 */
export interface CommandResult {
  success: boolean;
  message: string;
}

/**
 * Chat Message
 */
export interface ChatMessage {
  id: number;
  role: 'user' | 'assistant' | 'system';
  content: string;
  timestamp: string;
  parsedCommand?: ParsedCommand;
  commandResult?: CommandResult;
  isLoading?: boolean;
}

/**
 * Quick Command
 */
export interface QuickCommand {
  label: string;
  command: string;
  icon?: string;
}