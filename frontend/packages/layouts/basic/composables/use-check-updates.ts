import { NButton, useNotification } from 'naive-ui'
import { h, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAppStore } from '~/stores'

function isLocalHost(): boolean {
  const { hostname } = window.location
  return ['localhost', '127.0.0.1', '[::1]', '0.0.0.0'].includes(hostname)
}

/**
 * 定时检查前端资源是否有更新（通过 HEAD 请求对比 etag / last-modified）。
 * 检测到变化时弹出通知提示用户刷新页面。
 * 页面不可见时暂停轮询，重新可见时立即检查一次再恢复定时器。
 */
export function useCheckUpdates() {
  const appStore = useAppStore()
  const notification = useNotification()
  const { t } = useI18n()

  let timer: ReturnType<typeof setInterval> | null = null
  const versionTag = ref<string | null>(null)
  const hasUpdate = ref(false)

  async function getVersionTag(): Promise<string | null> {
    try {
      const response = await fetch(import.meta.env.BASE_URL, {
        cache: 'no-cache',
        method: 'HEAD',
      })
      return (
        response.headers.get('etag') || response.headers.get('last-modified')
      )
    }
    catch {
      return null
    }
  }

  async function check() {
    const tag = await getVersionTag()
    if (!tag)
      return

    if (versionTag.value && tag !== versionTag.value && !hasUpdate.value) {
      hasUpdate.value = true
      showUpdateNotification()
    }

    versionTag.value = tag
  }

  function showUpdateNotification() {
    notification.info({
      title: t('checkUpdates.title'),
      content: t('checkUpdates.description'),
      action: () =>
        h(
          NButton,
          {
            type: 'primary',
            size: 'small',
            onClick: () => window.location.reload(),
          },
          { default: () => t('checkUpdates.refresh') },
        ),
      duration: 0,
      keepAliveOnHover: true,
      closable: true,
      onClose: () => {
        hasUpdate.value = false
      },
    })
  }

  function startTimer() {
    stopTimer()
    if (!appStore.enableCheckUpdates)
      return
    const seconds = Math.max(10, Math.min(300, appStore.checkUpdatesInterval))
    timer = setInterval(check, seconds * 1000)
  }

  function stopTimer() {
    if (timer) {
      clearInterval(timer)
      timer = null
    }
  }

  /** 页面可见性变化：隐藏时暂停轮询，恢复时立即检查一次再重启定时器 */
  function handleVisibilityChange() {
    if (document.hidden) {
      stopTimer()
    }
    else if (appStore.enableCheckUpdates) {
      check()
      startTimer()
    }
  }

  watch(
    () => appStore.enableCheckUpdates,
    (enabled) => {
      if (enabled) {
        check()
        startTimer()
      }
      else {
        stopTimer()
      }
    },
  )

  watch(
    () => appStore.checkUpdatesInterval,
    () => {
      if (appStore.enableCheckUpdates) {
        startTimer()
      }
    },
  )

  onMounted(async () => {
    if (import.meta.env.DEV || isLocalHost())
      return
    if (!appStore.enableCheckUpdates)
      return

    versionTag.value = await getVersionTag()
    startTimer()
    document.addEventListener('visibilitychange', handleVisibilityChange)
  })

  onBeforeUnmount(() => {
    stopTimer()
    document.removeEventListener('visibilitychange', handleVisibilityChange)
  })
}
