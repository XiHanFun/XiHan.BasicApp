<script lang="ts" setup>
import type { ApiCredentialItem, ApiCredentialSecret } from '~/types'
import {
  NAlert,
  NButton,
  NEmpty,
  NIcon,
  NInput,
  NInputGroup,
  NModal,
  NSelect,
  NSpin,
  NSwitch,
  NTag,
  NTooltip,
  useDialog,
  useMessage,
} from 'naive-ui'
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/iconify'
import { useAppContext } from '~/stores'
import { copyToClipboard, formatDate } from '~/utils'

const { apis } = useAppContext()
const message = useMessage()
const dialog = useDialog()
const { t } = useI18n()

// ── API 凭证 ─────────────────────────────────────────────────────
const credentialsLoading = ref(false)
const credentials = ref<ApiCredentialItem[]>([])
/** 一次性密钥（仅创建/滚动后展示，离开页面即不可再见） */
const newSecret = ref<ApiCredentialSecret | null>(null)

const createModalVisible = ref(false)
const createSubmitting = ref(false)
const createName = ref('')

async function loadCredentials() {
  credentialsLoading.value = true
  try {
    credentials.value = await apis.getApiCredentialsApi()
  }
  catch (e) {
    message.error((e as Error).message || t('component.profile.developer.err_load_failed'))
  }
  finally {
    credentialsLoading.value = false
  }
}

function openCreateModal() {
  createName.value = ''
  createModalVisible.value = true
}

async function handleCreateCredential() {
  createSubmitting.value = true
  try {
    newSecret.value = await apis.createApiCredentialApi(createName.value.trim() || undefined)
    createModalVisible.value = false
    message.success(t('component.profile.developer.msg_credential_created'))
    await loadCredentials()
  }
  catch (e) {
    message.error((e as Error).message || t('component.profile.developer.err_create_failed'))
  }
  finally {
    createSubmitting.value = false
  }
}

function handleRotateSecret(cred: ApiCredentialItem) {
  dialog.warning({
    title: t('component.profile.developer.rotate_title'),
    content: t('component.profile.developer.rotate_content', { name: cred.credentialName }),
    positiveText: t('component.profile.developer.confirm_rotate'),
    negativeText: t('common.actions.cancel'),
    onPositiveClick: async () => {
      try {
        newSecret.value = await apis.rotateApiCredentialSecretApi(cred.basicId)
        message.success(t('component.profile.developer.msg_secret_rotated'))
        await loadCredentials()
      }
      catch (e) {
        message.error((e as Error).message || t('component.profile.developer.err_rotate_failed'))
      }
    },
  })
}

async function handleToggleStatus(cred: ApiCredentialItem, enabled: boolean) {
  try {
    await apis.updateApiCredentialStatusApi(cred.basicId, enabled ? 'Enabled' : 'Disabled')
    message.success(enabled ? t('component.profile.developer.msg_credential_enabled') : t('component.profile.developer.msg_credential_disabled'))
    await loadCredentials()
  }
  catch (e) {
    message.error((e as Error).message || t('component.profile.developer.err_update_status_failed'))
    await loadCredentials()
  }
}

function handleDeleteCredential(cred: ApiCredentialItem) {
  dialog.error({
    title: t('component.profile.developer.delete_title'),
    content: t('component.profile.developer.delete_content', { name: cred.credentialName, key: cred.appKey }),
    positiveText: t('component.profile.developer.confirm_delete'),
    negativeText: t('common.actions.cancel'),
    onPositiveClick: async () => {
      try {
        await apis.deleteApiCredentialApi(cred.basicId)
        if (newSecret.value?.basicId === cred.basicId) {
          newSecret.value = null
        }
        message.success(t('component.profile.developer.msg_credential_deleted'))
        await loadCredentials()
      }
      catch (e) {
        message.error((e as Error).message || t('component.profile.developer.err_delete_failed'))
      }
    },
  })
}

function copyText(text: string) {
  void copyToClipboard(text).then(() => message.success(t('component.profile.developer.msg_copied')))
}

// ── 调用安全设置（签名算法 + IP 白名单，按 用户 × 设置键 持久化） ──
const OPENAPI_SETTING_SCENE = 0 // UserSettingScene.Preference
const OPENAPI_SETTING_KEY = 'developer.openapi'

interface OpenApiSettings { signAlgorithm: string, ipWhitelist: string }

const settingsSaving = ref(false)
const signAlgorithm = ref('HmacSHA256')
const ipWhitelist = ref('')
const signAlgorithmOptions = [
  { label: 'HmacSHA256', value: 'HmacSHA256' },
  { label: 'HmacSHA512', value: 'HmacSHA512' },
  { label: 'RSA-SHA256', value: 'RSA-SHA256' },
]

