<script setup lang="ts">
import type { DataTableColumns, SelectOption } from 'naive-ui'
import type {
  ApiId,
  DateTimeString,
  NotificationDetailDto,
  NotificationListItemDto,
  NotificationReadStatsDto,
  NotificationUnreadUserDto,
  PageResult,
} from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import {
  NButton,
  NDataTable,
  NDatePicker,
  NDescriptions,
  NDescriptionsItem,
  NDrawer,
  NDrawerContent,
  NDynamicTags,
  NForm,
  NFormItem,
  NInput,
  NModal,
  NPagination,
  NPopconfirm,
  NProgress,
  NSelect,
  NStatistic,
  NSwitch,
  NTag,
  useDialog,
  useMessage,
} from 'naive-ui'
import { computed, h, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  createPageRequest,
  departmentApi,
  notificationApi,
  NotificationContentFormat,
  NotificationPriority,
  NotificationTargetType,
  NotificationType,
  querySortsFromSchema,
  roleApi,
} from '@/api'
import { IconPicker, NotificationContent, SchemaPage, XMdEditor } from '~/components'
import { useEnumOptions } from '~/hooks'
import { downloadBlob, formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'MessageNotificationPage' })

const { t } = useI18n()
const message = useMessage()
const dialog = useDialog()
const schemaPageRef = ref<InstanceType<typeof SchemaPage> | null>(null)

type TagType = 'default' | 'error' | 'info' | 'success' | 'warning'

// ── 选项 ─────────────────────────────────────────────────────────
const notificationTypeOptions = computed(() => [
  { label: t('message.notification.type_system'), value: NotificationType.System },
  { label: t('message.notification.type_security'), value: NotificationType.Security },
  { label: t('message.notification.type_business'), value: NotificationType.Business },
  { label: t('message.notification.type_todo'), value: NotificationType.Todo },
  { label: t('message.notification.type_emergency'), value: NotificationType.Emergency },
])

const NOTIFICATION_TYPE_TAG: Record<string, TagType> = {
  [NotificationType.System]: 'info',
  [NotificationType.Security]: 'warning',
  [NotificationType.Business]: 'success',
  [NotificationType.Todo]: 'default',
  [NotificationType.Emergency]: 'error',
}

/** 展示用全集（历史数据可能含角色/部门） */
const targetTypeOptions = computed(() => [
  { label: t('message.notification.target_all'), value: NotificationTargetType.All },
  { label: t('message.notification.target_role'), value: NotificationTargetType.Role },
  { label: t('message.notification.target_department'), value: NotificationTargetType.Department },
  { label: t('message.notification.target_user'), value: NotificationTargetType.User },
])

/** 表单可选目标（全员 / 角色 / 部门 / 指定用户） */
const targetTypeFormOptions = computed(() => [
  { label: t('message.notification.target_all'), value: NotificationTargetType.All },
  { label: t('message.notification.target_role'), value: NotificationTargetType.Role },
  { label: t('message.notification.target_department'), value: NotificationTargetType.Department },
  { label: t('message.notification.target_user'), value: NotificationTargetType.User },
])

const publishedOptions = computed(() => [
  { label: t('message.notification.published'), value: 1 },
  { label: t('message.notification.unpublished'), value: 0 },
])

// ── 表单枚举下拉（响应式 i18n + 静态兜底） ───────────────────────
const priorityOptions = useEnumOptions('NotificationPriority', [
  { label: '低', value: NotificationPriority.Low },
  { label: '普通', value: NotificationPriority.Normal },
  { label: '高', value: NotificationPriority.High },
  { label: '紧急', value: NotificationPriority.Urgent },
])
const contentFormatOptions = useEnumOptions('NotificationContentFormat', [
  { label: '纯文本', value: NotificationContentFormat.Text },
  { label: 'Markdown', value: NotificationContentFormat.Markdown },
  { label: 'HTML', value: NotificationContentFormat.Html },
])

// ── 角色/部门定向选项（打开弹窗时按需加载一次） ─────────────────
const roleOptions = ref<SelectOption[]>([])
const departmentOptions = ref<SelectOption[]>([])
const targetOptionsLoaded = ref(false)

