<script setup lang="ts">
import type { CascaderOption, DataTableColumns } from 'naive-ui'
import type {
  ApiId,
  DepartmentCreateDto,
  DepartmentDetailDto,
  DepartmentListItemDto,
  DepartmentManagementDetailDto,
  DepartmentManagementMemberDto,
  DepartmentTreeNodeDto,
  DepartmentUpdateDto,
} from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import {
  NButton,
  NCascader,
  NConfigProvider,
  NDataTable,
  NDescriptions,
  NDescriptionsItem,
  NEmpty,
  NForm,
  NFormItem,
  NInput,
  NInputNumber,
  NModal,
  NSelect,
  NSpace,
  NTabPane,
  NTabs,
  NTag,
  useMessage,
} from 'naive-ui'
import { computed, h, onMounted, ref } from 'vue'
import {
  DepartmentType,
  EnableStatus,
  orgManagementApi,
  ValidityStatus,
} from '@/api'
import { Icon, SchemaPage } from '~/components'
import { DEPARTMENT_TYPE_OPTIONS, STATUS_OPTIONS } from '~/constants'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemOrgPage' })

interface DeptFormModel extends DepartmentCreateDto {
  basicId?: ApiId
}

const message = useMessage()
const statusOptions = STATUS_OPTIONS
const deptTypeOptions = DEPARTMENT_TYPE_OPTIONS

const schemaPageRef = ref<{ reload: () => Promise<void> } | null>(null)

// 上级部门 Cascader 选项：单独维护一份树，随 reload 同步刷新
const treeNodes = ref<DepartmentTreeNodeDto[]>([])

function treeToCascaderOptions(nodes: DepartmentTreeNodeDto[]): CascaderOption[] {
  return nodes.map(node => ({
    children: node.children && node.children.length > 0 ? treeToCascaderOptions(node.children) : undefined,
    label: node.departmentName,
    value: node.basicId,
  }))
}

const cascaderOptions = computed(() => treeToCascaderOptions(treeNodes.value))

async function loadCascaderTree() {
  try {
    treeNodes.value = await orgManagementApi.tree({ keyword: null, limit: 2000, onlyEnabled: false })
  }
  catch {
    treeNodes.value = []
  }
}

async function reloadAll() {
  await Promise.all([schemaPageRef.value?.reload(), loadCascaderTree()])
}

// ── 字段单一事实源 ──────────────────────────────────────────────
const fields: ListFieldSchema[] = [
  {
    key: 'keyword',
    title: '关键词',
    dataType: 'string',
    visible: false,
    searchable: true,
    searchPlaceholder: '搜索部门名称/编码',
    width: 250,
    order: 0,
  },
  {
    key: 'departmentName',
    title: '部门名称',
    dataType: 'string',
    treeColumn: true,
    minWidth: 220,
    order: 1,
  },
  { key: 'departmentCode', title: '部门编码', dataType: 'string', minWidth: 130, order: 2 },
  {
    key: 'departmentType',
    title: '类型',
    dataType: 'string',
    minWidth: 100,
    order: 3,
    render: row =>
      h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, getOptionLabel(deptTypeOptions, (row as unknown as DepartmentListItemDto).departmentType)),
  },
  {
    key: 'status',
    title: '状态',
    dataType: 'enum',
    width: 80,
    order: 4,
    render: (row) => {
      const status = (row as unknown as DepartmentListItemDto).status
      return h(NTag, { size: 'small', round: true, type: status === EnableStatus.Enabled ? 'success' : 'error', bordered: false }, () => getOptionLabel(statusOptions, status))
    },
  },
  { key: 'phone', title: '电话', dataType: 'phone', minWidth: 130, order: 5 },
  { key: 'email', title: '邮箱', dataType: 'email', minWidth: 180, order: 6 },
  { key: 'sort', title: '排序', dataType: 'number', width: 80, order: 7 },
  { key: 'createdTime', title: '创建时间', dataType: 'datetime', minWidth: 170, order: 8 },
]

