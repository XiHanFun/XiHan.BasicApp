<script lang="ts" setup>
import type { FormInst, FormRules } from 'naive-ui'
import { NButton, NForm, NFormItem, NInput, NInputGroup, useMessage } from 'naive-ui'
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useTheme } from '~/hooks'

defineOptions({ name: 'CodeLoginPage' })

const { isDark } = useTheme()
const { t } = useI18n()
const router = useRouter()
const message = useMessage()
const formRef = ref<FormInst | null>(null)
const loading = ref(false)
const countdown = ref(0)
let timer: ReturnType<typeof setInterval> | null = null

const formData = ref({
  phone: '',
  code: '',
})

const rules: FormRules = {
  phone: [
    { required: true, message: () => t('page.auth.phone_placeholder'), trigger: 'blur' },
    { pattern: /^\d{11}$/, message: () => t('page.auth.phone_invalid'), trigger: 'blur' },
  ],
  code: [
    { required: true, message: () => t('page.auth.code_placeholder'), trigger: 'blur' },
    { len: 6, message: () => t('page.auth.code_length_tip'), trigger: 'blur' },
  ],
}

function handleSendCode() {
  formRef.value?.validate(
    (errors) => {
      if (errors) return
      countdown.value = 60
      timer = setInterval(() => {
        countdown.value--
        if (countdown.value <= 0) {
          clearInterval(timer!)
          timer = null
        }
      }, 1000)
      message.success(t('page.auth.code_sent'))
    },
    (rule) => rule?.key === 'phone',
  )
}

async function handleLogin() {
  try {
    await formRef.value?.validate()
    loading.value = true
    // TODO: call phone login API
    message.info('手机登录功能开发中')
  }
  finally {
    loading.value = false
  }
}

function handleKeydown(e: KeyboardEvent) {
  if (e.key === 'Enter') handleLogin()
}
</script>

<template>
  <div>
    <h1 class="mb-1 text-2xl font-bold">
      {{ t('page.auth.mobile_login') }} 📲
    </h1>
    <p
      class="mb-5 text-sm"
      :class="isDark ? 'text-gray-400' : 'text-[hsl(var(--muted-foreground))]'"
    >
      {{ t('page.auth.code_login_subtitle') }}
    </p>

    <NForm
      ref="formRef"
      :model="formData"
      :rules="rules"
      label-placement="top"
      size="medium"
      :show-label="false"
      @keydown="handleKeydown"
    >
      <NFormItem path="phone" :show-feedback="false" class="!mb-5">
        <NInput
          v-model:value="formData.phone"
          :placeholder="t('page.auth.phone_placeholder')"
          :maxlength="11"
        />
      </NFormItem>
      <NFormItem path="code" :show-feedback="false" class="!mb-5">
        <NInputGroup>
          <NInput
            v-model:value="formData.code"
            :placeholder="t('page.auth.code_placeholder')"
            :maxlength="6"
          />
          <NButton
            type="primary"
            ghost
            :disabled="countdown > 0"
            style="min-width: 120px"
            @click="handleSendCode"
          >
            {{ countdown > 0 ? `${countdown}s` : t('page.auth.send_code') }}
          </NButton>
        </NInputGroup>
      </NFormItem>

      <NButton type="primary" block :loading="loading" @click="handleLogin">
        {{ t('page.login.login_btn') }}
      </NButton>
    </NForm>

    <NButton class="mt-4 w-full" quaternary @click="router.push('/auth/login')">
      {{ t('page.auth.back_to_login') }}
    </NButton>
  </div>
</template>
