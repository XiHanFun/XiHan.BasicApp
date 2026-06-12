<script setup lang="ts">
import type {
  ApiId,
  ConstraintRuleDetailDto,
  ConstraintRuleItemDto,
  ConstraintRuleItemInputDto,
  ConstraintRuleListItemDto,
  PageResult,
} from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import {
  NButton,
  NDatePicker,
  NDescriptions,
  NDescriptionsItem,
  NDrawer,
  NDrawerContent,
  NEmpty,
  NForm,
  NFormItem,
  NInput,
  NInputNumber,
  NModal,
  NScrollbar,
  NSelect,
  NSpace,
  NSpin,
  NTag,
  useDialog,
  useMessage,
} from 'naive-ui'
import { computed, h, ref } from 'vue'
import {
  constraintRuleApi,
  ConstraintTargetType,
  ConstraintType,
  createPageRequest,
  EnableStatus,
  permissionApi,
  roleApi,
  tenantMemberApi,
  ViolationAction,
} from '@/api'
import { SchemaPage } from '~/components'
import { CONSTRAINT_TYPE_OPTIONS, STATUS_OPTIONS, VIOLATION_ACTION_OPTIONS } from '~/constants'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'ApprovalConstraintPage' })

interface TargetOption {
  label: string
  value: ApiId
}

interface ConstraintItemFormModel {
  constraintGroup: number | null
  remark: string | null
  targetId: ApiId | null
  targetType: ConstraintTargetType
}

interface ConstraintRuleFormModel {
  basicId?: ApiId
  constraintType: ConstraintType
  description: string | null
  effectiveTime: number | null
  expirationTime: number | null
  items: ConstraintItemFormModel[]
  parameters: string | null
  priority: number | null
  remark: string | null
  ruleCode: string
  ruleName: string
  status: EnableStatus
  targetType: ConstraintTargetType
  violationAction: ViolationAction
}

const message = useMessage()
const dialog = useDialog()

const statusOptions = STATUS_OPTIONS
const constraintTypeOptions = CONSTRAINT_TYPE_OPTIONS
const violationActionOptions = VIOLATION_ACTION_OPTIONS

const targetTypeOptions = [
  { label: '角色', value: ConstraintTargetType.Role },
  { label: '权限', value: ConstraintTargetType.Permission },
  { label: '用户', value: ConstraintTargetType.User },
]

const VIOLATION_TAG_TYPE: Record<ViolationAction, 'default' | 'error' | 'info' | 'warning'> = {
  [ViolationAction.Deny]: 'error',
  [ViolationAction.Warning]: 'warning',
  [ViolationAction.Log]: 'default',
  [ViolationAction.RequireApproval]: 'info',
}

/** 不同约束类型下规则项的填写提示 */
const ITEM_HINTS: Partial<Record<ConstraintType, string>> = {
  [ConstraintType.SSD]: '同组目标互斥，至少需要两个目标项',
  [ConstraintType.DSD]: '同组目标会话内互斥，至少需要两个目标项',
  [ConstraintType.MutualExclusion]: '同组目标互斥，至少需要两个目标项',
  [ConstraintType.Cardinality]: '基数上限等请在「约束参数」中以 JSON 配置',
  [ConstraintType.Prerequisite]: '分组 0 = 必备项，分组 1 = 目标项，两组均需配置',
}

const schemaPageRef = ref<{ reload: () => Promise<void> } | null>(null)

function reload() {
  void schemaPageRef.value?.reload()
}

// ── 通用辅助 ────────────────────────────────────────────────────
function toStr(value: unknown): string | undefined {
  return (value as string | undefined)?.trim() || undefined
}

function normalizeNullable(value?: string | null) {
  const normalized = value?.trim()
  return normalized || null
}

function formatNullable(value: unknown) {
  return value === null || value === undefined || value === '' ? '-' : String(value)
}

function formatNullableDate(value?: string | null) {
  return value ? formatDate(value) : '-'
}

