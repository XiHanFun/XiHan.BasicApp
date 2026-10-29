<script lang="ts" setup>
import type { OAuthAppForm, OAuthAppRow, OAuthAppSecret } from './oauth-app'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaPageInstance } from '~/components'
import type { SelectOption } from '~/types'
import { XhButton, XhCheckboxGroupIndicator, XhCheckboxGroupItem, XhCheckboxGroupItemText, XhCheckboxGroupRoot, XhDescriptionsItem, XhDescriptionsLabel, XhDescriptionsRoot, XhDescriptionsValue, XhDrawerCloseTrigger, XhDrawerContent, XhDrawerRoot, XhDrawerTitle, XhEmptyStateDescription, XhEmptyStateIndicator, XhEmptyStateRoot, XhEmptyStateTitle, XhFieldControl, XhFieldLabel, XhFieldRoot, XhFlex, XhFormRoot, XhSpinner, XhSwitch } from '@xihan-ui/vue'
import { computed, reactive, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { deleteConfirmText, Icon, renderTag, SchemaPage, statusConfirmText, XEditModal, XInput, XNumberInput, XSelect } from '~/components'
import { toast } from '~/composables'
import { oauthApps as oauthAppsApi } from '~/contracts'
import { copyToClipboard, formatDate } from '~/utils'
import {
  appTypeLabel,
  clientTypeLabel,
  clientTypeTone,
  emptyOAuthAppForm,
  enabledLabel,
  enabledOptions,
  errorText,
  findOAuthAppProblem,
  formatSeconds,
  GRANT_TYPES,
  LIFETIME_RANGES,
  normalizeGrantTypes,
  normalizeScopes,
  OAUTH_APP_TYPES,
  OAUTH_CLIENT_TYPES,
  oauthAppProblemMessage,
  oauthAppResource,
  ownerLabel,
  REQUIRED_GRANT_TYPE,
  REQUIRED_SCOPE,
  SCOPES,
  toCreateBody,
  toOAuthAppForm,
  toUpdateBody,
} from './oauth-app'

defineOptions({ name: 'OpenPlatformOAuthAppsView' })

const { t } = useI18n()
const resource = oauthAppResource(t)

/** 编辑弹窗的保存钮靠这个 id 关联到表单，点它才会走整表提交 */
const editFormId = useId()
const schemaPageRef = ref<SchemaPageInstance | null>(null)

function reloadApp() {
  void schemaPageRef.value?.reload()
}

const asRow = (row: object) => row as OAuthAppRow
const statusOptions = computed<SelectOption[]>(() => enabledOptions(t))
const appTypeOptions = computed<SelectOption[]>(() => OAUTH_APP_TYPES.map(type => ({ label: appTypeLabel(t, type), value: type })))
const clientTypeOptions = computed<SelectOption[]>(() => OAUTH_CLIENT_TYPES.map(type => ({ label: clientTypeLabel(t, type), value: type })))
/** 授权确认的搜索选项：后端按 `skipConsent eq true / false` 精确匹配 */
const consentOptions = computed<SelectOption[]>(() => [
  { label: t('openplatform.oauth_apps.consent_skip'), value: 'true' },
  { label: t('openplatform.oauth_apps.consent_required'), value: 'false' },
])
/** 归属的搜索选项：后端按 `personal eq true / false` 区分用户在个人中心建的与平台建的 */
const ownerOptions = computed<SelectOption[]>(() => [
  { label: t('openplatform.oauth_apps.owner_platform'), value: 'false' },
  { label: t('openplatform.oauth_apps.owner_personal'), value: 'true' },
])

function formatNullable(value: unknown) {
  return value === null || value === undefined || value === '' ? '-' : String(value)
}

function formatList(values: readonly string[]) {
  return values.length > 0 ? values.join(', ') : '-'
}

function formatNullableDate(value?: null | string) {
  return value ? formatDate(value) : '-'
}

// ── 字段单一事实源（列 + 搜索；仅搜索字段 visible:false；可排序的只放后端白名单里的：名称、Client ID、访问令牌有效期、创建时间） ──
// 导出只取能按选项翻成文案的列：启用与授权确认是布尔值，导出器按字符串选项对不上
const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('openplatform.oauth_apps.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('openplatform.oauth_apps.keyword_placeholder'), width: 250, order: 0 },
  { key: 'appName', title: t('openplatform.oauth_apps.app_name'), dataType: 'string', sortable: true, exportable: true, minWidth: 160, order: 1 },
  { key: 'clientId', title: t('openplatform.oauth_apps.client_id'), dataType: 'string', sortable: true, exportable: true, minWidth: 220, order: 2 },
  { key: 'appType', title: t('openplatform.oauth_apps.app_type'), dataType: 'enum', searchable: true, searchMultiple: true, exportable: true, options: appTypeOptions.value, searchPlaceholder: t('openplatform.oauth_apps.app_type_placeholder'), minWidth: 110, order: 3, render: row => appTypeLabel(t, asRow(row).appType) },
  { key: 'clientType', title: t('openplatform.oauth_apps.client_type'), dataType: 'enum', searchable: true, exportable: true, options: clientTypeOptions.value, searchPlaceholder: t('openplatform.oauth_apps.client_type_placeholder'), width: 110, order: 4, render: row => renderTag(clientTypeLabel(t, asRow(row).clientType), clientTypeTone(asRow(row).clientType)) },
  { key: 'grantTypes', title: t('openplatform.oauth_apps.grant_types'), dataType: 'string', exportable: true, ellipsis: true, minWidth: 180, order: 5, render: row => formatList(asRow(row).grantTypes) },
  { key: 'scopes', title: t('openplatform.oauth_apps.scopes'), dataType: 'string', exportable: true, ellipsis: true, minWidth: 160, order: 6, render: row => formatList(asRow(row).scopes) },
  { key: 'accessTokenLifetime', title: t('openplatform.oauth_apps.access_token'), dataType: 'number', sortable: true, exportable: true, minWidth: 130, order: 7, render: row => formatSeconds(t, asRow(row).accessTokenLifetime) },
  { key: 'skipConsent', title: t('openplatform.oauth_apps.skip_consent'), dataType: 'enum', searchable: true, options: consentOptions.value, searchPlaceholder: t('openplatform.oauth_apps.skip_consent_placeholder'), width: 110, order: 8, render: row => renderTag(asRow(row).skipConsent ? t('openplatform.oauth_apps.tag_skip') : t('openplatform.oauth_apps.tag_confirm'), asRow(row).skipConsent ? 'warning' : 'neutral') },
  { key: 'ownerUserName', title: t('openplatform.oauth_apps.owner'), dataType: 'string', exportable: true, minWidth: 120, order: 9, render: row => ownerLabel(t, asRow(row)) },
  { key: 'personal', title: t('openplatform.oauth_apps.owner'), dataType: 'enum', visible: false, searchable: true, options: ownerOptions.value, searchPlaceholder: t('openplatform.oauth_apps.owner_placeholder'), order: 10 },
  { key: 'isEnabled', title: t('openplatform.oauth_apps.status'), dataType: 'enum', searchable: true, options: statusOptions.value, searchPlaceholder: t('openplatform.oauth_apps.status_placeholder'), width: 90, order: 11, render: row => renderTag(enabledLabel(t, asRow(row).isEnabled), asRow(row).isEnabled ? 'success' : 'danger') },
  { key: 'createdAt', title: t('openplatform.oauth_apps.created_time'), dataType: 'datetime', sortable: true, exportable: true, minWidth: 170, order: 12 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'openplatform.oauth-apps',
  pageName: t('openplatform.oauth_apps.title'),
  exportPermission: 'openplatform.oauth-apps.read',
  batchRemovable: true,
  removePermission: 'openplatform.oauth-apps.delete',
  statusPermission: 'openplatform.oauth-apps.update',
  rowKey: 'id',
  fields: fields.value,
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    { key: 'create', title: t('openplatform.oauth_apps.action_create'), scope: 'page', tone: 'brand', icon: 'lucide:plus', permission: 'openplatform.oauth-apps.create' },
    { key: 'view', title: t('openplatform.oauth_apps.action_view'), scope: 'row', icon: 'lucide:eye' },
    { key: 'edit', title: t('openplatform.oauth_apps.action_edit'), scope: 'row', icon: 'lucide:pencil', permission: 'openplatform.oauth-apps.update' },
    { key: 'toggle', title: t('openplatform.oauth_apps.action_toggle'), scope: 'row', icon: 'lucide:power', confirm: true, confirmText: row => statusConfirmText(t, asRow(row).isEnabled, asRow(row).appName), permission: 'openplatform.oauth-apps.update' },
    // 公开客户端没有密钥（后端拒绝重置），不给入口
    { key: 'secret', title: t('openplatform.oauth_apps.action_secret'), scope: 'row', icon: 'lucide:key-square', tone: 'danger', confirm: true, confirmText: row => t('openplatform.oauth_apps.confirm_secret', { name: asRow(row).appName }), visible: row => asRow(row).clientType === 'Confidential', permission: 'openplatform.oauth-apps.update' },
    { key: 'delete', title: t('openplatform.oauth_apps.action_delete'), scope: 'row', icon: 'lucide:trash-2', tone: 'danger', confirm: true, confirmText: row => deleteConfirmText(t, asRow(row).appName), permission: 'openplatform.oauth-apps.delete' },
  ],
}))

