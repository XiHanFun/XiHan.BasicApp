/**
 * vue-plugin-hiprint 0.0.60 的项目内最小类型声明。
 * 上游包未发布 TypeScript 声明；这里只覆盖本项目实际调用的稳定 API，避免把 any 扩散到业务代码。
 */
declare module 'vue-plugin-hiprint' {
  interface PrintTemplateOptions {
    history?: boolean
    onDataChanged?: (type: string, template: unknown) => void
    onUpdateError?: (error: unknown) => void
    paginationContainer?: string
    settingContainer?: string
    template?: unknown
  }

  interface PrintElementTypeManagerLike {
    buildByHtml: (elements: unknown) => void
  }

  interface HiprintLike {
    PrintElementTypeGroup: new (name: string, elements: Record<string, unknown>[]) => unknown
    PrintElementTypeManager: PrintElementTypeManagerLike
    PrintTemplate: new (options?: PrintTemplateOptions) => import('./types').HiprintTemplateInstance
    init: (options?: Record<string, unknown>) => void
  }

  export const hiprint: HiprintLike
  export const defaultElementTypeProvider: new () => { addElementTypes: (manager: unknown) => void }
  export function autoConnect(callback?: (status: boolean, message?: unknown) => void): void
  export function disAutoConnect(): void
}

declare module 'vue-plugin-hiprint/dist/print-lock.css?url' {
  const url: string
  export default url
}

interface Window {
  $?: (value: unknown) => unknown
  hiwebSocket?: {
    opened: boolean
    getPrinterList: () => import('./types').PrintDevice[]
    refreshPrinterList: () => void
  }
  hinnn?: {
    event: {
      clear: (name: string) => void
      off: (name: string, callback: (payload: unknown) => void) => void
      on: (name: string, callback: (payload: unknown) => void) => void
      trigger: (name: string, payload?: unknown) => void
    }
  }
}
