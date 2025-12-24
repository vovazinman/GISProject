/**
 * ChatMessage - הודעה בודדת בצ'אט
 */

import React from 'react';
import { User, Bot, CheckCircle, XCircle, AlertCircle } from 'lucide-react';
import { JsonViewer } from './JsonViewer';
import type { ChatMessage as ChatMessageType } from '../../types';

interface ChatMessageProps {
  message: ChatMessageType;
}

export const ChatMessage: React.FC<ChatMessageProps> = ({ message }) => {
  const { 
    role,
    content,
    timestamp,
    parsedCommand,
    commandResult,
    isLoading
  } = message;

  const isUser = role === 'user';
  const isSystem = role === 'system';

  /**
   * אייקון לפי סטטוס ביצוע
   */
  const getStatusIcon = (): React.ReactNode => {
    if (!commandResult) return null;
    
    if (commandResult.success) {
      return <CheckCircle size={16} className="status-icon success" />;
    }
    return <XCircle size={16} className="status-icon error" />;
  };

  // הודעות מערכת
  if (isSystem) {
    return (
      <div className="chat-message system">
        <AlertCircle size={16} />
        <span>{content}</span>
      </div>
    );
  }

  return (
    <div className={`chat-message ${isUser ? 'user' : 'assistant'}`}>
      {/* אווטאר */}
      <div className="message-avatar">
        {isUser ? <User size={20} /> : <Bot size={20} />}
      </div>

      {/* תוכן */}
      <div className="message-content">
        {/* כותרת */}
        <div className="message-header">
          <span className="message-role">{isUser ? 'You' : 'AI Assistant'}</span>
          <span className="message-time">
            {new Date(timestamp).toLocaleTimeString()}
          </span>
          {getStatusIcon()}
        </div>

        {/* טקסט */}
        <div className="message-text">
          {isLoading ? (
            <div className="typing-indicator">
              <span></span><span></span><span></span>
            </div>
          ) : (
            content
          )}
        </div>

        {/* JSON מפורסר */}
        {!isUser && parsedCommand && (
          <JsonViewer data={parsedCommand} />
        )}

        {/* תוצאת ביצוע */}
        {commandResult && (
          <div className={`command-result ${commandResult.success ? 'success' : 'error'}`}>
            {commandResult.success ? '✅' : '❌'} {commandResult.message}
          </div>
        )}
      </div>
    </div>
  );
};

export default ChatMessage;