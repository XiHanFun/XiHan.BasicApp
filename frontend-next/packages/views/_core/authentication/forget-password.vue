<script lang="ts" setup>
import type { FormRules } from '@xihan-ui/headless'
import {
  XhButton,
  XhFieldControl,
  XhFieldLabel,
  XhFieldRoot,
  XhFormFieldGroup,
  XhFormRoot,
  XhFormSubmitTrigger,
  XhPinInputInput,
  XhPinInputRoot,
} from '@xihan-ui/vue'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { XInput } from '~/components'
import { toast } from '~/composables'
import { LOGIN_PATH } from '~/constants'
import { useTheme } from '~/hooks'
import { useAppContext } from '~/stores'
import CodeCountdown from '../shared/CodeCountdown.vue'
import { OTP_CODE_LENGTH } from '../shared/pin-code'
import { useAuthFormInvalid } from './use-auth-form-invalid'

/**
 * 找回密码：4.x 只按邮箱发一次性重置链接，6.0 发验证码（auth.request-password-reset），
 * 同一页填码与新密码（auth.reset-password），设好后这个账号的全部会话失效，回登录页用新密码登录。
 */
defineOptions({ name: 'ForgetPasswordPage' })

const { isDark } = useTheme()
const { t } = useI18n()
const router = useRouter()
const { apis } = useAppContext()
/** 重发倒计时这一轮的时长，大于 0 即正在倒计时 */
const resendSeconds = ref(0)

const formData = ref({
  tenant: '',
  email: '',
  code: '',
  newPassword: '',
  confirmPassword: '',
})

/** 验证码的逐格值；格子每次改动把串写回表单 */
const codeCells = ref<string[]>([])

