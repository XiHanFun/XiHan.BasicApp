<script lang="ts" setup>
import type { UserProfile } from '~/types'
import {
  NButton,
  NCard,
  NIcon,
  NInput,
  NSelect,
  NSpace,
  NTag,
  useDialog,
  useMessage,
} from 'naive-ui'
import { computed, h, ref, watch } from 'vue'
import { fileApi, ResourceAccessLevel } from '@/api'
import { XUserAvatar } from '~/components'
import { Icon } from '~/iconify'
import { useAppContext, useUserStore } from '~/stores'

const props = defineProps<{ profile: UserProfile | null }>()
const emit = defineEmits<{ saved: [] }>()

const userStore = useUserStore()
const { apis } = useAppContext()
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
        await apis.changeUserNameApi({
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

// ==================== 头像上传 / 删除 ====================

const AVATAR_MAX_SIZE = 2 * 1024 * 1024 // 2MB
const AVATAR_ACCEPT = ['image/jpeg', 'image/png']

const avatarInputRef = ref<HTMLInputElement | null>(null)
const avatarUploading = ref(false)
const avatarRemoving = ref(false)

/** 头像原始值（fileId 或旧数据的直链），用于按钮禁用判断与持久化对比 */
const currentAvatar = computed(() => props.profile?.avatar || userStore.avatar)

/** 把头像写入资料并同步全局状态（与 profileForm 合并提交，避免覆盖其它字段） */
async function persistAvatar(avatar: string) {
  await apis.updateProfileApi({
    ...profileForm.value,
    avatar,
    birthday: profileForm.value.birthday
      ? new Date(profileForm.value.birthday).toISOString()
      : undefined,
  })
  if (userStore.userInfo) {
    userStore.setUserInfo({ ...userStore.userInfo, avatar })
  }
  emit('saved')
}

function triggerAvatarUpload() {
  avatarInputRef.value?.click()
}

async function handleAvatarChange(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  // 清空 input，保证再次选择同一文件也能触发 change
  input.value = ''
  if (!file) {
    return
  }
  if (!AVATAR_ACCEPT.includes(file.type)) {
    message.warning('仅支持 JPG、PNG 格式的图片')
    return
  }
  if (file.size > AVATAR_MAX_SIZE) {
    message.warning('图片大小不能超过 2MB')
    return
  }
  avatarUploading.value = true
  try {
    const detail = await fileApi.upload({
      file,
      accessLevel: ResourceAccessLevel.Public,
      directory: 'avatars',
    })
    // user.avatar 存文件主键(fileId)，显示时再换取预签名 URL
    await persistAvatar(detail.basicId)
    message.success('头像已更新')
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '头像上传失败')
  }
  finally {
    avatarUploading.value = false
  }
}

function handleAvatarRemove() {
  if (!currentAvatar.value) {
    message.warning('当前没有可删除的头像')
    return
  }
  dialog.warning({
    title: '删除头像',
    content: '确定要删除当前头像吗？删除后将显示默认头像。',
    positiveText: '确认删除',
    negativeText: '取消',
    onPositiveClick: async () => {
      avatarRemoving.value = true
      try {
        await persistAvatar('')
        message.success('头像已删除')
      }
      catch (e: unknown) {
        message.error((e as Error)?.message || '删除失败')
        return false
      }
      finally {
        avatarRemoving.value = false
      }
    },
  })
}

// ==================== 表单 ====================

const profileSaving = ref(false)

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
  profileSaving.value = true
  try {
    await apis.updateProfileApi({
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
      ? await apis.sendEmailVerifyCodeApi()
      : await apis.sendPhoneVerifyCodeApi()
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
      await apis.verifyEmailApi(verifyCode.value)
    else
      await apis.verifyPhoneApi(verifyCode.value)
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
      ? await apis.sendChangeEmailCodeApi({
          newEmail: changeNewValue.value.trim(),
          password: changePassword.value,
        })
      : await apis.sendChangePhoneCodeApi({
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
      await apis.confirmChangeEmailApi(changeCode.value)
    else
      await apis.confirmChangePhoneApi(changeCode.value)
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
    <!-- 头像 -->
    <!-- 账户资料：头像 + 账户标识 + 联系方式 合并为一卡 -->
    <section class="pf-section">
      <div class="pf-section__head">
        <div class="pf-section__heading">
          <div class="pf-section__title">
            <Icon icon="lucide:id-card" width="16" />
            <span>账户资料</span>
          </div>
          <div class="pf-section__desc">
            头像、账户标识与联系方式
          </div>
        </div>
      </div>
      <div class="pf-section__body">
        <div class="pf-setting-row">
          <div class="pf-avatar-section">
            <XUserAvatar
              :size="56"
              :avatar="currentAvatar"
              :name="userStore.nickname"
            />
            <div class="pf-avatar-info">
              <div class="pf-setting-row__label">
                个人头像
              </div>
              <div class="pf-setting-row__desc">
                支持 JPG、PNG，不超过 2MB，建议正方形图片
              </div>
            </div>
          </div>
          <input
            ref="avatarInputRef"
            type="file"
            accept="image/jpeg,image/png"
            style="display: none"
            @change="handleAvatarChange"
          >
          <div class="pf-setting-row__control">
            <NButton
              size="small"
              type="primary"
              :loading="avatarUploading"
              @click="triggerAvatarUpload"
            >
              上传
            </NButton>
            <NButton
              size="small"
              quaternary
              :loading="avatarRemoving"
              :disabled="!currentAvatar"
              @click="handleAvatarRemove"
            >
              删除
            </NButton>
          </div>
        </div>

        <!-- 账户标识 + 联系方式 两列 -->
        <div class="pf-cols-2">
          <div class="pf-subgroup">
            <div class="pf-subgroup__title">
              账户标识
            </div>
            <div class="pf-setting-list">
          <!-- 用户名 -->
          <div class="pf-setting-row">
            <div class="pf-setting-row__main">
              <div class="pf-setting-row__label">
                用户名
                <NTag v-if="profile?.isSystemAccount" type="info" size="tiny" :bordered="false">
                  系统账号
                </NTag>
                <NTag v-else-if="!profile?.canChangeUserName && usernameHint" type="warning" size="tiny" :bordered="false">
                  {{ usernameHint }}
                </NTag>
              </div>
              <div class="pf-setting-row__desc">
                @{{ profile?.userName || '---' }}
              </div>
            </div>
            <div class="pf-setting-row__control">
              <NButton
                v-if="profile?.canChangeUserName"
                size="small"
                ghost
                type="primary"
                :loading="usernameChangeLoading"
                @click="handleChangeUserName"
              >
                修改
              </NButton>
            </div>
          </div>

          <!-- 显示名称 -->
          <div class="pf-setting-row">
            <div class="pf-setting-row__main">
              <div class="pf-setting-row__label">
                显示名称
              </div>
              <div class="pf-setting-row__desc">
                展示在系统中的昵称
              </div>
            </div>
            <div class="pf-setting-row__control">
              <NInput v-model:value="profileForm.nickName" placeholder="您的昵称" class="pf-field" />
            </div>
          </div>
            </div>
          </div>

          <div class="pf-subgroup">
            <div class="pf-subgroup__title">
              联系方式
            </div>
            <div class="pf-setting-list">
          <!-- 电子邮箱 -->
          <div class="pf-setting-row pf-setting-row--wrap">
            <div class="pf-setting-row__main">
              <div class="pf-setting-row__label">
                电子邮箱
                <NTag v-if="profile?.emailVerified" type="success" size="tiny" :bordered="false">
                  已验证
                </NTag>
                <NTag v-else-if="profile?.email" type="warning" size="tiny" :bordered="false">
                  未验证
                </NTag>
              </div>
              <div class="pf-setting-row__desc">
                {{ profile?.email || '未设置' }}
              </div>
            </div>
            <div class="pf-setting-row__control">
              <NButton size="small" ghost type="primary" @click="openChangeDialog('email')">
                {{ profile?.email ? '修改' : '绑定' }}
              </NButton>
              <NButton
                v-if="profile?.email && !profile?.emailVerified"
                size="small"
                quaternary
                :loading="verifyLoading && verifyTarget === 'email'"
                @click="sendVerifyCode('email')"
              >
                验证
              </NButton>
            </div>
            <div v-if="verifyTarget === 'email'" class="pf-inline-form">
              <NInput v-model:value="verifyCode" placeholder="请输入 6 位验证码" :maxlength="6" class="pf-field" />
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

          <!-- 手机号码 -->
          <div class="pf-setting-row pf-setting-row--wrap">
            <div class="pf-setting-row__main">
              <div class="pf-setting-row__label">
                手机号码
                <NTag v-if="profile?.phoneVerified" type="success" size="tiny" :bordered="false">
                  已验证
                </NTag>
                <NTag v-else-if="profile?.phone" type="warning" size="tiny" :bordered="false">
                  未验证
                </NTag>
              </div>
              <div class="pf-setting-row__desc">
                {{ profile?.phone || '未设置' }}
              </div>
            </div>
            <div class="pf-setting-row__control">
              <NButton size="small" ghost type="primary" @click="openChangeDialog('phone')">
                {{ profile?.phone ? '修改' : '绑定' }}
              </NButton>
              <NButton
                v-if="profile?.phone && !profile?.phoneVerified"
                size="small"
                quaternary
                :loading="verifyLoading && verifyTarget === 'phone'"
                @click="sendVerifyCode('phone')"
              >
                验证
              </NButton>
            </div>
            <div v-if="verifyTarget === 'phone'" class="pf-inline-form">
              <NInput v-model:value="verifyCode" placeholder="请输入 6 位验证码" :maxlength="6" class="pf-field" />
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
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- 个人信息 -->
    <section class="pf-section">
      <div class="pf-section__head">
        <div class="pf-section__heading">
          <div class="pf-section__title">
            <Icon icon="lucide:contact" width="16" />
            <span>个人信息</span>
          </div>
        </div>
      </div>
      <div class="pf-section__body">
        <div class="pf-field-grid">
          <div class="pf-field-card">
            <span class="pf-field-card__label">性别</span>
            <NSelect v-model:value="profileForm.gender" :options="genderOptions" />
          </div>
          <div class="pf-field-card">
            <span class="pf-field-card__label">国家 / 地区</span>
            <NInput v-model:value="profileForm.country" placeholder="例如：中国" />
          </div>
          <div class="pf-field-card">
            <span class="pf-field-card__label">语言</span>
            <NSelect v-model:value="profileForm.language" :options="languageOptions" />
          </div>
          <div class="pf-field-card">
            <span class="pf-field-card__label">时区</span>
            <NSelect v-model:value="profileForm.timeZone" :options="timezoneOptions" />
          </div>
          <div class="pf-field-card pf-field-card--block">
            <span class="pf-field-card__label">个人简介</span>
            <NInput
              v-model:value="profileForm.remark"
              type="textarea"
              placeholder="介绍一下你自己..."
              :autosize="{ minRows: 3, maxRows: 6 }"
              :maxlength="200"
              show-count
            />
          </div>
        </div>
      </div>
      <div class="pf-section__actions">
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
    </section>

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
  </div>
</template>

<style src="./profile-shared.css" />

<style scoped>
.pf-avatar-section {
  display: flex;
  align-items: center;
  gap: 14px;
  min-width: 0;
  flex: 1;
}

.pf-avatar-info {
  min-width: 0;
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