// ── 字段单一事实源：列 + 搜索 ───────────────────────────────────
const fields: ListFieldSchema[] = [
  { key: 'keyword', title: '关键词', dataType: 'string', visible: false, searchable: true, searchPlaceholder: '搜索规则编码/名称/描述/备注', width: 240, order: 0 },
  { key: 'ruleCode', title: '规则编码', dataType: 'string', minWidth: 160, order: 1 },
  { key: 'ruleName', title: '规则名称', dataType: 'string', minWidth: 160, order: 2 },
  {
    key: 'constraintType',
    title: '约束类型',
    dataType: 'enum',
    searchable: true,
    options: constraintTypeOptions,
    searchPlaceholder: '约束类型',
    minWidth: 130,
    order: 3,
    render: row => h(
      NTag,
      { size: 'small', round: true, bordered: false, type: 'info' },
      () => getOptionLabel(constraintTypeOptions, (row as unknown as ConstraintRuleListItemDto).constraintType),
    ),
  },
  {
    key: 'targetType',
    title: '目标类型',
    dataType: 'enum',
    options: targetTypeOptions,
    width: 96,
    order: 4,
    render: row => h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, getOptionLabel(targetTypeOptions, (row as unknown as ConstraintRuleListItemDto).targetType)),
  },
  {
    key: 'violationAction',
    title: '违规处理',
    dataType: 'enum',
    options: violationActionOptions,
    width: 110,
    order: 5,
    render: (row) => {
      const r = row as unknown as ConstraintRuleListItemDto
      return h(
        NTag,
        { size: 'small', round: true, bordered: false, type: VIOLATION_TAG_TYPE[r.violationAction] ?? 'default' },
        () => getOptionLabel(violationActionOptions, r.violationAction),
      )
    },
  },
  { key: 'itemCount', title: '规则项数', dataType: 'number', width: 92, order: 6 },
  { key: 'priority', title: '优先级', dataType: 'number', sortable: true, width: 88, order: 7 },
  {
    key: 'status',
    title: '状态',
    dataType: 'enum',
    searchable: true,
    options: statusOptions,
    searchPlaceholder: '状态',
    width: 82,
    order: 8,
    render: row => h(
      NTag,
      { size: 'small', round: true, bordered: false, type: (row as unknown as ConstraintRuleListItemDto).status === EnableStatus.Enabled ? 'success' : 'error' },
      () => (row as unknown as ConstraintRuleListItemDto).status === EnableStatus.Enabled ? '启用' : '禁用',
    ),
  },
  {
    key: 'isActive',
    title: '当前生效',
    dataType: 'boolean',
    width: 92,
    order: 9,
    render: row => h(
      NTag,
      { size: 'small', round: true, bordered: false, type: (row as unknown as ConstraintRuleListItemDto).isActive ? 'success' : 'default' },
      () => (row as unknown as ConstraintRuleListItemDto).isActive ? '生效中' : '未生效',
    ),
  },
  { key: 'description', title: '描述', dataType: 'string', minWidth: 200, order: 10 },
  {
    key: 'createdTime',
    title: '创建时间',
    dataType: 'datetime',
    sortable: true,
    minWidth: 170,
    order: 11,
    render: row => h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, formatDate((row as unknown as ConstraintRuleListItemDto).createdTime)),
  },
]

// ── 资源适配器：归一化查询参数 → 后端 API ──────────────────────
const schema: PageSchema = {
  pageCode: 'approval.constraint',
  pageName: '约束规则',
  rowKey: 'basicId',
  scrollX: 1600,
  fields,
  resource: {
    page: (params) => {
      const f = params.filters
      return constraintRuleApi.page({
        ...createPageRequest({ page: { pageIndex: params.page, pageSize: params.pageSize } }),
        keyword: toStr(f.keyword) ?? null,
        constraintType: (f.constraintType as ConstraintType | undefined) || undefined,
        status: (f.status as EnableStatus | undefined) || undefined,
      }) as unknown as Promise<PageResult<Record<string, unknown>>>
    },
  },
  actions: [
    { key: 'create', title: '新增规则', scope: 'page', type: 'primary', icon: 'lucide:plus', permission: 'saas:constraint-rule:create' },
    { key: 'view', title: '查看详情', scope: 'row', permission: 'saas:constraint-rule:read' },
    { key: 'edit', title: '编辑', scope: 'row', permission: 'saas:constraint-rule:update' },
    { key: 'toggle', title: '启用/停用', scope: 'row', permission: 'saas:constraint-rule:status' },
    { key: 'delete', title: '删除', scope: 'row', type: 'error', permission: 'saas:constraint-rule:delete' },
  ],
}

