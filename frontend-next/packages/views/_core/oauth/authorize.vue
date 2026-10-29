<script lang="ts" setup>
import type { RequestOf, ResponseOf } from '~/contracts'
import { XhAvatarFallback, XhAvatarImage, XhAvatarRoot, XhButton, XhButtonIndicator, XhButtonLabel, XhSpinner } from '@xihan-ui/vue'

import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute } from 'vue-router'
import { oauthConsents } from '~/contracts'
import { useTheme } from '~/hooks'
import { Icon } from '~/iconify'
import { ApiError } from '~/request'
import { useUserStore } from '~/stores'

defineOptions({ name: 'OAuthAuthorizePage' })

type OAuthAuthorizeRequest = RequestOf<'oauth-consents.approve'>
type OAuthConsentPreview = ResponseOf<'oauth-consents.resolve'>

const { t } = useI18n()
const { isDark } = useTheme()
const route = useRoute()
const userStore = useUserStore()

const loading = ref(true)
const submitting = ref(false)
/** 拒绝在途：拒绝也要等后端给出跳转地址，期间两个按钮都不可点 */
const denying = ref(false)
const preview = ref<OAuthConsentPreview | null>(null)
const errorMsg = ref<string | null>(null)

const displayName = computed(() => userStore.nickname || userStore.username)
const appName = computed(() => preview.value?.appName || preview.value?.clientId || '')
const scopes = computed(() => preview.value?.scopes ?? [])
const appInitial = computed(() => (appName.value ? appName.value.charAt(0).toUpperCase() : '?'))

/** 授权错误码 → 友好文案 */
const ERROR_TEXT: Record<string, string> = {
  invalid_request: 'page.oauth.err_invalid_request',
  unauthorized_client: 'page.oauth.err_unauthorized_client',
  unsupported_response_type: 'page.oauth.err_unsupported_response_type',
  invalid_client: 'page.oauth.err_invalid_client',
  invalid_scope: 'page.oauth.err_invalid_scope',
  access_denied: 'page.oauth.err_access_denied',
}

function firstQuery(value: unknown): string | undefined {
  if (typeof value === 'string') {
    return value
  }
  if (Array.isArray(value) && typeof value[0] === 'string') {
    return value[0]
  }
  return undefined
}

/** 从查询串解析标准 OAuth 授权参数，原样上送后端（单一事实源在后端校验） */
function buildRequest(): OAuthAuthorizeRequest {
  const query = route.query
  return {
    responseType: firstQuery(query.response_type),
    clientId: firstQuery(query.client_id),
    redirectUri: firstQuery(query.redirect_uri),
    scope: firstQuery(query.scope),
    state: firstQuery(query.state),
    codeChallenge: firstQuery(query.code_challenge),
    codeChallengeMethod: firstQuery(query.code_challenge_method),
    nonce: firstQuery(query.nonce),
  }
}

const request = buildRequest()

function resolveError(error?: string | null, description?: string | null): string {
  if (error && ERROR_TEXT[error]) {
    return t(ERROR_TEXT[error])
  }
  return description || t('page.oauth.err_generic')
}

/** 服务端错误取本地化后的 detail（不带错误码前缀），其余用页面的兜底文案 */
function failureText(error: unknown, fallback: string): string {
  return error instanceof ApiError && error.detail ? error.detail : t(fallback)
}

/** 同意授权：后端创建授权码，整页跳回第三方 redirect_uri?code=...&state=... */
async function approve() {
  if (submitting.value || denying.value) {
    return
  }
  submitting.value = true
  try {
    const result = await oauthConsents.approve(request)
    window.location.assign(result.redirectUri)
  }
  catch (error) {
    errorMsg.value = failureText(error, 'page.oauth.authorize_failed')
    submitting.value = false
  }
}

/**
 * 拒绝授权：后端确认 client_id 与 redirect_uri 已注册后给出 redirect_uri?error=access_denied&state=...，整页跳回第三方。
 * 跳转地址只认后端返回的，不在前端拼。
 */
async function deny() {
  if (submitting.value || denying.value) {
    return
  }
  denying.value = true
  try {
    const result = await oauthConsents.deny(request)
    window.location.assign(result.redirectUri)
  }
  catch (error) {
    errorMsg.value = failureText(error, 'page.oauth.err_generic')
    denying.value = false
  }
}

onMounted(async () => {
  try {
    const result = await oauthConsents.resolve(request)
    preview.value = result
    if (!result.valid) {
      errorMsg.value = resolveError(result.error, result.errorDescription)
      return
    }
    // 应用配置为跳过授权确认、或已同意过这些范围：静默授权后直接跳转
    if (result.skipConsent) {
      await approve()
    }
  }
  catch (error) {
    errorMsg.value = failureText(error, 'page.oauth.load_failed')
  }
  finally {
    loading.value = false
  }
})
</script>

