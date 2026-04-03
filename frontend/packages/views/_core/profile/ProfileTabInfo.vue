<script lang="ts" setup>
import type { FormInst } from 'naive-ui'
import type { UserProfile } from '~/types'
import {
  NAvatar,
  NButton,
  NCard,
  NForm,
  NFormItem,
  NGrid,
  NGridItem,
  NIcon,
  NInput,
  NInputGroup,
  NSelect,
  NSpace,
  NTag,
  useMessage,
} from 'naive-ui'
import { ref, watch } from 'vue'
import {
  sendEmailVerifyCodeApi,
  sendPhoneVerifyCodeApi,
  updateProfileApi,
  verifyEmailApi,
  verifyPhoneApi,
} from '@/api'
import { Icon } from '~/iconify'
import { useUserStore } from '~/stores'

const props = defineProps<{ profile: UserProfile | null }>()
const emit = defineEmits<{ saved: [] }>()

const userStore = useUserStore()
const message = useMessage()

// ==================== 表单 ====================

const profileSaving = ref(false)
const profileFormRef = ref<FormInst | null>(null)

const profileForm = ref({
  nickName: '',
  realName: '',
  email: '',
  phone: '',
  gender: 0 as number,
  birthday: null as null | number,
  country: '',
  remark: '',
  language: 'zh-CN',
  timeZone: '',
})

const genderOptions = [
  { label: '未设置', value: 0 },
  { label: '男', value: 1 },
  { label: '女', value: 2 },
]
const languageOptions = [
  { label: '简体中文', value: 'zh-CN' },
  { label: 'English', value: 'en-US' },
]
const timezoneOptions = [
  { label: 'UTC+8 北京时间', value: 'Asia/Shanghai' },
  { label: 'UTC+9 东京时间', value: 'Asia/Tokyo' },
  { label: 'UTC+0 格林尼治时间', value: 'UTC' },
  { label: 'UTC-5 美东时间', value: 'America/New_York' },
  { label: 'UTC-8 美西时间', value: 'America/Los_Angeles' },
  { label: 'UTC+1 中欧时间', value: 'Europe/Berlin' },
]

function syncProfileForm() {
  if (!props.profile)
    return
  const p = props.profile
  profileForm.value = {
    nickName: p.nickName ?? '',
    realName: p.realName ?? '',
    email: p.email ?? '',
    phone: p.phone ?? '',
    gender: p.gender ?? 0,
    birthday: p.birthday ? new Date(p.birthday).getTime() : null,
    country: p.country ?? '',
    remark: p.remark ?? '',
    language: p.language ?? 'zh-CN',
    timeZone: p.timeZone ?? '',
  }
}

watch(() => props.profile, syncProfileForm, { immediate: true })

async function saveProfile() {
  await profileFormRef.value?.validate()
  profileSaving.value = true
  try {
    await updateProfileApi({
      ...profileForm.value,
      birthday: profileForm.value.birthday ? new Date(profileForm.value.birthday).toISOString() : undefined,
    })
    message.success('个人资料已更新')
    if (userStore.userInfo) {
      userStore.setUserInfo({
        ...userStore.userInfo,
        nickName: profileForm.value.nickName,
        email: profileForm.value.email,
        phone: profileForm.value.phone,
      })
    }
    emit('saved')
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '保存失败')
  }
  finally {
    profileSaving.value = false
  }
}

// ==================== 邮箱验证 ====================

const emailVerifying = ref(false)
const emailVerifyCode = ref('')
const emailCodeSending = ref(false)
const emailCodeCountdown = ref(0)
let emailTimer: ReturnType<typeof setInterval> | null = null

// ==================== 手机验证 ====================

const phoneVerifying = ref(false)
const phoneVerifyCode = ref('')
const phoneCodeSending = ref(false)
const phoneCodeCountdown = ref(0)
let phoneTimer: ReturnType<typeof setInterval> | null = null

// ==================== 通用倒计时 ====================

function startCountdown(type: 'email' | 'phone', seconds: number) {
  const countdownRef = type === 'email' ? emailCodeCountdown : phoneCodeCountdown
  countdownRef.value = seconds
  const timer = setInterval(() => {
    countdownRef.value--
    if (countdownRef.value <= 0) {
      clearInterval(timer)
      if (type === 'email')
        emailTimer = null
      else
        phoneTimer = null
    }
  }, 1000)
  if (type === 'email')
    emailTimer = timer
  else
    phoneTimer = timer
}

// ==================== 邮箱操作 ====================

async function sendEmailCode() {
  if (!profileForm.value.email) {
    message.warning('请先填写邮箱地址')
    return
  }
  emailCodeSending.value = true
  try {
    const res = await sendEmailVerifyCodeApi()
    message.success('验证码已发送至邮箱')
    emailVerifying.value = true
    startCountdown('email', res.expiresInSeconds > 60 ? 60 : res.expiresInSeconds)
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '发送验证码失败')
  }
  finally {
    emailCodeSending.value = false
  }
}

