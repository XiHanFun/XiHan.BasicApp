<script setup lang="ts">
import type {
  OAuthAppCreateDto,
  OAuthAppDetailDto,
  OAuthAppListItemDto,
  OAuthAppSecretDto,
  OAuthAppUpdateDto,
} from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import {
  NButton,
  NDescriptions,
  NDescriptionsItem,
  NDrawer,
  NDrawerContent,
  NEmpty,
  NForm,
  NFormItem,
  NIcon,
  NInput,
  NInputNumber,
  NModal,
  NScrollbar,
  NSelect,
  NSpace,
  NSpin,
  NSwitch,
  NTag,
  useMessage,
} from 'naive-ui'
import { computed, h, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { appManagementApi, createPageRequest, EnableStatus, OAuthAppType, querySortsFromSchema } from '@/api'
import { Icon, SchemaPage } from '~/components'
import { OAUTH_APP_TYPE_OPTIONS, STATUS_OPTIONS } from '~/constants'
import { useEnumOptions } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'PlatformAppPage' })

interface AppFormModel {
  accessTokenLifetime: number
  appDescription?: string | null
  appName: string
  appType: OAuthAppType
  authorizationCodeLifetime: number
  basicId?: string
  clientId: string
  grantTypes: string
  homepage?: string | null
  logo?: string | null
  redirectUris?: string | null
  refreshTokenLifetime: number
  remark?: string | null
  scopes: string
  skipConsent: boolean
  status: EnableStatus
}

const { t } = useI18n()
const message = useMessage()
const statusOptions = useEnumOptions('EnableStatus', STATUS_OPTIONS)
const appTypeOptions = useEnumOptions('OAuthAppType', OAUTH_APP_TYPE_OPTIONS)

// 布尔筛选项：SchemaSelectOption.value 仅支持 string|number，用 1/0 表示真假
const consentOptions = computed(() => [
  { label: t('openapi.app.consent_skip'), value: 1 },
  { label: t('openapi.app.consent_required'), value: 0 },
])

const schemaPageRef = ref<{ reload: () => Promise<void> } | null>(null)

function reloadApp() {
  void schemaPageRef.value?.reload()
}

function formatSeconds(seconds: number) {
  if (seconds < 60) {
    return t('openapi.app.time_seconds', { value: seconds })
  }
  if (seconds < 3600) {
    return t('openapi.app.time_minutes', { value: Math.floor(seconds / 60) })
  }
  if (seconds < 86400) {
    return t('openapi.app.time_hours', { value: Math.floor(seconds / 3600) })
  }
  return t('openapi.app.time_days', { value: Math.floor(seconds / 86400) })
}

function formatNullable(value: unknown) {
  return value === null || value === undefined || value === '' ? '-' : String(value)
}

function formatNullableDate(value?: string | null) {
  return value ? formatDate(value) : '-'
}

