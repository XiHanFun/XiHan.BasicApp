<script lang="ts" setup>
import { ref, computed } from 'vue'
import {
  NCard,
  NAvatar,
  NTabs,
  NTabPane,
  NForm,
  NFormItem,
  NInput,
  NButton,
  NSpace,
  NTag,
  useMessage,
} from 'naive-ui'
import type { FormInst } from 'naive-ui'
import { Icon } from '@iconify/vue'
import { useUserStore } from '~/stores'

defineOptions({ name: 'ProfilePage' })

const userStore = useUserStore()
const message = useMessage()

const baseFormRef = ref<FormInst | null>(null)
const pwdFormRef = ref<FormInst | null>(null)

const baseForm = ref({
  nickname: userStore.nickname,
  email: userStore.userInfo?.email ?? '',
  phone: userStore.userInfo?.phone ?? '',
})

const pwdForm = ref({
  oldPassword: '',
  newPassword: '',
  confirmPassword: '',
})

const pwdRules = {
  oldPassword: [{ required: true, message: '请输入旧密码', trigger: 'blur' }],
  newPassword: [
    { required: true, message: '请输入新密码', trigger: 'blur' },
    { min: 6, message: '密码至少 6 位', trigger: 'blur' },
  ],
  confirmPassword: [
    { required: true, message: '请确认新密码', trigger: 'blur' },
    {
      validator: (_rule: any, value: string) => value === pwdForm.value.newPassword,
      message: '两次输入密码不一致',
      trigger: 'blur',
    },
  ],
}

async function saveBaseInfo() {
  await baseFormRef.value?.validate()
  message.success('基本信息保存成功')
}

async function changePassword() {
  await pwdFormRef.value?.validate()
  message.success('密码修改成功，请重新登录')
}
</script>

<template>
  <div class="mx-auto max-w-3xl">
    <!-- 用户信息卡片 -->
    <NCard class="mb-4">
      <div class="flex items-center gap-6">
        <NAvatar
          round
          :size="80"
          :src="userStore.avatar"
          :fallback-src="`https://api.dicebear.com/9.x/initials/svg?seed=${userStore.nickname}`"
        />
        <div class="flex-1">
          <h2 class="text-xl font-semibold">{{ userStore.nickname }}</h2>
          <p class="mt-1 text-sm text-gray-500">@{{ userStore.username }}</p>
          <div class="mt-2 flex flex-wrap gap-2">
            <NTag v-for="role in userStore.roles" :key="role" type="primary" size="small">
              {{ role }}
            </NTag>
          </div>
        </div>
      </div>
    </NCard>

    <!-- 选项卡 -->
    <NCard>
      <NTabs type="line" animated>
        <!-- 基本信息 -->
        <NTabPane name="base" tab="基本信息">
          <NForm
            ref="baseFormRef"
            :model="baseForm"
            label-placement="left"
            label-width="80px"
            class="mt-4 max-w-md"
          >
            <NFormItem label="昵称" path="nickname">
              <NInput v-model:value="baseForm.nickname" placeholder="请输入昵称" />
            </NFormItem>
            <NFormItem label="邮箱" path="email">
              <NInput v-model:value="baseForm.email" placeholder="请输入邮箱" />
            </NFormItem>
            <NFormItem label="手机号" path="phone">
              <NInput v-model:value="baseForm.phone" placeholder="请输入手机号" />
            </NFormItem>
            <NFormItem>
              <NButton type="primary" @click="saveBaseInfo">保存修改</NButton>
            </NFormItem>
          </NForm>
        </NTabPane>

        <!-- 修改密码 -->
        <NTabPane name="password" tab="修改密码">
          <NForm
            ref="pwdFormRef"
            :model="pwdForm"
            :rules="pwdRules"
            label-placement="left"
            label-width="80px"
            class="mt-4 max-w-md"
          >
            <NFormItem label="旧密码" path="oldPassword">
              <NInput
                v-model:value="pwdForm.oldPassword"
                type="password"
                placeholder="请输入旧密码"
                show-password-on="click"
              />
            </NFormItem>
            <NFormItem label="新密码" path="newPassword">
              <NInput
                v-model:value="pwdForm.newPassword"
                type="password"
                placeholder="请输入新密码（6~32位）"
                show-password-on="click"
              />
            </NFormItem>
            <NFormItem label="确认密码" path="confirmPassword">
              <NInput
                v-model:value="pwdForm.confirmPassword"
                type="password"
                placeholder="请再次输入新密码"
                show-password-on="click"
              />
            </NFormItem>
            <NFormItem>
              <NButton type="primary" @click="changePassword">修改密码</NButton>
            </NFormItem>
          </NForm>
        </NTabPane>
      </NTabs>
    </NCard>
  </div>
</template>
