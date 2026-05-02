<script lang="ts" setup>
import type { FormInst, FormRules } from 'naive-ui'
import type { LoginConfig, LoginResponse } from '~/types'
import {
  NButton,
  NCheckbox,
  NDivider,
  NForm,
  NFormItem,
  NIcon,
  NInput,
  NInputOtp,
  NRadioButton,
  NRadioGroup,
  useMessage,
} from 'naive-ui'
import { computed, nextTick, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { useTheme } from '~/hooks'
import { Icon } from '~/iconify'
import { useAppContext, useAuthStore } from '~/stores'

defineOptions({ name: 'LoginPage' })

const { isDark } = useTheme()
const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const { apis } = useAppContext()
const message = useMessage()
const formRef = ref<FormInst | null>(null)
const rememberMe = ref(true)
const showPassword = ref(false)
const loginConfig = ref<LoginConfig>({
  loginMethods: ['password'],
  tenantEnabled: true,
  oauthProviders: [],
})

// ==================== 2FA 三阶段状态 ====================

/** 阶段：credentials | method-select | code-input */
const tfStage = ref<'code-input' | 'credentials' | 'method-select'>('credentials')
const availableMethods = ref<string[]>([])
const selectedMethod = ref('')
const twoFactorCode = ref<string[]>([])
const codeSent = ref(false)
const sendingCode = ref(false)

const methodLabels: Record<string, string> = {
  totp: 'Authenticator App',
  email: '邮箱验证码',
  phone: '手机短信验证码',
}

const methodIcons: Record<string, string> = {
  totp: 'lucide:smartphone',
  email: 'lucide:mail',
  phone: 'lucide:phone',
}

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
      validator: (_rule: unknown, value: string) => {
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
  return (route.query.redirect as string) || undefined
})

const oauthProviderIcons: Record<string, string> = {
  github: 'lucide:github',
  google: 'lucide:globe',
  qq: 'lucide:message-circle',
}

const oauthProviders = computed(() => loginConfig.value.oauthProviders ?? [])

function getOauthProviderIcon(name: string) {
  return oauthProviderIcons[name.toLowerCase()] ?? 'lucide:link-2'
}

function handleOAuthLogin(provider: typeof oauthProviders.value[number]) {
  const parsedTenantId = Number(formData.value.tenantId)
  const tenantId = loginConfig.value.tenantEnabled && Number.isFinite(parsedTenantId) ? parsedTenantId : undefined
  authStore.startOAuthLogin(provider, tenantId)
}

async function loadLoginConfig() {
  loginConfig.value = await apis.getLoginConfigApi()
}

const cachedDeviceId = ref('')

onMounted(async () => {
  const { generateDeviceFingerprint } = await import('~/utils/device-fingerprint')
  cachedDeviceId.value = await generateDeviceFingerprint()
})

function buildLoginParams() {
  const parsedTenantId = Number(formData.value.tenantId)
  return {
    username: formData.value.username,
    password: formData.value.password,
    tenantId:
      loginConfig.value.tenantEnabled && Number.isFinite(parsedTenantId)
        ? parsedTenantId
        : undefined,
    twoFactorCode: tfStage.value === 'code-input' ? twoFactorCode.value.join('') : undefined,
    twoFactorMethod: selectedMethod.value || undefined,
    deviceId: cachedDeviceId.value || undefined,
  }
}

async function handleLogin() {
  try {
    if (tfStage.value === 'credentials') {
      await formRef.value?.validate()
    }

    const result: LoginResponse | null = await authStore.login(buildLoginParams(), redirect.value)

    if (!result) {
      return
    }

    // 服务端返回需要 2FA
    if (result.availableTwoFactorMethods?.length) {
      availableMethods.value = result.availableTwoFactorMethods
    }

    if (result.twoFactorMethod) {
      // 服务端已确认方式（可能已发送验证码）
      selectedMethod.value = result.twoFactorMethod
      codeSent.value = result.codeSent ?? false
      tfStage.value = 'code-input'
    }
    else if (availableMethods.value.length === 1) {
      // 仅一种方式可用，自动选中并进入下一步
      selectedMethod.value = availableMethods.value[0]!
      await handleSelectMethod()
    }
    else {
      // 多种方式可选
      tfStage.value = 'method-select'
      selectedMethod.value = availableMethods.value[0] ?? ''
    }
  }
  catch (err: unknown) {
    if (tfStage.value === 'code-input') {
      twoFactorCode.value = []
    }
    const error = err as { message?: string }
    if (error?.message) {
      message.error(error.message)
    }
  }
}

/** 用户选好方式后，发起带 twoFactorMethod 的登录请求 */
async function handleSelectMethod() {
  if (!selectedMethod.value) {
    message.warning('请选择验证方式')
    return
  }

  // TOTP 不需要发送验证码，直接进入输入界面
  if (selectedMethod.value === 'totp') {
    codeSent.value = false
    tfStage.value = 'code-input'
    return
  }

  // 邮箱/手机方式需要调用后端发送验证码
  sendingCode.value = true
  try {
    const result = await authStore.login(buildLoginParams(), redirect.value)
    if (result && result.twoFactorMethod) {
      codeSent.value = result.codeSent ?? false
      tfStage.value = 'code-input'
    }
  }
  catch (err: unknown) {
    const error = err as { message?: string }
    if (error?.message) {
      message.error(error.message)
    }
  }
  finally {
    sendingCode.value = false
  }
}