// ── 字段单一事实源 ──────────────────────────────────────────────
const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('openapi.app.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('openapi.app.keyword_placeholder'), width: 250, order: 0 },
  { key: 'appName', title: t('openapi.app.app_name'), dataType: 'string', sortable: true, minWidth: 160, order: 1 },
  { key: 'clientId', title: t('openapi.app.client_id'), dataType: 'string', sortable: true, minWidth: 220, order: 2 },
  {
    key: 'appType',
    title: t('openapi.app.app_type'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    dictionaryCode: 'OAuthAppType',
    options: appTypeOptions.value,
    searchPlaceholder: t('openapi.app.app_type_placeholder'),
    minWidth: 110,
    order: 3,
    render: row => h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, getOptionLabel(appTypeOptions.value, (row as unknown as OAuthAppListItemDto).appType)),
  },
  { key: 'grantTypes', title: t('openapi.app.grant_types'), dataType: 'string', sortable: true, minWidth: 180, order: 4 },
  { key: 'scopes', title: t('openapi.app.scopes'), dataType: 'string', sortable: true, minWidth: 160, order: 5 },
  {
    key: 'accessTokenLifetime',
    title: t('openapi.app.access_token'),
    dataType: 'number',
    sortable: true,
    minWidth: 130,
    order: 6,
    render: row => h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, formatSeconds(Number((row as unknown as OAuthAppListItemDto).accessTokenLifetime || 0))),
  },
  {
    key: 'skipConsent',
    title: t('openapi.app.skip_consent'),
    dataType: 'boolean',
    searchable: true,
    sortable: true,
    options: consentOptions.value,
    searchPlaceholder: t('openapi.app.skip_consent_placeholder'),
    width: 110,
    order: 7,
    render: row => h(NTag, { size: 'small', round: true, type: (row as unknown as OAuthAppListItemDto).skipConsent ? 'warning' : 'default', bordered: false }, () => (row as unknown as OAuthAppListItemDto).skipConsent ? t('openapi.app.tag_skip') : t('openapi.app.tag_confirm')),
  },
  {
    key: 'status',
    title: t('openapi.app.status'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    dictionaryCode: 'EnableStatus',
    options: statusOptions.value,
    searchPlaceholder: t('openapi.app.status_placeholder'),
    width: 90,
    order: 8,
  },
  { key: 'createdTime', title: t('openapi.app.created_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 9 },
])

// ── 资源适配器：归一化查询参数 → 后端 API ──────────────────────
const schema = computed<PageSchema>(() => ({
  pageCode: 'platform.app',
  exportPermission: 'saas:oauth-app:export',
  pageName: t('openapi.app.page_name'),
  batchRemovable: true,
  removePermission: 'saas:oauth-app:delete',
  statusPermission: 'saas:oauth-app:status',
  rowKey: 'basicId',
  scrollX: 1700,
  fields: fields.value,
  resource: {
    page: (params) => {
      const { keyword, skipConsent } = params.filters
      return appManagementApi.page({
        // 排序 + 多选(appType/status)等通用过滤统一走 conditions；后端 FLS 门控 + 默认兜底
        ...createPageRequest({
          page: { pageIndex: params.page, pageSize: params.pageSize },
          conditions: { sorts: querySortsFromSchema(params.sorts), filters: params.conditionFilters ?? [] },
        }),
        keyword: (keyword as string | undefined)?.trim() || null,
        // skipConsent 用 1/0 数值选项表示布尔，清洗为 boolean
        skipConsent: skipConsent === undefined || skipConsent === null || skipConsent === '' ? null : Boolean(Number(skipConsent)),
        // appType/status 改为多选，经 conditions.filters In 下发（不再走 DTO 顶层单值字段）
      }) as unknown as Promise<import('@/api').PageResult<Record<string, unknown>>>
    },
    remove: id => appManagementApi.delete(id),
    updateStatus: (id, enabled) => appManagementApi.updateStatus({ basicId: id, status: enabled ? EnableStatus.Enabled : EnableStatus.Disabled }),
  },
  actions: [
    { key: 'create', title: t('openapi.app.action_create'), scope: 'page', type: 'primary', icon: 'lucide:plus' },
    { key: 'view', title: t('openapi.app.action_view'), scope: 'row' },
    { key: 'edit', title: t('openapi.app.action_edit'), scope: 'row' },
    { key: 'toggle', title: t('openapi.app.action_toggle'), scope: 'row' },
    { key: 'secret', title: t('openapi.app.action_secret'), scope: 'row' },
    { key: 'delete', title: t('openapi.app.action_delete'), scope: 'row' },
  ],
}))

// ── 行/页面操作分发 ─────────────────────────────────────────────
function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as OAuthAppListItemDto | undefined
  switch (payload.key) {
    case 'create':
      handleAdd()
      break
    case 'view':
      if (row) {
        void handleView(row)
      }
      break
    case 'edit':
      if (row) {
        void handleEdit(row)
      }
      break
    case 'toggle':
      if (row) {
        void handleToggleStatus(row)
      }
      break
    case 'secret':
      if (row) {
        void handleRegenerateSecret(row.basicId)
      }
      break
    case 'delete':
      if (row) {
        void handleDelete(row)
      }
      break
  }
}

