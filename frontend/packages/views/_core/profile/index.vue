<script lang="ts" setup>
import type { FormInst, FormRules } from 'naive-ui'
import type { UserProfile, UserSessionItem } from '~/types'
import {
  NAlert,
  NAvatar,
  NButton,
  NCard,
  NDivider,
  NEmpty,
  NForm,
  NFormItem,
  NIcon,
  NInput,
  NInputGroup,
  NInputOtp,
  NPopconfirm,
  NQrCode,
  NSelect,
  NSpace,
  NSpin,
  NSwitch,
  NTabPane,
  NTabs,
  NTag,
  NTooltip,
  useDialog,
  useMessage,
} from 'naive-ui'
import { computed, onMounted, ref, watch } from 'vue'
import {
  changePasswordApi,
  disable2FAApi,
  enable2FAApi,
  getProfileApi,
  getSessionsApi,
  revokeOtherSessionsApi,
  revokeSessionApi,
  setup2FAApi,
  updateProfileApi,
} from '@/api'
import { Icon } from '~/iconify'
import { useUserStore } from '~/stores'
import { copyToClipboard, formatDate } from '~/utils'

defineOptions({ name: 'ProfilePage' })

const userStore = useUserStore()
const message = useMessage()
const dialog = useDialog()

const activeTab = ref('profile')

// ==================== 档案数据 ====================

const profileLoading = ref(false)
const profileSaving = ref(false)
const profile = ref<UserProfile | null>(null)
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

async function loadProfile() {
  profileLoading.value = true
  try {
    profile.value = await getProfileApi()
    syncProfileForm()
  }
  catch (e: any) {
    message.error(e?.message || '加载个人资料失败')
  }
  finally {
    profileLoading.value = false
  }
}

