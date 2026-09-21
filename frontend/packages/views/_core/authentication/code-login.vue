<script lang="ts" setup>
import type { FormRules } from '@xihan-ui/headless'
import { XhButton, XhFieldControl, XhFieldRoot, XhFormFieldGroup, XhFormRoot, XhFormSubmitTrigger, XhPinInputInput, XhPinInputRoot } from '@xihan-ui/vue'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { XInput } from '~/components'
import { toast } from '~/composables'
import { useTheme } from '~/hooks'
import { useAppContext, useAuthStore } from '~/stores'
import CodeCountdown from '../shared/CodeCountdown.vue'
import { OTP_CODE_LENGTH, splitPinCode } from '../shared/pin-code'
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
  phone: '',
  code: '',
})

/**
 * 验证码的逐格值。表单里的 code 是拼接后的串（规则按长度校验、发码接口回填调试码都用它），
 * 格子这边是逐格数组，两边在此互转：格子每次改动把串写回表单，表单的串被外部整份改写
 * （回填调试码）时再拆回格子。用户逐格编辑不经串往返——见 splitPinCode 的说明。
 */
const codeCells = ref<string[]>([])

watch(() => formData.value.code, (code) => {
  if (code !== codeCells.value.join(''))
    codeCells.value = splitPinCode(code, OTP_CODE_LENGTH)
})

// 规则写成 computed：文案要跟着语言切换。组件库按 rule.message 优先、
// 没写则回落 validateMessages 模板，这里逐条给了文案就不需要模板
const rules = computed<FormRules>(() => ({
  phone: [
    { required: true, message: t('page.auth.phone_placeholder') },
    { pattern: /^\d{11}$/, message: t('page.auth.phone_invalid') },
  ],
  code: [
    { required: true, message: t('page.auth.code_required') },
    // 组件库按 min/max 比长度，没有 len 这一档；两端同值即定长
    { min: OTP_CODE_LENGTH, max: OTP_CODE_LENGTH, message: t('page.auth.code_length_tip') },
  ],
}))

/**
 * 发验证码只关手机号这一个字段，而表单的公开 API 没有「单字段校验」这一项
 * （逐字段校验是 blur / change 模式下的内部动作）。这里就地判一次格式，
 * 提交那一路仍走表单自己的整表校验。
 */
function handleSendCode() {
  if (!/^\d{11}$/.test(formData.value.phone)) {
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
      toast.danger(error?.message || t('page.auth.code_send_failed'))
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
      toast.danger(error.message)
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
        class="mt-3 auth-body"
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
      <XhFormFieldGroup v-slot="{ value, setValue }" name="phone" class="!mb-6">
        <XhFieldRoot>
          <XhFieldControl>
            <XInput
              size="lg"
              :value="(value as string)"
              :placeholder="t('page.auth.phone_placeholder')"
              :max-length="11"
              @update:value="setValue"
            />
          </XhFieldControl>
        </XhFieldRoot>
      </XhFormFieldGroup>

      <XhFormFieldGroup v-slot="{ setValue }" name="code" class="!mb-6">
        <XhFieldRoot>
          <!-- 布局层留在控件外面：六格与发码钮同一行，放不下时钮换到下一行靠右。
               格子取缺省档：正方格的缺省档与 lg 档文本框、发码钮同一个控件高度，lg 档格子会高出一截 -->
          <div class="auth-code-row">
            <XhFieldControl>
              <XhPinInputRoot
                v-model:value="codeCells"
                :length="OTP_CODE_LENGTH"
                type="numeric"
                otp
                @value-change="setValue($event.valueAsString)"
              >
                <!-- 格间距长在格子自己身上，这层包裹只负责排成一行 -->
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
