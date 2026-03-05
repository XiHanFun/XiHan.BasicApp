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
  <div class="py-1">
    <div class="mb-8">
      <h1 class="text-[32px] font-semibold leading-tight sm:text-[36px]">
        {{ t('page.auth.welcome_back') }}
      </h1>
      <p
        class="mt-3 text-[15px] leading-7"
        :class="isDark ? 'text-gray-300' : 'text-[hsl(var(--muted-foreground))]'"
      >
        {{ t('page.auth.login_subtitle') }}
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
      <NFormItem path="selectAccount" :show-feedback="false" class="!mb-6">
        <NSelect
          v-model:value="formData.selectAccount"
          :options="accountOptions"
          :placeholder="t('page.auth.select_account')"
          size="large"
          @update:value="handleSelectAccount"
        />
      </NFormItem>
      <NFormItem path="username" :show-feedback="false" class="!mb-6">
        <NInput
          v-model:value="formData.username"
          size="large"
          :placeholder="t('page.login.username_placeholder')"
          :input-props="{ autocomplete: 'username' }"
        />
      </NFormItem>
      <NFormItem path="password" :show-feedback="false" class="!mb-6">
        <NInput
          v-model:value="formData.password"
          :type="showPassword ? 'text' : 'password'"
          size="large"
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

      <div class="mb-5 flex items-center justify-between text-sm">
        <NCheckbox v-model:checked="rememberMe">
          {{ t('page.login.remember_me') }}
        </NCheckbox>
        <span class="link-primary cursor-pointer" @click="goTo('/auth/forget-password')">
          {{ t('page.login.forgot_password') }}?
        </span>
      </div>

      <NButton
        type="primary"
        block
        :loading="authStore.loginLoading"
        class="!h-12 !rounded-xl !text-[15px] !font-semibold"
        @click="handleLogin"
      >
        {{ t('page.login.login_btn') }}
      </NButton>
    </NForm>

    <NDivider
      :class="isDark ? '!my-6 !border-white/10' : '!my-6 !border-[hsl(var(--border))]'"
    >
      {{ t('page.auth.third_party_login') }}
    </NDivider>
    <div class="flex items-center justify-center gap-4">
      <NButton circle quaternary class="!h-11 !w-11">
        <template #icon>
          <Icon icon="logos:github-icon" />
        </template>
      </NButton>
      <NButton circle quaternary class="!h-11 !w-11">
        <template #icon>
          <Icon icon="logos:google-icon" />
        </template>
      </NButton>
    </div>

    <p
      class="mt-6 text-center text-sm"
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