// ── 行/页面操作分发 ─────────────────────────────────────────────
function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as ConstraintRuleListItemDto | undefined
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
        confirmToggleStatus(row)
      }
      break
    case 'delete':
      if (row) {
        confirmDelete(row)
      }
      break
  }
}

// ── 目标候选（角色 / 权限 / 租户成员，远程搜索） ────────────────
const targetOptions = ref<Record<ConstraintTargetType, TargetOption[]>>({
  [ConstraintTargetType.Role]: [],
  [ConstraintTargetType.Permission]: [],
  [ConstraintTargetType.User]: [],
})
const targetLoading = ref<Record<ConstraintTargetType, boolean>>({
  [ConstraintTargetType.Role]: false,
  [ConstraintTargetType.Permission]: false,
  [ConstraintTargetType.User]: false,
})

function mergeOptions(current: TargetOption[], next: TargetOption[]) {
  const map = new Map<ApiId, TargetOption>()
  for (const option of current) {
    map.set(option.value, option)
  }
  for (const option of next) {
    map.set(option.value, option)
  }
  return [...map.values()]
}

async function loadTargetOptions(type: ConstraintTargetType, keyword = '') {
  targetLoading.value[type] = true
  try {
    let next: TargetOption[] = []
    if (type === ConstraintTargetType.Role) {
      const roles = await roleApi.enabledList({ limit: 50, keyword: normalizeNullable(keyword) })
      next = roles.map(role => ({ label: `${role.roleName} (${role.roleCode})`, value: role.basicId }))
    }
    else if (type === ConstraintTargetType.Permission) {
      const result = await permissionApi.page({
        ...createPageRequest({ page: { pageIndex: 1, pageSize: 50 } }),
        keyword: normalizeNullable(keyword),
        status: EnableStatus.Enabled,
      })
      next = result.items.map(permission => ({ label: `${permission.permissionName} (${permission.permissionCode})`, value: permission.basicId }))
    }
    else {
      const result = await tenantMemberApi.page({
        ...createPageRequest({ page: { pageIndex: 1, pageSize: 50 } }),
        keyword: normalizeNullable(keyword),
      })
      next = result.items.map(member => ({
        label: member.displayName ? `${member.displayName} (#${member.userId})` : `成员 #${member.userId}`,
        value: member.userId,
      }))
    }
    targetOptions.value[type] = mergeOptions(targetOptions.value[type], next)
  }
  catch (e) {
    message.error((e as Error).message || '加载目标候选失败')
  }
  finally {
    targetLoading.value[type] = false
  }
}

/** 编辑/详情回填时，把已有目标的名称种子化进候选，确保选中项有可读标签 */
function seedTargetOption(item: ConstraintRuleItemDto) {
  const label = item.targetName
    ? (item.targetCode ? `${item.targetName} (${item.targetCode})` : item.targetName)
    : `#${item.targetId}`
  targetOptions.value[item.targetType] = mergeOptions(targetOptions.value[item.targetType], [{ label, value: item.targetId }])
}

// ── 详情抽屉 ────────────────────────────────────────────────────
const detailVisible = ref(false)
const detailLoading = ref(false)
const currentDetail = ref<ConstraintRuleDetailDto | null>(null)

const parametersPretty = computed(() => {
  const parameters = currentDetail.value?.parameters
  if (!parameters) {
    return null
  }
  try {
    return JSON.stringify(JSON.parse(parameters), null, 2)
  }
  catch {
    return parameters
  }
})

