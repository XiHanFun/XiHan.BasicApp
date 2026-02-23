import { onMounted, onUnmounted, readonly, ref } from 'vue'

const MOBILE_BREAKPOINT = 768

/**
 * 检测当前视口是否为移动端尺寸。
 * 使用 MediaQueryList 监听，避免 resize 事件的性能开销。
 */
export function useIsMobile() {
  const isMobile = ref(false)

  let mql: MediaQueryList | null = null

  function onMediaChange(e: MediaQueryListEvent | MediaQueryList) {
    isMobile.value = e.matches
  }

  onMounted(() => {
    mql = window.matchMedia(`(max-width: ${MOBILE_BREAKPOINT - 1}px)`)
    onMediaChange(mql)
    mql.addEventListener('change', onMediaChange)
  })

  onUnmounted(() => {
    mql?.removeEventListener('change', onMediaChange)
  })

  return { isMobile: readonly(isMobile) }
}
