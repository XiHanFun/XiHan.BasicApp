<script setup lang="ts">
import type {
  ApiId,
  NumericString,
  PageResult,
  PermissionListItemDto,
  TenantEditionCreateDto,
  TenantEditionDetailDto,
  TenantEditionListItemDto,
  TenantEditionPermissionListItemDto,
  TenantEditionUpdateDto,
} from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import { XhButton, XhButtonIndicator, XhButtonLabel, XhDrawerCloseTrigger, XhDrawerContent, XhDrawerRoot, XhDrawerTitle, XhFieldControl, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFormFieldGroup, XhFormRoot, XhSwitch, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, h, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  createPageRequest,
  EnableStatus,
  permissionApi,
  PermissionSide,
  querySortsFromSchema,
  tenantEditionApi,
  tenantEditionPermissionApi,
  ValidityStatus,
} from '@/api'
import { STATUS_OPTIONS } from '@/constants'
import { SchemaPage, XEditModal, XInput, XNumberInput, XPermissionTransfer, XSelect } from '~/components'
import { dialog, toast } from '~/composables'
import { useEnumOptions, usePermission } from '~/hooks'
import { Icon } from '~/iconify'
import { getOptionLabel, randomString } from '~/utils'
import { diffEditionGrants, isEmptyEditionGrantDiff, mergeMappedIntoCatalog, validEditionPermissionIds } from './edition-grants'

defineOptions({ name: 'TenantEditionPage' })

interface EditionFormModel extends TenantEditionCreateDto {
  basicId?: ApiId
}

const { hasPermission } = usePermission()
const { t } = useI18n()

/** 编辑弹窗的保存钮靠这个 id 关联到表单，点它才会走整表校验 */
const editFormId = useId()

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
        return h(XhTagRoot, { variant: 'subtle', tone: 'success' }, () => h(XhTagLabel, () => t('tenant.edition.free')))
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
      return h(XhTagRoot, { variant: 'subtle', tone: r.isFree ? 'success' : 'neutral' }, () => h(XhTagLabel, () => (r.isFree ? t('tenant.edition.yes') : t('tenant.edition.no'))))
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
        ? h(XhTagRoot, { variant: 'subtle', tone: 'warning' }, () => h(XhTagLabel, () => t('tenant.edition.default_tag')))
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
      return h(XhTagRoot, { variant: 'subtle', tone: r.status === EnableStatus.Enabled ? 'success' : 'danger' }, () => h(XhTagLabel, () => getOptionLabel(statusOptions.value, r.status)))
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
  exportPermission: 'tenant.edition.export',
  pageName: t('tenant.edition.page_name'),
  statusPermission: 'tenant.edition.status',
  rowKey: 'basicId',
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
    { key: 'create', title: t('tenant.edition.add'), scope: 'page', type: 'primary', icon: 'lucide:plus', permission: 'tenant.edition.create' },
    { key: 'edit', title: t('tenant.edition.edit'), scope: 'row', icon: 'lucide:pencil', permission: 'tenant.edition.update' },
    {
      key: 'enable',
      title: t('tenant.edition.enable'),
      scope: 'row',
      type: 'success',
      icon: 'lucide:play',
      permission: 'tenant.edition.status',
      visible: row => (row as unknown as TenantEditionListItemDto).status === EnableStatus.Disabled,
    },
    {
      key: 'disable',
      title: t('tenant.edition.disable'),
      scope: 'row',
      type: 'warning',
      icon: 'lucide:pause',
      permission: 'tenant.edition.status',
      visible: row => (row as unknown as TenantEditionListItemDto).status === EnableStatus.Enabled,
    },
    {
      key: 'set-default',
      title: t('tenant.edition.set_default'),
      scope: 'row',
      icon: 'lucide:star',
      permission: 'tenant.edition.default',
      visible: row => !(row as unknown as TenantEditionListItemDto).isDefault,
    },
    { key: 'permissions', title: t('tenant.edition.permissions'), scope: 'row', icon: 'lucide:shield-check', permission: 'tenant.edition.permission-read' },
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
        void handleEdit(row)
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

/** 回填表单：存储上限是 long，按字符串传输，数字输入框要数字 */
function toNullableNumber(value?: NumericString | null) {
  return value == null ? null : Number(value)
}

function handleAdd() {
  editionForm.value = createDefaultForm()
  modalVisible.value = true
}

/** 随机生成版本编码：解决「起名困难、图省事」；唯一性由后端唯一索引与前置校验兜底 */
function generateEditionCode() {
  editionForm.value.editionCode = randomString(8)
}