function formatItemGroup(item: ConstraintRuleItemDto) {
  if (currentDetail.value?.constraintType !== ConstraintType.Prerequisite) {
    return String(item.constraintGroup)
  }
  if (item.constraintGroup === 0) {
    return '0（必备项）'
  }
  if (item.constraintGroup === 1) {
    return '1（目标项）'
  }
  return String(item.constraintGroup)
}

async function handleView(row: ConstraintRuleListItemDto) {
  detailVisible.value = true
  detailLoading.value = true
  currentDetail.value = null
  try {
    currentDetail.value = await constraintRuleApi.detail(row.basicId)
    if (!currentDetail.value) {
      message.warning('未查询到约束规则详情')
    }
  }
  catch (e) {
    message.error((e as Error).message || '加载约束规则详情失败')
  }
  finally {
    detailLoading.value = false
  }
}

// ── 新增 / 编辑表单 ─────────────────────────────────────────────
const modalVisible = ref(false)
const submitLoading = ref(false)
const editingStatus = ref<EnableStatus | null>(null)
const ruleForm = ref<ConstraintRuleFormModel>(createDefaultForm())

const modalTitle = computed(() => (ruleForm.value.basicId ? '编辑约束规则' : '新增约束规则'))
const isPrerequisite = computed(() => ruleForm.value.constraintType === ConstraintType.Prerequisite)
const itemHint = computed(() => ITEM_HINTS[ruleForm.value.constraintType] ?? '同一目标不可重复添加')

function createItem(targetType: ConstraintTargetType): ConstraintItemFormModel {
  return { constraintGroup: 0, remark: null, targetId: null, targetType }
}

function createDefaultForm(): ConstraintRuleFormModel {
  return {
    constraintType: ConstraintType.SSD,
    description: null,
    effectiveTime: null,
    expirationTime: null,
    items: [createItem(ConstraintTargetType.Role)],
    parameters: null,
    priority: 0,
    remark: null,
    ruleCode: '',
    ruleName: '',
    status: EnableStatus.Enabled,
    targetType: ConstraintTargetType.Role,
    violationAction: ViolationAction.Deny,
  }
}

function handleAdd() {
  editingStatus.value = null
  ruleForm.value = createDefaultForm()
  modalVisible.value = true
  void loadTargetOptions(ruleForm.value.targetType)
}

async function handleEdit(row: ConstraintRuleListItemDto) {
  try {
    const detail = await constraintRuleApi.detail(row.basicId)
    if (!detail) {
      message.warning('未查询到约束规则详情')
      return
    }
    for (const item of detail.items) {
      seedTargetOption(item)
    }
    editingStatus.value = detail.status
    ruleForm.value = {
      basicId: detail.basicId,
      constraintType: detail.constraintType,
      description: detail.description ?? null,
      effectiveTime: detail.effectiveTime ? new Date(detail.effectiveTime).getTime() : null,
      expirationTime: detail.expirationTime ? new Date(detail.expirationTime).getTime() : null,
      items: detail.items.map(item => ({
        constraintGroup: item.constraintGroup,
        remark: item.remark ?? null,
        targetId: item.targetId,
        targetType: item.targetType,
      })),
      parameters: detail.parameters ?? null,
      priority: detail.priority,
      remark: detail.remark ?? null,
      ruleCode: detail.ruleCode,
      ruleName: detail.ruleName,
      status: detail.status,
      targetType: detail.targetType,
      violationAction: detail.violationAction,
    }
    modalVisible.value = true
    void loadTargetOptions(detail.targetType)
  }
  catch (e) {
    message.error((e as Error).message || '加载约束规则详情失败')
  }
}

// ── 规则项动态行编辑 ────────────────────────────────────────────
function addItem() {
  ruleForm.value.items.push(createItem(ruleForm.value.targetType))
}

function removeItem(index: number) {
  ruleForm.value.items.splice(index, 1)
}