function syncProfileForm() {
  if (!profile.value)
    return
  const p = profile.value
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

async function saveProfile() {
  await profileFormRef.value?.validate()
  profileSaving.value = true
  try {
    await updateProfileApi({
      ...profileForm.value,
      birthday: profileForm.value.birthday
        ? new Date(profileForm.value.birthday).toISOString()
        : undefined,
    })
    message.success('个人资料已更新')
    await loadProfile()
    if (userStore.userInfo) {
      userStore.setUserInfo({
        ...userStore.userInfo,
        nickName: profileForm.value.nickName,
        email: profileForm.value.email,
        phone: profileForm.value.phone,
      })
    }
  }
  catch (e: any) {
    message.error(e?.message || '保存失败')
  }
  finally {
    profileSaving.value = false
  }
}

// ==================== 修改密码 ====================

const pwdFormRef = ref<FormInst | null>(null)
const pwdSaving = ref(false)
const pwdForm = ref({ oldPassword: '', newPassword: '', confirmPassword: '' })
const pwdRules: FormRules = {
  oldPassword: [{ required: true, message: '请输入当前密码', trigger: 'blur' }],
  newPassword: [
    { required: true, message: '请输入新密码', trigger: 'blur' },
    { min: 6, max: 32, message: '密码长度 6～32 位', trigger: 'blur' },
  ],
  confirmPassword: [
    { required: true, message: '请确认新密码', trigger: 'blur' },
    {
      validator: (_: any, v: string) => v === pwdForm.value.newPassword,
      message: '两次输入密码不一致',
      trigger: 'blur',
    },
  ],
}
const pwdStrength = computed(() => {
  const p = pwdForm.value.newPassword
  if (!p)
    return { score: 0, color: '', label: '' }
  let s = 0
  if (p.length > 6)
    s++
  if (p.length > 10)
    s++
  if (/[A-Z]/.test(p))
    s++
  if (/\d/.test(p) && /[^a-z\d]/i.test(p))
    s++
  const colors = ['', '#ef4444', '#f59e0b', '#3b82f6', '#22c55e']
  const labels = ['', '弱', '一般', '较强', '强']
  return { score: s, color: colors[s] || '', label: labels[s] || '' }
})

async function changePassword() {
  await pwdFormRef.value?.validate()
  if (!profile.value)
    return
  pwdSaving.value = true
  try {
    await changePasswordApi({
      userId: profile.value.userId,
      oldPassword: pwdForm.value.oldPassword,
      newPassword: pwdForm.value.newPassword,
    })
    message.success('密码已更新')
    pwdForm.value = { oldPassword: '', newPassword: '', confirmPassword: '' }
    await loadProfile()
  }
  catch (e: any) {
    message.error(e?.message || '密码修改失败')
  }
  finally {
    pwdSaving.value = false
  }
}

// ==================== 双因素认证 ====================

const tfLoading = ref(false)
const tfSetup = ref<{ sharedKey: string, authenticatorUri: string } | null>(null)
const tfCode = ref<string[]>([])

async function handleSetup2FA() {
  tfLoading.value = true
  try {
    tfSetup.value = await setup2FAApi()
  }
  catch (e: any) {
    message.error(e?.message || '初始化失败')
  }
  finally {
    tfLoading.value = false
  }
}

const tfCodeStr = computed(() => tfCode.value.join(''))

async function handleEnable2FA() {
  if (!tfCodeStr.value || tfCodeStr.value.length < 6) {
    message.warning('请输入完整的 6 位验证码')
    return
  }
  tfLoading.value = true
  try {
    await enable2FAApi(tfCodeStr.value)
    message.success('双因素认证已启用')
    tfSetup.value = null
    tfCode.value = []
    await loadProfile()
  }
  catch (e: any) {
    message.error(e?.message || '启用失败')
  }
  finally {
    tfLoading.value = false
  }
}

function onToggle2FA(val: boolean) {
  if (val) {
    handleSetup2FA()
  }
  else {
    dialog.warning({
      title: '禁用双因素认证',
      content: '关闭后账户安全性将降低，确定要继续吗？',
      positiveText: '确认禁用',
      negativeText: '取消',
      onPositiveClick: async () => {
        if (!tfCodeStr.value || tfCodeStr.value.length < 6) {
          message.warning('请输入完整的 6 位验证码以确认身份')
          return
        }
        tfLoading.value = true
        try {
          await disable2FAApi(tfCodeStr.value)
          message.success('双因素认证已禁用')
          tfCode.value = []
          await loadProfile()
        }
        catch (e: any) {
          message.error(e?.message || '禁用失败')
        }
        finally {
          tfLoading.value = false
        }
      },
    })
  }
}

// ==================== 会话管理 ====================

const sessionsLoading = ref(false)
const sessions = ref<UserSessionItem[]>([])
const sessionsLoaded = ref(false)

async function loadSessions() {
  sessionsLoading.value = true
  try {
    sessions.value = await getSessionsApi()
    sessionsLoaded.value = true
  }
  catch (e: any) {
    message.error(e?.message || '加载失败')
  }
  finally {
    sessionsLoading.value = false
  }
}

async function handleRevokeSession(sid: string) {
  try {
    await revokeSessionApi(sid)
    message.success('设备已登出')
    await loadSessions()
  }
  catch (e: any) {
    message.error(e?.message || '操作失败')
  }
}

function handleRevokeOthers() {
  const cnt = sessions.value.filter(s => !s.isCurrent).length
  if (!cnt) {
    message.info('没有其他在线设备')
    return
  }
  dialog.warning({
    title: '登出所有设备',
    content: `将下线除当前设备外的 ${cnt} 个设备，是否继续？`,
    positiveText: '确认',
    negativeText: '取消',
    onPositiveClick: async () => {
      try {
        await revokeOtherSessionsApi()
        message.success('已登出所有其他设备')
        await loadSessions()
      }
      catch (e: any) {
        message.error(e?.message || '操作失败')
      }
    },
  })
}

function deviceIcon(t: number) {
  return (
    { 1: 'lucide:globe', 2: 'lucide:smartphone', 3: 'lucide:monitor', 4: 'lucide:tablet' }[t]
    || 'lucide:help-circle'
  )
}

// ==================== 通知偏好 ====================

interface NotifyChannel {
  key: string
  label: string
  desc: string
  icon: string
  enabled: boolean
  /** 是否为营销类型（用户必须可关闭） */
  marketing?: boolean
}

const notifyChannels = ref<NotifyChannel[]>([
  { key: 'email_security', label: '安全警报邮件', desc: '登录异常、密码修改等安全事件通知', icon: 'lucide:shield-alert', enabled: true },
  { key: 'email_system', label: '系统通知邮件', desc: '账户变更、服务状态等系统消息', icon: 'lucide:mail', enabled: true },
  { key: 'sms_security', label: '安全短信通知', desc: '异地登录、高危操作短信提醒', icon: 'lucide:smartphone', enabled: false },
  { key: 'sms_system', label: '系统短信通知', desc: '重要系统公告短信推送', icon: 'lucide:message-square', enabled: false },
  { key: 'in_app', label: '站内消息', desc: '系统内消息中心通知', icon: 'lucide:bell', enabled: true },
  { key: 'email_marketing', label: '营销与推广', desc: '产品更新、优惠活动等推广信息', icon: 'lucide:megaphone', enabled: false, marketing: true },
])

// TODO: 对接后端通知偏好 API（GET/PUT /Auth/NotificationPreferences）

// ==================== 开发者设置 ====================

interface ApiCredential {
  appId: string
  appKey: string
  createdAt: string
  lastUsedAt?: string
}

const apiCredentials = ref<ApiCredential[]>([])
const newSecret = ref('')
const webhookUrl = ref('')
const ipWhitelist = ref('')
const signAlgorithm = ref('HmacSHA256')

const signAlgorithmOptions = [
  { label: 'HmacSHA256', value: 'HmacSHA256' },
  { label: 'HmacSHA512', value: 'HmacSHA512' },
  { label: 'RSA-SHA256', value: 'RSA-SHA256' },
]

function handleCreateCredential() {
  // TODO: 对接后端 POST /OpenApi/CreateCredential
  message.info('需要后端实现 OpenAPI 凭证创建接口')
}

function handleRotateSecret(appId: string) {
  dialog.warning({
    title: '滚动密钥',
    content: '生成新密钥后旧密钥将立即失效，确定继续？',
    positiveText: '确认',
    negativeText: '取消',
    onPositiveClick: () => {
      // TODO: 对接后端 POST /OpenApi/RotateSecret
      message.info(`[${appId}] 需要后端实现密钥滚动接口`)
    },
  })
}

function handleDeleteCredential(appId: string) {
  dialog.error({
    title: '删除凭证',
    content: '删除后使用此凭证的所有集成将立即失效，此操作不可恢复。',
    positiveText: '确认删除',
    negativeText: '取消',
    onPositiveClick: () => {
      // TODO: 对接后端 DELETE /OpenApi/Credential/{appId}
      message.info(`[${appId}] 需要后端实现凭证删除接口`)
    },
  })
}

function handleSaveWebhook() {
  // TODO: 对接后端 PUT /OpenApi/Webhook
  message.info('需要后端实现 Webhook 配置接口')
}

function handleSaveIpWhitelist() {
  // TODO: 对接后端 PUT /OpenApi/IpWhitelist
  message.info('需要后端实现 IP 白名单接口')
}

// ==================== 生命周期 ====================

watch(activeTab, (t) => {
  if (t === 'security' && !sessionsLoaded.value)
    loadSessions()
})

onMounted(() => {
  loadProfile()
})
</script>

<template>
  <div class="profile-page mx-auto max-w-4xl px-4 py-6 sm:px-6 lg:px-8">
    <NSpin :show="profileLoading && !profile">
      <!-- 顶部用户概览 -->
      <div class="mb-6 flex flex-col items-start gap-5 sm:flex-row sm:items-center">
        <div class="group relative">
          <NAvatar
            round
            :size="72"
            :src="profile?.avatar || userStore.avatar"
            :fallback-src="`https://api.dicebear.com/9.x/initials/svg?seed=${userStore.nickname}`"
            class="border-3 border-white shadow-md dark:border-gray-800"
          />
          <button
            class="absolute bottom-0 right-0 flex h-7 w-7 items-center justify-center rounded-full bg-[var(--primary-color)] text-white shadow transition-transform hover:scale-110"
          >
            <Icon icon="lucide:camera" width="13" />
          </button>
        </div>
        <div class="flex-1">
          <h1 class="text-xl font-bold">
            {{ profile?.nickName || profile?.userName || userStore.nickname || '---' }}
          </h1>
          <p class="mt-0.5 text-sm opacity-50">
            @{{ profile?.userName || '---' }}
            <template v-if="profile?.lastLoginTime">
              · 上次登录 {{ formatDate(profile.lastLoginTime) }}
            </template>
            <template v-if="profile?.lastLoginIp">
              ({{ profile.lastLoginIp }})
            </template>
          </p>
        </div>
      </div>

      <!-- ==================== 标签页导航 ==================== -->
      <NTabs v-model:value="activeTab" type="line" animated>
        <!-- ==================== 个人资料 ==================== -->
        <NTabPane name="profile">
          <template #tab>
            <div class="flex items-center gap-1.5">
              <Icon icon="lucide:user-round" width="16" />
              <span>个人资料</span>
            </div>
          </template>
          <div class="space-y-5 pt-4">
            <!-- 头像管理 -->
            <NCard size="small">
              <div class="flex items-center gap-5">
                <NAvatar
                  round
                  :size="64"
                  :src="profile?.avatar || userStore.avatar"
                  :fallback-src="`https://api.dicebear.com/9.x/initials/svg?seed=${userStore.nickname}`"
                />
                <div class="flex-1">
                  <p class="font-medium">
                    头像设置
                  </p>
                  <p class="mt-0.5 text-xs opacity-50">
                    支持 JPG、PNG 格式，不超过 2MB
                  </p>
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

            <!-- 基本信息表单 -->
            <NCard size="small" title="基本信息">
              <NForm ref="profileFormRef" :model="profileForm" label-placement="top">
                <div class="grid grid-cols-1 gap-x-6 md:grid-cols-2">
                  <!-- 用户名只读 -->
                  <NFormItem label="用户名">
                    <NInput :value="profile?.userName" disabled placeholder="---">
                      <template #suffix>
                        <NTag size="tiny" :bordered="false">
                          不可修改
                        </NTag>
                      </template>
                    </NInput>
                  </NFormItem>
                  <NFormItem label="显示名称" path="nickName">
                    <NInput v-model:value="profileForm.nickName" placeholder="您的昵称" />
                  </NFormItem>
                  <NFormItem label="电子邮箱" path="email">
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
                  </NFormItem>
                  <NFormItem label="手机号码" path="phone">
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
                  </NFormItem>
                  <NFormItem label="性别">
                    <NSelect v-model:value="profileForm.gender" :options="genderOptions" />
                  </NFormItem>
                  <NFormItem label="国家 / 地区">
                    <NInput v-model:value="profileForm.country" placeholder="例如：中国" />
                  </NFormItem>
                  <NFormItem label="语言">
                    <NSelect v-model:value="profileForm.language" :options="languageOptions" />
                  </NFormItem>
                  <NFormItem label="时区">
                    <NSelect v-model:value="profileForm.timeZone" :options="timezoneOptions" />
                  </NFormItem>
                </div>
                <NFormItem label="个人简介">
                  <NInput
                    v-model:value="profileForm.remark"
                    type="textarea"
                    placeholder="介绍一下你自己..."
                    :autosize="{ minRows: 3, maxRows: 6 }"
                    :maxlength="200"
                    show-count
                  />
                </NFormItem>
              </NForm>
              <template #action>
                <NSpace justify="end">
                  <NButton @click="syncProfileForm">
                    取消
                  </NButton>
                  <NButton type="primary" :loading="profileSaving" @click="saveProfile">
                    <template #icon>
                      <NIcon><Icon icon="lucide:save" /></NIcon>
                    </template>
                    保存更改
                  </NButton>
                </NSpace>
              </template>
            </NCard>
          </div>
        </NTabPane>

        <!-- ==================== 账户安全 ==================== -->
        <NTabPane name="security">
          <template #tab>
            <div class="flex items-center gap-1.5">
              <Icon icon="lucide:shield-check" width="16" />
              <span>安全设置</span>
            </div>
          </template>
          <div class="space-y-5 pt-4">
            <!-- 修改密码 -->
            <NCard size="small" title="修改密码">
              <template #header-extra>
                <span v-if="profile?.lastPasswordChangeTime" class="text-xs opacity-40">
                  上次修改：{{ formatDate(profile.lastPasswordChangeTime) }}
                </span>
              </template>
              <NForm ref="pwdFormRef" :model="pwdForm" :rules="pwdRules">
                <NFormItem path="oldPassword" :show-label="false">
                  <NInput v-model:value="pwdForm.oldPassword" type="password" placeholder="当前密码" show-password-on="click" />
                </NFormItem>
                <NFormItem path="newPassword" :show-label="false">
                  <div class="w-full">
                    <NInput v-model:value="pwdForm.newPassword" type="password" placeholder="新密码" show-password-on="click" />
                    <div v-if="pwdForm.newPassword" class="mt-2 flex items-center gap-2">
                      <div class="flex flex-1 gap-1">
                        <div
                          v-for="i in 4" :key="i"
                          class="h-1 flex-1 rounded-full transition-colors"
                          :style="{ background: i <= pwdStrength.score ? pwdStrength.color : 'var(--n-border-color, #e0e0e6)' }"
                        />
                      </div>
                      <span class="text-xs" :style="{ color: pwdStrength.color }">{{ pwdStrength.label }}</span>
                    </div>
                  </div>
                </NFormItem>
                <NFormItem path="confirmPassword" :show-label="false">
                  <NInput v-model:value="pwdForm.confirmPassword" type="password" placeholder="确认新密码" show-password-on="click" />
                </NFormItem>
              </NForm>
              <template #action>
                <NButton type="primary" block :loading="pwdSaving" @click="changePassword">
                  更新密码
                </NButton>
              </template>
            </NCard>

            <!-- 两步验证 -->
            <NCard size="small">
              <div class="flex items-start justify-between gap-4">
                <div class="flex items-start gap-3">
                  <div
                    class="mt-0.5 flex h-10 w-10 shrink-0 items-center justify-center rounded-lg"
                    :class="profile?.twoFactorEnabled
                      ? 'bg-green-100 text-green-600 dark:bg-green-900/30'
                      : 'bg-gray-100 text-gray-400 dark:bg-gray-800'"
                  >
                    <Icon icon="lucide:shield-check" width="20" />
                  </div>
                  <div>
                    <p class="font-medium">
                      两步验证 (TOTP)
                    </p>
                    <p class="mt-0.5 text-xs opacity-50">
                      使用 Google/Microsoft Authenticator 等应用生成一次性验证码
                    </p>
                    <NTag
                      v-if="profile?.twoFactorEnabled"
                      type="success" size="small" :bordered="false" class="mt-2"
                    >
                      <template #icon>
                        <NIcon><Icon icon="lucide:check-circle-2" /></NIcon>
                      </template>
                      已启用
                    </NTag>
                  </div>
                </div>
                <NSwitch :value="profile?.twoFactorEnabled" :loading="tfLoading" @update:value="onToggle2FA" />
              </div>

              <!-- 2FA 设置流程 -->
              <template v-if="tfSetup && !profile?.twoFactorEnabled">
                <NDivider />
                <div class="space-y-5">
                  <div>
                    <p class="mb-3 text-sm font-medium">
                      第 1 步：打开认证器应用，扫描二维码或手动输入密钥
                    </p>
                    <div class="flex flex-col items-start gap-5 sm:flex-row">
                      <div class="flex flex-col items-center gap-2 rounded-lg border p-3">
                        <NQrCode
                          :value="tfSetup.authenticatorUri"
                          :size="180"
                          error-correction-level="M"
                        />
                        <span class="text-xs opacity-40">扫描此二维码</span>
                      </div>
                      <div class="flex-1 space-y-2">
                        <p class="text-xs opacity-50">
                          无法扫码？手动输入以下密钥：
                        </p>
                        <div class="flex items-center gap-2">
                          <code class="flex-1 break-all rounded-md bg-gray-100 px-3 py-2 font-mono text-sm dark:bg-gray-800">
                            {{ tfSetup.sharedKey }}
                          </code>
                          <NTooltip>
                            <template #trigger>
                              <NButton
                                size="small" quaternary
                                @click="copyToClipboard(tfSetup.sharedKey).then(() => message.success('已复制'))"
                              >
                                <template #icon>
                                  <NIcon><Icon icon="lucide:copy" /></NIcon>
                                </template>
                              </NButton>
                            </template>
                            复制密钥
                          </NTooltip>
                        </div>
                      </div>
                    </div>
                  </div>
                  <div>
                    <p class="mb-3 text-sm font-medium">
                      第 2 步：输入认证器上的 6 位验证码
                    </p>
                    <NSpace align="center">
                      <NInputOtp
                        v-model:value="tfCode"
                        :length="6"
                        @complete="handleEnable2FA"
                      />
                      <NButton type="primary" :loading="tfLoading" @click="handleEnable2FA">
                        验证并启用
                      </NButton>
                    </NSpace>
                  </div>
                </div>
              </template>

              <template v-if="profile?.twoFactorEnabled">
                <NDivider />
                <div class="flex items-center gap-3">
                  <NInputOtp
                    v-model:value="tfCode"
                    :length="6"
                    size="small"
                  />
                  <span class="text-xs opacity-40">禁用前需验证身份</span>
                </div>
              </template>
            </NCard>

            <!-- 活跃会话 / 登录设备 -->
            <NCard size="small" title="登录设备管理">
              <template #header-extra>
                <NSpace :size="8">
                  <NButton size="tiny" quaternary @click="loadSessions">
                    <template #icon>
                      <NIcon><Icon icon="lucide:refresh-cw" /></NIcon>
                    </template>
                  </NButton>
                  <NButton size="tiny" @click="handleRevokeOthers">
                    登出其他设备
                  </NButton>
                </NSpace>
              </template>
              <NSpin :show="sessionsLoading">
                <NEmpty v-if="sessions.length === 0 && sessionsLoaded" description="暂无在线设备" />
                <div v-else class="space-y-3">
                  <div
                    v-for="s in sessions" :key="s.sessionId"
                    class="flex items-center justify-between rounded-lg border p-3"
                    :class="s.isCurrent ? 'border-[var(--primary-color)] border-opacity-30 bg-[var(--primary-color)] bg-opacity-[0.04]' : ''"
                  >
                    <div class="flex items-center gap-3">
                      <div
                        class="flex h-9 w-9 items-center justify-center rounded-lg"
                        :class="s.isCurrent
                          ? 'bg-[var(--primary-color)] bg-opacity-10 text-[var(--primary-color)]'
                          : 'bg-gray-100 text-gray-500 dark:bg-gray-800'"
                      >
                        <Icon :icon="deviceIcon(s.deviceType)" width="18" />
                      </div>
                      <div>
                        <p class="text-sm font-medium">
                          {{ s.deviceName || s.browser || '未知设备' }}
                          <NTag v-if="s.isCurrent" type="success" size="tiny" :bordered="false" class="ml-1.5 align-middle">
                            当前
                          </NTag>
                        </p>
                        <p class="text-xs opacity-50">
                          {{ s.ipAddress }}
                          <template v-if="s.location">
                            · {{ s.location }}
                          </template>
                          <template v-if="s.operatingSystem">
                            · {{ s.operatingSystem }}
                          </template>
                          · {{ s.isCurrent ? '在线' : formatDate(s.lastActivityTime, 'MM-DD HH:mm') }}
                        </p>
                      </div>
                    </div>
                    <NPopconfirm v-if="!s.isCurrent" @positive-click="handleRevokeSession(s.sessionId)">
                      <template #trigger>
                        <NButton size="tiny" type="error" text>
                          踢下线
                        </NButton>
                      </template>
                      确定登出该设备？
                    </NPopconfirm>
                  </div>
                </div>
              </NSpin>
            </NCard>
          </div>
        </NTabPane>

        <!-- ==================== 通知与偏好 ==================== -->
        <NTabPane name="notifications">
          <template #tab>
            <div class="flex items-center gap-1.5">
              <Icon icon="lucide:bell" width="16" />
              <span>通知偏好</span>
            </div>
          </template>
          <div class="space-y-5 pt-4">
            <NAlert type="info" :bordered="false">
              通知偏好设置将在后端接口就绪后生效。营销类通知您可随时关闭（GDPR 合规）。
            </NAlert>

            <NCard size="small" title="通知渠道">
              <div class="space-y-1">
                <div
                  v-for="ch in notifyChannels" :key="ch.key"
                  class="flex items-center justify-between rounded-lg px-2 py-3 transition-colors hover:bg-gray-50 dark:hover:bg-white/5"
                >
                  <div class="flex items-center gap-3">
                    <div class="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-gray-100 dark:bg-gray-800">
                      <Icon :icon="ch.icon" width="18" class="opacity-60" />
                    </div>
                    <div>
                      <p class="text-sm font-medium">
                        {{ ch.label }}
                        <NTag v-if="ch.marketing" size="tiny" :bordered="false" class="ml-1 align-middle">
                          营销
                        </NTag>
                      </p>
                      <p class="text-xs opacity-50">
                        {{ ch.desc }}
                      </p>
                    </div>
                  </div>
                  <NSwitch v-model:value="ch.enabled" />
                </div>
              </div>
            </NCard>
          </div>
        </NTabPane>

        <!-- ==================== 开发者设置 (OpenAPI) ==================== -->
        <NTabPane name="developer">
          <template #tab>
            <div class="flex items-center gap-1.5">
              <Icon icon="lucide:code-2" width="16" />
              <span>开发者设置</span>
            </div>
          </template>
          <div class="space-y-5 pt-4">
            <NAlert type="warning" :bordered="false">
              API 凭证功能需配合后端实现，当前为界面预览。Secret 仅在创建时显示一次，请妥善保管。
            </NAlert>

            <!-- API 凭证 -->
            <NCard size="small" title="API 凭证">
              <template #header-extra>
                <NButton size="small" type="primary" @click="handleCreateCredential">
                  <template #icon>
                    <NIcon><Icon icon="lucide:plus" /></NIcon>
                  </template>
                  创建凭证
                </NButton>
              </template>

              <NEmpty v-if="apiCredentials.length === 0" description="暂无 API 凭证">
                <template #extra>
                  <NButton size="small" @click="handleCreateCredential">
                    创建第一个凭证
                  </NButton>
                </template>
              </NEmpty>

              <div v-else class="space-y-3">
                <div
                  v-for="cred in apiCredentials" :key="cred.appId"
                  class="rounded-lg border p-4"
                >
                  <div class="mb-3 flex items-start justify-between">
                    <div>
                      <p class="font-mono text-sm font-medium">
                        {{ cred.appId }}
                      </p>
                      <p class="mt-0.5 text-xs opacity-40">
                        创建于 {{ formatDate(cred.createdAt) }}
                        <template v-if="cred.lastUsedAt">
                          · 最后使用 {{ formatDate(cred.lastUsedAt) }}
                        </template>
                      </p>
                    </div>
                    <NSpace :size="4">
                      <NTooltip>
                        <template #trigger>
                          <NButton size="tiny" quaternary @click="handleRotateSecret(cred.appId)">
                            <template #icon>
                              <NIcon><Icon icon="lucide:rotate-ccw" /></NIcon>
                            </template>
                          </NButton>
                        </template>
                        滚动密钥
                      </NTooltip>
                      <NTooltip>
                        <template #trigger>
                          <NButton size="tiny" quaternary type="error" @click="handleDeleteCredential(cred.appId)">
                            <template #icon>
                              <NIcon><Icon icon="lucide:trash-2" /></NIcon>
                            </template>
                          </NButton>
                        </template>
                        删除凭证
                      </NTooltip>
                    </NSpace>
                  </div>
                  <div class="grid grid-cols-1 gap-2 md:grid-cols-2">
                    <div>
                      <p class="mb-1 text-xs opacity-50">
                        AppKey
                      </p>
                      <NInputGroup>
                        <NInput :value="cred.appKey" readonly size="small" />
                        <NButton size="small" @click="copyToClipboard(cred.appKey).then(() => message.success('已复制'))">
                          <template #icon>
                            <NIcon><Icon icon="lucide:copy" /></NIcon>
                          </template>
                        </NButton>
                      </NInputGroup>
                    </div>
                  </div>
                </div>
              </div>

              <!-- 新创建的 Secret 提示（仅显示一次） -->
              <template v-if="newSecret">
                <NDivider />
                <NAlert type="warning" title="请立即保存 API Secret" :bordered="false">
                  <p class="mb-2 text-sm">
                    此密钥仅显示一次，关闭后将无法再次查看：
                  </p>
                  <NInputGroup>
                    <NInput :value="newSecret" readonly type="password" show-password-on="click" />
                    <NButton @click="copyToClipboard(newSecret).then(() => message.success('已复制'))">
                      <template #icon>
                        <NIcon><Icon icon="lucide:copy" /></NIcon>
                      </template>
                    </NButton>
                  </NInputGroup>
                </NAlert>
              </template>
            </NCard>

            <!-- 签名算法 -->
            <NCard size="small" title="签名算法">
              <p class="mb-3 text-xs opacity-50">
                选择 API 请求签名使用的算法，修改后需同步更新客户端配置
              </p>
              <NSelect
                v-model:value="signAlgorithm"
                :options="signAlgorithmOptions"
                style="max-width: 240px"
              />
            </NCard>

            <!-- Webhook -->
            <NCard size="small" title="Webhook 回调">
              <p class="mb-3 text-xs opacity-50">
                配置事件回调地址，系统会将订单状态变更等事件推送到此 URL
              </p>
              <NInputGroup>
                <NInput v-model:value="webhookUrl" placeholder="https://your-domain.com/webhook" />
                <NButton type="primary" @click="handleSaveWebhook">
                  保存
                </NButton>
              </NInputGroup>
            </NCard>

            <!-- IP 白名单 -->
            <NCard size="small" title="IP 白名单">
              <p class="mb-3 text-xs opacity-50">
                限制可调用 API 的来源 IP，每行一个，留空则不限制
              </p>
              <NInput
                v-model:value="ipWhitelist"
                type="textarea"
                placeholder="192.168.1.1&#10;10.0.0.0/24"
                :autosize="{ minRows: 3, maxRows: 8 }"
              />
              <div class="mt-3 flex justify-end">
                <NButton type="primary" size="small" @click="handleSaveIpWhitelist">
                  保存白名单
                </NButton>
              </div>
            </NCard>
          </div>
        </NTabPane>
      </NTabs>
    </NSpin>
  </div>
</template>

<style scoped>
.profile-page :deep(.n-tabs-nav) {
  padding-top: 0;
}
</style>
