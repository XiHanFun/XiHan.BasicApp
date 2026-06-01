<script lang="ts" setup>
import type { FormInst, FormRules } from 'naive-ui'
import type { LoginLogItem, UserProfile } from '~/types'
import {
  NAlert,
  NButton,
  NEmpty,
  NForm,
  NFormItem,
  NIcon,
  NInput,
  NInputOtp,
  NPagination,
  NQrCode,
  NSpin,
  NSwitch,
  NTag,
  NTooltip,
  useDialog,
  useMessage,
} from 'naive-ui'
import { computed, h, onMounted, ref } from 'vue'
import { LOGIN_PATH } from '~/constants'
import { Icon } from '~/iconify'
import { useAppContext } from '~/stores'
import { copyToClipboard, formatDate } from '~/utils'

const props = defineProps<{ profile: UserProfile | null }>()
const emit = defineEmits<{ updated: [] }>()

const message = useMessage()
const dialog = useDialog()
const { apis } = useAppContext()

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
  const colors = ['', 'var(--color-error)', 'var(--color-warning)', 'var(--color-info)', 'var(--color-success)']
  const labels = ['', '弱', '一般', '较强', '强']
  return { score: s, color: colors[s] || '', label: labels[s] || '' }
})

async function changePassword() {
  await pwdFormRef.value?.validate()
  if (!props.profile)
    return
  pwdSaving.value = true
  try {
    await apis.changePasswordApi({
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
    tfTotpSetup.value = await apis.setup2FAApi()
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
    await apis.enable2FAApi(TF_TOTP, tfTotpCodeStr.value)
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
    const res = await apis.send2FASetupCodeApi(method)
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
    await apis.enable2FAApi(TF_EMAIL, tfEmailCodeStr.value)
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
    await apis.enable2FAApi(TF_PHONE, tfPhoneCodeStr.value)
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
    const res = await apis.send2FASetupCodeApi(method)
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
    await apis.disable2FAApi(tfDisableTarget.value, tfDisableCodeStr.value)
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
    const res = await apis.getLoginLogsApi(page, 10)
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
        await apis.deactivateAccountApi(accountPassword.value)
        message.success('账号已停用，即将退出登录')
        setTimeout(() => {
          window.location.href = LOGIN_PATH
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
        await apis.deleteAccountApi(accountPassword.value)
        message.success('账号已注销，即将退出')
        setTimeout(() => {
          window.location.href = LOGIN_PATH
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
  loadLoginLogs()
})
</script>

<template>
  <div class="pf-tab-body">
    <!-- 修改密码 -->
    <section class="pf-section">
      <div class="pf-section__head">
        <div class="pf-section__heading">
          <div class="pf-section__title">
            <Icon icon="lucide:key-round" width="16" />
            <span>修改密码</span>
          </div>
          <div class="pf-section__desc">
            定期更换密码以保护账号安全，建议使用大小写字母、数字与符号组合。
          </div>
        </div>
        <div v-if="profile?.lastPasswordChangeTime" class="pf-section__extra pf-hint">
          上次修改：{{ formatDate(profile.lastPasswordChangeTime) }}
        </div>
      </div>
      <div class="pf-section__body">
        <NForm ref="pwdFormRef" :model="pwdForm" :rules="pwdRules" class="pf-pwd-form">
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
      </div>
      <div class="pf-section__actions">
        <NButton type="primary" :loading="pwdSaving" @click="changePassword">
          更新密码
        </NButton>
      </div>
    </section>

    <!-- 两步验证 -->
    <section class="pf-section">
      <div class="pf-section__head">
        <div class="pf-section__heading">
          <div class="pf-section__title">
            <Icon icon="lucide:shield-check" width="16" />
            <span>两步验证</span>
          </div>
          <div class="pf-section__desc">
            您可以同时启用多种两步验证方式，登录时自由选择使用哪种。
          </div>
        </div>
        <div v-if="enabledCount > 0" class="pf-section__extra">
          <NTag type="success" size="small" :bordered="false">
            {{ enabledCount }} 种已启用
          </NTag>
        </div>
      </div>
      <div class="pf-section__body">
        <!-- TOTP 方式 -->
        <div class="pf-setting-row pf-setting-row--wrap pf-2fa-method">
          <div class="pf-setting-row__main">
            <div class="pf-setting-row__label">
              <Icon icon="lucide:smartphone" width="16" />
              <span>Authenticator App (TOTP)</span>
              <NTag v-if="hasTotpEnabled" type="success" size="tiny" :bordered="false">
                已启用
              </NTag>
            </div>
            <div class="pf-setting-row__desc">
              使用验证器 App 生成动态口令，安全性最高。
            </div>
          </div>
          <div class="pf-setting-row__control">
            <NSwitch
              :value="hasTotpEnabled"
              :loading="tfLoading && (tfTotpSettingUp || tfDisableTarget === TF_TOTP)"
              @update:value="(v: boolean) => v ? startTotpSetup() : startDisable(TF_TOTP)"
            />
          </div>

          <!-- TOTP 设置中 -->
          <template v-if="tfTotpSettingUp && !hasTotpEnabled">
            <div v-if="tfTotpSetup" class="pf-inline-form pf-2fa-setup">
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
            <div class="pf-inline-form">
              <NAlert type="warning" :bordered="false" class="pf-full">
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
            </div>
          </template>
        </div>

        <!-- 邮箱方式 -->
        <div class="pf-setting-row pf-setting-row--wrap pf-2fa-method">
          <div class="pf-setting-row__main">
            <div class="pf-setting-row__label">
              <Icon icon="lucide:mail" width="16" />
              <span>邮箱验证码</span>
              <NTag v-if="hasEmailEnabled" type="success" size="tiny" :bordered="false">
                已启用
              </NTag>
              <NTag v-else-if="!profile?.emailVerified" type="warning" size="tiny" :bordered="false">
                未验证
              </NTag>
            </div>
            <div class="pf-setting-row__desc">
              登录时通过邮箱接收一次性验证码。
            </div>
          </div>
          <div class="pf-setting-row__control">
            <NSwitch
              :value="hasEmailEnabled"
              :disabled="!profile?.emailVerified && !hasEmailEnabled"
              :loading="tfLoading && (tfEmailSettingUp || tfDisableTarget === TF_EMAIL)"
              @update:value="(v: boolean) => v ? startEmailSetup() : startDisable(TF_EMAIL)"
            />
          </div>

          <!-- 邮箱设置中 -->
          <template v-if="tfEmailSettingUp && !hasEmailEnabled">
            <div class="pf-inline-form">
              <div class="pf-hint pf-full">
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
            </div>
          </template>

          <!-- 邮箱禁用流程 -->
          <template v-if="tfDisableTarget === TF_EMAIL">
            <div class="pf-inline-form">
              <NAlert type="warning" :bordered="false" class="pf-full">
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
            </div>
          </template>
        </div>

        <!-- 手机方式 -->
        <div class="pf-setting-row pf-setting-row--wrap pf-2fa-method">
          <div class="pf-setting-row__main">
            <div class="pf-setting-row__label">
              <Icon icon="lucide:phone" width="16" />
              <span>手机短信验证码</span>
              <NTag v-if="hasPhoneEnabled" type="success" size="tiny" :bordered="false">
                已启用
              </NTag>
              <NTag v-else-if="!profile?.phoneVerified" type="warning" size="tiny" :bordered="false">
                未验证
              </NTag>
            </div>
            <div class="pf-setting-row__desc">
              登录时通过短信接收一次性验证码。
            </div>
          </div>
          <div class="pf-setting-row__control">
            <NSwitch
              :value="hasPhoneEnabled"
              :disabled="!profile?.phoneVerified && !hasPhoneEnabled"
              :loading="tfLoading && (tfPhoneSettingUp || tfDisableTarget === TF_PHONE)"
              @update:value="(v: boolean) => v ? startPhoneSetup() : startDisable(TF_PHONE)"
            />
          </div>

          <!-- 手机设置中 -->
          <template v-if="tfPhoneSettingUp && !hasPhoneEnabled">
            <div class="pf-inline-form">
              <div class="pf-hint pf-full">
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
            </div>
          </template>

          <!-- 手机禁用流程 -->
          <template v-if="tfDisableTarget === TF_PHONE">
            <div class="pf-inline-form">
              <NAlert type="warning" :bordered="false" class="pf-full">
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
            </div>
          </template>
        </div>
      </div>
    </section>

    <!-- 登录日志 -->
    <section class="pf-section">
      <div class="pf-section__head">
        <div class="pf-section__heading">
          <div class="pf-section__title">
            <Icon icon="lucide:file-clock" width="16" />
            <span>登录日志</span>
          </div>
          <div class="pf-section__desc">
            最近的登录记录，发现异常请及时修改密码并登出可疑设备。
          </div>
        </div>
      </div>
      <div class="pf-section__body">
        <NSpin :show="loginLogLoading">
          <div v-if="loginLogs.length === 0 && !loginLogLoading" style="padding: 20px 0">
            <NEmpty description="暂无登录记录" />
          </div>
          <div v-else class="pf-list">
            <div v-for="(log, idx) in loginLogs" :key="idx" class="pf-list-item">
              <div class="pf-list-icon" :class="{ 'pf-list-icon--danger': log.loginResult !== 0 }">
                <Icon :icon="log.loginResult === 0 ? 'lucide:log-in' : 'lucide:shield-alert'" width="16" />
              </div>
              <div class="pf-list-body">
                <div class="pf-list-title">
                  <NTag :type="log.loginResult === 0 ? 'success' : 'error'" size="tiny" :bordered="false">
                    {{ loginResultLabel[log.loginResult] || `状态${log.loginResult}` }}
                  </NTag>
                  <span v-if="log.message" style="font-size: 12px; color: var(--text-secondary)">{{ log.message }}</span>
                </div>
                <div class="pf-list-desc">
                  {{ log.loginIp || '未知IP' }}
                  <template v-if="log.loginLocation">
                    · {{ log.loginLocation }}
                  </template>
                  <template v-if="log.browser">
                    · {{ log.browser }}
                  </template>
                  <template v-if="log.os">
                    · {{ log.os }}
                  </template>
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
      </div>
    </section>

    <!-- 安全状态 -->
    <section class="pf-section">
      <div class="pf-section__head">
        <div class="pf-section__heading">
          <div class="pf-section__title">
            <Icon icon="lucide:shield-alert" width="16" />
            <span>安全状态</span>
          </div>
          <div class="pf-section__desc">
            账号锁定、失败登录与关键变更时间的实时概览。
          </div>
        </div>
      </div>
      <div class="pf-section__body">
        <div class="pf-info-grid">
          <div class="pf-info-card">
            <span class="pf-info-card__label">账号锁定</span>
            <span class="pf-info-card__value">
              <NTag :type="profile?.isLocked ? 'error' : 'success'" size="small" :bordered="false">
                {{ profile?.isLocked ? '已锁定' : '正常' }}
              </NTag>
            </span>
          </div>
          <div v-if="profile?.isLocked && profile?.lockoutEndTime" class="pf-info-card">
            <span class="pf-info-card__label">锁定结束</span>
            <span class="pf-info-card__value">{{ formatDate(profile.lockoutEndTime) }}</span>
          </div>
          <div class="pf-info-card">
            <span class="pf-info-card__label">连续失败登录</span>
            <span class="pf-info-card__value" :style="(profile?.failedLoginAttempts ?? 0) > 0 ? 'color:var(--color-warning)' : ''">
              {{ profile?.failedLoginAttempts ?? 0 }} 次
            </span>
          </div>
          <div v-if="profile?.lastFailedLoginTime" class="pf-info-card">
            <span class="pf-info-card__label">最后失败登录</span>
            <span class="pf-info-card__value">{{ formatDate(profile.lastFailedLoginTime) }}</span>
          </div>
          <div class="pf-info-card">
            <span class="pf-info-card__label">最后修改密码</span>
            <span class="pf-info-card__value">{{ profile?.lastPasswordChangeTime ? formatDate(profile.lastPasswordChangeTime) : '—' }}</span>
          </div>
          <div class="pf-info-card">
            <span class="pf-info-card__label">最后修改用户名</span>
            <span class="pf-info-card__value">{{ profile?.lastUserNameChangeTime ? formatDate(profile.lastUserNameChangeTime) : '—' }}</span>
          </div>
        </div>
      </div>
    </section>

    <!-- 账号管理 -->
    <section class="pf-section">
      <div class="pf-section__head">
        <div class="pf-section__heading">
          <div class="pf-section__title">
            <Icon icon="lucide:user-cog" width="16" />
            <span>账号管理</span>
          </div>
          <div class="pf-section__desc">
            停用或注销账号，请谨慎操作。
          </div>
        </div>
      </div>
      <div class="pf-section__body">
        <div class="pf-list">
          <div class="pf-list-item">
            <div class="pf-list-icon">
              <Icon icon="lucide:user-x" width="16" />
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
              <Icon icon="lucide:trash-2" width="16" />
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
      </div>
    </section>
  </div>
</template>

<style src="./profile-shared.css" />

<style scoped>
/* 两步验证：每种方式独立卡片（覆盖设置行默认底边线，改卡片间距） */
.pf-2fa-method {
  padding: 16px;
  margin-bottom: 12px;
  border: 1px solid var(--border-color);
  border-radius: var(--radius);
  background: var(--bg-surface);
  transition: border-color 0.2s;
}

/* 覆盖 .pf-setting-row 的底边线与首尾 padding 特例 */
.pf-2fa-method,
.pf-2fa-method:first-child,
.pf-2fa-method:last-child {
  border-bottom: 1px solid var(--border-color);
  padding-top: 16px;
  padding-bottom: 16px;
}

.pf-2fa-method:last-child {
  margin-bottom: 0;
}

.pf-2fa-method:hover {
  border-color: hsl(var(--primary) / 28%);
}

.pf-pwd-form {
  max-width: 420px;
}

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
  align-items: flex-start;
}

.pf-2fa-qr {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 6px;
  padding: 16px;
  border-radius: 8px;
  /* 二维码功能性白底，非主题色 */
  background: #fff;
  border: 1px solid var(--border-color);
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
  flex-wrap: wrap;
  gap: 8px;
}

@media (max-width: 900px) {
  .pf-2fa-setup {
    flex-direction: column;
  }
}
</style>
