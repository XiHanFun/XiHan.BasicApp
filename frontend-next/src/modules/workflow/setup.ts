import { registerModuleLocales } from '~/locales'

/**
 * 工作流包的启动钩子：登记本包文案。
 * 页面由后端菜单下发（BasicApp:Modules:Workflow:Enabled 关掉即不下发）；流程设计器与查看器在打开对应页面时才注册。
 */
export default function setup(): void {
  registerModuleLocales(import.meta.glob<{ default: Record<string, unknown> }>('./locales/*.ts'))
}