// ── 详情抽屉 ────────────────────────────────────────────────────
const detailVisible = ref(false)
const detailLoading = ref(false)
const currentDetail = ref<OAuthAppDetailDto | null>(null)

// ── 密钥抽屉 ────────────────────────────────────────────────────
const secretVisible = ref(false)
const currentSecret = ref<OAuthAppSecretDto | null>(null)

// ── 新增/编辑弹窗 ───────────────────────────────────────────────
const modalVisible = ref(false)
const submitLoading = ref(false)
const appForm = ref<AppFormModel>(createDefaultForm())
const modalTitle = computed(() => (appForm.value.basicId ? t('openapi.app.modal_edit') : t('openapi.app.modal_add')))

function createDefaultForm(): AppFormModel {
  return {
    accessTokenLifetime: 3600,
    appDescription: null,
    appName: '',
    appType: OAuthAppType.Web,
    authorizationCodeLifetime: 600,
    clientId: '',
    grantTypes: '',
    homepage: null,
    logo: null,
    redirectUris: null,
    refreshTokenLifetime: 2592000,
    remark: null,
    scopes: '',
    skipConsent: false,
    status: EnableStatus.Enabled,
  }
}

async function handleView(row: OAuthAppListItemDto) {
  detailVisible.value = true
  detailLoading.value = true
  currentDetail.value = null
  try {
    currentDetail.value = await appManagementApi.detail(row.basicId)
  }
  catch {
    currentDetail.value = null
    message.error(t('openapi.app.msg_load_detail_failed'))
  }
  finally {
    detailLoading.value = false
  }
}

function handleAdd() {
  appForm.value = createDefaultForm()
  modalVisible.value = true
}

async function handleEdit(row: OAuthAppListItemDto) {
  try {
    const detail = await appManagementApi.detail(row.basicId)
    if (!detail) {
      message.error(t('openapi.app.msg_load_app_detail_failed'))
      return
    }
    appForm.value = {
      accessTokenLifetime: detail.accessTokenLifetime,
      appDescription: detail.appDescription ?? null,
      appName: detail.appName,
      appType: detail.appType,
      authorizationCodeLifetime: detail.authorizationCodeLifetime,
      basicId: detail.basicId,
      clientId: detail.clientId,
      grantTypes: detail.grantTypes,
      homepage: detail.homepage ?? null,
      logo: detail.logo ?? null,
      redirectUris: detail.redirectUris ?? null,
      refreshTokenLifetime: detail.refreshTokenLifetime,
      remark: detail.remark ?? null,
      scopes: detail.scopes ?? '',
      skipConsent: detail.skipConsent,
      status: detail.status,
    }
    modalVisible.value = true
  }
  catch {
    message.error(t('openapi.app.msg_load_app_detail_failed'))
  }
}

function validateForm() {
  if (!appForm.value.appName.trim()) {
    message.warning(t('openapi.app.msg_input_app_name'))
    return false
  }
  if (!appForm.value.basicId && !appForm.value.clientId.trim()) {
    message.warning(t('openapi.app.msg_input_client_id'))
    return false
  }
  if (!appForm.value.grantTypes.trim()) {
    message.warning(t('openapi.app.msg_input_grant_types'))
    return false
  }
  if (!appForm.value.scopes.trim()) {
    message.warning(t('openapi.app.msg_input_scopes'))
    return false
  }
  return true
}

