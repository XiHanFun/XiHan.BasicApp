import { onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { dialog } from '~/composables'
import { useAppStore } from '~/stores'

function isLocalHost(): boolean {
  const { hostname } = window.location
  return ['localhost', '127.0.0.1', '[::1]', '0.0.0.0'].includes(hostname)
}

/**
 * 只在正式部署的站点上比对：开发服务器每次改动都会换掉首页指纹，本机跑的产物也不会被别人重新部署，
 * 两种情况下轮询只会白发请求甚至误报。挂载与偏好开关走同一把尺，免得两条路一个查一个不查。
 */
function canCheckUpdates(): boolean {
  return !import.meta.env.DEV && !isLocalHost()
}

/**
 * 定时检查前端资源是否有更新（通过 HEAD 请求对比 etag / last-modified）。
 * 检测到变化时弹出通知提示用户刷新页面。
 * 页面不可见时暂停轮询，重新可见时立即检查一次再恢复定时器。
 */
export function useCheckUpdates() {
  const appStore = useAppStore()
  const { t } = useI18n()

  let timer: ReturnType<typeof setInterval> | null = null
  /** 有一次比对还在路上：上一次取指纹没回来就又轮到下一次时，两次都会拿老基线比，提示要弹两遍 */
  let checking = false
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
    if (checking) {
      return
    }
    checking = true
    try {
      const tag = await getVersionTag()
      if (!tag) {
        return
      }

      if (versionTag.value && tag !== versionTag.value && !hasUpdate.value) {
        hasUpdate.value = true
        showUpdateNotification()
      }

      versionTag.value = tag
    }
    finally {
      checking = false
    }
  }

  function showUpdateNotification() {
    // 命令式 toast 挂不了操作钮，改用带确认的对话框：确认即刷新，取消即本轮不再提醒
    void dialog
      .confirm({
        title: t('check_updates.title'),
        content: t('check_updates.description'),
        badge: 'info',
        okText: t('check_updates.refresh'),
        onOk: () => {
          window.location.reload()
        },
      })
      .then((confirmed) => {
        if (!confirmed) {
          hasUpdate.value = false
        }
      })
  }

  function startTimer() {
    stopTimer()
    if (!appStore.enableCheckUpdates || !canCheckUpdates())
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

  /** 开始盯：先记下当前指纹当基线（没有基线就无从比对），再起定时器并接上可见性开关 */
  async function startWatching() {
    if (!canCheckUpdates()) {
      return
    }
    versionTag.value ??= await getVersionTag()
    startTimer()
    document.addEventListener('visibilitychange', handleVisibilityChange)
  }

  function stopWatching() {
    stopTimer()
    document.removeEventListener('visibilitychange', handleVisibilityChange)
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
        void startWatching()
      }
      else {
        stopWatching()
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

  onMounted(() => {
    if (!appStore.enableCheckUpdates) {
      return
    }
    void startWatching()
  })

  onBeforeUnmount(stopWatching)
}
