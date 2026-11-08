<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script lang="ts" setup>
import type { FormRules } from '@xihan-ui/headless'
import type { LegalDocumentKind } from './legal'
import type { CaptchaChallenge } from '~/types'
import { XhCheckbox, XhFieldControl, XhFieldLabel, XhFieldRoot, XhFormFieldGroup, XhFormRoot, XhFormSubmitTrigger, XhPinInputInput, XhPinInputRoot } from '@xihan-ui/vue'
import { computed, onMounted, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { XInput } from '~/components'
import { toast } from '~/composables'
import { LOGIN_PATH } from '~/constants'
import { useTheme } from '~/hooks'
import { Icon } from '~/iconify'
import { useAppContext } from '~/stores'
import { CAPTCHA_CODE_LENGTH } from '../shared/pin-code'
import LegalDocumentDialog from './LegalDocumentDialog.vue'
import { useAuthFormInvalid } from './use-auth-form-invalid'

defineOptions({ name: 'RegisterPage' })

const { isDark } = useTheme()
const { t } = useI18n()
const router = useRouter()
const { apis } = useAppContext()

const formData = ref({
  username: '',
  email: '',
  password: '',
  confirmPassword: '',
})

const agreePolicy = ref(false)
/** 同意条款的文案里夹着两颗文书按钮，放不进复选框的标签插槽，复选框经 aria-labelledby 取它作名字 */
const agreeLabelId = useId()

/** 正在查看的文书；弹窗关掉后保留上一份，关闭动画里标题不会跳成另一份 */
const legalKind = ref<LegalDocumentKind>('privacy-policy')
const legalOpen = ref(false)

function openLegal(kind: LegalDocumentKind) {
  legalKind.value = kind
  legalOpen.value = true
}

// ==================== 图形验证码（登录配置要求时与密码登录同一套） ====================

const captchaEnabled = ref(false)
const captcha = ref<CaptchaChallenge | null>(null)
const captchaCode = ref<string[]>([])
const captchaCodeStr = computed(() => captchaCode.value.join(''))
const captchaLoading = ref(false)

async function refreshCaptcha() {
  if (!captchaEnabled.value)
    return
  captchaLoading.value = true
  try {
    captcha.value = await apis.getCaptchaApi()
    captchaCode.value = []
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('page.login.captcha_load_failed'))
  }
  finally {
    captchaLoading.value = false
  }
}

onMounted(async () => {
  try {
    captchaEnabled.value = (await apis.getLoginConfigApi()).captchaEnabled
    await refreshCaptcha()
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('page.auth.load_config_failed'))
  }
})

// ==================== 密码强度（四档） ====================

const passwordStrength = computed(() => {
  const pwd = formData.value.password
  if (!pwd)
    return 0
  let score = 0
  if (pwd.length >= 8)
    score++
  if (/[a-z]/.test(pwd) && /[A-Z]/.test(pwd))
    score++
  if (/\d/.test(pwd))
    score++
  if (/[^a-z0-9]/i.test(pwd))
    score++
  return score
})

const strengthLabel = computed(() => {
  const labels = [
    t('page.auth.strength_weak'),
    t('page.auth.strength_weak'),
    t('page.auth.strength_medium'),
    t('page.auth.strength_strong'),
    t('page.auth.strength_very_strong'),
  ]
  return labels[passwordStrength.value] || ''
})

/** 强度色走组件库语气前景：弱 danger、中 warning、强 success、很强 info，深浅两态与状态色设置都跟着走 */
const strengthColor = computed(() => {
  const colors = [
    'var(--xh-fg-danger)',
    'var(--xh-fg-danger)',
    'var(--xh-fg-warning)',
    'var(--xh-fg-success)',
    'var(--xh-fg-info)',
  ]
  return colors[passwordStrength.value] || 'var(--xh-fg-danger)'
})

/** 用户名与后端一致：小写字母开头，3 到 64 位，只含小写字母、数字、.、_、- */
const USERNAME_RE = /^[a-z][a-z0-9._-]{2,63}$/

const rules = computed<FormRules<typeof formData.value>>(() => ({
  username: [
    {
      required: true,
      message: t('page.auth.username_placeholder'),
    },
    {
      validator: value => (!value || USERNAME_RE.test(String(value))
        ? null
        : t('page.auth.username_rule')),
    },
  ],
  email: [
    {
      required: true,
      message: t('page.auth.email_placeholder'),
    },
    {
      type: 'email',
      message: t('page.auth.email_invalid'),
    },
  ],
  password: [
    {
      required: true,
      message: t('page.login.password_placeholder'),
    },
    {
      // 与后端密码策略一致：至少 8 位，同时含字母与数字；空值交给上面那条 required
      validator: (value) => {
        const password = String(value ?? '')
        if (!password)
          return null
        if (password.length < 8)
          return t('page.auth.password_rule_length')
        if (!/[a-z]/i.test(password) || !/\d/.test(password))
          return t('page.auth.password_rule_letter_digit')
        return null
      },
    },
  ],
  confirmPassword: [
    {
      required: true,
      message: t('page.auth.confirm_password_placeholder'),
    },
    {
      // 第二参是整表值，跨字段规则从它读；先填它、再改密码时由 deps 带着重验
      deps: ['password'],
      validator: (value, values) => (value === values.password
        ? null
        : t('page.auth.password_mismatch')),
    },
  ],
}))

