import { registerModuleLocales } from '~/locales'

/** 身份包的启动钩子：登记本包文案；壳层扩展（个人中心入口等）随 N1 加入。 */
export default function setup(): void {
  registerModuleLocales(import.meta.glob<{ default: Record<string, unknown> }>('./locales/*.ts'))
}
