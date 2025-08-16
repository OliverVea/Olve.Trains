<script lang="ts">
  import type { LogMessage } from "../generated/api/models";
  export let log: LogMessage;
</script>

<div class="log-entry log-{log.level?.toLowerCase()}">
  <div class="log-header">
    {log.time ? new Date(log.time).toLocaleTimeString() : "unknown time"}:
  </div>
  <div class="log-body">
    {log.message}
  </div>
</div>

<style>
  .log-entry {
    padding: 2px 0;
    margin: 0;
    white-space: unset;
    display: flex;
    flex-direction: column;
    width: 100%;
    transition: background-color 0.2s ease;
    padding-left: 2px;
  }

  .log-entry:hover {
    background-color: rgba(0, 0, 0, 0.05);
    cursor: pointer;
  }

  .log-header {
    font-size: 0.6rem;
    margin: 0;
    padding: 0;
  }

  .log-body {
    font-size: 1rem;
    margin: 0;
    padding: 0;
    white-space: pre-wrap;
    word-break: break-word;
  }

  .log-entry.log-debug {
    color: #555;
  }
  .log-entry.log-info {
    color: #0a0;
  }
  .log-entry.log-warning {
    color: #e65c00;
  }
  .log-entry.log-error {
    color: #c00;
  }
  .log-entry.log-critical {
    color: #900;
  }
</style>
