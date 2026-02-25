/// <reference types="vite/client" />

declare const __APP_VERSION__: string
declare const __APP_BUILD_TIME__: string
declare const __APP_HOMEPAGE__: string
declare const __APP_NAME__: string
declare const __APP_AUTHOR_NAME__: string
declare const __APP_AUTHOR_URL__: string

interface ImportMetaEnv {
  readonly VITE_APP_TITLE: string
  readonly VITE_APP_LOGO: string
  readonly VITE_APP_NAMESPACE: string
  readonly VITE_APP_VERSION: string
  readonly VITE_PORT: number
  readonly VITE_API_BASE_URL: string
  readonly VITE_API_PREFIX: string
  readonly VITE_ROUTER_HISTORY: 'hash' | 'history'
  readonly VITE_BASE: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
