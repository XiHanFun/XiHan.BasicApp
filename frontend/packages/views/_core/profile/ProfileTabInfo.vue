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
import { useI18n } from 'vue-i18n'
import { XUserAvatar } from '~/components'
import LocaleSwitcher from '~/components/common/LocaleSwitcher.vue'
import TimezoneSwitcher from '~/components/common/TimezoneSwitcher.vue'
import { islandStart } from '~/composables/useDynamicIsland'
import { Icon } from '~/iconify'
import { useAppContext, useUserStore } from '~/stores'

const props = defineProps<{ profile: UserProfile | null }>()
const emit = defineEmits<{ saved: [] }>()

const { t } = useI18n()
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
    return t('component.profile.system_account_immutable')
  if (props.profile.lastUserNameChangeTime && !props.profile.canChangeUserName) {
    const next = new Date(props.profile.lastUserNameChangeTime)
    next.setDate(next.getDate() + 90)
    const remaining = Math.ceil((next.getTime() - Date.now()) / (1000 * 60 * 60 * 24))
    return t('component.profile.info.username_change_cooldown', { days: remaining })
  }
  return ''
})

function handleChangeUserName() {
  newUserNameInput.value = props.profile?.userName ?? ''
  newUserNamePassword.value = ''
  dialog.create({
    title: t('component.profile.info.change_username_title'),
    content: () => h('div', { style: 'display:flex;flex-direction:column;gap:12px' }, [
      h('p', { style: 'margin:0;color:var(--text-secondary);font-size:13px' }, t('component.profile.info.change_username_hint')),
      h(NInput, {
        'value': newUserNameInput.value,
        'placeholder': t('component.profile.info.new_username_placeholder'),
        'onUpdate:value': (v: string) => {
          newUserNameInput.value = v
        },
      }),
      h(NInput, {
        'type': 'password',
        'value': newUserNamePassword.value,
        'placeholder': t('component.profile.info.current_password_placeholder'),
        'showPasswordOn': 'click',
        'onUpdate:value': (v: string) => {
          newUserNamePassword.value = v
        },
      }),
    ]),
    positiveText: t('component.profile.info.confirm_change'),
    negativeText: t('common.actions.cancel'),
    onPositiveClick: async () => {
      if (!newUserNameInput.value.trim()) {
        message.warning(t('component.profile.info.msg_username_required'))
        return false
      }
      if (!newUserNamePassword.value) {
        message.warning(t('component.profile.info.msg_password_required'))
        return false
      }
      usernameChangeLoading.value = true
      try {
        await apis.changeUserNameApi({
          userName: newUserNameInput.value.trim(),
          password: newUserNamePassword.value,
        })
        message.success(t('component.profile.info.msg_username_updated'))
        emit('saved')
      }
      catch (e: unknown) {
        message.error((e as Error)?.message || t('component.profile.info.err_username_update_failed'))
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

const genderOptions = computed(() => [
  { label: t('component.profile.info.gender_unset'), value: 0 },
  { label: t('common.gender.male'), value: 1 },
  { label: t('common.gender.female'), value: 2 },
])

// ==================== 头像上传 / 删除 ====================

const AVATAR_MAX_SIZE = 2 * 1024 * 1024 // 2MB
const AVATAR_ACCEPT = ['image/jpeg', 'image/png']

const avatarInputRef = ref<HTMLInputElement | null>(null)
const avatarUploading = ref(false)
const avatarRemoving = ref(false)

/** 头像原始值（fileId 或旧数据的直链），用于按钮禁用判断与持久化对比 */
const currentAvatar = computed(() => props.profile?.avatar || userStore.avatar)

/** 把头像写入资料并同步全局状态（其余字段取已保存的资料快照，避免连带提交表单里未保存的编辑） */
async function persistAvatar(avatar: string) {
  const saved = props.profile
  await apis.updateProfileApi({
    nickName: saved?.nickName ?? undefined,
    realName: saved?.realName ?? undefined,
    gender: saved?.gender ?? 0,
    birthday: saved?.birthday ?? undefined,
    country: saved?.country ?? undefined,
    remark: saved?.remark ?? undefined,
    language: saved?.language ?? undefined,
    timeZone: saved?.timeZone ?? undefined,
    avatar,
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
    message.warning(t('component.profile.info.warn_avatar_format'))
    return
  }
  if (file.size > AVATAR_MAX_SIZE) {
    message.warning(t('component.profile.info.warn_avatar_size', { size: AVATAR_MAX_SIZE / 1024 / 1024 }))
    return
  }
  avatarUploading.value = true
  // 灵动岛实时进度（确定性进度条）
  const task = islandStart('avatar-upload', t('island.avatar.uploading'), { icon: 'lucide:image-up', progress: 0 })
  try {
    const { fileId } = await apis.uploadAvatarApi(file, percent => task.setProgress(percent))
    // user.avatar 存文件主键(fileId)，显示时再换取预签名 URL
    await persistAvatar(fileId)
    task.success(t('island.avatar.updated'))
  }
  catch (e: unknown) {
    task.error((e as Error)?.message || t('island.avatar.upload_failed'))
  }
  finally {
    avatarUploading.value = false
  }
}

function handleAvatarRemove() {
  if (!currentAvatar.value) {
    message.warning(t('component.profile.info.warn_no_avatar_to_remove'))
    return
  }
  dialog.warning({
    title: t('component.profile.info.remove_avatar_title'),
    content: t('component.profile.info.remove_avatar_content'),
    positiveText: t('component.profile.info.confirm_remove'),
    negativeText: t('common.actions.cancel'),
    onPositiveClick: async () => {
      avatarRemoving.value = true
      try {
        await persistAvatar('')
        message.success(t('component.profile.info.msg_avatar_removed'))
      }
      catch (e: unknown) {
        message.error((e as Error)?.message || t('component.profile.info.err_avatar_remove_failed'))
        return false
      }
      finally {
        avatarRemoving.value = false
      }
    },
  })
}

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
    message.success(t('component.profile.info.msg_profile_updated'))
    if (userStore.userInfo) {
      userStore.setUserInfo({
        ...userStore.userInfo,
        nickName: profileForm.value.nickName,
      })
    }
    emit('saved')
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || t('component.profile.info.err_profile_save_failed'))
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
    message.success(type === 'email' ? t('component.profile.info.msg_code_sent_email') : t('component.profile.info.msg_code_sent_phone'))
    verifyTarget.value = type
    verifyCode.value = ''
    startTimer(
      verifyCountdown,
      t => (verifyTimer = t),
      Math.min(res.expiresInSeconds, 60),
    )
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || t('component.profile.info.err_code_send_failed'))
  }
  finally {
    verifyLoading.value = false
  }
}

async function confirmVerify() {
  if (verifyCode.value.length < 6) {
    message.warning(t('component.profile.info.warn_code_incomplete'))
    return
  }
  verifyLoading.value = true
  try {
    if (verifyTarget.value === 'email')
      await apis.verifyEmailApi(verifyCode.value)
    else
      await apis.verifyPhoneApi(verifyCode.value)
    message.success(t('component.profile.info.msg_verify_success'))
    cancelVerify()
    emit('saved')
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || t('component.profile.info.err_verify_failed'))
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
    message.warning(changeTarget.value === 'email' ? t('component.profile.info.warn_new_email_required') : t('component.profile.info.warn_new_phone_required'))
    return
  }
  if (!changePassword.value) {
    message.warning(t('component.profile.info.warn_password_required'))
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
    message.success(t('component.profile.info.msg_code_sent'))
    changeCodeSent.value = true
    changeCode.value = ''
    startTimer(
      changeCountdown,
      t => (changeTimer = t),
      Math.min(res.expiresInSeconds, 60),
    )
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || t('component.profile.info.err_code_send_failed'))
  }
  finally {
    changeLoading.value = false
  }
}

