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
  NGrid,
  NGridItem,
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

// ==================== 档案 ====================

const profileLoading = ref(false)
const profileSaving = ref(false)
const profile = ref<UserProfile | null>(null)
const profileFormRef = ref<FormInst | null>(null)

const profileForm = ref({
  nickName: '', realName: '', email: '', phone: '',
  gender: 0 as number, birthday: null as null | number,
  country: '', remark: '', language: 'zh-CN', timeZone: '',
})

const genderOptions = [
  { label: '未设置', value: 0 }, { label: '男', value: 1 }, { label: '女', value: 2 },
]
const languageOptions = [
  { label: '简体中文', value: 'zh-CN' }, { label: 'English', value: 'en-US' },
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
  try { profile.value = await getProfileApi(); syncProfileForm() }
  catch (e: any) { message.error(e?.message || '加载个人资料失败') }
  finally { profileLoading.value = false }
}

function syncProfileForm() {
  if (!profile.value)
    return
  const p = profile.value
  profileForm.value = {
    nickName: p.nickName ?? '', realName: p.realName ?? '',
    email: p.email ?? '', phone: p.phone ?? '',
    gender: p.gender ?? 0,
    birthday: p.birthday ? new Date(p.birthday).getTime() : null,
    country: p.country ?? '', remark: p.remark ?? '',
    language: p.language ?? 'zh-CN', timeZone: p.timeZone ?? '',
  }
}

