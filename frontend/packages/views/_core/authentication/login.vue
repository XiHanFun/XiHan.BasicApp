<script lang="ts" setup>
import type { FormInst, FormRules } from 'naive-ui'
import type { LoginConfig } from '~/types'
import { NButton, NCheckbox, NDivider, NForm, NFormItem, NIcon, NInput, NInputOtp, useMessage } from 'naive-ui'
import { computed, nextTick, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { getLoginConfigApi } from '@/api'
import { useTheme } from '~/hooks'
import { Icon } from '~/iconify'
import { useAuthStore } from '~/stores'

defineOptions({ name: 'LoginPage' })

const { isDark } = useTheme()
const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const message = useMessage()
const formRef = ref<FormInst | null>(null)
const rememberMe = ref(true)
const showPassword = ref(false)
const loginConfig = ref<LoginConfig>({
  loginMethods: ['password'],
  tenantEnabled: true,
  oauthProviders: [],
})

// 双因素认证状态
const requiresTwoFactor = ref(false)
const twoFactorCode = ref<string[]>([])

const formData = ref({
  username: 'superadmin',
  password: 'SuperAdmin@123',
  tenantId: '1',
})

const rules: FormRules = {
  username: [
    { required: true, message: () => t('page.login.username_placeholder'), trigger: 'blur' },
  ],
  password: [
    { required: true, message: () => t('page.login.password_placeholder'), trigger: 'blur' },
  ],
  tenantId: [
    {
      trigger: 'blur',
      validator: (_rule, value: string) => {
        if (!loginConfig.value.tenantEnabled)
          return true
        if (!value?.trim())
          return new Error('请输入租户ID')
        return true
      },
    },
  ],
}

const redirect = computed(() => {
  const r = route.query.redirect as string
  return r ? decodeURIComponent(r) : undefined
})

const defaultOauthProviders = ['github', 'google']
const oauthProviderMeta: Record<string, { icon: string, label: string }> = {
  github: { icon: 'mdi:github', label: 'GitHub' },
  google: { icon: 'logos:google-icon', label: 'Google' },
}

const oauthProviders = computed(() => {
  const providers = (loginConfig.value.oauthProviders || [])
    .map(provider => provider.trim().toLowerCase())
    .filter(Boolean)
  return providers.length > 0 ? providers : defaultOauthProviders
})

function getOauthProviderLabel(provider: string) {
  return oauthProviderMeta[provider]?.label ?? provider.toUpperCase()
}

function getOauthProviderIcon(provider: string) {
  return oauthProviderMeta[provider]?.icon ?? 'lucide:link-2'
}

async function loadLoginConfig() {
  loginConfig.value = await getLoginConfigApi()
}

function buildLoginParams() {
  const parsedTenantId = Number(formData.value.tenantId)
  return {
    username: formData.value.username,
    password: formData.value.password,
    tenantId:
      loginConfig.value.tenantEnabled && Number.isFinite(parsedTenantId)
        ? parsedTenantId
        : undefined,
    twoFactorCode: requiresTwoFactor.value ? twoFactorCode.value.join('') : undefined,
  }
}

async function handleLogin() {
  try {
    if (!requiresTwoFactor.value) {
      await formRef.value?.validate()
    }
    const needsTwoFactor = await authStore.login(buildLoginParams(), redirect.value)
    if (needsTwoFactor) {
      requiresTwoFactor.value = true
    }
  }
  catch (err: unknown) {
    // 2FA 验证码错误(400)等业务异常 → 清空重填
    if (requiresTwoFactor.value) {
      twoFactorCode.value = []
    }
    const error = err as { message?: string }
    if (error?.message) {
      message.error(error.message)
    }
  }
}

/** OTP 填满 6 位后自动提交 */
function handleOtpComplete(codes: string[]) {
  twoFactorCode.value = codes
  nextTick(() => handleLogin())
}

function handleBackToCredentials() {
  requiresTwoFactor.value = false
  twoFactorCode.value = []
}

function handleKeydown(e: KeyboardEvent) {
  if (e.key === 'Enter')
    handleLogin()
}

function goTo(path: string) {
  router.push(path)
}

onMounted(async () => {
  try {
    await loadLoginConfig()
  }
  catch {
    message.error('加载登录配置失败')
  }
})
</script>

<template>
  <div class="py-1">
    <!-- 双因素认证 OTP 输入界面 -->
    <Transition name="fade-slide" mode="out-in">
      <div v-if="requiresTwoFactor" key="two-factor">
        <div class="mb-8">
          <div class="flex items-center gap-3 mb-3">
            <div
              class="flex justify-center items-center w-11 h-11 rounded-xl"
              :class="isDark ? 'bg-white/10' : 'bg-[hsl(var(--primary)/0.08)]'"
            >
              <NIcon :size="22" :class="isDark ? 'text-blue-400' : 'text-[hsl(var(--primary))]'">
                <Icon icon="lucide:shield-check" />
              </NIcon>
            </div>
            <h1 class="text-[28px] font-semibold leading-tight sm:text-[32px]">
              {{ t('page.auth.two_factor_title') }}
            </h1>
          </div>
          <p
            class="text-[15px] leading-7"
            :class="isDark ? 'text-gray-300' : 'text-[hsl(var(--muted-foreground))]'"
          >
            {{ t('page.auth.two_factor_subtitle') }}
          </p>
        </div>

        <div class="flex flex-col items-center py-4" @keydown.enter="handleLogin">
          <NInputOtp
            v-model:value="twoFactorCode"
            :length="6"
            size="large"
            autofocus
            @complete="handleOtpComplete"
          />
          <p
            class="mt-4 text-xs"
            :class="isDark ? 'text-gray-500' : 'text-[hsl(var(--muted-foreground))]'"
          >
            {{ t('page.auth.two_factor_hint') }}
          </p>
        </div>

        <NButton
          type="primary"
          block
          :loading="authStore.loginLoading"
          :disabled="twoFactorCode.filter(Boolean).length < 6"
          class="!mt-4 !h-12 !rounded-xl !text-[15px] !font-semibold"
          @click="handleLogin"
        >
          {{ t('page.auth.two_factor_verify') }}
        </NButton>

        <NButton
          class="mt-3 !h-11 w-full !rounded-xl"
          quaternary
          @click="handleBackToCredentials"
        >
          <template #icon>
            <NIcon :size="16">
              <Icon icon="lucide:arrow-left" />
            </NIcon>
          </template>
          {{ t('page.auth.back_to_login') }}
        </NButton>
      </div>

      <!-- 常规登录表单 -->
      <div v-else key="credentials">
        <div class="mb-8">
          <h1 class="text-[32px] font-semibold leading-tight sm:text-[36px]">
            {{ t('page.auth.welcome_back') }}
          </h1>
          <p
            class="mt-3 text-[15px] leading-7"
            :class="isDark ? 'text-gray-300' : 'text-[hsl(var(--muted-foreground))]'"
          >
            {{ t('page.auth.login_subtitle') }}
          </p>
        </div>

        <NForm
          ref="formRef"
          :model="formData"
          :rules="rules"
          label-placement="top"
          size="large"
          :show-label="false"
          @keydown="handleKeydown"
        >
          <NFormItem path="username" :show-feedback="false" class="!mb-6">
            <NInput
              v-model:value="formData.username"
              size="large"
              :placeholder="t('page.login.username_placeholder')"
              :input-props="{ autocomplete: 'username' }"
            />
          </NFormItem>
          <NFormItem path="password" :show-feedback="false" class="!mb-6">
            <NInput
              v-model:value="formData.password"
              :type="showPassword ? 'text' : 'password'"
              size="large"
              :placeholder="t('page.login.password_placeholder')"
              :input-props="{ autocomplete: 'current-password' }"
            >
              <template #suffix>
                <NIcon
                  class="cursor-pointer"
                  :class="isDark ? 'text-gray-400' : 'text-[hsl(var(--muted-foreground))]'"
                  @click="showPassword = !showPassword"
                >
                  <Icon :icon="showPassword ? 'lucide:eye-off' : 'lucide:eye'" width="16" />
                </NIcon>
              </template>
            </NInput>
          </NFormItem>
          <NFormItem
            v-if="loginConfig.tenantEnabled"
            path="tenantId"
            :show-feedback="false"
            class="!mb-6"
          >
            <NInput v-model:value="formData.tenantId" size="large" placeholder="请输入租户ID" />
          </NFormItem>
          <div class="flex justify-between items-center mb-5 text-sm">
            <NCheckbox v-model:checked="rememberMe">
              {{ t('page.login.remember_me') }}
            </NCheckbox>
            <span class="cursor-pointer link-primary" @click="goTo('/auth/forget-password')">
              {{ t('page.login.forgot_password') }}?
            </span>
          </div>

          <NButton
            type="primary"
            block
            :loading="authStore.loginLoading"
            class="!h-12 !rounded-xl !text-[15px] !font-semibold"
            @click="handleLogin"
          >
            {{ t('page.login.login_btn') }}
          </NButton>
        </NForm>

        <NDivider :class="isDark ? '!my-6 !border-white/10' : '!my-6 !border-[hsl(var(--border))]'">
          {{ t('page.auth.third_party_login') }}
        </NDivider>
        <div class="flex flex-wrap gap-3 justify-center items-center">
          <NButton
            v-for="provider in oauthProviders"
            :key="provider"
            secondary
            class="!h-10 !rounded-xl !px-4 !text-sm"
          >
            <template #icon>
              <Icon :icon="getOauthProviderIcon(provider)" width="16" />
            </template>
            {{ getOauthProviderLabel(provider) }}
          </NButton>
        </div>

        <p
          class="mt-6 text-sm text-center"
          :class="isDark ? 'text-gray-500' : 'text-[hsl(var(--muted-foreground))]'"
        >
          {{ t('page.auth.no_account') }}
          <span class="cursor-pointer link-primary" @click="goTo('/auth/register')">
            {{ t('page.login.register') }}
          </span>
        </p>
      </div>
    </Transition>
  </div>
</template>

<style scoped>
.link-primary {
  color: hsl(var(--primary));
}

.link-primary:hover {
  text-decoration: underline;
}

.fade-slide-enter-active,
.fade-slide-leave-active {
  transition: all 0.3s ease;
}

.fade-slide-enter-from {
  opacity: 0;
  transform: translateX(24px);
}

.fade-slide-leave-to {
  opacity: 0;
  transform: translateX(-24px);
}
</style>
