import approval from './approval'
import common from './common'
import component from './component'
import develop from './develop'
import error from './error'
import file from './file'
import header from './header'
import identity from './identity'
import island from './island'
import log from './log'
import menu from './menu'
import message from './message'
import openapi from './openapi'
import page from './page'
import preference from './preference'
import setting from './setting'
import tabbar from './tabbar'
import tenant from './tenant'
import workbench from './workbench'

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
  // 子页为顶层键的区模块：嵌套到区名下（如 develop.ts = { code_gen: {...} } → develop.code_gen.*）
  develop,
  log,
  message,
  tenant,
  openapi,
  // 自身已包一层区名的区模块（如 setting.ts = { setting: {...} }）：展开合并，避免 setting.setting.* 双层嵌套
  ...setting,
  ...identity,
  ...approval,
  ...file,
  ...workbench,
  checkUpdates: {
    title: '发现新版本',
    description: '检测到网页有更新，请刷新页面以加载最新版本。',
    refresh: '立即刷新',
  },
}
