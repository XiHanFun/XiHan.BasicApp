import type { ServerTaskProgressPayload } from '~/composables'
import { useDialog, useNotification } from 'naive-ui'
import { onMounted, onUnmounted, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { applyServerTaskProgress, islandStatus, useSignalR } from '~/composables'
import { useAccessStore, useAuthStore } from '~/stores'

const SIGNALR_RECONNECT_INTERVAL_MS = 15000
const NOTIFICATION_RECEIVED_EVENT = 'xihan:notification-received'

/**
 * 从 JWT payload 中提取 session_id
 */
function getSessionIdFromToken(token: string | null): string | null {
  if (!token)
    return null
  try {
    const parts = token.split('.')
    if (parts.length < 2)
      return null
    const payload = JSON.parse(atob(parts[1]!.replace(/-/g, '+').replace(/_/g, '/')))
    return payload?.session_id ?? null
  }
  catch {
    return null
  }
}

/**
 * 在 BasicLayout 中初始化 SignalR，处理实时通知和踢下线
 * 需在 NDialogProvider / NNotificationProvider 内使用
 */
export function useSignalRIntegration() {
  const { t } = useI18n()
  const accessStore = useAccessStore()
  const authStore = useAuthStore()
  const notification = useNotification()
  const dialog = useDialog()
  const signalR = useSignalR()

  let isHandlingForceLogout = false
  let isListenersBound = false
  let reconnectTimer: ReturnType<typeof setInterval> | null = null

  function handleForceLogout(payload: { reason?: string, targetSessionIds?: string[] }) {
    if (isHandlingForceLogout)
      return

    // 如果后端指定了目标会话列表，当前会话不在列表中则忽略
    if (payload?.targetSessionIds?.length) {
      const mySessionId = getSessionIdFromToken(accessStore.accessToken)
      if (mySessionId && !payload.targetSessionIds.includes(mySessionId))
        return
    }

    isHandlingForceLogout = true

    // 立即清除登录状态，防止刷新浏览器后仍可操作
    signalR.stop()
    accessStore.$reset()

    dialog.warning({
      title: t('page.signalr.force_logout_title'),
      content: payload?.reason || t('page.signalr.force_logout_reason'),
      positiveText: t('page.signalr.force_logout_confirm'),
      closable: false,
      maskClosable: false,
      onPositiveClick: async () => {
        isHandlingForceLogout = false
        await authStore.logout()
      },
      onAfterLeave: () => {
        isHandlingForceLogout = false
      },
    })
  }

  function handleReceiveNotification(payload: {
    type?: string
    title?: string
    content?: string
  }) {
    const typeMap: Record<string, 'info' | 'success' | 'warning' | 'error'> = {
      Info: 'info',
      Success: 'success',
      Warning: 'warning',
      Error: 'error',
    }
    const nType = typeMap[payload?.type ?? 'Info'] ?? 'info'

    notification[nType]({
      title: payload?.title || t('page.signalr.new_notification'),
      content: payload?.content || '',
      duration: 5000,
      closable: false,
    })

    window.dispatchEvent(new CustomEvent(NOTIFICATION_RECEIVED_EVENT, { detail: payload }))
  }

  function clearReconnectTimer() {
    if (reconnectTimer) {
      clearInterval(reconnectTimer)
      reconnectTimer = null
    }
  }

  function ensureReconnectTimer() {
    if (reconnectTimer || !accessStore.accessToken) {
      return
    }

    reconnectTimer = setInterval(() => {
      void connect()
    }, SIGNALR_RECONNECT_INTERVAL_MS)
  }

  function setupListeners() {
    if (isListenersBound) {
      return
    }
    signalR.on('ForceLogout', handleForceLogout)
    signalR.on('ReceiveNotification', handleReceiveNotification)
    // 服务端后台任务进度 → 灵动岛（同 taskId 复用同一条，刷新后由会话级持久化恢复）
    signalR.on('TaskProgress', payload => applyServerTaskProgress(payload as ServerTaskProgressPayload))
    isListenersBound = true
  }

  async function connect() {
    if (accessStore.accessToken) {
      setupListeners()
      await signalR.start()
      if (signalR.connected.value) {
        clearReconnectTimer()
      }
      else {
        ensureReconnectTimer()
      }
    }
  }

  // 监听 token 变化：登录后连接，登出后断开
  const stopTokenWatch = watch(
    () => accessStore.accessToken,
    async (token) => {
      if (token) {
        await connect()
      }
      else {
        clearReconnectTimer()
        await signalR.destroy()
        isListenersBound = false
      }
    },
  )

  // 实时连接状态进灵动岛（常驻提示，仅在"曾连上后断开"时出现，避免登录初期噪音）
  let realtimeHandle: ReturnType<typeof islandStatus> | null = null
  const stopConnectedWatch = watch(
    () => signalR.connected.value,
    (isConnected, wasConnected) => {
      if (!accessStore.accessToken) {
        clearReconnectTimer()
        realtimeHandle?.dismiss()
        realtimeHandle = null
        return
      }
      if (isConnected) {
        clearReconnectTimer()
        if (realtimeHandle) {
          realtimeHandle.success(t('island.realtime_restored'))
          realtimeHandle = null
        }
      }
      else {
        ensureReconnectTimer()
        if (wasConnected) {
          realtimeHandle = islandStatus('sys:realtime', t('island.realtime_lost'), {
            state: 'error',
            icon: 'lucide:plug-zap',
            detail: t('island.realtime_lost_detail'),
          })
        }
      }
    },
  )

  onMounted(() => {
    void connect()
  })

  onUnmounted(() => {
    stopTokenWatch()
    stopConnectedWatch()
    clearReconnectTimer()
    isListenersBound = false
    void signalR.destroy()
  })
}
