/**
 * 本应用的业务文案。
 *
 * packages 的 i18n 实例只带 admin shell 自身的命名空间；业务命名空间
 * （approval/develop/file/identity/log/message/openapi/setting/tenant/workbench）
 * 属应用层，住在这里，启动时合并进同一实例——底层包不该知道本应用有哪些业务模块。
 *
 * 语言包是全量静态打包（非按需加载）：切语言时不会有网络往返，代价是首包体积。
 * 若将来要按语言分包，改成动态 import 后在此处 await 即可，调用方无需变。
 */
import { registerLocaleMessages } from '~/locales'
import enUS from './langs/en-US'
import zhCN from './langs/zh-CN'

/** 必须在 app.mount() 之前调用，否则首屏会渲染出裸 key */
export function setupBusinessLocales() {
  registerLocaleMessages({
    'zh-CN': zhCN,
    'en-US': enUS,
  })
}
