import { computed, onMounted, onUnmounted, ref } from 'vue'
import { useContentMaximize } from '~/hooks'
import { useAppStore } from '~/stores'

/** 窄屏阈值：与布局 isNarrowScreen 保持一致（< 960 走悬浮入口） */
const NARROW_SCREEN_MAX_WIDTH = 960

/**
 * 偏好设置入口（头部按钮 / 固定悬浮 FAB）的可见性，二者互斥：
 * - position=header：仅头部按钮
 * - position=fixed：仅悬浮 FAB
 * - position=auto：窄屏 / 内容最大化 / 头部隐藏 / 全屏内容布局时走 FAB，否则走头部按钮
 *
 * 头部按钮（AppHeader）与悬浮 FAB（AppPreferenceDrawer）分属两个组件，
 * 抽到此处统一判定，避免「auto + 窄屏」两端各自为政导致同时显示。
 */
export function usePreferenceEntry() {
  const appStore = useAppStore()
  const { contentIsMaximize } = useContentMaximize()

  const viewportWidth = ref(typeof window !== 'undefined' ? window.innerWidth : 1200)
  const isNarrowScreen = computed(() => viewportWidth.value < NARROW_SCREEN_MAX_WIDTH)
  const isFullContentLayout = computed(() => appStore.layoutMode === 'full')

  function handleResize() {
    viewportWidth.value = window.innerWidth
  }

  onMounted(() => {
    handleResize()
    window.addEventListener('resize', handleResize)
  })

  onUnmounted(() => {
    window.removeEventListener('resize', handleResize)
  })

  /** 固定悬浮 FAB 是否显示 */
  const showFab = computed(() => {
    const position = appStore.widgetPreferencePosition
    if (position === 'header') {
      return false
    }
    if (position === 'fixed') {
      return true
    }
    return (
      isNarrowScreen.value
      || contentIsMaximize.value
      || !appStore.headerShow
      || isFullContentLayout.value
    )
  })

  /** 头部偏好设置按钮是否显示（与 FAB 互斥） */
  const showHeaderButton = computed(() => !showFab.value)

  return { showFab, showHeaderButton }
}
