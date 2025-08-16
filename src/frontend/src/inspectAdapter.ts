import { FetchRequestAdapter } from "@microsoft/kiota-http-fetchlibrary";

const adapter = new FetchRequestAdapter({
  authenticateRequest: async (request, additionalAuthenticationContext) => {
    return Promise.resolve();
  }
});

console.log('Adapter keys:', Object.keys(adapter));
console.log('Adapter prototype keys:', Object.getOwnPropertyNames(Object.getPrototypeOf(adapter)));
