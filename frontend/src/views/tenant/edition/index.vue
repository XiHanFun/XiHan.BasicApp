<script setup lang="ts">
import type {
  ApiId,
  PageResult,
  PermissionListItemDto,
  TenantEditionCreateDto,
  TenantEditionListItemDto,
  TenantEditionPermissionListItemDto,
  TenantEditionUpdateDto,
} from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import {
  NButton,
  NCheckbox,
  NDrawer,
  NDrawerContent,
  NForm,
  NFormItem,
  NInput,
  NInputNumber,
  NSelect,
  NSwitch,
  NTag,
  useDialog,
  useMessage,
} from 'naive-ui'
import { computed, h, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  createPageRequest,
  EnableStatus,
  permissionApi,
  querySortsFromSchema,
  tenantEditionApi,
  tenantEditionPermissionApi,
  ValidityStatus,
} from '@/api'
import { STATUS_OPTIONS } from '@/constants'
import { SchemaPage, XEditModal, XPermissionGrantPanel } from '~/components'
import { useEnumOptions, usePermission } from '~/hooks'
import { getOptionLabel } from '~/utils'

defineOptions({ name: 'TenantEditionPage' })

interface EditionFormModel extends TenantEditionCreateDto {
  basicId?: ApiId
}

const message = useMessage()
const dialog = useDialog()
const { hasPermission } = usePermission()
const { t } = useI18n()

const statusOptions = useEnumOptions('EnableStatus', STATUS_OPTIONS)

const boolOptions = computed(() => [
  { label: t('tenant.edition.yes'), value: 1 },
  { label: t('tenant.edition.no'), value: 0 },
])

const schemaPageRef = ref<{ reload: () => Promise<void> } | null>(null)

function reloadList() {
  void schemaPageRef.value?.reload()
}

