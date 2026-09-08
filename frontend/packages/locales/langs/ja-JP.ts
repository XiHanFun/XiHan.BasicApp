// 只含 packages（admin shell）自身的文案命名空间。
// 应用业务文案（identity/setting/log/message/tenant/... 等）在 src/locales，
// 由 src 启动时经 registerLocaleMessages() 合并进同一个 i18n 实例——
// 底层包不该知道本应用有哪些业务模块。
import checkUpdates from './ja-JP/check_updates'
import common from './ja-JP/common'
import component from './ja-JP/component'
import error from './ja-JP/error'
import header from './ja-JP/header'
import island from './ja-JP/island'
import menu from './ja-JP/menu'
import page from './ja-JP/page'
import preference from './ja-JP/preference'
import tabbar from './ja-JP/tabbar'

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
