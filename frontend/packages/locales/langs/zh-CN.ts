// 只含 packages（admin shell）自身的文案命名空间。
// 应用业务文案（identity/setting/log/message/tenant/... 等）在 src/locales，
// 由 src 启动时经 registerLocaleMessages() 合并进同一个 i18n 实例——
// 底层包不该知道本应用有哪些业务模块。
import chat from './zh-CN/chat'
import checkUpdates from './zh-CN/check_updates'
import common from './zh-CN/common'
import component from './zh-CN/component'
import error from './zh-CN/error'
import header from './zh-CN/header'
import island from './zh-CN/island'
import menu from './zh-CN/menu'
import page from './zh-CN/page'
import preference from './zh-CN/preference'
import tabbar from './zh-CN/tabbar'

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