async function handleSubmit() {
  if (!validateForm()) {
    return
  }
  submitLoading.value = true
  try {
    if (appForm.value.basicId) {
      const updateInput: OAuthAppUpdateDto = {
        accessTokenLifetime: appForm.value.accessTokenLifetime,
        appDescription: appForm.value.appDescription,
        appName: appForm.value.appName.trim(),
        appType: appForm.value.appType,
        authorizationCodeLifetime: appForm.value.authorizationCodeLifetime,
        basicId: appForm.value.basicId,
        grantTypes: appForm.value.grantTypes.trim(),
        homepage: appForm.value.homepage,
        logo: appForm.value.logo,
        redirectUris: appForm.value.redirectUris,
        refreshTokenLifetime: appForm.value.refreshTokenLifetime,
        remark: appForm.value.remark,
        scopes: appForm.value.scopes.trim(),
        skipConsent: appForm.value.skipConsent,
      }
      await appManagementApi.update(updateInput)
      message.success(t('openapi.app.msg_save_success'))
    }
    else {
      const createInput: OAuthAppCreateDto = {
        accessTokenLifetime: appForm.value.accessTokenLifetime,
        appDescription: appForm.value.appDescription,
        appName: appForm.value.appName.trim(),
        appType: appForm.value.appType,
        authorizationCodeLifetime: appForm.value.authorizationCodeLifetime,
        // ClientId 为后端必填、不自动生成（领域服务对空值抛错并做唯一性校验），由前端录入；ClientSecret 留空由后端生成
        clientId: appForm.value.clientId.trim(),
        grantTypes: appForm.value.grantTypes.trim(),
        homepage: appForm.value.homepage,
        logo: appForm.value.logo,
        redirectUris: appForm.value.redirectUris,
        refreshTokenLifetime: appForm.value.refreshTokenLifetime,
        remark: appForm.value.remark,
        scopes: appForm.value.scopes.trim(),
        skipConsent: appForm.value.skipConsent,
        status: appForm.value.status,
      }
      // 新增成功返回客户端密钥，弹出密钥抽屉（仅显示一次）
      const secret = await appManagementApi.create(createInput)
      message.success(t('openapi.app.msg_save_success'))
      if (secret) {
        currentSecret.value = secret
        secretVisible.value = true
      }
    }
    modalVisible.value = false
    reloadApp()
  }
  catch {
    message.error(t('openapi.app.msg_save_failed'))
  }
  finally {
    submitLoading.value = false
  }
}

async function handleRegenerateSecret(id: string) {
  try {
    currentSecret.value = await appManagementApi.regenerateSecret(id)
    secretVisible.value = true
    message.success(t('openapi.app.msg_secret_regenerated'))
  }
  catch {
    message.error(t('openapi.app.msg_secret_regenerate_failed'))
  }
}

function copySecret() {
  if (!currentSecret.value?.clientSecret) {
    return
  }
  navigator.clipboard.writeText(currentSecret.value.clientSecret).then(() => {
    message.success(t('openapi.app.msg_secret_copied'))
  }).catch(() => {
    message.error(t('openapi.app.msg_copy_failed'))
  })
}

async function handleToggleStatus(row: OAuthAppListItemDto) {
  const newStatus = row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled
  try {
    await appManagementApi.updateStatus({
      basicId: row.basicId,
      status: newStatus,
    })
    message.success(newStatus === EnableStatus.Enabled ? t('openapi.app.msg_app_enabled') : t('openapi.app.msg_app_disabled'))
    reloadApp()
  }
  catch {
    message.error(t('openapi.app.msg_update_status_failed'))
  }
}

