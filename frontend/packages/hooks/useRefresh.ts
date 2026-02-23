import { useRoute } from 'vue-router'
import { useTabbarStore } from '~/stores'

/**
 * 刷新当前页面的组合式函数。
 *
 * 通过递增 tabbarStore 中对应路径的 refreshSeed，
 * BasicLayout 中监听该值并销毁/重建页面组件，实现无刷新重载。
 *
 */
export function useRefresh() {
  const route = useRoute()
  const tabbarStore = useTabbarStore()

  /** 刷新当前路由对应的页面 */
  function refresh() {
    tabbarStore.refreshTab(route.path)
  }

  return { refresh }
}
