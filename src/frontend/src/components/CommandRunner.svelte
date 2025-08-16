<script lang="ts">
  import { createEventDispatcher, onMount } from 'svelte';
  import { apiClient } from '../apiClientSingleton';
  import { snackbarStore } from '../stores/snackbarStore';

  const HISTORY_STORAGE_KEY = 'commandHistory.v1';
  const HISTORY_MAX = 100;

  let command = '';
  let commandResponse = '';

  let history: string[] = [];
  let collapsed = true; // contracted by default

  const dispatch = createEventDispatcher();

  function loadHistory() {
    try {
      const raw = localStorage.getItem(HISTORY_STORAGE_KEY);
      if (raw) {
        const parsed = JSON.parse(raw);
        if (Array.isArray(parsed)) history = parsed;
      }
    } catch (e) {
      console.warn('Failed to load command history', e);
    }
  }

  function saveHistory() {
    try {
      localStorage.setItem(HISTORY_STORAGE_KEY, JSON.stringify(history.slice(0, HISTORY_MAX)));
    } catch (e) {
      console.warn('Failed to save command history', e);
    }
  }

  function addToHistory(cmd: string) {
    if (!cmd) return;
    // Avoid consecutive duplicates: if top equals new, don't duplicate
    if (history[0] === cmd) return;
    // remove existing duplicate further down
    history = history.filter((h) => h !== cmd);
    history.unshift(cmd);
    if (history.length > HISTORY_MAX) history = history.slice(0, HISTORY_MAX);
    saveHistory();
  }

  function setCommandFromHistory(cmd: string) {
    command = cmd;
  }

  // Runs a raw command string. If clearInput is true, clear the input after starting.
  async function runCommandText(raw: string, clearInput = false): Promise<void> {
    const trimmed = (raw || '').trim();
    if (!trimmed) return;

    if (clearInput) {
      // clear input immediately like previous behaviour
      command = '';
    }

    // show immediate UI feedback
    commandResponse = 'Running…';

    // parse trailing "xN" (e.g. "my-cmd x100" or "my-cmdx100")
    const spacedMatch = trimmed.match(/\s+x(\d+)$/i);
    let cmd = trimmed;
    let times: number | undefined = undefined;

    if (spacedMatch) {
      times = parseInt(spacedMatch[1], 10);
      cmd = trimmed.slice(0, spacedMatch.index).trim();
    } else {
      times = 1;
    }

    // add to history (we do this even if it fails so user can rerun)
    addToHistory(trimmed);

    try {
      const body: Record<string, unknown> = { command: cmd, times };
      await apiClient.runCommand.post(body);
      commandResponse = '';
      dispatch('success');
    } catch (err) {
      console.error(err);
      snackbarStore.show('Error', `Command "${trimmed}" failed`);
      commandResponse = '';
      dispatch('error', { error: err });
    }
  }

  // wired to input (Enter) and Run button
  function runCommand(): Promise<void> {
    return runCommandText(command, true);
  }

  function toggleHistory() {
    collapsed = !collapsed;
  }

  onMount(() => {
    loadHistory();
  });
</script>

<section class="command-runner">
  <div class="runner-row">
    <input
      id="command-input"
      type="text"
      bind:value={command}
      placeholder="Enter command…"
      autocomplete="off"
      on:keydown={(e) => e.key === 'Enter' && runCommand()}
    />
    <button id="command-send" on:click={runCommand}>Run</button>
  </div>

  <div id="command-response" class="command-response">{commandResponse}</div>

  <div class="history-container">
    <button class="history-toggle" on:click={toggleHistory} aria-expanded={!collapsed}>
      Command history
      <span class="chev">{collapsed ? '▸' : '▾'}</span>
    </button>

    {#if !collapsed}
      <div class="history-list" role="list">
        {#if history.length === 0}
          <div class="history-empty">No commands yet.</div>
        {:else}
          {#each history as item, idx}
            <div class="history-item" role="listitem">
              <button class="history-command" on:click={() => setCommandFromHistory(item)}>{item}</button>
              <a class="history-run" href="#" on:click|preventDefault={() => runCommandText(item, false)}>run</a>
            </div>
          {/each}
        {/if}
      </div>
    {/if}
  </div>
</section>

<style>
  .command-runner {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    margin: 1rem 0;
    font-family: Arial, sans-serif;
    max-width: 900px;
    align-items: stretch;
  }

  .runner-row {
    display: flex;
    gap: 0.5rem;
    align-items: center;
  }

  #command-input {
    flex: 1;
    padding: 0.5rem;
    font-family: monospace;
    border: 1px solid #ccc;
    border-right: none;
    border-radius: 4px 0 0 4px;
  }

  #command-send {
    padding: 0.5rem 1rem;
    background-color: #007bff;
    border: none;
    color: white;
    font-weight: bold;
    border-radius: 0 4px 4px 0;
    cursor: pointer;
    transition: background-color 0.2s ease;
  }

  #command-send:hover {
    background-color: #0056b3;
  }

  .command-response {
    margin-top: 0.25rem;
    font-family: monospace;
    color: #333;
    min-height: 1.2rem;
  }

  .history-container {
    margin-top: 0.5rem;
    border-top: 1px solid #eee;
    padding-top: 0.5rem;
  }

  .history-toggle {
    background: transparent;
    border: none;
    color: #333;
    font-weight: bold;
    cursor: pointer;
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0;
  }

  .history-toggle .chev {
    font-size: 0.9rem;
    color: #666;
  }

  .history-list {
    margin-top: 0.5rem;
    max-height: 200px;
    overflow: auto;
    border: 1px solid #eee;
    border-radius: 6px;
    background: white;
    padding: 0.5rem;
  }

  .history-item {
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 0.5rem;
    padding: 0.25rem 0.25rem;
    border-bottom: 1px dashed #f0f0f0;
  }

  .history-item:last-child {
    border-bottom: none;
  }

  .history-command {
    background: transparent;
    border: none;
    text-align: left;
    padding: 0;
    font-family: monospace;
    cursor: pointer;
    color: #111;
    flex: 1;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .history-command:hover {
    text-decoration: underline;
  }

  .history-run {
    color: #007bff;
    cursor: pointer;
    margin-left: 0.5rem;
    text-decoration: none;
    font-size: 0.9rem;
  }

  .history-run:hover {
    text-decoration: underline;
  }

  .history-empty {
    color: #666;
    padding: 0.25rem;
  }
</style>
