<script lang="ts">
import { onMount, createEventDispatcher } from 'svelte';
import { serverEventStore } from '../stores/serverEventsStore';
  import { type LogMessage, type GetLogsRequest, LogLevelObject } from '../generated/api/models';
  import { apiClient } from '../apiClientSingleton';
  import LogFilter from './LogFilter.svelte';
  import LogMessages from './LogMessages.svelte';
  import CommandRunner from './CommandRunner.svelte';

  const dispatch = createEventDispatcher();

  let filterLevel = LogLevelObject.Info;
  let query = '';
  let mounted = false;
  let newLogDebounceTimer: ReturnType<typeof setTimeout> | null = null;
  const NEW_LOG_DEBOUNCE_MS = 300;

  let filteredLogs: LogMessage[] = [];
  let loadError = '';

  export async function loadLogs(): Promise<void> {
    console.debug('Logs.svelte: loadLogs() called', { query, filterLevel });
    const requestBody: GetLogsRequest = {
      query,
      count: 100,
      logLevel: filterLevel
    };
    try {
      const res = await apiClient.logs.post(requestBody);
      filteredLogs = res?.messages ?? [];
      loadError = '';
      dispatch('loaded', { logs: filteredLogs });
      console.debug('Logs.svelte: loadLogs() finished, loaded', filteredLogs.length, 'messages');
    } catch (e) {
      loadError = 'Failed to load logs. Please check console.';
      filteredLogs = [];
      dispatch('error', { error: e });
      console.error('Logs.svelte: loadLogs() failed', e);
    }
  }

  onMount(() => {
    // perform initial load and mark component as mounted so future changes to
    // filters trigger reloads (but the initial assignment to filter/query above
    // won't cause an extra reload)
    let cancelled = false;
    loadLogs().then(() => {
      if (!cancelled) mounted = true;
    });

    const unsubscribe = serverEventStore.subscribe((event) => {
      if (event?.type === 'NEW_LOG') {
        console.debug('Logs.svelte: received NEW_LOG; reloading logs (debounced)');
        if (newLogDebounceTimer !== null) {
          clearTimeout(newLogDebounceTimer);
        }
        newLogDebounceTimer = window.setTimeout(() => {
          loadLogs();
          newLogDebounceTimer = null;
        }, NEW_LOG_DEBOUNCE_MS);
      }
    });
    return () => {
      cancelled = true;
      if (newLogDebounceTimer !== null) {
        clearTimeout(newLogDebounceTimer);
        newLogDebounceTimer = null;
      }
      unsubscribe();
    };
  });

  // reload logs whenever the user changes the level or query after mount
  $: if (mounted) {
    // create a dependency on filterLevel and query so Svelte re-runs this block
    const _filters = `${filterLevel}|${query}`;
    console.debug('Logs.svelte: filters changed; reloading logs', { filterLevel, query });
    loadLogs();
  }
</script>

<section class="logs-container">
  <LogFilter bind:filterLevel bind:query />
  <LogMessages {filteredLogs} {loadError} />
  <CommandRunner />
</section>

<style>
  .logs-container {
    padding: 1rem;
    background: #f9f9f9;
    width: 100%;
    border-radius: 8px;
    font-family: monospace;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
    margin-bottom: 1rem;
    display: flex;
    flex-direction: column;
    height: 100%;
  }
  
  .refresh-button {
    align-self: flex-end;
    margin: 0.5rem 0;
    padding: 0.5rem 1rem;
    border: none;
    background: #007bff;
    color: white;
    border-radius: 4px;
    cursor: pointer;
  }
</style>
