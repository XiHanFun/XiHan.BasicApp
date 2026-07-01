<script lang="ts" setup>
import type { OAuthAuthorizeRequestDto, OAuthConsentPreviewDto } from '@/api'
import { NAvatar, NButton, NIcon, NSpin } from 'naive-ui'
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute } from 'vue-router'
import { oauthConsentApi } from '@/api'
import { useTheme } from '~/hooks'
import { Icon } from '~/iconify'
import { useUserStore } from '~/stores'

defineOptions({ name: 'OAuthAuthorizePage' })

const { t } = useI18n()
const { isDark } = useTheme()
const route = useRoute()
const userStore = useUserStore()

const loading = ref(true)
const submitting = ref(false)
const preview = ref<OAuthConsentPreviewDto | null>(null)
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
function buildRequest(): OAuthAuthorizeRequestDto {
  const query = route.query
  return {
    responseType: firstQuery(query.response_type),
    clientId: firstQuery(query.client_id),
    redirectUri: firstQuery(query.redirect_uri),
    scope: firstQuery(query.scope),
    state: firstQuery(query.state),
    codeChallenge: firstQuery(query.code_challenge),
    codeChallengeMethod: firstQuery(query.code_challenge_method),
  }
}

const request = buildRequest()

function resolveError(error?: string | null, description?: string | null): string {
  if (error && ERROR_TEXT[error]) {
    return t(ERROR_TEXT[error])
  }
  return description || t('page.oauth.err_generic')
}

/** 同意授权：创建授权码并整页跳回第三方 redirect_uri?code=...&state=... */
async function approve() {
  if (submitting.value) {
    return
  }
  submitting.value = true
  try {
    const result = await oauthConsentApi.authorize(request)
    if (result.success && result.redirectUri) {
      window.location.replace(result.redirectUri)
      return
    }
    errorMsg.value = resolveError(result.error, result.errorDescription)
    submitting.value = false
  }
  catch {
    errorMsg.value = t('page.oauth.authorize_failed')
    submitting.value = false
  }
}

/**
 * 拒绝授权：跳回第三方 redirect_uri?error=access_denied&state=...
 * 仅在预览通过（redirect_uri 已被后端确认为已注册回调）时可达，故跳转安全。
 */
function deny() {
  const redirectUri = request.redirectUri
  if (!redirectUri) {
    return
  }
  const separator = redirectUri.includes('?') ? '&' : '?'
  let url = `${redirectUri}${separator}error=access_denied`
  if (request.state) {
    url += `&state=${encodeURIComponent(request.state)}`
  }
  window.location.replace(url)
}

onMounted(async () => {
  try {
    const result = await oauthConsentApi.resolve(request)
    preview.value = result
    if (!result.valid) {
      errorMsg.value = resolveError(result.error, result.errorDescription)
      return
    }
    // 应用配置为跳过授权确认：静默授权后直接跳转
    if (result.skipConsent) {
      await approve()
    }
  }
  catch {
    errorMsg.value = t('page.oauth.load_failed')
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
        <NSpin :size="40" />
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
          <NIcon :size="32" class="text-red-500">
            <Icon icon="lucide:shield-alert" />
          </NIcon>
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
        <NAvatar
          v-if="preview.logo"
          :size="64"
          :src="preview.logo"
          round
          class="shadow-sm"
        />
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
              <NIcon :size="16" class="text-emerald-500 shrink-0">
                <Icon icon="lucide:check" />
              </NIcon>
              <span class="break-all">{{ scope }}</span>
            </li>
            <li
              v-if="scopes.length === 0"
              class="flex gap-2.5 items-center text-sm text-[hsl(var(--foreground))]"
            >
              <NIcon :size="16" class="text-emerald-500 shrink-0">
                <Icon icon="lucide:check" />
              </NIcon>
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
          <NButton
            size="large"
            :disabled="submitting"
            @click="deny"
          >
            {{ t('page.oauth.deny') }}
          </NButton>
          <NButton
            type="primary"
            size="large"
            :loading="submitting"
            @click="approve"
          >
            {{ submitting ? t('page.oauth.approving') : t('page.oauth.approve') }}
          </NButton>
        </div>

        <p class="mt-4 text-xs text-center text-[hsl(var(--muted-foreground))]">
          {{ t('page.oauth.redirect_notice') }}
        </p>
      </div>
    </div>
  </div>
</template>
