<script lang="ts" setup>
import type { DepartmentRow, DepartmentStatus, MembershipOptionScope, MembershipRow } from './use-departments'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaPageInstance, XDataTableColumn } from '~/components'
import type { SelectOption } from '~/types'
import { XhButton, XhDescriptionsItem, XhDescriptionsLabel, XhDescriptionsRoot, XhDescriptionsValue, XhDialogBody, XhDialogCloseTrigger, XhDialogContent, XhDialogFooter, XhDialogHeader, XhDialogRoot, XhDialogTitle, XhEmptyStateDescription, XhEmptyStateIndicator, XhEmptyStateRoot, XhEmptyStateTitle, XhFieldControl, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFormRoot, XhSwitch, XhTabsContent, XhTabsIndicator, XhTabsList, XhTabsRoot, XhTabsTrigger, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, h, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { deleteConfirmText, Icon, renderTag, SchemaPage, statusConfirmText, XCombobox, XDataTable, XDatePicker, XEditModal, XInput, XNumberInput, XSelect, XTreeSelect } from '~/components'
import { confirmDanger, toast } from '~/composables'
import { usePermission } from '~/hooks'
import { formatDate } from '~/utils'
import { departmentsResource, removeMembership, setPrimaryMembership, useDepartmentDetail, useDepartmentEditor, useMembershipEditor } from './use-departments'

defineOptions({ name: 'OrganizationDepartmentsView' })

const { t } = useI18n()
const { hasPermission } = usePermission()

// 资源取回的整棵树另存一份：上级部门的候选树由它派生，随列表刷新同步
const treeRows = ref<DepartmentRow[]>([])
const resource = departmentsResource((rows) => {
  treeRows.value = rows
})
const editor = useDepartmentEditor(treeRows)
/** 负责人要检索用户：看不了用户时不显示这一栏，编辑时原样带回 */
const canPickLeader = computed(() => hasPermission('identity.users.read'))

function onSearchLeaders(keyword: string) {
  editor.searchLeaders(keyword).catch((error: unknown) => toast.danger((error as Error).message || t('organization.departments.msg_load_users_failed')))
}

const canReadMembers = () => hasPermission('organization.memberships.read')
const detail = useDepartmentDetail(canReadMembers)
const memberEditor = useMembershipEditor()

/** 编辑弹窗的保存钮靠这个 id 关联到表单，点它才会走整表提交 */
const editFormId = useId()
const memberFormId = useId()
const schemaPageRef = ref<SchemaPageInstance | null>(null)

function reload() {
  void schemaPageRef.value?.reload()
}

// 状态值是契约字面量，文案键静态写出以便 i18n 门禁核对
function statusLabel(status: DepartmentStatus): string {
  return status === 'Disabled' ? t('organization.departments.status.disabled') : t('organization.departments.status.active')
}

function statusTone(status: DepartmentStatus) {
  return status === 'Active' ? 'success' : 'danger'
}

const statusOptions = computed<SelectOption[]>(() => (['Active', 'Disabled'] as const).map(status => ({ label: statusLabel(status), value: status })))

// ── 字段单一事实源（树表列；departments.tree 没有筛选与排序，故不声明搜索字段与可排序列） ──
// SchemaPage 的行类型在框架边界放宽（Record）：回调里按本页的行类型取用
const asRow = (row: object) => row as DepartmentRow

const fields = computed<ListFieldSchema[]>(() => [
  { key: 'name', title: t('organization.departments.fields.name'), dataType: 'string', treeColumn: true, exportable: true, minWidth: 220, order: 1 },
  { key: 'code', title: t('organization.departments.fields.code'), dataType: 'string', exportable: true, minWidth: 140, order: 2 },
  { key: 'status', title: t('organization.departments.fields.status'), dataType: 'enum', options: statusOptions.value, exportable: true, render: row => renderTag(statusLabel(asRow(row).status), statusTone(asRow(row).status)), width: 90, order: 3 },
  { key: 'memberCount', title: t('organization.departments.fields.member_count'), dataType: 'number', exportable: true, width: 90, order: 4 },
  { key: 'order', title: t('organization.departments.fields.order'), dataType: 'number', exportable: true, width: 80, order: 5 },
  { key: 'description', title: t('organization.departments.fields.description'), dataType: 'text', visible: false, exportable: true, order: 5.5 },
  { key: 'createdAt', title: t('organization.departments.fields.created_at'), dataType: 'datetime', exportable: true, minWidth: 170, order: 6 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'organization.departments',
  pageName: t('organization.departments.title'),
  exportPermission: 'organization.departments.read',
  batchRemovable: true,
  removePermission: 'organization.departments.delete',
  statusPermission: 'organization.departments.update',
  rowKey: 'id',
  tree: { childrenKey: 'children', defaultExpandAll: false },
  fields: fields.value,
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    { key: 'create', title: t('organization.departments.actions.create'), scope: 'page', tone: 'brand', icon: 'lucide:plus', permission: 'organization.departments.create' },
    { key: 'addChild', title: t('organization.departments.actions.add_child'), scope: 'row', icon: 'lucide:plus', permission: 'organization.departments.create' },
    { key: 'view', title: t('common.actions.view'), scope: 'row', icon: 'lucide:eye' },
    { key: 'edit', title: t('common.actions.edit'), scope: 'row', icon: 'lucide:pencil', permission: 'organization.departments.update' },
    { key: 'toggle', title: t('organization.departments.actions.toggle'), scope: 'row', icon: 'lucide:power', confirm: true, confirmText: row => statusConfirmText(t, asRow(row).status === 'Active', asRow(row).name), permission: 'organization.departments.update' },
    // 还有子部门或成员的由后端逐项拒绝（ORGANIZATION_CONFLICT_002）
    { key: 'delete', title: t('common.actions.delete'), scope: 'row', icon: 'lucide:trash-2', tone: 'danger', confirm: true, confirmText: row => deleteConfirmText(t, asRow(row).name), permission: 'organization.departments.delete' },
  ],
}))

