interface RuntimeEnv {
  VITE_API_BASE_URL?: string
}

const runtimeEnv = (import.meta as ImportMeta & { env?: RuntimeEnv }).env ?? {}

export const env = {
  apiBaseUrl: runtimeEnv.VITE_API_BASE_URL ?? 'http://127.0.0.1:8787/api',
} as const
