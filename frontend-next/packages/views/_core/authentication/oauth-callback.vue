<script lang="ts" setup>
import type { OAuthMode } from '~/stores'
import { XhSpinner } from '@xihan-ui/vue'
import { onBeforeUnmount, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { toast } from '~/composables'
import { LOGIN_PATH } from '~/constants'
import { useTheme } from '~/hooks'
import { Icon } from '~/iconify'
import { ApiError } from '~/request'
import { takeOAuthPending, useAppContext, useAuthStore } from '~/stores'

defineOptions({ name: 'OAuthCallbackPage' })

const { isDark } = useTheme()
const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const appContext = useAppContext()

const loading = ref(true)
const errorMsg = ref<string | null>(null)
/** 出错后回哪：登录回登录页，绑定（已登录）回个人中心「账号绑定」 */
const backToProfile = ref(false)
let timer: ReturnType<typeof setTimeout> | undefined

/** 后端回跳带的错误码（第三方那边失败一律回到这里）→ 友好文案；不认识的按登录 / 绑定失败说 */
const ERROR_TEXT: Record<string, string> = {
  invalid_provider: 'page.auth.oauth_err_invalid_provider',
  invalid_state: 'page.auth.oauth_err_invalid_state',
  access_denied: 'page.auth.oauth_err_access_denied',
  external_auth_failed: 'page.auth.oauth_err_external_auth_failed',
}

/**
 * 读取回跳参数：后端把票据（或错误码）、用途与提供方放在回调页地址的查询串里——
 * 哈希路由下形如 `#/auth/oauth-callback?ticket=x`，history 路由下形如 `/auth/oauth-callback?ticket=x`，都由 route.query 给出。
 * 4.x 把令牌放在地址片段里、另从 location.hash 解析；6.0 回来的只是一张一次性票据，不再有令牌进地址。
 */
function readCallbackParams(): Record<string, string> {
  const params: Record<string, string> = {}
  for (const [key, value] of Object.entries(route.query)) {
    const first = Array.isArray(value) ? value[0] : value
    if (typeof first === 'string') {
      params[key] = first
    }
  }
  return params
}

/** 服务端错误取本地化后的 detail（不带错误码前缀，未绑定、票据过期、账号停用或锁定都在这里说清），其余用兜底文案 */
function failureText(error: unknown, fallback: string): string {
  return error instanceof ApiError && error.detail ? error.detail : t(fallback)
}

function goProfileBinding() {
  // 个人中心路由由应用注册；未配置时回落到首页，别把用户扔到一个不存在的路径上
  const profilePath = appContext.shellRoutes.profile
  return profilePath
    ? router.replace({ path: profilePath, query: { tab: 'binding' } })
    : router.replace('/')
}

/** 失败：提示并留在本页说明原因，稍后回登录页（绑定且仍登录着的回个人中心） */
function fail(message: string, mode: OAuthMode) {
  backToProfile.value = mode === 'bind' && authStore.isAuthenticated
  errorMsg.value = message
  loading.value = false
  toast.danger(message)
  timer = setTimeout(() => {
    void (backToProfile.value ? goProfileBinding() : router.replace(LOGIN_PATH))
  }, 3000)
}

/** 确认绑定（个人中心发起）：票据绑到当前登录的人，成功回个人中心「账号绑定」 */
async function confirmBind(ticket: string) {
  if (!authStore.isAuthenticated) {
    fail(t('page.auth.oauth_bind_login_required'), 'login')
    return
  }
  try {
    await appContext.apis.linkAccountApi(ticket)
    toast.success(t('page.auth.oauth_bind_success'))
    await goProfileBinding()
  }
  catch (error) {
    fail(failureText(error, 'page.auth.oauth_bind_failed'), 'bind')
  }
}

/** 第三方登录：票据换会话；开了双因素时 store 带着挑战回登录页接着验证 */
async function completeLogin(ticket: string, redirect?: string) {
  try {
    await authStore.handleOAuthCallback(ticket, redirect)
  }
  catch (error) {
    fail(failureText(error, 'page.auth.oauth_callback_failed'), 'login')
  }
}

onMounted(async () => {
  const query = readCallbackParams()
  const pending = takeOAuthPending()
  const mode: OAuthMode = (query.mode ?? pending?.mode) === 'bind' ? 'bind' : 'login'

  const error = query.error
  if (error) {
    const key = ERROR_TEXT[error]
    fail(key ? t(key) : t(mode === 'bind' ? 'page.auth.oauth_bind_failed' : 'page.auth.oauth_callback_failed'), mode)
    return
  }

  const ticket = query.ticket
  if (!ticket) {
    fail(t('page.auth.oauth_callback_missing_ticket'), mode)
    return
  }

  // 票据只认本标签页发起的流程带回来的（发起记录见 auth store 的 OAuthPending）：别人发来的回调链接到这里即作废
  if (!pending || pending.mode !== mode || pending.provider !== (query.provider ?? '').toLowerCase()) {
    fail(t('page.auth.oauth_callback_not_initiated'), mode)
    return
  }

  if (mode === 'bind') {
    await confirmBind(ticket)
  }
  else {
    await completeLogin(ticket, pending.redirect)
  }
})

onBeforeUnmount(() => clearTimeout(timer))
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
          class="auth-helper"
          :class="isDark ? 'text-gray-500' : 'text-[hsl(var(--muted-foreground))]'"
        >
          {{ backToProfile ? t('page.auth.oauth_callback_redirect_profile') : t('page.auth.oauth_callback_redirect') }}
        </p>
      </div>
    </template>
  </div>
</template>
