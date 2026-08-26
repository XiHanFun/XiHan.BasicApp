<script lang="ts" setup>
import type { MyOAuthAppItem, MyOAuthAppSecret } from '~/types'
import { XhAlertDescription, XhAlertIcon, XhAlertRoot, XhAlertTitle, XhButton, XhEmptyStateDescription, XhEmptyStateIcon, XhEmptyStateRoot, XhEmptyStateTitle, XhSpinner, XhSwitch, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { XEditModal, XInput, XSelect, XTooltip } from '~/components'
import { dialog, toast } from '~/composables'
import { Icon } from '~/iconify'
import { useAppContext } from '~/stores'
import { copyToClipboard, formatDate } from '~/utils'

defineOptions({ name: 'ProfileTabOAuth' })

const { apis } = useAppContext()
const { t } = useI18n()

const loading = ref(false)
const apps = ref<MyOAuthAppItem[]>([])
/** 一次性客户端密钥（仅创建/重置机密客户端后展示，离开页面即不可再见） */
const newSecret = ref<MyOAuthAppSecret | null>(null)

const modalVisible = ref(false)
const submitting = ref(false)
/** null = 新建；否则为编辑中的应用主键 */
const editingId = ref<number | string | null>(null)
const form = reactive({
  appName: '',
  clientType: 'Confidential' as 'Confidential' | 'Public',
  homepage: '',
  appDescription: '',
  redirectUris: '',
  logo: '',
})
const clientTypeOptions = [
  { label: t('component.profile.oauth.type_confidential'), value: 'Confidential' },
  { label: t('component.profile.oauth.type_public'), value: 'Public' },
]

async function loadApps() {
  loading.value = true
  try {
    apps.value = await apis.getMyOAuthAppsApi()
  }
  catch (e) {
    toast.error((e as Error).message || t('component.profile.oauth.err_load_failed'))
  }
  finally {
    loading.value = false
  }
}

function openCreate() {
  editingId.value = null
  form.appName = ''
  form.clientType = 'Confidential'
  form.homepage = ''
  form.appDescription = ''
  form.redirectUris = ''
  form.logo = ''
  modalVisible.value = true
}

function openEdit(app: MyOAuthAppItem) {
  editingId.value = app.basicId
  form.appName = app.appName
  form.clientType = app.clientType
  form.homepage = app.homepage ?? ''
  form.appDescription = app.appDescription ?? ''
  form.redirectUris = app.redirectUris ?? ''
  form.logo = app.logo ?? ''
  modalVisible.value = true
}

async function handleSubmit() {
  if (!form.appName.trim()) {
    toast.error(t('component.profile.oauth.err_name_required'))
    return false
  }
  if (!form.redirectUris.trim()) {
    toast.error(t('component.profile.oauth.err_callback_required'))
    return false
  }

  submitting.value = true
  try {
    if (editingId.value == null) {
      newSecret.value = await apis.createMyOAuthAppApi({
        appName: form.appName.trim(),
        clientType: form.clientType,
        homepage: form.homepage.trim() || undefined,
        appDescription: form.appDescription.trim() || undefined,
        redirectUris: form.redirectUris.trim(),
        logo: form.logo.trim() || undefined,
      })
      toast.success(t('component.profile.oauth.msg_created'))
    }
    else {
      await apis.updateMyOAuthAppApi({
        basicId: editingId.value,
        appName: form.appName.trim(),
        homepage: form.homepage.trim() || undefined,
        appDescription: form.appDescription.trim() || undefined,
        redirectUris: form.redirectUris.trim(),
        logo: form.logo.trim() || undefined,
      })
      toast.success(t('component.profile.oauth.msg_updated'))
    }
    modalVisible.value = false
    await loadApps()
    return true
  }
  catch (e) {
    toast.error((e as Error).message || t('component.profile.oauth.err_save_failed'))
    return false
  }
  finally {
    submitting.value = false
  }
}

function handleRegenerate(app: MyOAuthAppItem) {
  void dialog.confirm({
    badge: 'warning',
    title: t('component.profile.oauth.regenerate_title'),
    content: t('component.profile.oauth.regenerate_content', { name: app.appName }),
    okText: t('component.profile.oauth.confirm_regenerate'),
    cancelText: t('common.actions.cancel'),
    onOk: async () => {
      try {
        newSecret.value = await apis.regenerateMyOAuthAppSecretApi(app.basicId)
        toast.success(t('component.profile.oauth.msg_secret_regenerated'))
        await loadApps()
      }
      catch (e) {
        toast.error((e as Error).message || t('component.profile.oauth.err_save_failed'))
      }
    },
  })
}

async function handleToggleStatus(app: MyOAuthAppItem, enabled: boolean) {
  try {
    await apis.updateMyOAuthAppStatusApi(app.basicId, enabled ? 'Enabled' : 'Disabled')
    toast.success(enabled ? t('component.profile.oauth.msg_enabled') : t('component.profile.oauth.msg_disabled'))
    await loadApps()
  }
  catch (e) {
    toast.error((e as Error).message || t('component.profile.oauth.err_save_failed'))
    await loadApps()
  }
}

function handleDelete(app: MyOAuthAppItem) {
  void dialog.confirm({
    badge: 'error',
    tone: 'danger',
    title: t('component.profile.oauth.delete_title'),
    content: t('component.profile.oauth.delete_content', { name: app.appName }),
    okText: t('component.profile.oauth.confirm_delete'),
    cancelText: t('common.actions.cancel'),
    onOk: async () => {
      try {
        await apis.deleteMyOAuthAppApi(app.basicId)
        if (newSecret.value?.basicId === app.basicId) {
          newSecret.value = null
        }
        toast.success(t('component.profile.oauth.msg_deleted'))
        await loadApps()
      }
      catch (e) {
        toast.error((e as Error).message || t('component.profile.oauth.err_delete_failed'))
      }
    },
  })
}

function copyText(text: string) {
  void copyToClipboard(text).then(() => toast.success(t('component.profile.oauth.msg_copied')))
}

onMounted(() => {
  void loadApps()
})
</script>

<template>
  <div class="pf-tab-body">
    <section class="pf-section">
      <div class="pf-section__head">
        <div class="pf-section__heading">
          <div class="pf-section__title">
            <Icon icon="lucide:blocks" width="16" />
            <span>{{ t('component.profile.oauth.section') }}</span>
          </div>
          <div class="pf-section__desc">
            {{ t('component.profile.oauth.section_desc') }}
          </div>
        </div>
        <div class="pf-section__extra">
          <XhButton size="sm" tone="brand" @click="openCreate">
            <span><Icon icon="lucide:plus" /></span>
            {{ t('component.profile.oauth.btn_create') }}
          </XhButton>
        </div>
      </div>
      <div class="pf-section__body">
        <XhAlertRoot v-if="newSecret" tone="warning" class="pf-secret-alert">
          <XhAlertIcon>
            <Icon icon="lucide:triangle-alert" width="16" height="16" />
          </XhAlertIcon>
          <XhAlertTitle>{{ t('component.profile.oauth.secret_alert_title') }}</XhAlertTitle>
          <XhAlertDescription>
            <div class="pf-secret-row">
              <span class="pf-secret-label">Client ID</span>
              <div class="xh-input-group">
                <XInput :value="newSecret.clientId" readonly size="sm" />
                <XhButton size="sm" @click="copyText(newSecret.clientId)">
                  <span><Icon icon="lucide:copy" /></span>
                </XhButton>
              </div>
            </div>
            <div v-if="newSecret.clientType === 'Confidential' && newSecret.clientSecret" class="pf-secret-row">
              <span class="pf-secret-label">Secret</span>
              <div class="xh-input-group">
                <XInput :value="newSecret.clientSecret" readonly size="sm" type="password" />
                <XhButton size="sm" @click="copyText(newSecret.clientSecret)">
                  <span><Icon icon="lucide:copy" /></span>
                </XhButton>
              </div>
            </div>
            <div v-else class="pf-secret-public-hint">
              {{ t('component.profile.oauth.secret_alert_public') }}
            </div>
          </XhAlertDescription>
        </XhAlertRoot>

        <div class="xh-loading-stage" :class="{ 'is-loading': loading }">
          <div class="xh-loading-stage__veil">
            <XhSpinner />
          </div>
          <XhEmptyStateRoot v-if="apps.length === 0 && !loading" class="pf-empty">
            <XhEmptyStateIcon>
              <Icon icon="lucide:inbox" width="28" height="28" />
            </XhEmptyStateIcon>
            <XhEmptyStateTitle>{{ t('common.no_data') }}</XhEmptyStateTitle>
            <XhEmptyStateDescription>{{ t('component.profile.oauth.empty') }}</XhEmptyStateDescription>
          </XhEmptyStateRoot>
          <div v-else class="pf-list">
            <div v-for="app in apps" :key="String(app.basicId)" class="pf-list-item pf-credential">
              <div class="pf-list-body">
                <div class="pf-list-title pf-credential__name">
                  <span>{{ app.appName }}</span>
                  <XhTagRoot variant="subtle" size="sm" :tone="app.clientType === 'Public' ? 'info' : 'neutral'">
                    <XhTagLabel>
                      {{ app.clientType === 'Public' ? t('component.profile.oauth.tag_public') : t('component.profile.oauth.tag_confidential') }}
                    </XhTagLabel>
                  </XhTagRoot>
                  <XhTagRoot variant="subtle" size="sm" :tone="app.status === 'Enabled' ? 'success' : 'neutral'">
                    <XhTagLabel>
                      {{ app.status === 'Enabled' ? t('component.profile.oauth.tag_enabled') : t('component.profile.oauth.tag_disabled') }}
                    </XhTagLabel>
                  </XhTagRoot>
                </div>
                <div class="pf-credential__key">
                  <code>{{ app.clientId }}</code>
                  <XhButton size="sm" variant="ghost" @click="copyText(app.clientId)">
                    <span><Icon icon="lucide:copy" /></span>
                  </XhButton>
                </div>
                <div v-if="app.redirectUris" class="pf-list-desc pf-oauth__callback">
                  {{ t('component.profile.oauth.callback_label') }}: {{ app.redirectUris }}
                </div>
                <div class="pf-list-desc">
                  {{ t('component.profile.oauth.created_at', { time: formatDate(app.createdTime) }) }}
                </div>
              </div>
              <div class="pf-credential__actions">
                <XhSwitch
                  :title="t('component.profile.oauth.tooltip_toggle')"
                  size="sm"
                  :checked="app.status === 'Enabled'"
                  @update:checked="(v: boolean) => handleToggleStatus(app, v)"
                />
                <XTooltip :content="t('component.profile.oauth.tooltip_edit')">
                  <XhButton size="sm" variant="ghost" @click="openEdit(app)">
                    <span><Icon icon="lucide:pencil" /></span>
                  </XhButton>
                </XTooltip>
                <XTooltip :content="t('component.profile.oauth.tooltip_regenerate')">
                  <XhButton size="sm" variant="ghost" @click="handleRegenerate(app)">
                    <span><Icon icon="lucide:rotate-ccw" /></span>
                  </XhButton>
                </XTooltip>
                <XTooltip :content="t('component.profile.oauth.tooltip_delete')">
                  <XhButton size="sm" variant="ghost" tone="danger" @click="handleDelete(app)">
                    <span><Icon icon="lucide:trash-2" /></span>
                  </XhButton>
                </XTooltip>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>

    <XEditModal
      v-model:show="modalVisible"
      :width="520"
      :title="editingId == null ? t('component.profile.oauth.create_title') : t('component.profile.oauth.edit_title')"
      :save-text="t('component.profile.oauth.btn_submit')"
      :loading="submitting"
      @save="handleSubmit"
    >
      <div class="pf-oauth-form">
        <div class="pf-oauth-field">
          <label class="pf-oauth-field__label">{{ t('component.profile.oauth.field_name') }}</label>
          <XInput v-model:value="form.appName" :placeholder="t('component.profile.oauth.field_name_ph')" :max-length="100" />
        </div>
        <div class="pf-oauth-field">
          <label class="pf-oauth-field__label">{{ t('component.profile.oauth.field_type') }}</label>
          <XSelect v-model:value="form.clientType" :options="clientTypeOptions" :disabled="editingId != null" />
        </div>
        <div class="pf-oauth-field">
          <label class="pf-oauth-field__label">{{ t('component.profile.oauth.field_callback') }}</label>
          <XInput
            v-model:value="form.redirectUris"
            type="textarea"
            :placeholder="t('component.profile.oauth.field_callback_ph')"
            :autosize="{ minRows: 2, maxRows: 4 }"
            :max-length="2000"
          />
        </div>
        <div class="pf-oauth-field">
          <label class="pf-oauth-field__label">{{ t('component.profile.oauth.field_homepage') }}</label>
          <XInput v-model:value="form.homepage" placeholder="https://example.com" :max-length="200" />
        </div>
        <div class="pf-oauth-field">
          <label class="pf-oauth-field__label">{{ t('component.profile.oauth.field_desc') }}</label>
          <XInput
            v-model:value="form.appDescription"
            type="textarea"
            :placeholder="t('component.profile.oauth.field_desc_ph')"
            :autosize="{ minRows: 2, maxRows: 3 }"
            :max-length="500"
          />
        </div>
        <div class="pf-oauth-field">
          <label class="pf-oauth-field__label">{{ t('component.profile.oauth.field_logo') }}</label>
          <XInput v-model:value="form.logo" placeholder="https://example.com/logo.png" :max-length="500" />
        </div>
      </div>
    </XEditModal>
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
  width: 68px;
  font-size: 12px;
  font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
  opacity: 0.75;
}

.pf-secret-public-hint {
  margin-top: 8px;
  font-size: 12px;
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

.pf-oauth__callback {
  word-break: break-all;
}

.pf-oauth-form {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.pf-oauth-field {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.pf-oauth-field__label {
  font-size: 13px;
  font-weight: 500;
  opacity: 0.85;
}
</style>
