<script lang="ts" setup>
import type { FormRules } from '@xihan-ui/headless'
import { XhButton, XhFieldControl, XhFieldRoot, XhFormFieldGroup, XhFormRoot, XhFormSubmitTrigger } from '@xihan-ui/vue'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { PhoneInput, XInput } from '~/components'
import { toast } from '~/composables'
import { useTheme } from '~/hooks'
import { useAppContext, useAuthStore } from '~/stores'
import CodeCountdown from '../shared/CodeCountdown.vue'
import { useAuthFormInvalid } from './use-auth-form-invalid'

defineOptions({ name: 'CodeLoginPage' })

const { isDark } = useTheme()
const { t } = useI18n()
const authStore = useAuthStore()
const { apis } = useAppContext()
const loading = ref(false)
/** 重发倒计时这一轮的时长，大于 0 即正在倒计时 */
const resendSeconds = ref(0)

const formData = ref({
  /** E.164 手机号码，由 PhoneInput 组装吐出 */
  phone: '',
  code: '',
})
/** PhoneInput 的号码有效性；required 拦空值，这里拦「填了但格式不对」 */
const phoneValid = ref(false)

// 规则写成 computed：文案要跟着语言切换。组件库按 rule.message 优先、
// 没写则回落 validateMessages 模板，这里逐条给了文案就不需要模板
const rules = computed<FormRules>(() => ({
  phone: [
    // 校验规则首败即停：PhoneInput 对无效号码吐出的是空串，required 单看 formData.phone
    // 会把「填了但格式不对」误判成「没填」。validator 只认 phoneValid（空值在该组件里算有效），
    // 放在 required 前面，格式错误才能在 required 之前先被拦下、显示 phone_invalid 而非必填文案
    { validator: () => (phoneValid.value ? null : t('page.auth.phone_invalid')) },
    { required: true, message: t('page.auth.phone_placeholder') },
  ],
  code: [
    { required: true, message: t('page.auth.code_placeholder') },
    // 组件库按 min/max 比长度，没有 len 这一档；两端同值即定长
    { min: 6, max: 6, message: t('page.auth.code_length_tip') },
  ],
}))

/**
 * 发验证码只关手机号这一个字段，而表单的公开 API 没有「单字段校验」这一项
 * （逐字段校验是 blur / change 模式下的内部动作）。这里就地判一次格式，
 * 提交那一路仍走表单自己的整表校验。
 */
function handleSendCode() {
  if (!phoneValid.value || !formData.value.phone) {
    toast.warning(t('page.auth.phone_invalid'))
    return
  }
  void (async () => {
    try {
      const response = await apis.sendPhoneLoginCodeApi(formData.value.phone)
      resendSeconds.value = 60
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
    await authStore.loginByPhoneCode({
      phone: formData.value.phone,
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

const onAuthInvalid = useAuthFormInvalid()
</script>

<template>
  <div class="py-1">
    <div class="mb-8">
      <p
        class="mt-3 text-[15px] leading-7"
        :class="isDark ? 'text-gray-300' : 'text-[hsl(var(--muted-foreground))]'"
      >
        {{ t('page.auth.code_login_subtitle') }}
      </p>
    </div>

    <!-- 校验归表单：通过才发 submit，被拦下的错误由各字段的 error-text 自己显 -->
    <XhFormRoot
      v-model:values="formData"
      :rules="rules"
      validate-on="blur"
      @invalid="onAuthInvalid"
      @submit="onSubmit"
    >
      <XhFormFieldGroup v-slot="{ value, setValue }" value="phone" class="!mb-6">
        <XhFieldRoot>
          <XhFieldControl>
            <PhoneInput
              size="lg"
              :value="(value as string)"
              @update:value="setValue"
              @valid="(v: boolean) => phoneValid = v"
            />
          </XhFieldControl>
        </XhFieldRoot>
      </XhFormFieldGroup>

      <XhFormFieldGroup v-slot="{ value, setValue }" value="code" class="!mb-6">
        <XhFieldRoot>
          <div class="auth-code-row">
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

      <XhFormSubmitTrigger
        class="auth-submit"
        :data-loading="loading ? '' : undefined"
        :disabled="loading"
      >
        {{ t('page.login.login_btn') }}
      </XhFormSubmitTrigger>
    </XhFormRoot>
  </div>
</template>
