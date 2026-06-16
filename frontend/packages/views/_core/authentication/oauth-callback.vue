<script lang="ts" setup>
import type { LoginToken } from '~/types'
import { NIcon, NSpin, useMessage } from 'naive-ui'
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { LOGIN_PATH } from '~/constants'
import { useTheme } from '~/hooks'
import { Icon } from '~/iconify'
import { useAuthStore } from '~/stores'

defineOptions({ name: 'OAuthCallbackPage' })

const { isDark } = useTheme()
const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const message = useMessage()

const loading = ref(true)
const errorMsg = ref<string | null>(null)

const BIND_ERROR_TEXT: Record<string, string> = {
  conflict: '该第三方账号已被其他用户绑定',
  ticket_invalid: '绑定请求已失效，请重新发起',
  external_profile_invalid: '未能获取第三方账号信息',
}

onMounted(async () => {
  const query = route.query

  // 绑定回调（已登录用户从个人中心发起）：提示后回到个人中心「账号绑定」
  const bind = query.bind as string | undefined
  if (bind) {
    loading.value = false
    if (bind === 'success') {
      message.success('第三方账号绑定成功')
    }
    else {
      message.error(BIND_ERROR_TEXT[bind] ?? '绑定失败')
    }
    setTimeout(() => {
      void router.push({ path: '/workbench/profile', query: { tab: 'binding' } })
    }, 1200)
    return
  }

  const error = query.error as string | undefined
  if (error) {
    errorMsg.value = decodeURIComponent(error)
    loading.value = false
    message.error(errorMsg.value)
    setTimeout(() => {
      window.location.href = LOGIN_PATH
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
      window.location.href = LOGIN_PATH
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
      window.location.href = LOGIN_PATH
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