async function confirmEmailVerify() {
  if (!emailVerifyCode.value || emailVerifyCode.value.length < 6) {
    message.warning('请输入完整的 6 位验证码')
    return
  }
  emailCodeSending.value = true
  try {
    await verifyEmailApi(emailVerifyCode.value)
    message.success('邮箱验证成功')
    emailVerifying.value = false
    emailVerifyCode.value = ''
    emit('saved')
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '验证失败')
  }
  finally {
    emailCodeSending.value = false
  }
}

function cancelEmailVerify() {
  emailVerifying.value = false
  emailVerifyCode.value = ''
  if (emailTimer) {
    clearInterval(emailTimer)
    emailTimer = null
  }
  emailCodeCountdown.value = 0
}

// ==================== 手机操作 ====================

async function sendPhoneCode() {
  if (!profileForm.value.phone) {
    message.warning('请先填写手机号码')
    return
  }
  phoneCodeSending.value = true
  try {
    const res = await sendPhoneVerifyCodeApi()
    message.success('验证码已发送至手机')
    phoneVerifying.value = true
    startCountdown('phone', res.expiresInSeconds > 60 ? 60 : res.expiresInSeconds)
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '发送验证码失败')
  }
  finally {
    phoneCodeSending.value = false
  }
}

async function confirmPhoneVerify() {
  if (!phoneVerifyCode.value || phoneVerifyCode.value.length < 6) {
    message.warning('请输入完整的 6 位验证码')
    return
  }
  phoneCodeSending.value = true
  try {
    await verifyPhoneApi(phoneVerifyCode.value)
    message.success('手机号验证成功')
    phoneVerifying.value = false
    phoneVerifyCode.value = ''
    emit('saved')
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '验证失败')
  }
  finally {
    phoneCodeSending.value = false
  }
}

function cancelPhoneVerify() {
  phoneVerifying.value = false
  phoneVerifyCode.value = ''
  if (phoneTimer) {
    clearInterval(phoneTimer)
    phoneTimer = null
  }
  phoneCodeCountdown.value = 0
}
</script>

