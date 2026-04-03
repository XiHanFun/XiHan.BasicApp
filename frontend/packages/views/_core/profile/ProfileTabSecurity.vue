<script lang="ts" setup>
import type { FormInst, FormRules } from 'naive-ui'
import type { UserProfile, UserSessionItem } from '~/types'
import {
  NAlert,
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
  NInputOtp,
  NPopconfirm,
  NQrCode,
  NSpace,
  NSpin,
  NSwitch,
  NTag,
  NTooltip,
  useDialog,
  useMessage,
} from 'naive-ui'
import { computed, h, onMounted, ref } from 'vue'
import {
  changePasswordApi,
  deactivateAccountApi,
  deleteAccountApi,
  disable2FAApi,
  enable2FAApi,
  getSessionsApi,
  revokeOtherSessionsApi,
  revokeSessionApi,
  setup2FAApi,
} from '@/api'
import { Icon } from '~/iconify'
import { copyToClipboard, formatDate } from '~/utils'

const props = defineProps<{ profile: UserProfile | null }>()
const emit = defineEmits<{ updated: [] }>()

const message = useMessage()
const dialog = useDialog()

// ==================== 密码 ====================

const pwdFormRef = ref<FormInst | null>(null)
const pwdSaving = ref(false)
const pwdForm = ref({
  oldPassword: '',
  newPassword: '',
  confirmPassword: '',
})
const pwdRules: FormRules = {
  oldPassword: [{ required: true, message: '请输入当前密码', trigger: 'blur' }],
  newPassword: [
    { required: true, message: '请输入新密码', trigger: 'blur' },
    { min: 6, max: 32, message: '密码长度 6～32 位', trigger: 'blur' },
  ],
  confirmPassword: [
    { required: true, message: '请确认新密码', trigger: 'blur' },
    {
      validator: (_: unknown, v: string) => v === pwdForm.value.newPassword,
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
  if (!props.profile)
    return
  pwdSaving.value = true
  try {
    await changePasswordApi({
      userId: props.profile.userId,
      oldPassword: pwdForm.value.oldPassword,
      newPassword: pwdForm.value.newPassword,
    })
    message.success('密码已更新')
    pwdForm.value = {
      oldPassword: '',
      newPassword: '',
      confirmPassword: '',
    }
    emit('updated')
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '密码修改失败')
  }
  finally {
    pwdSaving.value = false
  }
}

// ==================== 2FA ====================

const tfLoading = ref(false)
const tfSetup = ref<{ sharedKey: string, authenticatorUri: string } | null>(null)
const tfCode = ref<string[]>([])
const tfCodeStr = computed(() => tfCode.value.join(''))
const tfDisabling = ref(false)

async function handleSetup2FA() {
  tfLoading.value = true
  try {
    tfSetup.value = await setup2FAApi()
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '初始化失败')
  }
  finally {
    tfLoading.value = false
  }
}

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
    emit('updated')
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '启用失败')
  }
  finally {
    tfLoading.value = false
  }
}

async function handleDisable2FA() {
  if (!tfCodeStr.value || tfCodeStr.value.length < 6) {
    message.warning('请输入完整的 6 位验证码')
    return
  }
  tfLoading.value = true
  try {
    await disable2FAApi(tfCodeStr.value)
    message.success('双因素认证已禁用')
    tfCode.value = []
    tfDisabling.value = false
    emit('updated')
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '禁用失败')
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
    tfDisabling.value = true
    tfCode.value = []
  }
}

function cancelDisable2FA() {
  tfDisabling.value = false
  tfCode.value = []
}

// ==================== 会话 ====================

const sessionsLoading = ref(false)
const sessions = ref<UserSessionItem[]>([])
const sessionsLoaded = ref(false)

defineExpose({ sessions, sessionsLoaded })

async function loadSessions() {
  sessionsLoading.value = true
  try {
    sessions.value = await getSessionsApi()
    sessionsLoaded.value = true
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '加载失败')
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
  catch (e: unknown) {
    message.error((e as Error)?.message || '操作失败')
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
      catch (e: unknown) {
        message.error((e as Error)?.message || '操作失败')
      }
    },
  })
}

function deviceIcon(t: number) {
  const map: Record<number, string> = {
    1: 'lucide:globe',
    2: 'lucide:smartphone',
    3: 'lucide:monitor',
    4: 'lucide:tablet',
  }
  return map[t] || 'lucide:help-circle'
}

// ==================== 登录记录 ====================

interface LoginRecord {
  time: string
  ip: string
  location?: string
  device?: string
  status: 'success' | 'failed'
}
const loginHistory = ref<LoginRecord[]>([])
const loginHistoryLoading = ref(false)

// ==================== 账号管理 ====================

const accountPassword = ref('')
const accountActionLoading = ref(false)

