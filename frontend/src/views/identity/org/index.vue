<script setup lang="ts">
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
import type { ListFieldSchema, PageSchema, SchemaActionPayload, XDataTableColumn } from '~/components'
import type { TreeSelectOption } from '~/types'
import { XhButton, XhDescriptionsItem, XhDescriptionsLabel, XhDescriptionsRoot, XhDescriptionsValue, XhDialogCloseTrigger, XhDialogContent, XhDialogRoot, XhDialogTitle, XhEmptyStateDescription, XhEmptyStateIcon, XhEmptyStateRoot, XhEmptyStateTitle, XhFieldControl, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFlex, XhFormFieldGroup, XhFormRoot, XhTabsContent, XhTabsList, XhTabsRoot, XhTabsTrigger, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, h, onMounted, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  DepartmentType,
  EnableStatus,
  orgManagementApi,
  positionApi,
  userDepartmentApi,
  ValidityStatus,
} from '@/api'
import { DEPARTMENT_TYPE_OPTIONS, STATUS_OPTIONS } from '@/constants'
import { Icon, SchemaPage, XCascader, XDataTable, XDatePicker, XEditModal, XInput, XNumberInput, XSelect } from '~/components'
import { toast } from '~/composables'
import { useEnumOptions } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemOrgPage' })

const { t } = useI18n()

/** 编辑弹窗的保存钮靠这个 id 关联到表单，点它才会走整表校验 */
const editFormId = useId()
const membershipFormId = useId()

interface DeptFormModel extends DepartmentCreateDto {
  basicId?: ApiId
}

const statusOptions = useEnumOptions('EnableStatus', STATUS_OPTIONS)
const deptTypeOptions = useEnumOptions('DepartmentType', DEPARTMENT_TYPE_OPTIONS)

const schemaPageRef = ref<{ reload: () => Promise<void> } | null>(null)

// 上级部门 Cascader 选项：单独维护一份树，随 reload 同步刷新
const treeNodes = ref<DepartmentTreeNodeDto[]>([])

function treeToCascaderOptions(nodes: DepartmentTreeNodeDto[]): TreeSelectOption[] {
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
const fields = computed<ListFieldSchema[]>(() => [
  {
    key: 'keyword',
    title: t('identity.org.col_keyword'),
    dataType: 'string',
    visible: false,
    searchable: true,
    searchPlaceholder: t('identity.org.keyword_placeholder'),
    width: 250,
    order: 0,
  },
  {
    key: 'departmentName',
    title: t('identity.org.col_department_name'),
    dataType: 'string',
    treeColumn: true,
    minWidth: 220,
    order: 1,
  },
  { key: 'departmentCode', title: t('identity.org.col_department_code'), dataType: 'string', minWidth: 130, order: 2 },
  {
    key: 'departmentType',
    title: t('identity.org.col_type'),
    dataType: 'string',
    minWidth: 100,
    order: 3,
    render: row =>
      h('span', { style: 'font-size:13px;color:hsl(var(--muted-foreground));' }, getOptionLabel(deptTypeOptions.value, (row as unknown as DepartmentListItemDto).departmentType)),
  },
  {
    key: 'status',
    title: t('identity.org.col_status'),
    dataType: 'enum',
    width: 80,
    order: 4,
    dictionaryCode: 'EnableStatus',
    render: (row) => {
      const status = (row as unknown as DepartmentListItemDto).status
      return h(XhTagRoot, { variant: 'outline', tone: status === EnableStatus.Enabled ? 'success' : 'danger' }, () => h(XhTagLabel, () => getOptionLabel(statusOptions.value, status)))
    },
  },
  { key: 'phone', title: t('identity.org.col_phone'), dataType: 'phone', minWidth: 130, order: 5 },
  { key: 'email', title: t('identity.org.col_email'), dataType: 'email', minWidth: 180, order: 6 },
  { key: 'sort', title: t('identity.org.col_sort'), dataType: 'number', width: 80, order: 7 },
  { key: 'createdTime', title: t('identity.org.col_create_time'), dataType: 'datetime', minWidth: 170, order: 8 },
])

// ── 资源适配器：归一化查询参数 → 后端 API ──────────────────────
// DepartmentTreeQueryDto 仅支持 keyword/limit/onlyEnabled；类型/状态仅作为列展示。
const schema = computed<PageSchema>(() => ({
  pageCode: 'system.org',
  exportPermission: 'identity.org.export',
  pageName: t('identity.org.page_name'),
  batchRemovable: true,
  removePermission: 'identity.org.delete',
  statusPermission: 'identity.org.status',
  rowKey: 'basicId',
  tree: { childrenKey: 'children', defaultExpandAll: false },
  fields: fields.value,
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
    { key: 'create', title: t('identity.org.action_create'), scope: 'page', type: 'primary', icon: 'lucide:plus' },
    { key: 'addChild', title: t('identity.org.action_add_child'), scope: 'row' },
    { key: 'view', title: t('identity.org.action_view'), scope: 'row' },
    { key: 'edit', title: t('identity.org.action_edit'), scope: 'row' },
    { key: 'toggle', title: t('identity.org.action_toggle'), scope: 'row' },
  ],
}))

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
const modalTitle = computed(() => (deptForm.value.basicId ? t('identity.org.form_edit_title') : t('identity.org.form_create_title')))

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
  return getOptionLabel(statusOptions.value, value)
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