// ── 资源适配器：归一化查询参数 → 后端 API ──────────────────────
// DepartmentTreeQueryDto 仅支持 keyword/limit/onlyEnabled；类型/状态仅作为列展示。
const schema: PageSchema = {
  pageCode: 'system.org',
  exportPermission: 'saas:department:export',
  pageName: '组织机构',
  batchRemovable: true,
  removePermission: 'saas:department:delete',
  statusPermission: 'saas:department:status',
  rowKey: 'basicId',
  scrollX: 1400,
  tree: { childrenKey: 'children', defaultExpandAll: false },
  fields,
  resource: {
    tree: (params) => {
      const keyword = params.filters.keyword as string | undefined
      return orgManagementApi.tree({
        keyword: keyword?.trim() || null,
        limit: 2000,
        onlyEnabled: false,
      }) as unknown as Promise<Record<string, unknown>[]>
    },
    remove: id => orgManagementApi.delete(id),
    updateStatus: (id, enabled) => orgManagementApi.updateStatus({ basicId: id, status: enabled ? EnableStatus.Enabled : EnableStatus.Disabled }),
  },
  actions: [
    { key: 'create', title: '新增根部门', scope: 'page', type: 'primary', icon: 'lucide:plus' },
    { key: 'addChild', title: '新增子部门', scope: 'row' },
    { key: 'view', title: '查看详情', scope: 'row' },
    { key: 'edit', title: '编辑', scope: 'row' },
    { key: 'toggle', title: '启用/停用', scope: 'row' },
  ],
}

// ── 行/页面操作分发 ─────────────────────────────────────────────
function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as DepartmentListItemDto | undefined
  switch (payload.key) {
    case 'create':
      handleAdd()
      break
    case 'addChild':
      if (row) {
        handleAdd(row.basicId)
      }
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
  }
}

// ── 新增/编辑表单 ──────────────────────────────────────────────
const modalVisible = ref(false)
const submitLoading = ref(false)
const deptForm = ref<DeptFormModel>(createDefaultForm())
const modalTitle = computed(() => (deptForm.value.basicId ? '编辑部门' : '新增部门'))

// ── 详情弹窗 ───────────────────────────────────────────────────
const detailVisible = ref(false)
const detailLoading = ref(false)
const managementDetail = ref<DepartmentManagementDetailDto | null>(null)
const detDept = computed(() => managementDetail.value?.department ?? null)

function createDefaultForm(): DeptFormModel {
  return {
    address: null,
    departmentCode: '',
    departmentName: '',
    departmentType: DepartmentType.Department,
    email: null,
    leaderId: null,
    parentId: null,
    phone: null,
    remark: null,
    sort: 100,
    status: EnableStatus.Enabled,
  }
}

function toStr(value?: string | null) {
  const normalized = value?.trim()
  return normalized || null
}

function formatNullable(value: unknown) {
  return value === null || value === undefined || value === '' ? '-' : String(value)
}

function formatNullableDate(value?: string | null) {
  return value ? formatDate(value) : '-'
}

function formatStatus(value?: EnableStatus | null) {
  return getOptionLabel(statusOptions, value)
}

function findDepartmentName(parentId: ApiId) {
  function walk(nodes: DepartmentTreeNodeDto[]): string | undefined {
    for (const node of nodes) {
      if (node.basicId === parentId) {
        return node.departmentName
      }
      if (node.children?.length) {
        const found = walk(node.children)
        if (found) {
          return found
        }
      }
    }
    return undefined
  }
  return walk(treeNodes.value) ?? formatNullable(parentId)
}

