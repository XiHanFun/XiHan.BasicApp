<script lang="ts" setup>
import type { FormInst, FormRules } from 'naive-ui'
import { NButton, NForm, NFormItem, NInput, useMessage } from 'naive-ui'
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
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
    // TODO: call reset password API
    message.success(t('page.auth.reset_link_sent'))
  }
  finally {
    loading.value = false
  }
}

function handleKeydown(e: KeyboardEvent) {
  if (e.key === 'Enter') handleSubmit()
}
</script>

<template>
  <div>
    <h1 class="mb-1 text-2xl font-bold">
      {{ t('page.auth.forget_password_title') }}
    </h1>
    <p
      class="mb-5 text-sm"
      :class="isDark ? 'text-gray-400' : 'text-[hsl(var(--muted-foreground))]'"
    >
      {{ t('page.auth.forget_password_subtitle') }}
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
      <NFormItem path="email" :show-feedback="false" class="!mb-5">
        <NInput
          v-model:value="formData.email"
          placeholder="example@example.com"
          :input-props="{ autocomplete: 'email' }"
        />
      </NFormItem>

      <NButton type="primary" block :loading="loading" @click="handleSubmit">
        {{ t('page.auth.send_reset_link') }}
      </NButton>
    </NForm>

    <NButton class="mt-4 w-full" quaternary @click="router.push('/auth/login')">
      {{ t('page.auth.back_to_login') }}
    </NButton>
  </div>
</template>
