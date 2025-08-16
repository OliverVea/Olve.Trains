import { render, screen } from '@testing-library/svelte';
import '@testing-library/jest-dom';
import App from '../src/App.svelte';
import logs from './fixtures/logs.json';

test('renders log entries from fixture', async () => {
  render(App, { props: { initialLogs: logs } });

  expect(screen.getByText(/Started/)).toBeInTheDocument();
  expect(screen.getByText(/Warning message/)).toBeInTheDocument();
});
