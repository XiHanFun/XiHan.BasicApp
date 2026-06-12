<script setup lang="ts">
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import type {
  ApiId,
  DateTimeString,
  NotificationDetailDto,
  NotificationListItemDto,
  PageResult,
} from '@/api'
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
import {
  createPageRequest,
  notificationApi,
  NotificationTargetType,
  NotificationType,
} from '@/api'
import { SchemaPage } from '~/components'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'MessageNotificationPage' })

const message = useMessage()
const dialog = useDialog()
const schemaPageRef = ref<InstanceType<typeof SchemaPage> | null>(null)

type TagType = 'default' | 'error' | 'info' | 'success' | 'warning'

// ── 选项 ─────────────────────────────────────────────────────────
const notificationTypeOptions = [
  { label: '系统通知', value: NotificationType.System },
  { label: '用户通知', value: NotificationType.User },
  { label: '公告', value: NotificationType.Announcement },
  { label: '警告', value: NotificationType.Warning },
  { label: '错误', value: NotificationType.Error },
]

const NOTIFICATION_TYPE_TAG: Record<string, TagType> = {
  [NotificationType.System]: 'info',
  [NotificationType.User]: 'default',
  [NotificationType.Announcement]: 'success',
  [NotificationType.Warning]: 'warning',
  [NotificationType.Error]: 'error',
}

/** 展示用全集（历史数据可能含角色/部门） */
const targetTypeOptions = [
  { label: '全员', value: NotificationTargetType.All },
  { label: '角色', value: NotificationTargetType.Role },
  { label: '部门', value: NotificationTargetType.Department },
  { label: '指定用户', value: NotificationTargetType.User },
]

/** 表单可选目标（后端发布仅支持全员/指定用户） */
const targetTypeFormOptions = [
  { label: '全员', value: NotificationTargetType.All },
  { label: '指定用户', value: NotificationTargetType.User },
]

const publishedOptions = [
  { label: '已发布', value: 1 },
  { label: '未发布', value: 0 },
]

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
const modalTitle = computed(() => (notificationForm.value.basicId ? '编辑公告' : '新增公告'))
const isUserTarget = computed(() => notificationForm.value.targetType === NotificationTargetType.User)

const detailVisible = ref(false)
const currentDetail = ref<NotificationDetailDto | null>(null)

// ── 字段单一事实源：列 + 搜索 ────────────────────────────────────
const fields: ListFieldSchema[] = [
  { key: 'keyword', title: '关键词', dataType: 'string', visible: false, searchable: true, searchPlaceholder: '搜索标题/内容', order: 0 },
  { key: 'title', title: '标题', dataType: 'string', minWidth: 220, order: 10 },
  {
    key: 'notificationType',
    title: '类型',
    dataType: 'enum',
    searchable: true,
    options: notificationTypeOptions,
    searchPlaceholder: '类型',
    width: 110,
    order: 11,
    render: (row) => {
      const r = row as unknown as NotificationListItemDto
      return h(
        NTag,
        { size: 'small', round: true, bordered: false, type: NOTIFICATION_TYPE_TAG[r.notificationType] ?? 'default' },
        () => getOptionLabel(notificationTypeOptions, r.notificationType),
      )
    },
  },
  {
    key: 'targetType',
    title: '目标类型',
    dataType: 'enum',
    options: targetTypeOptions,
    width: 110,
    order: 12,
    render: row => getOptionLabel(targetTypeOptions, (row as unknown as NotificationListItemDto).targetType),
  },
  {
    key: 'isPublished',
    title: '是否发布',
    dataType: 'boolean',
    searchable: true,
    options: publishedOptions,
    searchPlaceholder: '发布状态',
    width: 100,
    order: 13,
    render: (row) => {
      const published = (row as unknown as NotificationListItemDto).isPublished
      return h(
        NTag,
        { size: 'small', round: true, bordered: false, type: published ? 'success' : 'default' },
        () => published ? '已发布' : '未发布',
      )
    },
  },
  { key: 'sendTime', title: '发送时间', dataType: 'datetime', minWidth: 170, order: 14 },
  { key: 'expirationTime', title: '过期时间', dataType: 'datetime', minWidth: 170, order: 15 },
  { key: 'createdTime', title: '创建时间', dataType: 'datetime', sortable: true, minWidth: 170, order: 16 },
]