async function handleDelete(row: OAuthAppListItemDto) {
  try {
    await appManagementApi.delete(row.basicId)
    message.success(t('openapi.app.msg_app_deleted'))
    reloadApp()
  }
  catch {
    message.error(t('openapi.app.msg_delete_failed'))
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
    <NDrawer v-model:show="detailVisible" :width="560">
      <NDrawerContent closable :title="t('openapi.app.detail_title')">
        <NSpin :show="detailLoading">
          <NEmpty v-if="!detailLoading && !currentDetail" class="xh-detail-empty" :description="t('openapi.app.detail_empty')">
            <template #icon>
              <NIcon><Icon icon="lucide:inbox" /></NIcon>
            </template>
          </NEmpty>
          <NScrollbar v-else-if="currentDetail" style="max-height: calc(100vh - 180px)">
            <NDescriptions :column="1" bordered size="small">
              <NDescriptionsItem :label="t('openapi.app.detail_app_name')">
                {{ currentDetail.appName }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('openapi.app.detail_app_description')">
                {{ formatNullable(currentDetail.appDescription) }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('openapi.app.detail_client_id')">
                {{ currentDetail.clientId }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('openapi.app.detail_app_type')">
                {{ getOptionLabel(appTypeOptions, currentDetail.appType) }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('openapi.app.detail_grant_types')">
                {{ formatNullable(currentDetail.grantTypes) }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('openapi.app.detail_scopes')">
                {{ formatNullable(currentDetail.scopes) }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('openapi.app.detail_redirect_uris')">
                {{ formatNullable(currentDetail.redirectUris) }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('openapi.app.detail_homepage')">
                {{ formatNullable(currentDetail.homepage) }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('openapi.app.detail_logo')">
                {{ formatNullable(currentDetail.logo) }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('openapi.app.detail_access_token_lifetime')">
                {{ formatSeconds(currentDetail.accessTokenLifetime) }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('openapi.app.detail_refresh_token_lifetime')">
                {{ formatSeconds(currentDetail.refreshTokenLifetime) }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('openapi.app.detail_authorization_code_lifetime')">
                {{ formatSeconds(currentDetail.authorizationCodeLifetime) }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('openapi.app.detail_skip_consent')">
                {{ currentDetail.skipConsent ? t('openapi.app.detail_skip_consent_yes') : t('openapi.app.detail_skip_consent_no') }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('openapi.app.detail_status')">
                {{ getOptionLabel(statusOptions, currentDetail.status) }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('openapi.app.detail_remark')">
                {{ formatNullable(currentDetail.remark) }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('openapi.app.detail_created_time')">
                {{ formatNullableDate(currentDetail.createdTime) }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('openapi.app.detail_modified_time')">
                {{ formatNullableDate(currentDetail.modifiedTime) }}
              </NDescriptionsItem>
            </NDescriptions>
          </NScrollbar>
        </NSpin>
      </NDrawerContent>
    </NDrawer>

    <!-- 新增/编辑弹窗 -->
    <NModal
      v-model:show="modalVisible"
      :auto-focus="false"
      :bordered="false"
      :title="modalTitle"
      preset="card"
      style="width: 680px; max-width: 92vw"
    >
      <NForm :model="appForm" class="xh-edit-form-grid" label-placement="top">
        <NFormItem :label="t('openapi.app.form_app_name')" path="appName">
          <NInput v-model:value="appForm.appName" clearable :placeholder="t('openapi.app.form_app_name_placeholder')" />
        </NFormItem>
        <NFormItem v-if="!appForm.basicId" :label="t('openapi.app.form_client_id')" path="clientId">
          <NInput v-model:value="appForm.clientId" clearable :placeholder="t('openapi.app.form_client_id_placeholder')" />
        </NFormItem>
        <NFormItem v-else :label="t('openapi.app.form_client_id')" path="clientId">
          <NInput v-model:value="appForm.clientId" disabled />
        </NFormItem>
        <NFormItem v-if="!appForm.basicId" :label="t('openapi.app.form_app_type')" path="appType">
          <NSelect v-model:value="appForm.appType" :options="appTypeOptions" />
        </NFormItem>
        <NFormItem :label="t('openapi.app.form_grant_types')" path="grantTypes">
          <NInput v-model:value="appForm.grantTypes" clearable :placeholder="t('openapi.app.form_grant_types_placeholder')" />
        </NFormItem>
        <NFormItem :label="t('openapi.app.form_scopes')" path="scopes">
          <NInput v-model:value="appForm.scopes" clearable :placeholder="t('openapi.app.form_scopes_placeholder')" />
        </NFormItem>
        <NFormItem :label="t('openapi.app.form_redirect_uris')" path="redirectUris">
          <NInput v-model:value="appForm.redirectUris" clearable :placeholder="t('openapi.app.form_redirect_uris_placeholder')" />
        </NFormItem>
        <NFormItem :label="t('openapi.app.form_homepage')" path="homepage">
          <NInput v-model:value="appForm.homepage" clearable :placeholder="t('openapi.app.form_homepage_placeholder')" />
        </NFormItem>
        <NFormItem :label="t('openapi.app.form_logo')" path="logo">
          <NInput v-model:value="appForm.logo" clearable :placeholder="t('openapi.app.form_logo_placeholder')" />
        </NFormItem>
        <NFormItem :label="t('openapi.app.form_access_token_lifetime')" path="accessTokenLifetime">
          <NInputNumber v-model:value="appForm.accessTokenLifetime" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem :label="t('openapi.app.form_refresh_token_lifetime')" path="refreshTokenLifetime">
          <NInputNumber v-model:value="appForm.refreshTokenLifetime" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem :label="t('openapi.app.form_authorization_code_lifetime')" path="authorizationCodeLifetime">
          <NInputNumber v-model:value="appForm.authorizationCodeLifetime" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem :label="t('openapi.app.form_skip_consent')" path="skipConsent">
          <NSwitch v-model:value="appForm.skipConsent" />
        </NFormItem>
        <NFormItem v-if="!appForm.basicId" :label="t('openapi.app.form_status')" path="status">
          <NSelect v-model:value="appForm.status" :options="statusOptions" />
        </NFormItem>
        <NFormItem :label="t('openapi.app.form_app_description')" path="appDescription">
          <NInput
            v-model:value="appForm.appDescription"
            clearable
            :placeholder="t('openapi.app.form_app_description_placeholder')"
            :rows="2"
            type="textarea"
          />
        </NFormItem>
        <NFormItem :label="t('openapi.app.form_remark')" path="remark">
          <NInput
            v-model:value="appForm.remark"
            clearable
            :placeholder="t('openapi.app.form_remark_placeholder')"
            :rows="2"
            type="textarea"
          />
        </NFormItem>
      </NForm>

      <template #footer>
        <NSpace justify="end">
          <NButton @click="modalVisible = false">
            {{ t('openapi.app.form_cancel') }}
          </NButton>
          <NButton :loading="submitLoading" type="primary" @click="handleSubmit">
            {{ t('openapi.app.form_save') }}
          </NButton>
        </NSpace>
      </template>
    </NModal>

    <!-- 密钥抽屉 -->
    <NDrawer v-model:show="secretVisible" :width="420">
      <NDrawerContent closable :title="t('openapi.app.secret_title')">
        <NSpace v-if="currentSecret" vertical>
          <NDescriptions :column="1" bordered size="small">
            <NDescriptionsItem :label="t('openapi.app.secret_client_id')">
              {{ currentSecret.clientId }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('openapi.app.secret_client_secret')">
              <div class="p-3 font-mono text-sm break-all bg-gray-50 rounded dark:bg-gray-800">
                {{ currentSecret.clientSecret }}
              </div>
            </NDescriptionsItem>
          </NDescriptions>
          <div class="mt-2 text-xs text-gray-400">
            {{ t('openapi.app.secret_tip') }}
          </div>
          <NButton block type="primary" @click="copySecret">
            <template #icon>
              <NIcon><Icon icon="lucide:copy" /></NIcon>
            </template>
            {{ t('openapi.app.secret_copy') }}
          </NButton>
          <NButton block @click="secretVisible = false">
            {{ t('openapi.app.secret_close') }}
          </NButton>
        </NSpace>
      </NDrawerContent>
    </NDrawer>
  </SchemaPage>
</template>

<style scoped>
.xh-detail-empty {
  padding: 48px 0;
}
</style>
