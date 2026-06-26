<script lang="ts" setup>
import type { FormInst, FormRules } from 'naive-ui'
import { NButton, NForm, NFormItem, NInput, NInputGroup, useMessage } from 'naive-ui'
import { onBeforeUnmount, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useTheme } from '~/hooks'
import { useAppContext, useAuthStore } from '~/stores'

defineOptions({ name: 'EmailLoginPage' })

const { isDark } = useTheme()
const { t } = useI18n()
const message = useMessage()
const authStore = useAuthStore()
const { apis } = useAppContext()
const formRef = ref<FormInst | null>(null)
const loading = ref(false)
const countdown = ref(0)
let timer: ReturnType<typeof setInterval> | null = null

const formData = ref({
  email: '',
  code: '',
})

const rules: FormRules = {
  email: [
    { required: true, message: () => t('page.auth.email_placeholder'), trigger: 'blur' },
    { type: 'email', message: () => t('page.auth.email_invalid'), trigger: 'blur' },
  ],
  code: [
    { required: true, message: () => t('page.auth.code_placeholder'), trigger: 'blur' },
    { len: 6, message: () => t('page.auth.code_length_tip'), trigger: 'blur' },
  ],
}

function handleSendCode() {
  formRef.value?.validate(
    async (errors) => {
      if (errors)
        return
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
        message.success(t('page.auth.code_sent'))
      }
      catch (err: unknown) {
        const error = err as { message?: string }
        message.error(error?.message || '发送验证码失败')
      }
    },
    rule => rule?.key === 'email',
  )
}

async function handleLogin() {
  try {
    await formRef.value?.validate()
    loading.value = true
    await authStore.loginByEmailCode({
      email: formData.value.email,
      code: formData.value.code,
    })
  }
  catch (err: unknown) {
    const error = err as { message?: string }
    if (error?.message) {
      message.error(error.message)
    }
  }
  finally {
    loading.value = false
  }
}

function handleKeydown(e: KeyboardEvent) {
  if (e.key === 'Enter')
    handleLogin()
}

onBeforeUnmount(() => {
  if (!timer)
    return
  clearInterval(timer)
  timer = null
})
</script>

<template>
  <div class="py-1">
    <div class="mb-8">
      <h1 class="text-[32px] font-semibold leading-tight sm:text-[36px]">
        {{ t('page.auth.email_login') }}
      </h1>
      <p
        class="mt-3 text-[15px] leading-7"
        :class="isDark ? 'text-gray-300' : 'text-[hsl(var(--muted-foreground))]'"
      >
        {{ t('page.auth.email_login_subtitle') }}
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
      <NFormItem path="email" :show-feedback="false" class="!mb-6">
        <NInput
          v-model:value="formData.email"
          size="large"
          :placeholder="t('page.auth.email_placeholder')"
          :input-props="{ autocomplete: 'email' }"
        />
      </NFormItem>
      <NFormItem path="code" :show-feedback="false" class="!mb-6">
        <NInputGroup>
          <NInput
            v-model:value="formData.code"
            size="large"
            :placeholder="t('page.auth.code_placeholder')"
            :maxlength="6"
          />
          <NButton
            type="primary"
            ghost
            :disabled="countdown > 0"
            size="large"
            style="min-width: 132px"
            @click="handleSendCode"
          >
            {{ countdown > 0 ? `${countdown}s` : t('page.auth.send_code') }}
          </NButton>
        </NInputGroup>
      </NFormItem>

      <NButton
        type="primary"
        block
        :loading="loading"
        class="!h-12 !rounded-xl !text-[15px] !font-semibold"
        @click="handleLogin"
      >
        {{ t('page.login.login_btn') }}
      </NButton>
    </NForm>
  </div>
</template>
