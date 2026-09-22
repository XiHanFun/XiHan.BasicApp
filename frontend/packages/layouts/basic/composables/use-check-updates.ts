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
 * 首页引用的那批资源地址：打包每次都会换掉其中的哈希，是比校验头更硬的版本证据。
 * 用 DOMParser 而不是正则去捞：只是解析成文档树，不执行脚本、不加载资源，也不必跟正则的回溯较劲。
 */
function collectAssetRefs(html: string): string {
  const doc = new DOMParser().parseFromString(html, 'text/html')
  const refs = [...doc.querySelectorAll('script[src], link[href]')]
    .map(element => element.getAttribute('src') ?? element.getAttribute('href') ?? '')
    .filter(Boolean)
  return refs.join('|')
}

/** FNV-1a：把首页正文压成一个短串，省得把整页 HTML 留在内存里逐字比 */
function hashText(text: string): string {
  let hash = 0x811C9DC5
  for (let i = 0; i < text.length; i++) {
    hash ^= text.charCodeAt(i)
    hash = Math.imul(hash, 0x01000193)
  }
  return (hash >>> 0).toString(36)
}

/**
 * 线上那一版的发布标记。构建时随产物落一份 version.json，同一份源码重复打包也会换新值，
 * 所以「发了一版」一定认得出来——只看资源哈希的话，源码没动时两次发布逐字节相同，发了也看不出来。
 * 旧产物或别处托管没有这份清单，返回 null 交给首页指纹那条路兜。
 */
async function fetchDeployedStamp(): Promise<string | null> {
  try {
    const response = await fetch(`${import.meta.env.BASE_URL}version.json?_=${Date.now()}`, { cache: 'no-store' })
    if (!response.ok) {
      return null
    }
    const manifest = await response.json() as { buildStamp?: unknown }
    return typeof manifest.buildStamp === 'string' ? manifest.buildStamp : null
  }
  catch {
    return null
  }
}

/**
 * 定时检查前端资源是否有更新。
 * 先比发布清单里的构建标记（与当前这份产物里打进来的那个比），没有清单再退到首页指纹。
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
  /** 被按掉的那一版：同一版不再每轮追着问，换了新版才重新提醒 */
  const dismissedVersion = ref<string | null>(null)

  /**
   * 取首页指纹。校验头优先，但不能只靠它：CDN 压缩（zstd / br）后常把 etag 一并剥掉，
   * 静态站又未必给 last-modified——两头都没有时，这个功能会静悄悄地一直不报更新。
   * 所以拿不到校验头就退到正文：首页里那串带哈希的资源引用每次打包都会换。
   * 查询串是为了绕开按 URL 记缓存的那类 CDN 边缘副本；把查询串排除在缓存键外的（如 Workers 静态资产）
   * 对它无感，那边靠发版本身换掉整套资产来保证新鲜。
   */
  async function getVersionTag(): Promise<string | null> {
    try {
      const response = await fetch(`${import.meta.env.BASE_URL}?_=${Date.now()}`, { cache: 'no-store' })
      if (!response.ok) {
        return null
      }
      const validator = response.headers.get('etag') ?? response.headers.get('last-modified')
      if (validator) {
        return validator
      }
      const html = await response.text()
      // 连资源引用都捞不到（首页形态异常）就退而求其次，整页正文照样能当指纹
      return hashText(collectAssetRefs(html) || html)
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
      // 有发布清单就直接比「线上这一版」与「我正在跑的这一版」，不必先攒基线：
      // 页面是在发版之后打开的也照样判得出来
      const deployedStamp = await fetchDeployedStamp()
      if (deployedStamp) {
        if (deployedStamp !== __APP_BUILD_STAMP__ && deployedStamp !== dismissedVersion.value && !hasUpdate.value) {
          hasUpdate.value = true
          showUpdateNotification(deployedStamp)
        }
        return
      }

      const tag = await getVersionTag()
      if (!tag) {
        return
      }

      if (versionTag.value && tag !== versionTag.value && !hasUpdate.value) {
        hasUpdate.value = true
        showUpdateNotification(tag)
      }

      versionTag.value = tag
    }
    finally {
      checking = false
    }
  }

  function showUpdateNotification(version: string) {
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
          dismissedVersion.value = version
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
