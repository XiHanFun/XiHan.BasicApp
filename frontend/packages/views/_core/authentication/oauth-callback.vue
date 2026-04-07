<script lang="ts" setup>
import type { LoginToken } from '~/types'
import { NIcon, NSpin, useMessage } from 'naive-ui'
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute } from 'vue-router'
import { useTheme } from '~/hooks'
import { Icon } from '~/iconify'
import { useAuthStore } from '~/stores'

defineOptions({ name: 'OAuthCallbackPage' })

const { isDark } = useTheme()
const { t } = useI18n()
const route = useRoute()
const authStore = useAuthStore()
const message = useMessage()

const loading = ref(true)
const errorMsg = ref<string | null>(null)

onMounted(async () => {
  const query = route.query
  const error = query.error as string | undefined
  if (error) {
    errorMsg.value = decodeURIComponent(error)
    loading.value = false
    message.error(errorMsg.value)
    setTimeout(() => {
      window.location.href = '/auth/login'
    }, 3000)
    return
  }

  const accessToken = query.accessToken as string | undefined
  const refreshToken = query.refreshToken as string | undefined

  if (!accessToken || !refreshToken) {
    errorMsg.value = t('page.auth.oauth_callback_missing_token')
    loading.value = false
    message.error(errorMsg.value)
    setTimeout(() => {
      window.location.href = '/auth/login'
    }, 3000)
    return
  }

  const token: LoginToken = {
    accessToken,
    refreshToken,
    tokenType: 'Bearer',
    expiresIn: Number(query.expiresIn) || 7200,
    issuedAt: new Date().toISOString(),
    expiresAt: '',
  }

  try {
    await authStore.handleOAuthCallback(token)
  }
  catch (err: unknown) {
    const e = err as { message?: string }
    errorMsg.value = e?.message || t('page.auth.oauth_callback_failed')
    message.error(errorMsg.value!)
    setTimeout(() => {
      window.location.href = '/auth/login'
    }, 3000)
  }
  finally {
    loading.value = false
  }
})
</script>

<template>
  <div class="flex flex-col justify-center items-center min-h-[300px] py-16">
    <template v-if="loading">
      <NSpin :size="48" />
      <p
        class="mt-6 text-base"
        :class="isDark ? 'text-gray-300' : 'text-[hsl(var(--muted-foreground))]'"
      >
        {{ t('page.auth.oauth_callback_loading') }}
      </p>
    </template>
    <template v-else-if="errorMsg">
      <div class="flex flex-col items-center gap-4">
        <div
          class="flex justify-center items-center w-16 h-16 rounded-full"
          :class="isDark ? 'bg-red-500/10' : 'bg-red-50'"
        >
          <NIcon :size="32" class="text-red-500">
            <Icon icon="lucide:x-circle" />
          </NIcon>
        </div>
        <p class="text-base font-medium text-red-500">
          {{ errorMsg }}
        </p>
        <p
          class="text-sm"
          :class="isDark ? 'text-gray-500' : 'text-[hsl(var(--muted-foreground))]'"
        >
          {{ t('page.auth.oauth_callback_redirect') }}
        </p>
      </div>
    </template>
  </div>
</template>
