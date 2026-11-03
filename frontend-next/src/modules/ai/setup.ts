import { registerModuleLocales } from '~/locales'

/**
 * AI 模块的启动钩子：登记本模块文案
 * 页面由后端菜单下发（BasicApp:Modules:AI:Enabled 关掉即不下发），模块关着也无副作用
 */
export default function setup(): void {
  registerModuleLocales(import.meta.glob<{ default: Record<string, unknown> }>('./locales/*.ts'))
}