const childDeptColumns: DataTableColumns<DepartmentListItemDto> = [
  { title: '部门名称', key: 'departmentName', minWidth: 120, ellipsis: { tooltip: true } },
  { title: '编码', key: 'departmentCode', width: 100, ellipsis: { tooltip: true } },
  {
    title: '类型',
    key: 'departmentType',
    width: 90,
    render: row => getOptionLabel(deptTypeOptions, row.departmentType),
  },
  {
    title: '状态',
    key: 'status',
    width: 72,
    render: row => h(NTag, { size: 'small', round: true, type: row.status === EnableStatus.Enabled ? 'success' : 'error', bordered: false }, () => formatStatus(row.status)),
  },
]

const memberColumns: DataTableColumns<DepartmentManagementMemberDto> = [
  {
    title: '用户',
    key: 'user',
    minWidth: 140,
    render: row => row.realName || row.nickName || row.userName || String(row.userId),
  },
  { title: '用户名', key: 'userName', width: 110, ellipsis: { tooltip: true }, render: row => row.userName ?? '—' },
  {
    title: '主部门',
    key: 'isMain',
    width: 72,
    render: row => row.isMain
      ? h(NTag, { size: 'small', type: 'info', bordered: false }, () => '是')
      : h('span', { style: 'color:var(--n-text-color-3)' }, '—'),
  },
  {
    title: '状态',
    key: 'status',
    width: 72,
    render: row => h(NTag, {
      size: 'small',
      round: true,
      type: row.status === ValidityStatus.Valid ? 'success' : 'default',
      bordered: false,
    }, () => (row.status === ValidityStatus.Valid ? '有效' : '无效')),
  },
]

function handleAdd(parentId?: ApiId) {
  deptForm.value = createDefaultForm()
  deptForm.value.parentId = parentId ?? null
  modalVisible.value = true
}

function buildFormModel(row: DepartmentDetailDto | DepartmentListItemDto): DeptFormModel {
  return {
    ...createDefaultForm(),
    address: 'address' in row ? row.address ?? null : null,
    basicId: row.basicId,
    departmentCode: row.departmentCode,
    departmentName: row.departmentName,
    departmentType: row.departmentType,
    email: 'email' in row ? row.email ?? null : null,
    leaderId: row.leaderId ?? null,
    parentId: row.parentId ?? null,
    phone: 'phone' in row ? row.phone ?? null : null,
    remark: 'remark' in row ? row.remark ?? null : null,
    sort: row.sort,
    status: row.status,
  }
}

async function handleEdit(row: DepartmentListItemDto) {
  try {
    const detail = await orgManagementApi.detail(row.basicId)
    deptForm.value = buildFormModel(detail ?? row)
  }
  catch {
    message.error('加载部门详情失败')
    deptForm.value = buildFormModel(row)
  }
  modalVisible.value = true
}

async function handleView(row: DepartmentListItemDto) {
  detailVisible.value = true
  detailLoading.value = true
  managementDetail.value = null

  try {
    managementDetail.value = await orgManagementApi.detailView(row.basicId)
    if (!managementDetail.value) {
      message.warning('未查询到部门详情')
    }
  }
  catch {
    message.error('加载部门详情失败')
  }
  finally {
    detailLoading.value = false
  }
}

async function handleToggleStatus(row: DepartmentListItemDto) {
  const nextStatus = row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled
  try {
    await orgManagementApi.updateStatus({ basicId: row.basicId, status: nextStatus })
    message.success('状态更新成功')
    await reloadAll()
  }
  catch {
    message.error('状态更新失败')
  }
}