async function handleEdit(row: TenantEditionListItemDto) {
  // 列表行不含备注，取详情回填；否则保存时会把备注覆盖为空
  let detail: TenantEditionDetailDto | null = null
  try {
    detail = await tenantEditionApi.detail(row.basicId)
  }
  catch {
    detail = null
  }
  editionForm.value = {
    basicId: row.basicId,
    billingPeriodMonths: detail?.billingPeriodMonths ?? row.billingPeriodMonths ?? null,
    description: detail?.description ?? row.description ?? null,
    editionCode: detail?.editionCode ?? row.editionCode,
    editionName: detail?.editionName ?? row.editionName,
    isDefault: detail?.isDefault ?? row.isDefault,
    isFree: detail?.isFree ?? row.isFree,
    price: detail?.price ?? row.price ?? null,
    remark: detail?.remark ?? null,
    sort: detail?.sort ?? row.sort,
    status: detail?.status ?? row.status,
    storageLimit: toNullableNumber(detail?.storageLimit ?? row.storageLimit),
    userLimit: detail?.userLimit ?? row.userLimit ?? null,
  }
  modalVisible.value = true
}

function validateForm() {
  if (!editionForm.value.basicId && !editionForm.value.editionCode.trim()) {
    toast.warning(t('tenant.edition.validate_edition_code'))
    return false
  }
  if (!editionForm.value.editionName.trim()) {
    toast.warning(t('tenant.edition.validate_edition_name'))
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

    toast.success(t('tenant.edition.save_success'))
    modalVisible.value = false
    reloadList()
  }
  catch (e) {
    toast.danger((e as Error).message || t('tenant.edition.save_failed'))
  }
  finally {
    submitLoading.value = false
  }
}

// ── 启停/设为默认 ───────────────────────────────────────────────
function confirmToggleStatus(row: TenantEditionListItemDto, next: EnableStatus) {
  const enabling = next === EnableStatus.Enabled
  void dialog.confirm({
    badge: 'warning',
    title: enabling ? t('tenant.edition.confirm_enable_title') : t('tenant.edition.confirm_disable_title'),
    content: enabling
      ? t('tenant.edition.confirm_enable_content', { name: row.editionName })
      : t('tenant.edition.confirm_disable_content', { name: row.editionName }),
    okText: enabling ? t('tenant.edition.enable') : t('tenant.edition.disable'),
    cancelText: t('tenant.edition.cancel'),
    onOk: async () => {
      try {
        await tenantEditionApi.updateStatus({ basicId: row.basicId, status: next })
        toast.success(enabling ? t('tenant.edition.enabled') : t('tenant.edition.disabled'))
        reloadList()
      }
      catch (e) {
        toast.danger((e as Error).message || t('tenant.edition.status_update_failed'))
      }
    },
  })
}

function confirmSetDefault(row: TenantEditionListItemDto) {
  void dialog.confirm({
    badge: 'warning',
    title: t('tenant.edition.confirm_set_default_title'),
    content: t('tenant.edition.confirm_set_default_content', { name: row.editionName }),
    okText: t('tenant.edition.set_default'),
    cancelText: t('tenant.edition.cancel'),
    onOk: async () => {
      try {
        await tenantEditionApi.updateDefault({ basicId: row.basicId })
        toast.success(t('tenant.edition.set_default_success'))
        reloadList()
      }
      catch (e) {
        toast.danger((e as Error).message || t('tenant.edition.set_default_failed'))
      }
    },
  })
}

// ── 版本权限抽屉 ────────────────────────────────────────────────
/** 授予、撤销、重新启用一项都没有时只能看；有其一就放开，提交时后端按实际出现的操作逐项校验 */
const canEditPermission = computed(() =>
  hasPermission('tenant.edition.permission-grant')
  || hasPermission('tenant.edition.permission-revoke')
  || hasPermission('tenant.edition.permission-update'),
)

const permDrawerVisible = ref(false)
const permLoading = ref(false)
/** 加载失败时穿梭框只读：拿不到现有映射就算不出差量，动了也存不对 */
const permError = ref(false)
const permEdition = ref<TenantEditionListItemDto | null>(null)
const permList = ref<TenantEditionPermissionListItemDto[]>([])
const permCatalog = ref<PermissionListItemDto[]>([])
/** 生效中的权限主键，即穿梭框右侧那一栏 */
const permChecked = ref<ApiId[]>([])
/** 条目为权限目录并上目录外的生效映射，否则一动穿梭框它们就被当成撤销 */
const permItems = computed(() => mergeMappedIntoCatalog(permCatalog.value, permList.value))
/** 停用的映射与未授予同在左栏，标出来：移到右栏即重新启用 */
const permDisabledIds = computed(() => new Set(
  permList.value.filter(item => item.status !== ValidityStatus.Valid).map(item => item.permissionId),
))

