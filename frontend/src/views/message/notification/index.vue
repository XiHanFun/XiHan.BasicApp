<script setup lang="ts">
import type {
  ApiId,
  DateTimeString,
  NotificationDetailDto,
  NotificationListItemDto,
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
  NDynamicTags,
  NForm,
  NFormItem,
  NInput,
  NModal,
  NSelect,
  NSwitch,
  NTag,
  useDialog,
  useMessage,
} from 'naive-ui'
import { computed, h, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  createPageRequest,
  notificationApi,
  NotificationTargetType,
  NotificationType,
} from '@/api'
import { SchemaPage } from '~/components'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'MessageNotificationPage' })

const { t } = useI18n()
const message = useMessage()
const dialog = useDialog()
const schemaPageRef = ref<InstanceType<typeof SchemaPage> | null>(null)

type TagType = 'default' | 'error' | 'info' | 'success' | 'warning'

// ── 选项 ─────────────────────────────────────────────────────────
const notificationTypeOptions = computed(() => [
  { label: t('message.notification.type_system'), value: NotificationType.System },
  { label: t('message.notification.type_user'), value: NotificationType.User },
  { label: t('message.notification.type_announcement'), value: NotificationType.Announcement },
  { label: t('message.notification.type_warning'), value: NotificationType.Warning },
  { label: t('message.notification.type_error'), value: NotificationType.Error },
])

const NOTIFICATION_TYPE_TAG: Record<string, TagType> = {
  [NotificationType.System]: 'info',
  [NotificationType.User]: 'default',
  [NotificationType.Announcement]: 'success',
  [NotificationType.Warning]: 'warning',
  [NotificationType.Error]: 'error',
}

/** 展示用全集（历史数据可能含角色/部门） */
const targetTypeOptions = computed(() => [
  { label: t('message.notification.target_all'), value: NotificationTargetType.All },
  { label: t('message.notification.target_role'), value: NotificationTargetType.Role },
  { label: t('message.notification.target_department'), value: NotificationTargetType.Department },
  { label: t('message.notification.target_user'), value: NotificationTargetType.User },
])

/** 表单可选目标（后端发布仅支持全员/指定用户） */
const targetTypeFormOptions = computed(() => [
  { label: t('message.notification.target_all'), value: NotificationTargetType.All },
  { label: t('message.notification.target_user'), value: NotificationTargetType.User },
])

const publishedOptions = computed(() => [
  { label: t('message.notification.published'), value: 1 },
  { label: t('message.notification.unpublished'), value: 0 },
])

// ── 表单 ─────────────────────────────────────────────────────────
interface NotificationFormModel {
  basicId?: ApiId
  title: string
  content: string | null
  notificationType: NotificationType
  targetType: NotificationTargetType
  userIds: string[]
  icon: string | null
  link: string | null
  expirationTime: number | null
  needConfirm: boolean
  // 编辑透传字段（表单不暴露，保持原值不被覆盖丢失）
  businessType: string | null
  businessId: ApiId | null
  remark: string | null
}

function createDefaultForm(): NotificationFormModel {
  return {
    title: '',
    content: null,
    notificationType: NotificationType.Announcement,
    targetType: NotificationTargetType.All,
    userIds: [],
    icon: null,
    link: null,
    expirationTime: null,
    needConfirm: false,
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

const detailVisible = ref(false)
const currentDetail = ref<NotificationDetailDto | null>(null)

// ── 字段单一事实源：列 + 搜索 ────────────────────────────────────
const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('message.notification.col_keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('message.notification.search_keyword_placeholder'), order: 0 },
  { key: 'title', title: t('message.notification.col_title'), dataType: 'string', minWidth: 220, order: 10 },
  {
    key: 'notificationType',
    title: t('message.notification.col_type'),
    dataType: 'enum',
    searchable: true,
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
  { key: 'sendTime', title: t('message.notification.col_send_time'), dataType: 'datetime', minWidth: 170, order: 14 },
  { key: 'expirationTime', title: t('message.notification.col_expiration_time'), dataType: 'datetime', minWidth: 170, order: 15 },
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
        ...createPageRequest({ page: { pageIndex: params.page, pageSize: params.pageSize } }),
        keyword: toStr(f.keyword),
        notificationType: (f.notificationType as NotificationType | undefined) ?? undefined,
        isPublished: f.isPublished === undefined || f.isPublished === null || f.isPublished === ''
          ? undefined
          : Boolean(Number(f.isPublished)),
      }) as unknown as Promise<PageResult<Record<string, unknown>>>
    },
  },
  actions: [
    { key: 'create', title: t('message.notification.action_create'), scope: 'page', type: 'primary', icon: 'lucide:plus', permission: 'saas:message:create' },
    { key: 'view', title: t('message.notification.action_view'), scope: 'row', icon: 'lucide:eye' },
    { key: 'edit', title: t('message.notification.action_edit'), scope: 'row', icon: 'lucide:pen', permission: 'saas:message:update', visible: isUnpublished },
    { key: 'publish', title: t('message.notification.action_publish'), scope: 'row', type: 'primary', icon: 'lucide:send', permission: 'saas:message:publish', visible: isUnpublished },
    { key: 'delete', title: t('message.notification.action_delete'), scope: 'row', type: 'error', icon: 'lucide:trash-2', permission: 'saas:message:delete', confirm: true, confirmText: t('message.notification.confirm_delete') },
  ],
}))