function validateForm() {
  if (!deptForm.value.departmentName.trim()) {
    message.warning('请输入部门名称')
    return false
  }
  if (!deptForm.value.basicId && !deptForm.value.departmentCode.trim()) {
    message.warning('请输入部门编码')
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
    if (deptForm.value.basicId) {
      const updateInput: DepartmentUpdateDto = {
        address: toStr(deptForm.value.address),
        basicId: deptForm.value.basicId,
        departmentName: deptForm.value.departmentName.trim(),
        departmentType: deptForm.value.departmentType,
        email: toStr(deptForm.value.email),
        leaderId: deptForm.value.leaderId,
        parentId: deptForm.value.parentId,
        phone: toStr(deptForm.value.phone),
        remark: toStr(deptForm.value.remark),
        sort: deptForm.value.sort,
        status: deptForm.value.status,
      }
      await orgManagementApi.update(updateInput)
    }
    else {
      const createInput: DepartmentCreateDto = {
        address: toStr(deptForm.value.address),
        departmentCode: deptForm.value.departmentCode.trim(),
        departmentName: deptForm.value.departmentName.trim(),
        departmentType: deptForm.value.departmentType,
        email: toStr(deptForm.value.email),
        leaderId: deptForm.value.leaderId,
        parentId: deptForm.value.parentId,
        phone: toStr(deptForm.value.phone),
        remark: toStr(deptForm.value.remark),
        sort: deptForm.value.sort,
        status: deptForm.value.status,
      }
      await orgManagementApi.create(createInput)
    }

    message.success('保存成功')
    modalVisible.value = false
    await reloadAll()
  }
  catch {
    message.error('保存失败')
  }
  finally {
    submitLoading.value = false
  }
}

onMounted(() => {
  void loadCascaderTree()
})
</script>

