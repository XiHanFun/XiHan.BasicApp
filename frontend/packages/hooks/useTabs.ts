import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { HOME_PATH } from '~/constants'
import { useTabbarStore } from '~/stores'

/**
 * Tab 管理组合式函数
 *
 * 封装 tabbarStore 的全部操作，并自动关联当前路由，
 * 页面组件中直接调用即可，无需手动操作 store 和 router。
 *
 */
export function useTabs() {
  const route = useRoute()
  const router = useRouter()
  const tabbarStore = useTabbarStore()

  /** 当前路由对应的 tab */
  const currentTab = computed(() => tabbarStore.tabs.find(tab => tab.path === route.path))

  /** 各关闭操作的禁用状态（供菜单项绑定） */
  const disableState = computed(() => {
    const tabs = tabbarStore.tabs
    const currentPath = route.path
    const currentIndex = tabs.findIndex(tab => tab.path === currentPath)
    const closableTabs = tabs.filter(t => t.closable)

    // 当前路由还没有对应标签（标签被别处关掉、或路由尚未 ensureTab）：
    // 此时 store 的 closeLeft/closeRight 会直接 return、closeOthers 会误清空全部可关闭标签，
    // 因此除「关闭全部」外一律禁用，避免菜单项可点却无效（或反而做错事）。
    if (currentIndex < 0) {
      return {
        closeCurrent: true,
        closeLeft: true,
        closeRight: true,
        closeOthers: true,
        closeAll: closableTabs.length === 0,
      }
    }

    const tab = tabs[currentIndex]
    const leftClosable = tabs.slice(0, currentIndex).some(t => t.closable)
    const rightClosable = tabs.slice(currentIndex + 1).some(t => t.closable)

    return {
      closeCurrent: !tab?.closable,
      closeLeft: !leftClosable,
      closeRight: !rightClosable,
      closeOthers: closableTabs.length <= (tab?.closable ? 1 : 0),
      closeAll: closableTabs.length === 0,
    }
  })

  /** 关闭当前 tab，自动跳转到上一个 */
  function closeCurrentTab() {
    tabbarStore.removeTab(route.path)
    router.push(tabbarStore.activeTab)
  }

  /** 关闭当前 tab 左侧所有可关闭 tab */
  function closeLeftTabs() {
    tabbarStore.closeLeft(route.path)
  }

  /** 关闭当前 tab 右侧所有可关闭 tab */
  function closeRightTabs() {
    tabbarStore.closeRight(route.path)
  }

  /** 关闭除当前 tab 以外的所有可关闭 tab */
  function closeOtherTabs() {
    tabbarStore.closeOthers(route.path)
  }

  /** 关闭全部可关闭 tab，返回首页 */
  function closeAllTabs() {
    tabbarStore.closeAll()
    router.push(HOME_PATH)
  }

  /** 刷新当前 tab 对应的页面内容 */
  function refreshCurrentTab() {
    tabbarStore.refreshTab(route.path)
  }

  /**
   * 切换当前 tab 的固定状态。
   * @param path 指定路径，默认为当前路由
   */
  function toggleTabPin(path?: string) {
    tabbarStore.togglePin(path ?? route.path)
  }

  /**
   * 用掉一次性的路由参数（深链里「打开某条详情」「切到某个页签」这类意图）。
   * 标签按完整地址区分：参数留在地址里，标签就一直记着它，切回来或刷新会再触发一次；
   * 这里把地址换成去掉这些参数的地址，带参数的那个标签并入目标标签。
   * @param keys 要用掉的参数名
   */
  async function consumeQuery(keys: readonly string[]) {
    const from = route.fullPath
    const query = { ...route.query }
    for (const key of keys)
      delete query[key]
    await router.replace({ path: route.path, query, hash: route.hash })
    if (route.fullPath !== from)
      tabbarStore.removeTab(from)
  }

  /** 在新标签页中打开当前路由（或指定路径） */
  function openTabInNewWindow(path?: string) {
    const target = path ?? route.path
    window.open(target, '_blank', 'noopener,noreferrer')
  }

  return {
    currentTab,
    disableState,
    closeCurrentTab,
    closeLeftTabs,
    closeRightTabs,
    closeOtherTabs,
    closeAllTabs,
    refreshCurrentTab,
    toggleTabPin,
    consumeQuery,
    openTabInNewWindow,
  }
}