/** 非先决条件约束：规则项目标类型必须与规则目标类型一致（后端强校验，前端直接同步） */
function syncItemTargetTypes() {
  if (isPrerequisite.value) {
    return
  }
  for (const item of ruleForm.value.items) {
    if (item.targetType !== ruleForm.value.targetType) {
      item.targetType = ruleForm.value.targetType
      item.targetId = null
    }
  }
}

function onRuleTargetTypeChange() {
  syncItemTargetTypes()
  void loadTargetOptions(ruleForm.value.targetType)
}

function onConstraintTypeChange() {
  syncItemTargetTypes()
}

function onItemTargetTypeChange(item: ConstraintItemFormModel) {
  item.targetId = null
  void loadTargetOptions(item.targetType)
}

// ── 提交 ────────────────────────────────────────────────────────
function validateForm(): boolean {
  const form = ruleForm.value
  if (!form.basicId && !form.ruleCode.trim()) {
    message.warning('请输入规则编码')
    return false
  }
  if (!form.ruleName.trim()) {
    message.warning('请输入规则名称')
    return false
  }
  if (form.parameters?.trim()) {
    try {
      JSON.parse(form.parameters)
    }
    catch {
      message.warning('约束参数必须是合法 JSON')
      return false
    }
  }
  if (form.effectiveTime != null && form.expirationTime != null && form.effectiveTime >= form.expirationTime) {
    message.warning('生效时间必须早于失效时间')
    return false
  }
  if (form.items.length === 0) {
    message.warning('约束规则至少需要一个规则项')
    return false
  }
  for (const [index, item] of form.items.entries()) {
    if (!item.targetId) {
      message.warning(`第 ${index + 1} 个规则项未选择目标`)
      return false
    }
    if (item.constraintGroup == null || item.constraintGroup < 0) {
      message.warning(`第 ${index + 1} 个规则项的约束分组不能小于 0`)
      return false
    }
  }
  const targetKeys = new Set(form.items.map(item => `${item.targetType}:${item.targetId}`))
  if (targetKeys.size !== form.items.length) {
    message.warning('规则项目标不能重复')
    return false
  }
  const needsPair = [ConstraintType.SSD, ConstraintType.DSD, ConstraintType.MutualExclusion]
  if (needsPair.includes(form.constraintType) && form.items.length < 2) {
    message.warning('职责分离或互斥约束至少需要两个目标项')
    return false
  }
  if (form.constraintType === ConstraintType.Prerequisite) {
    const groups = new Set(form.items.map(item => item.constraintGroup))
    if (!groups.has(0) || !groups.has(1)) {
      message.warning('先决条件约束必须同时包含分组 0（必备项）和分组 1（目标项）')
      return false
    }
  }
  return true
}

async function handleSubmit() {
  if (!validateForm()) {
    return
  }
  const form = ruleForm.value
  submitLoading.value = true
  try {
    const items: ConstraintRuleItemInputDto[] = form.items.map(item => ({
      constraintGroup: item.constraintGroup ?? 0,
      remark: normalizeNullable(item.remark),
      targetId: item.targetId!,
      targetType: item.targetType,
    }))
    const shared = {
      constraintType: form.constraintType,
      description: normalizeNullable(form.description),
      effectiveTime: form.effectiveTime != null ? new Date(form.effectiveTime).toISOString() : null,
      expirationTime: form.expirationTime != null ? new Date(form.expirationTime).toISOString() : null,
      items,
      parameters: normalizeNullable(form.parameters),
      priority: form.priority ?? 0,
      remark: normalizeNullable(form.remark),
      ruleName: form.ruleName.trim(),
      targetType: form.targetType,
      violationAction: form.violationAction,
    }
    if (form.basicId) {
      await constraintRuleApi.update({ basicId: form.basicId, ...shared })
      if (editingStatus.value !== form.status) {
        await constraintRuleApi.updateStatus({
          basicId: form.basicId,
          remark: shared.remark,
          status: form.status,
        })
      }
    }
    else {
      await constraintRuleApi.create({
        ...shared,
        ruleCode: form.ruleCode.trim(),
        status: form.status,
      })
    }
    message.success('保存成功')
    modalVisible.value = false
    reload()
  }
  catch (e) {
    message.error((e as Error).message || '保存失败')
  }
  finally {
    submitLoading.value = false
  }
}

