/**
 * ChatPanel - פאנל הצ'אט הראשי
 */

import React, { useState, useRef, useEffect } from 'react';
import { Send, Loader2, Trash2, Zap } from 'lucide-react';
import { ChatMessage } from './ChatMessage';
import { chatApi } from '../../services/api';
import type { ChatMessage as ChatMessageType, QuickCommand, ChatResponse } from '../../types';

interface ChatPanelProps {
  droneId?: string;
  onCommandExecuted?: (response: ChatResponse) => void;
}

// פקודות מהירות
const quickCommands: QuickCommand[] = [
  { label: '🛫 Takeoff', command: 'takeoff to 50 meters' },
  { label: '🛬 Land', command: 'land now' },
  { label: '🏠 RTL', command: 'return to home' },
  { label: '📊 Status', command: 'what is the drone status?' },
  { label: '🔄 Orbit', command: 'orbit current position with 30m radius' },
];

export const ChatPanel: React.FC<ChatPanelProps> = ({ droneId, onCommandExecuted }) => {
  const [messages, setMessages] = useState<ChatMessageType[]>([]);
  const [input, setInput] = useState('');
  const [isLoading, setIsLoading] = useState(false);

  const messagesEndRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  // גלילה אוטומטית
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  /**
   * שליחת הודעה
   */
  const handleSend = async (): Promise<void> => {
    const text = input.trim();
    if (!text || isLoading) return;

    setInput('');
    
    // הודעת משתמש
    const userMessage: ChatMessageType = {
      id: Date.now(),
      role: 'user',
      content: text,
      timestamp: new Date().toISOString()
    };
    setMessages(prev => [...prev, userMessage]);

    // הודעת AI בטעינה
    const loadingMessage: ChatMessageType = {
      id: Date.now() + 1,
      role: 'assistant',
      content: '',
      timestamp: new Date().toISOString(),
      isLoading: true
    };
    setMessages(prev => [...prev, loadingMessage]);
    setIsLoading(true);

    try {
      const response = await chatApi.command(text, droneId);

      // עדכון הודעת AI
      setMessages(prev => prev.map(msg => 
        msg.id === loadingMessage.id
          ? {
              ...msg,
              isLoading: false,
              content: response.response,
              parsedCommand: response.hasCommand ? {
                command: response.commandType || 'unknown',
                executed: response.commandExecuted,
                result: response.commandResult
              } : undefined,
              commandResult: response.commandExecuted !== undefined ? {
                success: response.commandExecuted,
                message: response.commandResult || (response.commandExecuted ? 'Command executed' : 'Command failed')
              } : undefined
            }
          : msg
      ));

      if (response.commandExecuted && onCommandExecuted) {
        onCommandExecuted(response);
      }
    } catch (error) {
      const err = error as Error;
      setMessages(prev => prev.map(msg =>
        msg.id === loadingMessage.id
          ? {
              ...msg,
              isLoading: false,
              content: `Error: ${err.message}`,
              commandResult: { success: false, message: err.message }
            }
          : msg
      ));
    } finally {
      setIsLoading(false);
      inputRef.current?.focus();
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>): void => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  const handleQuickCommand = (command: string): void => {
    setInput(command);
    inputRef.current?.focus();
  };

  const handleClear = (): void => {
    setMessages([]);
  };

  return (
    <div className="chat-panel">
      {/* כותרת */}
      <div className="chat-header">
        <div className="chat-title">
          <Zap size={20} />
          <span>AI Assistant</span>
        </div>
        <button className="btn-icon" onClick={handleClear} title="Clear chat">
          <Trash2 size={16} />
        </button>
      </div>

      {/* פקודות מהירות */}
      <div className="quick-commands">
        {quickCommands.map((cmd, i) => (
          <button
            key={i}
            className="quick-cmd-btn"
            onClick={() => handleQuickCommand(cmd.command)}
          >
            {cmd.label}
          </button>
        ))}
      </div>

      {/* הודעות */}
      <div className="chat-messages">
        {messages.length === 0 ? (
          <div className="chat-empty">
            <div className="empty-icon">🤖</div>
            <h3>AI Drone Assistant</h3>
            <p>Send commands in Hebrew or English</p>
            <div className="examples">
              <code>המרא לגובה 50 מטר</code>
              <code>fly to position 100, 200</code>
              <code>survey area 200x200</code>
            </div>
          </div>
        ) : (
          messages.map(msg => (
            <ChatMessage key={msg.id} message={msg} />
          ))
        )}
        <div ref={messagesEndRef} />
      </div>

      {/* קלט */}
      <div className="chat-input">
        <input
          ref={inputRef}
          type="text"
          value={input}
          onChange={(e) => setInput(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder="Type a command in Hebrew or English..."
          disabled={isLoading}
        />
        <button 
          className="send-btn"
          onClick={handleSend}
          disabled={isLoading || !input.trim()}
        >
          {isLoading ? <Loader2 size={20} className="spin" /> : <Send size={20} />}
        </button>
      </div>
    </div>
  );
};

export default ChatPanel;