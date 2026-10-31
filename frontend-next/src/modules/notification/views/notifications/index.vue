<script lang="ts" setup>
import type { NotificationDetail, NotificationForm, NotificationRow, ReadStats, RecipientRow } from './notification'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaPageInstance, XDataTableColumn } from '~/components'
import type { DeliveryChannel, NotificationRecipientFilter, NotificationTargetType } from '~/contracts'
import type { SelectOption } from '~/types'
import type { NotificationContentFormat as ShellContentFormat } from '~/types/enums'
import {
  XhCheckboxGroupIndicator,
  XhCheckboxGroupItem,
  XhCheckboxGroupItemText,
  XhCheckboxGroupRoot,
  XhDescriptionsItem,
  XhDescriptionsLabel,
  XhDescriptionsRoot,
  XhDescriptionsValue,
  XhDrawerBody,
  XhDrawerCloseTrigger,
  XhDrawerContent,
  XhDrawerHeader,
  XhDrawerRoot,
  XhDrawerTitle,
  XhFieldControl,
  XhFieldDescription,
  XhFieldLabel,
  XhFieldRoot,
  XhFormRoot,
  XhProgress,
  XhStatisticLabel,
  XhStatisticRoot,
  XhStatisticValue,
  XhSwitch,
  XhTagLabel,
  XhTagRoot,
} from '@xihan-ui/vue'
import { computed, h, reactive, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { IconPicker, NotificationContent, renderTag, SchemaPage, XContentEditorField, XDataTable, XDatePicker, XEditModal, XInput, XMdEditor, XPopconfirm, XSegmented, XSelect, XTagsInput } from '~/components'
import { dialog, toast } from '~/composables'
import { NotificationContentFormat as CONTENT_FORMATS, DeliveryChannel as DELIVERY_CHANNELS, notifications as notificationsApi, NotificationPriority as PRIORITIES, NotificationTargetType as TARGET_TYPES, NotificationType as TYPES } from '~/contracts'
import { useAuthStore } from '~/stores'
import { downloadBlob, formatDate } from '~/utils'
import { contentFormatLabel, deliveryChannelLabel, errorText, notificationTypeLabel, notificationTypeTone, priorityLabel, targetTypeLabel } from '../../labels'
import {
  emptyNotificationForm,
  findNotificationProblem,
  loadDepartmentOptions,
  loadRecipients,
  loadRoleOptions,
  loadUnreadForExport,
  needsTargets,
  normalizeChannels,
  notificationResource,
  problemMessage,
  readRate,
  RECIPIENT_PAGE_SIZE,
  recipientFilterLabel,
  recipientFilters,
  toCreateBody,
  toNotificationForm,
  toUpdateBody,
  unreadCsv,
} from './notification'

defineOptions({ name: 'NotificationNotificationsView' })

const { t } = useI18n()
const auth = useAuthStore()
const resource = notificationResource(t)

/** 编辑弹窗的保存钮靠这个 id 关联到表单，点它才会走整表提交 */
const editFormId = useId()
const schemaPageRef = ref<SchemaPageInstance | null>(null)

function reload() {
  void schemaPageRef.value?.reload()
}

const asRow = (row: object) => row as NotificationRow
/** 平台一侧没有部门（后端拒绝按部门投递），表单不给这一档 */
const isPlatformCaller = () => auth.tenantId === null

// ── 选项 ─────────────────────────────────────────────────────────
const typeOptions = computed<SelectOption[]>(() => TYPES.map(type => ({ label: notificationTypeLabel(t, type), value: type })))
const priorityOptions = computed<SelectOption[]>(() => PRIORITIES.map(priority => ({ label: priorityLabel(t, priority), value: priority })))
const contentFormatOptions = computed<SelectOption[]>(() => CONTENT_FORMATS.map(format => ({ label: contentFormatLabel(t, format), value: format })))
/** 展示用全集 */
const targetTypeOptions = computed<SelectOption[]>(() => TARGET_TYPES.map(type => ({ label: targetTypeLabel(t, type), value: type })))
/** 表单可选目标：平台一侧没有部门 */
const formTargetTypeOptions = computed<SelectOption[]>(() => targetTypeOptions.value.filter(option => option.value !== 'Department' || !isPlatformCaller()))
const publishedOptions = computed<SelectOption[]>(() => [
  { label: t('notification.notifications.published'), value: 'true' },
  { label: t('notification.notifications.unpublished'), value: 'false' },
])

function publishedTag(published: boolean) {
  return renderTag(published ? t('notification.notifications.published') : t('notification.notifications.unpublished'), published ? 'success' : 'neutral')
}

/** 投递渠道的勾选项：站内信始终投递，固定勾选、不可取消（4.x 同） */
const deliveryChannelOptions = computed(() => DELIVERY_CHANNELS.map(channel => ({ label: deliveryChannelLabel(t, channel), value: channel, disabled: channel === 'InApp' })))

/** 列表里的渠道标签（4.x 同）：站内信 neutral，其余 info；h() 节点不带页面 scope，样式写 :deep() */
function channelTags(channels: readonly DeliveryChannel[]) {
  return h('span', { class: 'notification-channels' }, normalizeChannels(channels).map(channel => renderTag(deliveryChannelLabel(t, channel), channel === 'InApp' ? 'neutral' : 'info')))
}

function formatChannels(channels: readonly DeliveryChannel[]): string {
  return normalizeChannels(channels).map(channel => deliveryChannelLabel(t, channel)).join(' / ')
}

// ── 字段单一事实源（列 + 搜索；仅搜索字段 visible:false；可排序的只放后端白名单里的：发布时间、创建时间） ──
const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('notification.notifications.col_keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('notification.notifications.search_keyword_placeholder'), order: 0 },
  { key: 'title', title: t('notification.notifications.col_title'), dataType: 'string', minWidth: 220, order: 10 },
  { key: 'notificationType', title: t('notification.notifications.col_type'), dataType: 'enum', searchable: true, searchMultiple: true, options: typeOptions.value, searchPlaceholder: t('notification.notifications.search_type_placeholder'), width: 110, order: 11, render: row => renderTag(notificationTypeLabel(t, asRow(row).notificationType), notificationTypeTone(asRow(row).notificationType)) },
  { key: 'targetType', title: t('notification.notifications.col_target_type'), dataType: 'enum', options: targetTypeOptions.value, width: 110, order: 12, render: row => targetTypeLabel(t, asRow(row).targetType) },
  { key: 'deliveryChannels', title: t('notification.notifications.col_delivery_channels'), dataType: 'string', width: 170, order: 13, render: row => channelTags(asRow(row).deliveryChannels) },
  { key: 'isPublished', title: t('notification.notifications.col_is_published'), dataType: 'enum', searchable: true, options: publishedOptions.value, searchPlaceholder: t('notification.notifications.search_published_placeholder'), width: 100, order: 14, render: row => publishedTag(asRow(row).isPublished) },
  { key: 'publishedAt', title: t('notification.notifications.col_send_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 15 },
  { key: 'expiresAt', title: t('notification.notifications.col_expiration_time'), dataType: 'datetime', minWidth: 170, order: 16 },
  { key: 'createdAt', title: t('notification.notifications.col_created_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 17 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'notification.notifications',
  pageName: t('notification.notifications.title'),
  exportPermission: 'notification.notifications.read',
  rowKey: 'id',
  fields: fields.value,
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    { key: 'create', title: t('notification.notifications.action_create'), scope: 'page', tone: 'brand', icon: 'lucide:plus', permission: 'notification.notifications.create' },
    { key: 'view', title: t('notification.notifications.action_view'), scope: 'row', icon: 'lucide:eye' },
    { key: 'stats', title: t('notification.notifications.action_stats'), scope: 'row', icon: 'lucide:bar-chart-3', permission: 'notification.notifications.read', visible: row => asRow(row).isPublished },
    // 已发布的不能再改、不能重复发布（后端同样拒绝），入口随之收起
    { key: 'edit', title: t('notification.notifications.action_edit'), scope: 'row', icon: 'lucide:pen', permission: 'notification.notifications.update', visible: row => !asRow(row).isPublished },
    { key: 'publish', title: t('notification.notifications.action_publish'), scope: 'row', tone: 'brand', icon: 'lucide:send', permission: 'notification.notifications.publish', visible: row => !asRow(row).isPublished },
    { key: 'delete', title: t('notification.notifications.action_delete'), scope: 'row', tone: 'danger', icon: 'lucide:trash-2', permission: 'notification.notifications.delete', confirm: true, confirmText: t('notification.notifications.confirm_delete') },
  ],
}))

function onAction(payload: SchemaActionPayload) {
  const row = payload.row ? asRow(payload.row) : undefined
  switch (payload.key) {
    case 'create':
      openCreate()
      break
    case 'view':
      if (row)
        void openDetail(row)
      break
    case 'stats':
      if (row)
        void openStats(row)
      break
    case 'edit':
      if (row)
        void openEdit(row)
      break
    case 'publish':
      if (row)
        confirmPublish(row)
      break
    case 'delete':
      if (row)
        void removeRow(row)
      break
  }
}

// ── 详情（抽屉） ─────────────────────────────────────────────────
const detailVisible = ref(false)
const currentDetail = ref<NotificationDetail | null>(null)

async function openDetail(row: NotificationRow) {
  try {
    currentDetail.value = await notificationsApi.get({ id: row.id })
    detailVisible.value = true
  }
  catch (error) {
    toast.danger(errorText(error) || t('notification.notifications.msg_load_detail_failed'))
  }
}

function yesNo(value: boolean): string {
  return value ? t('common.statuses.yes') : t('common.statuses.no')
}

// ── 角色 / 部门目标的候选（选到这一档时取一次） ──────────────────
const roleOptions = ref<SelectOption[]>([])
const departmentOptions = ref<SelectOption[]>([])
const targetLoading = ref(false)
const loadedTargets = new Set<NotificationTargetType>()

async function ensureTargetOptions(type: NotificationTargetType) {
  if ((type !== 'Role' && type !== 'Department') || loadedTargets.has(type))
    return
  targetLoading.value = true
  try {
    if (type === 'Role')
      roleOptions.value = await loadRoleOptions()
    else
      departmentOptions.value = await loadDepartmentOptions()
    loadedTargets.add(type)
  }
  catch (error) {
    toast.danger(errorText(error) || t('notification.notifications.msg_load_targets_failed'))
  }
  finally {
    targetLoading.value = false
  }
}

// ── 新建 / 编辑（未发布才可） ────────────────────────────────────
const modalVisible = ref(false)
const submitting = ref(false)
const submitError = ref<null | string>(null)
const form = reactive<NotificationForm>(emptyNotificationForm())
/** 正文按 Markdown 编辑（4.x 同：Markdown 用 md-editor-v3，其余格式用文本框） */
const isMarkdownContent = computed(() => form.contentFormat === 'Markdown')
const modalTitle = computed(() => (form.id ? t('notification.notifications.edit_title') : t('notification.notifications.add_title')))

function openCreate() {
  Object.assign(form, emptyNotificationForm())
  submitError.value = null
  modalVisible.value = true
}

async function openEdit(row: NotificationRow) {
  try {
    const detail = await notificationsApi.get({ id: row.id })
    if (detail.isPublished) {
      toast.warning(t('notification.notifications.msg_published_cannot_edit'))
      reload()
      return
    }
    Object.assign(form, toNotificationForm(detail))
    submitError.value = null
    void ensureTargetOptions(form.targetType)
    modalVisible.value = true
  }
  catch (error) {
    toast.danger(errorText(error) || t('notification.notifications.msg_load_failed'))
  }
}

/** 换目标类型即清空已选对象：角色、部门与用户的 ID 不通用 */
function changeTargetType(value: unknown) {
  form.targetType = value as NotificationTargetType
  form.targetIds = []
  void ensureTargetOptions(form.targetType)
}

function changeTargetIds(value: unknown) {
  form.targetIds = Array.isArray(value) ? value.map(String) : []
}

/** 勾选渠道：站内信总在内 */
function changeChannels(value: string[]) {
  form.deliveryChannels = normalizeChannels(value)
}

async function onSubmit() {
  const problem = findNotificationProblem(form)
  if (problem) {
    toast.warning(problemMessage(t, problem))
    return
  }
  submitting.value = true
  submitError.value = null
  try {
    if (form.id) {
      await notificationsApi.update(toUpdateBody({ ...form, id: form.id }))
      toast.success(t('notification.notifications.msg_update_success'))
    }
    else {
      await notificationsApi.create(toCreateBody(form))
      toast.success(t('notification.notifications.msg_create_success'))
    }
    modalVisible.value = false
    reload()
  }
  catch (error) {
    submitError.value = errorText(error) || t('notification.notifications.msg_save_failed')
  }
  finally {
    submitting.value = false
  }
}

// ── 发布（确认对话框；目标范围沿用草稿里设定的） ─────────────────
function confirmPublish(row: NotificationRow) {
  const target = row.targetType === 'All'
    ? t('notification.notifications.publish_target_all')
    : t('notification.notifications.publish_target_user')
  void dialog.confirm({
    badge: 'warning',
    title: t('notification.notifications.publish_dialog_title'),
    content: t('notification.notifications.publish_confirm_content', { title: row.title, target }),
    okText: t('notification.notifications.publish_positive'),
    cancelText: t('common.actions.cancel'),
    onOk: async () => {
      try {
        const result = await notificationsApi.publish({ id: row.id })
        toast.success(t('notification.notifications.msg_publish_success', { count: result.recipientCount }))
        reload()
      }
      catch (error) {
        toast.danger(errorText(error) || t('notification.notifications.msg_publish_failed'))
      }
    },
  })
}

// ── 删除 ─────────────────────────────────────────────────────────
async function removeRow(row: NotificationRow) {
  try {
    await resource.remove?.(row.id)
    toast.success(t('notification.notifications.msg_delete_success'))
    reload()
  }
  catch (error) {
    toast.danger(errorText(error) || t('notification.notifications.msg_delete_failed'))
  }
}

// ── 运营数据（抽屉） ─────────────────────────────────────────────
const statsVisible = ref(false)
const statsLoading = ref(false)
const statsRow = ref<{ id: string, title: string } | null>(null)
const readStats = ref<ReadStats | null>(null)
const recipientFilter = ref<NotificationRecipientFilter>('Unread')
const recipients = ref<RecipientRow[]>([])
const recipientTotal = ref(0)
const recipientPage = ref(1)
const remindLoading = ref(false)
const exportLoading = ref(false)

const statsTitle = computed(() => (statsRow.value ? `${t('notification.notifications.stats_title')} · ${statsRow.value.title}` : t('notification.notifications.stats_title')))
const statsReadRate = computed(() => (readStats.value ? readRate(readStats.value) : 0))
const canRemind = computed(() => auth.can('notification.notifications.publish'))
const recipientFilterOptions = computed(() => recipientFilters(readStats.value?.needConfirm ?? false).map(filter => ({ label: recipientFilterLabel(t, filter), value: filter })))

const recipientColumns = computed<XDataTableColumn<RecipientRow>[]>(() => [
  { key: 'userName', title: t('notification.notifications.col_user_name'), minWidth: 140 },
  { key: 'displayName', title: t('notification.notifications.col_display_name'), minWidth: 120, render: row => row.displayName || '-' },
  { key: 'status', title: t('notification.notifications.col_status'), width: 90, render: row => (row.status === 'Read' ? renderTag(t('notification.notifications.stats_read'), 'success') : renderTag(t('notification.notifications.stats_unread'), 'warning')) },
  { key: 'receivedAt', title: t('notification.notifications.col_received_time'), minWidth: 170, render: row => formatDate(row.receivedAt) },
  { key: 'readAt', title: t('notification.notifications.col_read_time'), minWidth: 170, render: row => formatDate(row.readAt) },
  ...(readStats.value?.needConfirm
    ? [{ key: 'confirmedAt', title: t('notification.notifications.col_confirm_time'), minWidth: 170, render: (row: RecipientRow) => formatDate(row.confirmedAt) }]
    : []),
])

async function fetchRecipients(id: string, page: number) {
  const result = await loadRecipients(id, recipientFilter.value, page)
  recipients.value = result.items
  recipientTotal.value = Number(result.total)
  recipientPage.value = page
}

async function openStats(row: NotificationRow) {
  statsRow.value = { id: row.id, title: row.title }
  readStats.value = null
  recipientFilter.value = 'Unread'
  recipients.value = []
  recipientTotal.value = 0
  recipientPage.value = 1
  statsVisible.value = true
  statsLoading.value = true
  try {
    const [stats] = await Promise.all([notificationsApi.readStats({ id: row.id }), fetchRecipients(row.id, 1)])
    readStats.value = stats
  }
  catch (error) {
    toast.danger(errorText(error) || t('notification.notifications.msg_load_failed'))
  }
  finally {
    statsLoading.value = false
  }
}

async function changeRecipients(page: number) {
  if (!statsRow.value)
    return
  statsLoading.value = true
  try {
    await fetchRecipients(statsRow.value.id, page)
  }
  catch (error) {
    toast.danger(errorText(error) || t('notification.notifications.msg_load_failed'))
  }
  finally {
    statsLoading.value = false
  }
}

function changeRecipientFilter(value: NotificationRecipientFilter) {
  recipientFilter.value = value
  void changeRecipients(1)
}

async function confirmRemind() {
  const row = statsRow.value
  if (!row)
    return
  remindLoading.value = true
  try {
    const result = await notificationsApi.remind({ id: row.id })
    toast.success(t('notification.notifications.stats_remind_success', { count: result.recipientCount }))
    readStats.value = await notificationsApi.readStats({ id: row.id })
  }
  catch (error) {
    toast.danger(errorText(error) || t('notification.notifications.stats_remind_failed'))
  }
  finally {
    remindLoading.value = false
  }
}

async function exportUnread() {
  const row = statsRow.value
  if (!row)
    return
  exportLoading.value = true
  try {
    const { rows, truncated } = await loadUnreadForExport(row.id)
    if (truncated)
      toast.warning(t('notification.notifications.stats_export_truncated'))
    const header = [
      t('notification.notifications.col_user_name'),
      t('notification.notifications.col_display_name'),
      t('notification.notifications.col_received_time'),
    ] as const
    const csv = unreadCsv(header, rows, value => formatDate(value))
    downloadBlob(new Blob([csv], { type: 'text/csv;charset=utf-8' }), `unread-users-${row.id}.csv`)
  }
  catch (error) {
    toast.danger(errorText(error) || t('notification.notifications.stats_export_failed'))
  }
  finally {
    exportLoading.value = false
  }
}
</script>

<template>
  <SchemaPage ref="schemaPageRef" :schema="schema" @action="onAction">
    <!-- 新建 / 编辑 -->
    <XEditModal v-model:show="modalVisible" :title="modalTitle" :loading="submitting" :form-id="editFormId" width="760px">
      <XhFormRoot :id="editFormId" class="xh-edit-form-grid" data-testid="notification-form" @submit="onSubmit">
        <XhFieldRoot class="xh-span-2" required>
          <XhFieldLabel>{{ t('notification.notifications.form_title') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.title" name="title" clearable :max-length="200" :placeholder="t('notification.notifications.form_title_placeholder')" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('notification.notifications.form_priority') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect v-model:value="form.priority" name="priority" :options="priorityOptions" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('notification.notifications.form_content_format') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect v-model:value="form.contentFormat" name="contentFormat" :options="contentFormatOptions" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot class="xh-span-2">
          <XhFieldLabel>{{ t('notification.notifications.form_content') }}</XhFieldLabel>
          <XhFieldControl :as-child="false">
            <XContentEditorField
              v-model="form.content"
              :title="t('notification.notifications.form_content_drawer_title')"
              :placeholder="t('notification.notifications.form_content_placeholder')"
              :edit-text="t('notification.notifications.form_content_edit')"
              :confirm-text="t('common.actions.confirm')"
              :cancel-text="t('common.actions.cancel')"
              :count-label="(count: number) => t('notification.notifications.form_content_count', { count })"
            >
              <template #editor="{ value, update }">
                <XMdEditor v-if="isMarkdownContent" editor-id="notification-content-editor" :model-value="value" @update:model-value="update" />
                <XInput v-else :value="value" type="textarea" :placeholder="t('notification.notifications.form_content_placeholder')" @update:value="update" />
              </template>
            </XContentEditorField>
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('notification.notifications.form_type') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect v-model:value="form.notificationType" name="notificationType" :options="typeOptions" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('notification.notifications.form_target_type') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect :value="form.targetType" name="targetType" :options="formTargetTypeOptions" @update:value="changeTargetType" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot class="xh-span-2">
          <XhFieldLabel>{{ t('notification.notifications.form_delivery_channels') }}</XhFieldLabel>
          <XhFieldControl>
            <XhCheckboxGroupRoot class="notification-channel-picker" orientation="horizontal" name="deliveryChannels" :value="form.deliveryChannels" @update:value="changeChannels">
              <XhCheckboxGroupItem v-for="option in deliveryChannelOptions" :key="option.value" :value="option.value" :disabled="option.disabled">
                <XhCheckboxGroupIndicator />
                <XhCheckboxGroupItemText>{{ option.label }}</XhCheckboxGroupItemText>
              </XhCheckboxGroupItem>
            </XhCheckboxGroupRoot>
          </XhFieldControl>
          <XhFieldDescription>{{ t('notification.notifications.form_delivery_channels_hint') }}</XhFieldDescription>
        </XhFieldRoot>
        <XhFieldRoot v-if="form.targetType === 'User'" class="xh-span-2" required>
          <XhFieldLabel>{{ t('notification.notifications.form_user_ids') }}</XhFieldLabel>
          <XhFieldControl>
            <XTagsInput v-model:value="form.targetIds" :placeholder="t('notification.notifications.form_user_ids_placeholder')" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot v-else-if="form.targetType === 'Role'" class="xh-span-2" required>
          <XhFieldLabel>{{ t('notification.notifications.form_role_ids') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect :value="form.targetIds" name="targetIds" multiple clearable :loading="targetLoading" :options="roleOptions" @update:value="changeTargetIds" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot v-else-if="needsTargets(form.targetType)" class="xh-span-2" required>
          <XhFieldLabel>{{ t('notification.notifications.form_department_ids') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect :value="form.targetIds" name="targetIds" multiple clearable :loading="targetLoading" :options="departmentOptions" @update:value="changeTargetIds" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('notification.notifications.form_icon') }}</XhFieldLabel>
          <XhFieldControl :as-child="false">
            <IconPicker v-model="form.icon" :placeholder="t('notification.notifications.form_icon_placeholder')" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('notification.notifications.form_link') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.link" name="link" clearable :max-length="500" :placeholder="t('notification.notifications.form_link_placeholder')" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('notification.notifications.form_start_time') }}</XhFieldLabel>
          <XhFieldControl>
            <XDatePicker v-model:value="form.startsAt" show-time clearable />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('notification.notifications.form_expiration_time') }}</XhFieldLabel>
          <XhFieldControl>
            <XDatePicker v-model:value="form.expiresAt" show-time clearable />
          </XhFieldControl>
          <XhFieldDescription>{{ t('notification.notifications.form_expiration_placeholder') }}</XhFieldDescription>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('notification.notifications.form_need_confirm') }}</XhFieldLabel>
          <XhFieldControl>
            <XhSwitch v-model:checked="form.needConfirm" name="needConfirm" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('notification.notifications.form_mandatory') }}</XhFieldLabel>
          <XhFieldControl>
            <XhSwitch v-model:checked="form.isMandatory" name="isMandatory" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('notification.notifications.form_banner') }}</XhFieldLabel>
          <XhFieldControl>
            <XhSwitch v-model:checked="form.isBanner" name="isBanner" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('notification.notifications.form_popup') }}</XhFieldLabel>
          <XhFieldControl>
            <XhSwitch v-model:checked="form.isPopup" name="isPopup" />
          </XhFieldControl>
        </XhFieldRoot>
        <p v-if="submitError" class="xh-span-2 text-caption text-fg-danger" role="alert">
          {{ submitError }}
        </p>
      </XhFormRoot>
    </XEditModal>

    <!-- 详情（抽屉） -->
    <XhDrawerRoot v-model:open="detailVisible" side="right">
      <XhDrawerContent style="--xh-drawer-size: 35rem">
        <XhDrawerHeader>
          <XhDrawerTitle>{{ t('notification.notifications.detail_title') }}</XhDrawerTitle>
        </XhDrawerHeader>
        <XhDrawerCloseTrigger />
        <XhDrawerBody v-if="currentDetail" class="xh-overlay-body">
          <XhDescriptionsRoot :columns="2" variant="outline" size="sm">
            <XhDescriptionsItem class="notification-detail__wide">
              <XhDescriptionsLabel>{{ t('notification.notifications.detail.label.title') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ currentDetail.title }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.notifications.detail.label.type') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ notificationTypeLabel(t, currentDetail.notificationType) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.notifications.detail.label.target_type') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ targetTypeLabel(t, currentDetail.targetType) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.notifications.detail.label.delivery_channels') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ formatChannels(currentDetail.deliveryChannels) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.notifications.detail.label.is_published') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                <XhTagRoot variant="subtle" size="sm" :tone="currentDetail.isPublished ? 'success' : 'neutral'">
                  <XhTagLabel>
                    {{ currentDetail.isPublished ? t('notification.notifications.published') : t('notification.notifications.unpublished') }}
                  </XhTagLabel>
                </XhTagRoot>
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.notifications.detail.label.priority') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ priorityLabel(t, currentDetail.priority) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.notifications.detail.label.need_confirm') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ yesNo(currentDetail.needConfirm) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.notifications.detail.label.mandatory') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ yesNo(currentDetail.isMandatory) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.notifications.detail.label.banner') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ yesNo(currentDetail.isBanner) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.notifications.detail.label.popup') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ yesNo(currentDetail.isPopup) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.notifications.detail.label.start_time') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ formatDate(currentDetail.startsAt) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.notifications.detail.label.send_time') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ formatDate(currentDetail.publishedAt) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.notifications.detail.label.expiration_time') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ currentDetail.expiresAt ? formatDate(currentDetail.expiresAt) : t('notification.notifications.never_expire') }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.notifications.detail.label.icon') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ currentDetail.icon || '-' }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.notifications.detail.label.link') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ currentDetail.link || '-' }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.notifications.detail.label.business_type') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ currentDetail.businessType || '-' }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.notifications.detail.label.business_id') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ currentDetail.businessId || '-' }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.notifications.detail.label.created_time') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ formatDate(currentDetail.createdAt) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem class="notification-detail__wide">
              <XhDescriptionsLabel>{{ t('notification.notifications.detail.label.remark') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ currentDetail.remark || '-' }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem class="notification-detail__wide">
              <XhDescriptionsLabel>{{ t('notification.notifications.detail.label.content') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                <NotificationContent
                  v-if="currentDetail.content"
                  :content="currentDetail.content"
                  :format="currentDetail.contentFormat as ShellContentFormat"
                />
                <span v-else>{{ t('notification.notifications.detail_no_content') }}</span>
              </XhDescriptionsValue>
            </XhDescriptionsItem>
          </XhDescriptionsRoot>
        </XhDrawerBody>
      </XhDrawerContent>
    </XhDrawerRoot>

    <!-- 运营数据（抽屉） -->
    <XhDrawerRoot v-model:open="statsVisible" side="right">
      <XhDrawerContent style="--xh-drawer-size: 45rem">
        <XhDrawerHeader>
          <XhDrawerTitle>{{ statsTitle }}</XhDrawerTitle>
        </XhDrawerHeader>
        <XhDrawerCloseTrigger />
        <XhDrawerBody class="xh-overlay-body">
          <div v-if="readStats" class="notification-stats" data-testid="notification-stats">
            <!-- 统计区 -->
            <div class="notification-stats__cards">
              <XhStatisticRoot>
                <XhStatisticLabel>{{ t('notification.notifications.stats_recipient') }}</XhStatisticLabel>
                <XhStatisticValue>{{ readStats.recipientCount }}</XhStatisticValue>
              </XhStatisticRoot>
              <XhStatisticRoot>
                <XhStatisticLabel>{{ t('notification.notifications.stats_read') }}</XhStatisticLabel>
                <XhStatisticValue>{{ readStats.readCount }}</XhStatisticValue>
              </XhStatisticRoot>
              <XhStatisticRoot>
                <XhStatisticLabel>{{ t('notification.notifications.stats_unread') }}</XhStatisticLabel>
                <XhStatisticValue>{{ readStats.unreadCount }}</XhStatisticValue>
              </XhStatisticRoot>
              <XhStatisticRoot v-if="readStats.needConfirm">
                <XhStatisticLabel>{{ t('notification.notifications.stats_confirm') }}</XhStatisticLabel>
                <XhStatisticValue>{{ readStats.confirmedCount }}</XhStatisticValue>
              </XhStatisticRoot>
            </div>
            <div class="notification-stats__rate">
              <span class="notification-stats__rate-label">{{ t('notification.notifications.stats_read_rate') }}</span>
              <XhProgress
                class="notification-stats__rate-bar"
                semantics="meter"
                :value="statsReadRate"
                :value-text="`${statsReadRate}%`"
                :aria-label="t('notification.notifications.stats_read_rate')"
              />
              <span class="notification-stats__rate-value">{{ statsReadRate }}%</span>
            </div>
            <!-- 操作区 -->
            <div class="notification-stats__ops">
              <XPopconfirm v-if="canRemind" :description="t('notification.notifications.stats_remind_confirm', { count: readStats.unreadCount })" @confirm="confirmRemind">
                <template #trigger>
                  <button type="button" class="xh-linklike-trigger" :disabled="remindLoading">
                    {{ t('notification.notifications.stats_remind') }}
                  </button>
                </template>
              </XPopconfirm>
              <button type="button" class="xh-linklike-trigger" :disabled="exportLoading" @click="exportUnread">
                {{ t('notification.notifications.stats_export') }}
              </button>
            </div>
            <!-- 接收人员（4.x 只有未读人员一档） -->
            <div class="notification-stats__section">
              <span class="notification-stats__section-title">{{ t('notification.notifications.stats_recipients') }}</span>
              <XSegmented :value="recipientFilter" :options="recipientFilterOptions" @update:value="changeRecipientFilter" />
            </div>
            <XDataTable
              :columns="recipientColumns"
              :data="recipients"
              :loading="statsLoading"
              :row-key="(row: RecipientRow) => row.userId"
              :pagination="{
                page: recipientPage,
                pageSize: RECIPIENT_PAGE_SIZE,
                itemCount: recipientTotal,
                onUpdatePage: changeRecipients,
                compact: true,
              }"
            />
          </div>
        </XhDrawerBody>
      </XhDrawerContent>
    </XhDrawerRoot>
  </SchemaPage>
</template>

<style scoped>
/* 列表里的渠道标签：h() 渲染的节点不带页面 scope，经 :deep() 命中 */
:deep(.notification-channels) {
  display: inline-flex;
  flex-wrap: wrap;
  gap: var(--xh-space-1);
}

.notification-channel-picker {
  --xh-checkbox-group-gap: var(--xh-space-3);
}

/* 描述列表里跨两列的项（标题、备注、正文） */
.notification-detail__wide {
  grid-column: span 2;
}

.notification-stats {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-4);
}

.notification-stats__cards {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(min(100%, 7.5rem), 1fr));
  gap: var(--xh-space-3);
}

.notification-stats__rate {
  display: flex;
  align-items: center;
  gap: var(--xh-space-3);
}

.notification-stats__rate-label,
.notification-stats__rate-value {
  flex: none;
  font-size: var(--xh-text-secondary-size);
  color: var(--xh-fg-subtle);
}

.notification-stats__rate-value {
  font-variant-numeric: tabular-nums;
}

.notification-stats__rate-bar {
  flex: 1;
  min-width: 0;
}

.notification-stats__ops {
  display: flex;
  flex-wrap: wrap;
  gap: var(--xh-space-3);
}

.notification-stats__section {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: var(--xh-space-2);
}

.notification-stats__section-title {
  font-size: var(--xh-font-size-md);
  font-weight: var(--xh-font-weight-semibold);
}
</style>
