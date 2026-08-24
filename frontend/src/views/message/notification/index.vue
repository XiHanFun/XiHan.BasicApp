<script setup lang="ts">
import type {
  ApiId,
  DateTimeString,
  NotificationDetailDto,
  NotificationListItemDto,
  NotificationReadStatsDto,
  NotificationUnreadUserDto,
  PageResult,
} from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, XDataTableColumn } from '~/components'
import type { SelectOption } from '~/types'
import { XhBadge, XhButton, XhCheckboxGroupIndicator, XhCheckboxGroupItem, XhCheckboxGroupItemText, XhCheckboxGroupRoot, XhCheckboxGroupTrigger, XhDescriptionsItem, XhDescriptionsLabel, XhDescriptionsRoot, XhDescriptionsValue, XhDrawerCloseTrigger, XhDrawerContent, XhDrawerRoot, XhDrawerTitle, XhFieldControl, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFormFieldGroup, XhFormRoot, XhPopconfirmCancelTrigger, XhPopconfirmConfirmTrigger, XhPopconfirmContent, XhPopconfirmDescription, XhPopconfirmPositioner, XhPopconfirmRoot, XhPopconfirmTrigger, XhSwitch } from '@xihan-ui/vue'
import { computed, h, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  createPageRequest,
  departmentApi,
  MessageChannel,
  notificationApi,
  NotificationContentFormat,
  NotificationPriority,
  NotificationTargetType,
  NotificationType,
  querySortsFromSchema,
  roleApi,
} from '@/api'
import { Icon, IconPicker, NotificationContent, SchemaPage, SchemaPagination, XContentEditorField, XDataTable, XDatePicker, XEditModal, XInput, XMdEditor, XSelect, XTagsInput } from '~/components'
import { dialog, toast } from '~/composables'
import { useEnumOptions } from '~/hooks'
import { downloadBlob, formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'MessageNotificationPage' })

const { t } = useI18n()

/** 编辑弹窗的保存钮靠这个 id 关联到表单，点它才会走整表校验 */
const editFormId = useId()
const schemaPageRef = ref<InstanceType<typeof SchemaPage> | null>(null)

type TagType = 'neutral' | 'danger' | 'info' | 'success' | 'warning'

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
  [NotificationType.Todo]: 'neutral',
  [NotificationType.Emergency]: 'danger',
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

// ── 投递渠道（MessageChannel [Flags]：站内信固定勾选禁用，可叠加邮箱/短信/机器人） ──
const DELIVERY_CHANNEL_BITS = [
  MessageChannel.SiteNotification,
  MessageChannel.Email,
  MessageChannel.Sms,
  MessageChannel.Bot,
] as const

const deliveryChannelOptions = computed(() => [
  { label: t('message.notification.channel_site'), value: MessageChannel.SiteNotification, disabled: true },
  { label: t('message.notification.channel_email'), value: MessageChannel.Email, disabled: false },
  { label: t('message.notification.channel_sms'), value: MessageChannel.Sms, disabled: false },
  { label: t('message.notification.channel_bot'), value: MessageChannel.Bot, disabled: false },
])

/** 位掩码 → 勾选数组（渲染列表/回填编辑表单） */
function channelsToArray(mask: MessageChannel): MessageChannel[] {
  return DELIVERY_CHANNEL_BITS.filter(bit => (mask & bit) === bit)
}

/** 勾选数组 → 位掩码（提交；兜底强制并入站内信位） */
function channelsToMask(values: MessageChannel[]): MessageChannel {
  return values.reduce<MessageChannel>((mask, bit) => mask | bit, MessageChannel.SiteNotification)
}