// 返回的 Promise 交给表单：落定前提交钮报在途、再按不重复提交，失败在这里自己接住
async function onSubmit() {
  if (!agreePolicy.value) {
    toast.warning(t('page.auth.agree_required'))
    return
  }
  if (captchaEnabled.value && (!captcha.value || captchaCodeStr.value.length < CAPTCHA_CODE_LENGTH)) {
    toast.warning(t('page.login.captcha_required'))
    return
  }
  try {
    await apis.registerApi({
      username: formData.value.username,
      email: formData.value.email,
      password: formData.value.password,
      captchaId: captchaEnabled.value ? captcha.value?.captchaId : undefined,
      captchaCode: captchaEnabled.value ? captchaCodeStr.value : undefined,
    })
    toast.success(t('page.auth.register_success'))
    router.push(LOGIN_PATH)
  }
  catch (err: unknown) {
    // 图形验证码一次一用：失败后换一张
    void refreshCaptcha()
    const error = err as { message?: string }
    if (error?.message)
      toast.danger(error.message)
  }
}

const onAuthInvalid = useAuthFormInvalid()
</script>

<template>
  <div class="py-1">
    <div class="mb-8">
      <h1 class="text-[32px] font-semibold leading-tight sm:text-[36px]">
        {{ t('page.auth.create_account_title') }}
      </h1>
      <p
        class="mt-3 auth-body"
        :class="isDark ? 'text-gray-300' : 'text-[hsl(var(--muted-foreground))]'"
      >
        {{ t('page.auth.register_subtitle') }}
      </p>
    </div>

    <XhFormRoot
      v-model:values="formData"
      :rules="rules"
      validate-on="blur"
      @invalid="onAuthInvalid"
      @submit="onSubmit"
    >
      <!-- 各字段靠占位文案表意，标签只留给读屏 -->
      <XhFormFieldGroup name="username" class="!mb-6">
        <XhFieldRoot>
          <XhFieldLabel class="sr-only">
            {{ t('page.auth.username_placeholder') }}
          </XhFieldLabel>
          <XhFieldControl>
            <XInput
              v-model:value="formData.username"
              size="lg"
              :placeholder="t('page.auth.username_placeholder')"
              autocomplete="username"
            >
              <template #prefix>
                <Icon icon="lucide:user" />
              </template>
            </XInput>
          </XhFieldControl>
        </XhFieldRoot>
      </XhFormFieldGroup>

      <XhFormFieldGroup name="email" class="!mb-6">
        <XhFieldRoot>
          <XhFieldLabel class="sr-only">
            {{ t('page.auth.email_placeholder') }}
          </XhFieldLabel>
          <XhFieldControl>
            <XInput
              v-model:value="formData.email"
              size="lg"
              :placeholder="`${t('page.auth.email_placeholder')}（${t('page.auth.register_email_tip')}）`"
              autocomplete="email"
            >
              <template #prefix>
                <Icon icon="lucide:mail" />
              </template>
            </XInput>
          </XhFieldControl>
        </XhFieldRoot>
      </XhFormFieldGroup>

      <!-- 密码档自带显隐钮，不再另摆眼睛图标 -->
      <XhFormFieldGroup name="password" class="!mb-6">
        <XhFieldRoot>
          <XhFieldLabel class="sr-only">
            {{ t('page.login.password_placeholder') }}
          </XhFieldLabel>
          <XhFieldControl>
            <XInput
              v-model:value="formData.password"
              type="password"
              size="lg"
              :placeholder="t('page.login.password_placeholder')"
              autocomplete="new-password"
            >
              <template #prefix>
                <Icon icon="lucide:lock-keyhole" />
              </template>
            </XInput>
          </XhFieldControl>
        </XhFieldRoot>
      </XhFormFieldGroup>

      <XhFormFieldGroup name="confirmPassword" :class="formData.password ? '!mb-3' : '!mb-6'">
        <XhFieldRoot>
          <XhFieldLabel class="sr-only">
            {{ t('page.auth.confirm_password_placeholder') }}
          </XhFieldLabel>
          <XhFieldControl>
            <XInput
              v-model:value="formData.confirmPassword"
              type="password"
              size="lg"
              :placeholder="t('page.auth.confirm_password_placeholder')"
              autocomplete="new-password"
            >
              <template #prefix>
                <Icon icon="lucide:lock-keyhole" />
              </template>
            </XInput>
          </XhFieldControl>
        </XhFieldRoot>
      </XhFormFieldGroup>

      <!-- 密码强度放在两个密码框之后：输密码与确认密码紧挨着，中间不插一条强度条 -->
      <div v-if="formData.password" class="flex gap-2 items-center mb-6">
        <div class="flex flex-1 gap-1">
          <div
            v-for="i in 4"
            :key="i"
            class="flex-1 h-1 rounded-full transition-colors"
            :style="{ backgroundColor: i <= passwordStrength ? strengthColor : 'var(--xh-border-default)' }"
          />
        </div>
        <span class="auth-caption" :style="{ color: strengthColor }">{{ strengthLabel }}</span>
      </div>

      <!-- 图形验证码：与密码登录同一套排布，格子在左、验证码图靠右缘，点图换一张 -->
      <div v-if="captchaEnabled" class="mb-6">
        <span class="sr-only">{{ t('page.login.captcha_required') }}</span>
        <div class="auth-code-row">
          <XhPinInputRoot v-model:value="captchaCode" :length="CAPTCHA_CODE_LENGTH" type="numeric" :aria-label="t('page.login.captcha_required')">
            <div style="display: flex">
              <XhPinInputInput v-for="i in CAPTCHA_CODE_LENGTH" :key="i" :index="i - 1" />
            </div>
          </XhPinInputRoot>
          <div
            class="auth-captcha"
            :class="isDark ? 'bg-white/10' : 'bg-[hsl(var(--muted)/0.15)]'"
            :title="t('page.login.captcha_refresh_title')"
            @click="refreshCaptcha"
          >
            <img
              v-if="captcha?.image"
              :src="captcha.image"
              :alt="t('page.login.captcha_refresh_title')"
              class="select-none"
              draggable="false"
            >
            <span v-else-if="captchaLoading" class="animate-spin" style="display: inline-flex; font-size: var(--xh-glyph-size-md)"><Icon icon="lucide:loader-2" /></span>
          </div>
        </div>
      </div>

      <!-- 文案里夹着按钮，不能塞进复选框的标签插槽（label 里不能套可聚焦元素）：并排放一段，复选框经 aria-labelledby 指向它 -->
      <div class="agree-row auth-body mb-6">
        <span class="agree-row__box">
          <XhCheckbox v-model:checked="agreePolicy" :aria-labelledby="agreeLabelId" />
        </span>
        <span :id="agreeLabelId">
          {{ t('page.auth.agree_text') }}
          <!-- 打开弹窗而不是跳页：哈希路由下 href="#" 会把人带回首页 -->
          <button type="button" class="link-primary legal-link" @click="openLegal('privacy-policy')">
            {{ t('page.auth.privacy_policy') }}
          </button>
          {{ t('page.auth.and') }}
          <button type="button" class="link-primary legal-link" @click="openLegal('terms-of-service')">
            {{ t('page.auth.terms_of_service') }}
          </button>
        </span>
      </div>

      <XhFormSubmitTrigger class="auth-submit">
        {{ t('page.auth.register_btn') }}
      </XhFormSubmitTrigger>
    </XhFormRoot>

    <LegalDocumentDialog v-model:open="legalOpen" :kind="legalKind" />

    <p
      class="mt-6 auth-helper text-center"
      :class="isDark ? 'text-gray-400' : 'text-[hsl(var(--muted-foreground))]'"
    >
      {{ t('page.auth.already_have_account') }}
      <span class="cursor-pointer link-primary" @click="router.push(LOGIN_PATH)">
        {{ t('page.auth.back_to_login') }}
      </span>
    </p>
  </div>
</template>

<style scoped>
.link-primary {
  color: var(--xh-fg-brand);
}

.link-primary:hover {
  text-decoration: underline;
}

.agree-row {
  display: flex;
  align-items: flex-start;
  gap: var(--xh-space-2);
}

/* 方框对齐首行：盒高取一行文字的行高，文案折行时方框不落到几行中间 */
.agree-row__box {
  display: flex;
  flex: none;
  align-items: center;
  block-size: 1lh;
}

/* 文书入口是按钮（打开弹窗），外观仍是行内链接 */
.legal-link {
  padding: 0;
  border: 0;
  background: none;
  font: inherit;
  cursor: pointer;
}

.legal-link:focus-visible {
  outline: var(--xh-ring-width) solid var(--xh-ring-focus);
  outline-offset: var(--xh-ring-offset);
}
</style>