async function loadTargetOptions() {
  if (targetOptionsLoaded.value) {
    return
  }
  try {
    const [roles, departments] = await Promise.all([
      roleApi.enabledList({ limit: 200 }),
      departmentApi.page(createPageRequest({ page: { pageIndex: 1, pageSize: 200 } })),
    ])
    roleOptions.value = roles.map(r => ({ label: r.roleName, value: String(r.basicId) }))
    departmentOptions.value = departments.items.map(d => ({ label: d.departmentName, value: String(d.basicId) }))
    targetOptionsLoaded.value = true
  }
  catch (e) {
    message.error((e as Error).message || t('message.notification.msg_load_failed'))
  }
}

// ── 表单 ─────────────────────────────────────────────────────────
interface NotificationFormModel {
  basicId?: ApiId
  title: string
  content: string | null
  notificationType: NotificationType
  priority: NotificationPriority
  contentFormat: NotificationContentFormat
  targetType: NotificationTargetType
  userIds: string[]
  icon: string | null
  link: string | null
  startTime: number | null
  expirationTime: number | null
  needConfirm: boolean
  isMandatory: boolean
  isBanner: boolean
  isPopup: boolean
  // 编辑透传字段（表单不暴露，保持原值不被覆盖丢失）
  businessType: string | null
  businessId: ApiId | null
  remark: string | null
}

function createDefaultForm(): NotificationFormModel {
  return {
    title: '',
    content: null,
    notificationType: NotificationType.System,
    priority: NotificationPriority.Normal,
    contentFormat: NotificationContentFormat.Markdown,
    targetType: NotificationTargetType.All,
    userIds: [],
    icon: null,
    link: null,
    startTime: null,
    expirationTime: null,
    needConfirm: false,
    isMandatory: false,
    isBanner: false,
    isPopup: false,
    businessType: null,
    businessId: null,
    remark: null,
  }
}

const modalVisible = ref(false)
const submitLoading = ref(false)
const notificationForm = ref<NotificationFormModel>(createDefaultForm())
const modalTitle = computed(() => (notificationForm.value.basicId ? t('message.notification.edit_title') : t('message.notification.add_title')))
const isUserTarget = computed(() => notificationForm.value.targetType === NotificationTargetType.User)
const isRoleTarget = computed(() => notificationForm.value.targetType === NotificationTargetType.Role)
const isDepartmentTarget = computed(() => notificationForm.value.targetType === NotificationTargetType.Department)
const isMarkdownContent = computed(() => notificationForm.value.contentFormat === NotificationContentFormat.Markdown)
/** XMdEditor 需要 string；表单 content 为 string|null，做空值适配 */
const markdownContent = computed<string>({
  get: () => notificationForm.value.content ?? '',
  set: (v) => { notificationForm.value.content = v || null },
})

const detailVisible = ref(false)
const currentDetail = ref<NotificationDetailDto | null>(null)

