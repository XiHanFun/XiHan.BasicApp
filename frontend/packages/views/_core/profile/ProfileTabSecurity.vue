<script lang="ts" setup>
import type { FormInst, FormRules } from 'naive-ui'
import type { ExternalLoginItem, LoginLogItem, UserProfile, UserSessionItem } from '~/types'
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
  NPagination,
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
  getLinkedAccountsApi,
  getLoginLogsApi,
  getSessionsApi,
  revokeOtherSessionsApi,
  revokeSessionApi,
  send2FASetupCodeApi,
  setup2FAApi,
  unlinkAccountApi,
} from '@/api'
import { Icon } from '~/iconify'
import { copyToClipboard, formatDate } from '~/utils'

const props = defineProps<{ profile: UserProfile | null }>()
const emit = defineEmits<{ updated: [] }>()

const message = useMessage()
const dialog = useDialog()

// ==================== 2FA flags 常量（与后端 [Flags] 枚举对应） ====================

const TF_TOTP = 1
const TF_EMAIL = 2
const TF_PHONE = 4

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

// ==================== 2FA 多方式独立开关 ====================

const tfMethods = computed(() => props.profile?.twoFactorMethod ?? 0)
const hasTotpEnabled = computed(() => (tfMethods.value & TF_TOTP) !== 0)
const hasEmailEnabled = computed(() => (tfMethods.value & TF_EMAIL) !== 0)
const hasPhoneEnabled = computed(() => (tfMethods.value & TF_PHONE) !== 0)
const enabledCount = computed(() => {
  let c = 0
  if (hasTotpEnabled.value)
    c++
  if (hasEmailEnabled.value)
    c++
  if (hasPhoneEnabled.value)
    c++
  return c
})

const tfLoading = ref(false)

// TOTP 设置
const tfTotpSetup = ref<{ sharedKey: string, authenticatorUri: string } | null>(null)
const tfTotpSettingUp = ref(false)
const tfTotpCode = ref<string[]>([])
const tfTotpCodeStr = computed(() => tfTotpCode.value.join(''))

// 邮箱/手机 设置
const tfEmailSettingUp = ref(false)
const tfPhoneSettingUp = ref(false)
const tfEmailCode = ref<string[]>([])
const tfPhoneCode = ref<string[]>([])
const tfEmailCodeStr = computed(() => tfEmailCode.value.join(''))
const tfPhoneCodeStr = computed(() => tfPhoneCode.value.join(''))

// 禁用用验证码
const tfDisableTarget = ref(0)
const tfDisableCode = ref<string[]>([])
const tfDisableCodeStr = computed(() => tfDisableCode.value.join(''))

// 倒计时
const tfCodeCountdown = ref(0)
let tfCountdownTimer: ReturnType<typeof setInterval> | null = null

function startTfCountdown(seconds: number) {
  tfCodeCountdown.value = seconds
  if (tfCountdownTimer)
    clearInterval(tfCountdownTimer)
  tfCountdownTimer = setInterval(() => {
    tfCodeCountdown.value--
    if (tfCodeCountdown.value <= 0) {
      clearInterval(tfCountdownTimer!)
      tfCountdownTimer = null
    }
  }, 1000)
}

function clearCountdown() {
  if (tfCountdownTimer) {
    clearInterval(tfCountdownTimer)
    tfCountdownTimer = null
  }
  tfCodeCountdown.value = 0
}

// -- TOTP 启用流程 --

async function startTotpSetup() {
  tfTotpSettingUp.value = true
  tfTotpCode.value = []
  tfTotpSetup.value = null
  tfLoading.value = true
  try {
    tfTotpSetup.value = await setup2FAApi()
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '初始化失败')
    tfTotpSettingUp.value = false
  }
  finally {
    tfLoading.value = false
  }
}