// ── 行 / 页面操作分发 ──
function onAction(payload: SchemaActionPayload) {
  const row = payload.row ? asRow(payload.row) : undefined
  switch (payload.key) {
    case 'create':
      editor.startCreate()
      break
    case 'addChild':
      if (row)
        editor.startCreate(row.id)
      break
    case 'view':
      if (row)
        void detail.show(row)
      break
    case 'edit':
      if (row)
        editor.startEdit(row, canPickLeader.value)
      break
    case 'toggle':
      if (row)
        void toggleStatus(row)
      break
    case 'delete':
      if (row)
        void removeRow(row)
      break
  }
}

const modalTitle = computed(() => editor.isEdit.value ? t('organization.departments.edit_title') : t('organization.departments.create_title'))

async function onSubmit() {
  if (await editor.submit()) {
    toast.success(t('common.messages.save_success'))
    reload()
  }
}

async function removeRow(row: DepartmentRow) {
  try {
    await resource.remove?.(row.id)
    toast.success(t('common.messages.delete_success'))
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.delete_failed'))
  }
}

async function toggleStatus(row: DepartmentRow) {
  try {
    await resource.updateStatus?.(row.id, row.status !== 'Active')
    toast.success(t('common.messages.status_updated'))
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.status_failed'))
  }
}

// ── 查看详情（概览 / 子部门 / 部门成员） ──
const detailRow = computed(() => detail.row.value)
const childRows = computed(() => detail.row.value?.children ?? [])

function editFromDetail() {
  const row = detail.row.value
  if (!row)
    return
  detail.open.value = false
  editor.startEdit(row, canPickLeader.value)
}

function formatNullable(value: unknown): string {
  return value === null || value === undefined || value === '' ? '-' : String(value)
}

const childColumns = computed<XDataTableColumn<DepartmentRow>[]>(() => [
  { title: t('organization.departments.fields.name'), key: 'name', minWidth: 120, ellipsis: true },
  { title: t('organization.departments.fields.code'), key: 'code', width: 120, ellipsis: true },
  { title: t('organization.departments.fields.status'), key: 'status', width: 80, render: row => renderTag(statusLabel(row.status), statusTone(row.status)) },
  { title: t('organization.departments.fields.member_count'), key: 'memberCount', width: 80 },
])

// ── 部门成员：编辑任职、设为主部门、移出各按归属的权限码放出；添加成员还要能读用户（候选来自 users.list） ──
const canAddMember = computed(() => hasPermission('organization.memberships.create') && hasPermission('identity.users.read'))
const canUpdateMember = computed(() => hasPermission('organization.memberships.update'))
const canRemoveMember = computed(() => hasPermission('organization.memberships.delete'))
const canPickPosition = computed(() => hasPermission('organization.positions.read'))

