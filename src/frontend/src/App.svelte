<script lang="ts">
  import Logs from "./components/Logs.svelte";
  import Snackbar from "./components/Snackbar.svelte";
</script>

<div class="app-container">
  <header>
    <h1>Olve Server Logs</h1>
  </header>

  <Logs />
  <Snackbar />
</div>

<style>
  .app-container {
    display: flex;
    flex-direction: column;
    align-items: center;
    padding: 1rem;
    font-family: Arial, sans-serif;
    max-width: 900px;
    margin: 0 auto;
  }
  header {
    width: 100%;
    background-color: #007bff;
    color: white;
    padding: 1rem 2rem;
    border-radius: 8px;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
    margin-bottom: 1rem;
    display: flex;
    justify-content: center;
    align-items: center;
  }
  header h1 {
    font-size: 2rem;
    font-weight: 900;
    font-family: "Segoe UI", Tahoma, Geneva, Verdana, sans-serif;
    letter-spacing: 2px;
    text-transform: uppercase;
    margin: 0;
    user-select: none;
    text-shadow: 1px 1px 2px rgba(0, 0, 0, 0.3);
  }
</style>
