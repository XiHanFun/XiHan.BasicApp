// 只含 packages（admin shell）自身的文案命名空间。
// 应用业务文案（identity/setting/log/message/tenant/... 等）在 src/locales，
// 由 src 启动时经 registerLocaleMessages() 合并进同一个 i18n 实例——
// 底层包不该知道本应用有哪些业务模块。
import checkUpdates from './de-DE/check_updates'
import common from './de-DE/common'
import component from './de-DE/component'
import error from './de-DE/error'
import header from './de-DE/header'
import island from './de-DE/island'
import menu from './de-DE/menu'
import page from './de-DE/page'
import preference from './de-DE/preference'
import tabbar from './de-DE/tabbar'

export default {
  common,
  component,
  menu,
  header,
  tabbar,
  preference,
  page,
  island,
  error,
  check_updates: checkUpdates,
}