/** 位掩码 → 渠道标签文本（详情展示） */
function formatChannels(mask: MessageChannel): string {
  return channelsToArray(mask)
    .map(bit => getOptionLabel(deliveryChannelOptions.value, bit))
    .join(' / ')
}

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
      departmentApi.enabledList(),
    ])
    roleOptions.value = roles.map(r => ({ label: r.roleName, value: String(r.basicId) }))
    departmentOptions.value = departments.map(d => ({ label: d.departmentName, value: String(d.basicId) }))
    targetOptionsLoaded.value = true
  }
  catch (e) {
    toast.error((e as Error).message || t('message.notification.msg_load_failed'))
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
  /** 投递渠道勾选数组（提交时合成 MessageChannel 位掩码；站内信固定勾选） */
  deliveryChannels: MessageChannel[]
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
    deliveryChannels: [MessageChannel.SiteNotification],
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
/** 编辑器统一收发 string；表单 content 为 string|null，做空值适配 */
const contentText = computed<string>({
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
      return h(XhBadge, { variant: 'subtle', size: 'sm', tone: NOTIFICATION_TYPE_TAG[r.notificationType] ?? 'neutral' }, () => getOptionLabel(notificationTypeOptions.value, r.notificationType))
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
    key: 'deliveryChannels',
    title: t('message.notification.col_delivery_channels'),
    dataType: 'string',
    width: 170,
    order: 13,
    render: (row) => {
      const mask = (row as unknown as NotificationListItemDto).deliveryChannels ?? MessageChannel.SiteNotification
      return h(
        'div',
        { style: 'display:flex;flex-wrap:wrap;gap:4px' },
        channelsToArray(mask).map(bit =>
          h(XhBadge, { variant: 'subtle', key: bit, size: 'sm', tone: bit === MessageChannel.SiteNotification ? 'neutral' : 'info' }, () => getOptionLabel(deliveryChannelOptions.value, bit))),
      )
    },
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
    order: 14,
    render: (row) => {
      const published = (row as unknown as NotificationListItemDto).isPublished
      return h(XhBadge, { variant: 'subtle', size: 'sm', tone: published ? 'success' : 'neutral' }, () => published ? t('message.notification.published') : t('message.notification.unpublished'))
    },
  },
  { key: 'sendTime', title: t('message.notification.col_send_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 15 },
  { key: 'expirationTime', title: t('message.notification.col_expiration_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 16 },
  { key: 'createdTime', title: t('message.notification.col_created_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 17 },
])

function toStr(v: unknown): string | undefined {
  return (v as string | undefined)?.trim() || undefined
}

const schema = computed<PageSchema>(() => ({
  pageCode: 'message.notification',
  exportPermission: 'saas:notification:export',
  pageName: t('message.notification.page_name'),
  rowKey: 'basicId',
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
      toast.error(t('message.notification.msg_not_found'))
      return
    }
    detailVisible.value = true
  }
  catch (e) {
    toast.error((e as Error).message || t('message.notification.msg_load_detail_failed'))
  }
}

// ── 编辑（未发布才可） ───────────────────────────────────────────
async function openEdit(row: NotificationListItemDto) {
  try {
    const detail = await notificationApi.detail(row.basicId)
    if (!detail) {
      toast.error(t('message.notification.msg_not_found'))
      return
    }
    if (detail.isPublished) {
      toast.warning(t('message.notification.msg_published_cannot_edit'))
      return
    }
    notificationForm.value = {
      basicId: detail.basicId,
      title: detail.title,
      content: detail.content ?? null,
      notificationType: detail.notificationType,
      priority: detail.priority,
      contentFormat: detail.contentFormat,
      deliveryChannels: channelsToArray(detail.deliveryChannels ?? MessageChannel.SiteNotification),
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
    toast.error((e as Error).message || t('message.notification.msg_load_failed'))
  }
}

// ── 发布（确认对话框；目标范围沿用创建时设定） ───────────────────
function confirmPublish(row: NotificationListItemDto) {
  const targetText = row.targetType === NotificationTargetType.All
    ? t('message.notification.publish_target_all')
    : t('message.notification.publish_target_user')
  void dialog.confirm({
    badge: 'warning',
    title: t('message.notification.publish_dialog_title'),
    content: t('message.notification.publish_confirm_content', { title: row.title, target: targetText }),
    okText: t('message.notification.publish_positive'),
    cancelText: t('common.actions.cancel'),
    onOk: async () => {
      try {
        const result = await notificationApi.publish({ basicId: row.basicId })
        toast.success(t('message.notification.msg_publish_success', { count: result.recipientCount }))
        void schemaPageRef.value?.reload()
      }
      catch (e) {
        toast.error((e as Error).message || t('message.notification.msg_publish_failed'))
      }
    },
  })
}

// ── 删除 ─────────────────────────────────────────────────────────
async function removeRow(row: NotificationListItemDto) {
  try {
    await notificationApi.delete(row.basicId)
    toast.success(t('message.notification.msg_delete_success'))
    void schemaPageRef.value?.reload()
  }
  catch (e) {
    toast.error((e as Error).message || t('message.notification.msg_delete_failed'))
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

const unreadColumns = computed<XDataTableColumn<NotificationUnreadUserDto>[]>(() => [
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
    toast.error((e as Error).message || t('message.notification.msg_load_failed'))
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
    toast.error((e as Error).message || t('message.notification.msg_load_failed'))
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
    toast.success(t('message.notification.stats_remind_success', { count: result.recipientCount }))
    readStats.value = await notificationApi.readStats(row.id)
  }
  catch (e) {
    toast.error((e as Error).message || t('message.notification.msg_publish_failed'))
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
      toast.warning(t('message.notification.stats_export_truncated'))
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
    toast.error((e as Error).message || t('message.notification.msg_load_failed'))
  }
  finally {
    exportLoading.value = false
  }
}

// ── 新增/编辑提交 ────────────────────────────────────────────────
function validateForm(form: NotificationFormModel): boolean {
  if (!form.title.trim()) {
    toast.warning(t('message.notification.msg_title_required'))
    return false
  }
  const needTarget = form.targetType === NotificationTargetType.User
    || form.targetType === NotificationTargetType.Role
    || form.targetType === NotificationTargetType.Department
  if (needTarget) {
    if (form.userIds.length === 0) {
      toast.warning(t('message.notification.msg_user_required'))
      return false
    }
    // 手填用户 ID 才校验正整数；角色/部门为选择来的 ID，非空即可
    if (form.targetType === NotificationTargetType.User
      && form.userIds.some(id => !/^[1-9]\d*$/.test(id.trim()))) {
      toast.warning(t('message.notification.msg_user_id_invalid'))
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
        deliveryChannels: channelsToMask(form.deliveryChannels),
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
      toast.success(t('message.notification.msg_update_success'))
    }
    else {
      await notificationApi.create({
        title: form.title.trim(),
        content: toStr(form.content) ?? null,
        notificationType: form.notificationType,
        priority: form.priority,
        contentFormat: form.contentFormat,
        deliveryChannels: channelsToMask(form.deliveryChannels),
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
      toast.success(t('message.notification.msg_create_success'))
    }
    modalVisible.value = false
    void schemaPageRef.value?.reload()
  }
  catch (e) {
    toast.error((e as Error).message || t('message.notification.msg_save_failed'))
  }
  finally {
    submitLoading.value = false
  }
}
</script>

<template>
  <SchemaPage ref="schemaPageRef" :schema="schema" @action="onAction">
    <!-- 新增/编辑 -->
    <XEditModal
      v-model:show="modalVisible"
      :title="modalTitle"
      :loading="submitLoading"
      :form-id="editFormId"
    >
      <XhFormRoot
        :id="editFormId"
        v-model:values="notificationForm"
        validate-on="blur"
        class="xh-edit-form-grid"
        @submit="handleSubmit"
      >
        <XhFormFieldGroup value="title" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.notification.form_title') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="notificationForm.title" clearable :max-length="200" :placeholder="t('message.notification.form_title_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="priority">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.notification.form_priority') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect v-model:value="notificationForm.priority" :options="priorityOptions" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="contentFormat">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.notification.form_content_format') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect v-model:value="notificationForm.contentFormat" :options="contentFormatOptions" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="content" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.notification.form_content') }}</XhFieldLabel>
            <XhFieldControl :as-child="false">
              <XContentEditorField
                v-model="contentText"
                :title="t('message.notification.form_content_drawer_title')"
                :placeholder="t('message.notification.form_content_placeholder')"
                :edit-text="t('message.notification.form_content_edit')"
                :confirm-text="t('common.actions.confirm')"
                :cancel-text="t('common.actions.cancel')"
                :count-label="(count: number) => t('message.notification.form_content_count', { count })"
              >
                <template #editor="{ value, update }">
                  <XMdEditor
                    v-if="isMarkdownContent"
                    :model-value="value"
                    @update:model-value="update"
                  />
                  <XInput
                    v-else
                    :value="value"
                    type="textarea"
                    :placeholder="t('message.notification.form_content_placeholder')"
                    @update:value="update"
                  />
                </template>
              </XContentEditorField>
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="notificationType">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.notification.form_type') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect v-model:value="notificationForm.notificationType" :options="notificationTypeOptions" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="targetType">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.notification.form_target_type') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect v-model:value="notificationForm.targetType" :options="targetTypeFormOptions" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="deliveryChannels" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.notification.form_delivery_channels') }}</XhFieldLabel>
            <div>
              <XhFieldControl>
                <XhCheckboxGroupRoot
                  :value="notificationForm.deliveryChannels.map(String)"
                  @update:value="(value: string[]) => (notificationForm.deliveryChannels = value as unknown as MessageChannel[])"
                >
                  <XhCheckboxGroupItem
                    v-for="option in deliveryChannelOptions"
                    :key="option.value"
                    :value="String(option.value)"
                  >
                    <XhCheckboxGroupTrigger>
                      <XhCheckboxGroupIndicator>
                        <Icon icon="lucide:check" width="12" height="12" />
                      </XhCheckboxGroupIndicator>
                    </XhCheckboxGroupTrigger>
                    <XhCheckboxGroupItemText>
                      {{ option.label }}
                    </XhCheckboxGroupItemText>
                  </XhCheckboxGroupItem>
                </XhCheckboxGroupRoot>
              </XhFieldControl>
              <p class="channel-hint">
                {{ t('message.notification.form_delivery_channels_hint') }}
              </p>
            </div>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup v-if="isUserTarget" value="userIds" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.notification.form_user_ids') }}</XhFieldLabel>
            <XhFieldControl>
              <XTagsInput v-model:value="notificationForm.userIds" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup v-else-if="isRoleTarget" value="userIds" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.notification.form_role_ids') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect v-model:value="notificationForm.userIds" multiple :options="roleOptions" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup v-else-if="isDepartmentTarget" value="userIds" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.notification.form_department_ids') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect v-model:value="notificationForm.userIds" multiple :options="departmentOptions" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="icon">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.notification.form_icon') }}</XhFieldLabel>
            <XhFieldControl :as-child="false">
              <IconPicker v-model="notificationForm.icon" :placeholder="t('message.notification.form_icon_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="link">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.notification.form_link') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="notificationForm.link" clearable :max-length="500" :placeholder="t('message.notification.form_link_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="startTime">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.notification.form_start_time') }}</XhFieldLabel>
            <XhFieldControl>
              <XDatePicker
                v-model:value="notificationForm.startTime"
                type="datetime"
                clearable
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="expirationTime">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.notification.form_expiration_time') }}</XhFieldLabel>
            <XhFieldControl>
              <XDatePicker
                v-model:value="notificationForm.expirationTime"
                type="datetime"
                clearable
                :placeholder="t('message.notification.form_expiration_placeholder')"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="needConfirm">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.notification.form_need_confirm') }}</XhFieldLabel>
            <XhFieldControl>
              <XhSwitch v-model:checked="notificationForm.needConfirm" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="isMandatory">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.notification.form_mandatory') }}</XhFieldLabel>
            <XhFieldControl>
              <XhSwitch v-model:checked="notificationForm.isMandatory" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="isBanner">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.notification.form_banner') }}</XhFieldLabel>
            <XhFieldControl>
              <XhSwitch v-model:checked="notificationForm.isBanner" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="isPopup">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.notification.form_popup') }}</XhFieldLabel>
            <XhFieldControl>
              <XhSwitch v-model:checked="notificationForm.isPopup" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <p v-if="isUserTarget && notificationForm.basicId" class="form-hint xh-span-2">
          {{ t('message.notification.edit_user_hint') }}
        </p>
      </XhFormRoot>
    </XEditModal>

    <!-- 详情（抽屉） -->
    <XhDrawerRoot v-model:open="detailVisible" side="right">
      <XhDrawerContent style="--xh-drawer-size: 560px">
        <XhDrawerTitle>{{ t('message.notification.detail_title') }}</XhDrawerTitle>
        <XhDrawerCloseTrigger />
        <template v-if="currentDetail">
          <XhDescriptionsRoot :columns="2" bordered placement="left" size="sm">
            <XhDescriptionsItem style="grid-column: span 2">
              <XhDescriptionsLabel>{{ t('message.notification.detail.label.title') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ currentDetail.title }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('message.notification.detail.label.type') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ getOptionLabel(notificationTypeOptions, currentDetail.notificationType) }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('message.notification.detail.label.target_type') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ getOptionLabel(targetTypeOptions, currentDetail.targetType) }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('message.notification.detail.label.delivery_channels') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ formatChannels(currentDetail.deliveryChannels ?? MessageChannel.SiteNotification) }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('message.notification.detail.label.is_published') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                <XhBadge variant="subtle" size="sm" :tone="currentDetail.isPublished ? 'success' : 'neutral'">
                  {{ currentDetail.isPublished ? t('message.notification.published') : t('message.notification.unpublished') }}
                </XhBadge>
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('message.notification.detail.label.priority') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ getOptionLabel(priorityOptions, currentDetail.priority) }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('message.notification.detail.label.need_confirm') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ currentDetail.needConfirm ? t('common.statuses.yes') : t('common.statuses.no') }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('message.notification.detail.label.mandatory') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ currentDetail.isMandatory ? t('common.statuses.yes') : t('common.statuses.no') }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('message.notification.detail.label.banner') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ currentDetail.isBanner ? t('common.statuses.yes') : t('common.statuses.no') }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('message.notification.detail.label.popup') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ currentDetail.isPopup ? t('common.statuses.yes') : t('common.statuses.no') }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('message.notification.detail.label.start_time') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ currentDetail.startTime ? formatDate(currentDetail.startTime) : '-' }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('message.notification.detail.label.send_time') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ formatDate(currentDetail.sendTime) }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('message.notification.detail.label.expiration_time') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ currentDetail.expirationTime ? formatDate(currentDetail.expirationTime) : t('message.notification.never_expire') }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('message.notification.detail.label.icon') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ currentDetail.icon || '-' }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('message.notification.detail.label.link') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ currentDetail.link || '-' }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('message.notification.detail.label.business_type') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ currentDetail.businessType || '-' }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('message.notification.detail.label.business_id') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ currentDetail.businessId || '-' }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('message.notification.detail.label.creator') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ currentDetail.createdBy || '-' }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('message.notification.detail.label.created_time') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ formatDate(currentDetail.createdTime) }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem style="grid-column: span 2">
              <XhDescriptionsLabel>{{ t('message.notification.detail.label.remark') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                {{ currentDetail.remark || '-' }}
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem style="grid-column: span 2">
              <XhDescriptionsLabel>{{ t('message.notification.detail.label.content') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                <NotificationContent
                  v-if="currentDetail.content"
                  :content="currentDetail.content"
                  :format="currentDetail.contentFormat"
                />
                <span v-else>{{ t('message.notification.detail_no_content') }}</span>
              </XhDescriptionsValue>
            </XhDescriptionsItem>
          </XhDescriptionsRoot>
        </template>
      </XhDrawerContent>
    </XhDrawerRoot>

    <!-- 运营数据（抽屉） -->
    <XhDrawerRoot v-model:open="statsVisible" side="right">
      <XhDrawerContent style="--xh-drawer-size: 640px">
        <XhDrawerTitle>{{ statsRow ? `${t('message.notification.stats_title')} · ${statsRow.title}` : t('message.notification.stats_title') }}</XhDrawerTitle>
        <XhDrawerCloseTrigger />
        <div v-if="readStats" class="stats">
          <!-- 统计区 -->
          <div class="stats__cards">
            <XhStatisticRoot>
              <XhStatisticLabel>{{ t('message.notification.stats_recipient') }}</XhStatisticLabel>
              <XhStatisticValue>{{ readStats.recipientCount }}</XhStatisticValue>
            </XhStatisticRoot>
            <XhStatisticRoot>
              <XhStatisticLabel>{{ t('message.notification.stats_read') }}</XhStatisticLabel>
              <XhStatisticValue>{{ readStats.readCount }}</XhStatisticValue>
            </XhStatisticRoot>
            <XhStatisticRoot>
              <XhStatisticLabel>{{ t('message.notification.stats_unread') }}</XhStatisticLabel>
              <XhStatisticValue>{{ readStats.unreadCount }}</XhStatisticValue>
            </XhStatisticRoot>
            <XhStatisticRoot v-if="readStats.needConfirm">
              <XhStatisticLabel>{{ t('message.notification.stats_confirm') }}</XhStatisticLabel>
              <XhStatisticValue>{{ readStats.confirmCount }}</XhStatisticValue>
            </XhStatisticRoot>
          </div>
          <div class="stats__rate">
            <span class="stats__rate-label">{{ t('message.notification.stats_read_rate') }}</span>
            <XhProgress variant="line" :value="readRate" :stroke-width="12" />
          </div>
          <!-- 操作区 -->
          <div class="stats__ops">
            <XhPopconfirmRoot @confirm="confirmRemind">
              <XhPopconfirmTrigger class="xh-linklike-trigger">
                {{ t('message.notification.stats_remind') }}
              </XhPopconfirmTrigger>
              <XhPopconfirmPositioner>
                <XhPopconfirmContent>
                  <XhPopconfirmDescription>{{ t('message.notification.stats_remind_confirm', { count: readStats.unreadCount }) }}</XhPopconfirmDescription>
                  <XhPopconfirmCancelTrigger>{{ t('common.actions.cancel') }}</XhPopconfirmCancelTrigger>
                  <XhPopconfirmConfirmTrigger>{{ t('common.actions.confirm') }}</XhPopconfirmConfirmTrigger>
                </XhPopconfirmContent>
              </XhPopconfirmPositioner>
            </XhPopconfirmRoot>
            <XhButton size="sm" :loading="exportLoading" @click="exportUnread">
              {{ t('message.notification.stats_export') }}
            </XhButton>
          </div>
          <!-- 未读人员区 -->
          <div class="stats__section-title">
            {{ t('message.notification.stats_unread_users') }}
          </div>
          <XDataTable
            :columns="unreadColumns"
            :data="unreadUsers"
            :loading="statsLoading"
            :row-key="(row: NotificationUnreadUserDto) => String(row.userId)"
            size="sm"
          />
          <div class="stats__pager">
            <SchemaPagination
              :page="unreadPage"
              :total="unreadTotal"
              :page-size="STATS_PAGE_SIZE" compact
              @update:page="handleUnreadPageChange"
            />
          </div>
        </div>
      </XhDrawerContent>
    </XhDrawerRoot>
  </SchemaPage>
</template>

<style scoped>
.form-hint {
  margin: 0 0 8px;
  font-size: 12px;
  color: hsl(var(--warning, 38 92% 50%));
}

.channel-hint {
  margin: 6px 0 0;
  font-size: 12px;
  color: var(--text-color-3, #999);
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
