<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script setup lang="ts">
import type { IslandHandle } from '~/composables'
import { XhButton } from '@xihan-ui/vue'
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { islandStatus, toast } from '~/composables'
import { Icon } from '~/iconify'
import { useAppStore, useAuthStore, useUserStore } from '~/stores'

/**
 * 模仿登录的状态提示
 * 灵动岛开着时常驻在岛上（展开面板里退出），关掉时退回顶部横幅：模仿中的提示不能消失
 */
defineOptions({ name: 'ImpersonationBanner' })

const ISLAND_ID = 'sys:impersonation'

const { t, locale } = useI18n()
const appStore = useAppStore()
const authStore = useAuthStore()
const userStore = useUserStore()

const errorMessage = ref('')

const visible = computed(() => Boolean(userStore.userInfo?.isImpersonating))
const onIsland = computed(() => visible.value && appStore.widgetDynamicIsland)
const targetName = computed(() => {
  const info = userStore.userInfo
  return info?.nickName || info?.userName || ''
})
const impersonatorName = computed(() => userStore.userInfo?.impersonatorUserName || '')

async function stop() {
  errorMessage.value = ''
  try {
    await authStore.stopImpersonation()
  }
  catch (error) {
    const message = (error as Error)?.message || t('header.impersonation.stop_failed')
    if (onIsland.value)
      toast.danger(message)
    else
      errorMessage.value = message
  }
}

let island: IslandHandle | null = null

watch([
  onIsland,
  targetName,
  impersonatorName,
  locale,
], ([show]) => {
  if (!show) {
    island?.dismiss()
    island = null
    return
  }
  // 同一个 ID 再调即更新文案（切换语言、资料刷新）
  island = islandStatus(ISLAND_ID, t('header.impersonation.island_label', { target: targetName.value }), {
    icon: 'lucide:user-round-cog',
    detail: t('header.impersonation.island_detail', { operator: impersonatorName.value }),
    actions: [
      {
        key: 'stop',
        label: t('header.impersonation.stop'),
        icon: 'lucide:log-out',
        tone: 'brand',
        // 退出成功会整页重建；失败时状态要留在岛上
        keepOpen: true,
        handler: () => void stop(),
      },
    ],
  })
}, { immediate: true })

onBeforeUnmount(() => {
  island?.dismiss()
  island = null
})
</script>

<template>
  <div v-if="visible && !onIsland" class="impersonation-banner" role="status">
    <Icon icon="lucide:user-round-cog" width="16" height="16" class="shrink-0" />
    <span class="impersonation-banner-text">
      {{ t('header.impersonation.banner', { target: targetName, operator: impersonatorName }) }}
    </span>
    <span v-if="errorMessage" class="impersonation-banner-error">{{ errorMessage }}</span>
    <XhButton
      variant="subtle"
      size="sm"
      tone="warning"
      :disabled="authStore.impersonationLoading"
      class="ml-auto shrink-0"
      @click="stop"
    >
      {{ t('header.impersonation.stop') }}
    </XhButton>
  </div>
</template>

<style scoped>
.impersonation-banner {
  display: flex;
  gap: var(--xh-space-2);
  align-items: center;
  padding: var(--xh-space-2) var(--xh-space-4);
  font-size: var(--xh-font-size-sm);
  /* 与通知横幅、组件库提示条同一套兑法：淡底 12%、描边 40%、文字往前景色兑到 60%，深浅两态都读得清 */
  color: color-mix(in oklab, var(--xh-color-warning-600) 60%, var(--xh-fg-default));
  background: color-mix(in oklab, var(--xh-color-warning-600) 12%, var(--xh-bg-surface));
  border-bottom: var(--xh-stroke-thin) solid color-mix(in oklab, var(--xh-color-warning-600) 40%, var(--xh-bg-surface));
}

.impersonation-banner-text {
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.impersonation-banner-error {
  color: var(--xh-fg-danger);
}
</style>