<template>
  <div
    class="flex justify-center items-center px-4 min-h-screen"
    :class="isDark ? 'bg-[hsl(var(--background))]' : 'bg-[hsl(var(--muted))]'"
  >
    <div
      class="p-8 w-full max-w-md rounded-2xl border shadow-lg"
      :class="isDark ? 'bg-[hsl(var(--card))] border-[hsl(var(--border))]' : 'bg-white border-gray-100'"
    >
      <!-- 加载态 -->
      <div v-if="loading" class="flex flex-col justify-center items-center py-12">
        <XhSpinner />
        <p class="mt-6 text-sm text-[hsl(var(--muted-foreground))]">
          {{ t('page.oauth.loading') }}
        </p>
      </div>

      <!-- 错误态 -->
      <div v-else-if="errorMsg" class="flex flex-col items-center py-8">
        <div
          class="flex justify-center items-center w-16 h-16 rounded-full"
          :class="isDark ? 'bg-red-500/10' : 'bg-red-50'"
        >
          <span class="text-red-500" style="display: inline-flex; font-size: 32px"><Icon icon="lucide:shield-alert" /></span>
        </div>
        <p class="mt-5 text-base font-medium text-[hsl(var(--foreground))]">
          {{ errorMsg }}
        </p>
        <p class="mt-2 text-sm text-center text-[hsl(var(--muted-foreground))]">
          {{ t('page.oauth.error_hint') }}
        </p>
      </div>

      <!-- 授权确认态 -->
      <div v-else-if="preview?.valid" class="flex flex-col items-center">
        <!-- src / alt 是根部件的属性：写在 image 部件上状态机拿不到地址，只会一直显示首字母 -->
        <XhAvatarRoot v-if="preview.logo" class="shadow-sm" :src="preview.logo" :alt="preview.appName ?? ''" :style="{ '--xh-avatar-size': '64px' }">
          <XhAvatarImage />
          <XhAvatarFallback>{{ appInitial }}</XhAvatarFallback>
        </XhAvatarRoot>
        <div
          v-else
          class="flex justify-center items-center w-16 h-16 text-2xl font-semibold text-white rounded-2xl bg-gradient-to-br from-indigo-500 to-purple-500"
        >
          {{ appInitial }}
        </div>

        <h1 class="mt-5 text-xl font-semibold text-center text-[hsl(var(--foreground))]">
          {{ appName }}
        </h1>
        <p class="mt-1.5 text-sm text-center text-[hsl(var(--muted-foreground))]">
          {{ t('page.oauth.requesting_access') }}
        </p>
        <p v-if="preview.appDescription" class="mt-2 text-xs text-center text-[hsl(var(--muted-foreground))]">
          {{ preview.appDescription }}
        </p>

        <!-- 权限范围 -->
        <div
          class="p-4 mt-6 w-full rounded-xl border"
          :class="isDark ? 'bg-[hsl(var(--muted))]/40 border-[hsl(var(--border))]' : 'bg-gray-50 border-gray-100'"
        >
          <p class="mb-3 text-xs font-medium text-[hsl(var(--muted-foreground))]">
            {{ t('page.oauth.scopes_title') }}
          </p>
          <ul class="space-y-2.5">
            <li
              v-for="scope in scopes"
              :key="scope"
              class="flex gap-2.5 items-center text-sm text-[hsl(var(--foreground))]"
            >
              <span class="text-emerald-500 shrink-0" style="display: inline-flex; font-size: 16px"><Icon icon="lucide:check" /></span>
              <span class="break-all">{{ scope }}</span>
            </li>
            <li
              v-if="scopes.length === 0"
              class="flex gap-2.5 items-center text-sm text-[hsl(var(--foreground))]"
            >
              <span class="text-emerald-500 shrink-0" style="display: inline-flex; font-size: 16px"><Icon icon="lucide:check" /></span>
              <span>{{ t('page.oauth.no_scopes') }}</span>
            </li>
          </ul>
        </div>

        <!-- 当前登录用户 -->
        <p class="mt-4 text-xs text-[hsl(var(--muted-foreground))]">
          {{ t('page.oauth.logged_in_as', { name: displayName }) }}
        </p>

        <!-- 操作按钮 -->
        <div class="grid grid-cols-2 gap-3 mt-6 w-full">
          <XhButton
            variant="subtle"
            size="lg"
            :disabled="submitting || denying"
            @click="deny"
          >
            {{ t('page.oauth.deny') }}
          </XhButton>
          <XhButton
            variant="subtle"
            tone="brand"
            size="lg"
            :loading="submitting"
            :disabled="denying"
            @click="approve"
          >
            <!-- 在途由钮中的加载环表达，标签保持不变，钮宽不跳 -->
            <XhButtonIndicator />
            <XhButtonLabel>{{ t('page.oauth.approve') }}</XhButtonLabel>
          </XhButton>
        </div>

        <p class="mt-4 text-xs text-center text-[hsl(var(--muted-foreground))]">
          {{ t('page.oauth.redirect_notice') }}
        </p>
      </div>
    </div>
  </div>
</template>
