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
  // Area modules whose top-level keys are sub-pages: nest under the area name (e.g. develop.ts = { code_gen: {...} } → develop.code_gen.*)
  develop,
  log,
  message,
  tenant,
  openapi,
  // Area modules that already wrap themselves under the area name (e.g. setting.ts = { setting: {...} }): spread to avoid double nesting like setting.setting.*
  ...setting,
  ...identity,
  ...approval,
  ...file,
  ...workbench,
  checkUpdates: {
    title: 'New Version Available',
    description: 'A new version has been detected. Please refresh the page to load the latest version.',
    refresh: 'Refresh Now',
  },
}