// ── 行/页面操作分发 ─────────────────────────────────────────────
function onAction(payload: SchemaActionPayload) {
  const row = payload.row ? asRow(payload.row) : undefined
  switch (payload.key) {
    case 'create':
      handleAdd()
      break
    case 'view':
      if (row)
        void handleView(row)
      break
    case 'edit':
      if (row)
        void handleEdit(row)
      break
    case 'toggle':
      if (row)
        void handleToggleStatus(row)
      break
    case 'secret':
      if (row)
        void handleRegenerateSecret(row)
      break
    case 'delete':
      if (row)
        void handleDelete(row)
      break
  }
}

// ── 详情抽屉 ────────────────────────────────────────────────────
const detailVisible = ref(false)
const detailLoading = ref(false)
const currentDetail = ref<OAuthAppRow | null>(null)

// ── 密钥抽屉 ────────────────────────────────────────────────────
const secretVisible = ref(false)
const currentSecret = ref<OAuthAppSecret | null>(null)

// ── 新增/编辑弹窗 ───────────────────────────────────────────────
const modalVisible = ref(false)
const submitLoading = ref(false)
const submitError = ref<null | string>(null)
const appForm = reactive<OAuthAppForm>(emptyOAuthAppForm())
const modalTitle = computed(() => (appForm.id ? t('openplatform.oauth_apps.modal_edit') : t('openplatform.oauth_apps.modal_add')))

