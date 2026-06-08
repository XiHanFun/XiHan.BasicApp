import { onMounted, onUnmounted, readonly, ref } from 'vue'

const MOBILE_BREAKPOINT = 768
const MOBILE_QUERY = `(max-width: ${MOBILE_BREAKPOINT - 1}px)`

/** 同步取当前是否移动端尺寸，用作初始值避免首帧按桌面态渲染后再切换造成闪烁 */
function matchMobile(): boolean {
  return typeof window !== 'undefined' && window.matchMedia(MOBILE_QUERY).matches
}

/**
 * 检测当前视口是否为移动端尺寸。
 * 使用 MediaQueryList 监听，避免 resize 事件的性能开销。
 */
export function useIsMobile() {
  const isMobile = ref(matchMobile())

  let mql: MediaQueryList | null = null

  function onMediaChange(e: MediaQueryListEvent | MediaQueryList) {
    isMobile.value = e.matches
  }

  onMounted(() => {
    mql = window.matchMedia(MOBILE_QUERY)
    onMediaChange(mql)
    mql.addEventListener('change', onMediaChange)
  })

  onUnmounted(() => {
    mql?.removeEventListener('change', onMediaChange)
  })

  return { isMobile: readonly(isMobile) }
}