/**
 * 脏态按差量算，不用回写事件置位的标志位：穿梭框挂载时会把规整后的值回写一次，
 * 标志位会被这一次空回写点亮。比出来的脏态没有这个问题，保存后也会自动归位。
 */
const permDirty = computed(() => !isEmptyEditionGrantDiff(diffEditionGrants(permChecked.value, permList.value)))

/** 权限目录一次取全；平台侧权限进不了租户，套餐白名单里不列 */
async function loadPermCatalog() {
  if (permCatalog.value.length > 0) {
    return
  }
  permCatalog.value = (await permissionApi.catalog()).filter(permission => permission.side !== PermissionSide.Platform)
}

async function openPermissionDrawer(row: TenantEditionListItemDto) {
  permEdition.value = row
  permList.value = []
  permChecked.value = []
  permError.value = false
  permDrawerVisible.value = true
  permLoading.value = true
  try {
    const [, mappings] = await Promise.all([
      loadPermCatalog(),
      tenantEditionPermissionApi.list(row.basicId),
    ])
    permList.value = mappings
    derivePermChecked()
  }
  catch (error) {
    permError.value = true
    toast.danger((error as Error)?.message || t('tenant.edition.perm_load_failed'))
  }
  finally {
    permLoading.value = false
  }
}

/** 本地授予态：打开抽屉时由生效中的映射推导，之后只改本地，保存时一次性提交 */
function derivePermChecked() {
  permChecked.value = validEditionPermissionIds(permList.value)
}

function onPermTransfer(next: (number | string)[]) {
  permChecked.value = next as ApiId[]
}

