import { registerModuleLocales } from '~/locales'

/** 开放平台包的启动钩子：登记本包文案（应用管理页；个人中心的「开放接口」「OAuth 应用」页签在壳层，经 AppContext 接 my-api-credentials.* 与 my-oauth-apps.*）。 */
export default function setup(): void {
  registerModuleLocales(import.meta.glob<{ default: Record<string, unknown> }>('./locales/*.ts'))
}