const childDeptColumns = computed<XDataTableColumn<DepartmentListItemDto>[]>(() => [
  { title: t('identity.org.detail_table_dept_name'), key: 'departmentName', minWidth: 120, ellipsis: true },
  { title: t('identity.org.detail_table_code'), key: 'departmentCode', width: 100, ellipsis: true },
  {
    title: t('identity.org.detail_table_type'),
    key: 'departmentType',
    width: 90,
    render: row => getOptionLabel(deptTypeOptions.value, row.departmentType),
  },
  {
    title: t('identity.org.detail_table_status'),
    key: 'status',
    width: 72,
    render: row => h(XhTagRoot, { variant: 'outline', tone: row.status === EnableStatus.Enabled ? 'success' : 'danger' }, () => h(XhTagLabel, () => formatStatus(row.status))),
  },
])

const memberColumns = computed<XDataTableColumn<DepartmentManagementMemberDto>[]>(() => [
  {
    title: t('identity.org.detail_table_user'),
    key: 'user',
    minWidth: 140,
    render: row => row.realName || row.nickName || row.userName || String(row.userId),
  },
  { title: t('identity.org.detail_table_username'), key: 'userName', width: 110, ellipsis: true, render: row => row.userName ?? '—' },
  { title: t('identity.org.detail_table_position'), key: 'positionName', width: 120, ellipsis: true, render: row => row.positionName ?? '—' },
  { title: t('identity.org.detail_table_job_number'), key: 'jobNumber', width: 100, ellipsis: true, render: row => row.jobNumber ?? '—' },
  {
    title: t('identity.org.detail_table_is_main'),
    key: 'isMain',
    width: 72,
    render: row => row.isMain
      ? h(XhTagRoot, { variant: 'outline', tone: 'info' }, () => h(XhTagLabel, () => t('common.statuses.yes')))
      : h('span', { style: 'color:hsl(var(--muted-foreground))' }, '—'),
  },
  {
    title: t('identity.org.detail_table_status'),
    key: 'status',
    width: 72,
    render: row => h(XhTagRoot, { variant: 'outline', tone: row.status === ValidityStatus.Valid ? 'success' : 'neutral' }, () => h(XhTagLabel, () => (row.status === ValidityStatus.Valid ? t('identity.org.member_valid') : t('identity.org.member_invalid')))),
  },
  {
    title: t('identity.org.detail_table_actions'),
    key: 'actions',
    width: 90,
    render: row => h(XhButton, { size: 'sm', variant: 'ghost', tone: 'brand', onClick: () => openEditMembership(row) }, () => t('identity.org.action_edit_membership')),
  },
])

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
  catch (error) {
    toast.error((error as Error)?.message || t('identity.org.msg_load_detail_failed'))
    deptForm.value = buildFormModel(row)
  }
  modalVisible.value = true
}