function handleDeactivateAccount() {
  accountPassword.value = ''
  dialog.warning({
    title: '停用账号',
    content: () => h('div', [
      h('p', { style: 'margin-bottom: 12px' }, '停用后您将无法登录，但数据会保留。请输入密码确认：'),
      h(NInput, {
        'type': 'password',
        'value': accountPassword.value,
        'placeholder': '请输入当前密码',
        'showPasswordOn': 'click',
        'onUpdate:value': (v: string) => { accountPassword.value = v },
      }),
    ]),
    positiveText: '确认停用',
    negativeText: '取消',
    onPositiveClick: async () => {
      if (!accountPassword.value) {
        message.warning('请输入密码')
        return false
      }
      accountActionLoading.value = true
      try {
        await deactivateAccountApi(accountPassword.value)
        message.success('账号已停用，即将退出登录')
        setTimeout(() => {
          window.location.href = '/auth/login'
        }, 1500)
      }
      catch (e: unknown) {
        message.error((e as Error)?.message || '停用失败')
        return false
      }
      finally {
        accountActionLoading.value = false
      }
    },
  })
}

function handleDeleteAccount() {
  accountPassword.value = ''
  dialog.error({
    title: '注销账号',
    content: () => h('div', [
      h('p', { style: 'margin-bottom: 12px; color: var(--color-error)' }, '注销后您的所有数据将被永久删除且无法恢复！请输入密码确认：'),
      h(NInput, {
        'type': 'password',
        'value': accountPassword.value,
        'placeholder': '请输入当前密码',
        'showPasswordOn': 'click',
        'onUpdate:value': (v: string) => { accountPassword.value = v },
      }),
    ]),
    positiveText: '确认注销',
    negativeText: '取消',
    onPositiveClick: async () => {
      if (!accountPassword.value) {
        message.warning('请输入密码')
        return false
      }
      accountActionLoading.value = true
      try {
        await deleteAccountApi(accountPassword.value)
        message.success('账号已注销，即将退出')
        setTimeout(() => {
          window.location.href = '/auth/login'
        }, 1500)
      }
      catch (e: unknown) {
        message.error((e as Error)?.message || '注销失败')
        return false
      }
      finally {
        accountActionLoading.value = false
      }
    },
  })
}

// ==================== 生命周期 ====================

onMounted(() => {
  loadSessions()
})
</script>