async function loadOpenApiSettings() {
  try {
    const result = await apis.userSettingApi.get({ scene: OPENAPI_SETTING_SCENE, settingKey: OPENAPI_SETTING_KEY })
    if (result.settingValue) {
      const parsed = JSON.parse(result.settingValue) as Partial<OpenApiSettings>
      signAlgorithm.value = parsed.signAlgorithm || 'HmacSHA256'
      ipWhitelist.value = parsed.ipWhitelist || ''
    }
  }
  catch {
    // 设置缺省即默认值，加载失败不打扰
  }
}

async function handleSaveOpenApiSettings() {
  // 简单校验：每行一个 IP / CIDR
  const invalid = ipWhitelist.value
    .split('\n')
    .map(line => line.trim())
    .filter(Boolean)
    .find(line => !/^\d{1,3}(?:\.\d{1,3}){3}(?:\/\d{1,2})?$/.test(line))
  if (invalid) {
    message.error(t('component.profile.developer.err_ip_whitelist_invalid', { value: invalid }))
    return
  }

  settingsSaving.value = true
  try {
    const payload: OpenApiSettings = { signAlgorithm: signAlgorithm.value, ipWhitelist: ipWhitelist.value.trim() }
    await apis.userSettingApi.save({
      scene: OPENAPI_SETTING_SCENE,
      settingKey: OPENAPI_SETTING_KEY,
      settingValue: JSON.stringify(payload),
    })
    message.success(t('component.profile.developer.msg_settings_saved'))
  }
  catch (e) {
    message.error((e as Error).message || t('component.profile.developer.err_save_failed'))
  }
  finally {
    settingsSaving.value = false
  }
}

onMounted(() => {
  void loadCredentials()
  void loadOpenApiSettings()
})
</script>

