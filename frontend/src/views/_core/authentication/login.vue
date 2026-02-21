<script lang="ts" setup>
import { ref, computed } from 'vue'
import { useRoute } from 'vue-router'
import {
  NCard,
  NForm,
  NFormItem,
  NInput,
  NButton,
  NCheckbox,
  NDivider,
  NIcon,
  useMessage,
} from 'naive-ui'
import type { FormInst, FormRules } from 'naive-ui'
import { Icon } from '@iconify/vue'
import { useAuthStore } from '@/store/auth'
import { useTheme } from '~/hooks'

defineOptions({ name: 'LoginPage' })

const route = useRoute()
const authStore = useAuthStore()
const message = useMessage()
const { isDark, toggleTheme } = useTheme()

const formRef = ref<FormInst | null>(null)
const rememberMe = ref(false)
const showPassword = ref(false)

const formData = ref({
  username: 'superadmin',
  password: 'Admin@123',
})

const rules: FormRules = {
  username: [
    { required: true, message: '请输入用户名', trigger: 'blur' },
    { min: 2, max: 32, message: '用户名长度为 2~32 个字符', trigger: 'blur' },
  ],
  password: [
    { required: true, message: '请输入密码', trigger: 'blur' },
    { min: 6, max: 32, message: '密码长度为 6~32 个字符', trigger: 'blur' },
  ],
}

const redirect = computed(() => {
  const r = route.query.redirect as string
  return r ? decodeURIComponent(r) : undefined
})

async function handleLogin() {
  try {
    await formRef.value?.validate()
    await authStore.login(formData.value, redirect.value)
  } catch (err: any) {
    if (err?.message) {
      message.error(err.message)
    }
  }
}

function handleKeydown(e: KeyboardEvent) {
  if (e.key === 'Enter') handleLogin()
}
</script>

<template>
  <div class="flex min-h-screen bg-gradient-to-br from-green-50 to-blue-50 dark:from-gray-900 dark:to-gray-800">
    <!-- 左侧装饰区 -->
    <div class="hidden flex-1 flex-col items-center justify-center p-12 lg:flex">
      <div class="mb-8 flex items-center gap-3">
        <div class="flex h-12 w-12 items-center justify-center rounded-2xl bg-primary-600 shadow-lg">
          <Icon icon="lucide:zap" class="text-white" width="28" />
        </div>
        <span class="text-3xl font-bold text-gray-800 dark:text-white">XiHan Admin</span>
      </div>
      <h2 class="mb-4 text-center text-2xl font-semibold text-gray-700 dark:text-gray-200">
        现代化后台管理系统
      </h2>
      <p class="max-w-md text-center text-gray-500 dark:text-gray-400">
        基于 Vue 3 + Naive UI 构建，提供优雅的管理界面和强大的功能支持。
      </p>

      <!-- 装饰图形 -->
      <div class="mt-12 grid grid-cols-3 gap-4 opacity-30">
        <div v-for="i in 9" :key="i" class="h-16 w-16 rounded-xl bg-primary-600" :class="[i % 3 === 0 ? 'opacity-20' : i % 2 === 0 ? 'opacity-60' : 'opacity-40']" />
      </div>
    </div>

    <!-- 右侧登录表单 -->
    <div class="flex w-full items-center justify-center p-6 lg:w-[480px] lg:border-l lg:border-gray-200 lg:bg-white lg:dark:border-gray-700 lg:dark:bg-gray-900">
      <div class="w-full max-w-sm">
        <!-- 移动端 Logo -->
        <div class="mb-8 flex items-center justify-center gap-3 lg:hidden">
          <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-primary-600">
            <Icon icon="lucide:zap" class="text-white" width="22" />
          </div>
          <span class="text-2xl font-bold text-gray-800 dark:text-white">XiHan Admin</span>
        </div>

        <div class="mb-8">
          <h1 class="text-2xl font-bold text-gray-900 dark:text-white">欢迎登录</h1>
          <p class="mt-2 text-sm text-gray-500 dark:text-gray-400">请输入您的账号信息</p>
        </div>

        <NForm
          ref="formRef"
          :model="formData"
          :rules="rules"
          label-placement="top"
          size="large"
          @keydown="handleKeydown"
        >
          <NFormItem label="用户名" path="username">
            <NInput
              v-model:value="formData.username"
              placeholder="请输入用户名"
              :input-props="{ autocomplete: 'username' }"
              clearable
            >
              <template #prefix>
                <NIcon>
                  <Icon icon="lucide:user" width="16" />
                </NIcon>
              </template>
            </NInput>
          </NFormItem>

          <NFormItem label="密码" path="password">
            <NInput
              v-model:value="formData.password"
              :type="showPassword ? 'text' : 'password'"
              placeholder="请输入密码"
              :input-props="{ autocomplete: 'current-password' }"
            >
              <template #prefix>
                <NIcon>
                  <Icon icon="lucide:lock" width="16" />
                </NIcon>
              </template>
              <template #suffix>
                <NIcon
                  class="cursor-pointer text-gray-400 hover:text-gray-600"
                  @click="showPassword = !showPassword"
                >
                  <Icon :icon="showPassword ? 'lucide:eye-off' : 'lucide:eye'" width="16" />
                </NIcon>
              </template>
            </NInput>
          </NFormItem>

          <div class="mb-4 flex items-center justify-between">
            <NCheckbox v-model:checked="rememberMe">记住我</NCheckbox>
            <a class="text-sm text-primary-600 hover:underline" href="#">忘记密码？</a>
          </div>

          <NButton
            type="primary"
            block
            :loading="authStore.loginLoading"
            @click="handleLogin"
          >
            登录
          </NButton>
        </NForm>

        <!-- 主题切换 -->
        <div class="mt-6 flex justify-center">
          <NButton quaternary circle @click="toggleTheme">
            <template #icon>
              <NIcon>
                <Icon :icon="isDark ? 'lucide:sun' : 'lucide:moon'" width="18" />
              </NIcon>
            </template>
          </NButton>
        </div>

        <p class="mt-6 text-center text-xs text-gray-400">
          Copyright © {{ new Date().getFullYear() }} XiHan. All rights reserved.
        </p>
      </div>
    </div>
  </div>
</template>
