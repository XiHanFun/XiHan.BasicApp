// 只含 packages（admin shell）自身的文案命名空间。
// 应用业务文案（identity/setting/log/message/tenant/... 等）在 src/locales，
// 由 src 启动时经 registerLocaleMessages() 合并进同一个 i18n 实例——
// 底层包不该知道本应用有哪些业务模块。
import chat from './en-US/chat'
import checkUpdates from './en-US/check_updates'
import common from './en-US/common'
import component from './en-US/component'
import error from './en-US/error'
import header from './en-US/header'
import island from './en-US/island'
import menu from './en-US/menu'
import page from './en-US/page'
import preference from './en-US/preference'
import tabbar from './en-US/tabbar'

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
  chat,
  check_updates: checkUpdates,
}