<template>
  <div class="pf-tab-body">
    <NGrid cols="1 m:2" responsive="screen" :x-gap="12" :y-gap="12">
      <!-- 修改密码 -->
      <NGridItem>
        <NCard :bordered="false" size="small" class="pf-card">
          <template #header>
            <div class="pf-card-header">
              <Icon icon="lucide:key-round" width="16" />
              <span>修改密码</span>
            </div>
          </template>
          <template #header-extra>
            <span v-if="profile?.lastPasswordChangeTime" class="pf-hint">
              上次修改：{{ formatDate(profile.lastPasswordChangeTime) }}
            </span>
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
            <NButton type="primary" block :loading="pwdSaving" @click="changePassword">
              更新密码
            </NButton>
          </template>
        </NCard>
      </NGridItem>

      <!-- 两步验证 -->
      <NGridItem>
        <NCard :bordered="false" size="small" class="pf-card">
          <template #header>
            <div class="pf-card-header">
              <Icon icon="lucide:shield-check" width="16" />
              <span>两步验证 (TOTP)</span>
            </div>
          </template>
          <template #header-extra>
            <NSwitch v-if="!tfDisabling" :value="profile?.twoFactorEnabled" :loading="tfLoading" @update:value="onToggle2FA" />
          </template>
          <div class="pf-hint" style="margin-bottom: 8px">
            使用 Google / Microsoft Authenticator 等应用生成一次性验证码
          </div>
          <NTag v-if="profile?.twoFactorEnabled && !tfDisabling" type="success" size="small" :bordered="false">
            <template #icon>
              <NIcon>
                <Icon icon="lucide:check-circle-2" />
              </NIcon>
            </template>
            已启用
          </NTag>

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
                        <template #icon>
                          <NIcon>
                            <Icon icon="lucide:copy" />
                          </NIcon>
                        </template>
                      </NButton>
                    </template>
                    复制密钥
                  </NTooltip>
                </div>
                <span class="pf-hint" style="margin-top: 12px; display: block">输入 6 位验证码：</span>
                <div class="pf-otp-row">
                  <NInputOtp v-model:value="tfCode" :length="6" @complete="handleEnable2FA" />
                  <NButton type="primary" :loading="tfLoading" @click="handleEnable2FA">
                    启用
                  </NButton>
                </div>
              </div>
            </div>
          </template>

          <template v-if="tfDisabling">
            <NDivider style="margin: 12px 0" />
            <NAlert type="warning" :bordered="false" style="margin-bottom: 12px">
              请输入认证器当前的 6 位验证码以确认身份
            </NAlert>
            <div class="pf-otp-row">
              <NInputOtp v-model:value="tfCode" :length="6" @complete="handleDisable2FA" />
              <NButton type="error" :loading="tfLoading" @click="handleDisable2FA">
                禁用
              </NButton>
              <NButton quaternary @click="cancelDisable2FA">
                取消
              </NButton>
            </div>
          </template>
        </NCard>
      </NGridItem>

      <!-- 登录设备 -->
      <NGridItem :span="2">
        <NCard :bordered="false" size="small" class="pf-card">
          <template #header>
            <div class="pf-card-header">
              <Icon icon="lucide:monitor-smartphone" width="16" />
              <span>登录设备管理</span>
            </div>
          </template>
          <template #header-extra>
            <NSpace :size="8">
              <NButton size="tiny" quaternary @click="loadSessions">
                <template #icon>
                  <NIcon>
                    <Icon icon="lucide:refresh-cw" />
                  </NIcon>
                </template>
              </NButton>
              <NButton size="tiny" @click="handleRevokeOthers">
                登出其他设备
              </NButton>
            </NSpace>
          </template>
          <NSpin :show="sessionsLoading">
            <NEmpty v-if="sessions.length === 0 && sessionsLoaded" description="暂无在线设备" />
            <div v-else class="pf-list">
              <div v-for="s in sessions" :key="s.sessionId" class="pf-list-item" :class="{ 'pf-list-item--active': s.isCurrent }">
                <div class="pf-list-icon" :class="{ 'pf-list-icon--active': s.isCurrent }">
                  <Icon :icon="deviceIcon(s.deviceType)" width="16" />
                </div>
                <div class="pf-list-body">
                  <div class="pf-list-title">
                    {{ s.deviceName || s.browser || '未知设备' }}
                    <NTag v-if="s.isCurrent" type="success" size="tiny" :bordered="false">
                      当前
                    </NTag>
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
      </NGridItem>

      <!-- 登录记录 -->
      <NGridItem>
        <NCard :bordered="false" size="small" class="pf-card">
          <template #header>
            <div class="pf-card-header">
              <Icon icon="lucide:history" width="16" />
              <span>登录记录</span>
            </div>
          </template>
          <template #header-extra>
            <span class="pf-hint">最近 30 天</span>
          </template>
          <NSpin :show="loginHistoryLoading">
            <NEmpty v-if="loginHistory.length === 0" description="暂无登录记录">
              <template #extra>
                <span class="pf-hint">需要后端实现登录记录接口</span>
              </template>
            </NEmpty>
            <div v-else class="pf-list">
              <div v-for="(r, i) in loginHistory" :key="i" class="pf-list-item">
                <div class="pf-list-icon" :class="r.status === 'success' ? 'pf-list-icon--active' : 'pf-list-icon--danger'">
                  <Icon :icon="r.status === 'success' ? 'lucide:log-in' : 'lucide:shield-x'" width="14" />
                </div>
                <div class="pf-list-body">
                  <div class="pf-list-title">
                    {{ r.device || '未知设备' }}
                  </div>
                  <div class="pf-list-desc">
                    {{ r.ip }}
                    <template v-if="r.location">
                      · {{ r.location }}
                    </template>
                    · {{ formatDate(r.time) }}
                  </div>
                </div>
                <NTag :type="r.status === 'success' ? 'success' : 'error'" size="tiny" :bordered="false">
                  {{ r.status === 'success' ? '成功' : '失败' }}
                </NTag>
              </div>
            </div>
          </NSpin>
        </NCard>
      </NGridItem>

      <!-- 账号管理 -->
      <NGridItem>
        <NCard :bordered="false" size="small" class="pf-card">
          <template #header>
            <div class="pf-card-header">
              <Icon icon="lucide:user-cog" width="16" />
              <span>账号管理</span>
            </div>
          </template>
          <div class="pf-list">
            <div class="pf-list-item">
              <div class="pf-list-icon">
                <Icon icon="lucide:user-x" width="14" />
              </div>
              <div class="pf-list-body">
                <div class="pf-list-title">
                  停用账号
                </div>
                <div class="pf-list-desc">
                  无法登录，数据保留，需管理员恢复
                </div>
              </div>
              <NButton size="small" type="warning" ghost @click="handleDeactivateAccount">
                停用
              </NButton>
            </div>
            <div class="pf-list-item">
              <div class="pf-list-icon pf-list-icon--danger">
                <Icon icon="lucide:trash-2" width="14" />
              </div>
              <div class="pf-list-body">
                <div class="pf-list-title" style="color: var(--color-error)">
                  注销账号
                </div>
                <div class="pf-list-desc">
                  永久删除所有数据，此操作不可恢复
                </div>
              </div>
              <NButton size="small" type="error" ghost @click="handleDeleteAccount">
                注销
              </NButton>
            </div>
          </div>
        </NCard>
      </NGridItem>
    </NGrid>
  </div>
</template>

<style src="./profile-shared.css" />

<style scoped>
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

@media (max-width: 900px) {
  .pf-2fa-setup {
    flex-direction: column;
  }
}
</style>
