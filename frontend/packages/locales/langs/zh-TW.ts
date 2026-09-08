// 只含 packages（admin shell）自身的文案命名空间。
// 应用业务文案（identity/setting/log/message/tenant/... 等）在 src/locales，
// 由 src 启动时经 registerLocaleMessages() 合并进同一个 i18n 实例——
// 底层包不该知道本应用有哪些业务模块。
import checkUpdates from './zh-TW/check_updates'
import common from './zh-TW/common'
import component from './zh-TW/component'
import error from './zh-TW/error'
import header from './zh-TW/header'
import island from './zh-TW/island'
import menu from './zh-TW/menu'
import page from './zh-TW/page'
import preference from './zh-TW/preference'
import tabbar from './zh-TW/tabbar'

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
