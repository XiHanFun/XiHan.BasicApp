<script setup lang="ts">
import { XhButton } from '@xihan-ui/vue'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/iconify'
import { useAuthStore, useUserStore } from '~/stores'

defineOptions({ name: 'ImpersonationBanner' })

const { t } = useI18n()
const authStore = useAuthStore()
const userStore = useUserStore()

const errorMessage = ref('')

const visible = computed(() => Boolean(userStore.userInfo?.isImpersonating))
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
    errorMessage.value = (error as Error)?.message || t('header.impersonation.stop_failed')
  }
}
</script>

<template>
  <div v-if="visible" class="impersonation-banner" role="status">
    <Icon icon="lucide:user-round-cog" width="16" height="16" class="shrink-0" />
    <span class="impersonation-banner-text">
      {{ t('header.impersonation.banner', { target: targetName, operator: impersonatorName }) }}
    </span>
    <span v-if="errorMessage" class="impersonation-banner-error">{{ errorMessage }}</span>
    <XhButton
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
  gap: 8px;
  align-items: center;
  padding: 8px 16px;
  font-size: 13px;
  color: hsl(var(--warning-foreground));
  background: hsl(var(--warning-surface));
  border-bottom: 1px solid hsl(var(--warning) / 35%);
}

.impersonation-banner-text {
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.impersonation-banner-error {
  color: hsl(var(--destructive));
}
</style>
