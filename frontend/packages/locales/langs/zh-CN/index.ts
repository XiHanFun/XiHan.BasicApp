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
  develop,
  log,
  message,
  tenant,
  openapi,
  setting,
  identity,
  approval,
  file,
  workbench,
  check_updates: {
    title: '发现新版本',
    description: '检测到网页有更新，请刷新页面以加载最新版本。',
    refresh: '立即刷新',
  },
}
