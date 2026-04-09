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

    // Ctrl/Cmd + K：全局搜索
    if (appStore.shortcutSearch && (e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') {
      e.preventDefault()
      layoutBridgeStore.requestOpenGlobalSearch()
      return
    }

    // Alt + Q：退出登录
    if (appStore.shortcutLogout && e.altKey && e.key.toLowerCase() === 'q') {
      e.preventDefault()
      authStore.logout()
      return
    }

    // Alt + L：锁屏
    if (appStore.shortcutLock && e.altKey && e.key.toLowerCase() === 'l') {
      e.preventDefault()
      if (appStore.widgetLockScreen) {
        layoutBridgeStore.requestLockScreen()
      }
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