async function handleView(row: OAuthAppRow) {
  detailVisible.value = true
  detailLoading.value = true
  currentDetail.value = null
  try {
    currentDetail.value = await oauthAppsApi.get({ id: row.id })
  }
  catch (error) {
    currentDetail.value = null
    toast.danger(errorText(error) || t('openplatform.oauth_apps.msg_load_detail_failed'))
  }
  finally {
    detailLoading.value = false
  }
}

function handleAdd() {
  Object.assign(appForm, emptyOAuthAppForm())
  submitError.value = null
  modalVisible.value = true
}

async function handleEdit(row: OAuthAppRow) {
  try {
    const detail = await oauthAppsApi.get({ id: row.id })
    Object.assign(appForm, toOAuthAppForm(detail))
    submitError.value = null
    modalVisible.value = true
  }
  catch (error) {
    toast.danger(errorText(error) || t('openplatform.oauth_apps.msg_load_app_detail_failed'))
  }
}

/** 勾选授权类型 / 范围：授权码与 openid 总在内 */
function changeGrantTypes(value: string[]) {
  appForm.grantTypes = normalizeGrantTypes(value)
}

function changeScopes(value: string[]) {
  appForm.scopes = normalizeScopes(value)
}

async function handleSubmit() {
  const problem = findOAuthAppProblem(appForm)
  if (problem) {
    toast.warning(oauthAppProblemMessage(t, problem))
    return
  }
  submitLoading.value = true
  submitError.value = null
  try {
    if (appForm.id) {
      await oauthAppsApi.update(toUpdateBody({ ...appForm, id: appForm.id }))
      toast.success(t('openplatform.oauth_apps.msg_save_success'))
    }
    else {
      // 新增成功返回客户端密钥，弹出密钥抽屉（仅显示一次；公开客户端没有密钥，只给 Client ID）
      currentSecret.value = await oauthAppsApi.create(toCreateBody(appForm))
      toast.success(t('openplatform.oauth_apps.msg_save_success'))
      secretVisible.value = true
    }
    modalVisible.value = false
    reloadApp()
  }
  catch (error) {
    submitError.value = errorText(error) || t('openplatform.oauth_apps.msg_save_failed')
  }
  finally {
    submitLoading.value = false
  }
}

