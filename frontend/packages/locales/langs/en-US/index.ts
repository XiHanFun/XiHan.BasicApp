import common from './common'
import error from './error'
import header from './header'
import island from './island'
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
  island,
  error,
  checkUpdates: {
    title: 'New Version Available',
    description: 'A new version has been detected. Please refresh the page to load the latest version.',
    refresh: 'Refresh Now',
  },
}
