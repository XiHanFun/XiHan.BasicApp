<script lang="ts" setup>
import type { FormInst, FormRules } from 'naive-ui'
import type { UserProfile } from '~/types'
import {
  NAlert,
  NButton,
  NForm,
  NFormItem,
  NIcon,
  NInput,
  NInputOtp,
  NQrCode,
  NSwitch,
  NTag,
  NTooltip,
  useDialog,
  useMessage,
} from 'naive-ui'
import { computed, h, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { LOGIN_PATH } from '~/constants'
import { Icon } from '~/iconify'
import { useAccessStore, useAppContext, useUserStore } from '~/stores'
import { copyToClipboard, formatDate } from '~/utils'

const props = defineProps<{ profile: UserProfile | null }>()
const emit = defineEmits<{ updated: [] }>()

const message = useMessage()
const dialog = useDialog()
const { t } = useI18n()
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
const pwdRules = computed<FormRules>(() => ({
  oldPassword: [{ required: true, message: t('component.profile.security.rule_old_password_required'), trigger: 'blur' }],
  newPassword: [
    { required: true, message: t('component.profile.security.rule_new_password_required'), trigger: 'blur' },
    { min: 6, max: 32, message: t('component.profile.security.rule_password_length'), trigger: 'blur' },
  ],
  confirmPassword: [
    { required: true, message: t('component.profile.security.rule_confirm_password_required'), trigger: 'blur' },
    {
      validator: (_: unknown, v: string) => v === pwdForm.value.newPassword,
      message: t('component.profile.security.rule_password_mismatch'),
      trigger: 'blur',
    },
  ],
}))
async function changePassword() {
  await pwdFormRef.value?.validate()
  if (!props.profile)
    return
  pwdSaving.value = true
  try {
    await apis.changePasswordApi({
      oldPassword: pwdForm.value.oldPassword,
      newPassword: pwdForm.value.newPassword,
    })
    message.success(t('component.profile.security.msg_password_updated'))
    pwdForm.value = {
      oldPassword: '',
      newPassword: '',
      confirmPassword: '',
    }
    emit('updated')
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || t('component.profile.security.err_password_change_failed'))
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
    message.error((e as Error)?.message || t('component.profile.security.err_init_failed'))
    tfTotpSettingUp.value = false
  }
  finally {
    tfLoading.value = false
  }
}

async function confirmEnableTotp() {
  if (!tfTotpCodeStr.value || tfTotpCodeStr.value.length < 6) {
    message.warning(t('component.profile.security.warn_code_incomplete'))
    return
  }
  tfLoading.value = true
  try {
    await apis.enable2FAApi(TF_TOTP, tfTotpCodeStr.value)
    message.success(t('component.profile.security.msg_totp_enabled'))
    tfTotpSetup.value = null
    tfTotpCode.value = []
    tfTotpSettingUp.value = false
    emit('updated')
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || t('component.profile.security.err_enable_failed'))
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
    message.success(method === TF_EMAIL ? t('component.profile.security.msg_code_sent_email') : t('component.profile.security.msg_code_sent_phone'))
    startTfCountdown(res.expiresInSeconds > 60 ? 60 : res.expiresInSeconds)
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || t('component.profile.security.err_code_send_failed'))
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
    message.warning(t('component.profile.security.warn_code_incomplete'))
    return
  }
  tfLoading.value = true
  try {
    await apis.enable2FAApi(TF_EMAIL, tfEmailCodeStr.value)
    message.success(t('component.profile.security.msg_email_2fa_enabled'))
    tfEmailSettingUp.value = false
    tfEmailCode.value = []
    clearCountdown()
    emit('updated')
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || t('component.profile.security.err_enable_failed'))
  }
  finally {
    tfLoading.value = false
  }
}

