<script lang="ts">
  import { snackbarStore } from '../stores/snackbarStore';
  import { derived } from 'svelte/store';

  const snackbar = snackbarStore;

  // Derived stores for convenience
  const visible = derived(snackbar, $snackbar => $snackbar.visible);
  const title = derived(snackbar, $snackbar => $snackbar.title);
  const message = derived(snackbar, $snackbar => $snackbar.message);
</script>

{#if $visible}
  <div class="snackbar">
    {#if $title}
      <div class="snackbar-title">{$title}</div>
    {/if}
    <div>{$message}</div>
  </div>
{/if}

<style>
  .snackbar {
    position: fixed;
    top: 20px;
    right: 20px;
    background-color: #b22222;
    color: #fff;
    padding: 1rem 1.5rem;
    border-radius: 6px;
    box-shadow: 0 4px 12px rgba(0,0,0,0.3);
    font-family: Arial, sans-serif;
    font-weight: normal;
    z-index: 1000;
    animation: fadein 0.3s, fadeout 0.3s 2.7s;
    max-width: 320px;
  }

  .snackbar-title {
    font-weight: bold;
    margin-bottom: 0.25rem;
    font-size: 1rem;
  }

  @keyframes fadein {
    from { opacity: 0; top: 0; }
    to { opacity: 1; top: 20px; }
  }

  @keyframes fadeout {
    from { opacity: 1; top: 20px; }
    to { opacity: 0; top: 0; }
  }
</style>
