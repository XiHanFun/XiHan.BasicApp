<script lang="ts" setup>
import type { FormRules } from '@xihan-ui/headless'
import type { CaptchaChallenge, LoginConfig, LoginResponse } from '~/types'

import { XhButton, XhCheckbox, XhFieldControl, XhFieldRoot, XhFormFieldGroup, XhFormRoot, XhFormSubmitTrigger, XhPinInputInput, XhPinInputRoot, XhPopoverContent, XhPopoverPositioner, XhPopoverRoot, XhPopoverTrigger, XhSeparator } from '@xihan-ui/vue'
import { computed, nextTick, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { XInput } from '~/components'
import { toast } from '~/composables'
import { useTheme } from '~/hooks'
import { Icon } from '~/iconify'
import { useAppContext, useAuthStore } from '~/stores'
import { useAuthFormInvalid } from './use-auth-form-invalid'

defineOptions({ name: 'LoginPage' })

const { isDark } = useTheme()
const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const { apis } = useAppContext()
const rememberMe = ref(true)
const loginConfig = ref<LoginConfig>({
  loginMethods: ['password'],
  oAuthProviders: [],
  captchaEnabled: false,
})

// ==================== 图形验证码 ====================

const captcha = ref<CaptchaChallenge | null>(null)
const captchaCode = ref('')
const captchaLoading = ref(false)

/** 拉取新验证码（页面加载、点击图片、验证码错误提示后调用） */
async function refreshCaptcha() {
  if (!loginConfig.value.captchaEnabled) {
    return
  }
  captchaLoading.value = true
  try {
    captcha.value = await apis.getCaptchaApi()
    captchaCode.value = ''
  }
  catch (error) {
    toast.error((error as Error)?.message || t('page.login.captcha_load_failed'))
  }
  finally {
    captchaLoading.value = false
  }
}

// ==================== 2FA 三阶段状态 ====================

/** 阶段：credentials | method-select | code-input */
const tfStage = ref<'code-input' | 'credentials' | 'method-select'>('credentials')
const availableMethods = ref<string[]>([])
const selectedMethod = ref('')
const twoFactorCode = ref<string[]>([])
const codeSent = ref(false)
const sendingCode = ref(false)

const methodLabels: Record<string, string> = {
  totp: '认证器（Authenticator）',
  email: '邮箱验证码',
  phone: '手机短信验证码',
}

const methodIcons: Record<string, string> = {
  totp: 'lucide:smartphone',
  email: 'lucide:mail',
  phone: 'lucide:phone',
}

const formData = ref({
  username: '',
  password: '',
})

const rules = computed<FormRules>(() => ({
  username: [
    { required: true, message: t('page.login.username_placeholder') },
  ],
  password: [
    { required: true, message: t('page.login.password_placeholder') },
  ],
}))

const redirect = computed(() => {
  return (route.query.redirect as string) || undefined
})

// 品牌图标用离线已预加载的图标集（offline.ts 预加载 lucide/tabler/mdi/simple-icons）。
// 企业微信、飞书在这四个集里都没有品牌 logo，先用语义相近的通用图标占位以便彼此区分；
// 要换成真 logo 需另行预载含该品牌的图标集，或把 SVG 注册成自定义图标集。
const oauthProviderIcons: Record<string, string> = {
  github: 'mdi:github',
  gitee: 'simple-icons:gitee',
  google: 'mdi:google',
  qq: 'mdi:qqchat',
  wechat: 'mdi:wechat',
  dingtalk: 'tabler:brand-dingtalk',
  wecom: 'mdi:briefcase-account',
  feishu: 'lucide:send',
}

const oauthProviders = computed(() => loginConfig.value.oAuthProviders ?? [])

/** 一行放得下的渠道数；多出来的收进「更多」浮层，免得换行把卡片撑高 */
const OAUTH_INLINE_COUNT = 3

const inlineOauthProviders = computed(() => oauthProviders.value.slice(0, OAUTH_INLINE_COUNT))
const moreOauthProviders = computed(() => oauthProviders.value.slice(OAUTH_INLINE_COUNT))
const showMoreOauth = ref(false)

function getOauthProviderIcon(name: string) {
  return oauthProviderIcons[name.toLowerCase()] ?? 'lucide:link-2'
}

function handleOAuthLogin(provider: typeof oauthProviders.value[number]) {
  showMoreOauth.value = false
  authStore.startOAuthLogin(provider)
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
  return {
    username: formData.value.username,
    password: formData.value.password,
    captchaId: loginConfig.value.captchaEnabled ? captcha.value?.captchaId : undefined,
    captchaCode: loginConfig.value.captchaEnabled ? captchaCode.value || undefined : undefined,
    twoFactorCode: tfStage.value === 'code-input' ? twoFactorCode.value.join('') : undefined,
    twoFactorMethod: selectedMethod.value || undefined,
    deviceId: cachedDeviceId.value || undefined,
  }
}

async function onSubmit() {
  try {
    if (tfStage.value === 'credentials') {
      // 图形验证码：提交前校验非空，避免白白消耗一次登录节流计数
      if (loginConfig.value.captchaEnabled && (!captcha.value || !captchaCode.value.trim())) {
        toast.warning(t('page.login.captcha_required'))
        return
      }
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
    else if (availableMethods.value.length > 1) {
      // 配置了多种双因素方式：全部列出，由用户任选一种进行验证
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
      toast.error(error.message)
    }
    // 验证码一次性消费：无论对错都已销毁，提示后立即换新码，避免反复撞已销毁的码
    if (error?.message && error.message.includes('验证码')) {
      void refreshCaptcha()
    }
  }
}

/** 用户选好方式后，发起带 twoFactorMethod 的登录请求 */
async function handleSelectMethod() {
  if (!selectedMethod.value) {
    toast.warning(t('page.auth.select_method_required'))
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
      toast.error(error.message)
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
      toast.success(t('page.auth.code_resent'))
    }
  }
  catch (err: unknown) {
    const error = err as { message?: string }
    if (error?.message)
      toast.error(error.message)
  }
  finally {
    sendingCode.value = false
  }
}

function handleOtpComplete(codes: string[]) {
  twoFactorCode.value = codes
  nextTick(() => onSubmit())
}

/** 返回双因素方式选择（多种方式时可换一种验证） */
function handleBackToMethodSelect() {
  tfStage.value = 'method-select'
  twoFactorCode.value = []
  codeSent.value = false
}

function handleKeydown(e: KeyboardEvent) {
  if (e.key === 'Enter')
    onSubmit()
}

function goTo(path: string) {
  router.push(path)
}

onMounted(async () => {
  try {
    await loadLoginConfig()
    if (loginConfig.value.captchaEnabled) {
      await refreshCaptcha()
    }
  }
  catch (error) {
    toast.error((error as Error)?.message || t('page.auth.load_config_failed'))
  }
})
const onAuthInvalid = useAuthFormInvalid()
</script>

<template>
  <div class="py-1">
    <Transition name="fade-slide" mode="out-in">
      <!-- 阶段2：输入验证码 -->
      <div v-if="tfStage === 'code-input'" key="code-input">
        <div class="mb-8">
          <div class="flex items-center gap-3 mb-3">
            <div
              class="flex justify-center items-center w-11 h-11 rounded-xl"
              :class="isDark ? 'bg-white/10' : 'bg-[hsl(var(--primary)/0.08)]'"
            >
              <span :class="isDark ? 'text-blue-400' : 'text-[hsl(var(--primary))]'" style="display: inline-flex; font-size: 22px"><Icon :icon="methodIcons[selectedMethod] || 'lucide:shield-check'" /></span>
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

        <div class="flex flex-col items-center py-4" @keydown.enter="onSubmit">
          <XhPinInputRoot
            v-model:value="twoFactorCode"
            :length="6"
            otp
            size="lg"
            @value-complete="(details: { value: string[] }) => handleOtpComplete(details.value)"
          >
            <!-- 格间距长在格子自己身上，这层包裹只负责排成一行 -->
            <div style="display: flex">
              <XhPinInputInput v-for="i in 6" :key="i" :index="i - 1" />
            </div>
          </XhPinInputRoot>
          <p
            class="mt-4 text-xs"
            :class="isDark ? 'text-gray-500' : 'text-[hsl(var(--muted-foreground))]'"
          >
            {{ selectedMethod === 'totp' ? t('page.auth.two_factor_hint') : '请输入 6 位验证码' }}
          </p>
        </div>

        <XhFormSubmitTrigger class="auth-submit !mt-4" :disabled="authStore.loginLoading">
          {{ t('page.auth.two_factor_verify') }}
        </XhFormSubmitTrigger>

        <div class="flex gap-2 mt-3">
          <XhButton
            v-if="selectedMethod !== 'totp'"
            class="!h-11 flex-1 !rounded-xl"
            variant="ghost"
            :loading="sendingCode"
            @click="handleResendCode"
          >
            重新发送
          </XhButton>
          <XhButton
            v-if="availableMethods.length > 1"
            class="!h-11 flex-1 !rounded-xl"
            variant="ghost"
            @click="handleBackToMethodSelect"
          >
            换种方式
          </XhButton>
        </div>
      </div>

      <!-- 阶段2：选择双因素验证方式（配置多种时全部列出，任选其一） -->
      <div v-else-if="tfStage === 'method-select'" key="method-select">
        <div class="mb-8">
          <div class="flex items-center gap-3 mb-3">
            <div
              class="flex justify-center items-center w-11 h-11 rounded-xl"
              :class="isDark ? 'bg-white/10' : 'bg-[hsl(var(--primary)/0.08)]'"
            >
              <span :class="isDark ? 'text-blue-400' : 'text-[hsl(var(--primary))]'" style="display: inline-flex; font-size: 22px"><Icon icon="lucide:shield-check" /></span>
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

        <div class="flex flex-col gap-3 mb-6">
          <button
            v-for="m in availableMethods"
            :key="m"
            type="button"
            class="flex items-center gap-3 px-4 w-full h-14 rounded-xl border transition-colors"
            :class="selectedMethod === m
              ? 'border-[hsl(var(--primary))] bg-[hsl(var(--primary)/0.08)]'
              : isDark ? 'border-white/10 hover:border-white/25' : 'border-[hsl(var(--border))] hover:border-[hsl(var(--primary)/0.4)]'"
            @click="selectedMethod = m"
          >
            <span
              :class="selectedMethod === m
                ? 'text-[hsl(var(--primary))]'
                : isDark ? 'text-gray-300' : 'text-[hsl(var(--muted-foreground))]'" style="display: inline-flex; font-size: 20px"
            ><Icon :icon="methodIcons[m] || 'lucide:shield-check'" /></span>
            <span class="text-[15px]">{{ methodLabels[m] || m }}</span>
          </button>
        </div>

        <XhButton
          variant="solid"
          tone="brand"
          full-width
          :loading="sendingCode"
          class="auth-submit"
          @click="handleSelectMethod"
        >
          继续
        </XhButton>
      </div>

      <!-- 阶段1：常规登录表单 -->
      <div v-else key="credentials">
        <div class="mb-8">
          <p
            class="mt-3 text-[15px] leading-7"
            :class="isDark ? 'text-gray-300' : 'text-[hsl(var(--muted-foreground))]'"
          >
            {{ t('page.auth.login_subtitle') }}
          </p>
        </div>

        <XhFormRoot
          v-model:values="formData"
          :rules="rules"
          validate-on="blur"
          @invalid="onAuthInvalid"
          @keydown="handleKeydown"
          @submit="onSubmit"
        >
          <XhFormFieldGroup value="username" class="!mb-6">
            <XhFieldRoot>
              <XhFieldControl>
                <XInput
                  v-model:value="formData.username"
                  size="lg"
                  :placeholder="t('page.login.username_placeholder')"
                  autocomplete="username"
                />
              </XhFieldControl>
            </XhFieldRoot>
          </XhFormFieldGroup>
          <XhFormFieldGroup value="password" class="!mb-6">
            <XhFieldRoot>
              <XhFieldControl>
                <XInput
                  v-model:value="formData.password"
                  type="password"
                  size="lg"
                  :placeholder="t('page.login.password_placeholder')"
                  autocomplete="current-password"
                />
              </XhFieldControl>
            </XhFieldRoot>
          </XhFormFieldGroup>
          <XhFormFieldGroup v-if="loginConfig.captchaEnabled" value="captchaCode" class="!mb-6">
            <XhFieldRoot>
              <!-- 布局层留在控件外面：唯一子节点若不是控件，会被组件库当成输入控件本体上妆 -->
              <div class="flex items-center gap-3">
                <XhFieldControl>
                  <XInput
                    v-model:value="captchaCode"
                    size="lg"
                    :max-length="4"
                    :placeholder="t('page.login.captcha_placeholder')"
                    autocomplete="off"
                  />
                </XhFieldControl>
                <div
                  class="flex justify-center items-center shrink-0 w-[120px] h-[40px] rounded-lg overflow-hidden"
                  :class="isDark ? 'bg-white/10' : 'bg-[hsl(var(--muted)/0.15)]'"
                  :title="t('page.login.captcha_refresh_title')"
                  @click="refreshCaptcha"
                >
                  <img
                    v-if="captcha?.image"
                    :src="captcha.image"
                    :alt="t('page.login.captcha_refresh_title')"
                    class="w-full h-full cursor-pointer select-none"
                    draggable="false"
                  >
                  <span v-else-if="captchaLoading" class="animate-spin" style="display: inline-flex; font-size: 18px"><Icon icon="lucide:loader-2" /></span>
                </div>
              </div>
            </XhFieldRoot>
          </XhFormFieldGroup>
          <div class="flex justify-between items-center mb-5 text-sm">
            <XhCheckbox v-model:checked="rememberMe" size="sm">
              {{ t('page.login.remember_me') }}
            </XhCheckbox>
            <span class="cursor-pointer link-primary" @click="goTo('/auth/forget-password')">
              {{ t('page.login.forgot_password') }}?
            </span>
          </div>

          <XhFormSubmitTrigger class="auth-submit" :disabled="authStore.loginLoading">
            {{ t('page.login.login_btn') }}
          </XhFormSubmitTrigger>
        </XhFormRoot>

        <p
          class="mt-6 text-sm text-center"
          :class="isDark ? 'text-gray-500' : 'text-[hsl(var(--muted-foreground))]'"
        >
          {{ t('page.auth.no_account') }}
          <span class="cursor-pointer link-primary" @click="goTo('/auth/register')">
            {{ t('page.login.register') }}
          </span>
        </p>

        <!-- 分隔线是纯线条、没有插槽，中缝那句文案要自己摆 -->
        <div v-if="oauthProviders.length > 0" class="flex gap-3 items-center my-6">
          <XhSeparator class="flex-1" :class="isDark ? '!border-white/10' : '!border-[hsl(var(--border))]'" />
          <span class="text-xs" :class="isDark ? 'text-gray-500' : 'text-[hsl(var(--muted-foreground))]'">
            {{ t('page.auth.third_party_login') }}
          </span>
          <XhSeparator class="flex-1" :class="isDark ? '!border-white/10' : '!border-[hsl(var(--border))]'" />
        </div>
        <div v-if="oauthProviders.length > 0" class="flex gap-3 justify-center items-center">
          <XhButton
            v-for="provider in inlineOauthProviders"
            :key="provider.name"
            variant="subtle"
            class="!h-10 !rounded-xl !px-4 !text-sm"
            @click="handleOAuthLogin(provider)"
          >
            <Icon :icon="getOauthProviderIcon(provider.name)" width="16" />
            {{ provider.displayName }}
          </XhButton>

          <!-- 触发器本身就是那颗按钮：浮层触发器渲染成 button，不能再往里套一颗 -->
          <XhPopoverRoot v-if="moreOauthProviders.length > 0" v-model:open="showMoreOauth" placement="top">
            <XhPopoverTrigger
              class="oauth-more-trigger !h-10 !w-10 !rounded-xl"
              :aria-label="t('page.auth.third_party_more')"
            >
              <Icon icon="lucide:ellipsis" width="16" />
            </XhPopoverTrigger>
            <XhPopoverPositioner>
              <XhPopoverContent :aria-label="t('page.auth.third_party_more')">
                <div class="oauth-more-grid">
                  <XhButton
                    v-for="provider in moreOauthProviders"
                    :key="provider.name"
                    variant="subtle"
                    class="!h-10 !rounded-xl !px-4 !text-sm !justify-start"
                    @click="handleOAuthLogin(provider)"
                  >
                    <Icon :icon="getOauthProviderIcon(provider.name)" width="16" />
                    {{ provider.displayName }}
                  </XhButton>
                </div>
              </XhPopoverContent>
            </XhPopoverPositioner>
          </XhPopoverRoot>
        </div>
      </div>
    </Transition>
  </div>
</template>

<style scoped>
/* 「更多渠道」触发器：浮层触发器自己就是 button，套不了 XhButton，只能照 subtle 变体补皮。
   尺寸走和旁边那几颗同一串工具类（!h-10 !w-10 !rounded-xl）——那是带 !important 的，
   本作用域样式压不过它，两处各写一份迟早对不齐，所以这里只管观感不管尺寸。
   边框留 1px 透明，与按钮同样的 border-box 盒模型 */
.oauth-more-trigger {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 0;
  border: var(--xh-stroke-thin) solid transparent;
  background: var(--xh-bg-subtle);
  color: var(--xh-fg-default);
  cursor: pointer;
  transition: background-color 0.15s ease;
}

.oauth-more-trigger:hover {
  background: var(--xh-bg-subtle-hover);
}

.oauth-more-trigger:active {
  background: var(--xh-bg-subtle-active);
}

/* 收进浮层的渠道排两列，条目左对齐便于扫读 */
.oauth-more-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(120px, 1fr));
  gap: 8px;
}

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