async function handleRegenerateSecret(row: OAuthAppRow) {
  try {
    currentSecret.value = await oauthAppsApi.resetSecret({ id: row.id })
    secretVisible.value = true
    toast.success(t('openplatform.oauth_apps.msg_secret_regenerated'))
  }
  catch (error) {
    toast.danger(errorText(error) || t('openplatform.oauth_apps.msg_secret_regenerate_failed'))
  }
}

function copyText(text: string, successMessage: string) {
  void copyToClipboard(text).then((copied) => {
    if (copied)
      toast.success(successMessage)
    else
      toast.danger(t('openplatform.oauth_apps.msg_copy_failed'))
  })
}

function copyClientId() {
  if (currentSecret.value)
    copyText(currentSecret.value.clientId, t('openplatform.oauth_apps.msg_client_id_copied'))
}

function copySecret() {
  if (currentSecret.value?.clientSecret)
    copyText(currentSecret.value.clientSecret, t('openplatform.oauth_apps.msg_secret_copied'))
}

async function handleToggleStatus(row: OAuthAppRow) {
  const enabled = !row.isEnabled
  try {
    await resource.updateStatus?.(row.id, enabled)
    toast.success(enabled ? t('openplatform.oauth_apps.msg_app_enabled') : t('openplatform.oauth_apps.msg_app_disabled'))
    reloadApp()
  }
  catch (error) {
    toast.danger(errorText(error) || t('openplatform.oauth_apps.msg_update_status_failed'))
  }
}

async function handleDelete(row: OAuthAppRow) {
  try {
    await resource.remove?.(row.id)
    toast.success(t('openplatform.oauth_apps.msg_app_deleted'))
    reloadApp()
  }
  catch (error) {
    toast.danger(errorText(error) || t('openplatform.oauth_apps.msg_delete_failed'))
  }
}
</script>

