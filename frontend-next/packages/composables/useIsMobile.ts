import { readonly, ref } from 'vue'

/**
 * 全站统一的小屏（移动端尺寸）断点（px）。所有「是否小屏」判断都以此为唯一阈值，
 * 避免各处各自定义（分屏、侧栏、页脚……）导致 768 / 960 不一致。
 */
export const MOBILE_BREAKPOINT = 768

const MOBILE_QUERY = `(max-width: ${MOBILE_BREAKPOINT - 1}px)`

// 模块级单例：全站共享同一个 MediaQueryList 监听与响应式状态。
// 相比「每个组件各自建监听」只建一次、且不依赖组件生命周期钩子，
// 因此也能在 Pinia store 等非组件上下文中安全复用（见 split-view store）。
const mql = typeof window === 'undefined' ? null : window.matchMedia(MOBILE_QUERY)
const isMobileRef = ref(mql?.matches ?? false)
mql?.addEventListener('change', (e) => {
  isMobileRef.value = e.matches
})

/**
 * 检测当前视口是否为移动端/小屏尺寸（全站统一判定）。
 * 使用 MediaQueryList 监听，避免 resize 事件的性能开销。
 */
export function useIsMobile() {
  return { isMobile: readonly(isMobileRef) }
}