// ── 启停 / 删除 ─────────────────────────────────────────────────
function confirmToggleStatus(row: ConstraintRuleListItemDto) {
  const enable = row.status !== EnableStatus.Enabled
  dialog.warning({
    title: enable ? '启用规则' : '停用规则',
    content: `确定${enable ? '启用' : '停用'}约束规则「${row.ruleName}」吗？${enable ? '启用后规则立即参与约束校验。' : '停用后规则不再参与约束校验。'}`,
    positiveText: enable ? '启用' : '停用',
    negativeText: '取消',
    onPositiveClick: async () => {
      try {
        await constraintRuleApi.updateStatus({
          basicId: row.basicId,
          remark: enable ? '前端启用约束规则' : '前端停用约束规则',
          status: enable ? EnableStatus.Enabled : EnableStatus.Disabled,
        })
        message.success('状态已更新')
        reload()
      }
      catch (e) {
        message.error((e as Error).message || '状态更新失败')
      }
    },
  })
}

function confirmDelete(row: ConstraintRuleListItemDto) {
  dialog.error({
    title: '删除规则',
    content: `确定删除约束规则「${row.ruleName}」吗？其下 ${row.itemCount} 个规则项将一并删除，且不可恢复。`,
    positiveText: '删除',
    negativeText: '取消',
    onPositiveClick: async () => {
      try {
        await constraintRuleApi.delete(row.basicId)
        message.success('删除成功')
        reload()
      }
      catch (e) {
        message.error((e as Error).message || '删除失败')
      }
    },
  })
}
</script>

