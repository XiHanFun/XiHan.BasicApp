<script lang="ts" setup>
import type { FormInst, FormRules } from 'naive-ui'
import { Icon } from '@iconify/vue'
import {
  NButton,
  NCheckbox,
  NDivider,
  NForm,
  NFormItem,
  NIcon,
  NInput,
  NSelect,
  useMessage,
} from 'naive-ui'
import { computed, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useTheme } from '~/hooks'
import { useAuthStore } from '~/stores'

defineOptions({ name: 'LoginPage' })

const { isDark } = useTheme()
const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const message = useMessage()
const formRef = ref<FormInst | null>(null)
const rememberMe = ref(true)
const showPassword = ref(false)

const accountOptions = [
  { label: 'Super', value: 'superadmin' },
  { label: 'Admin', value: 'admin' },
  { label: 'User', value: 'user' },
]

const formData = ref({
  selectAccount: 'superadmin',
  username: 'superadmin',
  password: 'Admin@123',
})

const rules: FormRules = {
  username: [{ required: true, message: () => t('page.login.username_placeholder'), trigger: 'blur' }],
  password: [{ required: true, message: () => t('page.login.password_placeholder'), trigger: 'blur' }],
}

const redirect = computed(() => {
  const r = route.query.redirect as string
  return r ? decodeURIComponent(r) : undefined
})

function handleSelectAccount(value: string) {
  formData.value.username = value
  formData.value.password = 'Admin@123'
}

async function handleLogin() {
  try {
    await formRef.value?.validate()
    await authStore.login(
      {
        username: formData.value.username,
        password: formData.value.password,
      },
      redirect.value,
    )
  }
  catch (err: unknown) {
    const error = err as { message?: string }
    if (error?.message) {
      message.error(error.message)
    }
  }
}

function handleKeydown(e: KeyboardEvent) {
  if (e.key === 'Enter') handleLogin()
}

function goTo(path: string) {
  router.push(path)
}
</script>

<template>
  <div>
    <h1 class="mb-1 text-2xl font-bold">
      {{ t('page.auth.welcome_back') }}
    </h1>
    <p
      class="mb-5 text-sm"
      :class="isDark ? 'text-gray-400' : 'text-[hsl(var(--muted-foreground))]'"
    >
      {{ t('page.auth.login_subtitle') }}
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
      <NFormItem path="selectAccount" :show-feedback="false" class="!mb-5">
        <NSelect
          v-model:value="formData.selectAccount"
          :options="accountOptions"
          :placeholder="t('page.auth.select_account')"
          @update:value="handleSelectAccount"
        />
      </NFormItem>
      <NFormItem path="username" :show-feedback="false" class="!mb-5">
        <NInput
          v-model:value="formData.username"
          :placeholder="t('page.login.username_placeholder')"
          :input-props="{ autocomplete: 'username' }"
        />
      </NFormItem>
      <NFormItem path="password" :show-feedback="false" class="!mb-5">
        <NInput
          v-model:value="formData.password"
          :type="showPassword ? 'text' : 'password'"
          :placeholder="t('page.login.password_placeholder')"
          :input-props="{ autocomplete: 'current-password' }"
        >
          <template #suffix>
            <NIcon
              class="cursor-pointer"
              :class="isDark ? 'text-gray-400' : 'text-[hsl(var(--muted-foreground))]'"
              @click="showPassword = !showPassword"
            >
              <Icon :icon="showPassword ? 'lucide:eye-off' : 'lucide:eye'" width="16" />
            </NIcon>
          </template>
        </NInput>
      </NFormItem>

      <div class="mb-4 flex items-center justify-between text-sm">
        <NCheckbox v-model:checked="rememberMe">
          {{ t('page.login.remember_me') }}
        </NCheckbox>
        <span class="link-primary cursor-pointer" @click="goTo('/auth/forget-password')">
          {{ t('page.login.forgot_password') }}?
        </span>
      </div>

      <NButton type="primary" block :loading="authStore.loginLoading" @click="handleLogin">
        {{ t('page.login.login_btn') }}
      </NButton>
    </NForm>

    <div class="mt-4 grid grid-cols-2 gap-2">
      <NButton quaternary @click="goTo('/auth/code-login')">
        {{ t('page.auth.mobile_login') }}
      </NButton>
      <NButton quaternary @click="goTo('/auth/qrcode-login')">
        {{ t('page.auth.qrcode_login') }}
      </NButton>
    </div>

    <NDivider
      :class="isDark ? '!my-5 !border-white/10' : '!my-5 !border-[hsl(var(--border))]'"
    >
      {{ t('page.auth.third_party_login') }}
    </NDivider>
    <div class="flex items-center justify-center gap-3">
      <NButton circle quaternary>
        <template #icon>
          <Icon icon="logos:github-icon" />
        </template>
      </NButton>
      <NButton circle quaternary>
        <template #icon>
          <Icon icon="logos:wechat" />
        </template>
      </NButton>
      <NButton circle quaternary>
        <template #icon>
          <Icon icon="logos:google-icon" />
        </template>
      </NButton>
    </div>

    <p
      class="mt-5 text-center text-xs"
      :class="isDark ? 'text-gray-500' : 'text-[hsl(var(--muted-foreground))]'"
    >
      {{ t('page.auth.no_account') }}
      <span class="link-primary cursor-pointer" @click="goTo('/auth/register')">
        {{ t('page.login.register') }}
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