async function savePermChanges() {
  const edition = permEdition.value
  if (!edition || permLoading.value) {
    return
  }
  const diff = diffEditionGrants(permChecked.value, permList.value)
  if (isEmptyEditionGrantDiff(diff)) {
    toast.info(t('tenant.edition.perm_no_change'))
    return
  }
  permLoading.value = true
  try {
    await tenantEditionPermissionApi.batchUpdate({ editionId: edition.basicId, ...diff })
    permList.value = await tenantEditionPermissionApi.list(edition.basicId)
    derivePermChecked()
    toast.success(t('tenant.edition.perm_saved', {
      grant: diff.grantPermissionIds.length,
      revoke: diff.revokeEditionPermissionIds.length,
      status: diff.statusChanges.length,
    }))
  }
  catch (e) {
    toast.danger((e as Error).message || t('common.messages.save_failed'))
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
      :form-id="editFormId"
    >
      <XhFormRoot
        :id="editFormId"
        v-model:values="editionForm"
        validate-on="blur"
        class="xh-edit-form-grid"
        @submit="handleSubmit"
      >
        <XhFormFieldGroup name="editionCode">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('tenant.edition.edition_code') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput
                v-model:value="editionForm.editionCode"
                :disabled="Boolean(editionForm.basicId)"
                clearable
                :placeholder="t('tenant.edition.edition_code_placeholder')"
              >
                <!-- 随机生成触发器仅新增态出现；type="button" 防止把整表提交掉 -->
                <template #suffix>
                  <button
                    v-if="!editionForm.basicId"
                    type="button"
                    class="edition-code-random"
                    :aria-label="t('tenant.edition.edition_code_random')"
                    :title="t('tenant.edition.edition_code_random')"
                    @click="generateEditionCode"
                  >
                    <Icon width="14" height="14" icon="lucide:dices" />
                  </button>
                </template>
              </XInput>
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="editionName">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('tenant.edition.edition_name') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="editionForm.editionName" clearable :placeholder="t('tenant.edition.edition_name_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="price">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('tenant.edition.price') }}</XhFieldLabel>
            <XhFieldControl>
              <XNumberInput
                v-model:value="editionForm.price"
                :disabled="editionForm.isFree"
                :min="0"
                :precision="2"
                :placeholder="t('tenant.edition.price_placeholder')"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="billingPeriodMonths">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('tenant.edition.billing_period_form') }}</XhFieldLabel>
            <XhFieldControl>
              <XNumberInput
                v-model:value="editionForm.billingPeriodMonths"
                :min="1"
                :precision="0"
                :placeholder="t('tenant.edition.billing_period_placeholder')"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="userLimit">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('tenant.edition.user_limit') }}</XhFieldLabel>
            <XhFieldControl>
              <XNumberInput
                v-model:value="editionForm.userLimit"
                :min="0"
                :precision="0"
                :placeholder="t('tenant.edition.user_limit_placeholder')"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="storageLimit">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('tenant.edition.storage_limit_form') }}</XhFieldLabel>
            <XhFieldControl>
              <XNumberInput
                v-model:value="editionForm.storageLimit"
                :min="0"
                :precision="0"
                :placeholder="t('tenant.edition.storage_limit_placeholder')"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="isFree">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('tenant.edition.is_free') }}</XhFieldLabel>
            <XhFieldControl>
              <XhSwitch v-model:checked="editionForm.isFree" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="isDefault">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('tenant.edition.set_default') }}</XhFieldLabel>
            <XhFieldControl>
              <XhSwitch v-model:checked="editionForm.isDefault" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup v-if="!editionForm.basicId" name="status">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('tenant.edition.status') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect v-model:value="editionForm.status" :options="statusOptions" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="sort">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('tenant.edition.sort') }}</XhFieldLabel>
            <XhFieldControl>
              <XNumberInput v-model:value="editionForm.sort" :min="0" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="description" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('tenant.edition.description') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput
                v-model:value="editionForm.description"
                :rows="2"
                clearable
                :placeholder="t('tenant.edition.description_placeholder')"
                type="textarea"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="remark" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('tenant.edition.remark') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput
                v-model:value="editionForm.remark"
                :rows="2"
                clearable
                :placeholder="t('tenant.edition.remark_placeholder')"
                type="textarea"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
      </XhFormRoot>
    </XEditModal>

    <XhDrawerRoot v-model:open="permDrawerVisible" side="right">
      <XhDrawerContent style="--xh-drawer-size: 980px">
        <XhDrawerTitle>{{ t('tenant.edition.perm_drawer_title', { name: permEdition?.editionName ?? '' }) }}</XhDrawerTitle>
        <XhDrawerCloseTrigger />
        <p v-if="permDisabledIds.size > 0" class="drawer-tip">
          {{ t('tenant.edition.perm_disabled_tip') }}
        </p>
        <XPermissionTransfer
          :items="permItems"
          :value="permChecked"
          :loading="permLoading"
          :disabled="permLoading || permError || !canEditPermission"
          :source-title="t('tenant.edition.perm_available')"
          :target-title="t('tenant.edition.perm_granted')"
          :search-placeholder="t('tenant.edition.perm_grant_placeholder')"
          :other-group-label="t('tenant.edition.perm_group_other')"
          @update:value="onPermTransfer"
        >
          <template #suffix="{ item, side }">
            <!-- 停用的映射不在白名单里，与未授予同在左栏；移到右栏保存即重新启用 -->
            <XhTagRoot v-if="side === 'source' && permDisabledIds.has(item.basicId)" variant="subtle" size="sm" tone="warning">
              <XhTagLabel>{{ t('tenant.edition.perm_disabled') }}</XhTagLabel>
            </XhTagRoot>
          </template>
        </XPermissionTransfer>
        <!-- 按钮行排在抽屉内容区末尾，右对齐 -->
        <div class="xh-dialog-footer">
          <XhButton variant="subtle" @click="permDrawerVisible = false">
            {{ t('tenant.edition.cancel') }}
          </XhButton>
          <XhButton variant="subtle" tone="brand" :loading="permLoading" :disabled="!permDirty" @click="savePermChanges">
            <XhButtonIndicator />
            <XhButtonLabel>{{ t('tenant.edition.perm_save') }}</XhButtonLabel>
          </XhButton>
        </div>
      </XhDrawerContent>
    </XhDrawerRoot>
  </SchemaPage>
</template>

<style scoped>
.drawer-tip {
  margin: 0;
  color: var(--xh-fg-muted);
  font-size: var(--xh-text-caption-size);
}

/* 版本编码随机生成触发器：排在输入框盒内，与清除钮同段；焦点环交给浏览器默认样式 */
.edition-code-random {
  display: inline-flex;
  flex: none;
  align-items: center;
  justify-content: center;
  padding: 0;
  border: none;
  background: transparent;
  color: var(--xh-fg-muted);
  cursor: pointer;
}

.edition-code-random:hover {
  color: var(--xh-fg-default);
}
</style>
