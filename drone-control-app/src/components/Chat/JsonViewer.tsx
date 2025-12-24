/**
 * JsonViewer - תצוגת JSON מעוצבת
 */

import React from 'react';

interface JsonViewerProps {
  data: unknown;
  title?: string;
}

export const JsonViewer: React.FC<JsonViewerProps> = ({ data, title = "Parsed Command" }) => {
  if (!data) return null;

  const getValueColor = (value: unknown): string => {
    if (value === null) return '#999';
    if (typeof value === 'boolean') return '#d73a49';
    if (typeof value === 'number') return '#005cc5';
    if (typeof value === 'string') return '#22863a';
    return '#24292e';
  };

  const renderValue = (value: unknown, indent: number = 0): React.ReactNode => {
    const padding = '  '.repeat(indent);

    if (value === null) {
      return <span style={{ color: getValueColor(null) }}>null</span>;
    }

    if (Array.isArray(value)) {
      if (value.length === 0) return <span>[]</span>;
      return (
        <>
          {'[\n'}
          {value.map((item, i) => (
            <span key={i}>
              {padding}  {renderValue(item, indent + 1)}
              {i < value.length - 1 ? ',' : ''}{'\n'}
            </span>
          ))}
          {padding}{']'}
        </>
      );
    }

    if (typeof value === 'object' && value !== null) {
      const obj = value as Record<string, unknown>;
      const keys = Object.keys(obj);
      if (keys.length === 0) return <span>{'{}'}</span>;
      return (
        <>
          {'{\n'}
          {keys.map((key, i) => (
            <span key={key}>
              {padding}  <span style={{ color: '#6f42c1' }}>"{key}"</span>: {renderValue(obj[key], indent + 1)}
              {i < keys.length - 1 ? ',' : ''}{'\n'}
            </span>
          ))}
          {padding}{'}'}
        </>
      );
    }

    if (typeof value === 'string') {
      return <span style={{ color: getValueColor(value) }}>"{value}"</span>;
    }

    return <span style={{ color: getValueColor(value) }}>{String(value)}</span>;
  };

  return (
    <div className="json-viewer">
      <div className="json-header">
        <span className="json-icon">📦</span>
        <span className="json-title">{title}</span>
      </div>
      <pre className="json-content">
        {renderValue(data)}
      </pre>
    </div>
  );
};

export default JsonViewer;