<template>
  <div class="pf-tab-body">
    <section class="pf-section">
      <div class="pf-section__head">
        <div class="pf-section__heading">
          <div class="pf-section__title">
            <Icon icon="lucide:key" width="16" />
            <span>{{ t('component.profile.developer.section_credentials') }}</span>
          </div>
          <div class="pf-section__desc">
            {{ t('component.profile.developer.section_credentials_desc') }}
          </div>
        </div>
        <div class="pf-section__extra">
          <NButton size="small" type="primary" @click="openCreateModal">
            <template #icon>
              <NIcon><Icon icon="lucide:plus" /></NIcon>
            </template>{{ t('component.profile.developer.btn_create_credential') }}
          </NButton>
        </div>
      </div>
      <div class="pf-section__body">
        <NAlert v-if="newSecret" type="warning" :title="t('component.profile.developer.secret_alert_title')" :bordered="false" class="pf-secret-alert">
          <div class="pf-secret-row">
            <span class="pf-secret-label">AppKey</span>
            <NInputGroup>
              <NInput :value="newSecret.appKey" readonly size="small" />
              <NButton size="small" @click="copyText(newSecret.appKey)">
                <template #icon>
                  <NIcon><Icon icon="lucide:copy" /></NIcon>
                </template>
              </NButton>
            </NInputGroup>
          </div>
          <div class="pf-secret-row">
            <span class="pf-secret-label">Secret</span>
            <NInputGroup>
              <NInput :value="newSecret.appSecret" readonly size="small" type="password" show-password-on="click" />
              <NButton size="small" @click="copyText(newSecret.appSecret)">
                <template #icon>
                  <NIcon><Icon icon="lucide:copy" /></NIcon>
                </template>
              </NButton>
            </NInputGroup>
          </div>
        </NAlert>

        <NSpin :show="credentialsLoading">
          <NEmpty v-if="credentials.length === 0 && !credentialsLoading" class="pf-empty" :description="t('component.profile.developer.empty_credentials')" />
          <div v-else class="pf-list">
            <div v-for="cred in credentials" :key="String(cred.basicId)" class="pf-list-item pf-credential">
              <div class="pf-list-body">
                <div class="pf-list-title pf-credential__name">
                  <span>{{ cred.credentialName }}</span>
                  <NTag size="small" round :bordered="false" :type="cred.status === 'Enabled' ? 'success' : 'default'">
                    {{ cred.status === 'Enabled' ? t('component.profile.developer.tag_enabled') : t('component.profile.developer.tag_disabled') }}
                  </NTag>
                </div>
                <div class="pf-credential__key">
                  <code>{{ cred.appKey }}</code>
                  <NButton size="tiny" quaternary @click="copyText(cred.appKey)">
                    <template #icon>
                      <NIcon><Icon icon="lucide:copy" /></NIcon>
                    </template>
                  </NButton>
                </div>
                <div class="pf-list-desc">
                  {{ t('component.profile.developer.created_at', { time: formatDate(cred.createdTime) }) }}
                  <template v-if="cred.lastUsedTime">
                    · {{ t('component.profile.developer.last_used', { time: formatDate(cred.lastUsedTime) }) }}
                  </template>
                  <template v-else>
                    · {{ t('component.profile.developer.never_used') }}
                  </template>
                </div>
              </div>
              <div class="pf-credential__actions">
                <NTooltip>
                  <template #trigger>
                    <NSwitch
                      size="small"
                      :value="cred.status === 'Enabled'"
                      @update:value="(v: boolean) => handleToggleStatus(cred, v)"
                    />
                  </template>{{ t('component.profile.developer.tooltip_toggle_status') }}
                </NTooltip>
                <NTooltip>
                  <template #trigger>
                    <NButton size="tiny" quaternary @click="handleRotateSecret(cred)">
                      <template #icon>
                        <NIcon><Icon icon="lucide:rotate-ccw" /></NIcon>
                      </template>
                    </NButton>
                  </template>{{ t('component.profile.developer.tooltip_rotate') }}
                </NTooltip>
                <NTooltip>
                  <template #trigger>
                    <NButton size="tiny" quaternary type="error" @click="handleDeleteCredential(cred)">
                      <template #icon>
                        <NIcon><Icon icon="lucide:trash-2" /></NIcon>
                      </template>
                    </NButton>
                  </template>{{ t('component.profile.developer.tooltip_delete') }}
                </NTooltip>
              </div>
            </div>
          </div>
        </NSpin>
      </div>
    </section>

    <section class="pf-section">
      <div class="pf-section__head">
        <div class="pf-section__heading">
          <div class="pf-section__title">
            <Icon icon="lucide:shield-check" width="16" />
            <span>{{ t('component.profile.developer.section_call_security') }}</span>
          </div>
          <div class="pf-section__desc">
            {{ t('component.profile.developer.section_call_security_desc') }}
          </div>
        </div>
      </div>
      <div class="pf-section__body">
        <div class="pf-setting-list">
          <div class="pf-setting-row">
            <div class="pf-setting-row__main">
              <div class="pf-setting-row__label">
                {{ t('component.profile.developer.field_sign_algorithm') }}
              </div>
              <div class="pf-setting-row__desc">
                {{ t('component.profile.developer.field_sign_algorithm_desc') }}
              </div>
            </div>
            <div class="pf-setting-row__control">
              <NSelect v-model:value="signAlgorithm" :options="signAlgorithmOptions" class="pf-field" size="small" />
            </div>
          </div>
          <div class="pf-setting-row pf-setting-row--block">
            <div class="pf-setting-row__main">
              <div class="pf-setting-row__label">
                {{ t('component.profile.developer.field_ip_whitelist') }}
              </div>
              <div class="pf-setting-row__desc">
                {{ t('component.profile.developer.field_ip_whitelist_desc') }}
              </div>
            </div>
            <NInput
              v-model:value="ipWhitelist"
              type="textarea"
              placeholder="192.168.1.1&#10;10.0.0.0/24"
              :autosize="{ minRows: 3, maxRows: 6 }"
            />
          </div>
        </div>
      </div>
      <div class="pf-section__actions">
        <NButton type="primary" size="small" :loading="settingsSaving" @click="handleSaveOpenApiSettings">
          {{ t('component.profile.developer.btn_save_settings') }}
        </NButton>
      </div>
    </section>

    <NModal
      v-model:show="createModalVisible"
      preset="dialog"
      :title="t('component.profile.developer.create_modal_title')"
      :positive-text="t('component.profile.developer.btn_create')"
      :negative-text="t('common.actions.cancel')"
      :loading="createSubmitting"
      @positive-click="handleCreateCredential"
    >
      <div class="pf-create-form">
        <div class="pf-create-form__tip">
          {{ t('component.profile.developer.create_form_tip') }}
        </div>
        <NInput v-model:value="createName" :placeholder="t('component.profile.developer.create_name_placeholder')" maxlength="100" show-count @keydown.enter="handleCreateCredential" />
      </div>
    </NModal>
  </div>
</template>

<style src="./profile-shared.css" />

<style scoped>
.pf-secret-alert {
  margin-bottom: 16px;
}

.pf-secret-row {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-top: 8px;
}

.pf-secret-label {
  flex-shrink: 0;
  width: 52px;
  font-size: 12px;
  font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
  opacity: 0.75;
}

.pf-credential {
  align-items: center;
}

.pf-credential__name {
  display: flex;
  align-items: center;
  gap: 8px;
}

.pf-credential__key {
  display: flex;
  align-items: center;
  gap: 4px;
  margin: 2px 0;
}

.pf-credential__key code {
  font-size: 12px;
  font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
  opacity: 0.85;
}

.pf-credential__actions {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-shrink: 0;
}

.pf-create-form__tip {
  margin-bottom: 8px;
  font-size: 13px;
  opacity: 0.7;
}
</style>
