// vite.config.ts
import { defineConfig } from 'vite';
import { svelte }    from '@sveltejs/vite-plugin-svelte';
import { fileURLToPath } from 'url';
import { dirname } from 'path';

const rootDir = dirname(fileURLToPath(import.meta.url));

export default defineConfig({
  // ▶︎ look for index.html here:
  root: rootDir,

  // ▶︎ serve everything in `public/` at `/…`
  publicDir: 'public',

  plugins: [svelte()],

  server: {
    open: true,
    port: 5173,
    proxy: {
      '/ws': {
        target: 'ws://localhost:5000',
        ws: true
      }
    }
  },

  build: {
    outDir: '../Olve.Engine3D.DebugServer/SvelteDist',
    emptyOutDir: true,
  },
});
