<script lang="ts" setup>
import type { FormInst, FormRules } from 'naive-ui'
import { NButton, NForm, NFormItem, NInput, useMessage } from 'naive-ui'
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { requestPasswordResetApi } from '@/api'
import { useTheme } from '~/hooks'

defineOptions({ name: 'ForgetPasswordPage' })

const { isDark } = useTheme()
const { t } = useI18n()
const router = useRouter()
const message = useMessage()
const formRef = ref<FormInst | null>(null)
const loading = ref(false)

const formData = ref({
  email: '',
})

const rules: FormRules = {
  email: [
    { required: true, message: () => t('page.auth.email_placeholder'), trigger: 'blur' },
    { type: 'email', message: () => t('page.auth.email_invalid'), trigger: 'blur' },
  ],
}

async function handleSubmit() {
  try {
    await formRef.value?.validate()
    loading.value = true
    const result = await requestPasswordResetApi(formData.value.email, 1)
    if (result.temporaryPassword) {
      message.success(`${t('page.auth.reset_link_sent')}（临时密码：${result.temporaryPassword}）`)
    } else {
      message.success(t('page.auth.reset_link_sent'))
    }
  }
  finally {
    loading.value = false
  }
}

function handleKeydown(e: KeyboardEvent) {
  if (e.key === 'Enter')
    handleSubmit()
}
</script>

<template>
  <div class="py-1">
    <div class="mb-8">
      <h1 class="text-[32px] font-semibold leading-tight sm:text-[36px]">
        {{ t('page.auth.forget_password_title') }}
      </h1>
      <p
        class="mt-3 text-[15px] leading-7"
        :class="isDark ? 'text-gray-300' : 'text-[hsl(var(--muted-foreground))]'"
      >
        {{ t('page.auth.forget_password_subtitle') }}
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
          placeholder="example@example.com"
          :input-props="{ autocomplete: 'email' }"
        />
      </NFormItem>

      <NButton
        type="primary"
        block
        :loading="loading"
        class="!h-12 !rounded-xl !text-[15px] !font-semibold"
        @click="handleSubmit"
      >
        {{ t('page.auth.send_reset_link') }}
      </NButton>
    </NForm>

    <NButton class="mt-5 !h-11 w-full !rounded-xl" quaternary @click="router.push('/auth/login')">
      {{ t('page.auth.back_to_login') }}
    </NButton>
  </div>
</template>