// ── 字段单一事实源：列 + 搜索 ───────────────────────────────────
const fields = computed<ListFieldSchema[]>(() => [
  // 仅搜索（不作为列）
  { key: 'keyword', title: t('tenant.edition.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('tenant.edition.keyword_placeholder'), width: 240, order: 0 },
  { key: 'editionCode', title: t('tenant.edition.edition_code'), dataType: 'string', sortable: true, minWidth: 140, order: 1 },
  {
    key: 'editionName',
    title: t('tenant.edition.edition_name'),
    dataType: 'string',
    sortable: true,
    minWidth: 150,
    order: 2,
    render: (row) => {
      const r = row as unknown as TenantEditionListItemDto
      return h('div', { style: 'display:flex;flex-direction:column;line-height:1.35' }, [
        h('span', { style: 'font-weight:500' }, r.editionName),
        r.description ? h('span', { style: 'font-size:12px;opacity:.65' }, r.description) : null,
      ])
    },
  },
  {
    key: 'price',
    title: t('tenant.edition.price'),
    dataType: 'money',
    sortable: true,
    width: 110,
    order: 3,
    render: (row) => {
      const r = row as unknown as TenantEditionListItemDto
      if (r.isFree) {
        return h(NTag, { size: 'small', round: true, bordered: false, type: 'success' }, () => t('tenant.edition.free'))
      }
      return h('span', r.price == null ? '-' : `¥ ${r.price}`)
    },
  },
  {
    key: 'billingPeriodMonths',
    title: t('tenant.edition.billing_period'),
    dataType: 'number',
    sortable: true,
    width: 100,
    order: 4,
    render: (row) => {
      const r = row as unknown as TenantEditionListItemDto
      return h('span', r.billingPeriodMonths == null ? '-' : t('tenant.edition.billing_period_months', { count: r.billingPeriodMonths }))
    },
  },
  {
    key: 'userLimit',
    title: t('tenant.edition.user_limit'),
    dataType: 'number',
    sortable: true,
    width: 100,
    order: 5,
    render: (row) => {
      const r = row as unknown as TenantEditionListItemDto
      return h('span', r.userLimit == null ? t('tenant.edition.unlimited') : String(r.userLimit))
    },
  },
  {
    key: 'storageLimit',
    title: t('tenant.edition.storage_limit'),
    dataType: 'number',
    sortable: true,
    width: 110,
    order: 6,
    render: (row) => {
      const r = row as unknown as TenantEditionListItemDto
      return h('span', r.storageLimit == null ? t('tenant.edition.unlimited') : t('tenant.edition.storage_limit_mb', { size: r.storageLimit }))
    },
  },
  {
    key: 'isFree',
    title: t('tenant.edition.is_free'),
    dataType: 'boolean',
    searchable: true,
    sortable: true,
    options: boolOptions.value,
    searchPlaceholder: t('tenant.edition.is_free_placeholder'),
    width: 86,
    order: 7,
    render: (row) => {
      const r = row as unknown as TenantEditionListItemDto
      return h(NTag, { size: 'small', round: true, bordered: false, type: r.isFree ? 'success' : 'default' }, () => (r.isFree ? t('tenant.edition.yes') : t('tenant.edition.no')))
    },
  },
  {
    key: 'isDefault',
    title: t('tenant.edition.is_default'),
    dataType: 'boolean',
    searchable: true,
    sortable: true,
    options: boolOptions.value,
    searchPlaceholder: t('tenant.edition.is_default_placeholder'),
    width: 86,
    order: 8,
    render: (row) => {
      const r = row as unknown as TenantEditionListItemDto
      return r.isDefault
        ? h(NTag, { size: 'small', round: true, bordered: false, type: 'warning' }, () => t('tenant.edition.default_tag'))
        : h('span', { style: 'opacity:.45' }, '-')
    },
  },
  {
    key: 'status',
    title: t('tenant.edition.status'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    dictionaryCode: 'EnableStatus',
    options: statusOptions.value,
    searchPlaceholder: t('tenant.edition.status_placeholder'),
    width: 90,
    order: 9,
    render: (row) => {
      const r = row as unknown as TenantEditionListItemDto
      return h(
        NTag,
        { size: 'small', round: true, bordered: false, type: r.status === EnableStatus.Enabled ? 'success' : 'error' },
        () => getOptionLabel(statusOptions.value, r.status),
      )
    },
  },
  { key: 'sort', title: t('tenant.edition.sort'), dataType: 'number', sortable: true, width: 80, order: 10 },
  { key: 'createdTime', title: t('tenant.edition.created_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 11 },
])

function toStr(v: unknown): string | undefined {
  return (v as string | undefined)?.trim() || undefined
}

function toBool(v: unknown): boolean | undefined {
  if (v === undefined || v === null || v === '') {
    return undefined
  }
  return Boolean(v)
}

const schema = computed<PageSchema>(() => ({
  pageCode: 'tenant.edition',
  exportPermission: 'saas:tenant-edition:export',
  pageName: t('tenant.edition.page_name'),
  statusPermission: 'saas:tenant-edition:status',
  rowKey: 'basicId',
  scrollX: 1500,
  fields: fields.value,
  resource: {
    page: (params) => {
      const f = params.filters
      return tenantEditionApi.page({
        ...createPageRequest({
          page: { pageIndex: params.page, pageSize: params.pageSize },
          conditions: { sorts: querySortsFromSchema(params.sorts), filters: params.conditionFilters ?? [] },
        }),
        keyword: toStr(f.keyword) ?? null,
        isFree: toBool(f.isFree) ?? null,
        isDefault: toBool(f.isDefault) ?? null,
      }) as unknown as Promise<PageResult<Record<string, unknown>>>
    },
    updateStatus: (id, enabled) => tenantEditionApi.updateStatus({ basicId: id, status: enabled ? EnableStatus.Enabled : EnableStatus.Disabled }),
  },
  actions: [
    { key: 'create', title: t('tenant.edition.add'), scope: 'page', type: 'primary', icon: 'lucide:plus', permission: 'saas:tenant-edition:create' },
    { key: 'edit', title: t('tenant.edition.edit'), scope: 'row', icon: 'lucide:pencil', permission: 'saas:tenant-edition:update' },
    {
      key: 'enable',
      title: t('tenant.edition.enable'),
      scope: 'row',
      type: 'success',
      icon: 'lucide:play',
      permission: 'saas:tenant-edition:status',
      visible: row => (row as unknown as TenantEditionListItemDto).status === EnableStatus.Disabled,
    },
    {
      key: 'disable',
      title: t('tenant.edition.disable'),
      scope: 'row',
      type: 'warning',
      icon: 'lucide:pause',
      permission: 'saas:tenant-edition:status',
      visible: row => (row as unknown as TenantEditionListItemDto).status === EnableStatus.Enabled,
    },
    {
      key: 'set-default',
      title: t('tenant.edition.set_default'),
      scope: 'row',
      icon: 'lucide:star',
      permission: 'saas:tenant-edition:default',
      visible: row => !(row as unknown as TenantEditionListItemDto).isDefault,
    },
    { key: 'permissions', title: t('tenant.edition.permissions'), scope: 'row', icon: 'lucide:shield-check', permission: 'saas:tenant-edition-permission:read' },
  ],
}))

// ── 行/页面操作分发 ─────────────────────────────────────────────
function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as TenantEditionListItemDto | undefined
  switch (payload.key) {
    case 'create':
      handleAdd()
      break
    case 'edit':
      if (row) {
        handleEdit(row)
      }
      break
    case 'enable':
      if (row) {
        confirmToggleStatus(row, EnableStatus.Enabled)
      }
      break
    case 'disable':
      if (row) {
        confirmToggleStatus(row, EnableStatus.Disabled)
      }
      break
    case 'set-default':
      if (row) {
        confirmSetDefault(row)
      }
      break
    case 'permissions':
      if (row) {
        openPermissionDrawer(row)
      }
      break
  }
}

// ── 新增/编辑 ───────────────────────────────────────────────────
const modalVisible = ref(false)
const submitLoading = ref(false)
const editionForm = ref<EditionFormModel>(createDefaultForm())
const modalTitle = computed(() => (editionForm.value.basicId ? t('tenant.edition.edit_title') : t('tenant.edition.add_title')))

function createDefaultForm(): EditionFormModel {
  return {
    billingPeriodMonths: null,
    description: null,
    editionCode: '',
    editionName: '',
    isDefault: false,
    isFree: false,
    price: null,
    remark: null,
    sort: 100,
    status: EnableStatus.Enabled,
    storageLimit: null,
    userLimit: null,
  }
}

function normalizeNullable(value?: string | null) {
  const normalized = value?.trim()
  return normalized || null
}

function handleAdd() {
  editionForm.value = createDefaultForm()
  modalVisible.value = true
}

function handleEdit(row: TenantEditionListItemDto) {
  editionForm.value = {
    basicId: row.basicId,
    billingPeriodMonths: row.billingPeriodMonths ?? null,
    description: row.description ?? null,
    editionCode: row.editionCode,
    editionName: row.editionName,
    isDefault: row.isDefault,
    isFree: row.isFree,
    price: row.price ?? null,
    remark: null,
    sort: row.sort,
    status: row.status,
    storageLimit: row.storageLimit ?? null,
    userLimit: row.userLimit ?? null,
  }
  modalVisible.value = true
}

function validateForm() {
  if (!editionForm.value.basicId && !editionForm.value.editionCode.trim()) {
    message.warning(t('tenant.edition.validate_edition_code'))
    return false
  }
  if (!editionForm.value.editionName.trim()) {
    message.warning(t('tenant.edition.validate_edition_name'))
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
    if (editionForm.value.basicId) {
      const updateInput: TenantEditionUpdateDto = {
        basicId: editionForm.value.basicId,
        billingPeriodMonths: editionForm.value.billingPeriodMonths ?? null,
        description: normalizeNullable(editionForm.value.description),
        editionName: editionForm.value.editionName.trim(),
        isDefault: editionForm.value.isDefault,
        isFree: editionForm.value.isFree,
        price: editionForm.value.price ?? null,
        remark: normalizeNullable(editionForm.value.remark),
        sort: editionForm.value.sort,
        storageLimit: editionForm.value.storageLimit ?? null,
        userLimit: editionForm.value.userLimit ?? null,
      }
      await tenantEditionApi.update(updateInput)
    }
    else {
      const createInput: TenantEditionCreateDto = {
        billingPeriodMonths: editionForm.value.billingPeriodMonths ?? null,
        description: normalizeNullable(editionForm.value.description),
        editionCode: editionForm.value.editionCode.trim(),
        editionName: editionForm.value.editionName.trim(),
        isDefault: editionForm.value.isDefault,
        isFree: editionForm.value.isFree,
        price: editionForm.value.price ?? null,
        remark: normalizeNullable(editionForm.value.remark),
        sort: editionForm.value.sort,
        status: editionForm.value.status,
        storageLimit: editionForm.value.storageLimit ?? null,
        userLimit: editionForm.value.userLimit ?? null,
      }
      await tenantEditionApi.create(createInput)
    }

    message.success(t('tenant.edition.save_success'))
    modalVisible.value = false
    reloadList()
  }
  catch (e) {
    message.error((e as Error).message || t('tenant.edition.save_failed'))
  }
  finally {
    submitLoading.value = false
  }
}

// ── 启停/设为默认 ───────────────────────────────────────────────
function confirmToggleStatus(row: TenantEditionListItemDto, next: EnableStatus) {
  const enabling = next === EnableStatus.Enabled
  dialog.warning({
    title: enabling ? t('tenant.edition.confirm_enable_title') : t('tenant.edition.confirm_disable_title'),
    content: enabling
      ? t('tenant.edition.confirm_enable_content', { name: row.editionName })
      : t('tenant.edition.confirm_disable_content', { name: row.editionName }),
    positiveText: enabling ? t('tenant.edition.enable') : t('tenant.edition.disable'),
    negativeText: t('tenant.edition.cancel'),
    onPositiveClick: async () => {
      try {
        await tenantEditionApi.updateStatus({ basicId: row.basicId, status: next })
        message.success(enabling ? t('tenant.edition.enabled') : t('tenant.edition.disabled'))
        reloadList()
      }
      catch (e) {
        message.error((e as Error).message || t('tenant.edition.status_update_failed'))
      }
    },
  })
}

function confirmSetDefault(row: TenantEditionListItemDto) {
  dialog.warning({
    title: t('tenant.edition.confirm_set_default_title'),
    content: t('tenant.edition.confirm_set_default_content', { name: row.editionName }),
    positiveText: t('tenant.edition.set_default'),
    negativeText: t('tenant.edition.cancel'),
    onPositiveClick: async () => {
      try {
        await tenantEditionApi.updateDefault({ basicId: row.basicId })
        message.success(t('tenant.edition.set_default_success'))
        reloadList()
      }
      catch (e) {
        message.error((e as Error).message || t('tenant.edition.set_default_failed'))
      }
    },
  })
}

// ── 版本权限抽屉 ────────────────────────────────────────────────
const canGrantPermission = computed(() => hasPermission('saas:tenant-edition-permission:grant'))
const canRevokePermission = computed(() => hasPermission('saas:tenant-edition-permission:revoke'))
const canUpdateMapping = computed(() => hasPermission('saas:tenant-edition-permission:update'))

const permDrawerVisible = ref(false)
const permLoading = ref(false)
const permError = ref(false)
const permEdition = ref<TenantEditionListItemDto | null>(null)
const permList = ref<TenantEditionPermissionListItemDto[]>([])

const permCatalog = ref<PermissionListItemDto[]>([])
const permPanelRef = ref<{ reset: () => void } | null>(null)
const permDraftGranted = ref<Set<ApiId>>(new Set())
const permDraftStatus = ref<Map<ApiId, ValidityStatus>>(new Map())
const permDirty = ref(false)

/** permissionId → 该版本的权限映射行（含停用态，停用后仍要能看到并启用回来） */
const permByPermissionId = computed(() => {
  const map = new Map<ApiId, TenantEditionPermissionListItemDto>()
  for (const item of permList.value) {
    map.set(item.permissionId, item)
  }
  return map
})

function openPermissionDrawer(row: TenantEditionListItemDto) {
  permEdition.value = row
  permList.value = []
  permDrawerVisible.value = true
  permPanelRef.value?.reset()
  void loadPermissionList()
  void loadPermCatalog()
}

async function loadPermCatalog() {
  if (permCatalog.value.length > 0) {
    return
  }
  try {
    permCatalog.value = await permissionApi.catalog()
  }
  catch {
    permCatalog.value = []
  }
}

async function loadPermissionList() {
  if (!permEdition.value) {
    return
  }
  permLoading.value = true
  permError.value = false
  try {
    permList.value = await tenantEditionPermissionApi.list(permEdition.value.basicId)
    derivePermDraft()
  }
  catch (error) {
    permError.value = true
    permList.value = []
    derivePermDraft()
    message.error((error as Error)?.message || t('tenant.edition.perm_load_failed'))
  }
  finally {
    permLoading.value = false
  }
}

/** 本地草稿：打开抽屉时由现有绑定推导，之后只改本地，保存时一次性提交 */
function derivePermDraft() {
  permDraftGranted.value = new Set(permList.value.map(item => item.permissionId))
  permDraftStatus.value = new Map(permList.value.map(item => [item.permissionId, item.status] as const))
  permDirty.value = false
}

function togglePermGrant(permission: PermissionListItemDto, checked: boolean) {
  const granted = new Set(permDraftGranted.value)
  const status = new Map(permDraftStatus.value)
  if (checked) {
    granted.add(permission.basicId)
    // 新授予默认有效；已有绑定重新勾选时沿用其原状态
    if (!status.has(permission.basicId)) {
      status.set(permission.basicId, ValidityStatus.Valid)
    }
  }
  else {
    granted.delete(permission.basicId)
  }
  permDraftGranted.value = granted
  permDraftStatus.value = status
  permDirty.value = true
}

function togglePermStatus(permissionId: ApiId) {
  const status = new Map(permDraftStatus.value)
  const next = status.get(permissionId) === ValidityStatus.Valid ? ValidityStatus.Invalid : ValidityStatus.Valid
  status.set(permissionId, next)
  permDraftStatus.value = status
  permDirty.value = true
}

async function savePermChanges() {
  const edition = permEdition.value
  if (!edition || permLoading.value) {
    return
  }
  const current = new Map(permList.value.map(item => [item.permissionId, item] as const))
  const grantPermissionIds = [...permDraftGranted.value].filter(permissionId => !current.has(permissionId))
  const revokeEditionPermissionIds = [...current.entries()]
    .filter(([permissionId]) => !permDraftGranted.value.has(permissionId))
    .map(([, item]) => item.basicId)
  // 启停只对留存的既有绑定有意义：本次新授予的还没有绑定主键，撤销掉的也不必再改状态
  const statusChanges = [...current.entries()]
    .filter(([permissionId, item]) =>
      permDraftGranted.value.has(permissionId)
      && permDraftStatus.value.get(permissionId) !== item.status,
    )
    .map(([permissionId, item]) => ({ basicId: item.basicId, status: permDraftStatus.value.get(permissionId)! }))
  if (grantPermissionIds.length === 0 && revokeEditionPermissionIds.length === 0 && statusChanges.length === 0) {
    message.info(t('tenant.edition.perm_no_change'))
    permDirty.value = false
    return
  }
  permLoading.value = true
  try {
    await tenantEditionPermissionApi.batchUpdate({
      editionId: edition.basicId,
      grantPermissionIds,
      revokeEditionPermissionIds,
      statusChanges,
    })
    await loadPermissionList()
    derivePermDraft()
    message.success(t('tenant.edition.perm_saved', {
      grant: grantPermissionIds.length,
      revoke: revokeEditionPermissionIds.length,
      status: statusChanges.length,
    }))
  }
  catch (e) {
    message.error((e as Error).message || t('common.messages.save_failed'))
  }
  finally {
    permLoading.value = false
  }
}
</script>

<template>
  <SchemaPage
    ref="schemaPageRef"
    :schema="schema"
    @action="onAction"
  >
    <XEditModal
      v-model:show="modalVisible"
      :title="modalTitle"
      :loading="submitLoading"
      @save="handleSubmit"
    >
      <NForm :model="editionForm" class="xh-edit-form-grid" label-placement="top">
        <NFormItem :label="t('tenant.edition.edition_code')" path="editionCode">
          <NInput
            v-model:value="editionForm.editionCode"
            :disabled="Boolean(editionForm.basicId)"
            clearable
            :placeholder="t('tenant.edition.edition_code_placeholder')"
          />
        </NFormItem>
        <NFormItem :label="t('tenant.edition.edition_name')" path="editionName">
          <NInput v-model:value="editionForm.editionName" clearable :placeholder="t('tenant.edition.edition_name_placeholder')" />
        </NFormItem>
        <NFormItem :label="t('tenant.edition.price')" path="price">
          <NInputNumber
            v-model:value="editionForm.price"
            :disabled="editionForm.isFree"
            :min="0"
            :precision="2"
            clearable
            :placeholder="t('tenant.edition.price_placeholder')"
          />
        </NFormItem>
        <NFormItem :label="t('tenant.edition.billing_period_form')" path="billingPeriodMonths">
          <NInputNumber
            v-model:value="editionForm.billingPeriodMonths"
            :min="1"
            :precision="0"
            clearable
            :placeholder="t('tenant.edition.billing_period_placeholder')"
          />
        </NFormItem>
        <NFormItem :label="t('tenant.edition.user_limit')" path="userLimit">
          <NInputNumber
            v-model:value="editionForm.userLimit"
            :min="0"
            :precision="0"
            clearable
            :placeholder="t('tenant.edition.user_limit_placeholder')"
          />
        </NFormItem>
        <NFormItem :label="t('tenant.edition.storage_limit_form')" path="storageLimit">
          <NInputNumber
            v-model:value="editionForm.storageLimit"
            :min="0"
            :precision="0"
            clearable
            :placeholder="t('tenant.edition.storage_limit_placeholder')"
          />
        </NFormItem>
        <NFormItem :label="t('tenant.edition.is_free')" path="isFree">
          <NSwitch v-model:value="editionForm.isFree" />
        </NFormItem>
        <NFormItem :label="t('tenant.edition.set_default')" path="isDefault">
          <NSwitch v-model:value="editionForm.isDefault" />
        </NFormItem>
        <NFormItem v-if="!editionForm.basicId" :label="t('tenant.edition.status')" path="status">
          <NSelect v-model:value="editionForm.status" :options="statusOptions" />
        </NFormItem>
        <NFormItem :label="t('tenant.edition.sort')" path="sort">
          <NInputNumber v-model:value="editionForm.sort" :min="0" />
        </NFormItem>
        <NFormItem :label="t('tenant.edition.description')" path="description" class="xh-span-2">
          <NInput
            v-model:value="editionForm.description"
            :rows="2"
            clearable
            :placeholder="t('tenant.edition.description_placeholder')"
            type="textarea"
          />
        </NFormItem>
        <NFormItem :label="t('tenant.edition.remark')" path="remark" class="xh-span-2">
          <NInput
            v-model:value="editionForm.remark"
            :rows="2"
            clearable
            :placeholder="t('tenant.edition.remark_placeholder')"
            type="textarea"
          />
        </NFormItem>
      </NForm>
    </XEditModal>

    <NDrawer v-model:show="permDrawerVisible" :width="760">
      <NDrawerContent :title="t('tenant.edition.perm_drawer_title', { name: permEdition?.editionName ?? '' })" closable>
        <XPermissionGrantPanel
          ref="permPanelRef"
          :items="permCatalog"
          :loading="permLoading"
          :search-placeholder="t('tenant.edition.perm_grant_placeholder')"
          :granted-count-label="t('tenant.edition.perm_granted_count', { count: permDraftGranted.size })"
          :empty-description="t('tenant.edition.perm_empty')"
          :other-group-label="t('tenant.edition.perm_group_other')"
        >
          <template #toolbar>
            <NButton v-if="permError" size="small" @click="loadPermissionList">
              {{ t('tenant.edition.perm_retry') }}
            </NButton>
          </template>
          <template #action="{ item }">
            <NButton
              v-if="permDraftGranted.has(item.basicId) && permByPermissionId.get(item.basicId)"
              :disabled="!canUpdateMapping || permLoading"
              size="tiny"
              :type="permDraftStatus.get(item.basicId) === ValidityStatus.Valid ? 'success' : 'warning'"
              @click="togglePermStatus(item.basicId)"
            >
              {{ permDraftStatus.get(item.basicId) === ValidityStatus.Valid ? t('tenant.edition.perm_enabled') : t('tenant.edition.perm_disabled') }}
            </NButton>
            <NCheckbox
              :checked="permDraftGranted.has(item.basicId)"
              :disabled="permLoading || (permDraftGranted.has(item.basicId) ? !canRevokePermission : !canGrantPermission)"
              @update:checked="(checked: boolean) => togglePermGrant(item as PermissionListItemDto, checked)"
            />
          </template>
        </XPermissionGrantPanel>
        <template #footer>
          <NButton @click="permDrawerVisible = false">
            {{ t('tenant.edition.cancel') }}
          </NButton>
          <NButton type="primary" :loading="permLoading" :disabled="!permDirty" style="margin-left: 8px" @click="savePermChanges">
            {{ t('tenant.edition.perm_save') }}
          </NButton>
        </template>
      </NDrawerContent>
    </NDrawer>
  </SchemaPage>
</template>
