import { onMounted, onUnmounted } from 'vue'
import { LAYOUT_EVENT_LOCK_SCREEN } from '~/constants'
import { useAppStore, useAuthStore, useLayoutBridgeStore } from '~/stores'

/**
 * 注册全局快捷键（搜索 / 退出 / 锁屏）及锁屏自定义事件监听。
 * 在 App 根组件中调用一次即可。
 */
export function useGlobalShortcuts() {
  const appStore = useAppStore()
  const authStore = useAuthStore()
  const layoutBridgeStore = useLayoutBridgeStore()

  function handleKeydown(e: KeyboardEvent) {
    if (!appStore.shortcutEnable)
      return

    // 用物理键 e.code 判定字母，兼容 Mac：Option(Alt) 会改变 e.key 字符（如 ⌥L→¬）
    // Ctrl/Cmd + K：全局搜索
    if (appStore.shortcutSearch && (e.ctrlKey || e.metaKey) && (e.code === 'KeyK' || e.key.toLowerCase() === 'k')) {
      e.preventDefault()
      layoutBridgeStore.requestOpenGlobalSearch()
      return
    }

    // Alt/Option + Q：退出登录
    if (appStore.shortcutLogout && e.altKey && (e.code === 'KeyQ' || e.key.toLowerCase() === 'q')) {
      e.preventDefault()
      void authStore.logout()
      return
    }

    // Alt/Option + L：锁屏
    if (appStore.shortcutLock && e.altKey && (e.code === 'KeyL' || e.key.toLowerCase() === 'l')) {
      e.preventDefault()
      if (appStore.widgetLockScreen) {
        layoutBridgeStore.requestLockScreen()
      }
      return
    }

    // Alt/Option + B：标签卡片总览（避开 Alt+W 等浏览器内置快捷键）
    if (appStore.shortcutTabOverview && e.altKey && (e.code === 'KeyB' || e.key.toLowerCase() === 'b')) {
      e.preventDefault()
      layoutBridgeStore.requestOpenTabOverview()
    }
  }

  function handleLockScreenRequest() {
    layoutBridgeStore.requestLockScreen()
  }

  onMounted(() => {
    window.addEventListener('keydown', handleKeydown)
    window.addEventListener(LAYOUT_EVENT_LOCK_SCREEN, handleLockScreenRequest)
  })

  onUnmounted(() => {
    window.removeEventListener('keydown', handleKeydown)
    window.removeEventListener(LAYOUT_EVENT_LOCK_SCREEN, handleLockScreenRequest)
  })
}
