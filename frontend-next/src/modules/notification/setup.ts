import { registerModuleLocales } from '~/locales'

/** 通知包的启动钩子：登记本包文案（顶栏通知中心与横幅 / 弹窗 / 强制阅读在壳层，经 AppContext 接 inbox.*）。 */
export default function setup(): void {
  registerModuleLocales(import.meta.glob<{ default: Record<string, unknown> }>('./locales/*.ts'))
}
