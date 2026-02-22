<script lang="ts" setup>
import type { FormInst, FormRules } from 'naive-ui'
import type { LoginFormAlign } from './LoginToolbar.vue'
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
import { useTheme } from '~/hooks'
import { useAppStore } from '~/stores'
import LoginToolbar from './LoginToolbar.vue'

defineOptions({ name: 'LoginPage' })

const formAlign = ref<LoginFormAlign>('right')
const { isDark } = useTheme()

const route = useRoute()
const authStore = useAuthStore()
const appStore = useAppStore()
const message = useMessage()
const formRef = ref<FormInst | null>(null)
const rememberMe = ref(true)
const showPassword = ref(false)
const appTitle = computed(
  () => appStore.brandTitle || import.meta.env.VITE_APP_TITLE || 'XiHan Admin',
)
const appLogo = computed(
  () => appStore.brandLogo || import.meta.env.VITE_APP_LOGO || '/favicon.png',
)

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
  <div
    class="relative min-h-screen"
    :class="
      isDark
        ? 'bg-[#0b1220] text-white'
        : 'bg-[hsl(var(--background-deep))] text-[hsl(var(--foreground))]'
    "
  >
    <LoginToolbar @layout-change="(align) => (formAlign = align)" />
    <div
      class="grid min-h-screen grid-cols-1"
      :class="{
        'lg:grid-cols-[420px_1fr]': formAlign === 'left',
        'lg:grid-cols-1': formAlign === 'center',
        'lg:grid-cols-[1fr_420px]': formAlign === 'right',
      }"
    >
      <!-- 居中时隐藏插图列；居左时插图在右侧 -->
      <div
        class="relative hidden overflow-hidden lg:flex lg:items-center lg:justify-center"
        :class="{ 'lg:order-1': formAlign === 'left', 'lg:hidden': formAlign === 'center' }"
      >
        <!-- 暗色：星空感径向渐变；亮色：柔和蓝紫渐变 -->
        <div
          class="pointer-events-none absolute inset-0 opacity-40"
          :class="
            isDark
              ? 'bg-[radial-gradient(circle_at_30%_30%,#1d4ed8_0%,transparent_38%),radial-gradient(circle_at_80%_65%,#0ea5e9_0%,transparent_28%)]'
              : 'bg-[radial-gradient(circle_at_30%_30%,#93c5fd_0%,transparent_45%),radial-gradient(circle_at_75%_70%,#a5f3fc_0%,transparent_35%)]'
          "
        />
        <div class="relative text-center">
          <div
            class="mx-auto mb-6 flex h-16 w-16 items-center justify-center rounded-2xl"
            :class="isDark ? 'bg-white/90' : 'bg-[hsl(var(--card))] shadow-md'"
          >
            <img :src="appLogo" :alt="appTitle" class="h-10 w-10 object-contain">
          </div>
          <h2 class="mb-3 text-3xl font-semibold">
            开箱即用的大型中后台管理系统
          </h2>
          <p
            class="text-sm"
            :class="isDark ? 'text-gray-300' : 'text-[hsl(var(--muted-foreground))]'"
          >
            工程化、智能化、国际化的前端组织架构
          </p>
        </div>
      </div>

      <div
        class="flex items-center justify-center px-8 py-10 lg:border-[hsl(var(--border))]"
        :class="{
          'lg:border-r': formAlign === 'left',
          'lg:border-l': formAlign === 'right',
          'lg:bg-black/30 backdrop-blur-xl': isDark,
          'lg:bg-[hsl(var(--card))]': !isDark,
        }"
      >
        <div class="w-full" :class="formAlign === 'center' ? 'max-w-[400px]' : 'max-w-[340px]'">
          <!-- 标题区 -->
          <h1 class="mb-1 text-2xl font-bold">
            欢迎回来 👋🏻
          </h1>
          <p
            class="mb-5 text-sm"
            :class="isDark ? 'text-gray-400' : 'text-[hsl(var(--muted-foreground))]'"
          >
            请输入您的账号密码以登录管理系统
          </p>

          <!-- 登录表单 -->
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
                placeholder="选择演示账号"
                @update:value="handleSelectAccount"
              />
            </NFormItem>
            <NFormItem path="username" :show-feedback="false" class="!mb-5">
              <NInput
                v-model:value="formData.username"
                placeholder="请输入用户名"
                :input-props="{ autocomplete: 'username' }"
              />
            </NFormItem>
            <NFormItem path="password" :show-feedback="false" class="!mb-5">
              <NInput
                v-model:value="formData.password"
                :type="showPassword ? 'text' : 'password'"
                placeholder="请输入密码"
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
                记住账号
              </NCheckbox>
              <a href="#" class="link-primary">忘记密码?</a>
            </div>

            <NButton type="primary" block :loading="authStore.loginLoading" @click="handleLogin">
              立即登录
            </NButton>
          </NForm>

          <!-- 其他登录方式 -->
          <div class="mt-4 grid grid-cols-2 gap-2">
            <NButton quaternary>
              手机登录
            </NButton>
            <NButton quaternary>
              扫码登录
            </NButton>
          </div>

          <NDivider
            :class="isDark ? '!my-5 !border-white/10' : '!my-5 !border-[hsl(var(--border))]'"
          >
            第三方登录
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
            还没有账号?
            <a class="link-primary" href="#">立即注册</a>
          </p>
        </div>
      </div>
    </div>
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

