import { registerModuleLocales } from '~/locales'

/** 设置包的启动钩子：登记本包文案。 */
export default function setup(): void {
  registerModuleLocales(import.meta.glob<{ default: Record<string, unknown> }>('./locales/*.ts'))
}
