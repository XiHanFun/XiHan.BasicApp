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
import {
  createPageRequest,
  departmentApi,
  EnableStatus,
  fieldLevelSecurityApi,
  FieldMaskStrategy,
  FieldSecurityTargetType,
  permissionApi,
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

const modalTitle = computed(() => (flsForm.value.basicId ? '编辑字段规则' : '新增字段规则'))
const needMaskPattern = computed(() =>
  flsForm.value.maskStrategy === FieldMaskStrategy.PartialMask
  || flsForm.value.maskStrategy === FieldMaskStrategy.Custom,
)
const targetPlaceholder = computed(() => {
  switch (flsForm.value.targetType) {
    case FieldSecurityTargetType.User:
      return '搜索并选择用户'
    case FieldSecurityTargetType.Permission:
      return '搜索并选择权限'
    case FieldSecurityTargetType.Department:
      return '搜索并选择部门'
    default:
      return '搜索并选择角色'
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
    message.error('加载目标候选失败')
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
    message.error('加载资源候选失败')
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
const fields: ListFieldSchema[] = [
  { key: 'keyword', title: '关键词', dataType: 'string', visible: false, searchable: true, searchPlaceholder: '搜索字段名 / 目标 / 资源', width: 240, order: 0 },
  {
    key: 'targetType',
    title: '目标类型',
    dataType: 'enum',
    searchable: true,
    options: targetTypeOptions,
    searchPlaceholder: '全部目标类型',
    width: 110,
    order: 1,
    render: row => getOptionLabel(targetTypeOptions, (row as unknown as FieldLevelSecurityListItemDto).targetType),
  },
  {
    key: 'targetName',
    title: '目标',
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
    title: '资源',
    dataType: 'string',
    minWidth: 150,
    order: 3,
    render: (row) => {
      const r = row as unknown as FieldLevelSecurityListItemDto
      return r.resourceName || r.resourceCode || String(r.resourceId)
    },
  },
  { key: 'fieldName', title: '字段名', dataType: 'string', minWidth: 150, order: 4 },
  {
    key: 'isReadable',
    title: '可读',
    dataType: 'boolean',
    width: 84,
    order: 5,
    render: row => boolTag((row as unknown as FieldLevelSecurityListItemDto).isReadable, '可读', '不可读'),
  },
  {
    key: 'isEditable',
    title: '可编辑',
    dataType: 'boolean',
    width: 90,
    order: 6,
    render: row => boolTag((row as unknown as FieldLevelSecurityListItemDto).isEditable, '可编辑', '只读'),
  },
  {
    key: 'maskStrategy',
    title: '脱敏策略',
    dataType: 'enum',
    searchable: true,
    options: maskStrategyOptions,
    searchPlaceholder: '全部脱敏策略',
    minWidth: 110,
    order: 7,
    render: row => getOptionLabel(maskStrategyOptions, (row as unknown as FieldLevelSecurityListItemDto).maskStrategy),
  },
  { key: 'priority', title: '优先级', dataType: 'number', sortable: true, width: 90, order: 8 },
  {
    key: 'status',
    title: '状态',
    dataType: 'enum',
    searchable: true,
    options: STATUS_OPTIONS,
    searchPlaceholder: '全部状态',
    width: 90,
    order: 9,
  },
  { key: 'createdTime', title: '创建时间', dataType: 'datetime', sortable: true, minWidth: 170, order: 10 },
]

const schema: PageSchema = {
  pageCode: 'system.field-security',
  exportPermission: 'saas:field-level-security:export',
  pageName: '字段级权限',
  batchRemovable: true,
  removePermission: 'saas:field-level-security:delete',
  rowKey: 'basicId',
  scrollX: 1500,
  fields,
  resource: {
    page: (params) => {
      const { keyword, targetType, maskStrategy, status } = params.filters
      return fieldLevelSecurityApi.page({
        ...createPageRequest({ page: { pageIndex: params.page, pageSize: params.pageSize } }),
        keyword: toStr(keyword),
        targetType: targetType as FieldSecurityTargetType | undefined,
        maskStrategy: maskStrategy as FieldMaskStrategy | undefined,
        status: status as EnableStatus | undefined,
      }) as unknown as Promise<PageResult<Record<string, unknown>>>
    },
    remove: id => fieldLevelSecurityApi.delete(id),
  },
  actions: [
    { key: 'create', title: '新增字段规则', scope: 'page', type: 'primary', icon: 'lucide:plus' },
    { key: 'edit', title: '编辑', scope: 'row' },
    { key: 'toggle', title: '启用/停用', scope: 'row' },
    { key: 'delete', title: '删除', scope: 'row' },
  ],
}

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
    message.warning('请选择授权目标')
    return false
  }
  if (!form.resourceId) {
    message.warning('请选择资源')
    return false
  }
  if (!form.fieldName.trim()) {
    message.warning('请输入字段名')
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
    message.success('保存成功')
    modalVisible.value = false
    reloadList()
  }
  catch {
    message.error('保存失败')
  }
  finally {
    submitLoading.value = false
  }
}

async function handleDelete(row: FieldLevelSecurityListItemDto) {
  await fieldLevelSecurityApi.delete(row.basicId)
  message.success('删除成功')
  reloadList()
}

async function handleToggleStatus(row: FieldLevelSecurityListItemDto) {
  await fieldLevelSecurityApi.updateStatus({
    basicId: row.basicId,
    remark: row.status === EnableStatus.Enabled ? '前端停用字段规则' : '前端启用字段规则',
    status: row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled,
  })
  message.success('状态已更新')
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
        <NFormItem label="目标类型" path="targetType">
          <NSelect
            v-model:value="flsForm.targetType"
            :disabled="Boolean(flsForm.basicId)"
            :options="targetTypeOptions"
          />
        </NFormItem>
        <NFormItem label="授权目标" path="targetId">
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
        <NFormItem label="资源" path="resourceId">
          <NSelect
            v-model:value="flsForm.resourceId"
            clearable
            filterable
            :loading="resourceLoading"
            :options="resourceOptions"
            placeholder="搜索并选择资源"
            remote
            @focus="loadResourceOptions()"
            @search="(kw: string) => loadResourceOptions(kw)"
          />
        </NFormItem>
        <NFormItem label="字段名" path="fieldName">
          <NInput v-model:value="flsForm.fieldName" clearable placeholder="实体/DTO 属性名，如 Email" />
        </NFormItem>
        <NFormItem label="可读" path="isReadable">
          <NSwitch v-model:value="flsForm.isReadable" />
        </NFormItem>
        <NFormItem label="可编辑" path="isEditable">
          <NSwitch v-model:value="flsForm.isEditable" />
        </NFormItem>
        <NFormItem label="脱敏策略" path="maskStrategy">
          <NSelect v-model:value="flsForm.maskStrategy" :options="maskStrategyOptions" />
        </NFormItem>
        <NFormItem v-if="needMaskPattern" label="脱敏规则" path="maskPattern">
          <NInput
            v-model:value="flsForm.maskPattern"
            clearable
            placeholder="如 keep:3,4（保留前3后4）"
          />
        </NFormItem>
        <NFormItem label="优先级" path="priority">
          <NInputNumber v-model:value="flsForm.priority" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem v-if="!flsForm.basicId" label="状态" path="status">
          <NSelect v-model:value="flsForm.status" :options="STATUS_OPTIONS" />
        </NFormItem>
        <NFormItem label="描述" path="description">
          <NInput
            v-model:value="flsForm.description"
            clearable
            placeholder="请输入规则描述"
            :rows="2"
            type="textarea"
          />
        </NFormItem>
        <NFormItem label="备注" path="remark">
          <NInput v-model:value="flsForm.remark" clearable placeholder="请输入备注" />
        </NFormItem>
      </NForm>

      <template #footer>
        <NSpace justify="end">
          <NButton @click="modalVisible = false">
            取消
          </NButton>
          <NButton :loading="submitLoading" type="primary" @click="handleSubmit">
            保存
          </NButton>
        </NSpace>
      </template>
    </NModal>
  </SchemaPage>
</template>