function optionScope(): MembershipOptionScope {
  return { users: hasPermission('identity.users.read'), positions: canPickPosition.value }
}

function memberActions(member: MembershipRow) {
  const buttons = []
  if (canUpdateMember.value) {
    buttons.push(h(XhButton, { size: 'sm', variant: 'ghost', tone: 'brand', onClick: () => openEditMember(member) }, () => t('organization.departments.member_actions.edit')))
    if (!member.isPrimary)
      buttons.push(h(XhButton, { size: 'sm', variant: 'ghost', tone: 'brand', onClick: () => void setPrimary(member) }, () => t('organization.departments.member_actions.set_primary')))
  }
  if (canRemoveMember.value)
    buttons.push(h(XhButton, { size: 'sm', variant: 'ghost', tone: 'danger', onClick: () => confirmRemoveMember(member) }, () => t('organization.departments.member_actions.remove')))
  return h('span', { class: 'inline-flex gap-1' }, buttons)
}

const memberColumns = computed<XDataTableColumn<MembershipRow>[]>(() => [
  { title: t('organization.departments.members.user'), key: 'displayName', minWidth: 120, ellipsis: true },
  { title: t('organization.departments.members.user_name'), key: 'userName', width: 110, ellipsis: true },
  { title: t('organization.departments.members.position'), key: 'positionName', width: 110, ellipsis: true, render: row => row.positionName ?? '—' },
  { title: t('organization.departments.members.employee_number'), key: 'employeeNumber', width: 100, ellipsis: true, render: row => row.employeeNumber ?? '—' },
  {
    title: t('organization.departments.members.primary'),
    key: 'isPrimary',
    width: 80,
    render: row => row.isPrimary ? renderTag(t('common.statuses.yes'), 'info') : h('span', { class: 'text-fg-muted' }, '—'),
  },
  ...(canUpdateMember.value || canRemoveMember.value
    ? [{ title: t('organization.departments.members.actions'), key: 'actions', width: 220, render: (row: MembershipRow) => memberActions(row) }]
    : []),
])

function openAddMember() {
  const row = detail.row.value
  if (row)
    void memberEditor.startCreate(row.id, optionScope())
}

function openEditMember(member: MembershipRow) {
  void memberEditor.startEdit(member, optionScope())
}

const memberModalTitle = computed(() => {
  const editing = memberEditor.editing.value
  return memberEditor.isEdit.value && editing
    ? t('organization.departments.edit_member_title', { name: editing.displayName })
    : t('organization.departments.add_member_title')
})

/** 成员变动后：成员数列随列表刷新，详情与成员页签重取 */
async function afterMemberChange() {
  reload()
  try {
    await detail.refresh()
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.load_failed'))
  }
}

async function onMemberSubmit() {
  if (await memberEditor.submit()) {
    toast.success(t('common.messages.save_success'))
    await afterMemberChange()
  }
}

async function setPrimary(member: MembershipRow) {
  try {
    await setPrimaryMembership(member)
    toast.success(t('organization.departments.primary_set'))
    await afterMemberChange()
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.operation_failed'))
  }
}

