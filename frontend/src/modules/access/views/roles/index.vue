<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script lang="ts" setup>
import type { PermissionWindowDraft } from './grant-window'
import type { ChangeOutcome, DataScopeValue, MemberCandidate, RoleMember, RolePermissionEntry, RoleRow, RoleStatus } from './use-roles'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaPageInstance, XDataTableColumn } from '~/components'
import type { PermissionAction } from '~/contracts'
import type { SelectOption } from '~/types'
import { XhButton, XhButtonIndicator, XhButtonLabel, XhDescriptionsItem, XhDescriptionsLabel, XhDescriptionsRoot, XhDescriptionsValue, XhDrawerBody, XhDrawerCloseTrigger, XhDrawerContent, XhDrawerFooter, XhDrawerHeader, XhDrawerRoot, XhDrawerTitle, XhEmptyStateDescription, XhEmptyStateIndicator, XhEmptyStateRoot, XhEmptyStateTitle, XhFieldControl, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFormRoot, XhSpinner, XhTabsContent, XhTabsIndicator, XhTabsList, XhTabsRoot, XhTabsTrigger, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, reactive, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { deleteConfirmText, Icon, renderRowActionMenu, renderTag, SchemaPage, statusConfirmText, XDataTable, XEditModal, XGrantTransfer, XInput, XNumberInput, XPermissionTransfer, XSelect } from '~/components'
import { toast } from '~/composables'
import { useAuthStore } from '~/stores'
import { formatDate } from '~/utils'
import DataScopeEditor from './components/DataScopeEditor.vue'
import GrantConditionsDrawer from './components/GrantConditionsDrawer.vue'
import GrantWindowDialog from './components/GrantWindowDialog.vue'
import GrantWindowFields from './components/GrantWindowFields.vue'
import RoleParentsDialog from './components/RoleParentsDialog.vue'
import { emptyGrantWindow, grantWindowOf } from './grant-window'
import { ROLE_STATUSES, rolesResource, useRoleDataScopeEditor, useRoleDetail, useRoleEditor, useRoleMemberEditor, useRolePermissionEditor } from './use-roles'

defineOptions({ name: 'AccessRolesView' })

const { t } = useI18n()
const auth = useAuthStore()
/** 平台角色的数据范围固定为全部、平台没有部门：数据范围只在租户侧出现 */
const isTenantCaller = computed(() => auth.tenantId !== null)
const resource = rolesResource()
const editor = useRoleEditor()
const detailView = useRoleDetail()
const permissionEditor = useRolePermissionEditor()
const memberEditor = useRoleMemberEditor(() => !isTenantCaller.value)
const scopeEditor = useRoleDataScopeEditor()

/** 编辑弹窗的保存钮靠这个 id 关联到表单，点它才会走整表提交 */
const editFormId = useId()
const schemaPageRef = ref<SchemaPageInstance | null>(null)
/** 设置上级弹窗 */
const parentsVisible = ref(false)
const parentsRole = ref<RoleRow | null>(null)
/** 授权条件抽屉 */
const conditionsVisible = ref(false)
const conditionsRole = ref<RoleRow | null>(null)

function reload() {
  void schemaPageRef.value?.reload()
}

// 枚举值是契约字面量，文案键静态写出以便 i18n 门禁核对
function statusLabel(status: RoleStatus): string {
  return status === 'Disabled'
    ? t('access.roles.status.disabled')
    : t('access.roles.status.active')
}

function dataScopeLabel(scope: DataScopeValue): string {
  switch (scope) {
    case 'All': return t('access.roles.data_scope.all')
    case 'DepartmentAndChildren': return t('access.roles.data_scope.department_and_children')
    case 'DepartmentOnly': return t('access.roles.data_scope.department_only')
    case 'Custom': return t('access.roles.data_scope.custom')
    default: return t('access.roles.data_scope.self_only')
  }
}

function memberStatusLabel(status: RoleMember['status']): string {
  if (status === 'Locked')
    return t('access.roles.member_status.locked')
  return status === 'Disabled'
    ? t('access.roles.member_status.disabled')
    : t('access.roles.member_status.active')
}

function ownerLabel(owner: string): string {
  return owner === 'platform'
    ? t('access.roles.owner_platform')
    : owner
}

function formatBoolean(value: boolean): string {
  return value
    ? t('common.statuses.yes')
    : t('common.statuses.no')
}

function formatNullable(value: unknown): string {
  return value === null || value === undefined || value === ''
    ? '-'
    : String(value)
}

/** 继承方式：1 级为直接继承，更深为经其他上级的间接继承 */
function inheritMode(depth: number): string {
  return depth <= 1
    ? t('access.roles.inherit_direct')
    : t('access.roles.inherit_indirect', { depth })
}

/** 「{from} 至 {to}」：两端都没有即不限 */
function validityRange(from?: null | string, to?: null | string): string {
  if (!from && !to)
    return t('access.roles.validity_unlimited')
  return t('access.roles.validity_range', {
    from: from
      ? formatDate(from)
      : '-',
    to: to
      ? formatDate(to)
      : '-',
  })
}

const statusOptions = computed<SelectOption[]>(() => ROLE_STATUSES.map(status => ({
  label: statusLabel(status),
  value: status,
})))
const dataScopeOptions = computed<SelectOption[]>(() => ([
  'All',
  'DepartmentAndChildren',
  'DepartmentOnly',
  'SelfOnly',
  'Custom',
] as const).map(scope => ({
  label: dataScopeLabel(scope),
  value: scope,
})))

// ── 字段单一事实源（列 + 搜索；仅搜索字段 visible:false；order 控顺序；可排序的只放后端白名单里的） ──
// SchemaPage 的行类型在框架边界放宽（Record）：回调里按本页的行类型取用
const asRow = (row: object) => row as RoleRow

const fields = computed<ListFieldSchema[]>(() => [
  {
    key: 'keyword',
    title: t('access.roles.search'),
    dataType: 'string',
    visible: false,
    searchable: true,
    searchPlaceholder: t('access.roles.keyword_placeholder'),
    order: 0,
  },
  {
    key: 'name',
    title: t('access.roles.fields.name'),
    dataType: 'string',
    sortable: true,
    exportable: true,
    minWidth: 150,
    order: 1,
  },
  {
    key: 'code',
    title: t('access.roles.fields.code'),
    dataType: 'string',
    sortable: true,
    exportable: true,
    minWidth: 150,
    order: 2,
  },
  {
    key: 'description',
    title: t('access.roles.fields.description'),
    dataType: 'text',
    exportable: true,
    ellipsis: true,
    minWidth: 220,
    order: 3,
  },
  {
    key: 'dataScope',
    title: t('access.roles.fields.data_scope'),
    dataType: 'enum',
    exportable: true,
    options: dataScopeOptions.value,
    render: row => dataScopeLabel(asRow(row).dataScope),
    minWidth: 130,
    order: 4,
  },
  {
    key: 'isBuiltIn',
    title: t('access.roles.fields.built_in'),
    dataType: 'boolean',
    exportable: true,
    render: row => renderTag(formatBoolean(asRow(row).isBuiltIn), asRow(row).isBuiltIn
      ? 'warning'
      : 'neutral'),
    width: 82,
    order: 5,
  },
  {
    key: 'order',
    title: t('access.roles.fields.order'),
    dataType: 'number',
    sortable: true,
    exportable: true,
    width: 80,
    order: 6,
  },
  {
    key: 'status',
    title: t('access.roles.fields.status'),
    dataType: 'enum',
    searchable: true,
    exportable: true,
    options: statusOptions.value,
    render: row => renderTag(statusLabel(asRow(row).status), asRow(row).status === 'Active'
      ? 'success'
      : 'danger'),
    width: 90,
    order: 7,
  },
  {
    key: 'createdAt',
    title: t('access.roles.fields.created_at'),
    dataType: 'datetime',
    sortable: true,
    exportable: true,
    minWidth: 170,
    order: 8,
  },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'access.roles',
  pageName: t('access.roles.title'),
  exportPermission: 'access.roles.read',
  batchRemovable: true,
  removePermission: 'access.roles.delete',
  statusPermission: 'access.roles.update',
  rowKey: 'id',
  fields: fields.value,
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    {
      key: 'create',
      title: t('access.roles.actions.create'),
      scope: 'page',
      tone: 'brand',
      icon: 'lucide:plus',
      permission: 'access.roles.create',
    },
    {
      key: 'view',
      title: t('common.actions.view'),
      scope: 'row',
      icon: 'lucide:eye',
    },
    {
      key: 'edit',
      title: t('common.actions.edit'),
      scope: 'row',
      icon: 'lucide:pencil',
      permission: 'access.roles.update',
    },
    // 权限分配要读权限目录（套餐收窄后的本侧目录）
    {
      key: 'assignPermission',
      title: t('access.roles.actions.assign_permission'),
      scope: 'row',
      icon: 'lucide:key-round',
      visible: () => auth.can('access.permissions.read'),
      permission: 'access.roles.grant',
    },
    {
      key: 'members',
      title: t('access.roles.actions.members'),
      scope: 'row',
      icon: 'lucide:users',
      permission: 'access.roles.assign',
    },
    // 内建角色不挂条件、不参与继承
    {
      key: 'conditions',
      title: t('access.conditions.open'),
      scope: 'row',
      icon: 'lucide:filter',
      visible: row => !asRow(row).isBuiltIn,
      permission: 'access.permission-conditions.read',
    },
    {
      key: 'parents',
      title: t('access.roles.actions.parents'),
      scope: 'row',
      icon: 'lucide:git-fork',
      visible: row => !asRow(row).isBuiltIn,
      permission: 'access.roles.set-parents',
    },
    {
      key: 'assignDataScope',
      title: t('access.roles.actions.data_scope'),
      scope: 'row',
      icon: 'lucide:building-2',
      visible: () => isTenantCaller.value,
      permission: 'access.roles.update',
    },
    {
      key: 'toggle',
      title: t('access.roles.actions.toggle'),
      scope: 'row',
      icon: 'lucide:power',
      confirm: true,
      confirmText: row => statusConfirmText(t, asRow(row).status === 'Active', asRow(row).name),
      permission: 'access.roles.update',
    },
    // 内建角色后端不许删
    {
      key: 'delete',
      title: t('common.actions.delete'),
      scope: 'row',
      icon: 'lucide:trash-2',
      tone: 'danger',
      confirm: true,
      confirmText: row => deleteConfirmText(t, asRow(row).name),
      visible: row => !asRow(row).isBuiltIn,
      permission: 'access.roles.delete',
    },
  ],
}))