<template>
  <div class="pf-tab-body">
    <NGrid cols="1 m:2" responsive="screen" :x-gap="12" :y-gap="12">
      <NGridItem :span="2">
        <NCard :bordered="false" size="small" class="pf-card">
          <template #header>
            <div class="pf-card-header">
              <Icon icon="lucide:image" width="16" />
              <span>头像</span>
            </div>
          </template>
          <div class="pf-avatar-section">
            <NAvatar
              round :size="56"
              :src="profile?.avatar || userStore.avatar"
              :fallback-src="`https://api.dicebear.com/9.x/initials/svg?seed=${userStore.nickname}`"
            />
            <div class="pf-avatar-info">
              <span class="pf-avatar-hint">支持 JPG、PNG，不超过 2MB</span>
            </div>
            <NSpace :size="8">
              <NButton size="small" type="primary">
                上传
              </NButton>
              <NButton size="small" quaternary>
                删除
              </NButton>
            </NSpace>
          </div>
        </NCard>
      </NGridItem>
      <NGridItem :span="2">
        <NCard :bordered="false" size="small" class="pf-card">
          <template #header>
            <div class="pf-card-header">
              <Icon icon="lucide:contact" width="16" />
              <span>基本信息</span>
            </div>
          </template>
          <NForm ref="profileFormRef" :model="profileForm" label-placement="top">
            <NGrid cols="1 m:2" responsive="screen" :x-gap="16" :y-gap="0">
              <NGridItem>
                <NFormItem label="用户名">
                  <NInput :value="profile?.userName" disabled placeholder="---">
                    <template #suffix>
                      <NTag size="tiny" :bordered="false">
                        不可修改
                      </NTag>
                    </template>
                  </NInput>
                </NFormItem>
              </NGridItem>
              <NGridItem>
                <NFormItem label="显示名称" path="nickName">
                  <NInput v-model:value="profileForm.nickName" placeholder="您的昵称" />
                </NFormItem>
              </NGridItem>
              <NGridItem>
                <NFormItem label="电子邮箱" path="email">
                  <div class="pf-full">
                    <NInputGroup>
                      <NInput v-model:value="profileForm.email" placeholder="your@email.com">
                        <template #suffix>
                          <NTag v-if="profile?.emailVerified" type="success" size="tiny" :bordered="false">
                            已验证
                          </NTag>
                          <NTag v-else-if="profile?.email" type="warning" size="tiny" :bordered="false">
                            未验证
                          </NTag>
                        </template>
                      </NInput>
                      <NButton
                        v-if="profile?.email && !profile?.emailVerified && !emailVerifying"
                        type="primary" ghost
                        :loading="emailCodeSending"
                        @click="sendEmailCode"
                      >
                        发送验证码
                      </NButton>
                    </NInputGroup>
                    <div v-if="emailVerifying" class="pf-verify-row">
                      <NInput v-model:value="emailVerifyCode" placeholder="请输入 6 位验证码" :maxlength="6" />
                      <NButton type="primary" :loading="emailCodeSending" :disabled="emailVerifyCode.length < 6" @click="confirmEmailVerify">
                        验证
                      </NButton>
                      <NButton
                        :disabled="emailCodeCountdown > 0"
                        :loading="emailCodeSending"
                        quaternary
                        @click="sendEmailCode"
                      >
                        {{ emailCodeCountdown > 0 ? `${emailCodeCountdown}s` : '重新发送' }}
                      </NButton>
                      <NButton quaternary @click="cancelEmailVerify">
                        取消
                      </NButton>
                    </div>
                  </div>
                </NFormItem>
              </NGridItem>
              <NGridItem>
                <NFormItem label="手机号码" path="phone">
                  <div class="pf-full">
                    <NInputGroup>
                      <NInput v-model:value="profileForm.phone" placeholder="您的手机号">
                        <template #suffix>
                          <NTag v-if="profile?.phoneVerified" type="success" size="tiny" :bordered="false">
                            已验证
                          </NTag>
                          <NTag v-else-if="profile?.phone" type="warning" size="tiny" :bordered="false">
                            未验证
                          </NTag>
                        </template>
                      </NInput>
                      <NButton
                        v-if="profile?.phone && !profile?.phoneVerified && !phoneVerifying"
                        type="primary" ghost
                        :loading="phoneCodeSending"
                        @click="sendPhoneCode"
                      >
                        发送验证码
                      </NButton>
                    </NInputGroup>
                    <div v-if="phoneVerifying" class="pf-verify-row">
                      <NInput v-model:value="phoneVerifyCode" placeholder="请输入 6 位验证码" :maxlength="6" />
                      <NButton type="primary" :loading="phoneCodeSending" :disabled="phoneVerifyCode.length < 6" @click="confirmPhoneVerify">
                        验证
                      </NButton>
                      <NButton
                        :disabled="phoneCodeCountdown > 0"
                        :loading="phoneCodeSending"
                        quaternary
                        @click="sendPhoneCode"
                      >
                        {{ phoneCodeCountdown > 0 ? `${phoneCodeCountdown}s` : '重新发送' }}
                      </NButton>
                      <NButton quaternary @click="cancelPhoneVerify">
                        取消
                      </NButton>
                    </div>
                  </div>
                </NFormItem>
              </NGridItem>
              <NGridItem>
                <NFormItem label="性别">
                  <NSelect v-model:value="profileForm.gender" :options="genderOptions" />
                </NFormItem>
              </NGridItem>
              <NGridItem>
                <NFormItem label="国家 / 地区">
                  <NInput v-model:value="profileForm.country" placeholder="例如：中国" />
                </NFormItem>
              </NGridItem>
              <NGridItem>
                <NFormItem label="语言">
                  <NSelect v-model:value="profileForm.language" :options="languageOptions" />
                </NFormItem>
              </NGridItem>
              <NGridItem>
                <NFormItem label="时区">
                  <NSelect v-model:value="profileForm.timeZone" :options="timezoneOptions" />
                </NFormItem>
              </NGridItem>
              <NGridItem :span="2">
                <NFormItem label="个人简介">
                  <NInput v-model:value="profileForm.remark" type="textarea" placeholder="介绍一下你自己..." :autosize="{ minRows: 3, maxRows: 6 }" :maxlength="200" show-count />
                </NFormItem>
              </NGridItem>
            </NGrid>
          </NForm>
          <template #action>
            <div class="pf-card-actions">
              <NButton @click="syncProfileForm">
                取消
              </NButton>
              <NButton type="primary" :loading="profileSaving" @click="saveProfile">
                <template #icon>
                  <NIcon>
                    <Icon icon="lucide:save" />
                  </NIcon>
                </template>
                保存更改
              </NButton>
            </div>
          </template>
        </NCard>
      </NGridItem>
    </NGrid>
  </div>
</template>

<style src="./profile-shared.css" />

<style scoped>
.pf-avatar-section {
  display: flex;
  align-items: center;
  gap: 14px;
}

.pf-avatar-info {
  flex: 1;
  min-width: 0;
}

.pf-avatar-hint {
  font-size: 12px;
  color: var(--text-secondary);
}

.pf-verify-row {
  display: flex;
  align-items: center;
  gap: 6px;
  margin-top: 6px;
}
</style>
