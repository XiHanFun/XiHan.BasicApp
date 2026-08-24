<script lang="ts" setup>
import type { LoginToken } from '~/types'
import { XhSpinner } from '@xihan-ui/vue'
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { toast } from '~/composables'
import { LOGIN_PATH } from '~/constants'
import { useTheme } from '~/hooks'
import { Icon } from '~/iconify'
import { useAppContext, useAuthStore } from '~/stores'

defineOptions({ name: 'OAuthCallbackPage' })

const { isDark } = useTheme()
const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const appContext = useAppContext()

const loading = ref(true)
const errorMsg = ref<string | null>(null)

const BIND_ERROR_TEXT: Record<string, string> = {
  conflict: t('page.auth.oauth_bind_err_conflict'),
  ticket_invalid: t('page.auth.oauth_bind_err_ticket_invalid'),
  external_profile_invalid: t('page.auth.oauth_bind_err_profile_invalid'),
}

/**
 * 读取回跳参数。
 *
 * 后端把参数一律放进 URL 片段（片段不进服务器访问日志、也不随 Referer 外发，令牌不会外泄）。
 * 哈希路由下形如 `#/auth/oauth-callback?accessToken=x`，vue-router 已把它解析进 route.query；
 * history 路由下形如 `/auth/oauth-callback#accessToken=x`，路由取不到，这里补一层从 location.hash 解析。
 */
function readCallbackParams(): Record<string, string> {
  const params: Record<string, string> = {}

  for (const [key, value] of Object.entries(route.query)) {
    if (typeof value === 'string') {
      params[key] = value
    }
  }

  const hash = window.location.hash
  // `#/` 开头的是哈希路由的路径，其中的参数已由 route.query 给出，再解析一次只会得到脏键
  if (hash.startsWith('#') && !hash.startsWith('#/')) {
    for (const [key, value] of new URLSearchParams(hash.slice(1))) {
      params[key] ??= value
    }
  }

  return params
}

onMounted(async () => {
  const query = readCallbackParams()

  // 绑定回调（已登录用户从个人中心发起）：提示后回到个人中心「账号绑定」
  const bind = query.bind
  if (bind) {
    loading.value = false
    if (bind === 'success') {
      toast.success(t('page.auth.oauth_bind_success'))
    }
    else {
      toast.error(BIND_ERROR_TEXT[bind] ?? t('page.auth.oauth_bind_failed'))
    }
    setTimeout(() => {
      // 个人中心路由由应用注册；未配置时回落到首页，别把用户扔到一个不存在的路径上
      const profilePath = appContext.shellRoutes.profile
      void (profilePath
        ? router.push({ path: profilePath, query: { tab: 'binding' } })
        : router.push('/'))
    }, 1200)
    return
  }

  const error = query.error
  if (error) {
    errorMsg.value = decodeURIComponent(error)
    loading.value = false
    toast.error(errorMsg.value)
    setTimeout(() => {
      void router.push(LOGIN_PATH)
    }, 3000)
    return
  }

  const accessToken = query.accessToken
  const refreshToken = query.refreshToken

  if (!accessToken || !refreshToken) {
    errorMsg.value = t('page.auth.oauth_callback_missing_token')
    loading.value = false
    toast.error(errorMsg.value)
    setTimeout(() => {
      void router.push(LOGIN_PATH)
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
    toast.error(errorMsg.value!)
    setTimeout(() => {
      void router.push(LOGIN_PATH)
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
      <XhSpinner />
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
          <span class="text-red-500" style="display: inline-flex; font-size: 32px"><Icon icon="lucide:x-circle" /></span>
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