<template>
  <SchemaPage
    ref="schemaPageRef"
    :schema="schema"
    @action="onAction"
  >
    <!-- 详情抽屉 -->
    <XhDrawerRoot v-model:open="detailVisible" side="right">
      <XhDrawerContent style="--xh-drawer-size: 560px">
        <XhDrawerTitle>{{ t('openplatform.oauth_apps.detail_title') }}</XhDrawerTitle>
        <XhDrawerCloseTrigger />
        <div class="xh-loading-stage" :class="{ 'is-loading': detailLoading }">
          <div class="xh-loading-stage__veil">
            <XhSpinner />
          </div>
          <XhEmptyStateRoot v-if="!detailLoading && !currentDetail" class="xh-detail-empty">
            <XhEmptyStateIndicator>
              <Icon icon="lucide:inbox" width="28" />
            </XhEmptyStateIndicator>
            <XhEmptyStateTitle>{{ t('common.no_data') }}</XhEmptyStateTitle>
            <XhEmptyStateDescription>{{ t('openplatform.oauth_apps.detail_empty') }}</XhEmptyStateDescription>
          </XhEmptyStateRoot>
          <div v-else-if="currentDetail" class="xh-scroll-area" style="max-height: calc(100vh - 180px)">
            <XhDescriptionsRoot :columns="1" variant="outline" size="sm">
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('openplatform.oauth_apps.detail_app_name') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ currentDetail.appName }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('openplatform.oauth_apps.detail_app_description') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatNullable(currentDetail.appDescription) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('openplatform.oauth_apps.detail_client_id') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ currentDetail.clientId }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('openplatform.oauth_apps.detail_client_type') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ clientTypeLabel(t, currentDetail.clientType) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('openplatform.oauth_apps.detail_app_type') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ appTypeLabel(t, currentDetail.appType) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('openplatform.oauth_apps.detail_grant_types') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatList(currentDetail.grantTypes) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('openplatform.oauth_apps.detail_scopes') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatList(currentDetail.scopes) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('openplatform.oauth_apps.detail_redirect_uris') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatList(currentDetail.redirectUris) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('openplatform.oauth_apps.detail_homepage') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatNullable(currentDetail.homepage) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('openplatform.oauth_apps.detail_logo') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatNullable(currentDetail.logo) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('openplatform.oauth_apps.detail_access_token_lifetime') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatSeconds(t, currentDetail.accessTokenLifetime) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('openplatform.oauth_apps.detail_refresh_token_lifetime') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatSeconds(t, currentDetail.refreshTokenLifetime) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('openplatform.oauth_apps.detail_authorization_code_lifetime') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatSeconds(t, currentDetail.authorizationCodeLifetime) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('openplatform.oauth_apps.detail_skip_consent') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ currentDetail.skipConsent ? t('openplatform.oauth_apps.detail_skip_consent_yes') : t('openplatform.oauth_apps.detail_skip_consent_no') }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('openplatform.oauth_apps.detail_owner') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ ownerLabel(t, currentDetail) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('openplatform.oauth_apps.detail_status') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ enabledLabel(t, currentDetail.isEnabled) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('openplatform.oauth_apps.detail_remark') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatNullable(currentDetail.remark) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('openplatform.oauth_apps.detail_created_time') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatNullableDate(currentDetail.createdAt) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('openplatform.oauth_apps.detail_modified_time') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatNullableDate(currentDetail.modifiedAt) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
            </XhDescriptionsRoot>
          </div>
        </div>
      </XhDrawerContent>
    </XhDrawerRoot>

    <!-- 新增/编辑弹窗 -->
    <XEditModal
      v-model:show="modalVisible"
      :title="modalTitle"
      :loading="submitLoading"
      :form-id="editFormId"
    >
      <XhFormRoot :id="editFormId" class="xh-edit-form-grid" data-testid="oauth-app-form" @submit="handleSubmit">
        <XhFieldRoot required>
          <XhFieldLabel>{{ t('openplatform.oauth_apps.form_app_name') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="appForm.appName" name="appName" clearable :max-length="100" :placeholder="t('openplatform.oauth_apps.form_app_name_placeholder')" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot v-if="!appForm.id">
          <XhFieldLabel>{{ t('openplatform.oauth_apps.form_client_id') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="appForm.clientId" name="clientId" clearable :max-length="100" autocomplete="off" :placeholder="t('openplatform.oauth_apps.form_client_id_placeholder')" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot v-else>
          <XhFieldLabel>{{ t('openplatform.oauth_apps.form_client_id') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="appForm.clientId" name="clientId" disabled />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot v-if="!appForm.id" required>
          <XhFieldLabel>{{ t('openplatform.oauth_apps.form_client_type') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect v-model:value="appForm.clientType" name="clientType" :options="clientTypeOptions" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot v-if="!appForm.id" required>
          <XhFieldLabel>{{ t('openplatform.oauth_apps.form_app_type') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect v-model:value="appForm.appType" name="appType" :options="appTypeOptions" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot class="xh-span-2" required>
          <XhFieldLabel>{{ t('openplatform.oauth_apps.form_grant_types') }}</XhFieldLabel>
          <XhFieldControl>
            <XhCheckboxGroupRoot class="oauth-app-picker" orientation="horizontal" name="grantTypes" :value="appForm.grantTypes" @update:value="changeGrantTypes">
              <XhCheckboxGroupItem v-for="grantType in GRANT_TYPES" :key="grantType" :value="grantType" :disabled="grantType === REQUIRED_GRANT_TYPE">
                <XhCheckboxGroupIndicator />
                <XhCheckboxGroupItemText>{{ grantType }}</XhCheckboxGroupItemText>
              </XhCheckboxGroupItem>
            </XhCheckboxGroupRoot>
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot class="xh-span-2" required>
          <XhFieldLabel>{{ t('openplatform.oauth_apps.form_scopes') }}</XhFieldLabel>
          <XhFieldControl>
            <XhCheckboxGroupRoot class="oauth-app-picker" orientation="horizontal" name="scopes" :value="appForm.scopes" @update:value="changeScopes">
              <XhCheckboxGroupItem v-for="scope in SCOPES" :key="scope" :value="scope" :disabled="scope === REQUIRED_SCOPE">
                <XhCheckboxGroupIndicator />
                <XhCheckboxGroupItemText>{{ scope }}</XhCheckboxGroupItemText>
              </XhCheckboxGroupItem>
            </XhCheckboxGroupRoot>
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot class="xh-span-2" required>
          <XhFieldLabel>{{ t('openplatform.oauth_apps.form_redirect_uris') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="appForm.redirectUris" name="redirectUris" type="textarea" :rows="3" :placeholder="t('openplatform.oauth_apps.form_redirect_uris_placeholder')" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('openplatform.oauth_apps.form_homepage') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="appForm.homepage" name="homepage" clearable :max-length="500" :placeholder="t('openplatform.oauth_apps.form_homepage_placeholder')" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('openplatform.oauth_apps.form_logo') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="appForm.logo" name="logo" clearable :max-length="500" :placeholder="t('openplatform.oauth_apps.form_logo_placeholder')" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot required>
          <XhFieldLabel>{{ t('openplatform.oauth_apps.form_access_token_lifetime') }}</XhFieldLabel>
          <XhFieldControl>
            <XNumberInput v-model:value="appForm.accessTokenLifetime" name="accessTokenLifetime" :precision="0" :min="LIFETIME_RANGES.accessTokenLifetime.min" :max="LIFETIME_RANGES.accessTokenLifetime.max" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot required>
          <XhFieldLabel>{{ t('openplatform.oauth_apps.form_refresh_token_lifetime') }}</XhFieldLabel>
          <XhFieldControl>
            <XNumberInput v-model:value="appForm.refreshTokenLifetime" name="refreshTokenLifetime" :precision="0" :min="LIFETIME_RANGES.refreshTokenLifetime.min" :max="LIFETIME_RANGES.refreshTokenLifetime.max" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot required>
          <XhFieldLabel>{{ t('openplatform.oauth_apps.form_authorization_code_lifetime') }}</XhFieldLabel>
          <XhFieldControl>
            <XNumberInput v-model:value="appForm.authorizationCodeLifetime" name="authorizationCodeLifetime" :precision="0" :min="LIFETIME_RANGES.authorizationCodeLifetime.min" :max="LIFETIME_RANGES.authorizationCodeLifetime.max" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('openplatform.oauth_apps.form_skip_consent') }}</XhFieldLabel>
          <XhFieldControl>
            <XhSwitch v-model:checked="appForm.skipConsent" name="skipConsent" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot v-if="!appForm.id">
          <XhFieldLabel>{{ t('openplatform.oauth_apps.form_status') }}</XhFieldLabel>
          <XhFieldControl>
            <XhSwitch v-model:checked="appForm.isEnabled" name="isEnabled" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot class="xh-span-2">
          <XhFieldLabel>{{ t('openplatform.oauth_apps.form_app_description') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput
              v-model:value="appForm.appDescription"
              name="appDescription"
              clearable
              :max-length="500"
              :placeholder="t('openplatform.oauth_apps.form_app_description_placeholder')"
              :rows="2"
              type="textarea"
            />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot class="xh-span-2">
          <XhFieldLabel>{{ t('openplatform.oauth_apps.form_remark') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput
              v-model:value="appForm.remark"
              name="remark"
              clearable
              :max-length="500"
              :placeholder="t('openplatform.oauth_apps.form_remark_placeholder')"
              :rows="2"
              type="textarea"
            />
          </XhFieldControl>
        </XhFieldRoot>
        <p v-if="submitError" class="xh-span-2 text-caption text-fg-danger" role="alert">
          {{ submitError }}
        </p>
      </XhFormRoot>
    </XEditModal>

    <!-- 密钥抽屉 -->
    <XhDrawerRoot v-model:open="secretVisible" side="right">
      <XhDrawerContent style="--xh-drawer-size: 420px">
        <XhDrawerTitle>{{ t('openplatform.oauth_apps.secret_title') }}</XhDrawerTitle>
        <XhDrawerCloseTrigger />
        <XhFlex v-if="currentSecret" orientation="vertical" gap="md">
          <XhDescriptionsRoot :columns="1" variant="outline" size="sm">
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('openplatform.oauth_apps.secret_client_id') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                <div class="flex items-center gap-1">
                  <span class="font-mono text-sm break-all">{{ currentSecret.clientId }}</span>
                  <XhButton size="sm" variant="ghost" icon-only :aria-label="t('common.actions.copy')" @click="copyClientId">
                    <span><Icon icon="lucide:copy" /></span>
                  </XhButton>
                </div>
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem v-if="currentSecret.clientSecret">
              <XhDescriptionsLabel>{{ t('openplatform.oauth_apps.secret_client_secret') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                <div class="p-3 font-mono text-sm break-all bg-muted rounded">
                  {{ currentSecret.clientSecret }}
                </div>
              </XhDescriptionsValue>
            </XhDescriptionsItem>
          </XhDescriptionsRoot>
          <div class="mt-2 text-caption text-fg-muted">
            {{ currentSecret.clientSecret ? t('openplatform.oauth_apps.secret_tip') : t('openplatform.oauth_apps.secret_public_tip') }}
          </div>
          <XhButton v-if="currentSecret.clientSecret" full-width variant="solid" tone="brand" @click="copySecret">
            <span><Icon icon="lucide:copy" /></span>
            {{ t('openplatform.oauth_apps.secret_copy') }}
          </XhButton>
          <XhButton variant="subtle" full-width @click="secretVisible = false">
            {{ t('openplatform.oauth_apps.secret_close') }}
          </XhButton>
        </XhFlex>
      </XhDrawerContent>
    </XhDrawerRoot>
  </SchemaPage>
</template>

<style scoped>
.xh-detail-empty {
  padding-block: var(--xh-space-8);
}

.oauth-app-picker {
  --xh-checkbox-group-gap: var(--xh-space-3);
}
</style>
