<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script lang="ts" setup>
import type { PasskeyItem } from '~/types'
import { XhButton, XhButtonLabel, XhButtonPrefix, XhEmptyStateDescription, XhEmptyStateIndicator, XhEmptyStateRoot, XhEmptyStateTitle, XhFieldControl, XhFieldErrorText, XhFieldRoot, XhFormRoot, XhPopconfirmCancelTrigger, XhPopconfirmConfirmTrigger, XhPopconfirmContent, XhPopconfirmPositioner, XhPopconfirmRoot, XhPopconfirmTitle, XhPopconfirmTrigger, XhSpinner, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { onMounted, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { XEditModal, XFieldLabel, XInput } from '~/components'
import { toast } from '~/composables'
import { Icon } from '~/iconify'
import { ApiError } from '~/request'
import { useAppContext } from '~/stores'
import { formatDate, isPasskeyCancelled, passkeysSupported } from '~/utils'

/**
 * 账号安全里的通行密钥：登录方式开放了才出现
 * 添加先核对当前密码，再由浏览器创建；浏览器不支持时照样列出、改名与删除，只是不能添加
 */
defineOptions({ name: 'ProfilePasskeys' })

const { apis } = useAppContext()
const { t } = useI18n()

const available = ref(false)
const supported = passkeysSupported()
const passkeys = ref<PasskeyItem[]>([])
const loading = ref(false)
const loaded = ref(false)

/** 服务端错误取本地化后的 detail（不带错误码前缀），其余取 message */
function messageOf(error: unknown): string | undefined {
  return error instanceof ApiError
    ? error.detail
    : (error as Error | undefined)?.message
}

async function loadPasskeys() {
  loading.value = true
  try {
    passkeys.value = await apis.getPasskeysApi()
    loaded.value = true
  }
  catch (error: unknown) {
    toast.danger(messageOf(error) || t('component.profile.passkeys.err_load_failed'))
  }
  finally {
    loading.value = false
  }
}

onMounted(async () => {
  try {
    available.value = (await apis.getLoginConfigApi()).loginMethods.includes('passkey')
  }
  catch {
    // 取不到登录配置即不出这一节，其余各节照常
    available.value = false
  }
  if (available.value)
    await loadPasskeys()
})

// ==================== 添加与改名 ====================

const formId = useId()
const modalVisible = ref(false)
const mode = ref<'create' | 'rename'>('create')
const editing = ref<PasskeyItem | null>(null)
const name = ref('')
const password = ref('')
const submitted = ref(false)
const submitting = ref(false)

function openCreate() {
  mode.value = 'create'
  editing.value = null
  name.value = ''
  password.value = ''
  submitted.value = false
  modalVisible.value = true
}

function openRename(passkey: PasskeyItem) {
  mode.value = 'rename'
  editing.value = passkey
  name.value = passkey.name
  password.value = ''
  submitted.value = false
  modalVisible.value = true
}

async function onSubmit() {
  submitted.value = true
  if (mode.value === 'create'
    ? !password.value
    : !name.value.trim()) {
    return
  }
  submitting.value = true
  try {
    if (mode.value === 'create') {
      await apis.addPasskeyApi({ password: password.value, name: name.value.trim() || undefined })
      toast.success(t('component.profile.passkeys.msg_added'))
    }
    else {
      await apis.renamePasskeyApi(editing.value!.id, name.value.trim())
      toast.success(t('component.profile.passkeys.msg_renamed'))
    }
    modalVisible.value = false
    await loadPasskeys()
  }
  catch (error: unknown) {
    // 本人在浏览器的弹窗里取消：留在弹窗里，可以再点一次
    if (!isPasskeyCancelled(error))
      toast.danger(messageOf(error) || t('component.profile.passkeys.err_operation_failed'))
  }
  finally {
    submitting.value = false
  }
}

async function handleDelete(passkey: PasskeyItem) {
  try {
    await apis.deletePasskeyApi(passkey.id)
    toast.success(t('component.profile.passkeys.msg_deleted'))
    await loadPasskeys()
  }
  catch (error: unknown) {
    toast.danger(messageOf(error) || t('component.profile.passkeys.err_operation_failed'))
  }
}
</script>

<template>
  <section v-if="available" class="pf-section">
    <div class="pf-section__head">
      <div class="pf-section__heading">
        <div class="pf-section__title">
          <Icon icon="lucide:fingerprint" width="16" />
          <span>{{ t('component.profile.passkeys.section_title') }}</span>
        </div>
        <div class="pf-section__desc">
          {{ supported ? t('component.profile.passkeys.section_desc') : t('component.profile.passkeys.unsupported') }}
        </div>
      </div>
      <div v-if="supported" class="pf-section__extra">
        <XhButton variant="subtle" size="sm" tone="brand" @click="openCreate">
          <XhButtonPrefix><Icon icon="lucide:plus" /></XhButtonPrefix>
          <XhButtonLabel>{{ t('component.profile.passkeys.btn_add') }}</XhButtonLabel>
        </XhButton>
      </div>
    </div>
    <div class="pf-section__body">
      <div class="xh-loading-stage" :class="{ 'is-loading': loading }">
        <div class="xh-loading-stage__veil">
          <XhSpinner />
        </div>
        <XhEmptyStateRoot v-if="passkeys.length === 0 && loaded">
          <XhEmptyStateIndicator>
            <Icon icon="lucide:key-round" width="28" height="28" />
          </XhEmptyStateIndicator>
          <XhEmptyStateTitle>{{ t('component.profile.passkeys.empty') }}</XhEmptyStateTitle>
          <XhEmptyStateDescription>{{ t('component.profile.passkeys.empty_desc') }}</XhEmptyStateDescription>
        </XhEmptyStateRoot>
        <div v-else class="pf-list">
          <div v-for="passkey in passkeys" :key="passkey.id" class="pf-list-item">
            <div class="pf-list-icon pf-list-icon--active">
              <Icon icon="lucide:key-round" width="16" />
            </div>
            <div class="pf-list-body">
              <div class="pf-list-title">
                {{ passkey.name }}
                <XhTagRoot variant="subtle" size="sm" :tone="passkey.backupEligible ? 'success' : 'neutral'">
                  <XhTagLabel>{{ passkey.backupEligible ? t('component.profile.passkeys.tag_synced') : t('component.profile.passkeys.tag_device') }}</XhTagLabel>
                </XhTagRoot>
              </div>
              <div class="pf-list-desc">
                {{ t('component.profile.passkeys.created_at', { time: formatDate(passkey.createdTime) }) }}
                ·
                {{ passkey.lastUsedTime ? t('component.profile.passkeys.last_used', { time: formatDate(passkey.lastUsedTime) }) : t('component.profile.passkeys.never_used') }}
              </div>
            </div>
            <XhButton variant="link" size="sm" class="xh-link-button" @click="openRename(passkey)">
              {{ t('component.profile.passkeys.btn_rename') }}
            </XhButton>
            <XhPopconfirmRoot @confirm="handleDelete(passkey)">
              <XhPopconfirmTrigger as-child>
                <XhButton variant="link" size="sm" tone="danger" class="xh-link-button">
                  {{ t('common.actions.delete') }}
                </XhButton>
              </XhPopconfirmTrigger>
              <XhPopconfirmPositioner>
                <XhPopconfirmContent>
                  <XhPopconfirmTitle>
                    {{ t('component.profile.passkeys.confirm_delete', { name: passkey.name }) }}
                  </XhPopconfirmTitle>
                  <XhPopconfirmCancelTrigger>{{ t('common.actions.cancel') }}</XhPopconfirmCancelTrigger>
                  <XhPopconfirmConfirmTrigger>{{ t('common.actions.confirm') }}</XhPopconfirmConfirmTrigger>
                </XhPopconfirmContent>
              </XhPopconfirmPositioner>
            </XhPopconfirmRoot>
          </div>
        </div>
      </div>
    </div>

    <XEditModal
      v-model:show="modalVisible"
      :title="mode === 'create' ? t('component.profile.passkeys.create_title') : t('component.profile.passkeys.rename_title')"
      :save-text="mode === 'create' ? t('component.profile.passkeys.btn_continue') : undefined"
      :loading="submitting"
      :form-id="formId"
    >
      <XhFormRoot :id="formId" class="xh-edit-form-grid" novalidate @submit="onSubmit">
        <XhFieldRoot class="xh-span-2" :required="mode === 'rename'" :invalid="submitted && mode === 'rename' && !name.trim()">
          <XFieldLabel :help="t('component.profile.passkeys.name_help')">
            {{ t('component.profile.passkeys.name_label') }}
          </XFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="name" name="name" :max-length="64" clearable :placeholder="t('component.profile.passkeys.name_placeholder')" />
          </XhFieldControl>
          <XhFieldErrorText>{{ t('component.profile.passkeys.warn_name_required') }}</XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot v-if="mode === 'create'" class="xh-span-2" required :invalid="submitted && !password">
          <XFieldLabel :help="t('component.profile.passkeys.password_help')">
            {{ t('component.profile.passkeys.password_label') }}
          </XFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="password" name="currentPassword" type="password" autocomplete="current-password" :placeholder="t('component.profile.security.current_password_placeholder')" />
          </XhFieldControl>
          <XhFieldErrorText>{{ t('component.profile.security.warn_password_required') }}</XhFieldErrorText>
        </XhFieldRoot>
      </XhFormRoot>
    </XEditModal>
  </section>
</template>

<style src="./profile-shared.css" />

<style scoped>
.pf-list-title {
  display: flex;
  align-items: center;
  gap: var(--xh-space-2);
}
</style>
