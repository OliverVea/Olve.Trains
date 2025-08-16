import { writable } from 'svelte/store';

interface SnackbarState {
  title: string;
  message: string;
  visible: boolean;
}

function createSnackbarStore() {
  const { subscribe, set, update } = writable<SnackbarState>({
    title: '',
    message: '',
    visible: false,
  });

  let timeoutId: ReturnType<typeof setTimeout> | null = null;

  function show(title: string, message: string, duration = 3000) {
    set({ title, message, visible: true });
    if (timeoutId) {
      clearTimeout(timeoutId);
    }
    timeoutId = setTimeout(() => {
      set({ title: '', message: '', visible: false });
      timeoutId = null;
    }, duration);
  }

  function hide() {
    if (timeoutId) {
      clearTimeout(timeoutId);
      timeoutId = null;
    }
    set({ title: '', message: '', visible: false });
  }

  return {
    subscribe,
    show,
    hide,
  };
}

export const snackbarStore = createSnackbarStore();