async function handleView(row: DepartmentListItemDto) {
  detailVisible.value = true
  detailLoading.value = true
  managementDetail.value = null

  try {
    const detail = await orgManagementApi.detailView(row.basicId)
    // 后端异常时可能返回非 DTO 形状（缺少 department/childDepartments/members），按未找到处理，避免渲染崩溃
    if (!detail || !detail.department) {
      managementDetail.value = null
      toast.warning(t('identity.org.msg_detail_not_found'))
      return
    }
    managementDetail.value = detail
  }
  catch (error) {
    toast.error((error as Error)?.message || t('identity.org.msg_load_detail_failed'))
  }
  finally {
    detailLoading.value = false
  }
}

async function handleToggleStatus(row: DepartmentListItemDto) {
  const nextStatus = row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled
  try {
    await orgManagementApi.updateStatus({ basicId: row.basicId, status: nextStatus })
    toast.success(t('identity.org.msg_status_updated'))
    await reloadAll()
  }
  catch (error) {
    toast.error((error as Error)?.message || t('identity.org.msg_status_failed'))
  }
}

// ── 成员任职（岗位/工号/职级/入职日期）编辑 ──────────────────
const positionOptions = ref<{ label: string, value: ApiId }[]>([])

async function loadPositionOptions() {
  try {
    const positions = await positionApi.enabledList()
    positionOptions.value = positions.map(item => ({ label: item.positionName, value: item.basicId }))
  }
  catch {
    positionOptions.value = []
  }
}

interface MembershipFormModel {
  basicId: ApiId
  isMain: boolean
  jobLevel: string | null
  jobNumber: string | null
  joinTime: number | null
  positionId: ApiId | null
  remark: string | null
}

const membershipVisible = ref(false)
const membershipLoading = ref(false)
const membershipMemberName = ref('')
const membershipForm = ref<MembershipFormModel>(createDefaultMembershipForm())

function createDefaultMembershipForm(): MembershipFormModel {
  return { basicId: '', isMain: false, jobLevel: null, jobNumber: null, joinTime: null, positionId: null, remark: null }
}

function openEditMembership(member: DepartmentManagementMemberDto) {
  membershipMemberName.value = member.realName || member.nickName || member.userName || String(member.userId)
  membershipForm.value = {
    basicId: member.basicId,
    isMain: member.isMain,
    jobLevel: member.jobLevel ?? null,
    jobNumber: member.jobNumber ?? null,
    joinTime: member.joinTime ? new Date(member.joinTime).getTime() : null,
    positionId: member.positionId ?? null,
    remark: member.remark ?? null,
  }
  membershipVisible.value = true
}

async function refreshManagementDetail() {
  const deptId = managementDetail.value?.department.basicId
  if (!deptId) {
    return
  }
  try {
    managementDetail.value = await orgManagementApi.detailView(deptId)
  }
  catch {
    // 静默：保留旧详情
  }
}

async function submitMembership() {
  membershipLoading.value = true
  try {
    await userDepartmentApi.update({
      basicId: membershipForm.value.basicId,
      isMain: membershipForm.value.isMain,
      jobLevel: toStr(membershipForm.value.jobLevel),
      jobNumber: toStr(membershipForm.value.jobNumber),
      joinTime: membershipForm.value.joinTime ? new Date(membershipForm.value.joinTime).toISOString() : null,
      positionId: membershipForm.value.positionId,
      remark: toStr(membershipForm.value.remark),
    })
    toast.success(t('common.messages.save_success'))
    membershipVisible.value = false
    await refreshManagementDetail()
  }
  catch (error) {
    toast.error((error as Error)?.message || t('common.messages.save_failed'))
  }
  finally {
    membershipLoading.value = false
  }
}

function validateForm() {
  if (!deptForm.value.departmentName.trim()) {
    toast.warning(t('identity.org.msg_department_name_required'))
    return false
  }
  if (!deptForm.value.basicId && !deptForm.value.departmentCode.trim()) {
    toast.warning(t('identity.org.msg_department_code_required'))
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

    toast.success(t('common.messages.save_success'))
    modalVisible.value = false
    await reloadAll()
  }
  catch (error) {
    toast.error((error as Error)?.message || t('common.messages.save_failed'))
  }
  finally {
    submitLoading.value = false
  }
}

onMounted(() => {
  void loadCascaderTree()
  void loadPositionOptions()
})
</script>

