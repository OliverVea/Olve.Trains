<script lang="ts">
  import { LogLevelObject } from "../generated/api/models";
  import type { LogLevel } from "../generated/api/models";

  export let filterLevel: LogLevel;
  export let query: string;
</script>

<div class="filters">
  <label for="level-filter">Level:</label>
  <select id="level-filter" bind:value={filterLevel}>
    <option value={LogLevelObject.Debug}>Debug</option>
    <option value={LogLevelObject.Info}>Info</option>
    <option value={LogLevelObject.Warning}>Warning</option>
    <option value={LogLevelObject.ErrorEscaped}>Error</option>
    <option value={LogLevelObject.Critical}>Critical</option>
  </select>


  <label for="query-input">Search:</label>
  <div class="query-input-container">
    <input
      id="query-input"
      type="text"
      bind:value={query}
      placeholder="Search logs..."
    />
  </div>
</div>

<style>
  .filters {
    display: flex;
    gap: 1rem;
    align-items: center;
    margin-bottom: 1rem;
    font-family: Arial, sans-serif;
  }

  .filters label {
    font-weight: bold;
    font-size: 0.9rem;
  }

  .filters select {
    padding: 0.3rem 0.5rem;
    border: 1px solid #ccc;
    border-radius: 4px;
    font-size: 0.9rem;
    font-family: Arial, sans-serif;
  }

  .query-input-container {
    display: flex;
    align-items: center;
  }

  .query-input-container input {
    flex-grow: 1;
    padding: 0.3rem 0.5rem;
    border: 1px solid #ccc;
    border-radius: 4px;
    font-size: 0.9rem;
    font-family: Arial, sans-serif;
  }
</style>