async function saveProfile() {
  await profileFormRef.value?.validate()
  profileSaving.value = true
  try {
    await updateProfileApi({
      ...profileForm.value,
      birthday: profileForm.value.birthday ? new Date(profileForm.value.birthday).toISOString() : undefined,
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
  catch (e: any) { message.error(e?.message || '保存失败') }
  finally { profileSaving.value = false }
}

// ==================== 密码 ====================

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
    { validator: (_: any, v: string) => v === pwdForm.value.newPassword, message: '两次输入密码不一致', trigger: 'blur' },
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
    await changePasswordApi({ userId: profile.value.userId, oldPassword: pwdForm.value.oldPassword, newPassword: pwdForm.value.newPassword })
    message.success('密码已更新')
    pwdForm.value = { oldPassword: '', newPassword: '', confirmPassword: '' }
    await loadProfile()
  }
  catch (e: any) { message.error(e?.message || '密码修改失败') }
  finally { pwdSaving.value = false }
}

// ==================== 2FA ====================

const tfLoading = ref(false)
const tfSetup = ref<{ sharedKey: string, authenticatorUri: string } | null>(null)
const tfCode = ref<string[]>([])
const tfCodeStr = computed(() => tfCode.value.join(''))
const tfDisabling = ref(false)

async function handleSetup2FA() {
  tfLoading.value = true
  try { tfSetup.value = await setup2FAApi() }
  catch (e: any) { message.error(e?.message || '初始化失败') }
  finally { tfLoading.value = false }
}

async function handleEnable2FA() {
  if (!tfCodeStr.value || tfCodeStr.value.length < 6) { message.warning('请输入完整的 6 位验证码'); return }
  tfLoading.value = true
  try {
    await enable2FAApi(tfCodeStr.value)
    message.success('双因素认证已启用')
    tfSetup.value = null; tfCode.value = []
    await loadProfile()
  }
  catch (e: any) { message.error(e?.message || '启用失败') }
  finally { tfLoading.value = false }
}

async function handleDisable2FA() {
  if (!tfCodeStr.value || tfCodeStr.value.length < 6) { message.warning('请输入完整的 6 位验证码'); return }
  tfLoading.value = true
  try {
    await disable2FAApi(tfCodeStr.value)
    message.success('双因素认证已禁用')
    tfCode.value = []; tfDisabling.value = false
    await loadProfile()
  }
  catch (e: any) { message.error(e?.message || '禁用失败') }
  finally { tfLoading.value = false }
}

function onToggle2FA(val: boolean) {
  if (val)
    handleSetup2FA()
  else { tfDisabling.value = true; tfCode.value = [] }
}

function cancelDisable2FA() { tfDisabling.value = false; tfCode.value = [] }

// ==================== 会话 ====================

const sessionsLoading = ref(false)
const sessions = ref<UserSessionItem[]>([])
const sessionsLoaded = ref(false)

async function loadSessions() {
  sessionsLoading.value = true
  try { sessions.value = await getSessionsApi(); sessionsLoaded.value = true }
  catch (e: any) { message.error(e?.message || '加载失败') }
  finally { sessionsLoading.value = false }
}

async function handleRevokeSession(sid: string) {
  try { await revokeSessionApi(sid); message.success('设备已登出'); await loadSessions() }
  catch (e: any) { message.error(e?.message || '操作失败') }
}

function handleRevokeOthers() {
  const cnt = sessions.value.filter(s => !s.isCurrent).length
  if (!cnt) { message.info('没有其他在线设备'); return }
  dialog.warning({
    title: '登出所有设备', content: `将下线除当前设备外的 ${cnt} 个设备，是否继续？`,
    positiveText: '确认', negativeText: '取消',
    onPositiveClick: async () => {
      try { await revokeOtherSessionsApi(); message.success('已登出所有其他设备'); await loadSessions() }
      catch (e: any) { message.error(e?.message || '操作失败') }
    },
  })
}

function deviceIcon(t: number) {
  return { 1: 'lucide:globe', 2: 'lucide:smartphone', 3: 'lucide:monitor', 4: 'lucide:tablet' }[t] || 'lucide:help-circle'
}

// ==================== 登录记录 ====================

interface LoginRecord { time: string, ip: string, location?: string, device?: string, status: 'success' | 'failed' }
const loginHistory = ref<LoginRecord[]>([])
const loginHistoryLoading = ref(false)

// ==================== 账号管理 ====================

function handleDeactivateAccount() {
  dialog.warning({
    title: '停用账号', content: '停用后您将无法登录，但数据会保留。您可以联系管理员重新激活账号。确定继续？',
    positiveText: '确认停用', negativeText: '取消',
    onPositiveClick: () => { message.info('需要后端实现账号停用接口') },
  })
}

function handleFreezeAccount() {
  dialog.warning({
    title: '冻结账号', content: '冻结后账号将进入只读状态，无法执行任何写操作。可随时解冻。确定继续？',
    positiveText: '确认冻结', negativeText: '取消',
    onPositiveClick: () => { message.info('需要后端实现账号冻结接口') },
  })
}

// ==================== 通知偏好 ====================

interface NotifyChannel { key: string, label: string, desc: string, icon: string, enabled: boolean, marketing?: boolean }

const notifyChannels = ref<NotifyChannel[]>([
  { key: 'email_security', label: '安全警报邮件', desc: '登录异常、密码修改等安全事件通知', icon: 'lucide:shield-alert', enabled: true },
  { key: 'email_system', label: '系统通知邮件', desc: '账户变更、服务状态等系统消息', icon: 'lucide:mail', enabled: true },
  { key: 'sms_security', label: '安全短信通知', desc: '异地登录、高危操作短信提醒', icon: 'lucide:smartphone', enabled: false },
  { key: 'sms_system', label: '系统短信通知', desc: '重要系统公告短信推送', icon: 'lucide:message-square', enabled: false },
  { key: 'in_app', label: '站内消息', desc: '系统内消息中心通知', icon: 'lucide:bell', enabled: true },
  { key: 'email_marketing', label: '营销与推广', desc: '产品更新、优惠活动等推广信息', icon: 'lucide:megaphone', enabled: false, marketing: true },
])

// ==================== 开发者设置 ====================

interface ApiCredential { appId: string, appKey: string, createdAt: string, lastUsedAt?: string }

const apiCredentials = ref<ApiCredential[]>([])
const newSecret = ref('')
const ipWhitelist = ref('')
const signAlgorithm = ref('HmacSHA256')
const signAlgorithmOptions = [
  { label: 'HmacSHA256', value: 'HmacSHA256' },
  { label: 'HmacSHA512', value: 'HmacSHA512' },
  { label: 'RSA-SHA256', value: 'RSA-SHA256' },
]

function handleCreateCredential() { message.info('需要后端实现 OpenAPI 凭证创建接口') }
function handleRotateSecret(appId: string) {
  dialog.warning({ title: '滚动密钥', content: '生成新密钥后旧密钥将立即失效，确定继续？', positiveText: '确认', negativeText: '取消', onPositiveClick: () => { message.info(`[${appId}] 需要后端实现密钥滚动接口`) } })
}
function handleDeleteCredential(appId: string) {
  dialog.error({ title: '删除凭证', content: '删除后使用此凭证的所有集成将立即失效，此操作不可恢复。', positiveText: '确认删除', negativeText: '取消', onPositiveClick: () => { message.info(`[${appId}] 需要后端实现凭证删除接口`) } })
}
function handleSaveIpWhitelist() { message.info('需要后端实现 IP 白名单接口') }

// ==================== 生命周期 ====================

watch(activeTab, (t) => { if (t === 'security' && !sessionsLoaded.value) loadSessions() })
onMounted(() => { loadProfile() })
</script>

<template>
  <div class="pf-page">
    <NSpin :show="profileLoading && !profile">
      <!-- ===== Banner ===== -->
      <div class="pf-banner">
        <div class="pf-banner-glow" />
        <div class="pf-banner-head">
          <div class="pf-banner-title">
            <Icon icon="lucide:user-cog" width="18" />
            <span>个人中心</span>
          </div>
        </div>
        <div class="pf-hero">
          <div class="pf-avatar-wrap">
            <NAvatar
              round :size="64"
              :src="profile?.avatar || userStore.avatar"
              :fallback-src="`https://api.dicebear.com/9.x/initials/svg?seed=${userStore.nickname}`"
            />
            <button class="pf-avatar-edit">
              <Icon icon="lucide:camera" width="12" />
            </button>
          </div>
          <div class="pf-hero-body">
            <div class="pf-hero-name">
              {{ profile?.nickName || profile?.userName || userStore.nickname || '---' }}
            </div>
            <div class="pf-hero-sub">
              @{{ profile?.userName || '---' }}
              <template v-if="profile?.lastLoginTime">
                · {{ formatDate(profile.lastLoginTime) }}
              </template>
              <template v-if="profile?.lastLoginIp">
                ({{ profile.lastLoginIp }})
              </template>
            </div>
          </div>
        </div>
        <div class="pf-stat-grid">
          <div class="pf-stat-item">
            <div class="pf-stat-icon"><Icon icon="lucide:shield-check" width="16" /></div>
            <div class="pf-stat-body">
              <div class="pf-stat-label">两步验证</div>
              <div class="pf-stat-value">{{ profile?.twoFactorEnabled ? '已启用' : '未启用' }}</div>
            </div>
          </div>
          <div class="pf-stat-item">
            <div class="pf-stat-icon"><Icon icon="lucide:mail-check" width="16" /></div>
            <div class="pf-stat-body">
              <div class="pf-stat-label">邮箱验证</div>
              <div class="pf-stat-value">{{ profile?.emailVerified ? '已验证' : '未验证' }}</div>
            </div>
          </div>
          <div class="pf-stat-item">
            <div class="pf-stat-icon"><Icon icon="lucide:smartphone" width="16" /></div>
            <div class="pf-stat-body">
              <div class="pf-stat-label">手机验证</div>
              <div class="pf-stat-value">{{ profile?.phoneVerified ? '已验证' : '未验证' }}</div>
            </div>
          </div>
          <div class="pf-stat-item">
            <div class="pf-stat-icon"><Icon icon="lucide:monitor" width="16" /></div>
            <div class="pf-stat-body">
              <div class="pf-stat-label">在线设备</div>
              <div class="pf-stat-value">{{ sessionsLoaded ? sessions.length : '-' }}</div>
            </div>
          </div>
        </div>
      </div>

      <!-- ===== Tabs ===== -->
      <NTabs v-model:value="activeTab" type="line" animated class="pf-tabs">
        <!-- ==================== 个人资料 ==================== -->
        <NTabPane name="profile" tab="个人资料">
          <div class="pf-tab-body">
            <NGrid cols="1 m:2" responsive="screen" :x-gap="12" :y-gap="12">
              <NGridItem :span="2">
                <NCard :bordered="false" size="small" class="pf-card">
                  <template #header>
                    <div class="pf-card-header"><Icon icon="lucide:image" width="16" /><span>头像</span></div>
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
                      <NButton size="small" type="primary">上传</NButton>
                      <NButton size="small" quaternary>删除</NButton>
                    </NSpace>
                  </div>
                </NCard>
              </NGridItem>
              <NGridItem :span="2">
                <NCard :bordered="false" size="small" class="pf-card">
                  <template #header>
                    <div class="pf-card-header"><Icon icon="lucide:contact" width="16" /><span>基本信息</span></div>
                  </template>
                  <NForm ref="profileFormRef" :model="profileForm" label-placement="top">
                    <NGrid cols="1 m:2" responsive="screen" :x-gap="16" :y-gap="0">
                      <NGridItem>
                        <NFormItem label="用户名">
                          <NInput :value="profile?.userName" disabled placeholder="---">
                            <template #suffix><NTag size="tiny" :bordered="false">不可修改</NTag></template>
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
                          <NInput v-model:value="profileForm.email" placeholder="your@email.com">
                            <template #suffix>
                              <NTag v-if="profile?.emailVerified" type="success" size="tiny" :bordered="false">已验证</NTag>
                              <NTag v-else-if="profile?.email" type="warning" size="tiny" :bordered="false">未验证</NTag>
                            </template>
                          </NInput>
                        </NFormItem>
                      </NGridItem>
                      <NGridItem>
                        <NFormItem label="手机号码" path="phone">
                          <NInput v-model:value="profileForm.phone" placeholder="您的手机号">
                            <template #suffix>
                              <NTag v-if="profile?.phoneVerified" type="success" size="tiny" :bordered="false">已验证</NTag>
                              <NTag v-else-if="profile?.phone" type="warning" size="tiny" :bordered="false">未验证</NTag>
                            </template>
                          </NInput>
                        </NFormItem>
                      </NGridItem>
                      <NGridItem><NFormItem label="性别"><NSelect v-model:value="profileForm.gender" :options="genderOptions" /></NFormItem></NGridItem>
                      <NGridItem><NFormItem label="国家 / 地区"><NInput v-model:value="profileForm.country" placeholder="例如：中国" /></NFormItem></NGridItem>
                      <NGridItem><NFormItem label="语言"><NSelect v-model:value="profileForm.language" :options="languageOptions" /></NFormItem></NGridItem>
                      <NGridItem><NFormItem label="时区"><NSelect v-model:value="profileForm.timeZone" :options="timezoneOptions" /></NFormItem></NGridItem>
                      <NGridItem :span="2">
                        <NFormItem label="个人简介">
                          <NInput v-model:value="profileForm.remark" type="textarea" placeholder="介绍一下你自己..." :autosize="{ minRows: 3, maxRows: 6 }" :maxlength="200" show-count />
                        </NFormItem>
                      </NGridItem>
                    </NGrid>
                  </NForm>
                  <template #action>
                    <div class="pf-card-actions">
                      <NButton @click="syncProfileForm">取消</NButton>
                      <NButton type="primary" :loading="profileSaving" @click="saveProfile">
                        <template #icon><NIcon><Icon icon="lucide:save" /></NIcon></template>
                        保存更改
                      </NButton>
                    </div>
                  </template>
                </NCard>
              </NGridItem>
            </NGrid>
          </div>
        </NTabPane>

        <!-- ==================== 安全设置 ==================== -->
        <NTabPane name="security" tab="安全设置">
          <div class="pf-tab-body">
            <NGrid cols="1 m:2" responsive="screen" :x-gap="12" :y-gap="12">
              <!-- 修改密码 -->
              <NGridItem>
                <NCard :bordered="false" size="small" class="pf-card">
                  <template #header>
                    <div class="pf-card-header"><Icon icon="lucide:key-round" width="16" /><span>修改密码</span></div>
                  </template>
                  <template #header-extra>
                    <span v-if="profile?.lastPasswordChangeTime" class="pf-hint">上次修改：{{ formatDate(profile.lastPasswordChangeTime) }}</span>
                  </template>
                  <NForm ref="pwdFormRef" :model="pwdForm" :rules="pwdRules">
                    <NFormItem path="oldPassword" :show-label="false">
                      <NInput v-model:value="pwdForm.oldPassword" type="password" placeholder="当前密码" show-password-on="click" />
                    </NFormItem>
                    <NFormItem path="newPassword" :show-label="false">
                      <div class="pf-full">
                        <NInput v-model:value="pwdForm.newPassword" type="password" placeholder="新密码" show-password-on="click" />
                        <div v-if="pwdForm.newPassword" class="pf-strength">
                          <div class="pf-strength-bars">
                            <div v-for="i in 4" :key="i" class="pf-strength-bar" :style="{ background: i <= pwdStrength.score ? pwdStrength.color : 'var(--border-color)' }" />
                          </div>
                          <span class="pf-strength-label" :style="{ color: pwdStrength.color }">{{ pwdStrength.label }}</span>
                        </div>
                      </div>
                    </NFormItem>
                    <NFormItem path="confirmPassword" :show-label="false">
                      <NInput v-model:value="pwdForm.confirmPassword" type="password" placeholder="确认新密码" show-password-on="click" />
                    </NFormItem>
                  </NForm>
                  <template #action>
                    <NButton type="primary" block :loading="pwdSaving" @click="changePassword">更新密码</NButton>
                  </template>
                </NCard>
              </NGridItem>

              <!-- 两步验证 -->
              <NGridItem>
                <NCard :bordered="false" size="small" class="pf-card">
                  <template #header>
                    <div class="pf-card-header"><Icon icon="lucide:shield-check" width="16" /><span>两步验证 (TOTP)</span></div>
                  </template>
                  <template #header-extra>
                    <NSwitch v-if="!tfDisabling" :value="profile?.twoFactorEnabled" :loading="tfLoading" @update:value="onToggle2FA" />
                  </template>
                  <div class="pf-hint" style="margin-bottom: 8px">
                    使用 Google / Microsoft Authenticator 等应用生成一次性验证码
                  </div>
                  <NTag v-if="profile?.twoFactorEnabled && !tfDisabling" type="success" size="small" :bordered="false">
                    <template #icon><NIcon><Icon icon="lucide:check-circle-2" /></NIcon></template>
                    已启用
                  </NTag>

                  <!-- 初次启用 -->
                  <template v-if="tfSetup && !profile?.twoFactorEnabled">
                    <NDivider style="margin: 12px 0" />
                    <div class="pf-2fa-setup">
                      <div class="pf-2fa-qr">
                        <NQrCode :value="tfSetup.authenticatorUri" :size="160" error-correction-level="M" />
                        <span class="pf-hint">扫描二维码</span>
                      </div>
                      <div class="pf-2fa-manual">
                        <span class="pf-hint">无法扫码？手动输入密钥：</span>
                        <div class="pf-secret-row">
                          <code class="pf-secret">{{ tfSetup.sharedKey }}</code>
                          <NTooltip>
                            <template #trigger>
                              <NButton size="small" quaternary @click="copyToClipboard(tfSetup.sharedKey).then(() => message.success('已复制'))">
                                <template #icon><NIcon><Icon icon="lucide:copy" /></NIcon></template>
                              </NButton>
                            </template>
                            复制密钥
                          </NTooltip>
                        </div>
                        <span class="pf-hint" style="margin-top: 12px; display: block">输入 6 位验证码：</span>
                        <div class="pf-otp-row">
                          <NInputOtp v-model:value="tfCode" :length="6" @complete="handleEnable2FA" />
                          <NButton type="primary" :loading="tfLoading" @click="handleEnable2FA">启用</NButton>
                        </div>
                      </div>
                    </div>
                  </template>

                  <!-- 禁用流程 -->
                  <template v-if="tfDisabling">
                    <NDivider style="margin: 12px 0" />
                    <NAlert type="warning" :bordered="false" style="margin-bottom: 12px">请输入认证器当前的 6 位验证码以确认身份</NAlert>
                    <div class="pf-otp-row">
                      <NInputOtp v-model:value="tfCode" :length="6" @complete="handleDisable2FA" />
                      <NButton type="error" :loading="tfLoading" @click="handleDisable2FA">禁用</NButton>
                      <NButton quaternary @click="cancelDisable2FA">取消</NButton>
                    </div>
                  </template>
                </NCard>
              </NGridItem>

              <!-- 登录设备 -->
              <NGridItem :span="2">
                <NCard :bordered="false" size="small" class="pf-card">
                  <template #header>
                    <div class="pf-card-header"><Icon icon="lucide:monitor-smartphone" width="16" /><span>登录设备管理</span></div>
                  </template>
                  <template #header-extra>
                    <NSpace :size="8">
                      <NButton size="tiny" quaternary @click="loadSessions"><template #icon><NIcon><Icon icon="lucide:refresh-cw" /></NIcon></template></NButton>
                      <NButton size="tiny" @click="handleRevokeOthers">登出其他设备</NButton>
                    </NSpace>
                  </template>
                  <NSpin :show="sessionsLoading">
                    <NEmpty v-if="sessions.length === 0 && sessionsLoaded" description="暂无在线设备" />
                    <div v-else class="pf-list">
                      <div v-for="s in sessions" :key="s.sessionId" class="pf-list-item" :class="{ 'pf-list-item--active': s.isCurrent }">
                        <div class="pf-list-icon" :class="{ 'pf-list-icon--active': s.isCurrent }"><Icon :icon="deviceIcon(s.deviceType)" width="16" /></div>
                        <div class="pf-list-body">
                          <div class="pf-list-title">
                            {{ s.deviceName || s.browser || '未知设备' }}
                            <NTag v-if="s.isCurrent" type="success" size="tiny" :bordered="false">当前</NTag>
                          </div>
                          <div class="pf-list-desc">
                            {{ s.ipAddress }}
                            <template v-if="s.location">
                              · {{ s.location }}
                            </template>
                            <template v-if="s.operatingSystem">
                              · {{ s.operatingSystem }}
                            </template>
                            · {{ s.isCurrent ? '在线' : formatDate(s.lastActivityTime, 'MM-DD HH:mm') }}
                          </div>
                        </div>
                        <NPopconfirm v-if="!s.isCurrent" @positive-click="handleRevokeSession(s.sessionId)">
                          <template #trigger><NButton size="tiny" type="error" text>踢下线</NButton></template>
                          确定登出该设备？
                        </NPopconfirm>
                      </div>
                    </div>
                  </NSpin>
                </NCard>
              </NGridItem>

              <!-- 登录记录 -->
              <NGridItem>
                <NCard :bordered="false" size="small" class="pf-card">
                  <template #header>
                    <div class="pf-card-header"><Icon icon="lucide:history" width="16" /><span>登录记录</span></div>
                  </template>
                  <template #header-extra><span class="pf-hint">最近 30 天</span></template>
                  <NSpin :show="loginHistoryLoading">
                    <NEmpty v-if="loginHistory.length === 0" description="暂无登录记录">
                      <template #extra><span class="pf-hint">需要后端实现登录记录接口</span></template>
                    </NEmpty>
                    <div v-else class="pf-list">
                      <div v-for="(r, i) in loginHistory" :key="i" class="pf-list-item">
                        <div class="pf-list-icon" :class="r.status === 'success' ? 'pf-list-icon--active' : 'pf-list-icon--danger'">
                          <Icon :icon="r.status === 'success' ? 'lucide:log-in' : 'lucide:shield-x'" width="14" />
                        </div>
                        <div class="pf-list-body">
                          <div class="pf-list-title">{{ r.device || '未知设备' }}</div>
                          <div class="pf-list-desc">{{ r.ip }}<template v-if="r.location"> · {{ r.location }}</template> · {{ formatDate(r.time) }}</div>
                        </div>
                        <NTag :type="r.status === 'success' ? 'success' : 'error'" size="tiny" :bordered="false">{{ r.status === 'success' ? '成功' : '失败' }}</NTag>
                      </div>
                    </div>
                  </NSpin>
                </NCard>
              </NGridItem>

              <!-- 账号管理 -->
              <NGridItem>
                <NCard :bordered="false" size="small" class="pf-card">
                  <template #header>
                    <div class="pf-card-header"><Icon icon="lucide:user-cog" width="16" /><span>账号管理</span></div>
                  </template>
                  <div class="pf-list">
                    <div class="pf-list-item">
                      <div class="pf-list-icon"><Icon icon="lucide:snowflake" width="14" /></div>
                      <div class="pf-list-body">
                        <div class="pf-list-title">冻结账号</div>
                        <div class="pf-list-desc">进入只读模式，可随时解冻</div>
                      </div>
                      <NButton size="small" @click="handleFreezeAccount">冻结</NButton>
                    </div>
                    <div class="pf-list-item">
                      <div class="pf-list-icon pf-list-icon--danger"><Icon icon="lucide:user-x" width="14" /></div>
                      <div class="pf-list-body">
                        <div class="pf-list-title" style="color: var(--color-error)">停用账号</div>
                        <div class="pf-list-desc">无法登录，数据保留，需管理员恢复</div>
                      </div>
                      <NButton size="small" type="error" ghost @click="handleDeactivateAccount">停用</NButton>
                    </div>
                  </div>
                </NCard>
              </NGridItem>
            </NGrid>
          </div>
        </NTabPane>

        <!-- ==================== 通知偏好 ==================== -->
        <NTabPane name="notifications" tab="通知偏好">
          <div class="pf-tab-body">
            <NAlert type="info" :bordered="false">通知偏好设置将在后端接口就绪后生效。营销类通知您可随时关闭（GDPR 合规）。</NAlert>
            <NCard :bordered="false" size="small" class="pf-card">
              <template #header>
                <div class="pf-card-header"><Icon icon="lucide:bell-ring" width="16" /><span>通知渠道</span></div>
              </template>
              <div class="pf-list">
                <div v-for="ch in notifyChannels" :key="ch.key" class="pf-list-item">
                  <div class="pf-list-icon"><Icon :icon="ch.icon" width="16" /></div>
                  <div class="pf-list-body">
                    <div class="pf-list-title">{{ ch.label }} <NTag v-if="ch.marketing" size="tiny" :bordered="false">营销</NTag></div>
                    <div class="pf-list-desc">{{ ch.desc }}</div>
                  </div>
                  <NSwitch v-model:value="ch.enabled" />
                </div>
              </div>
            </NCard>
          </div>
        </NTabPane>

        <!-- ==================== 开发者设置 ==================== -->
        <NTabPane name="developer" tab="开发者设置">
          <div class="pf-tab-body">
            <NAlert type="warning" :bordered="false">API 凭证功能需配合后端实现，当前为界面预览。Secret 仅在创建时显示一次。</NAlert>

            <NCard :bordered="false" size="small" class="pf-card">
              <template #header><div class="pf-card-header"><Icon icon="lucide:key" width="16" /><span>API 凭证</span></div></template>
              <template #header-extra>
                <NButton size="small" type="primary" @click="handleCreateCredential"><template #icon><NIcon><Icon icon="lucide:plus" /></NIcon></template>创建凭证</NButton>
              </template>
              <NEmpty v-if="apiCredentials.length === 0" description="暂无 API 凭证">
                <template #extra><NButton size="small" @click="handleCreateCredential">创建第一个凭证</NButton></template>
              </NEmpty>
              <div v-else class="pf-list">
                <div v-for="cred in apiCredentials" :key="cred.appId" class="pf-list-item" style="flex-wrap: wrap;">
                  <div class="pf-list-body" style="width: 100%;">
                    <div class="pf-list-title" style="font-family: monospace">{{ cred.appId }}</div>
                    <div class="pf-list-desc">创建于 {{ formatDate(cred.createdAt) }}<template v-if="cred.lastUsedAt"> · 最后使用 {{ formatDate(cred.lastUsedAt) }}</template></div>
                  </div>
                  <NInputGroup style="margin-top: 8px">
                    <NInput :value="cred.appKey" readonly size="small" />
                    <NButton size="small" @click="copyToClipboard(cred.appKey).then(() => message.success('已复制'))"><template #icon><NIcon><Icon icon="lucide:copy" /></NIcon></template></NButton>
                  </NInputGroup>
                  <NSpace :size="4" style="margin-left: auto; margin-top: 8px">
                    <NTooltip><template #trigger><NButton size="tiny" quaternary @click="handleRotateSecret(cred.appId)"><template #icon><NIcon><Icon icon="lucide:rotate-ccw" /></NIcon></template></NButton></template>滚动密钥</NTooltip>
                    <NTooltip><template #trigger><NButton size="tiny" quaternary type="error" @click="handleDeleteCredential(cred.appId)"><template #icon><NIcon><Icon icon="lucide:trash-2" /></NIcon></template></NButton></template>删除凭证</NTooltip>
                  </NSpace>
                </div>
              </div>
              <template v-if="newSecret">
                <NDivider />
                <NAlert type="warning" title="请立即保存 API Secret" :bordered="false">
                  <p style="margin-bottom: 8px; font-size: 13px">此密钥仅显示一次：</p>
                  <NInputGroup>
                    <NInput :value="newSecret" readonly type="password" show-password-on="click" />
                    <NButton @click="copyToClipboard(newSecret).then(() => message.success('已复制'))"><template #icon><NIcon><Icon icon="lucide:copy" /></NIcon></template></NButton>
                  </NInputGroup>
                </NAlert>
              </template>
            </NCard>

            <NGrid cols="1 m:2" responsive="screen" :x-gap="12" :y-gap="12">
              <NGridItem>
                <NCard :bordered="false" size="small" class="pf-card">
                  <template #header><div class="pf-card-header"><Icon icon="lucide:file-signature" width="16" /><span>签名算法</span></div></template>
                  <span class="pf-hint" style="display: block; margin-bottom: 8px">修改后需同步更新客户端配置</span>
                  <NSelect v-model:value="signAlgorithm" :options="signAlgorithmOptions" />
                </NCard>
              </NGridItem>
              <NGridItem>
                <NCard :bordered="false" size="small" class="pf-card">
                  <template #header><div class="pf-card-header"><Icon icon="lucide:shield-ban" width="16" /><span>IP 白名单</span></div></template>
                  <span class="pf-hint" style="display: block; margin-bottom: 8px">每行一个 IP，留空则不限制</span>
                  <NInput v-model:value="ipWhitelist" type="textarea" placeholder="192.168.1.1&#10;10.0.0.0/24" :autosize="{ minRows: 3, maxRows: 6 }" />
                  <div style="display: flex; justify-content: flex-end; margin-top: 8px">
                    <NButton type="primary" size="small" @click="handleSaveIpWhitelist">保存</NButton>
                  </div>
                </NCard>
              </NGridItem>
            </NGrid>
          </div>
        </NTabPane>
      </NTabs>
    </NSpin>
  </div>
</template>

<style scoped>
.pf-page {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

/* ===== Banner ===== */
.pf-banner {
  position: relative;
  overflow: hidden;
  padding: 16px 20px;
  border-radius: var(--radius);
  background:
    radial-gradient(circle at 85% 0%, hsl(var(--primary) / 16%), transparent 45%),
    linear-gradient(135deg, hsl(var(--accent)), hsl(var(--muted)));
  border: 1px solid var(--border-color);
}

.pf-banner-glow {
  position: absolute;
  right: -60px;
  top: -60px;
  width: 180px;
  height: 180px;
  border-radius: 50%;
  background: hsl(var(--primary) / 12%);
  filter: blur(20px);
  pointer-events: none;
}

.pf-banner-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 14px;
}

.pf-banner-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 15px;
  font-weight: 600;
  color: var(--text-primary);
}