<template>
  <SchemaPage
    ref="schemaPageRef"
    :schema="schema"
    @action="onAction"
  >
    <XhDialogRoot v-model:open="detailVisible">
      <XhDialogContent class="xh-mgmt-detail-modal" style="--xh-dialog-max-w: 720px">
        <XhDialogTitle v-if="detDept">
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
        </XhDialogTitle>
        <XhDialogCloseTrigger />

        <div v-if="detailLoading" class="modal-loading">
          {{ t('common.statuses.loading') }}
        </div>
        <!-- 面板内容各不相同，标签与面板手摆而不喂 collection -->
        <XhTabsRoot v-else-if="managementDetail && detDept" default-value="overview" variant="line">
          <XhTabsList>
            <XhTabsTrigger value="overview">
              {{ t('identity.org.tab_overview') }}
            </XhTabsTrigger>
            <XhTabsTrigger value="children">
              {{ t('identity.org.tab_children', { count: managementDetail.childDepartments?.length ?? 0 }) }}
            </XhTabsTrigger>
            <XhTabsTrigger value="members">
              {{ t('identity.org.tab_members', { count: managementDetail.members?.length ?? 0 }) }}
            </XhTabsTrigger>
          </XhTabsList>
          <XhTabsContent value="overview">
            <XhDescriptionsRoot :columns="2" bordered size="sm">
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('identity.org.label_department_type') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ getOptionLabel(deptTypeOptions, detDept!.departmentType) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('identity.org.label_parent') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ detDept!.parentId ? findDepartmentName(detDept!.parentId) : '—' }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('identity.org.label_leader_id') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatNullable(detDept!.leaderId) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('identity.org.label_status') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  <XhTagRoot variant="subtle" size="sm" :tone="detDept!.status === EnableStatus.Enabled ? 'success' : 'danger'">
                    <XhTagLabel>
                      {{ formatStatus(detDept!.status) }}
                    </XhTagLabel>
                  </XhTagRoot>
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('identity.org.label_phone') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatNullable(detDept!.phone) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('identity.org.label_email') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatNullable(detDept!.email) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem style="grid-column: span 2">
                <XhDescriptionsLabel>{{ t('identity.org.label_address') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatNullable(detDept!.address) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('identity.org.label_sort') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ detDept!.sort }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('identity.org.label_create_time') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatNullableDate(detDept!.createdTime) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem v-if="detDept!.remark" style="grid-column: span 2">
                <XhDescriptionsLabel>{{ t('identity.org.label_remark') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ detDept!.remark }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
            </XhDescriptionsRoot>
          </XhTabsContent>
          <XhTabsContent value="children">
            <div class="xh-detail-table-wrap">
              <XDataTable
                v-if="managementDetail.childDepartments?.length"
                :columns="childDeptColumns"
                :data="managementDetail.childDepartments"
                size="sm"
                :row-key="(row: DepartmentListItemDto) => row.basicId"
              />
              <XhEmptyStateRoot v-else size="sm" style="padding: 32px 0">
                <XhEmptyStateIcon>
                  <Icon icon="lucide:inbox" width="24" />
                </XhEmptyStateIcon>
                <XhEmptyStateTitle>{{ t('common.no_data') }}</XhEmptyStateTitle>
                <XhEmptyStateDescription>{{ t('identity.org.empty_children') }}</XhEmptyStateDescription>
              </XhEmptyStateRoot>
            </div>
          </XhTabsContent>
          <XhTabsContent value="members">
            <div class="xh-detail-table-wrap">
              <XDataTable
                v-if="managementDetail.members?.length"
                :columns="memberColumns"
                :data="managementDetail.members"
                size="sm"
                :row-key="(row: DepartmentManagementMemberDto) => row.basicId"
              />
              <XhEmptyStateRoot v-else size="sm" style="padding: 32px 0">
                <XhEmptyStateIcon>
                  <Icon icon="lucide:inbox" width="24" />
                </XhEmptyStateIcon>
                <XhEmptyStateTitle>{{ t('common.no_data') }}</XhEmptyStateTitle>
                <XhEmptyStateDescription>{{ t('identity.org.empty_members') }}</XhEmptyStateDescription>
              </XhEmptyStateRoot>
            </div>
          </XhTabsContent>
        </XhTabsRoot>
        <XhEmptyStateRoot v-else style="padding: 48px 0">
          <XhEmptyStateIcon>
            <Icon icon="lucide:inbox" width="28" />
          </XhEmptyStateIcon>
          <XhEmptyStateTitle>{{ t('common.no_data') }}</XhEmptyStateTitle>
          <XhEmptyStateDescription>{{ t('identity.org.msg_detail_not_found') }}</XhEmptyStateDescription>
        </XhEmptyStateRoot>

        <div class="xh-dialog-footer">
          <XhFlex justify="end">
            <XhButton size="sm" @click="detailVisible = false">
              {{ t('common.actions.close') }}
            </XhButton>
            <XhButton
              v-if="detDept"
              size="sm"
              tone="brand"
              @click="detailVisible = false; handleEdit(detDept as DepartmentListItemDto)"
            >
              {{ t('common.actions.edit') }}
            </XhButton>
          </XhFlex>
        </div>
      </XhDialogContent>
    </XhDialogRoot>

    <XEditModal
      v-model:show="modalVisible"
      :title="modalTitle"
      :loading="submitLoading"
      :form-id="editFormId"
    >
      <XhFormRoot
        :id="editFormId"
        v-model:values="deptForm"
        validate-on="blur"
        class="xh-edit-form-grid"
        @submit="handleSubmit"
      >
        <XhFormFieldGroup value="departmentName">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('identity.org.label_department_name') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="deptForm.departmentName" clearable :placeholder="t('identity.org.ph_department_name')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="departmentCode">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('identity.org.label_department_code') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput
                v-model:value="deptForm.departmentCode"
                :disabled="Boolean(deptForm.basicId)"
                clearable
                :placeholder="t('identity.org.ph_department_code')"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="parentId">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('identity.org.label_parent_dept') }}</XhFieldLabel>
            <XhFieldControl>
              <XCascader
                v-model:value="deptForm.parentId"
                :options="cascaderOptions"
                clearable
                :placeholder="t('identity.org.ph_parent')"
                style="width: 100%"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="departmentType">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('identity.org.label_department_type') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect v-model:value="deptForm.departmentType" :options="deptTypeOptions" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="phone">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('identity.org.label_phone') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="deptForm.phone" clearable :placeholder="t('identity.org.ph_phone')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="email">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('identity.org.label_email') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="deptForm.email" clearable :placeholder="t('identity.org.ph_email')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="address">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('identity.org.label_address') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="deptForm.address" clearable :placeholder="t('identity.org.ph_address')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="sort">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('identity.org.label_sort') }}</XhFieldLabel>
            <XhFieldControl>
              <XNumberInput v-model:value="deptForm.sort" :min="0" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="remark" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('identity.org.label_remark') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="deptForm.remark" clearable :placeholder="t('identity.org.ph_remark')" :rows="3" type="textarea" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
      </XhFormRoot>
    </XEditModal>

    <XEditModal
      v-model:show="membershipVisible"
      :title="t('identity.org.membership_title', { name: membershipMemberName })"
      :loading="membershipLoading"
      :form-id="membershipFormId"
    >
      <XhFormRoot
        v-model:values="membershipForm"
        validate-on="blur"
        class="xh-edit-form-grid"
        @submit="submitMembership"
      >
        <XhFormFieldGroup value="positionId">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('identity.org.label_position') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect
                v-model:value="membershipForm.positionId"
                :options="positionOptions"
                clearable
                :placeholder="t('identity.org.ph_position')"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="jobNumber">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('identity.org.label_job_number') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="membershipForm.jobNumber" clearable :placeholder="t('identity.org.ph_job_number')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="jobLevel">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('identity.org.label_job_level') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="membershipForm.jobLevel" clearable :placeholder="t('identity.org.ph_job_level')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="joinTime">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('identity.org.label_join_time') }}</XhFieldLabel>
            <XhFieldControl>
              <XDatePicker v-model:value="membershipForm.joinTime" type="date" clearable />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="remark" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('identity.org.label_remark') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="membershipForm.remark" clearable :rows="2" type="textarea" :placeholder="t('identity.org.ph_remark')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
      </XhFormRoot>
    </XEditModal>
  </SchemaPage>
</template>

<style scoped>
.xh-detail-table-wrap {
  width: 100%;
}
</style>