async function confirmEnableTotp() {
  if (!tfTotpCodeStr.value || tfTotpCodeStr.value.length < 6) {
    message.warning('请输入完整的 6 位验证码')
    return
  }
  tfLoading.value = true
  try {
    await enable2FAApi(TF_TOTP, tfTotpCodeStr.value)
    message.success('TOTP 已启用')
    tfTotpSetup.value = null
    tfTotpCode.value = []
    tfTotpSettingUp.value = false
    emit('updated')
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '启用失败')
  }
  finally {
    tfLoading.value = false
  }
}

function cancelTotpSetup() {
  tfTotpSettingUp.value = false
  tfTotpSetup.value = null
  tfTotpCode.value = []
}

// -- 邮箱/手机 启用流程 --

async function sendSetupCode(method: number) {
  tfLoading.value = true
  try {
    const res = await send2FASetupCodeApi(method)
    message.success(method === TF_EMAIL ? '验证码已发送至邮箱' : '验证码已发送至手机')
    startTfCountdown(res.expiresInSeconds > 60 ? 60 : res.expiresInSeconds)
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '发送失败')
  }
  finally {
    tfLoading.value = false
  }
}

function startEmailSetup() {
  tfEmailSettingUp.value = true
  tfEmailCode.value = []
  clearCountdown()
  sendSetupCode(TF_EMAIL)
}

function startPhoneSetup() {
  tfPhoneSettingUp.value = true
  tfPhoneCode.value = []
  clearCountdown()
  sendSetupCode(TF_PHONE)
}

async function confirmEnableEmail() {
  if (!tfEmailCodeStr.value || tfEmailCodeStr.value.length < 6) {
    message.warning('请输入完整的 6 位验证码')
    return
  }
  tfLoading.value = true
  try {
    await enable2FAApi(TF_EMAIL, tfEmailCodeStr.value)
    message.success('邮箱两步验证已启用')
    tfEmailSettingUp.value = false
    tfEmailCode.value = []
    clearCountdown()
    emit('updated')
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '启用失败')
  }
  finally {
    tfLoading.value = false
  }
}

async function confirmEnablePhone() {
  if (!tfPhoneCodeStr.value || tfPhoneCodeStr.value.length < 6) {
    message.warning('请输入完整的 6 位验证码')
    return
  }
  tfLoading.value = true
  try {
    await enable2FAApi(TF_PHONE, tfPhoneCodeStr.value)
    message.success('手机两步验证已启用')
    tfPhoneSettingUp.value = false
    tfPhoneCode.value = []
    clearCountdown()
    emit('updated')
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '启用失败')
  }
  finally {
    tfLoading.value = false
  }
}

function cancelEmailSetup() {
  tfEmailSettingUp.value = false
  tfEmailCode.value = []
  clearCountdown()
}

function cancelPhoneSetup() {
  tfPhoneSettingUp.value = false
  tfPhoneCode.value = []
  clearCountdown()
}

// -- 禁用流程 --

function startDisable(method: number) {
  tfDisableTarget.value = method
  tfDisableCode.value = []
  clearCountdown()
  if (method !== TF_TOTP) {
    sendDisableCode(method)
  }
}

async function sendDisableCode(method: number) {
  tfLoading.value = true
  try {
    const res = await send2FASetupCodeApi(method)
    message.success('验证码已发送')
    startTfCountdown(res.expiresInSeconds > 60 ? 60 : res.expiresInSeconds)
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '发送失败')
  }
  finally {
    tfLoading.value = false
  }
}

async function confirmDisable() {
  if (!tfDisableCodeStr.value || tfDisableCodeStr.value.length < 6) {
    message.warning('请输入完整的 6 位验证码')
    return
  }
  tfLoading.value = true
  try {
    await disable2FAApi(tfDisableTarget.value, tfDisableCodeStr.value)
    message.success('已禁用')
    tfDisableTarget.value = 0
    tfDisableCode.value = []
    clearCountdown()
    emit('updated')
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '禁用失败')
  }
  finally {
    tfLoading.value = false
  }
}