// ── 字段单一事实源：列 + 搜索 ────────────────────────────────────
const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('message.notification.col_keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('message.notification.search_keyword_placeholder'), order: 0 },
  { key: 'title', title: t('message.notification.col_title'), dataType: 'string', sortable: true, minWidth: 220, order: 10 },
  {
    key: 'notificationType',
    title: t('message.notification.col_type'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    dictionaryCode: 'NotificationType',
    options: notificationTypeOptions.value,
    searchPlaceholder: t('message.notification.search_type_placeholder'),
    width: 110,
    order: 11,
    render: (row) => {
      const r = row as unknown as NotificationListItemDto
      return h(
        NTag,
        { size: 'small', round: true, bordered: false, type: NOTIFICATION_TYPE_TAG[r.notificationType] ?? 'default' },
        () => getOptionLabel(notificationTypeOptions.value, r.notificationType),
      )
    },
  },
  {
    key: 'targetType',
    title: t('message.notification.col_target_type'),
    dataType: 'enum',
    sortable: true,
    options: targetTypeOptions.value,
    width: 110,
    order: 12,
    render: row => getOptionLabel(targetTypeOptions.value, (row as unknown as NotificationListItemDto).targetType),
  },
  {
    key: 'isPublished',
    title: t('message.notification.col_is_published'),
    dataType: 'boolean',
    searchable: true,
    sortable: true,
    options: publishedOptions.value,
    searchPlaceholder: t('message.notification.search_published_placeholder'),
    width: 100,
    order: 13,
    render: (row) => {
      const published = (row as unknown as NotificationListItemDto).isPublished
      return h(
        NTag,
        { size: 'small', round: true, bordered: false, type: published ? 'success' : 'default' },
        () => published ? t('message.notification.published') : t('message.notification.unpublished'),
      )
    },
  },
  { key: 'sendTime', title: t('message.notification.col_send_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 14 },
  { key: 'expirationTime', title: t('message.notification.col_expiration_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 15 },
  { key: 'createdTime', title: t('message.notification.col_created_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 16 },
])

function toStr(v: unknown): string | undefined {
  return (v as string | undefined)?.trim() || undefined
}

const schema = computed<PageSchema>(() => ({
  pageCode: 'message.notification',
  exportPermission: 'saas:notification:export',
  pageName: t('message.notification.page_name'),
  rowKey: 'basicId',
  scrollX: 1300,
  fields: fields.value,
  resource: {
    page: (params) => {
      const f = params.filters
      return notificationApi.page({
        ...createPageRequest({
          page: { pageIndex: params.page, pageSize: params.pageSize },
          // 排序 + 多选(notificationType)等通用过滤统一走 conditions
          conditions: { sorts: querySortsFromSchema(params.sorts), filters: params.conditionFilters ?? [] },
        }),
        keyword: toStr(f.keyword),
        // notificationType 改为多选，经 conditions.filters In 下发（不再走 DTO 顶层单值字段）
        isPublished: f.isPublished === undefined || f.isPublished === null || f.isPublished === ''
          ? undefined
          : Boolean(Number(f.isPublished)),
      }) as unknown as Promise<PageResult<Record<string, unknown>>>
    },
  },
  actions: [
    { key: 'create', title: t('message.notification.action_create'), scope: 'page', type: 'primary', icon: 'lucide:plus', permission: 'saas:message:create' },
    { key: 'view', title: t('message.notification.action_view'), scope: 'row', icon: 'lucide:eye' },
    { key: 'stats', title: t('message.notification.action_stats'), scope: 'row', icon: 'lucide:bar-chart-3', permission: 'saas:message:read', visible: isPublished },
    { key: 'edit', title: t('message.notification.action_edit'), scope: 'row', icon: 'lucide:pen', permission: 'saas:message:update', visible: isUnpublished },
    { key: 'publish', title: t('message.notification.action_publish'), scope: 'row', type: 'primary', icon: 'lucide:send', permission: 'saas:message:publish', visible: isUnpublished },
    { key: 'delete', title: t('message.notification.action_delete'), scope: 'row', type: 'error', icon: 'lucide:trash-2', permission: 'saas:message:delete', confirm: true, confirmText: t('message.notification.confirm_delete') },
  ],
}))

function isUnpublished(row: unknown): boolean {
  return !(row as NotificationListItemDto).isPublished
}

function isPublished(row: unknown): boolean {
  return (row as NotificationListItemDto).isPublished === true
}

function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as NotificationListItemDto | undefined
  if (payload.scope === 'page' && payload.key === 'create') {
    notificationForm.value = createDefaultForm()
    void loadTargetOptions()
    modalVisible.value = true
    return
  }
  if (payload.scope === 'row' && row) {
    if (payload.key === 'view')
      void openDetail(row)
    else if (payload.key === 'stats')
      void openStats(row)
    else if (payload.key === 'edit')
      void openEdit(row)
    else if (payload.key === 'publish')
      confirmPublish(row)
    else if (payload.key === 'delete')
      void removeRow(row)
  }
}

// ── 详情（抽屉） ─────────────────────────────────────────────────
async function openDetail(row: NotificationListItemDto) {
  try {
    currentDetail.value = await notificationApi.detail(row.basicId)
    if (!currentDetail.value) {
      message.error(t('message.notification.msg_not_found'))
      return
    }
    detailVisible.value = true
  }
  catch (e) {
    message.error((e as Error).message || t('message.notification.msg_load_detail_failed'))
  }
}

// ── 编辑（未发布才可） ───────────────────────────────────────────
async function openEdit(row: NotificationListItemDto) {
  try {
    const detail = await notificationApi.detail(row.basicId)
    if (!detail) {
      message.error(t('message.notification.msg_not_found'))
      return
    }
    if (detail.isPublished) {
      message.warning(t('message.notification.msg_published_cannot_edit'))
      return
    }
    notificationForm.value = {
      basicId: detail.basicId,
      title: detail.title,
      content: detail.content ?? null,
      notificationType: detail.notificationType,
      priority: detail.priority,
      contentFormat: detail.contentFormat,
      targetType: detail.targetType,
      userIds: [],
      icon: detail.icon ?? null,
      link: detail.link ?? null,
      startTime: detail.startTime ? new Date(detail.startTime).getTime() : null,
      expirationTime: detail.expirationTime ? new Date(detail.expirationTime).getTime() : null,
      needConfirm: detail.needConfirm,
      isMandatory: detail.isMandatory,
      isBanner: detail.isBanner,
      isPopup: detail.isPopup,
      businessType: detail.businessType ?? null,
      businessId: detail.businessId ?? null,
      remark: detail.remark ?? null,
    }
    void loadTargetOptions()
    modalVisible.value = true
  }
  catch (e) {
    message.error((e as Error).message || t('message.notification.msg_load_failed'))
  }
}

// ── 发布（确认对话框；目标范围沿用创建时设定） ───────────────────
function confirmPublish(row: NotificationListItemDto) {
  const targetText = row.targetType === NotificationTargetType.All
    ? t('message.notification.publish_target_all')
    : t('message.notification.publish_target_user')
  dialog.warning({
    title: t('message.notification.publish_dialog_title'),
    content: t('message.notification.publish_confirm_content', { title: row.title, target: targetText }),
    positiveText: t('message.notification.publish_positive'),
    negativeText: t('common.actions.cancel'),
    onPositiveClick: async () => {
      try {
        const result = await notificationApi.publish({ basicId: row.basicId })
        message.success(t('message.notification.msg_publish_success', { count: result.recipientCount }))
        void schemaPageRef.value?.reload()
      }
      catch (e) {
        message.error((e as Error).message || t('message.notification.msg_publish_failed'))
      }
    },
  })
}

// ── 删除 ─────────────────────────────────────────────────────────
async function removeRow(row: NotificationListItemDto) {
  try {
    await notificationApi.delete(row.basicId)
    message.success(t('message.notification.msg_delete_success'))
    void schemaPageRef.value?.reload()
  }
  catch (e) {
    message.error((e as Error).message || t('message.notification.msg_delete_failed'))
  }
}

// ── 运营数据（抽屉） ─────────────────────────────────────────────
const STATS_PAGE_SIZE = 10
const STATS_EXPORT_LIMIT = 1000

const statsVisible = ref(false)
const statsLoading = ref(false)
const statsRow = ref<{ id: ApiId, title: string } | null>(null)
const readStats = ref<NotificationReadStatsDto | null>(null)
const unreadUsers = ref<NotificationUnreadUserDto[]>([])
const unreadTotal = ref(0)
const unreadPage = ref(1)
const remindLoading = ref(false)
const exportLoading = ref(false)

const readRate = computed(() => {
  const stats = readStats.value
  if (!stats || stats.recipientCount <= 0) {
    return 0
  }
  return Math.round((stats.readCount / stats.recipientCount) * 100)
})

const unreadColumns = computed<DataTableColumns<NotificationUnreadUserDto>>(() => [
  { key: 'userName', title: t('message.notification.col_user_name'), minWidth: 140 },
  { key: 'realName', title: t('message.notification.col_real_name'), minWidth: 120, render: row => row.realName || '-' },
  { key: 'receivedTime', title: t('message.notification.col_received_time'), minWidth: 170, render: row => formatDate(row.receivedTime) },
])

async function loadUnreadUsers(id: ApiId, page: number) {
  const result = await notificationApi.unreadUserPage({
    ...createPageRequest({ page: { pageIndex: page, pageSize: STATS_PAGE_SIZE } }),
    notificationId: id,
  })
  unreadUsers.value = result.items
  unreadTotal.value = result.page.totalCount
}

async function openStats(row: NotificationListItemDto) {
  statsRow.value = { id: row.basicId, title: row.title }
  readStats.value = null
  unreadUsers.value = []
  unreadTotal.value = 0
  unreadPage.value = 1
  statsVisible.value = true
  statsLoading.value = true
  try {
    const [stats] = await Promise.all([
      notificationApi.readStats(row.basicId),
      loadUnreadUsers(row.basicId, 1),
    ])
    readStats.value = stats
  }
  catch (e) {
    message.error((e as Error).message || t('message.notification.msg_load_failed'))
  }
  finally {
    statsLoading.value = false
  }
}

async function handleUnreadPageChange(page: number) {
  if (!statsRow.value) {
    return
  }
  unreadPage.value = page
  statsLoading.value = true
  try {
    await loadUnreadUsers(statsRow.value.id, page)
  }
  catch (e) {
    message.error((e as Error).message || t('message.notification.msg_load_failed'))
  }
  finally {
    statsLoading.value = false
  }
}

async function confirmRemind() {
  const row = statsRow.value
  if (!row) {
    return
  }
  remindLoading.value = true
  try {
    const result = await notificationApi.remind(row.id)
    message.success(t('message.notification.stats_remind_success', { count: result.recipientCount }))
    readStats.value = await notificationApi.readStats(row.id)
  }
  catch (e) {
    message.error((e as Error).message || t('message.notification.msg_publish_failed'))
  }
  finally {
    remindLoading.value = false
  }
}

function csvCell(value: string): string {
  return `"${value.replace(/"/g, '""')}"`
}

async function exportUnread() {
  const row = statsRow.value
  if (!row) {
    return
  }
  exportLoading.value = true
  try {
    const result = await notificationApi.unreadUserPage({
      ...createPageRequest({ page: { pageIndex: 1, pageSize: STATS_EXPORT_LIMIT } }),
      notificationId: row.id,
    })
    if (result.page.totalCount > STATS_EXPORT_LIMIT) {
      message.warning(t('message.notification.stats_export_truncated'))
    }
    const header = [
      t('message.notification.col_user_name'),
      t('message.notification.col_real_name'),
      t('message.notification.col_received_time'),
    ]
    const lines = [header, ...result.items.map(u => [u.userName, u.realName || '-', formatDate(u.receivedTime)])]
      .map(cols => cols.map(csvCell).join(','))
    // 加 UTF-8 BOM 防中文乱码
    const bom = String.fromCharCode(0xFEFF)
    const csv = `${bom}${lines.join('\r\n')}`
    downloadBlob(new Blob([csv], { type: 'text/csv;charset=utf-8' }), `unread-users-${row.id}.csv`)
  }
  catch (e) {
    message.error((e as Error).message || t('message.notification.msg_load_failed'))
  }
  finally {
    exportLoading.value = false
  }
}

// ── 新增/编辑提交 ────────────────────────────────────────────────
function validateForm(form: NotificationFormModel): boolean {
  if (!form.title.trim()) {
    message.warning(t('message.notification.msg_title_required'))
    return false
  }
  const needTarget = form.targetType === NotificationTargetType.User
    || form.targetType === NotificationTargetType.Role
    || form.targetType === NotificationTargetType.Department
  if (needTarget) {
    if (form.userIds.length === 0) {
      message.warning(t('message.notification.msg_user_required'))
      return false
    }
    // 手填用户 ID 才校验正整数；角色/部门为选择来的 ID，非空即可
    if (form.targetType === NotificationTargetType.User
      && form.userIds.some(id => !/^[1-9]\d*$/.test(id.trim()))) {
      message.warning(t('message.notification.msg_user_id_invalid'))
      return false
    }
  }
  return true
}

async function handleSubmit() {
  const form = notificationForm.value
  if (!validateForm(form)) {
    return
  }

  // userIds 复用为「目标 ID 列表」：User=手填用户ID，Role/Department=所选角色/部门ID
  const userIds = form.targetType === NotificationTargetType.User
    || form.targetType === NotificationTargetType.Role
    || form.targetType === NotificationTargetType.Department
    ? form.userIds.map(id => id.trim())
    : []
  const expirationTime: DateTimeString | null = form.expirationTime
    ? new Date(form.expirationTime).toISOString()
    : null
  const startTime: DateTimeString | null = form.startTime
    ? new Date(form.startTime).toISOString()
    : null

  submitLoading.value = true
  try {
    if (form.basicId) {
      await notificationApi.update({
        basicId: form.basicId,
        title: form.title.trim(),
        content: toStr(form.content) ?? null,
        notificationType: form.notificationType,
        priority: form.priority,
        contentFormat: form.contentFormat,
        targetType: form.targetType,
        userIds,
        icon: toStr(form.icon) ?? null,
        link: toStr(form.link) ?? null,
        sendTime: null,
        startTime,
        expirationTime,
        needConfirm: form.needConfirm,
        isMandatory: form.isMandatory,
        isBanner: form.isBanner,
        isPopup: form.isPopup,
        businessType: form.businessType,
        businessId: form.businessId,
        remark: form.remark,
      })
      message.success(t('message.notification.msg_update_success'))
    }
    else {
      await notificationApi.create({
        title: form.title.trim(),
        content: toStr(form.content) ?? null,
        notificationType: form.notificationType,
        priority: form.priority,
        contentFormat: form.contentFormat,
        targetType: form.targetType,
        userIds,
        icon: toStr(form.icon) ?? null,
        link: toStr(form.link) ?? null,
        sendTime: null,
        startTime,
        expirationTime,
        needConfirm: form.needConfirm,
        isMandatory: form.isMandatory,
        isBanner: form.isBanner,
        isPopup: form.isPopup,
        templateCode: null,
        templateParams: null,
        publishImmediately: false,
        businessType: null,
        businessId: null,
        remark: null,
      })
      message.success(t('message.notification.msg_create_success'))
    }
    modalVisible.value = false
    void schemaPageRef.value?.reload()
  }
  catch (e) {
    message.error((e as Error).message || t('message.notification.msg_save_failed'))
  }
  finally {
    submitLoading.value = false
  }
}
</script>

<template>
  <SchemaPage ref="schemaPageRef" :schema="schema" @action="onAction">
    <!-- 新增/编辑 -->
    <NModal
      v-model:show="modalVisible"
      preset="card"
      :title="modalTitle"
      style="width: 680px"
    >
      <NForm :model="notificationForm" label-placement="top">
        <NFormItem :label="t('message.notification.form_title')" path="title">
          <NInput v-model:value="notificationForm.title" clearable :maxlength="200" :placeholder="t('message.notification.form_title_placeholder')" />
        </NFormItem>
        <div class="grid grid-cols-2 gap-x-4">
          <NFormItem :label="t('message.notification.form_priority')" path="priority">
            <NSelect v-model:value="notificationForm.priority" :options="priorityOptions" />
          </NFormItem>
          <NFormItem :label="t('message.notification.form_content_format')" path="contentFormat">
            <NSelect v-model:value="notificationForm.contentFormat" :options="contentFormatOptions" />
          </NFormItem>
        </div>
        <NFormItem :label="t('message.notification.form_content')" path="content">
          <XMdEditor v-if="isMarkdownContent" v-model="markdownContent" />
          <NInput
            v-else
            v-model:value="notificationForm.content"
            type="textarea"
            :autosize="{ minRows: 5, maxRows: 12 }"
            :placeholder="t('message.notification.form_content_placeholder')"
          />
        </NFormItem>
        <div class="grid grid-cols-2 gap-x-4">
          <NFormItem :label="t('message.notification.form_type')" path="notificationType">
            <NSelect v-model:value="notificationForm.notificationType" :options="notificationTypeOptions" />
          </NFormItem>
          <NFormItem :label="t('message.notification.form_target_type')" path="targetType">
            <NSelect v-model:value="notificationForm.targetType" :options="targetTypeFormOptions" />
          </NFormItem>
        </div>
        <NFormItem v-if="isUserTarget" :label="t('message.notification.form_user_ids')" path="userIds">
          <NDynamicTags v-model:value="notificationForm.userIds" />
        </NFormItem>
        <NFormItem v-else-if="isRoleTarget" :label="t('message.notification.form_role_ids')" path="userIds">
          <NSelect v-model:value="notificationForm.userIds" multiple :options="roleOptions" />
        </NFormItem>
        <NFormItem v-else-if="isDepartmentTarget" :label="t('message.notification.form_department_ids')" path="userIds">
          <NSelect v-model:value="notificationForm.userIds" multiple :options="departmentOptions" />
        </NFormItem>
        <div class="grid grid-cols-2 gap-x-4">
          <NFormItem :label="t('message.notification.form_icon')" path="icon">
            <IconPicker v-model="notificationForm.icon" :placeholder="t('message.notification.form_icon_placeholder')" />
          </NFormItem>
          <NFormItem :label="t('message.notification.form_link')" path="link">
            <NInput v-model:value="notificationForm.link" clearable :maxlength="500" :placeholder="t('message.notification.form_link_placeholder')" />
          </NFormItem>
        </div>
        <div class="grid grid-cols-2 gap-x-4">
          <NFormItem :label="t('message.notification.form_start_time')" path="startTime">
            <NDatePicker
              v-model:value="notificationForm.startTime"
              type="datetime"
              clearable
              style="width: 100%"
            />
          </NFormItem>
          <NFormItem :label="t('message.notification.form_expiration_time')" path="expirationTime">
            <NDatePicker
              v-model:value="notificationForm.expirationTime"
              type="datetime"
              clearable
              style="width: 100%"
              :placeholder="t('message.notification.form_expiration_placeholder')"
            />
          </NFormItem>
        </div>
        <div class="grid grid-cols-2 gap-x-4">
          <NFormItem :label="t('message.notification.form_need_confirm')" path="needConfirm">
            <NSwitch v-model:value="notificationForm.needConfirm" />
          </NFormItem>
          <NFormItem :label="t('message.notification.form_mandatory')" path="isMandatory">
            <NSwitch v-model:value="notificationForm.isMandatory" />
          </NFormItem>
          <NFormItem :label="t('message.notification.form_banner')" path="isBanner">
            <NSwitch v-model:value="notificationForm.isBanner" />
          </NFormItem>
          <NFormItem :label="t('message.notification.form_popup')" path="isPopup">
            <NSwitch v-model:value="notificationForm.isPopup" />
          </NFormItem>
        </div>
        <p v-if="isUserTarget && notificationForm.basicId" class="form-hint">
          {{ t('message.notification.edit_user_hint') }}
        </p>
      </NForm>
      <template #footer>
        <div class="flex justify-end gap-2">
          <NButton size="small" @click="modalVisible = false">
            {{ t('common.actions.cancel') }}
          </NButton>
          <NButton size="small" type="primary" :loading="submitLoading" @click="handleSubmit">
            {{ t('common.actions.save') }}
          </NButton>
        </div>
      </template>
    </NModal>

    <!-- 详情（抽屉） -->
    <NDrawer v-model:show="detailVisible" :width="560">
      <NDrawerContent :title="t('message.notification.detail_title')" closable>
        <template v-if="currentDetail">
          <NDescriptions :column="2" label-placement="left" bordered size="small">
            <NDescriptionsItem :label="t('message.notification.detail.label.title')" :span="2">
              {{ currentDetail.title }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('message.notification.detail.label.type')">
              {{ getOptionLabel(notificationTypeOptions, currentDetail.notificationType) }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('message.notification.detail.label.target_type')">
              {{ getOptionLabel(targetTypeOptions, currentDetail.targetType) }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('message.notification.detail.label.is_published')">
              <NTag size="small" round :bordered="false" :type="currentDetail.isPublished ? 'success' : 'default'">
                {{ currentDetail.isPublished ? t('message.notification.published') : t('message.notification.unpublished') }}
              </NTag>
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('message.notification.detail.label.priority')">
              {{ getOptionLabel(priorityOptions, currentDetail.priority) }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('message.notification.detail.label.need_confirm')">
              {{ currentDetail.needConfirm ? t('common.statuses.yes') : t('common.statuses.no') }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('message.notification.detail.label.mandatory')">
              {{ currentDetail.isMandatory ? t('common.statuses.yes') : t('common.statuses.no') }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('message.notification.detail.label.banner')">
              {{ currentDetail.isBanner ? t('common.statuses.yes') : t('common.statuses.no') }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('message.notification.detail.label.popup')">
              {{ currentDetail.isPopup ? t('common.statuses.yes') : t('common.statuses.no') }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('message.notification.detail.label.start_time')">
              {{ currentDetail.startTime ? formatDate(currentDetail.startTime) : '-' }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('message.notification.detail.label.send_time')">
              {{ formatDate(currentDetail.sendTime) }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('message.notification.detail.label.expiration_time')">
              {{ currentDetail.expirationTime ? formatDate(currentDetail.expirationTime) : t('message.notification.never_expire') }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('message.notification.detail.label.icon')">
              {{ currentDetail.icon || '-' }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('message.notification.detail.label.link')">
              {{ currentDetail.link || '-' }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('message.notification.detail.label.business_type')">
              {{ currentDetail.businessType || '-' }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('message.notification.detail.label.business_id')">
              {{ currentDetail.businessId || '-' }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('message.notification.detail.label.creator')">
              {{ currentDetail.createdBy || '-' }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('message.notification.detail.label.created_time')">
              {{ formatDate(currentDetail.createdTime) }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('message.notification.detail.label.remark')" :span="2">
              {{ currentDetail.remark || '-' }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('message.notification.detail.label.content')" :span="2">
              <NotificationContent
                v-if="currentDetail.content"
                :content="currentDetail.content"
                :format="currentDetail.contentFormat"
              />
              <span v-else>{{ t('message.notification.detail_no_content') }}</span>
            </NDescriptionsItem>
          </NDescriptions>
        </template>
      </NDrawerContent>
    </NDrawer>

    <!-- 运营数据（抽屉） -->
    <NDrawer v-model:show="statsVisible" :width="640">
      <NDrawerContent :title="statsRow ? `${t('message.notification.stats_title')} · ${statsRow.title}` : t('message.notification.stats_title')" closable>
        <div v-if="readStats" class="stats">
          <!-- 统计区 -->
          <div class="stats__cards">
            <NStatistic :label="t('message.notification.stats_recipient')" :value="readStats.recipientCount" />
            <NStatistic :label="t('message.notification.stats_read')" :value="readStats.readCount" />
            <NStatistic :label="t('message.notification.stats_unread')" :value="readStats.unreadCount" />
            <NStatistic v-if="readStats.needConfirm" :label="t('message.notification.stats_confirm')" :value="readStats.confirmCount" />
          </div>
          <div class="stats__rate">
            <span class="stats__rate-label">{{ t('message.notification.stats_read_rate') }}</span>
            <NProgress type="line" :percentage="readRate" :height="12" />
          </div>
          <!-- 操作区 -->
          <div class="stats__ops">
            <NPopconfirm @positive-click="confirmRemind">
              <template #trigger>
                <NButton size="small" type="primary" :loading="remindLoading">
                  {{ t('message.notification.stats_remind') }}
                </NButton>
              </template>
              {{ t('message.notification.stats_remind_confirm', { count: readStats.unreadCount }) }}
            </NPopconfirm>
            <NButton size="small" :loading="exportLoading" @click="exportUnread">
              {{ t('message.notification.stats_export') }}
            </NButton>
          </div>
          <!-- 未读人员区 -->
          <div class="stats__section-title">
            {{ t('message.notification.stats_unread_users') }}
          </div>
          <NDataTable
            :columns="unreadColumns"
            :data="unreadUsers"
            :loading="statsLoading"
            :row-key="(row: NotificationUnreadUserDto) => String(row.userId)"
            size="small"
          />
          <div class="stats__pager">
            <NPagination
              :page="unreadPage"
              :item-count="unreadTotal"
              :page-size="STATS_PAGE_SIZE"
              :page-slot="5"
              size="small"
              @update:page="handleUnreadPageChange"
            />
          </div>
        </div>
      </NDrawerContent>
    </NDrawer>
  </SchemaPage>
</template>

<style scoped>
.form-hint {
  margin: 0 0 8px;
  font-size: 12px;
  color: hsl(var(--warning, 38 92% 50%));
}

.stats {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.stats__cards {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 12px;
}

.stats__rate {
  display: flex;
  align-items: center;
  gap: 12px;
}

.stats__rate-label {
  flex: none;
  font-size: 13px;
  color: var(--text-color-3, #999);
}

.stats__ops {
  display: flex;
  gap: 8px;
}

.stats__section-title {
  font-size: 14px;
  font-weight: 600;
}

.stats__pager {
  display: flex;
  justify-content: flex-end;
}
</style>
