import { registerModuleLocales } from '~/locales'

/** 运维包的启动钩子：登记本包文案（任务调度页）。 */
export default function setup(): void {
  registerModuleLocales(import.meta.glob<{ default: Record<string, unknown> }>('./locales/*.ts'))
}
