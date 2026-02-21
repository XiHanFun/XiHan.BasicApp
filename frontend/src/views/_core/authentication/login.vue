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
import { useRoute } from 'vue-router'
import { useAuthStore } from '@/store/auth'

defineOptions({ name: 'LoginPage' })

const route = useRoute()
const authStore = useAuthStore()
const message = useMessage()
const formRef = ref<FormInst | null>(null)
const rememberMe = ref(true)
const showPassword = ref(false)
const appTitle = import.meta.env.VITE_APP_TITLE || 'XiHan Admin'
const appLogo = import.meta.env.VITE_APP_LOGO || '/favicon.png'

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
  username: [{ required: true, message: '请输入用户名', trigger: 'blur' }],
  password: [{ required: true, message: '请输入密码', trigger: 'blur' }],
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
  if (e.key === 'Enter') {
    handleLogin()
  }
}
</script>

<template>
  <div class="min-h-screen bg-[#0b1220] text-white">
    <div class="grid min-h-screen grid-cols-1 lg:grid-cols-[1fr_420px]">
      <div class="relative hidden overflow-hidden lg:flex lg:items-center lg:justify-center">
        <div
          class="pointer-events-none absolute inset-0 bg-[radial-gradient(circle_at_30%_30%,#1d4ed8_0%,transparent_38%),radial-gradient(circle_at_80%_65%,#0ea5e9_0%,transparent_28%)] opacity-40"
        />
        <div class="relative text-center">
          <div
            class="mx-auto mb-6 flex h-16 w-16 items-center justify-center rounded-2xl bg-white/90"
          >
            <img :src="appLogo" :alt="appTitle" class="h-10 w-10 object-contain" />
          </div>
          <h2 class="mb-3 text-3xl font-semibold">开箱即用的大型中后台管理系统</h2>
          <p class="text-sm text-gray-300">工程化、智能化、国际化的前端组织架构</p>
        </div>
      </div>

      <div
        class="flex items-center justify-center px-8 py-10 backdrop-blur-xl lg:border-l lg:border-white/10 lg:bg-black/30"
      >
        <div class="w-full max-w-[340px]">
          <h1 class="mb-1 text-3xl font-bold">欢迎回来 👋🏻</h1>
          <p class="mb-6 text-sm text-gray-400">请输入您的账号密码以登录管理系统</p>

          <NForm
            ref="formRef"
            :model="formData"
            :rules="rules"
            label-placement="top"
            size="large"
            @keydown="handleKeydown"
          >
            <NFormItem path="selectAccount">
              <NSelect
                v-model:value="formData.selectAccount"
                :options="accountOptions"
                placeholder="选择账号"
                @update:value="handleSelectAccount"
              />
            </NFormItem>
            <NFormItem path="username">
              <NInput
                v-model:value="formData.username"
                placeholder="请输入用户名"
                :input-props="{ autocomplete: 'username' }"
              />
            </NFormItem>
            <NFormItem path="password">
              <NInput
                v-model:value="formData.password"
                :type="showPassword ? 'text' : 'password'"
                placeholder="请输入密码"
                :input-props="{ autocomplete: 'current-password' }"
              >
                <template #suffix>
                  <NIcon class="cursor-pointer text-gray-400" @click="showPassword = !showPassword">
                    <Icon :icon="showPassword ? 'lucide:eye-off' : 'lucide:eye'" width="16" />
                  </NIcon>
                </template>
              </NInput>
            </NFormItem>

            <div class="mb-4 flex items-center justify-between text-sm">
              <NCheckbox v-model:checked="rememberMe">记住账号</NCheckbox>
              <a href="#" class="text-sky-400 hover:underline">忘记密码?</a>
            </div>

            <NButton
              type="primary"
              block
              secondary
              :loading="authStore.loginLoading"
              @click="handleLogin"
            >
              立即登录
            </NButton>
          </NForm>

          <div class="mt-4 grid grid-cols-2 gap-2">
            <NButton quaternary>手机登录</NButton>
            <NButton quaternary>扫码登录</NButton>
          </div>

          <NDivider class="!my-5 !border-white/10">第三方登录</NDivider>
          <div class="flex items-center justify-center gap-3">
            <NButton circle quaternary>
              <template #icon><Icon icon="logos:github-icon" /></template>
            </NButton>
            <NButton circle quaternary>
              <template #icon><Icon icon="logos:wechat" /></template>
            </NButton>
            <NButton circle quaternary>
              <template #icon><Icon icon="logos:google-icon" /></template>
            </NButton>
          </div>

          <p class="mt-8 text-center text-xs text-gray-500">
            还没有账号?
            <a class="text-sky-400 hover:underline" href="#">立即注册</a>
          </p>
        </div>
      </div>
    </div>
  </div>
</template>
