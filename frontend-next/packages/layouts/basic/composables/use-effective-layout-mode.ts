import { computed } from 'vue'
import { useIsMobile } from '~/composables/useIsMobile'
import { useAppStore } from '~/stores'

/**
 * 生效布局模式：小屏一律按 side（垂直）渲染。
 *
 * top / mix / header-mix / side-mixed / full 都依赖横向空间或双列，窄屏下会挤成不可用的形态。
 * 此处只改渲染，不动用户存的偏好，回到大屏即恢复原选择。
 *
 * 布局相关组件一律读本值，不要直接读 `appStore.layoutMode`：直接读会绕过这条强制，
 * 出现外壳已按 side 计算、而头部或侧栏仍在渲染多列形态的错位。
 */
export function useEffectiveLayoutMode() {
  const appStore = useAppStore()
  const { isMobile } = useIsMobile()

  return computed(() => (isMobile.value ? 'side' : appStore.layoutMode))
}