async function confirmChange() {
  if (changeCode.value.length < 6) {
    message.warning(t('component.profile.info.warn_code_incomplete'))
    return
  }
  changeLoading.value = true
  try {
    if (changeTarget.value === 'email')
      await apis.confirmChangeEmailApi(changeCode.value)
    else
      await apis.confirmChangePhoneApi(changeCode.value)
    message.success(changeTarget.value === 'email' ? t('component.profile.info.msg_email_updated') : t('component.profile.info.msg_phone_updated'))
    cancelChange()
    emit('saved')
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || t('component.profile.info.err_operation_failed'))
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
    <!-- 账户资料：头像 + 账户标识 + 联系方式 合并为一卡 -->
    <section class="pf-section">
      <div class="pf-section__head">
        <div class="pf-section__heading">
          <div class="pf-section__title">
            <Icon icon="lucide:id-card" width="16" />
            <span>{{ t('component.profile.info.section_account_profile') }}</span>
          </div>
          <div class="pf-section__desc">
            {{ t('component.profile.info.section_account_profile_desc') }}
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
                {{ t('component.profile.info.avatar_label') }}
              </div>
              <div class="pf-setting-row__desc">
                {{ t('component.profile.info.avatar_desc') }}
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
              {{ t('component.profile.info.btn_upload') }}
            </NButton>
            <NButton
              size="small"
              quaternary
              :loading="avatarRemoving"
              :disabled="!currentAvatar"
              @click="handleAvatarRemove"
            >
              {{ t('component.profile.info.btn_remove') }}
            </NButton>
          </div>
        </div>

        <!-- 账户标识 + 联系方式 两列 -->
        <div class="pf-cols-2">
          <div class="pf-subgroup">
            <div class="pf-subgroup__title">
              {{ t('component.profile.info.subgroup_account_id') }}
            </div>
            <div class="pf-setting-list">
              <!-- 用户名 -->
              <div class="pf-setting-row">
                <div class="pf-setting-row__main">
                  <div class="pf-setting-row__label">
                    {{ t('component.profile.info.field_username') }}
                    <NTag v-if="profile?.isSystemAccount" type="info" size="tiny" :bordered="false">
                      {{ t('component.profile.info.tag_system_account') }}
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
                    {{ t('component.profile.info.btn_modify') }}
                  </NButton>
                </div>
              </div>

              <!-- 显示名称 -->
              <div class="pf-setting-row">
                <div class="pf-setting-row__main">
                  <div class="pf-setting-row__label">
                    {{ t('component.profile.info.field_display_name') }}
                  </div>
                  <div class="pf-setting-row__desc">
                    {{ t('component.profile.info.field_display_name_desc') }}
                  </div>
                </div>
                <div class="pf-setting-row__control">
                  <NInput v-model:value="profileForm.nickName" :placeholder="t('component.profile.info.nickname_placeholder')" class="pf-field" />
                </div>
              </div>
            </div>
          </div>

          <div class="pf-subgroup">
            <div class="pf-subgroup__title">
              {{ t('component.profile.info.subgroup_contact') }}
            </div>
            <div class="pf-setting-list">
              <!-- 电子邮箱 -->
              <div class="pf-setting-row pf-setting-row--wrap">
                <div class="pf-setting-row__main">
                  <div class="pf-setting-row__label">
                    {{ t('component.profile.info.field_email') }}
                    <NTag v-if="profile?.emailVerified" type="success" size="tiny" :bordered="false">
                      {{ t('component.profile.info.tag_verified') }}
                    </NTag>
                    <NTag v-else-if="profile?.email" type="warning" size="tiny" :bordered="false">
                      {{ t('component.profile.info.tag_unverified') }}
                    </NTag>
                  </div>
                  <div class="pf-setting-row__desc">
                    {{ profile?.email || t('component.profile.info.value_not_set') }}
                  </div>
                </div>
                <div class="pf-setting-row__control">
                  <NButton size="small" ghost type="primary" @click="openChangeDialog('email')">
                    {{ profile?.email ? t('component.profile.info.btn_modify') : t('component.profile.info.btn_bind') }}
                  </NButton>
                  <NButton
                    v-if="profile?.email && !profile?.emailVerified"
                    size="small"
                    quaternary
                    :loading="verifyLoading && verifyTarget === 'email'"
                    @click="sendVerifyCode('email')"
                  >
                    {{ t('component.profile.info.btn_verify') }}
                  </NButton>
                </div>
                <div v-if="verifyTarget === 'email'" class="pf-inline-form">
                  <NInput v-model:value="verifyCode" :placeholder="t('component.profile.info.verify_code_placeholder')" :maxlength="6" class="pf-field" />
                  <NButton type="primary" :loading="verifyLoading" :disabled="verifyCode.length < 6" @click="confirmVerify">
                    {{ t('common.actions.confirm') }}
                  </NButton>
                  <NButton :disabled="verifyCountdown > 0" quaternary @click="sendVerifyCode('email')">
                    {{ verifyCountdown > 0 ? `${verifyCountdown}s` : t('common.actions.resend') }}
                  </NButton>
                  <NButton quaternary @click="cancelVerify">
                    {{ t('common.actions.cancel') }}
                  </NButton>
                </div>
              </div>

              <!-- 手机号码 -->
              <div class="pf-setting-row pf-setting-row--wrap">
                <div class="pf-setting-row__main">
                  <div class="pf-setting-row__label">
                    {{ t('component.profile.info.field_phone') }}
                    <NTag v-if="profile?.phoneVerified" type="success" size="tiny" :bordered="false">
                      {{ t('component.profile.info.tag_verified') }}
                    </NTag>
                    <NTag v-else-if="profile?.phone" type="warning" size="tiny" :bordered="false">
                      {{ t('component.profile.info.tag_unverified') }}
                    </NTag>
                  </div>
                  <div class="pf-setting-row__desc">
                    {{ profile?.phone || t('component.profile.info.value_not_set') }}
                  </div>
                </div>
                <div class="pf-setting-row__control">
                  <NButton size="small" ghost type="primary" @click="openChangeDialog('phone')">
                    {{ profile?.phone ? t('component.profile.info.btn_modify') : t('component.profile.info.btn_bind') }}
                  </NButton>
                  <NButton
                    v-if="profile?.phone && !profile?.phoneVerified"
                    size="small"
                    quaternary
                    :loading="verifyLoading && verifyTarget === 'phone'"
                    @click="sendVerifyCode('phone')"
                  >
                    {{ t('component.profile.info.btn_verify') }}
                  </NButton>
                </div>
                <div v-if="verifyTarget === 'phone'" class="pf-inline-form">
                  <NInput v-model:value="verifyCode" :placeholder="t('component.profile.info.verify_code_placeholder')" :maxlength="6" class="pf-field" />
                  <NButton type="primary" :loading="verifyLoading" :disabled="verifyCode.length < 6" @click="confirmVerify">
                    {{ t('common.actions.confirm') }}
                  </NButton>
                  <NButton :disabled="verifyCountdown > 0" quaternary @click="sendVerifyCode('phone')">
                    {{ verifyCountdown > 0 ? `${verifyCountdown}s` : t('common.actions.resend') }}
                  </NButton>
                  <NButton quaternary @click="cancelVerify">
                    {{ t('common.actions.cancel') }}
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
            <span>{{ t('component.profile.info.section_personal_info') }}</span>
          </div>
        </div>
      </div>
      <div class="pf-section__body">
        <div class="pf-field-grid">
          <div class="pf-field-card">
            <span class="pf-field-card__label">{{ t('component.profile.info.field_real_name') }}</span>
            <NInput v-model:value="profileForm.realName" :placeholder="t('component.profile.info.real_name_placeholder')" :maxlength="50" />
          </div>
          <div class="pf-field-card">
            <span class="pf-field-card__label">{{ t('component.profile.info.field_birthday') }}</span>
            <NDatePicker
              v-model:value="profileForm.birthday"
              type="date"
              :placeholder="t('component.profile.info.birthday_placeholder')"
              clearable
              :is-date-disabled="(ts: number) => ts > Date.now()"
              style="width: 100%"
            />
          </div>
          <div class="pf-field-card">
            <span class="pf-field-card__label">{{ t('component.profile.info.field_gender') }}</span>
            <NSelect v-model:value="profileForm.gender" :options="genderOptions" />
          </div>
          <div class="pf-field-card">
            <span class="pf-field-card__label">{{ t('component.profile.info.field_country') }}</span>
            <NInput v-model:value="profileForm.country" :placeholder="t('component.profile.info.country_placeholder')" />
          </div>
          <div class="pf-field-card">
            <span class="pf-field-card__label">{{ t('component.profile.info.field_language') }}</span>
            <LocaleSwitcher v-model:value="profileForm.language" variant="select" />
          </div>
          <div class="pf-field-card">
            <span class="pf-field-card__label">{{ t('component.profile.info.field_timezone') }}</span>
            <TimezoneSwitcher v-model:value="profileForm.timeZone" variant="select" />
          </div>
          <div class="pf-field-card pf-field-card--block">
            <span class="pf-field-card__label">{{ t('component.profile.info.field_bio') }}</span>
            <NInput
              v-model:value="profileForm.remark"
              type="textarea"
              :placeholder="t('component.profile.info.bio_placeholder')"
              :autosize="{ minRows: 3, maxRows: 6 }"
              :maxlength="200"
              show-count
            />
          </div>
        </div>
      </div>
      <div class="pf-section__actions">
        <NButton @click="syncProfileForm">
          {{ t('common.actions.cancel') }}
        </NButton>
        <NButton type="primary" :loading="profileSaving" @click="saveProfile">
          <template #icon>
            <NIcon>
              <Icon icon="lucide:save" />
            </NIcon>
          </template>
          {{ t('component.profile.info.btn_save_changes') }}
        </NButton>
      </div>
    </section>

    <!-- 换绑对话框（邮箱/手机共用） -->
    <Teleport to="body">
      <div v-if="changeTarget" class="pf-change-overlay" @click.self="cancelChange">
        <NCard
          class="pf-change-dialog"
          :title="changeTarget === 'email' ? t('component.profile.info.change_email_title') : t('component.profile.info.change_phone_title')"
          size="small"
          closable
          @close="cancelChange"
        >
          <div class="pf-change-body">
            <template v-if="!changeCodeSent">
              <NInput
                v-model:value="changeNewValue"
                :placeholder="changeTarget === 'email' ? t('component.profile.info.new_email_placeholder') : t('component.profile.info.new_phone_placeholder')"
              />
              <NInput
                v-model:value="changePassword"
                type="password"
                :placeholder="t('component.profile.info.current_password_placeholder')"
                show-password-on="click"
              />
              <NButton type="primary" block :loading="changeLoading" @click="sendChangeCode">
                {{ t('component.profile.info.send_code') }}
              </NButton>
            </template>
            <template v-else>
              <p class="pf-change-hint">
                {{ t('component.profile.info.code_sent_to') }} <strong>{{ changeNewValue }}</strong>
              </p>
              <NInput
                v-model:value="changeCode"
                :placeholder="t('component.profile.info.verify_code_placeholder')"
                :maxlength="6"
              />
              <NSpace :size="8">
                <NButton type="primary" :loading="changeLoading" :disabled="changeCode.length < 6" @click="confirmChange">
                  {{ t('common.actions.confirm') }}
                </NButton>
                <NButton :disabled="changeCountdown > 0" quaternary @click="sendChangeCode">
                  {{ changeCountdown > 0 ? t('component.profile.info.resend_after', { seconds: changeCountdown }) : t('component.profile.info.resend_now') }}
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