function cancelDisable() {
  tfDisableTarget.value = 0
  tfDisableCode.value = []
  clearCountdown()
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

// ==================== 第三方账号 ====================

const linkedAccounts = ref<ExternalLoginItem[]>([])
const linkedLoading = ref(false)
const linkedLoaded = ref(false)

async function loadLinkedAccounts() {
  linkedLoading.value = true
  try {
    linkedAccounts.value = await getLinkedAccountsApi()
    linkedLoaded.value = true
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '加载失败')
  }
  finally {
    linkedLoading.value = false
  }
}

async function handleUnlinkAccount(provider: string) {
  try {
    await unlinkAccountApi(provider)
    message.success('已解除绑定')
    await loadLinkedAccounts()
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '操作失败')
  }
}

function providerIcon(name: string) {
  const map: Record<string, string> = {
    github: 'simple-icons:github',
    google: 'simple-icons:google',
    microsoft: 'simple-icons:microsoft',
    qq: 'simple-icons:tencentqq',
    wechat: 'simple-icons:wechat',
    weibo: 'simple-icons:sinaweibo',
  }
  return map[name.toLowerCase()] || 'lucide:link'
}

function handleLinkNewAccount(_provider: string) {
  message.info('绑定功能需要配合 OAuth 回调端点实现')
}

// ==================== 登录日志 ====================

const loginLogs = ref<LoginLogItem[]>([])
const loginLogTotal = ref(0)
const loginLogPage = ref(1)
const loginLogLoading = ref(false)

const loginResultLabel: Record<number, string> = {
  0: '成功',
  1: '密码错误',
  2: '账号锁定',
  3: '账号禁用',
  4: '需要两步验证',
}