.pf-hero {
  display: flex;
  align-items: center;
  gap: 14px;
  margin-bottom: 14px;
}

.pf-avatar-wrap {
  position: relative;
  flex-shrink: 0;
}

.pf-avatar-edit {
  position: absolute;
  bottom: -2px;
  right: -2px;
  width: 22px;
  height: 22px;
  border-radius: 50%;
  border: 2px solid var(--bg-surface, #fff);
  background: hsl(var(--primary));
  color: #fff;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: transform 0.15s;
}

.pf-avatar-edit:hover {
  transform: scale(1.1);
}

.pf-hero-body {
  min-width: 0;
}

.pf-hero-name {
  font-size: 20px;
  font-weight: 700;
  color: var(--text-primary);
  line-height: 1.2;
}

.pf-hero-sub {
  font-size: 13px;
  color: var(--text-secondary);
  margin-top: 4px;
}

/* ===== Stat Grid ===== */
.pf-stat-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 10px;
}

.pf-stat-item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px 12px;
  border-radius: var(--radius);
  background: var(--bg-surface);
  border: 1px solid var(--border-color);
  transition: border-color 0.2s;
}

.pf-stat-item:hover {
  border-color: hsl(var(--primary) / 30%);
}

.pf-stat-icon {
  width: 32px;
  height: 32px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  background: hsl(var(--primary) / 10%);
  color: hsl(var(--primary));
}