const rules = computed<FormRules<typeof formData.value>>(() => ({
  email: [
    { required: true, message: t('page.auth.email_placeholder') },
    { type: 'email', message: t('page.auth.email_invalid') },
  ],
  code: [
    { required: true, message: t('page.auth.code_required') },
    { min: OTP_CODE_LENGTH, max: OTP_CODE_LENGTH, message: t('page.auth.code_length_tip') },
  ],
  newPassword: [
    { required: true, message: t('page.auth.reset_new_password_placeholder') },
    {
      // 与后端密码策略一致：至少 8 位，同时含字母与数字（不合格后端也不消费验证码）
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
    { required: true, message: t('page.auth.confirm_password_placeholder') },
    {
      deps: ['newPassword'],
      validator: (value, values) => (value === values.newPassword ? null : t('page.auth.password_mismatch')),
    },
  ],
}))

const EMAIL_RE = /^[^\s@]+@[^\s@][^\s.@]*\.[^\s@]+$/

/** 发码只关邮箱这一个字段：就地判一次格式，提交那一路仍走表单自己的整表校验 */
function handleSendCode() {
  if (!EMAIL_RE.test(formData.value.email)) {
    toast.warning(t('page.auth.email_invalid'))
    return
  }
  void (async () => {
    try {
      await apis.requestPasswordResetApi({ email: formData.value.email, tenant: formData.value.tenant || undefined })
      resendSeconds.value = 60
      toast.success(t('page.auth.reset_code_sent'))
    }
    catch (err: unknown) {
      const error = err as { message?: string }
      toast.danger(error?.message || t('page.auth.code_send_failed'))
    }
  })()
}

// 返回的 Promise 交给表单：落定前提交钮报在途、再按不重复提交，失败在这里自己接住
async function onSubmit() {
  try {
    await apis.resetPasswordApi({
      tenant: formData.value.tenant || undefined,
      email: formData.value.email,
      code: formData.value.code,
      newPassword: formData.value.newPassword,
    })
    toast.success(t('page.auth.reset_success'))
    router.push(LOGIN_PATH)
  }
  catch (err: unknown) {
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
        {{ t('page.auth.forget_password_title') }}
      </h1>
      <p
        class="mt-3 auth-body"
        :class="isDark ? 'text-gray-300' : 'text-[hsl(var(--muted-foreground))]'"
      >
        {{ t('page.auth.forget_password_subtitle') }}
      </p>
    </div>

    <XhFormRoot
      v-model:values="formData"
      :rules="rules"
      validate-on="blur"
      @invalid="onAuthInvalid"
      @submit="onSubmit"
    >
      <!-- 6.0 的账号属于注册地租户：租户账号填租户 ID，平台账号留空（同验证码登录） -->
      <XhFormFieldGroup name="tenant" class="!mb-6">
        <XhFieldRoot>
          <XhFieldLabel class="sr-only">
            {{ t('page.login.tenant') }}
          </XhFieldLabel>
          <XhFieldControl>
            <XInput
              v-model:value="formData.tenant"
              size="lg"
              :placeholder="t('page.login.tenant_placeholder')"
              autocomplete="organization"
            />
          </XhFieldControl>
        </XhFieldRoot>
      </XhFormFieldGroup>

      <XhFormFieldGroup name="email" class="!mb-6">
        <XhFieldRoot>
          <!-- 占位只是示例地址，名字给读屏 -->
          <XhFieldLabel class="sr-only">
            {{ t('page.auth.email_placeholder') }}
          </XhFieldLabel>
          <XhFieldControl>
            <XInput
              v-model:value="formData.email"
              size="lg"
              placeholder="example@example.com"
              autocomplete="email"
            />
          </XhFieldControl>
        </XhFieldRoot>
      </XhFormFieldGroup>

      <XhFormFieldGroup v-slot="{ setValue }" name="code" class="!mb-6">
        <XhFieldRoot>
          <XhFieldLabel class="sr-only">
            {{ t('page.auth.code_required') }}
          </XhFieldLabel>
          <!-- 六格与发码钮同一行，放不下时钮换到下一行靠右（同验证码登录） -->
          <div class="auth-code-row">
            <XhFieldControl>
              <XhPinInputRoot
                v-model:value="codeCells"
                :length="OTP_CODE_LENGTH"
                type="numeric"
                otp
                @value-change="setValue($event.valueAsString)"
              >
                <div style="display: flex">
                  <XhPinInputInput v-for="i in OTP_CODE_LENGTH" :key="i" :index="i - 1" />
                </div>
              </XhPinInputRoot>
            </XhFieldControl>
            <XhButton
              tone="brand"
              variant="outline"
              :disabled="resendSeconds > 0"
              size="lg"
              style="min-width: 132px"
              @click="handleSendCode"
            >
              <CodeCountdown v-if="resendSeconds > 0" :seconds="resendSeconds" @finish="resendSeconds = 0" />
              <template v-else>
                {{ t('page.auth.send_code') }}
              </template>
            </XhButton>
          </div>
        </XhFieldRoot>
      </XhFormFieldGroup>

      <XhFormFieldGroup name="newPassword" class="!mb-6">
        <XhFieldRoot>
          <XhFieldLabel class="sr-only">
            {{ t('page.auth.reset_new_password_placeholder') }}
          </XhFieldLabel>
          <XhFieldControl>
            <XInput
              v-model:value="formData.newPassword"
              type="password"
              size="lg"
              :placeholder="t('page.auth.reset_new_password_placeholder')"
              autocomplete="new-password"
            />
          </XhFieldControl>
        </XhFieldRoot>
      </XhFormFieldGroup>

      <XhFormFieldGroup name="confirmPassword" class="!mb-6">
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
            />
          </XhFieldControl>
        </XhFieldRoot>
      </XhFormFieldGroup>

      <XhFormSubmitTrigger class="auth-submit">
        {{ t('page.auth.reset_btn') }}
      </XhFormSubmitTrigger>
    </XhFormRoot>

    <p
      class="mt-6 auth-helper text-center"
      :class="isDark ? 'text-gray-400' : 'text-[hsl(var(--muted-foreground))]'"
    >
      <span class="cursor-pointer link-primary" @click="router.push(LOGIN_PATH)">
        {{ t('page.auth.back_to_login') }}
      </span>
    </p>
  </div>
</template>

<style scoped>
.link-primary {
  color: hsl(var(--primary));
}

.link-primary:hover {
  text-decoration: underline;
}
</style>
