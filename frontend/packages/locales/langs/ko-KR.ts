// 只含 packages（admin shell）自身的文案命名空间。
// 应用业务文案（identity/setting/log/message/tenant/... 等）在 src/locales，
// 由 src 启动时经 registerLocaleMessages() 合并进同一个 i18n 实例——
// 底层包不该知道本应用有哪些业务模块。
import checkUpdates from './ko-KR/check_updates'
import common from './ko-KR/common'
import component from './ko-KR/component'
import error from './ko-KR/error'
import header from './ko-KR/header'
import island from './ko-KR/island'
import menu from './ko-KR/menu'
import page from './ko-KR/page'
import preference from './ko-KR/preference'
import tabbar from './ko-KR/tabbar'

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
