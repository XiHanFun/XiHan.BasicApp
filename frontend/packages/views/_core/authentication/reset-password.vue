<script lang="ts" setup>
import type { FormRules } from '@xihan-ui/headless'
import { XhFieldControl, XhFieldLabel, XhFieldRoot, XhFormFieldGroup, XhFormRoot, XhFormSubmitTrigger } from '@xihan-ui/vue'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { XInput } from '~/components'
import { toast } from '~/composables'
import { LOGIN_PATH } from '~/constants'
import { useTheme } from '~/hooks'
import { useAppContext } from '~/stores'
import { useAuthFormInvalid } from './use-auth-form-invalid'

defineOptions({ name: 'ResetPasswordPage' })

const { isDark } = useTheme()
const route = useRoute()
const router = useRouter()
const { t } = useI18n()
const { apis } = useAppContext()

// 一次性重置令牌（来自找回密码邮件链接）
const token = computed(() => (route.query.token as string) || '')

const formData = ref({
  newPassword: '',
  confirmPassword: '',
})

const rules = computed<FormRules>(() => ({
  newPassword: [
    { required: true, message: '请输入新密码' },
    { min: 8, max: 128, message: '密码长度需为 8-128 位' },
  ],
  confirmPassword: [
    { required: true, message: '请再次输入新密码' },
    {
      // 先填确认、再改新密码时由 deps 带着重验
      deps: ['newPassword'],
      validator: (value, values) =>
        value === values.newPassword ? null : '两次输入的密码不一致',
    },
  ],
}))

// 返回的 Promise 交给表单：落定前提交钮报在途、再按不重复提交，失败在这里自己接住
async function onSubmit() {
  if (!token.value) {
    toast.danger(t('page.auth.reset_token_invalid'))
    return
  }
  try {
    await apis.consumePasswordResetTokenApi(token.value, formData.value.newPassword)
    toast.success(t('page.auth.reset_success'))
    router.push(LOGIN_PATH)
  }
  catch (e: unknown) {
    const msg = (e as Error)?.message
    if (msg)
      toast.danger(msg)
  }
}

const onAuthInvalid = useAuthFormInvalid()
</script>

<template>
  <div class="py-1">
    <div class="mb-8">
      <h1 class="text-[32px] font-semibold leading-tight sm:text-[36px]">
        重置密码
      </h1>
      <p
        class="mt-3 auth-body"
        :class="isDark ? 'text-gray-300' : 'text-[hsl(var(--muted-foreground))]'"
      >
        请设置新的登录密码，该链接仅可使用一次。
      </p>
    </div>

    <XhFormRoot
      v-model:values="formData"
      :rules="rules"
      validate-on="blur"
      @invalid="onAuthInvalid"
      @submit="onSubmit"
    >
      <!-- 两个字段靠占位文案表意，标签只留给读屏 -->
      <XhFormFieldGroup name="newPassword" class="!mb-4">
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
            {{ t('page.auth.reset_confirm_placeholder') }}
          </XhFieldLabel>
          <XhFieldControl>
            <XInput
              v-model:value="formData.confirmPassword"
              type="password"
              size="lg"
              :placeholder="t('page.auth.reset_confirm_placeholder')"
              autocomplete="new-password"
            />
          </XhFieldControl>
        </XhFieldRoot>
      </XhFormFieldGroup>

      <XhFormSubmitTrigger class="auth-submit">
        确认重置
      </XhFormSubmitTrigger>
    </XhFormRoot>

    <p
      class="mt-6 auth-helper text-center"
      :class="isDark ? 'text-gray-400' : 'text-[hsl(var(--muted-foreground))]'"
    >
      <span class="cursor-pointer link-primary" @click="router.push(LOGIN_PATH)">
        返回登录
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
