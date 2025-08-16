<script lang="ts">
  import { afterUpdate } from "svelte";
  import type { LogMessage } from "../generated/api/models";
  import LogEntry from "./LogEntry.svelte";
  let container: HTMLDivElement;
  let isAtBottom = true;

  export let filteredLogs: LogMessage[];
  export let loadError: string;

  function handleScroll() {
    if (container) {
      isAtBottom =
        container.scrollTop + container.clientHeight >=
        container.scrollHeight - 5;
    }
  }

  afterUpdate(() => {
    if (container && isAtBottom) {
      container.scrollTop = container.scrollHeight;
    }
  });
</script>

<log-messages bind:this={container} on:scroll={handleScroll}>
  {#if loadError}
    <div class="error-message">{loadError}</div>
  {:else}
    {#each filteredLogs as log}
      <LogEntry {log} />
    {/each}
  {/if}
</log-messages>

<style>
  log-messages {
    height: 300px;
    overflow-y: auto;
    padding: 0.5rem;
    border: 1px solid #ccc;
    display: flex;
    flex-direction: column;
    overflow-x: hidden;
  }

  .error-message {
    color: #c00;
    font-weight: bold;
    margin-bottom: 0.5rem;
  }
</style>
