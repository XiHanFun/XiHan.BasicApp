/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { dialog } from '~/composables'
import { useAppStore } from '~/stores'

function isLocalHost(): boolean {
  const { hostname } = window.location
  return [
    'localhost',
    '127.0.0.1',
    '[::1]',
    '0.0.0.0',
  ].includes(hostname)
}

/**
 * 只在正式部署的站点上检查更新
 * 开发服务器与本机产物轮询只会白发请求甚至误报；挂载与偏好开关共用此判定
 */
function canCheckUpdates(): boolean {
  return !import.meta.env.DEV && !isLocalHost()
}

/**
 * 线上那一版的发布标记，取自 version.json，每次打包都换新值
 * 没有清单（老产物或别处托管）返回 null，本轮不判
 * 查询串绕开按 URL 缓存的 CDN 副本；缓存键不含查询串的（如 Workers 静态资产）靠发版换掉整套资产保鲜
 */
async function fetchDeployedStamp(): Promise<string | null> {
  try {
    const response = await fetch(`${import.meta.env.BASE_URL}version.json?_=${Date.now()}`, { cache: 'no-store' })
    if (!response.ok) {
      return null
    }
    const manifest = await response.json() as { buildStamp?: unknown }
    return typeof manifest.buildStamp === 'string'
      ? manifest.buildStamp
      : null
  }
  catch {
    return null
  }
}

/**
 * 定时检查前端新版，有新版时提示刷新
 * 比线上与当前产物的构建标记而不攒基线，发版后才打开的页面也判得出
 * 页面不可见时暂停，重新可见立即查一次再恢复定时
 */
export function useCheckUpdates() {
  const appStore = useAppStore()
  const { t } = useI18n()

  let timer: ReturnType<typeof setInterval> | null = null
  /** 有一次比对还在路上：上一次没回来就又轮到下一次时，两次都会判出同一版，提示要弹两遍 */
  let checking = false
  const hasUpdate = ref(false)
  /** 被按掉的那一版：同一版不再每轮追着问，换了新版才重新提醒 */
  const dismissedVersion = ref<string | null>(null)

  async function check() {
    if (checking) {
      return
    }
    checking = true
    try {
      const deployedStamp = await fetchDeployedStamp()
      if (!deployedStamp || deployedStamp === __APP_BUILD_STAMP__) {
        return
      }
      if (deployedStamp !== dismissedVersion.value && !hasUpdate.value) {
        hasUpdate.value = true
        showUpdateNotification(deployedStamp)
      }
    }
    finally {
      checking = false
    }
  }

  function showUpdateNotification(version: string) {
    // 命令式 toast 挂不了操作钮，改用带确认的对话框：确认即刷新，取消即本版不再提醒
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

  /** 开始盯：先比一次（页面可能就是在发版之后打开的），再起定时器并接上可见性开关 */
  function startWatching() {
    if (!canCheckUpdates()) {
      return
    }
    void check()
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
      void check()
      startTimer()
    }
  }

  watch(
    () => appStore.enableCheckUpdates,
    (enabled) => {
      if (enabled) {
        startWatching()
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
    startWatching()
  })

  onBeforeUnmount(stopWatching)
}
