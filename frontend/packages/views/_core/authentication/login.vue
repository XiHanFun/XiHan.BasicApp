<script lang="ts" setup>
import type { FormInst, FormRules } from 'naive-ui'
import type { LoginConfig } from '~/types'
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
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { getLoginConfigApi } from '@/api'
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
const loginConfig = ref<LoginConfig>({
  loginMethods: ['password'],
  tenantEnabled: true,
  oauthProviders: [],
})

const accountOptions = [
  { label: 'Super', value: 'superadmin' },
  { label: 'Admin', value: 'admin' },
  { label: 'User', value: 'user' },
]

const formData = ref({
  selectAccount: 'superadmin',
  username: 'superadmin',
  password: 'Admin@123',
  tenantId: '1',
})

const rules: FormRules = {
  username: [
    { required: true, message: () => t('page.login.username_placeholder'), trigger: 'blur' },
  ],
  password: [
    { required: true, message: () => t('page.login.password_placeholder'), trigger: 'blur' },
  ],
  tenantId: [
    {
      trigger: 'blur',
      validator: (_rule, value: string) => {
        if (!loginConfig.value.tenantEnabled) return true
        if (!value?.trim()) return new Error('请输入租户ID')
        return true
      },
    },
  ],
}

const redirect = computed(() => {
  const r = route.query.redirect as string
  return r ? decodeURIComponent(r) : undefined
})

const defaultOauthProviders = ['github', 'google']
const oauthProviderMeta: Record<string, { icon: string; label: string }> = {
  github: { icon: 'mdi:github', label: 'GitHub' },
  google: { icon: 'logos:google-icon', label: 'Google' },
}

const oauthProviders = computed(() => {
  const providers = (loginConfig.value.oauthProviders || [])
    .map((provider) => provider.trim().toLowerCase())
    .filter(Boolean)
  return providers.length > 0 ? providers : defaultOauthProviders
})

function getOauthProviderLabel(provider: string) {
  return oauthProviderMeta[provider]?.label ?? provider.toUpperCase()
}

function getOauthProviderIcon(provider: string) {
  return oauthProviderMeta[provider]?.icon ?? 'lucide:link-2'
}

function handleSelectAccount(value: string) {
  formData.value.username = value
  formData.value.password = 'Admin@123'
}

async function loadLoginConfig() {
  loginConfig.value = await getLoginConfigApi()
}

async function handleLogin() {
  try {
    await formRef.value?.validate()
    const parsedTenantId = Number(formData.value.tenantId)
    await authStore.login(
      {
        username: formData.value.username,
        password: formData.value.password,
        tenantId:
          loginConfig.value.tenantEnabled && Number.isFinite(parsedTenantId)
            ? parsedTenantId
            : undefined,
      },
      redirect.value,
    )
  } catch (err: unknown) {
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

onMounted(async () => {
  try {
    await loadLoginConfig()
  } catch {
    message.error('加载登录配置失败')
  }
})
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
      <NFormItem
        v-if="loginConfig.tenantEnabled"
        path="tenantId"
        :show-feedback="false"
        class="!mb-6"
      >
        <NInput v-model:value="formData.tenantId" size="large" placeholder="请输入租户ID" />
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

    <NDivider :class="isDark ? '!my-6 !border-white/10' : '!my-6 !border-[hsl(var(--border))]'">
      {{ t('page.auth.third_party_login') }}
    </NDivider>
    <div class="flex flex-wrap items-center justify-center gap-3">
      <NButton
        v-for="provider in oauthProviders"
        :key="provider"
        secondary
        class="!h-10 !rounded-xl !px-4 !text-sm"
      >
        <template #icon>
          <Icon :icon="getOauthProviderIcon(provider)" width="16" />
        </template>
        {{ getOauthProviderLabel(provider) }}
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