function toStr(v: unknown): string | undefined {
  return (v as string | undefined)?.trim() || undefined
}

const schema: PageSchema = {
  pageCode: 'message.notification',
  pageName: '公告管理',
  rowKey: 'basicId',
  scrollX: 1300,
  fields,
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
    { key: 'create', title: '新增公告', scope: 'page', type: 'primary', icon: 'lucide:plus', permission: 'saas:message:create' },
    { key: 'view', title: '详情', scope: 'row', icon: 'lucide:eye' },
    { key: 'edit', title: '编辑', scope: 'row', icon: 'lucide:pen', permission: 'saas:message:update', visible: isUnpublished },
    { key: 'publish', title: '发布', scope: 'row', type: 'primary', icon: 'lucide:send', permission: 'saas:message:publish', visible: isUnpublished },
    { key: 'delete', title: '删除', scope: 'row', type: 'error', icon: 'lucide:trash-2', permission: 'saas:message:delete', confirm: true, confirmText: '确认删除该公告？已下发的用户通知会一并删除。' },
  ],
}

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
      message.error('公告不存在')
      return
    }
    detailVisible.value = true
  }
  catch (e) {
    message.error((e as Error).message || '加载公告详情失败')
  }
}

// ── 编辑（未发布才可） ───────────────────────────────────────────
async function openEdit(row: NotificationListItemDto) {
  try {
    const detail = await notificationApi.detail(row.basicId)
    if (!detail) {
      message.error('公告不存在')
      return
    }
    if (detail.isPublished) {
      message.warning('已发布公告不可编辑')
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
    message.error((e as Error).message || '加载公告失败')
  }
}

// ── 发布（确认对话框；目标范围沿用创建时设定） ───────────────────
function confirmPublish(row: NotificationListItemDto) {
  const targetText = row.targetType === NotificationTargetType.All
    ? '发布后将立即推送给全体启用用户。'
    : '发布后将立即推送给创建时指定的用户。'
  dialog.warning({
    title: '发布公告',
    content: `确定发布「${row.title}」吗？${targetText}发布后不可再编辑。`,
    positiveText: '发布',
    negativeText: '取消',
    onPositiveClick: async () => {
      try {
        const result = await notificationApi.publish({ basicId: row.basicId })
        message.success(`发布成功，已发送给 ${result.recipientCount} 位用户`)
        void schemaPageRef.value?.reload()
      }
      catch (e) {
        message.error((e as Error).message || '发布失败')
      }
    },
  })
}

// ── 删除 ─────────────────────────────────────────────────────────
async function removeRow(row: NotificationListItemDto) {
  try {
    await notificationApi.delete(row.basicId)
    message.success('删除成功')
    void schemaPageRef.value?.reload()
  }
  catch (e) {
    message.error((e as Error).message || '删除失败')
  }
}

// ── 新增/编辑提交 ────────────────────────────────────────────────
function validateForm(form: NotificationFormModel): boolean {
  if (!form.title.trim()) {
    message.warning('请输入公告标题')
    return false
  }
  if (form.targetType === NotificationTargetType.User) {
    if (form.userIds.length === 0) {
      message.warning('指定用户目标需填写至少一个用户ID')
      return false
    }
    if (form.userIds.some(id => !/^[1-9]\d*$/.test(id.trim()))) {
      message.warning('用户ID必须为正整数')
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
      message.success('更新成功')
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
      message.success('创建成功，可在列表中发布')
    }
    modalVisible.value = false
    void schemaPageRef.value?.reload()
  }
  catch (e) {
    message.error((e as Error).message || '保存失败')
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
        <NFormItem label="标题" path="title">
          <NInput v-model:value="notificationForm.title" clearable :maxlength="200" placeholder="请输入公告标题" />
        </NFormItem>
        <NFormItem label="内容" path="content">
          <NInput
            v-model:value="notificationForm.content"
            type="textarea"
            :autosize="{ minRows: 5, maxRows: 12 }"
            placeholder="请输入公告内容"
          />
        </NFormItem>
        <div class="grid grid-cols-2 gap-x-4">
          <NFormItem label="类型" path="notificationType">
            <NSelect v-model:value="notificationForm.notificationType" :options="notificationTypeOptions" />
          </NFormItem>
          <NFormItem label="目标类型" path="targetType">
            <NSelect v-model:value="notificationForm.targetType" :options="targetTypeFormOptions" />
          </NFormItem>
        </div>
        <NFormItem v-if="isUserTarget" label="接收用户ID列表（回车添加）" path="userIds">
          <NDynamicTags v-model:value="notificationForm.userIds" />
        </NFormItem>
        <div class="grid grid-cols-2 gap-x-4">
          <NFormItem label="图标" path="icon">
            <NInput v-model:value="notificationForm.icon" clearable :maxlength="100" placeholder="如 lucide:megaphone（可空）" />
          </NFormItem>
          <NFormItem label="链接" path="link">
            <NInput v-model:value="notificationForm.link" clearable :maxlength="500" placeholder="点击通知跳转地址（可空）" />
          </NFormItem>
        </div>
        <div class="grid grid-cols-2 gap-x-4">
          <NFormItem label="过期时间" path="expirationTime">
            <NDatePicker
              v-model:value="notificationForm.expirationTime"
              type="datetime"
              clearable
              style="width: 100%"
              placeholder="留空永不过期"
            />
          </NFormItem>
          <NFormItem label="需用户确认" path="needConfirm">
            <NSwitch v-model:value="notificationForm.needConfirm" />
          </NFormItem>
        </div>
        <p v-if="isUserTarget && notificationForm.basicId" class="form-hint">
          编辑会整体覆盖原指定用户列表，请重新填写完整的接收用户ID。
        </p>
      </NForm>
      <template #footer>
        <div class="flex justify-end gap-2">
          <NButton size="small" @click="modalVisible = false">
            取消
          </NButton>
          <NButton size="small" type="primary" :loading="submitLoading" @click="handleSubmit">
            保存
          </NButton>
        </div>
      </template>
    </NModal>

    <!-- 详情（抽屉） -->
    <NDrawer v-model:show="detailVisible" :width="560">
      <NDrawerContent title="公告详情" closable>
        <template v-if="currentDetail">
          <NDescriptions :column="2" label-placement="left" bordered size="small">
            <NDescriptionsItem label="标题" :span="2">
              {{ currentDetail.title }}
            </NDescriptionsItem>
            <NDescriptionsItem label="类型">
              {{ getOptionLabel(notificationTypeOptions, currentDetail.notificationType) }}
            </NDescriptionsItem>
            <NDescriptionsItem label="目标类型">
              {{ getOptionLabel(targetTypeOptions, currentDetail.targetType) }}
            </NDescriptionsItem>
            <NDescriptionsItem label="是否发布">
              <NTag size="small" round :bordered="false" :type="currentDetail.isPublished ? 'success' : 'default'">
                {{ currentDetail.isPublished ? '已发布' : '未发布' }}
              </NTag>
            </NDescriptionsItem>
            <NDescriptionsItem label="需用户确认">
              {{ currentDetail.needConfirm ? '是' : '否' }}
            </NDescriptionsItem>
            <NDescriptionsItem label="发送时间">
              {{ formatDate(currentDetail.sendTime) }}
            </NDescriptionsItem>
            <NDescriptionsItem label="过期时间">
              {{ currentDetail.expirationTime ? formatDate(currentDetail.expirationTime) : '永不过期' }}
            </NDescriptionsItem>
            <NDescriptionsItem label="图标">
              {{ currentDetail.icon || '-' }}
            </NDescriptionsItem>
            <NDescriptionsItem label="链接">
              {{ currentDetail.link || '-' }}
            </NDescriptionsItem>
            <NDescriptionsItem label="业务类型">
              {{ currentDetail.businessType || '-' }}
            </NDescriptionsItem>
            <NDescriptionsItem label="业务主键">
              {{ currentDetail.businessId || '-' }}
            </NDescriptionsItem>
            <NDescriptionsItem label="创建者">
              {{ currentDetail.createdBy || '-' }}
            </NDescriptionsItem>
            <NDescriptionsItem label="创建时间">
              {{ formatDate(currentDetail.createdTime) }}
            </NDescriptionsItem>
            <NDescriptionsItem label="备注" :span="2">
              {{ currentDetail.remark || '-' }}
            </NDescriptionsItem>
          </NDescriptions>
          <div class="mt-3 text-xs opacity-70">
            公告内容
          </div>
          <pre class="detail-content">{{ currentDetail.content || '（无内容）' }}</pre>
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