<template>
  <SchemaPage
    ref="schemaPageRef"
    :schema="schema"
    @action="onAction"
  >
    <!-- 详情抽屉：规则 + 明细项列表 -->
    <NDrawer v-model:show="detailVisible" :width="880">
      <NDrawerContent closable title="约束规则详情">
        <NSpin :show="detailLoading">
          <NEmpty v-if="!detailLoading && !currentDetail" class="rule-detail-empty" description="暂无约束规则详情" />
          <NScrollbar v-else-if="currentDetail" style="max-height: calc(100vh - 120px)">
            <NDescriptions :column="2" bordered size="small">
              <NDescriptionsItem label="规则编码">
                {{ currentDetail.ruleCode }}
              </NDescriptionsItem>
              <NDescriptionsItem label="规则名称">
                {{ currentDetail.ruleName }}
              </NDescriptionsItem>
              <NDescriptionsItem label="约束类型">
                {{ getOptionLabel(constraintTypeOptions, currentDetail.constraintType) }}
              </NDescriptionsItem>
              <NDescriptionsItem label="目标类型">
                {{ getOptionLabel(targetTypeOptions, currentDetail.targetType) }}
              </NDescriptionsItem>
              <NDescriptionsItem label="违规处理">
                {{ getOptionLabel(violationActionOptions, currentDetail.violationAction) }}
              </NDescriptionsItem>
              <NDescriptionsItem label="优先级">
                {{ currentDetail.priority }}
              </NDescriptionsItem>
              <NDescriptionsItem label="状态">
                {{ getOptionLabel(statusOptions, currentDetail.status) }}
              </NDescriptionsItem>
              <NDescriptionsItem label="当前生效">
                {{ currentDetail.isActive ? '生效中' : '未生效' }}
              </NDescriptionsItem>
              <NDescriptionsItem label="全局规则">
                {{ currentDetail.isGlobal ? '是' : '否' }}
              </NDescriptionsItem>
              <NDescriptionsItem label="有效期">
                {{ formatNullableDate(currentDetail.effectiveTime) }} 至 {{ formatNullableDate(currentDetail.expirationTime) }}
              </NDescriptionsItem>
              <NDescriptionsItem label="描述">
                {{ formatNullable(currentDetail.description) }}
              </NDescriptionsItem>
              <NDescriptionsItem label="备注">
                {{ formatNullable(currentDetail.remark) }}
              </NDescriptionsItem>
              <NDescriptionsItem label="创建">
                {{ formatNullable(currentDetail.createdBy) }} · {{ formatNullableDate(currentDetail.createdTime) }}
              </NDescriptionsItem>
              <NDescriptionsItem label="最后修改">
                {{ formatNullable(currentDetail.modifiedBy) }} · {{ formatNullableDate(currentDetail.modifiedTime) }}
              </NDescriptionsItem>
            </NDescriptions>

            <template v-if="parametersPretty">
              <h4 class="rule-detail-subtitle">
                约束参数
              </h4>
              <pre class="rule-params">{{ parametersPretty }}</pre>
            </template>

            <h4 class="rule-detail-subtitle">
              规则项（{{ currentDetail.items.length }}）
            </h4>
            <table v-if="currentDetail.items.length" class="rule-detail-table">
              <thead>
                <tr>
                  <th>分组</th>
                  <th>目标类型</th>
                  <th>目标名称</th>
                  <th>目标编码</th>
                  <th>备注</th>
                  <th>创建时间</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="item in currentDetail.items" :key="String(item.basicId)">
                  <td>{{ formatItemGroup(item) }}</td>
                  <td>{{ getOptionLabel(targetTypeOptions, item.targetType) }}</td>
                  <td>{{ item.targetName || `#${item.targetId}` }}</td>
                  <td>{{ formatNullable(item.targetCode) }}</td>
                  <td>{{ formatNullable(item.remark) }}</td>
                  <td>{{ formatNullableDate(item.createdTime) }}</td>
                </tr>
              </tbody>
            </table>
            <NEmpty v-else description="暂无规则项" style="padding: 24px 0" />
          </NScrollbar>
        </NSpin>
      </NDrawerContent>
    </NDrawer>

    <!-- 新增 / 编辑弹窗：基础字段 + 规则项动态行 -->
    <NModal
      v-model:show="modalVisible"
      :auto-focus="false"
      :bordered="false"
      :title="modalTitle"
      preset="card"
      style="width: 860px; max-width: 94vw"
    >
      <NForm :model="ruleForm" class="xh-edit-form-grid" label-placement="top">
        <NFormItem label="规则编码" path="ruleCode">
          <NInput
            v-model:value="ruleForm.ruleCode"
            clearable
            :disabled="Boolean(ruleForm.basicId)"
            placeholder="如: ssd_finance_audit"
          />
        </NFormItem>
        <NFormItem label="规则名称" path="ruleName">
          <NInput v-model:value="ruleForm.ruleName" clearable placeholder="请输入规则名称" />
        </NFormItem>
        <NFormItem label="约束类型" path="constraintType">
          <NSelect
            v-model:value="ruleForm.constraintType"
            :options="constraintTypeOptions"
            @update:value="onConstraintTypeChange"
          />
        </NFormItem>
        <NFormItem label="目标类型" path="targetType">
          <NSelect
            v-model:value="ruleForm.targetType"
            :options="targetTypeOptions"
            @update:value="onRuleTargetTypeChange"
          />
        </NFormItem>
        <NFormItem label="违规处理" path="violationAction">
          <NSelect v-model:value="ruleForm.violationAction" :options="violationActionOptions" />
        </NFormItem>
        <NFormItem label="优先级" path="priority">
          <NInputNumber v-model:value="ruleForm.priority" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem label="状态" path="status">
          <NSelect v-model:value="ruleForm.status" :options="statusOptions" />
        </NFormItem>
        <NFormItem label="备注" path="remark">
          <NInput v-model:value="ruleForm.remark" clearable placeholder="请输入备注" />
        </NFormItem>
        <NFormItem label="生效时间" path="effectiveTime">
          <NDatePicker v-model:value="ruleForm.effectiveTime" clearable style="width: 100%" type="datetime" />
        </NFormItem>
        <NFormItem label="失效时间" path="expirationTime">
          <NDatePicker v-model:value="ruleForm.expirationTime" clearable style="width: 100%" type="datetime" />
        </NFormItem>
        <NFormItem class="xh-form-full-row" label="约束参数（JSON，可选）" path="parameters">
          <NInput
            v-model:value="ruleForm.parameters"
            clearable
            placeholder='如基数约束: {"maxCount": 2}'
            :rows="2"
            type="textarea"
          />
        </NFormItem>
        <NFormItem class="xh-form-full-row" label="描述" path="description">
          <NInput
            v-model:value="ruleForm.description"
            clearable
            placeholder="请输入规则描述"
            :rows="2"
            type="textarea"
          />
        </NFormItem>
      </NForm>

      <div class="rule-items">
        <div class="rule-items__head">
          <span class="rule-items__title">规则项（{{ ruleForm.items.length }}）</span>
          <span class="rule-items__hint">{{ itemHint }}</span>
          <NButton dashed size="small" @click="addItem">
            添加规则项
          </NButton>
        </div>
        <NEmpty v-if="ruleForm.items.length === 0" description="尚未添加规则项" style="padding: 20px 0" />
        <div v-for="(item, index) in ruleForm.items" :key="index" class="rule-item-row">
          <NInputNumber
            v-model:value="item.constraintGroup"
            :min="0"
            placeholder="分组"
            size="small"
            style="width: 100px"
          />
          <NSelect
            v-model:value="item.targetType"
            :disabled="!isPrerequisite"
            :options="targetTypeOptions"
            size="small"
            style="width: 110px"
            @update:value="() => onItemTargetTypeChange(item)"
          />
          <NSelect
            v-model:value="item.targetId"
            clearable
            filterable
            :loading="targetLoading[item.targetType]"
            :options="targetOptions[item.targetType]"
            placeholder="搜索并选择目标"
            remote
            size="small"
            style="flex: 1; min-width: 0"
            @focus="() => loadTargetOptions(item.targetType)"
            @search="(kw: string) => loadTargetOptions(item.targetType, kw)"
          />
          <NInput
            v-model:value="item.remark"
            clearable
            placeholder="备注（可选）"
            size="small"
            style="width: 160px"
          />
          <NButton quaternary size="small" type="error" @click="removeItem(index)">
            删除
          </NButton>
        </div>
      </div>

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