/** 重新发送验证码 */
async function handleResendCode() {
  sendingCode.value = true
  try {
    const result = await authStore.login(buildLoginParams(), redirect.value)
    if (result?.codeSent) {
      codeSent.value = true
      message.success('验证码已重新发送')
    }
  }
  catch (err: unknown) {
    const error = err as { message?: string }
    if (error?.message)
      message.error(error.message)
  }
  finally {
    sendingCode.value = false
  }
}

function handleOtpComplete(codes: string[]) {
  twoFactorCode.value = codes
  nextTick(() => handleLogin())
}

function handleBackToCredentials() {
  tfStage.value = 'credentials'
  twoFactorCode.value = []
  selectedMethod.value = ''
  availableMethods.value = []
  codeSent.value = false
}

function handleBackToMethodSelect() {
  tfStage.value = 'method-select'
  twoFactorCode.value = []
  codeSent.value = false
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
    <Transition name="fade-slide" mode="out-in">
      <!-- 阶段3：输入验证码 -->
      <div v-if="tfStage === 'code-input'" key="code-input">
        <div class="mb-8">
          <div class="flex items-center gap-3 mb-3">
            <div
              class="flex justify-center items-center w-11 h-11 rounded-xl"
              :class="isDark ? 'bg-white/10' : 'bg-[hsl(var(--primary)/0.08)]'"
            >
              <NIcon :size="22" :class="isDark ? 'text-blue-400' : 'text-[hsl(var(--primary))]'">
                <Icon :icon="methodIcons[selectedMethod] || 'lucide:shield-check'" />
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
            <template v-if="selectedMethod === 'totp'">
              {{ t('page.auth.two_factor_subtitle') }}
            </template>
            <template v-else-if="selectedMethod === 'email'">
              验证码已发送至您的邮箱，请查收
            </template>
            <template v-else-if="selectedMethod === 'phone'">
              验证码已发送至您的手机，请查收
            </template>
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
            {{ selectedMethod === 'totp' ? t('page.auth.two_factor_hint') : '请输入 6 位验证码' }}
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

        <div class="flex gap-2 mt-3">
          <NButton
            v-if="selectedMethod !== 'totp'"
            class="!h-11 flex-1 !rounded-xl"
            quaternary
            :loading="sendingCode"
            @click="handleResendCode"
          >
            重新发送
          </NButton>
          <NButton
            v-if="availableMethods.length > 1"
            class="!h-11 flex-1 !rounded-xl"
            quaternary
            @click="handleBackToMethodSelect"
          >
            换种方式
          </NButton>
        </div>

        <NButton
          class="mt-2 !h-11 w-full !rounded-xl"
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

      <!-- 阶段2：选择 2FA 方式 -->
      <div v-else-if="tfStage === 'method-select'" key="method-select">
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
              选择验证方式
            </h1>
          </div>
          <p
            class="text-[15px] leading-7"
            :class="isDark ? 'text-gray-300' : 'text-[hsl(var(--muted-foreground))]'"
          >
            您的账号已开启两步验证，请选择一种方式进行身份验证
          </p>
        </div>

        <NRadioGroup v-model:value="selectedMethod" class="!w-full !flex !flex-col !gap-3 !mb-6">
          <NRadioButton
            v-for="m in availableMethods"
            :key="m"
            :value="m"
            class="!rounded-xl !h-14 !flex !items-center !px-4 !w-full"
          >
            <div class="flex items-center gap-3">
              <NIcon :size="20">
                <Icon :icon="methodIcons[m] || 'lucide:shield-check'" />
              </NIcon>
              <span class="text-[15px]">{{ methodLabels[m] || m }}</span>
            </div>
          </NRadioButton>
        </NRadioGroup>

        <NButton
          type="primary"
          block
          :loading="sendingCode"
          class="!h-12 !rounded-xl !text-[15px] !font-semibold"
          @click="handleSelectMethod"
        >
          继续
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

      <!-- 阶段1：常规登录表单 -->
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

        <NDivider
          v-if="oauthProviders.length > 0"
          :class="isDark ? '!my-6 !border-white/10' : '!my-6 !border-[hsl(var(--border))]'"
        >
          {{ t('page.auth.third_party_login') }}
        </NDivider>
        <div v-if="oauthProviders.length > 0" class="flex flex-wrap gap-3 justify-center items-center">
          <NButton
            v-for="provider in oauthProviders"
            :key="provider.name"
            secondary
            class="!h-10 !rounded-xl !px-4 !text-sm"
            @click="handleOAuthLogin(provider)"
          >
            <template #icon>
              <Icon :icon="getOauthProviderIcon(provider.name)" width="16" />
            </template>
            {{ provider.displayName }}
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