function confirmRemoveMember(member: MembershipRow) {
  void confirmDanger({
    title: t('organization.departments.member_actions.remove'),
    content: t('organization.departments.confirm_remove_member', { name: member.displayName }),
    okText: t('common.actions.confirm'),
    cancelText: t('common.actions.cancel'),
    onOk: async () => {
      try {
        await removeMembership(member.id)
        toast.success(t('organization.departments.member_removed'))
        await afterMemberChange()
      }
      catch (error) {
        toast.danger((error as Error).message || t('common.messages.operation_failed'))
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
    <XhDialogRoot v-model:open="detail.open.value">
      <XhDialogContent class="xh-mgmt-detail-modal" style="--xh-dialog-max-w: 720px">
        <XhDialogHeader>
          <!-- 标题取树里的这一行，弹窗打开期间一直在 -->
          <XhDialogTitle>
            <div v-if="detailRow" class="det-hd-entity">
              <div class="det-hd-ico">
                <Icon icon="tabler:building" width="22" height="22" />
              </div>
              <div class="min-w-0">
                <div class="det-hd-name">
                  {{ detailRow.name }}
                </div>
                <div class="det-hd-sub">
                  {{ detailRow.code }}
                </div>
              </div>
            </div>
          </XhDialogTitle>
        </XhDialogHeader>
        <XhDialogCloseTrigger />
        <XhDialogBody class="xh-overlay-body">
          <div v-if="detail.loading.value" class="modal-loading">
            {{ t('common.statuses.loading') }}
          </div>
          <!-- 面板内容各不相同，标签与面板手摆而不喂 collection -->
          <XhTabsRoot v-else-if="detail.detail.value" default-value="overview" variant="line">
            <XhTabsList>
              <XhTabsTrigger value="overview">
                {{ t('organization.departments.tabs.overview') }}
              </XhTabsTrigger>
              <XhTabsTrigger value="children">
                {{ t('organization.departments.tabs.children', { count: childRows.length }) }}
              </XhTabsTrigger>
              <XhTabsTrigger v-if="canReadMembers()" value="members">
                {{ t('organization.departments.tabs.members', { count: detail.detail.value.memberCount }) }}
              </XhTabsTrigger>
              <XhTabsIndicator />
            </XhTabsList>
            <XhTabsContent value="overview">
              <XhDescriptionsRoot :columns="2" variant="outline" size="sm">
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('organization.departments.fields.code') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>{{ detail.detail.value.department.code }}</XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('organization.departments.fields.parent') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>{{ detail.detail.value.ancestors[0]?.name ?? '-' }}</XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('organization.departments.fields.leader') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>{{ formatNullable(detail.detail.value.leaderUserName) }}</XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('organization.departments.fields.status') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>
                    <XhTagRoot variant="subtle" size="sm" :tone="statusTone(detail.detail.value.department.status)">
                      <XhTagLabel>{{ statusLabel(detail.detail.value.department.status) }}</XhTagLabel>
                    </XhTagRoot>
                  </XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('organization.departments.fields.order') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>{{ detail.detail.value.department.order }}</XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('organization.departments.fields.created_at') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>{{ formatDate(detail.detail.value.department.createdAt) }}</XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('organization.departments.fields.member_count') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>{{ detail.detail.value.memberCount }}</XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem>
                  <XhDescriptionsLabel>{{ t('organization.departments.fields.child_count') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>{{ detail.detail.value.childCount }}</XhDescriptionsValue>
                </XhDescriptionsItem>
                <!-- 跨列项排在末尾：边框按子节点奇偶画竖线，跨列项之后的项奇偶会与实际列位错开 -->
                <XhDescriptionsItem style="grid-column: span 2">
                  <XhDescriptionsLabel>{{ t('organization.departments.fields.path') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>{{ detail.path.value }}</XhDescriptionsValue>
                </XhDescriptionsItem>
                <XhDescriptionsItem style="grid-column: span 2">
                  <XhDescriptionsLabel>{{ t('organization.departments.fields.description') }}</XhDescriptionsLabel>
                  <XhDescriptionsValue>{{ formatNullable(detail.detail.value.department.description) }}</XhDescriptionsValue>
                </XhDescriptionsItem>
              </XhDescriptionsRoot>
            </XhTabsContent>
            <XhTabsContent value="children">
              <div class="xh-detail-table-wrap">
                <XDataTable
                  v-if="childRows.length"
                  :columns="childColumns"
                  :data="childRows"
                  row-key="id"
                />
                <XhEmptyStateRoot v-else size="sm" class="py-8">
                  <XhEmptyStateIndicator>
                    <Icon icon="lucide:inbox" width="24" />
                  </XhEmptyStateIndicator>
                  <XhEmptyStateTitle>{{ t('common.no_data') }}</XhEmptyStateTitle>
                  <XhEmptyStateDescription>{{ t('organization.departments.empty_children') }}</XhEmptyStateDescription>
                </XhEmptyStateRoot>
              </div>
            </XhTabsContent>
            <XhTabsContent v-if="canReadMembers()" value="members">
              <div v-if="canAddMember" class="member-toolbar">
                <XhButton size="sm" variant="subtle" tone="brand" @click="openAddMember">
                  {{ t('organization.departments.member_actions.add') }}
                </XhButton>
              </div>
              <div class="xh-detail-table-wrap">
                <XDataTable
                  v-if="detail.members.value.length"
                  :columns="memberColumns"
                  :data="detail.members.value"
                  row-key="id"
                />
                <XhEmptyStateRoot v-else size="sm" class="py-8">
                  <XhEmptyStateIndicator>
                    <Icon icon="lucide:inbox" width="24" />
                  </XhEmptyStateIndicator>
                  <XhEmptyStateTitle>{{ t('common.no_data') }}</XhEmptyStateTitle>
                  <XhEmptyStateDescription>{{ t('organization.departments.empty_members') }}</XhEmptyStateDescription>
                </XhEmptyStateRoot>
              </div>
            </XhTabsContent>
          </XhTabsRoot>
          <XhEmptyStateRoot v-else class="py-12">
            <XhEmptyStateIndicator>
              <Icon icon="lucide:inbox" width="28" />
            </XhEmptyStateIndicator>
            <XhEmptyStateTitle>{{ t('common.no_data') }}</XhEmptyStateTitle>
            <XhEmptyStateDescription>{{ detail.error.value || t('organization.departments.detail_not_found') }}</XhEmptyStateDescription>
          </XhEmptyStateRoot>
        </XhDialogBody>
        <XhDialogFooter>
          <XhButton variant="subtle" size="sm" @click="detail.open.value = false">
            {{ t('common.actions.close') }}
          </XhButton>
          <XhButton
            v-if="detailRow && hasPermission('organization.departments.update')"
            variant="subtle"
            size="sm"
            tone="brand"
            @click="editFromDetail"
          >
            {{ t('common.actions.edit') }}
          </XhButton>
        </XhDialogFooter>
      </XhDialogContent>
    </XhDialogRoot>

    <XEditModal
      v-model:show="editor.open.value"
      :title="modalTitle"
      :loading="editor.submitting.value"
      :form-id="editFormId"
    >
      <XhFormRoot :id="editFormId" class="xh-edit-form-grid" data-testid="department-form" @submit="onSubmit">
        <XhFieldRoot :invalid="!!editor.fieldError('name')" required>
          <XhFieldLabel>{{ t('organization.departments.fields.name') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.name" name="name" clearable />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('name')">
            {{ editor.fieldError('name') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('code')" :required="!editor.isEdit.value">
          <XhFieldLabel>{{ t('organization.departments.fields.code') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.code" name="code" clearable autocomplete="off" :disabled="editor.isEdit.value" :placeholder="t('organization.departments.code_placeholder')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('code')">
            {{ editor.fieldError('code') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('parentId')">
          <XhFieldLabel>{{ t('organization.departments.fields.parent') }}</XhFieldLabel>
          <XhFieldControl>
            <XTreeSelect v-model:value="editor.form.parentId" name="parentId" :options="editor.parentOptions.value" clearable :placeholder="t('organization.departments.parent_placeholder')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('parentId')">
            {{ editor.fieldError('parentId') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot v-if="canPickLeader" :invalid="!!editor.fieldError('leaderUserId')">
          <XhFieldLabel>{{ t('organization.departments.fields.leader') }}</XhFieldLabel>
          <XhFieldControl>
            <XCombobox
              v-model:value="editor.form.leaderUserId"
              name="leaderUserId"
              remote
              :options="editor.leaderOptions.value"
              :loading="editor.leaderLoading.value"
              clearable
              :placeholder="t('organization.departments.leader_placeholder')"
              @search="onSearchLeaders"
            />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('leaderUserId')">
            {{ editor.fieldError('leaderUserId') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('order')">
          <XhFieldLabel>{{ t('organization.departments.fields.order') }}</XhFieldLabel>
          <XhFieldControl>
            <XNumberInput v-model:value="editor.form.order" name="order" :min="0" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('order')">
            {{ editor.fieldError('order') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot v-if="editor.isEdit.value">
          <XhFieldLabel>{{ t('organization.departments.fields.status') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect v-model:value="editor.form.status" name="status" :options="statusOptions" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot class="xh-span-2" :invalid="!!editor.fieldError('description')">
          <XhFieldLabel>{{ t('organization.departments.fields.description') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.description" name="description" type="textarea" :rows="3" clearable />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('description')">
            {{ editor.fieldError('description') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <p v-if="editor.submitError.value" class="xh-span-2 text-caption text-fg-danger" role="alert">
          {{ editor.submitError.value }}
        </p>
      </XhFormRoot>
    </XEditModal>

    <XEditModal
      v-model:show="memberEditor.open.value"
      :title="memberModalTitle"
      :loading="memberEditor.submitting.value"
      :save-disabled="!memberEditor.isEdit.value && !memberEditor.form.userId"
      :form-id="memberFormId"
    >
      <XhFormRoot :id="memberFormId" class="xh-edit-form-grid" data-testid="membership-form" @submit="onMemberSubmit">
        <XhFieldRoot v-if="!memberEditor.isEdit.value" class="xh-span-2" :invalid="!!memberEditor.fieldError('userId')" required>
          <XhFieldLabel>{{ t('organization.departments.members.user') }}</XhFieldLabel>
          <XhFieldControl>
            <XCombobox
              v-model:value="memberEditor.form.userId"
              name="userId"
              :options="memberEditor.userOptions.value"
              :loading="memberEditor.optionsLoading.value"
              clearable
              :placeholder="t('organization.departments.user_placeholder')"
            />
          </XhFieldControl>
          <XhFieldErrorText v-if="memberEditor.fieldError('userId')">
            {{ memberEditor.fieldError('userId') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot v-if="canPickPosition" :invalid="!!memberEditor.fieldError('positionId')">
          <XhFieldLabel>{{ t('organization.departments.members.position') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect
              v-model:value="memberEditor.form.positionId"
              name="positionId"
              :options="memberEditor.positionOptions.value"
              :loading="memberEditor.optionsLoading.value"
              clearable
              :placeholder="t('organization.departments.position_placeholder')"
            />
          </XhFieldControl>
          <XhFieldErrorText v-if="memberEditor.fieldError('positionId')">
            {{ memberEditor.fieldError('positionId') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!memberEditor.fieldError('employeeNumber')">
          <XhFieldLabel>{{ t('organization.departments.members.employee_number') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="memberEditor.form.employeeNumber" name="employeeNumber" clearable autocomplete="off" />
          </XhFieldControl>
          <XhFieldErrorText v-if="memberEditor.fieldError('employeeNumber')">
            {{ memberEditor.fieldError('employeeNumber') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!memberEditor.fieldError('jobGrade')">
          <XhFieldLabel>{{ t('organization.departments.members.job_grade') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="memberEditor.form.jobGrade" name="jobGrade" clearable autocomplete="off" />
          </XhFieldControl>
          <XhFieldErrorText v-if="memberEditor.fieldError('jobGrade')">
            {{ memberEditor.fieldError('jobGrade') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!memberEditor.fieldError('hiredAt')">
          <XhFieldLabel>{{ t('organization.departments.members.hired_at') }}</XhFieldLabel>
          <XhFieldControl>
            <XDatePicker v-model:value="memberEditor.form.hiredAt" name="hiredAt" clearable />
          </XhFieldControl>
          <XhFieldErrorText v-if="memberEditor.fieldError('hiredAt')">
            {{ memberEditor.fieldError('hiredAt') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!memberEditor.fieldError('order')">
          <XhFieldLabel>{{ t('organization.departments.members.order') }}</XhFieldLabel>
          <XhFieldControl>
            <XNumberInput v-model:value="memberEditor.form.order" name="order" :min="0" />
          </XhFieldControl>
          <XhFieldErrorText v-if="memberEditor.fieldError('order')">
            {{ memberEditor.fieldError('order') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot v-if="!memberEditor.isEdit.value">
          <XhFieldLabel>{{ t('organization.departments.members.primary') }}</XhFieldLabel>
          <XhFieldControl>
            <XhSwitch v-model:checked="memberEditor.form.isPrimary" name="isPrimary" />
          </XhFieldControl>
        </XhFieldRoot>
        <p v-if="memberEditor.submitError.value" class="xh-span-2 text-caption text-fg-danger" role="alert">
          {{ memberEditor.submitError.value }}
        </p>
      </XhFormRoot>
    </XEditModal>
  </SchemaPage>
</template>

<style scoped>
.xh-detail-table-wrap {
  width: 100%;
}

.member-toolbar {
  display: flex;
  justify-content: flex-end;
  margin-bottom: var(--xh-space-2);
}
</style>