.pf-stat-body {
  min-width: 0;
}

.pf-stat-label {
  font-size: 12px;
  color: var(--text-secondary);
}

.pf-stat-value {
  font-size: 13px;
  font-weight: 600;
  color: var(--text-primary);
  margin-top: 1px;
}

/* ===== Tabs ===== */
.pf-tabs {
  padding: 0 24px;
}

.pf-tabs :deep(.n-tabs-nav) {
  padding-top: 0;
}

/* 去掉 NCard action 区域默认阴影和边框 */
.pf-tabs :deep(.n-card__action) {
  box-shadow: none !important;
  border-top: none !important;
  background: transparent;
}

.pf-tab-body {
  display: flex;
  flex-direction: column;
  gap: 12px;
  padding-top: 12px;
}

/* ===== Card ===== */
.pf-card {
  background: var(--bg-card);
}

.pf-card-header {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 14px;
  font-weight: 600;
  color: var(--text-primary);
}

.pf-card-actions {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
}

/* ===== Shared ===== */
.pf-hint {
  font-size: 12px;
  color: var(--text-secondary);
}

.pf-full {
  width: 100%;
}

/* ===== Avatar Section ===== */
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

/* ===== Password Strength ===== */
.pf-strength {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-top: 8px;
}

.pf-strength-bars {
  display: flex;
  flex: 1;
  gap: 4px;
}

