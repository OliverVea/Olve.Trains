import { http, HttpResponse } from 'msw';
import logs from './fixtures/logs.json';

export const handlers = [
  http.get('http://localhost:5000/logs', () => {
    return HttpResponse.json(logs);
  }),
];
