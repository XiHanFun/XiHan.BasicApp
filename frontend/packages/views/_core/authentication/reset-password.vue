<script lang="ts" setup>
import type { FormInst, FormItemRule, FormRules } from 'naive-ui'
import { NButton, NForm, NFormItem, NInput, useMessage } from 'naive-ui'
import { computed, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { LOGIN_PATH } from '~/constants'
import { useTheme } from '~/hooks'
import { useAppContext } from '~/stores'

defineOptions({ name: 'ResetPasswordPage' })

const { isDark } = useTheme()
const route = useRoute()
const router = useRouter()
const message = useMessage()
const { apis } = useAppContext()
const formRef = ref<FormInst | null>(null)
const loading = ref(false)

// 一次性重置令牌（来自找回密码邮件链接）
const token = computed(() => (route.query.token as string) || '')

const formData = ref({
  newPassword: '',
  confirmPassword: '',
})

const rules: FormRules = {
  newPassword: [
    { required: true, message: '请输入新密码', trigger: 'blur' },
    { min: 8, max: 128, message: '密码长度需为 8-128 位', trigger: 'blur' },
  ],
  confirmPassword: [
    { required: true, message: '请再次输入新密码', trigger: 'blur' },
    {
      validator: (_rule: FormItemRule, value: string) => value === formData.value.newPassword,
      message: '两次输入的密码不一致',
      trigger: 'blur',
    },
  ],
}

async function handleSubmit() {
  if (!token.value) {
    message.error('重置链接无效或已过期，请重新申请找回密码')
    return
  }
  try {
    await formRef.value?.validate()
    loading.value = true
    await apis.consumePasswordResetTokenApi(token.value, formData.value.newPassword)
    message.success('密码已重置，请使用新密码登录')
    router.push(LOGIN_PATH)
  }
  catch (e: unknown) {
    const msg = (e as Error)?.message
    if (msg)
      message.error(msg)
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
        重置密码
      </h1>
      <p
        class="mt-3 text-[15px] leading-7"
        :class="isDark ? 'text-gray-300' : 'text-[hsl(var(--muted-foreground))]'"
      >
        请设置新的登录密码，该链接仅可使用一次。
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
      <NFormItem path="newPassword" class="!mb-4">
        <NInput
          v-model:value="formData.newPassword"
          type="password"
          show-password-on="click"
          size="large"
          placeholder="新密码（8-128 位）"
          :input-props="{ autocomplete: 'new-password' }"
        />
      </NFormItem>
      <NFormItem path="confirmPassword" class="!mb-6">
        <NInput
          v-model:value="formData.confirmPassword"
          type="password"
          show-password-on="click"
          size="large"
          placeholder="确认新密码"
          :input-props="{ autocomplete: 'new-password' }"
        />
      </NFormItem>

      <NButton
        type="primary"
        block
        :loading="loading"
        class="!h-12 !rounded-xl !text-[15px] !font-semibold"
        @click="handleSubmit"
      >
        确认重置
      </NButton>
    </NForm>

    <p
      class="mt-6 text-sm text-center"
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
