import { FetchRequestAdapter } from "@microsoft/kiota-http-fetchlibrary";
import { createApiClient } from './generated/api/apiClient';

const adapter = new FetchRequestAdapter({
  authenticateRequest: async (request, additionalAuthenticationContext) => {
    return Promise.resolve();
  },
});
adapter.baseUrl = "http://localhost:5000";

export const apiClient = createApiClient(adapter);
