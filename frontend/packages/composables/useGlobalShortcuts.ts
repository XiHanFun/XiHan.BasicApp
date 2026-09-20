import { onMounted, onUnmounted } from 'vue'
import { LAYOUT_EVENT_LOCK_SCREEN } from '~/constants'
import { useAppStore, useAuthStore, useLayoutBridgeStore } from '~/stores'
import { useHotkey } from './useHotkey'

/**
 * 全局快捷键的键位声明。注册端（useHotkey）与偏好设置里的键帽（XhKbd）读同一份。
 * `Mod` 在 Mac 上解析成 ⌘、其余平台解析成 Ctrl。
 */
export const GLOBAL_HOTKEYS = {
  search: ['Mod', 'K'],
  tabOverview: ['Alt', 'B'],
  lock: ['Alt', 'L'],
  logout: ['Alt', 'Q'],
} as const

/**
 * 注册全局快捷键（搜索 / 标签总览 / 锁屏 / 退出）及锁屏自定义事件监听。
 * 在 App 根组件中调用一次即可。
 */
export function useGlobalShortcuts() {
  const appStore = useAppStore()
  const authStore = useAuthStore()
  const layoutBridgeStore = useLayoutBridgeStore()

  useHotkey(() => ({
    keys: [...GLOBAL_HOTKEYS.search],
    enabled: appStore.shortcutEnable && appStore.shortcutSearch,
    onHotKey: () => layoutBridgeStore.requestOpenGlobalSearch(),
  }))

  useHotkey(() => ({
    keys: [...GLOBAL_HOTKEYS.tabOverview],
    enabled: appStore.shortcutEnable && appStore.shortcutTabOverview,
    onHotKey: () => layoutBridgeStore.requestOpenTabOverview(),
  }))

  useHotkey(() => ({
    keys: [...GLOBAL_HOTKEYS.lock],
    enabled: appStore.shortcutEnable && appStore.shortcutLock && appStore.widgetLockScreen,
    onHotKey: () => layoutBridgeStore.requestLockScreen(),
  }))

  useHotkey(() => ({
    keys: [...GLOBAL_HOTKEYS.logout],
    enabled: appStore.shortcutEnable && appStore.shortcutLogout,
    onHotKey: () => {
      void authStore.logout()
    },
  }))

  function handleLockScreenRequest() {
    layoutBridgeStore.requestLockScreen()
  }

  onMounted(() => {
    window.addEventListener(LAYOUT_EVENT_LOCK_SCREEN, handleLockScreenRequest)
  })

  onUnmounted(() => {
    window.removeEventListener(LAYOUT_EVENT_LOCK_SCREEN, handleLockScreenRequest)
  })
}
