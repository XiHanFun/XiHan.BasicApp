<script setup lang="ts">
import type {
  ApiId,
  FieldLevelSecurityCreateDto,
  FieldLevelSecurityListItemDto,
  FieldLevelSecurityUpdateDto,
  PageResult,
} from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import {
  NButton,
  NForm,
  NFormItem,
  NInput,
  NInputNumber,
  NModal,
  NSelect,
  NSpace,
  NSwitch,
  NTag,
  useMessage,
} from 'naive-ui'
import { computed, h, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  createPageRequest,
  departmentApi,
  EnableStatus,
  fieldLevelSecurityApi,
  FieldMaskStrategy,
  FieldSecurityTargetType,
  permissionApi,
  querySortsFromSchema,
  resourceApi,
  roleApi,
  userManagementApi,
} from '@/api'
import { SchemaPage } from '~/components'
import {
  FIELD_MASK_STRATEGY_OPTIONS,
  FIELD_SECURITY_TARGET_TYPE_OPTIONS,
  STATUS_OPTIONS,
} from '~/constants'
import { getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemFieldSecurityPage' })

const { t } = useI18n()

interface FlsFormModel extends FieldLevelSecurityCreateDto {
  basicId?: ApiId
}

interface NumericSelectOption {
  label: string
  value: ApiId
}

const message = useMessage()

const submitLoading = ref(false)
const modalVisible = ref(false)
const flsForm = ref<FlsFormModel>(createDefaultForm())

const targetOptions = ref<NumericSelectOption[]>([])
const targetLoading = ref(false)
const resourceOptions = ref<NumericSelectOption[]>([])
const resourceLoading = ref(false)

const maskStrategyOptions = FIELD_MASK_STRATEGY_OPTIONS
const targetTypeOptions = FIELD_SECURITY_TARGET_TYPE_OPTIONS

const modalTitle = computed(() => (flsForm.value.basicId ? t('identity.field_security.form_edit_title') : t('identity.field_security.form_create_title')))
const needMaskPattern = computed(() =>
  flsForm.value.maskStrategy === FieldMaskStrategy.PartialMask
  || flsForm.value.maskStrategy === FieldMaskStrategy.Custom,
)
const targetPlaceholder = computed(() => {
  switch (flsForm.value.targetType) {
    case FieldSecurityTargetType.User:
      return t('identity.field_security.ph_target_user')
    case FieldSecurityTargetType.Permission:
      return t('identity.field_security.ph_target_permission')
    case FieldSecurityTargetType.Department:
      return t('identity.field_security.ph_target_department')
    default:
      return t('identity.field_security.ph_target_role')
  }
})

const schemaPageRef = ref<{ reload: () => Promise<void> } | null>(null)

function reloadList() {
  void schemaPageRef.value?.reload()
}

// 切换目标类型时清空已选目标并重新加载候选
watch(
  () => flsForm.value.targetType,
  () => {
    flsForm.value.targetId = 0 as unknown as ApiId
    targetOptions.value = []
  },
)

function createDefaultForm(): FlsFormModel {
  return {
    description: null,
    fieldName: '',
    isEditable: true,
    isReadable: true,
    maskPattern: null,
    maskStrategy: FieldMaskStrategy.None,
    priority: 0,
    remark: null,
    resourceId: 0 as unknown as ApiId,
    status: EnableStatus.Enabled,
    targetId: 0 as unknown as ApiId,
    targetType: FieldSecurityTargetType.Role,
  }
}

function toStr(value: unknown) {
  return (value as string | undefined)?.trim() || undefined
}

function normalizeNullable(value?: string | null) {
  const normalized = value?.trim()
  return normalized || null
}

function mergeOptions(current: NumericSelectOption[], next: NumericSelectOption[]) {
  const map = new Map<ApiId, NumericSelectOption>()
  for (const option of current) {
    map.set(option.value, option)
  }
  for (const option of next) {
    map.set(option.value, option)
  }
  return [...map.values()]
}

function flattenDeptOptions(
  nodes: { basicId: ApiId, departmentName: string, children?: { basicId: ApiId, departmentName: string, children?: unknown }[] | null }[],
  depth = 0,
): NumericSelectOption[] {
  const out: NumericSelectOption[] = []
  for (const node of nodes) {
    out.push({ label: `${'　'.repeat(depth)}${node.departmentName}`, value: node.basicId })
    if (node.children?.length) {
      out.push(...flattenDeptOptions(node.children as never, depth + 1))
    }
  }
  return out
}

async function loadTargetOptions(keyword = '') {
  targetLoading.value = true
  try {
    const kw = normalizeNullable(keyword)
    let next: NumericSelectOption[] = []
    switch (flsForm.value.targetType) {
      case FieldSecurityTargetType.User: {
        const result = await userManagementApi.page({
          ...createPageRequest({ page: { pageIndex: 1, pageSize: 50 } }),
          keyword: kw ?? undefined,
        })
        next = result.items.map(u => ({
          label: `${u.realName || u.nickName || u.userName} (@${u.userName})`,
          value: u.basicId,
        }))
        break
      }
      case FieldSecurityTargetType.Permission: {
        const perms = await permissionApi.availableGlobal({ limit: 50, keyword: kw })
        next = perms.map(p => ({ label: `${p.permissionName} (${p.permissionCode})`, value: p.basicId }))
        break
      }
      case FieldSecurityTargetType.Department: {
        const tree = await departmentApi.tree({ limit: 500, keyword: kw ?? undefined })
        next = flattenDeptOptions(tree)
        break
      }
      default: {
        const roles = await roleApi.enabledList({ limit: 50, keyword: kw })
        next = roles.map(r => ({ label: `${r.roleName} (${r.roleCode})`, value: r.basicId }))
        break
      }
    }
    targetOptions.value = mergeOptions(targetOptions.value, next)
  }
  catch {
    message.error(t('identity.field_security.msg_load_target_failed'))
  }
  finally {
    targetLoading.value = false
  }
}

async function loadResourceOptions(keyword = '') {
  resourceLoading.value = true
  try {
    const items = await resourceApi.availableGlobal({ keyword: normalizeNullable(keyword), limit: 50 })
    resourceOptions.value = mergeOptions(
      resourceOptions.value,
      items.map(item => ({ label: `${item.resourceName} (${item.resourceCode})`, value: item.basicId })),
    )
  }
  catch {
    message.error(t('identity.field_security.msg_load_resource_failed'))
  }
  finally {
    resourceLoading.value = false
  }
}

function ensureRowOptions(row: FieldLevelSecurityListItemDto) {
  if (row.targetId && (row.targetName || row.targetCode)) {
    targetOptions.value = mergeOptions(targetOptions.value, [
      { label: row.targetName || row.targetCode || String(row.targetId), value: row.targetId },
    ])
  }
  if (row.resourceId && (row.resourceName || row.resourceCode)) {
    resourceOptions.value = mergeOptions(resourceOptions.value, [
      {
        label: row.resourceCode ? `${row.resourceName} (${row.resourceCode})` : row.resourceName || String(row.resourceId),
        value: row.resourceId,
      },
    ])
  }
}

function boolTag(value: boolean, onText: string, offText: string) {
  return h(
    NTag,
    { size: 'small', bordered: false, type: value ? 'success' : 'error', style: { fontSize: '11px' } },
    () => (value ? onText : offText),
  )
}

// ── 字段单一事实源 ──────────────────────────────────────────────
const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('identity.field_security.col_keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('identity.field_security.keyword_placeholder'), width: 240, order: 0 },
  {
    key: 'targetType',
    title: t('identity.field_security.col_target_type'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    dictionaryCode: 'FieldSecurityTargetType',
    options: targetTypeOptions,
    searchPlaceholder: t('identity.field_security.target_type_placeholder'),
    width: 110,
    order: 1,
    render: row => getOptionLabel(targetTypeOptions, (row as unknown as FieldLevelSecurityListItemDto).targetType),
  },
  {
    key: 'targetName',
    title: t('identity.field_security.col_target'),
    dataType: 'string',
    minWidth: 150,
    order: 2,
    render: (row) => {
      const r = row as unknown as FieldLevelSecurityListItemDto
      return r.targetName || r.targetCode || String(r.targetId)
    },
  },
  {
    key: 'resourceName',
    title: t('identity.field_security.col_resource'),
    dataType: 'string',
    minWidth: 150,
    order: 3,
    render: (row) => {
      const r = row as unknown as FieldLevelSecurityListItemDto
      return r.resourceName || r.resourceCode || String(r.resourceId)
    },
  },
  { key: 'fieldName', title: t('identity.field_security.col_field_name'), dataType: 'string', sortable: true, minWidth: 150, order: 4 },
  {
    key: 'isReadable',
    title: t('identity.field_security.col_readable'),
    dataType: 'boolean',
    sortable: true,
    width: 84,
    order: 5,
    render: row => boolTag((row as unknown as FieldLevelSecurityListItemDto).isReadable, t('identity.field_security.readable_on'), t('identity.field_security.readable_off')),
  },
  {
    key: 'isEditable',
    title: t('identity.field_security.col_editable'),
    dataType: 'boolean',
    sortable: true,
    width: 90,
    order: 6,
    render: row => boolTag((row as unknown as FieldLevelSecurityListItemDto).isEditable, t('identity.field_security.editable_on'), t('identity.field_security.editable_off')),
  },
  {
    key: 'maskStrategy',
    title: t('identity.field_security.col_mask_strategy'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    dictionaryCode: 'FieldMaskStrategy',
    options: maskStrategyOptions,
    searchPlaceholder: t('identity.field_security.mask_strategy_placeholder'),
    minWidth: 110,
    order: 7,
    render: row => getOptionLabel(maskStrategyOptions, (row as unknown as FieldLevelSecurityListItemDto).maskStrategy),
  },
  { key: 'priority', title: t('identity.field_security.col_priority'), dataType: 'number', sortable: true, width: 90, order: 8 },
  {
    key: 'status',
    title: t('identity.field_security.col_status'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    dictionaryCode: 'EnableStatus',
    options: STATUS_OPTIONS,
    searchPlaceholder: t('identity.field_security.status_placeholder'),
    width: 90,
    order: 9,
  },
  { key: 'createdTime', title: t('identity.field_security.col_create_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 10 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'system.field-security',
  exportPermission: 'saas:field-level-security:export',
  pageName: t('identity.field_security.page_name'),
  batchRemovable: true,
  removePermission: 'saas:field-level-security:delete',
  statusPermission: 'saas:field-level-security:status',
  rowKey: 'basicId',
  scrollX: 1500,
  fields: fields.value,
  resource: {
    page: (params) => {
      const { keyword } = params.filters
      return fieldLevelSecurityApi.page({
        ...createPageRequest({
          page: { pageIndex: params.page, pageSize: params.pageSize },
          // 排序 + 多选(targetType/maskStrategy/status) 等通用过滤统一走 conditions
          conditions: { sorts: querySortsFromSchema(params.sorts), filters: params.conditionFilters ?? [] },
        }),
        keyword: toStr(keyword),
        // targetType/maskStrategy/status 改为多选，经 conditions.filters In 下发（不再走 DTO 顶层单值字段）
      }) as unknown as Promise<PageResult<Record<string, unknown>>>
    },
    remove: id => fieldLevelSecurityApi.delete(id),
    updateStatus: (id, enabled) => fieldLevelSecurityApi.updateStatus({ basicId: id, status: enabled ? EnableStatus.Enabled : EnableStatus.Disabled, remark: enabled ? t('identity.field_security.batch_enable_remark') : t('identity.field_security.batch_disable_remark') }),
  },
  actions: [
    { key: 'create', title: t('identity.field_security.action_create'), scope: 'page', type: 'primary', icon: 'lucide:plus' },
    { key: 'edit', title: t('identity.field_security.action_edit'), scope: 'row' },
    { key: 'toggle', title: t('identity.field_security.action_toggle'), scope: 'row' },
    { key: 'delete', title: t('identity.field_security.action_delete'), scope: 'row' },
  ],
}))

function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as FieldLevelSecurityListItemDto | undefined
  switch (payload.key) {
    case 'create':
      handleAdd()
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
    case 'delete':
      if (row) {
        void handleDelete(row)
      }
      break
  }
}

function handleAdd() {
  flsForm.value = createDefaultForm()
  targetOptions.value = []
  resourceOptions.value = []
  modalVisible.value = true
  void loadTargetOptions()
  void loadResourceOptions()
}

async function handleEdit(row: FieldLevelSecurityListItemDto) {
  let maskPattern: string | null = null
  try {
    const detail = await fieldLevelSecurityApi.detail(row.basicId)
    maskPattern = detail?.maskPattern ?? null
  }
  catch {
    // 详情失败不阻断编辑，maskPattern 留空
  }
  flsForm.value = {
    basicId: row.basicId,
    description: row.description ?? null,
    fieldName: row.fieldName,
    isEditable: row.isEditable,
    isReadable: row.isReadable,
    maskPattern,
    maskStrategy: row.maskStrategy,
    priority: row.priority,
    remark: null,
    resourceId: row.resourceId,
    status: row.status,
    targetId: row.targetId,
    targetType: row.targetType,
  }
  ensureRowOptions(row)
  modalVisible.value = true
}

function validateForm() {
  const form = flsForm.value
  if (!form.targetId) {
    message.warning(t('identity.field_security.msg_target_required'))
    return false
  }
  if (!form.resourceId) {
    message.warning(t('identity.field_security.msg_resource_required'))
    return false
  }
  if (!form.fieldName.trim()) {
    message.warning(t('identity.field_security.msg_field_name_required'))
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
    if (flsForm.value.basicId) {
      const updateInput: FieldLevelSecurityUpdateDto = {
        basicId: flsForm.value.basicId,
        description: normalizeNullable(flsForm.value.description),
        fieldName: flsForm.value.fieldName.trim(),
        isEditable: flsForm.value.isEditable,
        isReadable: flsForm.value.isReadable,
        maskPattern: needMaskPattern.value ? normalizeNullable(flsForm.value.maskPattern) : null,
        maskStrategy: flsForm.value.maskStrategy,
        priority: flsForm.value.priority,
        remark: normalizeNullable(flsForm.value.remark),
        resourceId: flsForm.value.resourceId,
        targetId: flsForm.value.targetId,
        targetType: flsForm.value.targetType,
      }
      await fieldLevelSecurityApi.update(updateInput)
    }
    else {
      const createInput: FieldLevelSecurityCreateDto = {
        description: normalizeNullable(flsForm.value.description),
        fieldName: flsForm.value.fieldName.trim(),
        isEditable: flsForm.value.isEditable,
        isReadable: flsForm.value.isReadable,
        maskPattern: needMaskPattern.value ? normalizeNullable(flsForm.value.maskPattern) : null,
        maskStrategy: flsForm.value.maskStrategy,
        priority: flsForm.value.priority,
        remark: normalizeNullable(flsForm.value.remark),
        resourceId: flsForm.value.resourceId,
        status: flsForm.value.status,
        targetId: flsForm.value.targetId,
        targetType: flsForm.value.targetType,
      }
      await fieldLevelSecurityApi.create(createInput)
    }
    message.success(t('common.messages.save_success'))
    modalVisible.value = false
    reloadList()
  }
  catch {
    message.error(t('common.messages.save_failed'))
  }
  finally {
    submitLoading.value = false
  }
}

async function handleDelete(row: FieldLevelSecurityListItemDto) {
  await fieldLevelSecurityApi.delete(row.basicId)
  message.success(t('common.messages.delete_success'))
  reloadList()
}

async function handleToggleStatus(row: FieldLevelSecurityListItemDto) {
  await fieldLevelSecurityApi.updateStatus({
    basicId: row.basicId,
    remark: row.status === EnableStatus.Enabled ? t('identity.field_security.front_disable_remark') : t('identity.field_security.front_enable_remark'),
    status: row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled,
  })
  message.success(t('common.messages.status_updated'))
  reloadList()
}
</script>

<template>
  <SchemaPage ref="schemaPageRef" :schema="schema" @action="onAction">
    <NModal
      v-model:show="modalVisible"
      :auto-focus="false"
      :bordered="false"
      preset="card"
      :title="modalTitle"
      style="width: 720px; max-width: 92vw"
    >
      <NForm :model="flsForm" class="xh-edit-form-grid" label-placement="top">
        <NFormItem :label="t('identity.field_security.label_target_type')" path="targetType">
          <NSelect
            v-model:value="flsForm.targetType"
            :disabled="Boolean(flsForm.basicId)"
            :options="targetTypeOptions"
          />
        </NFormItem>
        <NFormItem :label="t('identity.field_security.label_target')" path="targetId">
          <NSelect
            v-model:value="flsForm.targetId"
            clearable
            filterable
            :loading="targetLoading"
            :options="targetOptions"
            :placeholder="targetPlaceholder"
            remote
            @focus="loadTargetOptions()"
            @search="(kw: string) => loadTargetOptions(kw)"
          />
        </NFormItem>
        <NFormItem :label="t('identity.field_security.label_resource')" path="resourceId">
          <NSelect
            v-model:value="flsForm.resourceId"
            clearable
            filterable
            :loading="resourceLoading"
            :options="resourceOptions"
            :placeholder="t('identity.field_security.ph_resource')"
            remote
            @focus="loadResourceOptions()"
            @search="(kw: string) => loadResourceOptions(kw)"
          />
        </NFormItem>
        <NFormItem :label="t('identity.field_security.label_field_name')" path="fieldName">
          <NInput v-model:value="flsForm.fieldName" clearable :placeholder="t('identity.field_security.ph_field_name')" />
        </NFormItem>
        <NFormItem :label="t('identity.field_security.label_readable')" path="isReadable">
          <NSwitch v-model:value="flsForm.isReadable" />
        </NFormItem>
        <NFormItem :label="t('identity.field_security.label_editable')" path="isEditable">
          <NSwitch v-model:value="flsForm.isEditable" />
        </NFormItem>
        <NFormItem :label="t('identity.field_security.label_mask_strategy')" path="maskStrategy">
          <NSelect v-model:value="flsForm.maskStrategy" :options="maskStrategyOptions" />
        </NFormItem>
        <NFormItem v-if="needMaskPattern" :label="t('identity.field_security.label_mask_pattern')" path="maskPattern">
          <NInput
            v-model:value="flsForm.maskPattern"
            clearable
            :placeholder="t('identity.field_security.ph_mask_pattern')"
          />
        </NFormItem>
        <NFormItem :label="t('identity.field_security.label_priority')" path="priority">
          <NInputNumber v-model:value="flsForm.priority" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem v-if="!flsForm.basicId" :label="t('identity.field_security.label_status')" path="status">
          <NSelect v-model:value="flsForm.status" :options="STATUS_OPTIONS" />
        </NFormItem>
        <NFormItem :label="t('identity.field_security.label_description')" path="description">
          <NInput
            v-model:value="flsForm.description"
            clearable
            :placeholder="t('identity.field_security.ph_description')"
            :rows="2"
            type="textarea"
          />
        </NFormItem>
        <NFormItem :label="t('identity.field_security.label_remark')" path="remark">
          <NInput v-model:value="flsForm.remark" clearable :placeholder="t('identity.field_security.ph_remark')" />
        </NFormItem>
      </NForm>

      <template #footer>
        <NSpace justify="end">
          <NButton @click="modalVisible = false">
            {{ t('common.actions.cancel') }}
          </NButton>
          <NButton :loading="submitLoading" type="primary" @click="handleSubmit">
            {{ t('common.actions.save') }}
          </NButton>
        </NSpace>
      </template>
    </NModal>
  </SchemaPage>
</template>
