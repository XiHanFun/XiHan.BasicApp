<script lang="ts" setup>
import type { FormRules } from '@xihan-ui/headless'
import {
  XhButton,
  XhFieldControl,
  XhFieldErrorText,
  XhFieldRoot,
  XhFormFieldGroup,
  XhFormRoot,
  XhFormSubmitTrigger,
} from '@xihan-ui/vue'
import { computed, onBeforeUnmount, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { XInput } from '~/components'
import { toast } from '~/composables'
import { useTheme } from '~/hooks'
import { useAppContext, useAuthStore } from '~/stores'

defineOptions({ name: 'EmailLoginPage' })

const { isDark } = useTheme()
const { t } = useI18n()
const authStore = useAuthStore()
const { apis } = useAppContext()
const loading = ref(false)
const countdown = ref(0)
let timer: ReturnType<typeof setInterval> | null = null

const formData = ref({
  email: '',
  code: '',
})

// 规则写成 computed：文案要跟着语言切换
const rules = computed<FormRules>(() => ({
  email: [
    { required: true, message: t('page.auth.email_placeholder') },
    { type: 'email', message: t('page.auth.email_invalid') },
  ],
  code: [
    { required: true, message: t('page.auth.code_placeholder') },
    // 组件库按 min/max 比长度，没有 len 这一档；两端同值即定长
    { min: 6, max: 6, message: t('page.auth.code_length_tip') },
  ],
}))

const EMAIL_RE = /^[^\s@]+@[^\s@][^\s.@]*\.[^\s@]+$/

/**
 * 发验证码只关邮箱这一个字段，而表单的公开 API 没有「单字段校验」这一项
 * （逐字段校验是 blur / change 模式下的内部动作）。这里就地判一次格式，
 * 提交那一路仍走表单自己的整表校验。
 */
function handleSendCode() {
  if (!EMAIL_RE.test(formData.value.email)) {
    toast.warning(t('page.auth.email_invalid'))
    return
  }
  void (async () => {
    try {
      const response = await apis.sendEmailLoginCodeApi(formData.value.email)
      countdown.value = 60
      timer = setInterval(() => {
        countdown.value--
        if (countdown.value <= 0) {
          clearInterval(timer!)
          timer = null
        }
      }, 1000)
      if (response.debugCode) {
        formData.value.code = response.debugCode
      }
      toast.success(t('page.auth.code_sent'))
    }
    catch (err: unknown) {
      const error = err as { message?: string }
      toast.error(error?.message || t('page.auth.code_send_failed'))
    }
  })()
}

/** 校验通过表单才发 submit；被拦下走 invalid，错误文案由字段自己显 */
async function onSubmit() {
  loading.value = true
  try {
    await authStore.loginByEmailCode({
      email: formData.value.email,
      code: formData.value.code,
    })
  }
  catch (err: unknown) {
    const error = err as { message?: string }
    if (error?.message) {
      toast.error(error.message)
    }
  }
  finally {
    loading.value = false
  }
}

onBeforeUnmount(() => {
  if (timer) {
    clearInterval(timer)
  }
})
</script>

<template>
  <div class="py-1">
    <div class="mb-8">
      <p
        class="mt-3 text-[15px] leading-7"
        :class="isDark ? 'text-gray-300' : 'text-[hsl(var(--muted-foreground))]'"
      >
        {{ t('page.auth.email_login_subtitle') }}
      </p>
    </div>

    <!-- 校验归表单：通过才发 submit，被拦下的错误由各字段的 error-text 自己显 -->
    <XhFormRoot
      v-model:values="formData"
      :rules="rules"
      validate-on="blur"
      @submit="onSubmit"
    >
      <XhFormFieldGroup v-slot="{ value, setValue }" value="email" class="!mb-6">
        <XhFieldRoot>
          <XhFieldControl>
            <XInput
              size="lg"
              :value="(value as string)"
              :placeholder="t('page.auth.email_placeholder')"
              @update:value="setValue"
            />
          </XhFieldControl>
          <XhFieldErrorText />
        </XhFieldRoot>
      </XhFormFieldGroup>

      <XhFormFieldGroup v-slot="{ value, setValue }" value="code" class="!mb-6">
        <XhFieldRoot>
          <div class="xh-input-group">
            <XhFieldControl>
              <XInput
                size="lg"
                :value="(value as string)"
                :placeholder="t('page.auth.code_placeholder')"
                :max-length="6"
                @update:value="setValue"
              />
            </XhFieldControl>
            <XhButton
              tone="brand"
              variant="outline"
              :disabled="countdown > 0"
              size="lg"
              style="min-width: 132px"
              @click="handleSendCode"
            >
              {{ countdown > 0 ? `${countdown}s` : t('page.auth.send_code') }}
            </XhButton>
          </div>
          <XhFieldErrorText />
        </XhFieldRoot>
      </XhFormFieldGroup>

      <XhFormSubmitTrigger class="auth-submit" :disabled="loading">
        {{ t('page.login.login_btn') }}
      </XhFormSubmitTrigger>
    </XhFormRoot>
  </div>
</template>
