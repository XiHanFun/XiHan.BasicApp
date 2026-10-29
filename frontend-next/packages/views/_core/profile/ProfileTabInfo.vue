<script lang="ts" setup>
import type { UserProfile } from '~/types'
import { XhButton, XhButtonIndicator, XhButtonLabel, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { XDatePicker, XInput, XSelect, XUserAvatar } from '~/components'
import { dialog, prompt, toast } from '~/composables'
import { islandStart } from '~/composables/useDynamicIsland'
import { Icon } from '~/iconify'
import { ApiError } from '~/request'
import { useAppContext, useUserStore } from '~/stores'

const props = defineProps<{ profile: UserProfile | null }>()
const emit = defineEmits<{ saved: [] }>()

const { t } = useI18n()
const userStore = useUserStore()
const { apis } = useAppContext()

/** 服务端错误取本地化后的 detail（不带错误码前缀），其余取 message */
function messageOf(error: unknown): string | undefined {
  return error instanceof ApiError ? error.detail : (error as Error | undefined)?.message
}

// ==================== 用户名修改 ====================

const usernameChangeLoading = ref(false)

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
  // 弹窗里问两个值：新用户名与当前密码。校验不过返回 false，弹窗留在原地让用户改
  void prompt({
    title: t('component.profile.info.change_username_title'),
    description: t('component.profile.info.change_username_hint'),
    okText: t('component.profile.info.confirm_change'),
    cancelText: t('common.actions.cancel'),
    fields: [
      {
        key: 'userName',
        value: props.profile?.userName ?? '',
        placeholder: t('component.profile.info.new_username_placeholder'),
      },
      {
        key: 'password',
        type: 'password',
        placeholder: t('component.profile.info.current_password_placeholder'),
      },
    ],
    onOk: async (values) => {
      const userName = values.userName?.trim() ?? ''
      if (!userName) {
        toast.warning(t('component.profile.info.msg_username_required'))
        return false
      }
      if (!values.password) {
        toast.warning(t('component.profile.info.msg_password_required'))
        return false
      }
      usernameChangeLoading.value = true
      try {
        // 改好后壳层把会话里的用户换成回来的那份（顶栏与侧栏的 @用户名 随之刷新）
        await apis.changeUserNameApi({ userName, password: values.password })
        toast.success(t('component.profile.info.msg_username_updated'))
        emit('saved')
      }
      catch (e: unknown) {
        toast.danger(messageOf(e) || t('component.profile.info.err_username_update_failed'))
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
})

const genderOptions = computed(() => [
  { label: t('component.profile.info.gender_unset'), value: 0 },
  { label: t('common.gender.male'), value: 1 },
  { label: t('common.gender.female'), value: 2 },
])

// ==================== 头像上传 / 删除 ====================
// 与 4.x 不同：4.x 先把文件传进文件库、再经 updateProfileApi 把 fileId 写进资料（去掉头像写空串）；
// 6.0 的头像端点上传即换成本人的头像并回收旧的，去掉头像走 removeAvatarApi（profile.remove-avatar），资料保存不再带头像。
// 会话里的用户由壳层换成后端回的那份（userStore 是会话的投影，顶栏与侧栏头像随之刷新），这里只重拉资料。

const AVATAR_MAX_SIZE = 2 * 1024 * 1024 // 2MB
const AVATAR_ACCEPT = ['image/jpeg', 'image/png']

const avatarInputRef = ref<HTMLInputElement | null>(null)
const avatarUploading = ref(false)
const avatarRemoving = ref(false)

/** 头像原始值（fileId），用于按钮禁用判断与显示 */
const currentAvatar = computed(() => props.profile?.avatar || userStore.avatar)

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
    toast.warning(t('component.profile.info.warn_avatar_format'))
    return
  }
  if (file.size > AVATAR_MAX_SIZE) {
    toast.warning(t('component.profile.info.warn_avatar_size', { size: AVATAR_MAX_SIZE / 1024 / 1024 }))
    return
  }
  avatarUploading.value = true
  // 灵动岛实时进度（确定性进度条）
  const task = islandStart('avatar-upload', t('island.avatar.uploading'), { icon: 'lucide:image-up', progress: 0 })
  try {
    // 上传即换好（后端按文件头再认一次格式与大小），返回的 fileId 已在会话里，显示时再换签名地址
    await apis.uploadAvatarApi(file, percent => task.setProgress(percent))
    emit('saved')
    task.success(t('island.avatar.updated'))
  }
  catch (e: unknown) {
    task.error(messageOf(e) || t('island.avatar.upload_failed'))
  }
  finally {
    avatarUploading.value = false
  }
}

function handleAvatarRemove() {
  if (!currentAvatar.value) {
    toast.warning(t('component.profile.info.warn_no_avatar_to_remove'))
    return
  }
  void dialog.confirm({
    badge: 'warning',
    tone: 'danger',
    title: t('component.profile.info.remove_avatar_title'),
    content: t('component.profile.info.remove_avatar_content'),
    okText: t('component.profile.info.confirm_remove'),
    cancelText: t('common.actions.cancel'),
    onOk: async () => {
      avatarRemoving.value = true
      try {
        await apis.removeAvatarApi()
        emit('saved')
        toast.success(t('component.profile.info.msg_avatar_removed'))
      }
      catch (e: unknown) {
        toast.danger(messageOf(e) || t('component.profile.info.err_avatar_remove_failed'))
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
  }
}

watch(() => props.profile, syncProfileForm, { immediate: true })

async function saveProfile() {
  profileSaving.value = true
  try {
    // 整体覆盖（6.0 的 profile.update 不收头像）；显示名进顶栏由壳层刷新会话里的用户（4.x 在这里改 userStore）
    await apis.updateProfileApi({
      ...profileForm.value,
      birthday: profileForm.value.birthday ? new Date(profileForm.value.birthday).toISOString() : undefined,
    })
    toast.success(t('component.profile.info.msg_profile_updated'))
    emit('saved')
  }
  catch (e: unknown) {
    toast.danger(messageOf(e) || t('component.profile.info.err_profile_save_failed'))
  }
  finally {
    profileSaving.value = false
  }
}

// ==================== 邮箱 / 手机 ====================
// 4.x 在这里验证当前邮箱 / 手机、换绑新地址（发码、输码、换绑对话框）。6.0 的发码依赖邮件短信回落的平台配置（待拍板），
// 还没有对应的后端：页签只显示当前值，验证 / 换绑的按钮、对话框与壳层接口等发码能力接入后再从 4.x 搬回
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
            <XhButton
              variant="subtle"
              tone="brand"
              :loading="avatarUploading"
              @click="triggerAvatarUpload"
            >
              <XhButtonIndicator />
              <XhButtonLabel>{{ t('component.profile.info.btn_upload') }}</XhButtonLabel>
            </XhButton>
            <XhButton
              variant="ghost"
              :loading="avatarRemoving"
              :disabled="!currentAvatar"
              @click="handleAvatarRemove"
            >
              <XhButtonIndicator />
              <XhButtonLabel>{{ t('component.profile.info.btn_remove') }}</XhButtonLabel>
            </XhButton>
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
                    <XhTagRoot v-if="profile?.isSystemAccount" variant="subtle" tone="info" size="sm">
                      <XhTagLabel>
                        {{ t('component.profile.info.tag_system_account') }}
                      </XhTagLabel>
                    </XhTagRoot>
                    <XhTagRoot v-else-if="!profile?.canChangeUserName && usernameHint" variant="subtle" tone="warning" size="sm">
                      <XhTagLabel>
                        {{ usernameHint }}
                      </XhTagLabel>
                    </XhTagRoot>
                  </div>
                  <div class="pf-setting-row__desc">
                    @{{ profile?.userName || '---' }}
                  </div>
                </div>
                <div class="pf-setting-row__control">
                  <XhButton
                    v-if="profile?.canChangeUserName"
                    variant="outline"
                    tone="brand"
                    :loading="usernameChangeLoading"
                    @click="handleChangeUserName"
                  >
                    <XhButtonIndicator />
                    <XhButtonLabel>{{ t('component.profile.info.btn_modify') }}</XhButtonLabel>
                  </XhButton>
                </div>
              </div>

              <!-- 显示名称（6.0 必填，1–64 字） -->
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
                  <XInput v-model:value="profileForm.nickName" :placeholder="t('component.profile.info.nickname_placeholder')" :max-length="64" class="pf-field" :aria-label="t('component.profile.info.field_display_name')" />
                </div>
              </div>
            </div>
          </div>

          <div class="pf-subgroup">
            <div class="pf-subgroup__title">
              {{ t('component.profile.info.subgroup_contact') }}
            </div>
            <!-- 邮箱 / 手机只显示当前值：验证状态与验证、换绑入口等 6.0 的发码能力（见脚本末尾） -->
            <div class="pf-setting-list">
              <!-- 电子邮箱 -->
              <div class="pf-setting-row">
                <div class="pf-setting-row__main">
                  <div class="pf-setting-row__label">
                    {{ t('component.profile.info.field_email') }}
                  </div>
                  <div class="pf-setting-row__desc">
                    {{ profile?.email || t('component.profile.info.value_not_set') }}
                  </div>
                </div>
              </div>

              <!-- 手机号码 -->
              <div class="pf-setting-row">
                <div class="pf-setting-row__main">
                  <div class="pf-setting-row__label">
                    {{ t('component.profile.info.field_phone') }}
                  </div>
                  <div class="pf-setting-row__desc">
                    {{ profile?.phone || t('component.profile.info.value_not_set') }}
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- 个人信息（长度上限与 6.0 的 profile.update 一致：真实姓名、国家或地区 50，简介 500） -->
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
            <XInput v-model:value="profileForm.realName" :placeholder="t('component.profile.info.real_name_placeholder')" :max-length="50" :aria-label="t('component.profile.info.field_real_name')" />
          </div>
          <div class="pf-field-card">
            <span class="pf-field-card__label">{{ t('component.profile.info.field_birthday') }}</span>
            <XDatePicker
              v-model:value="profileForm.birthday"
              :aria-label="t('component.profile.info.field_birthday')"
              clearable
              :max="Date.now()"
              style="width: 100%"
            />
          </div>
          <div class="pf-field-card">
            <span class="pf-field-card__label">{{ t('component.profile.info.field_gender') }}</span>
            <XSelect v-model:value="profileForm.gender" :options="genderOptions" :aria-label="t('component.profile.info.field_gender')" />
          </div>
          <div class="pf-field-card">
            <span class="pf-field-card__label">{{ t('component.profile.info.field_country') }}</span>
            <XInput v-model:value="profileForm.country" :placeholder="t('component.profile.info.country_placeholder')" :max-length="50" :aria-label="t('component.profile.info.field_country')" />
          </div>
          <div class="pf-field-card pf-field-card--block">
            <span class="pf-field-card__label">{{ t('component.profile.info.field_bio') }}</span>
            <XInput
              v-model:value="profileForm.remark"
              type="textarea"
              :placeholder="t('component.profile.info.bio_placeholder')"
              :autosize="{ minRows: 3, maxRows: 6 }"
              :max-length="500"
              show-count
              :aria-label="t('component.profile.info.field_bio')"
            />
          </div>
        </div>
      </div>
      <div class="pf-section__actions">
        <XhButton variant="subtle" @click="syncProfileForm">
          {{ t('common.actions.cancel') }}
        </XhButton>
        <XhButton variant="subtle" tone="brand" :loading="profileSaving" @click="saveProfile">
          <XhButtonIndicator />
          <span><Icon icon="lucide:save" /></span>
          <XhButtonLabel>{{ t('component.profile.info.btn_save_changes') }}</XhButtonLabel>
        </XhButton>
      </div>
    </section>
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
</style>