async function loadLoginLogs(page = 1) {
  loginLogLoading.value = true
  try {
    const res = await getLoginLogsApi(page, 10)
    loginLogs.value = res.items
    loginLogTotal.value = res.total
    loginLogPage.value = page
  }
  catch {
    // 静默处理
  }
  finally {
    loginLogLoading.value = false
  }
}

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
        'onUpdate:value': (v: string) => {
          accountPassword.value = v
        },
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
        'onUpdate:value': (v: string) => {
          accountPassword.value = v
        },
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
  loadLinkedAccounts()
  loadLoginLogs()
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
              <span>两步验证</span>
            </div>
          </template>
          <template #header-extra>
            <NTag v-if="enabledCount > 0" type="success" size="small" :bordered="false">
              {{ enabledCount }} 种已启用
            </NTag>
          </template>

          <div class="pf-hint" style="margin-bottom: 12px">
            您可以同时启用多种两步验证方式，登录时自由选择使用哪种
          </div>

          <!-- TOTP 方式 -->
          <div class="pf-2fa-method">
            <div class="pf-2fa-method-header">
              <div class="pf-2fa-method-info">
                <Icon icon="lucide:smartphone" width="16" />
                <span>Authenticator App (TOTP)</span>
              </div>
              <NSwitch
                :value="hasTotpEnabled"
                :loading="tfLoading && (tfTotpSettingUp || tfDisableTarget === TF_TOTP)"
                @update:value="(v: boolean) => v ? startTotpSetup() : startDisable(TF_TOTP)"
              />
            </div>

            <!-- TOTP 设置中 -->
            <template v-if="tfTotpSettingUp && !hasTotpEnabled">
              <div v-if="tfTotpSetup" class="pf-2fa-setup">
                <div class="pf-2fa-qr">
                  <NQrCode
                    :value="tfTotpSetup.authenticatorUri"
                    :size="120"
                    :padding="0"
                    background-color="transparent"
                    error-correction-level="M"
                  />
                  <span class="pf-hint">扫描二维码</span>
                </div>
                <div class="pf-2fa-manual">
                  <span class="pf-hint">手动输入密钥：</span>
                  <div class="pf-secret-row">
                    <code class="pf-secret">{{ tfTotpSetup.sharedKey }}</code>
                    <NTooltip>
                      <template #trigger>
                        <NButton size="small" quaternary @click="copyToClipboard(tfTotpSetup.sharedKey).then(() => message.success('已复制'))">
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
                  <span class="pf-hint" style="margin-top: 10px; display: block">输入 6 位验证码：</span>
                  <div class="pf-otp-row">
                    <NInputOtp v-model:value="tfTotpCode" :length="6" @complete="confirmEnableTotp" />
                    <NButton type="primary" size="small" :loading="tfLoading" @click="confirmEnableTotp">
                      启用
                    </NButton>
                    <NButton size="small" quaternary @click="cancelTotpSetup">
                      取消
                    </NButton>
                  </div>
                </div>
              </div>
            </template>

            <!-- TOTP 禁用流程 -->
            <template v-if="tfDisableTarget === TF_TOTP">
              <NAlert type="warning" :bordered="false" style="margin-top: 8px">
                请输入 Authenticator App 中的 6 位验证码以禁用
              </NAlert>
              <div class="pf-otp-row">
                <NInputOtp v-model:value="tfDisableCode" :length="6" @complete="confirmDisable" />
                <NButton type="error" size="small" :loading="tfLoading" @click="confirmDisable">
                  禁用
                </NButton>
                <NButton size="small" quaternary @click="cancelDisable">
                  取消
                </NButton>
              </div>
            </template>
          </div>

          <NDivider style="margin: 8px 0" />

          <!-- 邮箱方式 -->
          <div class="pf-2fa-method">
            <div class="pf-2fa-method-header">
              <div class="pf-2fa-method-info">
                <Icon icon="lucide:mail" width="16" />
                <span>邮箱验证码</span>
                <NTag v-if="!profile?.emailVerified" type="warning" size="tiny" :bordered="false">
                  未验证
                </NTag>
              </div>
              <NSwitch
                :value="hasEmailEnabled"
                :disabled="!profile?.emailVerified && !hasEmailEnabled"
                :loading="tfLoading && (tfEmailSettingUp || tfDisableTarget === TF_EMAIL)"
                @update:value="(v: boolean) => v ? startEmailSetup() : startDisable(TF_EMAIL)"
              />
            </div>

            <!-- 邮箱设置中 -->
            <template v-if="tfEmailSettingUp && !hasEmailEnabled">
              <div class="pf-hint" style="margin-top: 6px">
                验证码已发送至 {{ profile?.email }}
              </div>
              <div class="pf-otp-row">
                <NInputOtp v-model:value="tfEmailCode" :length="6" @complete="confirmEnableEmail" />
                <NButton type="primary" size="small" :loading="tfLoading" @click="confirmEnableEmail">
                  启用
                </NButton>
                <NButton
                  size="small" quaternary
                  :disabled="tfCodeCountdown > 0"
                  @click="sendSetupCode(TF_EMAIL)"
                >
                  {{ tfCodeCountdown > 0 ? `${tfCodeCountdown}s` : '重发' }}
                </NButton>
                <NButton size="small" quaternary @click="cancelEmailSetup">
                  取消
                </NButton>
              </div>
            </template>

            <!-- 邮箱禁用流程 -->
            <template v-if="tfDisableTarget === TF_EMAIL">
              <NAlert type="warning" :bordered="false" style="margin-top: 8px">
                验证码已发送至邮箱，请输入 6 位验证码以禁用
              </NAlert>
              <div class="pf-otp-row">
                <NInputOtp v-model:value="tfDisableCode" :length="6" @complete="confirmDisable" />
                <NButton type="error" size="small" :loading="tfLoading" @click="confirmDisable">
                  禁用
                </NButton>
                <NButton
                  size="small" quaternary
                  :disabled="tfCodeCountdown > 0"
                  @click="sendDisableCode(TF_EMAIL)"
                >
                  {{ tfCodeCountdown > 0 ? `${tfCodeCountdown}s` : '重发' }}
                </NButton>
                <NButton size="small" quaternary @click="cancelDisable">
                  取消
                </NButton>
              </div>
            </template>
          </div>

          <NDivider style="margin: 8px 0" />

          <!-- 手机方式 -->
          <div class="pf-2fa-method">
            <div class="pf-2fa-method-header">
              <div class="pf-2fa-method-info">
                <Icon icon="lucide:phone" width="16" />
                <span>手机短信验证码</span>
                <NTag v-if="!profile?.phoneVerified" type="warning" size="tiny" :bordered="false">
                  未验证
                </NTag>
              </div>
              <NSwitch
                :value="hasPhoneEnabled"
                :disabled="!profile?.phoneVerified && !hasPhoneEnabled"
                :loading="tfLoading && (tfPhoneSettingUp || tfDisableTarget === TF_PHONE)"
                @update:value="(v: boolean) => v ? startPhoneSetup() : startDisable(TF_PHONE)"
              />
            </div>

            <!-- 手机设置中 -->
            <template v-if="tfPhoneSettingUp && !hasPhoneEnabled">
              <div class="pf-hint" style="margin-top: 6px">
                验证码已发送至 {{ profile?.phone }}
              </div>
              <div class="pf-otp-row">
                <NInputOtp v-model:value="tfPhoneCode" :length="6" @complete="confirmEnablePhone" />
                <NButton type="primary" size="small" :loading="tfLoading" @click="confirmEnablePhone">
                  启用
                </NButton>
                <NButton
                  size="small" quaternary
                  :disabled="tfCodeCountdown > 0"
                  @click="sendSetupCode(TF_PHONE)"
                >
                  {{ tfCodeCountdown > 0 ? `${tfCodeCountdown}s` : '重发' }}
                </NButton>
                <NButton size="small" quaternary @click="cancelPhoneSetup">
                  取消
                </NButton>
              </div>
            </template>

            <!-- 手机禁用流程 -->
            <template v-if="tfDisableTarget === TF_PHONE">
              <NAlert type="warning" :bordered="false" style="margin-top: 8px">
                验证码已发送至手机，请输入 6 位验证码以禁用
              </NAlert>
              <div class="pf-otp-row">
                <NInputOtp v-model:value="tfDisableCode" :length="6" @complete="confirmDisable" />
                <NButton type="error" size="small" :loading="tfLoading" @click="confirmDisable">
                  禁用
                </NButton>
                <NButton
                  size="small" quaternary
                  :disabled="tfCodeCountdown > 0"
                  @click="sendDisableCode(TF_PHONE)"
                >
                  {{ tfCodeCountdown > 0 ? `${tfCodeCountdown}s` : '重发' }}
                </NButton>
                <NButton size="small" quaternary @click="cancelDisable">
                  取消
                </NButton>
              </div>
            </template>
          </div>
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

      <!-- 关联账号 -->
      <NGridItem :span="2">
        <NCard :bordered="false" size="small" class="pf-card">
          <template #header>
            <div class="pf-card-header">
              <Icon icon="lucide:link" width="16" />
              <span>关联第三方账号</span>
            </div>
          </template>
          <template #header-extra>
            <NButton size="tiny" quaternary @click="loadLinkedAccounts">
              <template #icon>
                <NIcon>
                  <Icon icon="lucide:refresh-cw" />
                </NIcon>
              </template>
            </NButton>
          </template>
          <NSpin :show="linkedLoading">
            <NEmpty v-if="linkedAccounts.length === 0 && linkedLoaded" description="暂无绑定的第三方账号" />
            <div v-else class="pf-list">
              <div v-for="item in linkedAccounts" :key="item.provider" class="pf-list-item">
                <div class="pf-list-icon">
                  <Icon :icon="providerIcon(item.provider)" width="16" />
                </div>
                <div class="pf-list-body">
                  <div class="pf-list-title">
                    {{ item.providerDisplayName || item.provider }}
                  </div>
                  <div class="pf-list-desc">
                    {{ item.email || '未关联邮箱' }}
                    <template v-if="item.lastLoginTime">
                      · 最后登录 {{ formatDate(item.lastLoginTime) }}
                    </template>
                  </div>
                </div>
                <NPopconfirm @positive-click="handleUnlinkAccount(item.provider)">
                  <template #trigger>
                    <NButton size="tiny" type="warning" text>
                      解除绑定
                    </NButton>
                  </template>
                  确定解除与 {{ item.providerDisplayName || item.provider }} 的绑定？
                </NPopconfirm>
              </div>
            </div>
            <div style="margin-top: 10px">
              <NButton size="small" dashed @click="handleLinkNewAccount('')">
                <template #icon>
                  <NIcon>
                    <Icon icon="lucide:plus" />
                  </NIcon>
                </template>
                绑定新账号
              </NButton>
            </div>
          </NSpin>
        </NCard>
      </NGridItem>

      <!-- 登录日志 -->
      <NGridItem :span="2">
        <NCard :bordered="false" size="small" class="pf-card">
          <template #header>
            <div class="pf-card-header">
              <Icon icon="lucide:file-clock" width="16" />
              <span>登录日志</span>
            </div>
          </template>
          <NSpin :show="loginLogLoading">
            <div v-if="loginLogs.length === 0 && !loginLogLoading" style="padding: 20px 0">
              <NEmpty description="暂无登录记录" />
            </div>
            <div v-else class="pf-list">
              <div v-for="(log, idx) in loginLogs" :key="idx" class="pf-list-item">
                <div class="pf-list-icon" :class="{ 'pf-list-icon--danger': log.loginResult !== 0 }">
                  <Icon :icon="log.loginResult === 0 ? 'lucide:log-in' : 'lucide:shield-alert'" width="14" />
                </div>
                <div class="pf-list-body">
                  <div class="pf-list-title">
                    <NTag :type="log.loginResult === 0 ? 'success' : 'error'" size="tiny" :bordered="false">
                      {{ loginResultLabel[log.loginResult] || `状态${log.loginResult}` }}
                    </NTag>
                    <span v-if="log.message" style="margin-left: 6px; font-size: 12px; color: var(--text-secondary)">{{ log.message }}</span>
                  </div>
                  <div class="pf-list-desc">
                    {{ log.loginIp || '未知IP' }}
                    <template v-if="log.loginLocation"> · {{ log.loginLocation }}</template>
                    <template v-if="log.browser"> · {{ log.browser }}</template>
                    <template v-if="log.os"> · {{ log.os }}</template>
                  </div>
                </div>
                <span class="pf-list-time">{{ formatDate(log.loginTime) }}</span>
              </div>
            </div>
            <div v-if="loginLogTotal > 10" style="display: flex; justify-content: flex-end; margin-top: 12px">
              <NPagination
                :page="loginLogPage"
                :page-size="10"
                :item-count="loginLogTotal"
                simple
                @update:page="loadLoginLogs"
              />
            </div>
          </NSpin>
        </NCard>
      </NGridItem>

      <!-- 账号管理 -->
      <NGridItem :span="2">
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

.pf-2fa-method {
  padding: 6px 0;
}

.pf-2fa-method-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
}

.pf-2fa-method-info {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 14px;
}

.pf-2fa-setup {
  display: flex;
  gap: 16px;
  margin-top: 10px;
}

.pf-2fa-qr {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 6px;
  padding: 16px;
  border-radius: 8px;
  background: #fff;
  border: 1px solid var(--n-border-color, #e5e7eb);
  flex-shrink: 0;
  width: fit-content;
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