<style scoped>
/* 详情抽屉 */
.rule-detail-empty {
  padding: 48px 0;
}

.rule-detail-subtitle {
  margin: 18px 0 8px;
  font-size: 13px;
  font-weight: 600;
}

.rule-params {
  margin: 0;
  padding: 10px 12px;
  border: 1px solid var(--n-border-color);
  border-radius: 8px;
  background: var(--n-merged-th-color, rgb(0 0 0 / 0.02));
  font-size: 12px;
  line-height: 1.6;
  white-space: pre-wrap;
  word-break: break-all;
}

.rule-detail-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 13px;
}

.rule-detail-table th,
.rule-detail-table td {
  padding: 9px 10px;
  border: 1px solid var(--n-border-color);
  text-align: left;
  vertical-align: top;
}

.rule-detail-table th {
  background: var(--n-merged-th-color);
  font-weight: 500;
}

/* 规则项动态行编辑 */
.rule-items {
  margin-top: 4px;
  padding: 12px;
  border: 1px solid var(--n-border-color);
  border-radius: 8px;
}

.rule-items__head {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 10px;
}

.rule-items__title {
  font-size: 13px;
  font-weight: 600;
}

.rule-items__hint {
  flex: 1;
  font-size: 12px;
  opacity: 0.6;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.rule-item-row {
  display: flex;
  align-items: center;
  gap: 8px;
}

.rule-item-row + .rule-item-row {
  margin-top: 8px;
}
</style>