.pf-strength-bar {
  height: 4px;
  flex: 1;
  border-radius: 2px;
  transition: background 0.2s;
}

.pf-strength-label {
  font-size: 12px;
  flex-shrink: 0;
}

/* ===== 2FA Setup ===== */
.pf-2fa-setup {
  display: flex;
  gap: 16px;
}

.pf-2fa-qr {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 6px;
  padding: 10px;
  border-radius: var(--radius);
  border: 1px solid var(--border-color);
  flex-shrink: 0;
}

.pf-2fa-manual {
  flex: 1;
  min-width: 0;
}

.pf-secret-row {
  display: flex;
  align-items: center;
  gap: 6px;
  margin-top: 6px;
}

.pf-secret {
  flex: 1;
  word-break: break-all;
  padding: 6px 10px;
  border-radius: 6px;
  background: hsl(var(--muted));
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
  font-size: 12px;
}

.pf-otp-row {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-top: 8px;
}

/* ===== List ===== */
.pf-list {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.pf-list-item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 8px 10px;
  border-radius: var(--radius);
  border: 1px solid var(--border-color);
  background: var(--bg-surface);
  transition: border-color 0.2s;
}

.pf-list-item:hover {
  border-color: hsl(var(--primary) / 25%);
}

.pf-list-item--active {
  border-color: hsl(var(--primary) / 30%);
  background: hsl(var(--primary) / 4%);
}

.pf-list-icon {
  width: 32px;
  height: 32px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  background: hsl(var(--muted));
  color: var(--text-secondary);
}

.pf-list-icon--active {
  background: hsl(var(--primary) / 10%);
  color: hsl(var(--primary));
}

.pf-list-icon--danger {
  background: hsl(0 80% 55% / 10%);
  color: var(--color-error);
}

.pf-list-body {
  flex: 1;
  min-width: 0;
}

.pf-list-title {
  font-size: 13px;
  font-weight: 500;
  color: var(--text-primary);
  display: flex;
  align-items: center;
  gap: 6px;
}

.pf-list-desc {
  font-size: 12px;
  color: var(--text-secondary);
  margin-top: 1px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

/* ===== Responsive ===== */
@media (max-width: 900px) {
  .pf-stat-grid {
    grid-template-columns: repeat(2, 1fr);
  }

  .pf-2fa-setup {
    flex-direction: column;
  }
}

@media (max-width: 640px) {
  .pf-tabs {
    padding: 0 12px;
  }

  .pf-banner {
    padding: 12px 14px;
  }

  .pf-stat-grid {
    grid-template-columns: 1fr 1fr;
    gap: 8px;
  }

  .pf-hero-name {
    font-size: 18px;
  }
}
</style>
