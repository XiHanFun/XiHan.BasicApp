import { useDialog, useNotification } from 'naive-ui'
import { onMounted, onUnmounted, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useSignalR } from '~/composables'
import { useAccessStore, useAuthStore } from '~/stores'

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
  }

  function setupListeners() {
    signalR.on('ForceLogout', handleForceLogout)
    signalR.on('ReceiveNotification', handleReceiveNotification)
  }

  async function connect() {
    if (accessStore.accessToken) {
      setupListeners()
      await signalR.start()
    }
  }

  // 监听 token 变化：登录后连接，登出后断开
  const stopWatch = watch(
    () => accessStore.accessToken,
    async (token) => {
      if (token) {
        await connect()
      }
      else {
        await signalR.destroy()
      }
    },
  )

  onMounted(() => {
    connect()
  })

  onUnmounted(() => {
    stopWatch()
    signalR.destroy()
  })
}
