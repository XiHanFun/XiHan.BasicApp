import { registerModuleLocales } from '~/locales'

/** 工作台模块的启动钩子：登记本模块文案。页面由后端菜单下发（BasicApp:Modules:Workbench:Enabled 关掉即不下发）。 */
export default function setup(): void {
  registerModuleLocales(import.meta.glob<{ default: Record<string, unknown> }>('./locales/*.ts'))
}
