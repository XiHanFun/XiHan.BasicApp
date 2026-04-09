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
  useDialog,
  useMessage,
} from 'naive-ui'
import { computed, h, ref, watch } from 'vue'
import {
  changeUserNameApi,
  confirmChangeEmailApi,
  confirmChangePhoneApi,
  sendChangeEmailCodeApi,
  sendChangePhoneCodeApi,
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
const dialog = useDialog()

// ==================== 用户名修改 ====================

const usernameChangeLoading = ref(false)
const newUserNameInput = ref('')
const newUserNamePassword = ref('')

const usernameHint = computed(() => {
  if (!props.profile)
    return ''
  if (props.profile.isSystemAccount)
    return '系统内置账号不可修改'
  if (props.profile.lastUserNameChangeTime && !props.profile.canChangeUserName) {
    const next = new Date(props.profile.lastUserNameChangeTime)
    next.setDate(next.getDate() + 90)
    const remaining = Math.ceil((next.getTime() - Date.now()) / (1000 * 60 * 60 * 24))
    return `还需等待 ${remaining} 天后可修改`
  }
  return ''
})

function handleChangeUserName() {
  newUserNameInput.value = props.profile?.userName ?? ''
  newUserNamePassword.value = ''
  dialog.create({
    title: '修改用户名',
    content: () => h('div', { style: 'display:flex;flex-direction:column;gap:12px' }, [
      h('p', { style: 'margin:0;color:var(--text-secondary);font-size:13px' }, '修改后 90 天内不可再次修改，请谨慎操作。'),
      h(NInput, {
        'value': newUserNameInput.value,
        'placeholder': '新用户名（3~30位，字母/数字/下划线）',
        'onUpdate:value': (v: string) => {
          newUserNameInput.value = v
        },
      }),
      h(NInput, {
        'type': 'password',
        'value': newUserNamePassword.value,
        'placeholder': '输入当前密码确认身份',
        'showPasswordOn': 'click',
        'onUpdate:value': (v: string) => {
          newUserNamePassword.value = v
        },
      }),
    ]),
    positiveText: '确认修改',
    negativeText: '取消',
    onPositiveClick: async () => {
      if (!newUserNameInput.value.trim()) {
        message.warning('请输入用户名')
        return false
      }
      if (!newUserNamePassword.value) {
        message.warning('请输入密码')
        return false
      }
      usernameChangeLoading.value = true
      try {
        await changeUserNameApi({
          userName: newUserNameInput.value.trim(),
          password: newUserNamePassword.value,
        })
        message.success('用户名已修改')
        emit('saved')
      }
      catch (e: unknown) {
        message.error((e as Error)?.message || '修改失败')
        return false
      }
      finally {
        usernameChangeLoading.value = false
      }
    },
  })
}

// ==================== 表单 ====================

const profileSaving = ref(false)
const profileFormRef = ref<FormInst | null>(null)

const profileForm = ref({
  nickName: '',
  realName: '',
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

// ==================== 邮箱/手机 通用状态 ====================

type ContactTarget = 'email' | 'phone'

// 验证当前地址
const verifyLoading = ref(false)
const verifyTarget = ref<ContactTarget | null>(null)
const verifyCode = ref('')
const verifyCountdown = ref(0)
let verifyTimer: ReturnType<typeof setInterval> | null = null

// 换绑新地址
const changeTarget = ref<ContactTarget | null>(null)
const changeNewValue = ref('')
const changePassword = ref('')
const changeLoading = ref(false)
const changeCodeSent = ref(false)
const changeCode = ref('')
const changeCountdown = ref(0)
let changeTimer: ReturnType<typeof setInterval> | null = null

function startTimer(
  countdownRef: { value: number },
  timerSetter: (t: ReturnType<typeof setInterval> | null) => void,
  seconds: number,
) {
  countdownRef.value = seconds
  const t = setInterval(() => {
    countdownRef.value--
    if (countdownRef.value <= 0) {
      clearInterval(t)
      timerSetter(null)
    }
  }, 1000)
  timerSetter(t)
}

// ==================== 验证当前邮箱/手机 ====================

async function sendVerifyCode(type: ContactTarget) {
  verifyLoading.value = true
  try {
    const res = type === 'email'
      ? await sendEmailVerifyCodeApi()
      : await sendPhoneVerifyCodeApi()
    message.success(type === 'email' ? '验证码已发送至邮箱' : '验证码已发送至手机')
    verifyTarget.value = type
    verifyCode.value = ''
    startTimer(
      verifyCountdown,
      t => (verifyTimer = t),
      Math.min(res.expiresInSeconds, 60),
    )
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '发送失败')
  }
  finally {
    verifyLoading.value = false
  }
}

async function confirmVerify() {
  if (verifyCode.value.length < 6) {
    message.warning('请输入完整的 6 位验证码')
    return
  }
  verifyLoading.value = true
  try {
    if (verifyTarget.value === 'email')
      await verifyEmailApi(verifyCode.value)
    else
      await verifyPhoneApi(verifyCode.value)
    message.success('验证成功')
    cancelVerify()
    emit('saved')
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '验证失败')
  }
  finally {
    verifyLoading.value = false
  }
}

function cancelVerify() {
  verifyTarget.value = null
  verifyCode.value = ''
  verifyCountdown.value = 0
  if (verifyTimer) {
    clearInterval(verifyTimer)
    verifyTimer = null
  }
}

// ==================== 换绑邮箱/手机 ====================

function openChangeDialog(type: ContactTarget) {
  changeTarget.value = type
  changeNewValue.value = ''
  changePassword.value = ''
  changeCodeSent.value = false
  changeCode.value = ''
  changeCountdown.value = 0
  if (changeTimer) {
    clearInterval(changeTimer)
    changeTimer = null
  }
}

async function sendChangeCode() {
  if (!changeNewValue.value.trim()) {
    message.warning(changeTarget.value === 'email' ? '请输入新邮箱' : '请输入新手机号')
    return
  }
  if (!changePassword.value) {
    message.warning('请输入当前密码')
    return
  }
  changeLoading.value = true
  try {
    const res = changeTarget.value === 'email'
      ? await sendChangeEmailCodeApi({
          newEmail: changeNewValue.value.trim(),
          password: changePassword.value,
        })
      : await sendChangePhoneCodeApi({
          newPhone: changeNewValue.value.trim(),
          password: changePassword.value,
        })
    message.success('验证码已发送')
    changeCodeSent.value = true
    changeCode.value = ''
    startTimer(
      changeCountdown,
      t => (changeTimer = t),
      Math.min(res.expiresInSeconds, 60),
    )
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '发送失败')
  }
  finally {
    changeLoading.value = false
  }
}

