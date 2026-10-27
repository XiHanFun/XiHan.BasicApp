/// <reference types="vite/client" />

/** 构建期由 vite.config 注入的应用版本（package.json 的 version） */
declare const __APP_VERSION__: string
declare const __APP_NAME__: string
declare const __APP_BUILD_TIME__: string
declare const __APP_BUILD_STAMP__: string
declare const __APP_REPOSITORY__: string

interface ImportMetaEnv {
  readonly VITE_API_BASE_URL?: string
  readonly VITE_DEV_PROXY_TARGET?: string
  readonly VITE_PORT?: string
  /** 应用名与标志（登录页品牌区；缺省取文案与 /favicon.png） */
  readonly VITE_APP_TITLE?: string
  readonly VITE_APP_LOGO?: string
  readonly VITE_APP_SUBTITLE?: string
  readonly VITE_APP_DESCRIPTION?: string
  /** 首页兜底路径（壳层固定首页标签、守卫重定向） */
  readonly VITE_HOME_PATH?: string
  /** 实时 Hub 路径；为空即不建实时连接 */
  readonly VITE_REALTIME_HUB?: string
  readonly VITE_API_PREFIX?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}

declare module '*.vue' {
  import type { DefineComponent } from 'vue'

  const component: DefineComponent<object, object, unknown>
  export default component
}
