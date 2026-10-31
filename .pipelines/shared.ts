import { secret } from "@olivervea/pipelines";

export const GITHUB_TOKEN = secret("GITHUB_TOKEN", "GitHub read token for the clone (raises the rate limit).");
export const ITCH_API_KEY = secret("ITCH_API_KEY", "butler API key for pushing builds to itch.io.");
