// 只含 packages（admin shell）自身的文案命名空间。
// 应用业务文案（identity/setting/log/message/tenant/... 等）在 src/locales，
// 由 src 启动时经 registerLocaleMessages() 合并进同一个 i18n 实例——
// 底层包不该知道本应用有哪些业务模块。
import checkUpdates from './hi-IN/check_updates'
import common from './hi-IN/common'
import component from './hi-IN/component'
import error from './hi-IN/error'
import header from './hi-IN/header'
import island from './hi-IN/island'
import menu from './hi-IN/menu'
import page from './hi-IN/page'
import preference from './hi-IN/preference'
import tabbar from './hi-IN/tabbar'

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
