import { writable } from 'svelte/store';

/**
 * serverEventStore opens the WebSocket and keeps it alive with an automatic
 * reconnect strategy. Each emitted value is an object with a `type` and a
 * unique `id` (timestamp) so repeated events with the same type still notify
 * subscribers.
 */
type ServerEvent = { type: string; id: number } | null;
const store = writable<ServerEvent>(null);

let socket: WebSocket | null = null;
let reconnectAttempts = 0;
let reconnectTimer: number | null = null;
const MAX_BACKOFF = 30000; // ms

function connect() {
  if (typeof WebSocket === 'undefined') {
    console.debug('serverEventStore: WebSocket not available in this environment');
    return;
  }

  const url = 'ws://localhost:5000/ws';
  console.debug('serverEventStore: connecting to', url);
  socket = new WebSocket(url);

  socket.onopen = () => {
    console.debug('serverEventStore: websocket open');
    reconnectAttempts = 0;
    if (reconnectTimer !== null) {
      clearTimeout(reconnectTimer);
      reconnectTimer = null;
    }
  };

  socket.onmessage = (messageEvent) => {
    try {
      const parsed = JSON.parse(messageEvent.data);
      console.info('serverEventStore: got raw event:', parsed);
      if (typeof parsed === 'string') {
        store.set({ type: parsed, id: Date.now() });
      } else if (parsed && typeof parsed === 'object' && 'type' in parsed && typeof (parsed as any).type === 'string') {
        store.set({ type: (parsed as any).type, id: Date.now() });
      } else {
        store.set({ type: JSON.stringify(parsed), id: Date.now() });
      }
    } catch (e) {
      // not valid JSON? treat as raw text
      console.info('serverEventStore: message not JSON, treating as raw text:', messageEvent.data);
      store.set({ type: messageEvent.data as string, id: Date.now() });
    }
  };

  socket.onclose = (ev) => {
    console.debug('serverEventStore: websocket closed', ev);
    // notify subscribers that connection closed
    store.set(null);
    attemptReconnect();
  };

  socket.onerror = (e) => {
    console.error('serverEventStore: websocket error', e);
    // let onclose handle reconnect scheduling
  };
}

function attemptReconnect() {
  reconnectAttempts++;
  const backoff = Math.min(1000 * Math.pow(2, reconnectAttempts - 1), MAX_BACKOFF);
  console.debug(`serverEventStore: scheduling reconnect #${reconnectAttempts} in ${backoff}ms`);
  if (reconnectTimer !== null) clearTimeout(reconnectTimer);
  reconnectTimer = window.setTimeout(() => {
    connect();
  }, backoff);
}

// Start connection immediately on module load
connect();

export const serverEventStore = {
  subscribe: store.subscribe
};
