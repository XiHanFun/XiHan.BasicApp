<script lang="ts" setup>
import type { FormRules } from '@xihan-ui/headless'

import type { UserProfile } from '~/types'
import { XhAlertDescription, XhAlertIcon, XhAlertRoot, XhBadge, XhButton, XhFieldControl, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFormFieldGroup, XhFormRoot, XhPinInputInput, XhPinInputRoot, XhQrCode, XhSwitch } from '@xihan-ui/vue'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { XInput, XTooltip } from '~/components'
import { prompt, toast } from '~/composables'
import { LOGIN_PATH } from '~/constants'
import { Icon } from '~/iconify'
import { useAccessStore, useAppContext, useUserStore } from '~/stores'
import { copyToClipboard, formatDate } from '~/utils'
import CodeCountdown from '../shared/CodeCountdown.vue'

const props = defineProps<{ profile: UserProfile | null }>()
const emit = defineEmits<{ updated: [] }>()

const { t } = useI18n()
const { apis } = useAppContext()

// ==================== 2FA flags 常量（与后端 [Flags] 枚举对应） ====================

const TF_TOTP = 1
const TF_EMAIL = 2
const TF_PHONE = 4

// ==================== 密码 ====================

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
      validator: (value, values) =>
        value === values.newPassword ? null : t('component.profile.security.rule_password_mismatch'),
    },
  ],
}))
async function onSubmit() {
  if (!props.profile)
    return
  pwdSaving.value = true
  try {
    await apis.changePasswordApi({
      oldPassword: pwdForm.value.oldPassword,
      newPassword: pwdForm.value.newPassword,
    })
    toast.success(t('component.profile.security.msg_password_updated'))
    pwdForm.value = {
      oldPassword: '',
      newPassword: '',
      confirmPassword: '',
    }
    emit('updated')
  }
  catch (e: unknown) {
    toast.error((e as Error)?.message || t('component.profile.security.err_password_change_failed'))
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
/** 重发倒计时这一轮的时长，大于 0 即正在倒计时 */
const tfResendSeconds = ref(0)

/** 停掉倒计时，重发按钮立刻可按 */
function clearCountdown() {
  tfResendSeconds.value = 0
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
    toast.error((e as Error)?.message || t('component.profile.security.err_init_failed'))
    tfTotpSettingUp.value = false
  }
  finally {
    tfLoading.value = false
  }
}

async function confirmEnableTotp() {
  if (!tfTotpCodeStr.value || tfTotpCodeStr.value.length < 6) {
    toast.warning(t('component.profile.security.warn_code_incomplete'))
    return
  }
  tfLoading.value = true
  try {
    await apis.enable2FAApi(TF_TOTP, tfTotpCodeStr.value)
    toast.success(t('component.profile.security.msg_totp_enabled'))
    tfTotpSetup.value = null
    tfTotpCode.value = []
    tfTotpSettingUp.value = false
    emit('updated')
  }
  catch (e: unknown) {
    toast.error((e as Error)?.message || t('component.profile.security.err_enable_failed'))
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
    toast.success(method === TF_EMAIL ? t('component.profile.security.msg_code_sent_email') : t('component.profile.security.msg_code_sent_phone'))
    tfResendSeconds.value = Math.min(res.expiresInSeconds, 60)
  }
  catch (e: unknown) {
    toast.error((e as Error)?.message || t('component.profile.security.err_code_send_failed'))
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
    toast.warning(t('component.profile.security.warn_code_incomplete'))
    return
  }
  tfLoading.value = true
  try {
    await apis.enable2FAApi(TF_EMAIL, tfEmailCodeStr.value)
    toast.success(t('component.profile.security.msg_email_2fa_enabled'))
    tfEmailSettingUp.value = false
    tfEmailCode.value = []
    clearCountdown()
    emit('updated')
  }
  catch (e: unknown) {
    toast.error((e as Error)?.message || t('component.profile.security.err_enable_failed'))
  }
  finally {
    tfLoading.value = false
  }
}

async function confirmEnablePhone() {
  if (!tfPhoneCodeStr.value || tfPhoneCodeStr.value.length < 6) {
    toast.warning(t('component.profile.security.warn_code_incomplete'))
    return
  }
  tfLoading.value = true
  try {
    await apis.enable2FAApi(TF_PHONE, tfPhoneCodeStr.value)
    toast.success(t('component.profile.security.msg_phone_2fa_enabled'))
    tfPhoneSettingUp.value = false
    tfPhoneCode.value = []
    clearCountdown()
    emit('updated')
  }
  catch (e: unknown) {
    toast.error((e as Error)?.message || t('component.profile.security.err_enable_failed'))
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
    toast.success(t('component.profile.security.msg_code_sent'))
    tfResendSeconds.value = Math.min(res.expiresInSeconds, 60)
  }
  catch (e: unknown) {
    toast.error((e as Error)?.message || t('component.profile.security.err_code_send_failed'))
  }
  finally {
    tfLoading.value = false
  }
}

async function confirmDisable() {
  if (!tfDisableCodeStr.value || tfDisableCodeStr.value.length < 6) {
    toast.warning(t('component.profile.security.warn_code_incomplete'))
    return
  }
  tfLoading.value = true
  try {
    await apis.disable2FAApi(tfDisableTarget.value, tfDisableCodeStr.value)
    toast.success(t('component.profile.security.msg_disabled'))
    tfDisableTarget.value = 0
    tfDisableCode.value = []
    clearCountdown()
    emit('updated')
  }
  catch (e: unknown) {
    toast.error((e as Error)?.message || t('component.profile.security.err_disable_failed'))
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
  void prompt({
    title: t('component.profile.security.deactivate_title'),
    description: t('component.profile.security.deactivate_content'),
    okText: t('component.profile.security.confirm_deactivate'),
    cancelText: t('common.actions.cancel'),
    fields: [
      {
        key: 'password',
        type: 'password',
        placeholder: t('component.profile.security.current_password_placeholder'),
      },
    ],
    onOk: async (values) => {
      if (!values.password) {
        toast.warning(t('component.profile.security.warn_password_required'))
        return false
      }
      accountActionLoading.value = true
      try {
        await apis.deactivateAccountApi(values.password)
        toast.success(t('component.profile.security.msg_account_deactivated'))
        setTimeout(() => {
          cleanupAuthAndRedirect()
        }, 1500)
      }
      catch (e: unknown) {
        toast.error((e as Error)?.message || t('component.profile.security.err_deactivate_failed'))
        return false
      }
      finally {
        accountActionLoading.value = false
      }
    },
  })
}

function handleDeleteAccount() {
  void prompt({
    title: t('component.profile.security.delete_title'),
    description: t('component.profile.security.delete_content'),
    tone: 'danger',
    okText: t('component.profile.security.confirm_delete'),
    cancelText: t('common.actions.cancel'),
    fields: [
      {
        key: 'password',
        type: 'password',
        placeholder: t('component.profile.security.current_password_placeholder'),
      },
    ],
    onOk: async (values) => {
      if (!values.password) {
        toast.warning(t('component.profile.security.warn_password_required'))
        return false
      }
      accountActionLoading.value = true
      try {
        await apis.deleteAccountApi(values.password)
        toast.success(t('component.profile.security.msg_account_deleted'))
        setTimeout(() => {
          cleanupAuthAndRedirect()
        }, 1500)
      }
      catch (e: unknown) {
        toast.error((e as Error)?.message || t('component.profile.security.err_delete_failed'))
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
          <XhFormRoot
            v-model:values="pwdForm"
            :rules="pwdRules"
            validate-on="blur"
            class="pf-pwd__form"
            @submit="onSubmit"
          >
            <XhFormFieldGroup value="oldPassword">
              <XhFieldRoot>
                <!-- 三个字段靠占位文案表意，标签只留给读屏 -->
                <XhFieldLabel class="sr-only">
                  {{ t('component.profile.security.old_password_placeholder') }}
                </XhFieldLabel>
                <XhFieldControl>
                  <XInput v-model:value="pwdForm.oldPassword" type="password" :placeholder="t('component.profile.security.old_password_placeholder')" />
                </XhFieldControl>
                <XhFieldErrorText />
              </XhFieldRoot>
            </XhFormFieldGroup>
            <XhFormFieldGroup value="newPassword">
              <XhFieldRoot>
                <!-- 三个字段靠占位文案表意，标签只留给读屏 -->
                <XhFieldLabel class="sr-only">
                  {{ t('component.profile.security.new_password_placeholder') }}
                </XhFieldLabel>
                <XhFieldControl>
                  <XInput v-model:value="pwdForm.newPassword" type="password" :placeholder="t('component.profile.security.new_password_placeholder')" />
                </XhFieldControl>
                <XhFieldErrorText />
              </XhFieldRoot>
            </XhFormFieldGroup>
            <XhFormFieldGroup value="confirmPassword">
              <XhFieldRoot>
                <!-- 三个字段靠占位文案表意，标签只留给读屏 -->
                <XhFieldLabel class="sr-only">
                  {{ t('component.profile.security.confirm_password_placeholder') }}
                </XhFieldLabel>
                <XhFieldControl>
                  <XInput v-model:value="pwdForm.confirmPassword" type="password" :placeholder="t('component.profile.security.confirm_password_placeholder')" />
                </XhFieldControl>
                <XhFieldErrorText />
              </XhFieldRoot>
            </XhFormFieldGroup>
            <XhButton class="pf-pwd__submit" type="submit" tone="brand" :loading="pwdSaving">
              {{ t('component.profile.security.btn_update_password') }}
            </XhButton>
          </XhFormRoot>
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
          <XhBadge variant="subtle" tone="success" size="sm">
            {{ t('component.profile.security.enabled_count', { count: enabledCount }) }}
          </XhBadge>
        </div>
      </div>
      <div class="pf-section__body">
        <!-- TOTP 方式 -->
        <div class="pf-setting-row pf-setting-row--wrap pf-2fa-method" :class="{ 'is-on': hasTotpEnabled }">
          <span class="pf-2fa-icon"><Icon icon="lucide:smartphone" width="18" /></span>
          <div class="pf-setting-row__main">
            <div class="pf-setting-row__label">
              <span>{{ t('component.profile.security.totp_method') }}</span>
              <XhBadge v-if="hasTotpEnabled" variant="subtle" tone="success" size="sm">
                {{ t('component.profile.security.tag_enabled') }}
              </XhBadge>
            </div>
            <div class="pf-setting-row__desc">
              {{ t('component.profile.security.totp_desc') }}
            </div>
          </div>
          <div class="pf-setting-row__control">
            <XhSwitch
              :checked="hasTotpEnabled"
              :loading="tfLoading && (tfTotpSettingUp || tfDisableTarget === TF_TOTP)"
              @update:checked="(v: boolean) => v ? startTotpSetup() : startDisable(TF_TOTP)"
            />
          </div>

          <!-- TOTP 设置中 -->
          <template v-if="tfTotpSettingUp && !hasTotpEnabled">
            <div v-if="tfTotpSetup" class="pf-inline-form pf-2fa-setup">
              <div class="pf-2fa-qr">
                <XhQrCode
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
                  <XTooltip :content="t('component.profile.security.copy_key')">
                    <XhButton size="sm" variant="ghost" @click="copyToClipboard(tfTotpSetup.sharedKey).then(() => toast.success(t('component.profile.security.msg_copied')))">
                      <span><Icon icon="lucide:copy" /></span>
                    </XhButton>
                  </XTooltip>
                </div>
                <span class="pf-hint" style="margin-top: 10px; display: block">{{ t('component.profile.security.enter_6_digit') }}</span>
                <div class="pf-otp-row">
                  <XhPinInputRoot
                    v-model:value="tfTotpCode"
                    :length="6"
                    otp
                    @value-complete="() => confirmEnableTotp()"
                  >
                    <!-- 格间距长在格子自己身上，这层包裹只负责排成一行 -->
                    <div style="display: flex">
                      <XhPinInputInput v-for="i in 6" :key="i" :index="i - 1" />
                    </div>
                  </XhPinInputRoot>
                  <XhButton tone="brand" size="sm" :loading="tfLoading" @click="confirmEnableTotp">
                    {{ t('component.profile.security.btn_enable') }}
                  </XhButton>
                  <XhButton size="sm" variant="ghost" @click="cancelTotpSetup">
                    {{ t('common.actions.cancel') }}
                  </XhButton>
                </div>
              </div>
            </div>
          </template>

          <!-- TOTP 禁用流程 -->
          <template v-if="tfDisableTarget === TF_TOTP">
            <div class="pf-inline-form">
              <XhAlertRoot tone="warning" class="pf-full">
                <XhAlertIcon>
                  <Icon icon="lucide:triangle-alert" width="16" height="16" />
                </XhAlertIcon>
                <XhAlertDescription>
                  {{ t('component.profile.security.totp_disable_hint') }}
                </XhAlertDescription>
              </XhAlertRoot>
              <div class="pf-otp-row">
                <XhPinInputRoot
                  v-model:value="tfDisableCode"
                  :length="6"
                  otp
                  @value-complete="() => confirmDisable()"
                >
                  <!-- 格间距长在格子自己身上，这层包裹只负责排成一行 -->
                  <div style="display: flex">
                    <XhPinInputInput v-for="i in 6" :key="i" :index="i - 1" />
                  </div>
                </XhPinInputRoot>
                <XhButton tone="danger" size="sm" :loading="tfLoading" @click="confirmDisable">
                  {{ t('component.profile.security.btn_disable') }}
                </XhButton>
                <XhButton size="sm" variant="ghost" @click="cancelDisable">
                  {{ t('common.actions.cancel') }}
                </XhButton>
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
              <XhBadge v-if="hasEmailEnabled" variant="subtle" tone="success" size="sm">
                {{ t('component.profile.security.tag_enabled') }}
              </XhBadge>
              <XhBadge v-else-if="!profile?.emailVerified" variant="subtle" tone="warning" size="sm">
                {{ t('component.profile.security.tag_unverified') }}
              </XhBadge>
            </div>
            <div class="pf-setting-row__desc">
              {{ t('component.profile.security.email_method_desc') }}
            </div>
          </div>
          <div class="pf-setting-row__control">
            <XhSwitch
              :checked="hasEmailEnabled"
              :disabled="!profile?.emailVerified && !hasEmailEnabled"
              :loading="tfLoading && (tfEmailSettingUp || tfDisableTarget === TF_EMAIL)"
              @update:checked="(v: boolean) => v ? startEmailSetup() : startDisable(TF_EMAIL)"
            />
          </div>

          <!-- 邮箱设置中 -->
          <template v-if="tfEmailSettingUp && !hasEmailEnabled">
            <div class="pf-inline-form">
              <div class="pf-hint pf-full">
                {{ t('component.profile.security.code_sent_to', { target: profile?.email }) }}
              </div>
              <div class="pf-otp-row">
                <XhPinInputRoot
                  v-model:value="tfEmailCode"
                  :length="6"
                  otp
                  @value-complete="() => confirmEnableEmail()"
                >
                  <!-- 格间距长在格子自己身上，这层包裹只负责排成一行 -->
                  <div style="display: flex">
                    <XhPinInputInput v-for="i in 6" :key="i" :index="i - 1" />
                  </div>
                </XhPinInputRoot>
                <XhButton tone="brand" size="sm" :loading="tfLoading" @click="confirmEnableEmail">
                  {{ t('component.profile.security.btn_enable') }}
                </XhButton>
                <XhButton
                  size="sm" variant="ghost"
                  :disabled="tfResendSeconds > 0"
                  @click="sendSetupCode(TF_EMAIL)"
                >
                  <CodeCountdown v-if="tfResendSeconds > 0" :seconds="tfResendSeconds" @finish="clearCountdown" />
                  <template v-else>
                    {{ t('common.actions.resend') }}
                  </template>
                </XhButton>
                <XhButton size="sm" variant="ghost" @click="cancelEmailSetup">
                  {{ t('common.actions.cancel') }}
                </XhButton>
              </div>
            </div>
          </template>

          <!-- 邮箱禁用流程 -->
          <template v-if="tfDisableTarget === TF_EMAIL">
            <div class="pf-inline-form">
              <XhAlertRoot tone="warning" class="pf-full">
                <XhAlertIcon>
                  <Icon icon="lucide:triangle-alert" width="16" height="16" />
                </XhAlertIcon>
                <XhAlertDescription>
                  {{ t('component.profile.security.email_disable_hint') }}
                </XhAlertDescription>
              </XhAlertRoot>
              <div class="pf-otp-row">
                <XhPinInputRoot
                  v-model:value="tfDisableCode"
                  :length="6"
                  otp
                  @value-complete="() => confirmDisable()"
                >
                  <!-- 格间距长在格子自己身上，这层包裹只负责排成一行 -->
                  <div style="display: flex">
                    <XhPinInputInput v-for="i in 6" :key="i" :index="i - 1" />
                  </div>
                </XhPinInputRoot>
                <XhButton tone="danger" size="sm" :loading="tfLoading" @click="confirmDisable">
                  {{ t('component.profile.security.btn_disable') }}
                </XhButton>
                <XhButton
                  size="sm" variant="ghost"
                  :disabled="tfResendSeconds > 0"
                  @click="sendDisableCode(TF_EMAIL)"
                >
                  <CodeCountdown v-if="tfResendSeconds > 0" :seconds="tfResendSeconds" @finish="clearCountdown" />
                  <template v-else>
                    {{ t('common.actions.resend') }}
                  </template>
                </XhButton>
                <XhButton size="sm" variant="ghost" @click="cancelDisable">
                  {{ t('common.actions.cancel') }}
                </XhButton>
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
              <XhBadge v-if="hasPhoneEnabled" variant="subtle" tone="success" size="sm">
                {{ t('component.profile.security.tag_enabled') }}
              </XhBadge>
              <XhBadge v-else-if="!profile?.phoneVerified" variant="subtle" tone="warning" size="sm">
                {{ t('component.profile.security.tag_unverified') }}
              </XhBadge>
            </div>
            <div class="pf-setting-row__desc">
              {{ t('component.profile.security.sms_method_desc') }}
            </div>
          </div>
          <div class="pf-setting-row__control">
            <XhSwitch
              :checked="hasPhoneEnabled"
              :disabled="!profile?.phoneVerified && !hasPhoneEnabled"
              :loading="tfLoading && (tfPhoneSettingUp || tfDisableTarget === TF_PHONE)"
              @update:checked="(v: boolean) => v ? startPhoneSetup() : startDisable(TF_PHONE)"
            />
          </div>

          <!-- 手机设置中 -->
          <template v-if="tfPhoneSettingUp && !hasPhoneEnabled">
            <div class="pf-inline-form">
              <div class="pf-hint pf-full">
                {{ t('component.profile.security.code_sent_to', { target: profile?.phone }) }}
              </div>
              <div class="pf-otp-row">
                <XhPinInputRoot
                  v-model:value="tfPhoneCode"
                  :length="6"
                  otp
                  @value-complete="() => confirmEnablePhone()"
                >
                  <!-- 格间距长在格子自己身上，这层包裹只负责排成一行 -->
                  <div style="display: flex">
                    <XhPinInputInput v-for="i in 6" :key="i" :index="i - 1" />
                  </div>
                </XhPinInputRoot>
                <XhButton tone="brand" size="sm" :loading="tfLoading" @click="confirmEnablePhone">
                  {{ t('component.profile.security.btn_enable') }}
                </XhButton>
                <XhButton
                  size="sm" variant="ghost"
                  :disabled="tfResendSeconds > 0"
                  @click="sendSetupCode(TF_PHONE)"
                >
                  <CodeCountdown v-if="tfResendSeconds > 0" :seconds="tfResendSeconds" @finish="clearCountdown" />
                  <template v-else>
                    {{ t('common.actions.resend') }}
                  </template>
                </XhButton>
                <XhButton size="sm" variant="ghost" @click="cancelPhoneSetup">
                  {{ t('common.actions.cancel') }}
                </XhButton>
              </div>
            </div>
          </template>

          <!-- 手机禁用流程 -->
          <template v-if="tfDisableTarget === TF_PHONE">
            <div class="pf-inline-form">
              <XhAlertRoot tone="warning" class="pf-full">
                <XhAlertIcon>
                  <Icon icon="lucide:triangle-alert" width="16" height="16" />
                </XhAlertIcon>
                <XhAlertDescription>
                  {{ t('component.profile.security.phone_disable_hint') }}
                </XhAlertDescription>
              </XhAlertRoot>
              <div class="pf-otp-row">
                <XhPinInputRoot
                  v-model:value="tfDisableCode"
                  :length="6"
                  otp
                  @value-complete="() => confirmDisable()"
                >
                  <!-- 格间距长在格子自己身上，这层包裹只负责排成一行 -->
                  <div style="display: flex">
                    <XhPinInputInput v-for="i in 6" :key="i" :index="i - 1" />
                  </div>
                </XhPinInputRoot>
                <XhButton tone="danger" size="sm" :loading="tfLoading" @click="confirmDisable">
                  {{ t('component.profile.security.btn_disable') }}
                </XhButton>
                <XhButton
                  size="sm" variant="ghost"
                  :disabled="tfResendSeconds > 0"
                  @click="sendDisableCode(TF_PHONE)"
                >
                  <CodeCountdown v-if="tfResendSeconds > 0" :seconds="tfResendSeconds" @finish="clearCountdown" />
                  <template v-else>
                    {{ t('common.actions.resend') }}
                  </template>
                </XhButton>
                <XhButton size="sm" variant="ghost" @click="cancelDisable">
                  {{ t('common.actions.cancel') }}
                </XhButton>
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
              <XhBadge variant="subtle" :tone="profile?.isLocked ? 'danger' : 'success'" size="sm">
                {{ profile?.isLocked ? t('component.profile.security.value_locked') : t('component.profile.security.value_normal') }}
              </XhBadge>
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
            <XhButton size="sm" tone="warning" ghost @click="handleDeactivateAccount">
              {{ t('component.profile.security.btn_deactivate') }}
            </XhButton>
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
            <XhButton size="sm" tone="danger" ghost @click="handleDeleteAccount">
              {{ t('component.profile.security.btn_delete') }}
            </XhButton>
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