// ── 行 / 页面操作分发 ──
function onAction(payload: SchemaActionPayload) {
  const row = payload.row
    ? asRow(payload.row)
    : undefined
  switch (payload.key) {
    case 'create':
      editor.startCreate()
      break
    case 'view':
      if (row)
        void run(detailView.start(row, auth.can('organization.departments.read')), t('access.roles.detail_load_failed'))
      break
    case 'edit':
      if (row)
        editor.startEdit(row)
      break
    case 'assignPermission':
      if (row)
        void run(permissionEditor.start(row), t('access.roles.perm_load_failed'))
      break
    case 'members':
      if (row)
        void run(memberEditor.start(row, auth.can('identity.users.read')), t('access.roles.members_load_failed'))
      break
    case 'conditions':
      if (row) {
        conditionsRole.value = row
        conditionsVisible.value = true
      }
      break
    case 'parents':
      if (row) {
        parentsRole.value = row
        parentsVisible.value = true
      }
      break
    case 'assignDataScope':
      if (row)
        void run(scopeEditor.start(row, auth.can('organization.departments.read')), t('access.roles.scope_editor.load_failed'))
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

async function run(task: Promise<unknown>, fallback: string) {
  try {
    await task
  }
  catch (error) {
    toast.danger((error as Error).message || fallback)
  }
}

const modalTitle = computed(() => (editor.isEdit.value
  ? t('access.roles.edit_title')
  : t('access.roles.create_title')))

async function onSubmit() {
  if (await editor.submit()) {
    toast.success(t('common.messages.save_success'))
    reload()
  }
}

async function removeRow(row: RoleRow) {
  try {
    await resource.remove?.(row.id)
    toast.success(t('common.messages.delete_success'))
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.delete_failed'))
  }
}

async function toggleStatus(row: RoleRow) {
  try {
    await resource.updateStatus?.(row.id, row.status !== 'Active')
    toast.success(t('common.messages.status_updated'))
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.status_failed'))
  }
}

/** 授予 / 指派的结果：逐项失败单独提示原因，其余按条数报成功；转为审批的另起一条提示 */
function reportOutcome(outcome: ChangeOutcome | null, saved: (outcome: ChangeOutcome) => string, noChange: string) {
  if (!outcome) {
    toast.info(noChange)
    return
  }
  if (outcome.failures.length > 0) {
    toast.warning(t('access.roles.partial_failed', {
      failures: outcome.failures.join('; '),
    }))
  }
  else if (outcome.added + outcome.denied + outcome.removed > 0 || outcome.submitted.length === 0) {
    toast.success(saved(outcome))
  }
  if (outcome.submitted.length > 0)
    toast.info(outcome.submitted.join('; '))
}

// ── 权限分配 ──
async function savePermissions() {
  try {
    reportOutcome(await permissionEditor.save(), outcome => t('access.roles.perm_saved', { grant: outcome.added, deny: outcome.denied, revoke: outcome.removed }), t('access.roles.perm_no_change'))
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.save_failed'))
  }
}

/** 有未保存的穿梭框改动时不放出逐条改：改完会重读现状、草稿随之重置 */
const permissionEntryColumns = computed<XDataTableColumn<RolePermissionEntry>[]>(() => [
  {
    key: 'displayName',
    title: t('access.roles.th.permission'),
    minWidth: 160,
    ellipsis: true,
    render: entry => entry.displayName,
  },
  {
    key: 'code',
    title: t('access.roles.th.code'),
    minWidth: 180,
    ellipsis: true,
    render: entry => entry.code,
  },
  {
    key: 'action',
    title: t('access.roles.th.action'),
    width: 80,
    render: entry => entry.action === 'Deny'
      ? renderTag(t('access.roles.action_deny'), 'danger')
      : renderTag(t('access.roles.action_grant'), 'success'),
  },
  {
    key: 'validity',
    title: t('access.roles.th.validity'),
    minWidth: 220,
    render: entry => validityRange(entry.effectiveAt, entry.expiresAt),
  },
  {
    key: 'reason',
    title: t('access.roles.th.reason'),
    minWidth: 120,
    ellipsis: true,
    render: entry => formatNullable(entry.reason),
  },
  {
    key: 'actions',
    title: t('component.schema_page.actions_column'),
    width: 90,
    render: entry => renderRowActionMenu([
      {
        key: 'window',
        title: t('access.roles.validity_edit'),
        icon: 'lucide:calendar-clock',
        disabled: permissionEditor.dirty.value || permissionEditor.saving.value,
      },
    ], t('component.schema_page.more'), () => openEntryWindow(entry)),
  },
])

// ── 角色成员 ──
/** 读不到用户目录时候选里只有现有成员：只能移出 */
const canListUsers = computed(() => auth.can('identity.users.read'))
const memberGroups = computed(() => [
  { key: 'members', name: '', items: memberEditor.items.value },
])

/** 外部成员、运维支持成员的标记；运维支持成员注册在别处，先看它 */
function memberMark(item: { isExternalMember: boolean, isSupportMember: boolean }): null | string {
  if (item.isSupportMember)
    return t('access.roles.member_support')
  return item.isExternalMember
    ? t('access.roles.member_external')
    : null
}

function memberDescription(item: MemberCandidate): string {
  const mark = memberMark(item)
  return mark
    ? `@${item.userName} · ${mark}`
    : `@${item.userName}`
}

async function saveMembers() {
  try {
    reportOutcome(await memberEditor.save(), outcome => t('access.roles.members_saved', { grant: outcome.added, revoke: outcome.removed }), t('access.roles.members_no_change'))
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.save_failed'))
  }
}

/** 有未保存的成员改动时不放出逐个改：改完会重读成员、草稿随之重置 */
const memberColumns = computed<XDataTableColumn<RoleMember>[]>(() => [
  {
    key: 'displayName',
    title: t('access.roles.th.user'),
    minWidth: 140,
    ellipsis: true,
    render: member => memberMark(member)
      ? `${member.displayName} · ${memberMark(member)}`
      : member.displayName,
  },
  {
    key: 'userName',
    title: t('access.roles.th.user_name'),
    minWidth: 120,
    ellipsis: true,
    render: member => member.userName,
  },
  {
    key: 'status',
    title: t('access.roles.th.status'),
    width: 80,
    render: member => memberStatusLabel(member.status),
  },
  {
    key: 'validity',
    title: t('access.roles.th.validity'),
    minWidth: 220,
    render: member => validityRange(member.effectiveAt, member.expiresAt),
  },
  {
    key: 'actions',
    title: t('component.schema_page.actions_column'),
    width: 90,
    render: member => renderRowActionMenu([
      {
        key: 'window',
        title: t('access.roles.validity_edit'),
        icon: 'lucide:calendar-clock',
        disabled: memberEditor.dirty.value || memberEditor.saving.value,
      },
    ], t('component.schema_page.more'), () => openMemberWindow(member)),
  },
])

// ── 逐条改有效期（权限的授予与拒绝、成员的指派共用一个弹窗） ──
type WindowTarget
  = | { kind: 'permission', code: string, action: PermissionAction }
    | { kind: 'member', userId: string }

const windowDialog = reactive({
  show: false,
  title: '',
  withReason: false,
  initial: emptyGrantWindow(),
})
const windowTarget = ref<null | WindowTarget>(null)
const windowSaving = computed(() => permissionEditor.saving.value || memberEditor.saving.value)

function openEntryWindow(entry: RolePermissionEntry) {
  windowTarget.value = { kind: 'permission', code: entry.code, action: entry.action }
  windowDialog.title = t('access.roles.validity_edit_title', { name: entry.displayName })
  windowDialog.withReason = true
  windowDialog.initial = grantWindowOf(entry)
  windowDialog.show = true
}

function openMemberWindow(member: RoleMember) {
  windowTarget.value = { kind: 'member', userId: member.userId }
  windowDialog.title = t('access.roles.validity_edit_title', { name: member.displayName })
  windowDialog.withReason = false
  windowDialog.initial = grantWindowOf(member)
  windowDialog.show = true
}

async function submitWindow(next: PermissionWindowDraft) {
  const target = windowTarget.value
  if (!target)
    return
  try {
    if (target.kind === 'permission')
      await permissionEditor.updateEntry(target.code, target.action, next)
    else
      await memberEditor.updateMember(target.userId, next)
    windowDialog.show = false
    toast.success(t('access.roles.validity_saved'))
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.save_failed'))
  }
}

// ── 数据范围 ──
async function saveScope() {
  if (!scopeEditor.complete.value) {
    toast.warning(t('access.roles.scope_editor.custom_required'))
    return
  }
  try {
    if (await scopeEditor.save()) {
      toast.success(t('access.roles.scope_editor.saved'))
      reload()
    }
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.save_failed'))
  }
}
</script>

<template>
  <SchemaPage
    ref="schemaPageRef"
    :schema="schema"
    @action="onAction"
  >
    <!-- 详情 -->
    <XhDrawerRoot v-model:open="detailView.open.value" side="right">
      <XhDrawerContent class="w-5xl">
        <XhDrawerTitle>{{ t('access.roles.detail_title') }}</XhDrawerTitle>
        <XhDrawerCloseTrigger />
        <div class="xh-loading-stage" :class="{ 'is-loading': detailView.loading.value }">
          <div class="xh-loading-stage__veil">
            <XhSpinner />
          </div>
          <XhEmptyStateRoot v-if="!detailView.loading.value && !detailView.detail.value" class="role-empty">
            <XhEmptyStateIndicator>
              <Icon icon="lucide:inbox" width="28" />
            </XhEmptyStateIndicator>
            <XhEmptyStateTitle>{{ t('common.empty') }}</XhEmptyStateTitle>
            <XhEmptyStateDescription>{{ t('access.roles.detail_empty') }}</XhEmptyStateDescription>
          </XhEmptyStateRoot>
          <div v-else-if="detailView.detail.value" class="xh-scroll-area app-fade max-h-[calc(100vh-(--spacing(34)))]">
            <!-- 面板内容各不相同，标签与面板手摆而不喂 collection -->
            <XhTabsRoot default-value="overview" variant="line">
              <XhTabsList>
                <XhTabsTrigger value="overview">
                  {{ t('access.roles.tabs.overview') }}
                </XhTabsTrigger>
                <XhTabsTrigger value="permissions">
                  {{ t('access.roles.tabs.permissions', { count: detailView.permissions.value.length }) }}
                </XhTabsTrigger>
                <XhTabsTrigger value="inheritedPermissions">
                  {{ t('access.roles.tabs.inherited_permissions', { count: detailView.inherited.value.length }) }}
                </XhTabsTrigger>
                <XhTabsTrigger v-if="detailView.detail.value.role.dataScope === 'Custom'" value="dataScopes">
                  {{ t('access.roles.tabs.data_scopes', { count: detailView.detail.value.customDepartments.length }) }}
                </XhTabsTrigger>
                <XhTabsTrigger value="members">
                  {{ t('access.roles.tabs.members', { count: detailView.members.value.length }) }}
                </XhTabsTrigger>
                <XhTabsTrigger value="ancestors">
                  {{ t('access.roles.tabs.ancestors', { count: detailView.ancestors.value.length }) }}
                </XhTabsTrigger>
                <XhTabsTrigger value="descendants">
                  {{ t('access.roles.tabs.descendants', { count: detailView.descendants.value.length }) }}
                </XhTabsTrigger>
                <XhTabsIndicator />
              </XhTabsList>
              <XhTabsContent value="overview">
                <XhDescriptionsRoot :columns="2" variant="outline" size="sm">
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('access.roles.fields.name') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>{{ detailView.detail.value.role.name }}</XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('access.roles.fields.code') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>{{ detailView.detail.value.role.code }}</XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('access.roles.fields.owner') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>{{ ownerLabel(detailView.detail.value.role.ownerTenantId) }}</XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('access.roles.fields.data_scope') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>{{ dataScopeLabel(detailView.detail.value.role.dataScope) }}</XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('access.roles.fields.built_in') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>{{ formatBoolean(detailView.detail.value.role.isBuiltIn) }}</XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('access.roles.fields.status') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>{{ statusLabel(detailView.detail.value.role.status) }}</XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('access.roles.fields.member_count') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>{{ detailView.detail.value.memberCount }}</XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('access.roles.fields.order') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>{{ detailView.detail.value.role.order }}</XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('access.roles.fields.created_at') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>{{ formatDate(detailView.detail.value.role.createdAt) }}</XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('access.roles.fields.description') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>{{ formatNullable(detailView.detail.value.role.description) }}</XhDescriptionsValue>
                  </XhDescriptionsItem>
                </XhDescriptionsRoot>
              </XhTabsContent>
              <XhTabsContent value="permissions">
                <table v-if="detailView.permissions.value.length" class="xh-detail-table">
                  <thead>
                    <tr>
                      <th>{{ t('access.roles.th.permission') }}</th>
                      <th>{{ t('access.roles.th.code') }}</th>
                      <th>{{ t('access.roles.th.action') }}</th>
                      <th>{{ t('access.roles.th.validity') }}</th>
                      <th>{{ t('access.roles.th.reason') }}</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="item in detailView.permissions.value" :key="`${item.action}-${item.code}`">
                      <td>{{ item.displayName }}</td>
                      <td>{{ item.code }}</td>
                      <td>{{ item.action === 'Deny' ? t('access.roles.action_deny') : t('access.roles.action_grant') }}</td>
                      <td>{{ validityRange(item.effectiveAt, item.expiresAt) }}</td>
                      <td>{{ formatNullable(item.reason) }}</td>
                    </tr>
                  </tbody>
                </table>
                <XhEmptyStateRoot v-else class="role-tab-empty">
                  <XhEmptyStateIndicator>
                    <Icon icon="lucide:inbox" width="28" />
                  </XhEmptyStateIndicator>
                  <XhEmptyStateTitle>{{ t('common.empty') }}</XhEmptyStateTitle>
                  <XhEmptyStateDescription>{{ t('access.roles.empty_permissions') }}</XhEmptyStateDescription>
                </XhEmptyStateRoot>
              </XhTabsContent>
              <XhTabsContent value="inheritedPermissions">
                <table v-if="detailView.inherited.value.length" class="xh-detail-table">
                  <thead>
                    <tr>
                      <th>{{ t('access.roles.th.permission') }}</th>
                      <th>{{ t('access.roles.th.code') }}</th>
                      <th>{{ t('access.roles.th.action') }}</th>
                      <th>{{ t('access.roles.th.source_role') }}</th>
                      <th>{{ t('access.roles.th.inherit_mode') }}</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="item in detailView.inherited.value" :key="`${item.permission}-${item.sourceRoleId}-${item.action}`">
                      <td>{{ item.displayName }}</td>
                      <td>{{ item.permission }}</td>
                      <td>{{ item.action === 'Deny' ? t('access.roles.action_deny') : t('access.roles.action_grant') }}</td>
                      <td>{{ item.sourceRoleName }}</td>
                      <td>{{ inheritMode(item.depth) }}</td>
                    </tr>
                  </tbody>
                </table>
                <XhEmptyStateRoot v-else class="role-tab-empty">
                  <XhEmptyStateIndicator>
                    <Icon icon="lucide:inbox" width="28" />
                  </XhEmptyStateIndicator>
                  <XhEmptyStateTitle>{{ t('common.empty') }}</XhEmptyStateTitle>
                  <XhEmptyStateDescription>{{ t('access.roles.empty_inherited_permissions') }}</XhEmptyStateDescription>
                </XhEmptyStateRoot>
              </XhTabsContent>
              <XhTabsContent v-if="detailView.detail.value.role.dataScope === 'Custom'" value="dataScopes">
                <table v-if="detailView.detail.value.customDepartments.length" class="xh-detail-table">
                  <thead>
                    <tr>
                      <th>{{ t('access.roles.th.department') }}</th>
                      <th>{{ t('access.roles.th.include_children') }}</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="item in detailView.detail.value.customDepartments" :key="item.departmentId">
                      <td>{{ detailView.departmentName(item.departmentId) }}</td>
                      <td>{{ formatBoolean(item.includeChildren ?? false) }}</td>
                    </tr>
                  </tbody>
                </table>
                <XhEmptyStateRoot v-else class="role-tab-empty">
                  <XhEmptyStateIndicator>
                    <Icon icon="lucide:inbox" width="28" />
                  </XhEmptyStateIndicator>
                  <XhEmptyStateTitle>{{ t('common.empty') }}</XhEmptyStateTitle>
                  <XhEmptyStateDescription>{{ t('access.roles.empty_data_scopes') }}</XhEmptyStateDescription>
                </XhEmptyStateRoot>
              </XhTabsContent>
              <XhTabsContent value="members">
                <table v-if="detailView.members.value.length" class="xh-detail-table">
                  <thead>
                    <tr>
                      <th>{{ t('access.roles.th.user') }}</th>
                      <th>{{ t('access.roles.th.user_name') }}</th>
                      <th>{{ t('access.roles.th.status') }}</th>
                      <th>{{ t('access.roles.th.assigned_at') }}</th>
                      <th>{{ t('access.roles.th.validity') }}</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="item in detailView.members.value" :key="item.userId">
                      <td>{{ memberMark(item) ? `${item.displayName} · ${memberMark(item)}` : item.displayName }}</td>
                      <td>{{ item.userName }}</td>
                      <td>{{ memberStatusLabel(item.status) }}</td>
                      <td>{{ formatDate(item.assignedAt) }}</td>
                      <td>{{ validityRange(item.effectiveAt, item.expiresAt) }}</td>
                    </tr>
                  </tbody>
                </table>
                <XhEmptyStateRoot v-else class="role-tab-empty">
                  <XhEmptyStateIndicator>
                    <Icon icon="lucide:inbox" width="28" />
                  </XhEmptyStateIndicator>
                  <XhEmptyStateTitle>{{ t('common.empty') }}</XhEmptyStateTitle>
                  <XhEmptyStateDescription>{{ t('access.roles.empty_members') }}</XhEmptyStateDescription>
                </XhEmptyStateRoot>
              </XhTabsContent>
              <XhTabsContent value="ancestors">
                <table v-if="detailView.ancestors.value.length" class="xh-detail-table">
                  <thead>
                    <tr>
                      <th>{{ t('access.roles.th.parent_role') }}</th>
                      <th>{{ t('access.roles.th.role_code') }}</th>
                      <th>{{ t('access.roles.th.inherit_mode') }}</th>
                      <th>{{ t('access.roles.th.status') }}</th>
                      <th>{{ t('access.roles.th.effective') }}</th>
                      <th>{{ t('access.roles.th.path') }}</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="item in detailView.ancestors.value" :key="item.roleId">
                      <td>{{ item.name }}</td>
                      <td>{{ item.code }}</td>
                      <td>{{ inheritMode(item.depth) }}</td>
                      <td>{{ statusLabel(item.status) }}</td>
                      <td>
                        <XhTagRoot v-if="item.isEffective" variant="subtle" size="sm" tone="success">
                          <XhTagLabel>{{ t('access.roles.effective_yes') }}</XhTagLabel>
                        </XhTagRoot>
                        <XhTagRoot v-else variant="subtle" size="sm" tone="neutral" :title="t('access.roles.effective_no_hint')">
                          <XhTagLabel>{{ t('access.roles.effective_no') }}</XhTagLabel>
                        </XhTagRoot>
                      </td>
                      <td>{{ item.path.join(' → ') }}</td>
                    </tr>
                  </tbody>
                </table>
                <XhEmptyStateRoot v-else class="role-tab-empty">
                  <XhEmptyStateIndicator>
                    <Icon icon="lucide:inbox" width="28" />
                  </XhEmptyStateIndicator>
                  <XhEmptyStateTitle>{{ t('common.empty') }}</XhEmptyStateTitle>
                  <XhEmptyStateDescription>{{ t('access.roles.empty_ancestors') }}</XhEmptyStateDescription>
                </XhEmptyStateRoot>
              </XhTabsContent>
              <XhTabsContent value="descendants">
                <table v-if="detailView.descendants.value.length" class="xh-detail-table">
                  <thead>
                    <tr>
                      <th>{{ t('access.roles.th.child_role') }}</th>
                      <th>{{ t('access.roles.th.role_code') }}</th>
                      <th>{{ t('access.roles.th.inherit_mode') }}</th>
                      <th>{{ t('access.roles.th.status') }}</th>
                      <th>{{ t('access.roles.th.effective') }}</th>
                      <th>{{ t('access.roles.th.path') }}</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="item in detailView.descendants.value" :key="item.roleId">
                      <td>{{ item.name }}</td>
                      <td>{{ item.code }}</td>
                      <td>{{ inheritMode(item.depth) }}</td>
                      <td>{{ statusLabel(item.status) }}</td>
                      <td>
                        <XhTagRoot v-if="item.isEffective" variant="subtle" size="sm" tone="success">
                          <XhTagLabel>{{ t('access.roles.effective_yes') }}</XhTagLabel>
                        </XhTagRoot>
                        <XhTagRoot v-else variant="subtle" size="sm" tone="neutral" :title="t('access.roles.effective_no_hint')">
                          <XhTagLabel>{{ t('access.roles.effective_no') }}</XhTagLabel>
                        </XhTagRoot>
                      </td>
                      <td>{{ item.path.join(' → ') }}</td>
                    </tr>
                  </tbody>
                </table>
                <XhEmptyStateRoot v-else class="role-tab-empty">
                  <XhEmptyStateIndicator>
                    <Icon icon="lucide:inbox" width="28" />
                  </XhEmptyStateIndicator>
                  <XhEmptyStateTitle>{{ t('common.empty') }}</XhEmptyStateTitle>
                  <XhEmptyStateDescription>{{ t('access.roles.empty_descendants') }}</XhEmptyStateDescription>
                </XhEmptyStateRoot>
              </XhTabsContent>
            </XhTabsRoot>
          </div>
        </div>
      </XhDrawerContent>
    </XhDrawerRoot>

    <!-- 新建 / 编辑 -->
    <XEditModal
      v-model:show="editor.open.value"
      :title="modalTitle"
      :loading="editor.submitting.value"
      :form-id="editFormId"
    >
      <XhFormRoot :id="editFormId" class="xh-edit-form-grid" data-testid="role-form" @submit="onSubmit">
        <XhFieldRoot :invalid="!!editor.fieldError('name')" required>
          <XhFieldLabel>{{ t('access.roles.fields.name') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.name" name="name" clearable :placeholder="t('access.roles.name_placeholder')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('name')">
            {{ editor.fieldError('name') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('code')" :required="!editor.isEdit.value">
          <XhFieldLabel>{{ t('access.roles.fields.code') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.code" name="code" clearable autocomplete="off" :disabled="editor.isEdit.value" :placeholder="t('access.roles.code_placeholder')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('code')">
            {{ editor.fieldError('code') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('order')">
          <XhFieldLabel>{{ t('access.roles.fields.order') }}</XhFieldLabel>
          <XhFieldControl>
            <XNumberInput v-model:value="editor.form.order" name="order" :min="0" :max="100000" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('order')">
            {{ editor.fieldError('order') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot v-if="editor.isEdit.value" :invalid="!!editor.fieldError('status')">
          <XhFieldLabel>{{ t('access.roles.fields.status') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect v-model:value="editor.form.status" name="status" :options="statusOptions" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('status')">
            {{ editor.fieldError('status') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot class="xh-span-2" :invalid="!!editor.fieldError('description')">
          <XhFieldLabel>{{ t('access.roles.fields.description') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.description" name="description" type="textarea" :rows="3" clearable :placeholder="t('access.roles.description_placeholder')" />
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

    <!-- 权限分配 -->
    <XhDrawerRoot v-model:open="permissionEditor.open.value" side="right">
      <XhDrawerContent class="w-6xl">
        <XhDrawerHeader>
          <XhDrawerTitle>{{ t('access.roles.perm_drawer_title', { name: permissionEditor.role.value?.name ?? '' }) }}</XhDrawerTitle>
        </XhDrawerHeader>
        <XhDrawerCloseTrigger />
        <XhDrawerBody class="xh-overlay-body">
          <XhTabsRoot v-model:value="permissionEditor.tab.value" class="grant-tabs" variant="line">
            <XhTabsList>
              <XhTabsTrigger value="grant">
                {{ t('access.roles.action_grant') }}
              </XhTabsTrigger>
              <XhTabsTrigger value="deny">
                {{ t('access.roles.action_deny') }}
              </XhTabsTrigger>
              <XhTabsTrigger value="validity">
                {{ t('access.roles.tab_validity') }}
              </XhTabsTrigger>
              <XhTabsIndicator />
            </XhTabsList>
            <XhTabsContent value="grant">
              <p v-if="permissionEditor.inherited.value.size > 0" class="drawer-tip">
                {{ t('access.roles.perm_inherit_tip') }}
              </p>
              <GrantWindowFields
                v-model="permissionEditor.windowDraft.value"
                with-reason
                :effective-help="t('access.roles.window.new_permission_help')"
                :disabled="permissionEditor.loading.value || permissionEditor.saving.value"
              />
              <XPermissionTransfer
                :items="permissionEditor.items.value"
                :value="permissionEditor.grantIds.value"
                :loading="permissionEditor.loading.value"
                :disabled="permissionEditor.loading.value || permissionEditor.saving.value"
                :source-title="t('access.roles.perm_available')"
                :target-title="t('access.roles.perm_granted')"
                :search-placeholder="t('access.roles.perm_search')"
                :other-group-label="t('access.roles.perm_group_other')"
                @update:value="next => permissionEditor.transfer('Grant', next)"
              >
                <template #suffix="{ item, side }">
                  <!-- 左栏标出已在拒绝里的：挪过来会把拒绝改成授予 -->
                  <XhTagRoot v-if="side === 'source' && permissionEditor.actions.value.get(String(item.basicId)) === 'Deny'" variant="subtle" size="sm" tone="danger">
                    <XhTagLabel>{{ t('access.roles.perm_denied') }}</XhTagLabel>
                  </XhTagRoot>
                  <!-- 上级拒绝的，本角色授予了也不生效；已继承的，不必再授予 -->
                  <XhTagRoot
                    v-else-if="permissionEditor.inherited.value.get(String(item.basicId))?.deniedBy.length"
                    variant="subtle"
                    size="sm"
                    tone="danger"
                    :title="t('access.roles.perm_denied_by', { names: permissionEditor.inherited.value.get(String(item.basicId))?.deniedBy.join('、') })"
                  >
                    <XhTagLabel>{{ t('access.roles.perm_inherited_denied') }}</XhTagLabel>
                  </XhTagRoot>
                  <XhTagRoot
                    v-else-if="permissionEditor.inherited.value.get(String(item.basicId))?.grantedBy.length"
                    variant="subtle"
                    size="sm"
                    tone="success"
                    :title="t('access.roles.perm_granted_by', { names: permissionEditor.inherited.value.get(String(item.basicId))?.grantedBy.join('、') })"
                  >
                    <XhTagLabel>{{ t('access.roles.perm_inherited') }}</XhTagLabel>
                  </XhTagRoot>
                </template>
              </XPermissionTransfer>
            </XhTabsContent>
            <XhTabsContent value="deny">
              <p class="drawer-tip">
                {{ t('access.roles.perm_deny_tip') }}
              </p>
              <GrantWindowFields
                v-model="permissionEditor.windowDraft.value"
                with-reason
                :effective-help="t('access.roles.window.new_permission_help')"
                :disabled="permissionEditor.loading.value || permissionEditor.saving.value"
              />
              <XPermissionTransfer
                :items="permissionEditor.items.value"
                :value="permissionEditor.denyIds.value"
                :loading="permissionEditor.loading.value"
                :disabled="permissionEditor.loading.value || permissionEditor.saving.value"
                :source-title="t('access.roles.perm_deny_available')"
                :target-title="t('access.roles.perm_denied')"
                :search-placeholder="t('access.roles.perm_search')"
                :other-group-label="t('access.roles.perm_group_other')"
                @update:value="next => permissionEditor.transfer('Deny', next)"
              >
                <template #suffix="{ item, side }">
                  <!-- 左栏标出已在授予里的：挪过来会把授予改成拒绝 -->
                  <XhTagRoot v-if="side === 'source' && permissionEditor.actions.value.get(String(item.basicId)) === 'Grant'" variant="subtle" size="sm" tone="success">
                    <XhTagLabel>{{ t('access.roles.perm_granted') }}</XhTagLabel>
                  </XhTagRoot>
                </template>
              </XPermissionTransfer>
            </XhTabsContent>
            <XhTabsContent value="validity">
              <p v-if="permissionEditor.dirty.value" class="drawer-tip">
                {{ t('access.roles.validity_dirty_tip') }}
              </p>
              <XDataTable
                :columns="permissionEntryColumns"
                :data="permissionEditor.entries.value"
                row-key="code"
                :loading="permissionEditor.loading.value"
                :empty-text="t('access.roles.empty_permissions')"
              />
            </XhTabsContent>
          </XhTabsRoot>
        </XhDrawerBody>
        <XhDrawerFooter>
          <XhButton variant="subtle" @click="permissionEditor.open.value = false">
            {{ t('common.actions.cancel') }}
          </XhButton>
          <XhButton variant="subtle" tone="brand" :loading="permissionEditor.saving.value" :disabled="!permissionEditor.dirty.value || !permissionEditor.windowValid.value || permissionEditor.loading.value" @click="savePermissions">
            <XhButtonIndicator />
            <XhButtonLabel>{{ t('access.roles.perm_save') }}</XhButtonLabel>
          </XhButton>
        </XhDrawerFooter>
      </XhDrawerContent>
    </XhDrawerRoot>

    <!-- 角色成员 -->
    <XhDrawerRoot v-model:open="memberEditor.open.value" side="right">
      <XhDrawerContent class="w-3xl">
        <XhDrawerHeader>
          <XhDrawerTitle>{{ t('access.roles.members_title', { name: memberEditor.role.value?.name ?? '' }) }}</XhDrawerTitle>
        </XhDrawerHeader>
        <XhDrawerCloseTrigger />
        <XhDrawerBody class="xh-overlay-body">
          <XhTabsRoot v-model:value="memberEditor.tab.value" class="grant-tabs" variant="line">
            <XhTabsList>
              <XhTabsTrigger value="members">
                {{ t('access.roles.members_tab') }}
              </XhTabsTrigger>
              <XhTabsTrigger value="validity">
                {{ t('access.roles.tab_validity') }}
              </XhTabsTrigger>
              <XhTabsIndicator />
            </XhTabsList>
            <XhTabsContent value="members">
              <p class="drawer-tip">
                {{ canListUsers ? t('access.roles.members_tip') : t('access.roles.members_tip_readonly') }}
              </p>
              <!-- 读不到用户目录时加不了人：有效期只给新加入的成员 -->
              <GrantWindowFields
                v-if="canListUsers"
                v-model="memberEditor.windowDraft.value"
                :effective-help="t('access.roles.window.new_member_help')"
                :disabled="memberEditor.loading.value || memberEditor.saving.value"
              />
              <XGrantTransfer
                :items="memberEditor.items.value"
                :value="memberEditor.draft.value"
                :groups="memberGroups"
                :get-label="item => item.displayName"
                :get-description="memberDescription"
                :loading="memberEditor.loading.value"
                :disabled="memberEditor.loading.value || memberEditor.saving.value"
                :source-title="t('access.roles.members_source')"
                :target-title="t('access.roles.members_target')"
                :search-placeholder="t('access.roles.members_search')"
                @update:value="value => (memberEditor.draft.value = value)"
              />
            </XhTabsContent>
            <XhTabsContent value="validity">
              <p v-if="memberEditor.dirty.value" class="drawer-tip">
                {{ t('access.roles.validity_dirty_tip') }}
              </p>
              <XDataTable
                :columns="memberColumns"
                :data="memberEditor.members.value"
                row-key="userId"
                :loading="memberEditor.loading.value"
                :empty-text="t('access.roles.empty_members')"
              />
            </XhTabsContent>
          </XhTabsRoot>
        </XhDrawerBody>
        <XhDrawerFooter>
          <XhButton variant="subtle" @click="memberEditor.open.value = false">
            {{ t('common.actions.cancel') }}
          </XhButton>
          <XhButton variant="subtle" tone="brand" :loading="memberEditor.saving.value" :disabled="!memberEditor.dirty.value || !memberEditor.windowValid.value || memberEditor.loading.value" @click="saveMembers">
            <XhButtonIndicator />
            <XhButtonLabel>{{ t('access.roles.members_save') }}</XhButtonLabel>
          </XhButton>
        </XhDrawerFooter>
      </XhDrawerContent>
    </XhDrawerRoot>

    <!-- 数据范围（租户侧） -->
    <XhDrawerRoot v-model:open="scopeEditor.open.value" side="right">
      <XhDrawerContent class="w-3xl">
        <XhDrawerHeader>
          <XhDrawerTitle>{{ t('access.roles.scope_editor.drawer_title', { name: scopeEditor.role.value?.name ?? '' }) }}</XhDrawerTitle>
        </XhDrawerHeader>
        <XhDrawerCloseTrigger />
        <XhDrawerBody class="xh-overlay-body">
          <div class="xh-loading-stage scope-stage" :class="{ 'is-loading': scopeEditor.loading.value }">
            <div class="xh-loading-stage__veil">
              <XhSpinner />
            </div>
            <DataScopeEditor v-model="scopeEditor.draft.value" :department-tree="scopeEditor.tree.value" :readonly="scopeEditor.saving.value" />
          </div>
        </XhDrawerBody>
        <XhDrawerFooter>
          <XhButton variant="subtle" @click="scopeEditor.open.value = false">
            {{ t('common.actions.cancel') }}
          </XhButton>
          <XhButton variant="subtle" tone="brand" :loading="scopeEditor.saving.value" :disabled="!scopeEditor.dirty.value || scopeEditor.loading.value" @click="saveScope">
            <XhButtonIndicator />
            <XhButtonLabel>{{ t('access.roles.scope_editor.save') }}</XhButtonLabel>
          </XhButton>
        </XhDrawerFooter>
      </XhDrawerContent>
    </XhDrawerRoot>

    <GrantWindowDialog
      v-model:show="windowDialog.show"
      :title="windowDialog.title"
      :initial="windowDialog.initial"
      :with-reason="windowDialog.withReason"
      :loading="windowSaving"
      @submit="submitWindow"
    />
    <RoleParentsDialog v-model:show="parentsVisible" :role="parentsRole" @saved="reload" />
    <GrantConditionsDrawer v-model:open="conditionsVisible" :title="t('access.conditions.drawer_title', { name: conditionsRole?.name ?? '' })" subject-kind="Role" :subject-id="conditionsRole?.id ?? null" />
  </SchemaPage>
</template>

<style scoped>
.role-empty {
  padding-block: var(--xh-space-8);
}

.role-tab-empty {
  padding-block: var(--xh-space-6);
}

/* 页签里的说明（成员、继承、拒绝的作用范围、有未保存改动） */
.drawer-tip {
  margin: 0;
  color: var(--xh-fg-muted);
  font-size: var(--xh-text-caption-size);
}

.grant-tabs {
  display: flex;
  flex: 1;
  flex-direction: column;
  min-block-size: 0;
}

/* 当前页签的面板接着往下撑，穿梭框才拿得到剩余高度 */
.grant-tabs > [data-scope='tabs'][data-part='content'][data-state='active'] {
  display: flex;
  flex: 1;
  flex-direction: column;
  gap: var(--xh-space-3);
  min-block-size: 0;
}

/* 数据范围抽屉：编辑区撑满抽屉剩余高度，保存 / 取消落在抽屉底部；部门多时在这里滚动 */
.scope-stage {
  flex: 1;
  min-block-size: 0;
  overflow-y: auto;
}
</style>
