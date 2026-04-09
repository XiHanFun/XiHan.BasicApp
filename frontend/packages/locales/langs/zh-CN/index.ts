import common from './common'
import error from './error'
import header from './header'
import menu from './menu'
import page from './page'
import preference from './preference'
import tabbar from './tabbar'

export default {
  common,
  menu,
  header,
  tabbar,
  preference,
  page,
  error,
  checkUpdates: {
    title: '发现新版本',
    description: '检测到网页有更新，请刷新页面以加载最新版本。',
    refresh: '立即刷新',
  },
}