async function confirmEnablePhone() {
  if (!tfPhoneCodeStr.value || tfPhoneCodeStr.value.length < 6) {
    message.warning(t('component.profile.security.warn_code_incomplete'))
    return
  }
  tfLoading.value = true
  try {
    await apis.enable2FAApi(TF_PHONE, tfPhoneCodeStr.value)
    message.success(t('component.profile.security.msg_phone_2fa_enabled'))
    tfPhoneSettingUp.value = false
    tfPhoneCode.value = []
    clearCountdown()
    emit('updated')
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || t('component.profile.security.err_enable_failed'))
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
    message.success(t('component.profile.security.msg_code_sent'))
    startTfCountdown(res.expiresInSeconds > 60 ? 60 : res.expiresInSeconds)
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || t('component.profile.security.err_code_send_failed'))
  }
  finally {
    tfLoading.value = false
  }
}

async function confirmDisable() {
  if (!tfDisableCodeStr.value || tfDisableCodeStr.value.length < 6) {
    message.warning(t('component.profile.security.warn_code_incomplete'))
    return
  }
  tfLoading.value = true
  try {
    await apis.disable2FAApi(tfDisableTarget.value, tfDisableCodeStr.value)
    message.success(t('component.profile.security.msg_disabled'))
    tfDisableTarget.value = 0
    tfDisableCode.value = []
    clearCountdown()
    emit('updated')
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || t('component.profile.security.err_disable_failed'))
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

// ==================== 账号管理 ====================

const accountPassword = ref('')

/** 停用/注销成功后：清理本地凭证与用户态（会话已被服务端吊销），再整页跳登录 */
function cleanupAuthAndRedirect() {
  try {
    useAccessStore().$reset()
    useUserStore().$reset()
  }
  catch {
    // 清理失败不阻断跳转，整页导航会重建全部内存态
  }
  window.location.href = LOGIN_PATH
}
const accountActionLoading = ref(false)

function handleDeactivateAccount() {
  accountPassword.value = ''
  dialog.warning({
    title: t('component.profile.security.deactivate_title'),
    content: () => h('div', [
      h('p', { style: 'margin-bottom: 12px' }, t('component.profile.security.deactivate_content')),
      h(NInput, {
        'type': 'password',
        'value': accountPassword.value,
        'placeholder': t('component.profile.security.current_password_placeholder'),
        'showPasswordOn': 'click',
        'onUpdate:value': (v: string) => {
          accountPassword.value = v
        },
      }),
    ]),
    positiveText: t('component.profile.security.confirm_deactivate'),
    negativeText: t('common.actions.cancel'),
    onPositiveClick: async () => {
      if (!accountPassword.value) {
        message.warning(t('component.profile.security.warn_password_required'))
        return false
      }
      accountActionLoading.value = true
      try {
        await apis.deactivateAccountApi(accountPassword.value)
        message.success(t('component.profile.security.msg_account_deactivated'))
        setTimeout(() => {
          cleanupAuthAndRedirect()
        }, 1500)
      }
      catch (e: unknown) {
        message.error((e as Error)?.message || t('component.profile.security.err_deactivate_failed'))
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
    title: t('component.profile.security.delete_title'),
    content: () => h('div', [
      h('p', { style: 'margin-bottom: 12px; color: var(--color-error)' }, t('component.profile.security.delete_content')),
      h(NInput, {
        'type': 'password',
        'value': accountPassword.value,
        'placeholder': t('component.profile.security.current_password_placeholder'),
        'showPasswordOn': 'click',
        'onUpdate:value': (v: string) => {
          accountPassword.value = v
        },
      }),
    ]),
    positiveText: t('component.profile.security.confirm_delete'),
    negativeText: t('common.actions.cancel'),
    onPositiveClick: async () => {
      if (!accountPassword.value) {
        message.warning(t('component.profile.security.warn_password_required'))
        return false
      }
      accountActionLoading.value = true
      try {
        await apis.deleteAccountApi(accountPassword.value)
        message.success(t('component.profile.security.msg_account_deleted'))
        setTimeout(() => {
          cleanupAuthAndRedirect()
        }, 1500)
      }
      catch (e: unknown) {
        message.error((e as Error)?.message || t('component.profile.security.err_delete_failed'))
        return false
      }
      finally {
        accountActionLoading.value = false
      }
    },
  })
}
</script>

<template>
  <div class="pf-tab-body">
    <!-- 修改密码 -->
    <section class="pf-section">
      <div class="pf-section__head">
        <div class="pf-section__heading">
          <div class="pf-section__title">
            <Icon icon="lucide:key-round" width="16" />
            <span>{{ t('component.profile.security.section_change_password') }}</span>
          </div>
          <div class="pf-section__desc">
            {{ t('component.profile.security.section_change_password_desc') }}
          </div>
        </div>
        <div v-if="profile?.lastPasswordChangeTime" class="pf-section__extra pf-hint">
          {{ t('component.profile.security.last_changed', { time: formatDate(profile.lastPasswordChangeTime) }) }}
        </div>
      </div>
      <div class="pf-section__body">
        <div class="pf-pwd">
          <NForm ref="pwdFormRef" :model="pwdForm" :rules="pwdRules" class="pf-pwd__form">
            <NFormItem path="oldPassword" :show-label="false">
              <NInput v-model:value="pwdForm.oldPassword" type="password" :placeholder="t('component.profile.security.old_password_placeholder')" show-password-on="click" />
            </NFormItem>
            <NFormItem path="newPassword" :show-label="false">
              <NInput v-model:value="pwdForm.newPassword" type="password" :placeholder="t('component.profile.security.new_password_placeholder')" show-password-on="click" />
            </NFormItem>
            <NFormItem path="confirmPassword" :show-label="false">
              <NInput v-model:value="pwdForm.confirmPassword" type="password" :placeholder="t('component.profile.security.confirm_password_placeholder')" show-password-on="click" />
            </NFormItem>
            <NButton class="pf-pwd__submit" type="primary" :loading="pwdSaving" @click="changePassword">
              {{ t('component.profile.security.btn_update_password') }}
            </NButton>
          </NForm>
        </div>
      </div>
    </section>

    <!-- 两步验证 -->
    <section class="pf-section">
      <div class="pf-section__head">
        <div class="pf-section__heading">
          <div class="pf-section__title">
            <Icon icon="lucide:shield-check" width="16" />
            <span>{{ t('component.profile.security.section_2fa') }}</span>
          </div>
          <div class="pf-section__desc">
            {{ t('component.profile.security.section_2fa_desc') }}
          </div>
        </div>
        <div v-if="enabledCount > 0" class="pf-section__extra">
          <NTag type="success" size="small" :bordered="false">
            {{ t('component.profile.security.enabled_count', { count: enabledCount }) }}
          </NTag>
        </div>
      </div>
      <div class="pf-section__body">
        <!-- TOTP 方式 -->
        <div class="pf-setting-row pf-setting-row--wrap pf-2fa-method" :class="{ 'is-on': hasTotpEnabled }">
          <span class="pf-2fa-icon"><Icon icon="lucide:smartphone" width="18" /></span>
          <div class="pf-setting-row__main">
            <div class="pf-setting-row__label">
              <span>{{ t('component.profile.security.totp_method') }}</span>
              <NTag v-if="hasTotpEnabled" type="success" size="tiny" :bordered="false">
                {{ t('component.profile.security.tag_enabled') }}
              </NTag>
            </div>
            <div class="pf-setting-row__desc">
              {{ t('component.profile.security.totp_desc') }}
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
                <span class="pf-hint">{{ t('component.profile.security.scan_qr') }}</span>
              </div>
              <div class="pf-2fa-manual">
                <span class="pf-hint">{{ t('component.profile.security.manual_key') }}</span>
                <div class="pf-secret-row">
                  <code class="pf-secret">{{ tfTotpSetup.sharedKey }}</code>
                  <NTooltip>
                    <template #trigger>
                      <NButton size="small" quaternary @click="copyToClipboard(tfTotpSetup.sharedKey).then(() => message.success(t('component.profile.security.msg_copied')))">
                        <template #icon>
                          <NIcon>
                            <Icon icon="lucide:copy" />
                          </NIcon>
                        </template>
                      </NButton>
                    </template>
                    {{ t('component.profile.security.copy_key') }}
                  </NTooltip>
                </div>
                <span class="pf-hint" style="margin-top: 10px; display: block">{{ t('component.profile.security.enter_6_digit') }}</span>
                <div class="pf-otp-row">
                  <NInputOtp v-model:value="tfTotpCode" :length="6" @complete="confirmEnableTotp" />
                  <NButton type="primary" size="small" :loading="tfLoading" @click="confirmEnableTotp">
                    {{ t('component.profile.security.btn_enable') }}
                  </NButton>
                  <NButton size="small" quaternary @click="cancelTotpSetup">
                    {{ t('common.actions.cancel') }}
                  </NButton>
                </div>
              </div>
            </div>
          </template>

          <!-- TOTP 禁用流程 -->
          <template v-if="tfDisableTarget === TF_TOTP">
            <div class="pf-inline-form">
              <NAlert type="warning" :bordered="false" class="pf-full">
                {{ t('component.profile.security.totp_disable_hint') }}
              </NAlert>
              <div class="pf-otp-row">
                <NInputOtp v-model:value="tfDisableCode" :length="6" @complete="confirmDisable" />
                <NButton type="error" size="small" :loading="tfLoading" @click="confirmDisable">
                  {{ t('component.profile.security.btn_disable') }}
                </NButton>
                <NButton size="small" quaternary @click="cancelDisable">
                  {{ t('common.actions.cancel') }}
                </NButton>
              </div>
            </div>
          </template>
        </div>

        <!-- 邮箱方式 -->
        <div class="pf-setting-row pf-setting-row--wrap pf-2fa-method" :class="{ 'is-on': hasEmailEnabled }">
          <span class="pf-2fa-icon"><Icon icon="lucide:mail" width="18" /></span>
          <div class="pf-setting-row__main">
            <div class="pf-setting-row__label">
              <span>{{ t('component.profile.security.email_method') }}</span>
              <NTag v-if="hasEmailEnabled" type="success" size="tiny" :bordered="false">
                {{ t('component.profile.security.tag_enabled') }}
              </NTag>
              <NTag v-else-if="!profile?.emailVerified" type="warning" size="tiny" :bordered="false">
                {{ t('component.profile.security.tag_unverified') }}
              </NTag>
            </div>
            <div class="pf-setting-row__desc">
              {{ t('component.profile.security.email_method_desc') }}
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
                {{ t('component.profile.security.code_sent_to', { target: profile?.email }) }}
              </div>
              <div class="pf-otp-row">
                <NInputOtp v-model:value="tfEmailCode" :length="6" @complete="confirmEnableEmail" />
                <NButton type="primary" size="small" :loading="tfLoading" @click="confirmEnableEmail">
                  {{ t('component.profile.security.btn_enable') }}
                </NButton>
                <NButton
                  size="small" quaternary
                  :disabled="tfCodeCountdown > 0"
                  @click="sendSetupCode(TF_EMAIL)"
                >
                  {{ tfCodeCountdown > 0 ? `${tfCodeCountdown}s` : t('common.actions.resend') }}
                </NButton>
                <NButton size="small" quaternary @click="cancelEmailSetup">
                  {{ t('common.actions.cancel') }}
                </NButton>
              </div>
            </div>
          </template>

          <!-- 邮箱禁用流程 -->
          <template v-if="tfDisableTarget === TF_EMAIL">
            <div class="pf-inline-form">
              <NAlert type="warning" :bordered="false" class="pf-full">
                {{ t('component.profile.security.email_disable_hint') }}
              </NAlert>
              <div class="pf-otp-row">
                <NInputOtp v-model:value="tfDisableCode" :length="6" @complete="confirmDisable" />
                <NButton type="error" size="small" :loading="tfLoading" @click="confirmDisable">
                  {{ t('component.profile.security.btn_disable') }}
                </NButton>
                <NButton
                  size="small" quaternary
                  :disabled="tfCodeCountdown > 0"
                  @click="sendDisableCode(TF_EMAIL)"
                >
                  {{ tfCodeCountdown > 0 ? `${tfCodeCountdown}s` : t('common.actions.resend') }}
                </NButton>
                <NButton size="small" quaternary @click="cancelDisable">
                  {{ t('common.actions.cancel') }}
                </NButton>
              </div>
            </div>
          </template>
        </div>

        <!-- 手机方式 -->
        <div class="pf-setting-row pf-setting-row--wrap pf-2fa-method" :class="{ 'is-on': hasPhoneEnabled }">
          <span class="pf-2fa-icon"><Icon icon="lucide:phone" width="18" /></span>
          <div class="pf-setting-row__main">
            <div class="pf-setting-row__label">
              <span>{{ t('component.profile.security.sms_method') }}</span>
              <NTag v-if="hasPhoneEnabled" type="success" size="tiny" :bordered="false">
                {{ t('component.profile.security.tag_enabled') }}
              </NTag>
              <NTag v-else-if="!profile?.phoneVerified" type="warning" size="tiny" :bordered="false">
                {{ t('component.profile.security.tag_unverified') }}
              </NTag>
            </div>
            <div class="pf-setting-row__desc">
              {{ t('component.profile.security.sms_method_desc') }}
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
                {{ t('component.profile.security.code_sent_to', { target: profile?.phone }) }}
              </div>
              <div class="pf-otp-row">
                <NInputOtp v-model:value="tfPhoneCode" :length="6" @complete="confirmEnablePhone" />
                <NButton type="primary" size="small" :loading="tfLoading" @click="confirmEnablePhone">
                  {{ t('component.profile.security.btn_enable') }}
                </NButton>
                <NButton
                  size="small" quaternary
                  :disabled="tfCodeCountdown > 0"
                  @click="sendSetupCode(TF_PHONE)"
                >
                  {{ tfCodeCountdown > 0 ? `${tfCodeCountdown}s` : t('common.actions.resend') }}
                </NButton>
                <NButton size="small" quaternary @click="cancelPhoneSetup">
                  {{ t('common.actions.cancel') }}
                </NButton>
              </div>
            </div>
          </template>

          <!-- 手机禁用流程 -->
          <template v-if="tfDisableTarget === TF_PHONE">
            <div class="pf-inline-form">
              <NAlert type="warning" :bordered="false" class="pf-full">
                {{ t('component.profile.security.phone_disable_hint') }}
              </NAlert>
              <div class="pf-otp-row">
                <NInputOtp v-model:value="tfDisableCode" :length="6" @complete="confirmDisable" />
                <NButton type="error" size="small" :loading="tfLoading" @click="confirmDisable">
                  {{ t('component.profile.security.btn_disable') }}
                </NButton>
                <NButton
                  size="small" quaternary
                  :disabled="tfCodeCountdown > 0"
                  @click="sendDisableCode(TF_PHONE)"
                >
                  {{ tfCodeCountdown > 0 ? `${tfCodeCountdown}s` : t('common.actions.resend') }}
                </NButton>
                <NButton size="small" quaternary @click="cancelDisable">
                  {{ t('common.actions.cancel') }}
                </NButton>
              </div>
            </div>
          </template>
        </div>
      </div>
    </section>

    <!-- 安全状态 -->
    <section class="pf-section">
      <div class="pf-section__head">
        <div class="pf-section__heading">
          <div class="pf-section__title">
            <Icon icon="lucide:shield-alert" width="16" />
            <span>{{ t('component.profile.security.section_security_status') }}</span>
          </div>
          <div class="pf-section__desc">
            {{ t('component.profile.security.section_security_status_desc') }}
          </div>
        </div>
      </div>
      <div class="pf-section__body">
        <div class="pf-info-grid">
          <div class="pf-info-card">
            <span class="pf-info-card__label">{{ t('component.profile.security.label_account_lock') }}</span>
            <span class="pf-info-card__value">
              <NTag :type="profile?.isLocked ? 'error' : 'success'" size="small" :bordered="false">
                {{ profile?.isLocked ? t('component.profile.security.value_locked') : t('component.profile.security.value_normal') }}
              </NTag>
            </span>
          </div>
          <div v-if="profile?.isLocked && profile?.lockoutEndTime" class="pf-info-card">
            <span class="pf-info-card__label">{{ t('component.profile.security.label_lock_end') }}</span>
            <span class="pf-info-card__value">{{ formatDate(profile.lockoutEndTime) }}</span>
          </div>
          <div class="pf-info-card">
            <span class="pf-info-card__label">{{ t('component.profile.security.label_failed_logins') }}</span>
            <span class="pf-info-card__value" :style="(profile?.failedLoginAttempts ?? 0) > 0 ? 'color:var(--color-warning)' : ''">
              {{ t('component.profile.security.failed_logins_count', { count: profile?.failedLoginAttempts ?? 0 }) }}
            </span>
          </div>
          <div v-if="profile?.lastFailedLoginTime" class="pf-info-card">
            <span class="pf-info-card__label">{{ t('component.profile.security.label_last_failed_login') }}</span>
            <span class="pf-info-card__value">{{ formatDate(profile.lastFailedLoginTime) }}</span>
          </div>
          <div class="pf-info-card">
            <span class="pf-info-card__label">{{ t('component.profile.security.label_last_password_change') }}</span>
            <span class="pf-info-card__value">{{ profile?.lastPasswordChangeTime ? formatDate(profile.lastPasswordChangeTime) : '—' }}</span>
          </div>
          <div class="pf-info-card">
            <span class="pf-info-card__label">{{ t('component.profile.security.label_last_username_change') }}</span>
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
            <span>{{ t('component.profile.security.section_account_mgmt') }}</span>
          </div>
          <div class="pf-section__desc">
            {{ t('component.profile.security.section_account_mgmt_desc') }}
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
                {{ t('component.profile.security.deactivate_title') }}
              </div>
              <div class="pf-list-desc">
                {{ t('component.profile.security.deactivate_desc') }}
              </div>
            </div>
            <NButton size="small" type="warning" ghost @click="handleDeactivateAccount">
              {{ t('component.profile.security.btn_deactivate') }}
            </NButton>
          </div>
          <div class="pf-list-item">
            <div class="pf-list-icon pf-list-icon--danger">
              <Icon icon="lucide:trash-2" width="16" />
            </div>
            <div class="pf-list-body">
              <div class="pf-list-title" style="color: var(--color-error)">
                {{ t('component.profile.security.delete_title') }}
              </div>
              <div class="pf-list-desc">
                {{ t('component.profile.security.delete_desc') }}
              </div>
            </div>
            <NButton size="small" type="error" ghost @click="handleDeleteAccount">
              {{ t('component.profile.security.btn_delete') }}
            </NButton>
          </div>
        </div>
      </div>
    </section>
  </div>
</template>

<style src="./profile-shared.css" />

<style scoped>
/* 两步验证：每种方式独立描边盒（GitHub 式，无底色） */
.pf-2fa-method {
  gap: 14px;
  padding: 16px;
  margin-bottom: 10px;
  border: 1px solid hsl(var(--border) / 70%);
  border-radius: var(--radius);
  background: transparent;
  transition:
    background 0.2s,
    border-color 0.2s;
}

.pf-2fa-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 38px;
  height: 38px;
  flex-shrink: 0;
  border-radius: 10px;
  background: hsl(var(--muted));
  color: var(--text-secondary);
  transition:
    background 0.2s,
    color 0.2s;
}

.pf-2fa-method.is-on {
  border-color: hsl(var(--primary) / 45%);
  background: hsl(var(--primary) / 4%);
}

.pf-2fa-method.is-on .pf-2fa-icon {
  background: hsl(var(--primary) / 14%);
  color: hsl(var(--primary));
}

/* 覆盖 .pf-setting-row 的底边线与首尾 padding 特例 */
.pf-2fa-method,
.pf-2fa-method:first-child,
.pf-2fa-method:last-child {
  padding-top: 16px;
  padding-bottom: 16px;
}

.pf-2fa-method:last-child {
  margin-bottom: 0;
}

.pf-2fa-method:hover {
  background: hsl(var(--muted) / 42%);
}

/* 修改密码 */
.pf-pwd {
  display: grid;
  grid-template-columns: 1fr;
  align-items: start;
}

.pf-pwd__form {
  display: flex;
  flex-direction: column;
  max-width: none;
}

.pf-pwd__submit {
  align-self: flex-start;
  margin-top: 4px;
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

  .pf-pwd {
    gap: 18px;
  }
}
</style>