<template>
  <SchemaPage
    ref="schemaPageRef"
    :schema="schema"
    @action="onAction"
  >
    <NModal
      v-model:show="detailVisible"
      class="xh-mgmt-detail-modal"
      preset="card"
      :bordered="false"
      :mask-closable="true"
      style="width: 720px; max-width: calc(100vw - 32px);"
    >
      <template v-if="detDept" #header>
        <div class="det-hd-entity">
          <div class="det-hd-ico">
            <Icon icon="tabler:building" :size="22" />
          </div>
          <div class="min-w-0">
            <div class="det-hd-name">
              {{ detDept.departmentName }}
            </div>
            <div class="det-hd-sub">
              {{ detDept.departmentCode }}
            </div>
          </div>
        </div>
      </template>

      <div v-if="detailLoading" class="modal-loading">
        加载中…
      </div>
      <NTabs v-else-if="managementDetail" type="line" animated size="small">
        <NTabPane name="overview" tab="概览">
          <NDescriptions :column="2" bordered size="small">
            <NDescriptionsItem label="部门类型">
              {{ getOptionLabel(deptTypeOptions, detDept!.departmentType) }}
            </NDescriptionsItem>
            <NDescriptionsItem label="父级部门">
              {{ detDept!.parentId ? findDepartmentName(detDept!.parentId) : '—' }}
            </NDescriptionsItem>
            <NDescriptionsItem label="负责人 ID">
              {{ formatNullable(detDept!.leaderId) }}
            </NDescriptionsItem>
            <NDescriptionsItem label="状态">
              <NTag size="small" :type="detDept!.status === EnableStatus.Enabled ? 'success' : 'error'" :bordered="false">
                {{ formatStatus(detDept!.status) }}
              </NTag>
            </NDescriptionsItem>
            <NDescriptionsItem label="电话">
              {{ formatNullable(detDept!.phone) }}
            </NDescriptionsItem>
            <NDescriptionsItem label="邮箱">
              {{ formatNullable(detDept!.email) }}
            </NDescriptionsItem>
            <NDescriptionsItem label="地址" :span="2">
              {{ formatNullable(detDept!.address) }}
            </NDescriptionsItem>
            <NDescriptionsItem label="排序">
              {{ detDept!.sort }}
            </NDescriptionsItem>
            <NDescriptionsItem label="创建时间">
              {{ formatNullableDate(detDept!.createdTime) }}
            </NDescriptionsItem>
            <NDescriptionsItem v-if="detDept!.remark" label="备注" :span="2">
              {{ detDept!.remark }}
            </NDescriptionsItem>
          </NDescriptions>
        </NTabPane>
        <NTabPane name="children" :tab="`子部门 (${managementDetail.childDepartments.length})`">
          <div class="xh-detail-table-wrap">
            <NDataTable
              v-if="managementDetail.childDepartments.length"
              :columns="childDeptColumns"
              :data="managementDetail.childDepartments"
              :bordered="false"
              size="small"
              :row-key="(row: DepartmentListItemDto) => row.basicId"
            />
            <NEmpty v-else description="暂无子部门" style="padding: 32px 0" />
          </div>
        </NTabPane>
        <NTabPane name="members" :tab="`部门成员 (${managementDetail.members.length})`">
          <div class="xh-detail-table-wrap">
            <NDataTable
              v-if="managementDetail.members.length"
              :columns="memberColumns"
              :data="managementDetail.members"
              :bordered="false"
              size="small"
              :row-key="(row: DepartmentManagementMemberDto) => row.basicId"
            />
            <NEmpty v-else description="暂无部门成员" style="padding: 32px 0" />
          </div>
        </NTabPane>
      </NTabs>

      <template #footer>
        <NSpace justify="end">
          <NButton size="small" @click="detailVisible = false">
            关闭
          </NButton>
          <NButton
            v-if="detDept"
            size="small"
            type="primary"
            @click="detailVisible = false; handleEdit(detDept as DepartmentListItemDto)"
          >
            编辑
          </NButton>
        </NSpace>
      </template>
    </NModal>

    <NModal
      v-model:show="modalVisible"
      :auto-focus="false"
      :bordered="false"
      :title="modalTitle"
      preset="card"
      style="width: 720px; max-width: 92vw"
    >
      <NConfigProvider size="small" abstract>
        <NForm :model="deptForm" size="small" class="xh-edit-form-grid" label-placement="top">
          <NFormItem label="部门名称" path="departmentName">
            <NInput v-model:value="deptForm.departmentName" clearable placeholder="请输入部门名称" />
          </NFormItem>
          <NFormItem label="部门编码" path="departmentCode">
            <NInput
              v-model:value="deptForm.departmentCode"
              :disabled="Boolean(deptForm.basicId)"
              clearable
              placeholder="如: tech_dept"
            />
          </NFormItem>
          <NFormItem label="上级部门" path="parentId">
            <NCascader
              v-model:value="deptForm.parentId"
              :options="cascaderOptions"
              check-strategy="child"
              clearable
              placeholder="选择上级部门（可留空）"
              style="width: 100%"
            />
          </NFormItem>
          <NFormItem label="部门类型" path="departmentType">
            <NSelect v-model:value="deptForm.departmentType" :options="deptTypeOptions" />
          </NFormItem>
          <NFormItem label="电话" path="phone">
            <NInput v-model:value="deptForm.phone" clearable placeholder="请输入电话" />
          </NFormItem>
          <NFormItem label="邮箱" path="email">
            <NInput v-model:value="deptForm.email" clearable placeholder="请输入邮箱" />
          </NFormItem>
          <NFormItem label="地址" path="address">
            <NInput v-model:value="deptForm.address" clearable placeholder="请输入地址" />
          </NFormItem>
          <NFormItem label="排序" path="sort">
            <NInputNumber v-model:value="deptForm.sort" :min="0" style="width: 100%" />
          </NFormItem>
          <NFormItem label="备注" path="remark">
            <NInput v-model:value="deptForm.remark" clearable placeholder="请输入备注" :rows="3" type="textarea" />
          </NFormItem>
        </NForm>
      </NConfigProvider>

      <template #footer>
        <NSpace justify="end">
          <NButton size="small" @click="modalVisible = false">
            取消
          </NButton>
          <NButton size="small" :loading="submitLoading" type="primary" @click="handleSubmit">
            保存
          </NButton>
        </NSpace>
      </template>
    </NModal>
  </SchemaPage>
</template>

<style scoped>
.xh-detail-table-wrap {
  width: 100%;
}
</style>