function isUnpublished(row: unknown): boolean {
  return !(row as NotificationListItemDto).isPublished
}

function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as NotificationListItemDto | undefined
  if (payload.scope === 'page' && payload.key === 'create') {
    notificationForm.value = createDefaultForm()
    modalVisible.value = true
    return
  }
  if (payload.scope === 'row' && row) {
    if (payload.key === 'view')
      void openDetail(row)
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
      targetType: detail.targetType,
      userIds: [],
      icon: detail.icon ?? null,
      link: detail.link ?? null,
      expirationTime: detail.expirationTime ? new Date(detail.expirationTime).getTime() : null,
      needConfirm: detail.needConfirm,
      businessType: detail.businessType ?? null,
      businessId: detail.businessId ?? null,
      remark: detail.remark ?? null,
    }
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

// ── 新增/编辑提交 ────────────────────────────────────────────────
function validateForm(form: NotificationFormModel): boolean {
  if (!form.title.trim()) {
    message.warning(t('message.notification.msg_title_required'))
    return false
  }
  if (form.targetType === NotificationTargetType.User) {
    if (form.userIds.length === 0) {
      message.warning(t('message.notification.msg_user_required'))
      return false
    }
    if (form.userIds.some(id => !/^[1-9]\d*$/.test(id.trim()))) {
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

  const userIds = form.targetType === NotificationTargetType.User
    ? form.userIds.map(id => id.trim())
    : []
  const expirationTime: DateTimeString | null = form.expirationTime
    ? new Date(form.expirationTime).toISOString()
    : null

  submitLoading.value = true
  try {
    if (form.basicId) {
      await notificationApi.update({
        basicId: form.basicId,
        title: form.title.trim(),
        content: toStr(form.content) ?? null,
        notificationType: form.notificationType,
        targetType: form.targetType,
        userIds,
        icon: toStr(form.icon) ?? null,
        link: toStr(form.link) ?? null,
        sendTime: null,
        expirationTime,
        needConfirm: form.needConfirm,
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
        targetType: form.targetType,
        userIds,
        icon: toStr(form.icon) ?? null,
        link: toStr(form.link) ?? null,
        sendTime: null,
        expirationTime,
        needConfirm: form.needConfirm,
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
        <NFormItem :label="t('message.notification.form_content')" path="content">
          <NInput
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
        <div class="grid grid-cols-2 gap-x-4">
          <NFormItem :label="t('message.notification.form_icon')" path="icon">
            <NInput v-model:value="notificationForm.icon" clearable :maxlength="100" :placeholder="t('message.notification.form_icon_placeholder')" />
          </NFormItem>
          <NFormItem :label="t('message.notification.form_link')" path="link">
            <NInput v-model:value="notificationForm.link" clearable :maxlength="500" :placeholder="t('message.notification.form_link_placeholder')" />
          </NFormItem>
        </div>
        <div class="grid grid-cols-2 gap-x-4">
          <NFormItem :label="t('message.notification.form_expiration_time')" path="expirationTime">
            <NDatePicker
              v-model:value="notificationForm.expirationTime"
              type="datetime"
              clearable
              style="width: 100%"
              :placeholder="t('message.notification.form_expiration_placeholder')"
            />
          </NFormItem>
          <NFormItem :label="t('message.notification.form_need_confirm')" path="needConfirm">
            <NSwitch v-model:value="notificationForm.needConfirm" />
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
            <NDescriptionsItem :label="t('message.notification.detail.label.need_confirm')">
              {{ currentDetail.needConfirm ? t('common.statuses.yes') : t('common.statuses.no') }}
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
          </NDescriptions>
          <div class="mt-3 text-xs opacity-70">
            {{ t('message.notification.detail_content_title') }}
          </div>
          <pre class="detail-content">{{ currentDetail.content || t('message.notification.detail_no_content') }}</pre>
        </template>
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

.detail-content {
  margin-top: 4px;
  max-height: 320px;
  overflow: auto;
  padding: 12px;
  border: 1px solid hsl(var(--border));
  border-radius: 8px;
  background: hsl(var(--muted) / 40%);
  font-size: 12px;
  line-height: 1.6;
  white-space: pre-wrap;
  word-break: break-all;
}
</style>