async function confirmChange() {
  if (changeCode.value.length < 6) {
    message.warning('请输入完整的 6 位验证码')
    return
  }
  changeLoading.value = true
  try {
    if (changeTarget.value === 'email')
      await confirmChangeEmailApi(changeCode.value)
    else
      await confirmChangePhoneApi(changeCode.value)
    message.success(changeTarget.value === 'email' ? '邮箱已更新' : '手机号已更新')
    cancelChange()
    emit('saved')
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '操作失败')
  }
  finally {
    changeLoading.value = false
  }
}

function cancelChange() {
  changeTarget.value = null
  changeNewValue.value = ''
  changePassword.value = ''
  changeCodeSent.value = false
  changeCode.value = ''
  changeCountdown.value = 0
  if (changeTimer) {
    clearInterval(changeTimer)
    changeTimer = null
  }
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
                  <NInputGroup>
                    <NInput :value="profile?.userName" disabled placeholder="---">
                      <template #suffix>
                        <NTag v-if="profile?.isSystemAccount" type="info" size="tiny" :bordered="false">
                          系统账号
                        </NTag>
                        <NTag v-else-if="!profile?.canChangeUserName && usernameHint" type="warning" size="tiny" :bordered="false">
                          {{ usernameHint }}
                        </NTag>
                      </template>
                    </NInput>
                    <NButton
                      v-if="profile?.canChangeUserName"
                      type="primary"
                      ghost
                      :loading="usernameChangeLoading"
                      @click="handleChangeUserName"
                    >
                      修改
                    </NButton>
                  </NInputGroup>
                </NFormItem>
              </NGridItem>
              <NGridItem>
                <NFormItem label="显示名称" path="nickName">
                  <NInput v-model:value="profileForm.nickName" placeholder="您的昵称" />
                </NFormItem>
              </NGridItem>
              <!-- 邮箱 -->
              <NGridItem>
                <NFormItem label="电子邮箱">
                  <div class="pf-full">
                    <NInputGroup>
                      <NInput :value="profile?.email || '未设置'" disabled>
                        <template #suffix>
                          <NTag v-if="profile?.emailVerified" type="success" size="tiny" :bordered="false">
                            已验证
                          </NTag>
                          <NTag v-else-if="profile?.email" type="warning" size="tiny" :bordered="false">
                            未验证
                          </NTag>
                        </template>
                      </NInput>
                      <NButton type="primary" ghost @click="openChangeDialog('email')">
                        {{ profile?.email ? '修改' : '绑定' }}
                      </NButton>
                      <NButton
                        v-if="profile?.email && !profile?.emailVerified"
                        quaternary
                        :loading="verifyLoading && verifyTarget === 'email'"
                        @click="sendVerifyCode('email')"
                      >
                        验证
                      </NButton>
                    </NInputGroup>
                    <!-- 验证当前邮箱的验证码输入行 -->
                    <div v-if="verifyTarget === 'email'" class="pf-verify-row">
                      <NInput v-model:value="verifyCode" placeholder="请输入 6 位验证码" :maxlength="6" />
                      <NButton type="primary" :loading="verifyLoading" :disabled="verifyCode.length < 6" @click="confirmVerify">
                        确认
                      </NButton>
                      <NButton :disabled="verifyCountdown > 0" quaternary @click="sendVerifyCode('email')">
                        {{ verifyCountdown > 0 ? `${verifyCountdown}s` : '重发' }}
                      </NButton>
                      <NButton quaternary @click="cancelVerify">
                        取消
                      </NButton>
                    </div>
                  </div>
                </NFormItem>
              </NGridItem>

              <!-- 手机 -->
              <NGridItem>
                <NFormItem label="手机号码">
                  <div class="pf-full">
                    <NInputGroup>
                      <NInput :value="profile?.phone || '未设置'" disabled>
                        <template #suffix>
                          <NTag v-if="profile?.phoneVerified" type="success" size="tiny" :bordered="false">
                            已验证
                          </NTag>
                          <NTag v-else-if="profile?.phone" type="warning" size="tiny" :bordered="false">
                            未验证
                          </NTag>
                        </template>
                      </NInput>
                      <NButton type="primary" ghost @click="openChangeDialog('phone')">
                        {{ profile?.phone ? '修改' : '绑定' }}
                      </NButton>
                      <NButton
                        v-if="profile?.phone && !profile?.phoneVerified"
                        quaternary
                        :loading="verifyLoading && verifyTarget === 'phone'"
                        @click="sendVerifyCode('phone')"
                      >
                        验证
                      </NButton>
                    </NInputGroup>
                    <div v-if="verifyTarget === 'phone'" class="pf-verify-row">
                      <NInput v-model:value="verifyCode" placeholder="请输入 6 位验证码" :maxlength="6" />
                      <NButton type="primary" :loading="verifyLoading" :disabled="verifyCode.length < 6" @click="confirmVerify">
                        确认
                      </NButton>
                      <NButton :disabled="verifyCountdown > 0" quaternary @click="sendVerifyCode('phone')">
                        {{ verifyCountdown > 0 ? `${verifyCountdown}s` : '重发' }}
                      </NButton>
                      <NButton quaternary @click="cancelVerify">
                        取消
                      </NButton>
                    </div>
                  </div>
                </NFormItem>
              </NGridItem>

              <!-- 换绑对话框（邮箱/手机共用） -->
              <Teleport to="body">
                <div v-if="changeTarget" class="pf-change-overlay" @click.self="cancelChange">
                  <NCard
                    class="pf-change-dialog"
                    :title="changeTarget === 'email' ? '修改邮箱' : '修改手机号'"
                    size="small"
                    closable
                    @close="cancelChange"
                  >
                    <div class="pf-change-body">
                      <template v-if="!changeCodeSent">
                        <NInput
                          v-model:value="changeNewValue"
                          :placeholder="changeTarget === 'email' ? '新邮箱地址' : '新手机号'"
                        />
                        <NInput
                          v-model:value="changePassword"
                          type="password"
                          placeholder="输入当前密码确认身份"
                          show-password-on="click"
                        />
                        <NButton type="primary" block :loading="changeLoading" @click="sendChangeCode">
                          发送验证码
                        </NButton>
                      </template>
                      <template v-else>
                        <p class="pf-change-hint">
                          验证码已发送至 <strong>{{ changeNewValue }}</strong>
                        </p>
                        <NInput
                          v-model:value="changeCode"
                          placeholder="请输入 6 位验证码"
                          :maxlength="6"
                        />
                        <NSpace :size="8">
                          <NButton type="primary" :loading="changeLoading" :disabled="changeCode.length < 6" @click="confirmChange">
                            确认
                          </NButton>
                          <NButton :disabled="changeCountdown > 0" quaternary @click="sendChangeCode">
                            {{ changeCountdown > 0 ? `${changeCountdown}s 后重发` : '重新发送' }}
                          </NButton>
                        </NSpace>
                      </template>
                    </div>
                  </NCard>
                </div>
              </Teleport>
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

.pf-change-overlay {
  position: fixed;
  inset: 0;
  z-index: 2000;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgb(0 0 0 / 0.35);
}

.pf-change-dialog {
  width: 380px;
  max-width: 90vw;
}

.pf-change-body {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.pf-change-hint {
  margin: 0;
  font-size: 13px;
  color: var(--text-secondary);
}
</style>
