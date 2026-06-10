<script setup lang="ts">
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import type {
  ApiId,
  MessageTemplateDetailDto,
  MessageTemplateListItemDto,
  PageResult,
} from '@/api'
import {
  NButton,
  NDescriptions,
  NDescriptionsItem,
  NForm,
  NFormItem,
  NInput,
  NInputNumber,
  NModal,
  NSelect,
  NSwitch,
  NTag,
  useMessage,
} from 'naive-ui'
import { computed, h, ref } from 'vue'
import {
  createPageRequest,
  EnableStatus,
  MessageChannel,
  messageTemplateApi,
} from '@/api'
import { SchemaPage } from '~/components'
import { useUserStore } from '~/stores'
import { getOptionLabel } from '~/utils'

defineOptions({ name: 'MessageTemplatePage' })

const message = useMessage()
const userStore = useUserStore()
const schemaPageRef = ref<InstanceType<typeof SchemaPage> | null>(null)

/**
 * 全局模板(TenantId=0)仅平台运维态可维护；非平台态隐藏编辑/启停/删除入口，
 * 租户可通过「新增」创建同编码模板覆盖全局默认。
 */
function canMaintainTemplate(row: unknown): boolean {
  const template = row as MessageTemplateListItemDto
  return !template.isGlobal || (userStore.userInfo?.isPlatform ?? false)
}

const channelOptions = [
  { label: '站内通知', value: MessageChannel.SiteNotification },
  { label: '邮件', value: MessageChannel.Email },
  { label: '短信', value: MessageChannel.Sms },
]

const statusOptions = [
  { label: '启用', value: EnableStatus.Enabled },
  { label: '禁用', value: EnableStatus.Disabled },
]

interface TemplateFormModel {
  basicId?: ApiId
  templateCode: string
  channel: MessageChannel
  templateName: string
  subject: string | null
  content: string
  isHtml: boolean
  description: string | null
  status: EnableStatus
  sort: number
  remark: string | null
}

function createDefaultForm(): TemplateFormModel {
  return {
    templateCode: '',
    channel: MessageChannel.Email,
    templateName: '',
    subject: null,
    content: '',
    isHtml: false,
    description: null,
    status: EnableStatus.Enabled,
    sort: 100,
    remark: null,
  }
}

const modalVisible = ref(false)
const submitLoading = ref(false)
const templateForm = ref<TemplateFormModel>(createDefaultForm())
const modalTitle = computed(() => (templateForm.value.basicId ? '编辑模板' : '新增模板'))

const detailVisible = ref(false)
const currentDetail = ref<MessageTemplateDetailDto | null>(null)

// ── 字段单一事实源：列 + 搜索 ─────────────────────
const fields: ListFieldSchema[] = [
  { key: 'keyword', title: '关键词', dataType: 'string', visible: false, searchable: true, searchPlaceholder: '搜索编码/名称/描述', order: 0 },
  { key: 'templateCode', title: '模板编码', dataType: 'string', minWidth: 200, order: 10 },
  {
    key: 'channel',
    title: '渠道',
    dataType: 'enum',
    searchable: true,
    options: channelOptions,
    searchPlaceholder: '渠道',
    width: 100,
    order: 11,
    render: row => getOptionLabel(channelOptions, (row as unknown as MessageTemplateListItemDto).channel),
  },
  { key: 'templateName', title: '模板名称', dataType: 'string', minWidth: 140, order: 12 },
  { key: 'subject', title: '主题模板', dataType: 'string', minWidth: 200, order: 13 },
  {
    key: 'isGlobal',
    title: '范围',
    dataType: 'enum',
    width: 90,
    order: 14,
    render: (row) => {
      const isGlobal = (row as unknown as MessageTemplateListItemDto).isGlobal
      return h(NTag, { size: 'small', round: true, bordered: false, type: isGlobal ? 'info' : 'default' }, () => isGlobal ? '全局' : '租户')
    },
  },
  {
    key: 'isHtml',
    title: 'HTML',
    dataType: 'enum',
    width: 80,
    order: 15,
    render: row => (row as unknown as MessageTemplateListItemDto).isHtml ? '是' : '否',
  },
  { key: 'description', title: '描述', dataType: 'string', minWidth: 220, order: 16 },
  {
    key: 'status',
    title: '状态',
    dataType: 'enum',
    searchable: true,
    options: statusOptions,
    searchPlaceholder: '状态',
    width: 90,
    order: 17,
    render: row => h(NTag, { size: 'small', round: true, bordered: false, type: (row as unknown as MessageTemplateListItemDto).status === EnableStatus.Enabled ? 'success' : 'error' }, () => (row as unknown as MessageTemplateListItemDto).status === EnableStatus.Enabled ? '启用' : '禁用'),
  },
  { key: 'sort', title: '排序', dataType: 'number', width: 80, order: 18 },
]

function toStr(v: unknown): string | undefined {
  return (v as string | undefined)?.trim() || undefined
}

const schema: PageSchema = {
  pageCode: 'message.template',
  pageName: '消息模板',
  rowKey: 'basicId',
  scrollX: 1500,
  fields,
  resource: {
    page: (params) => {
      const f = params.filters
      return messageTemplateApi.page({
        ...createPageRequest({ page: { pageIndex: params.page, pageSize: params.pageSize } }),
        keyword: toStr(f.keyword),
        channel: (f.channel as MessageChannel | undefined) ?? undefined,
        status: (f.status as EnableStatus | undefined) ?? undefined,
      }) as unknown as Promise<PageResult<Record<string, unknown>>>
    },
  },
  actions: [
    { key: 'create', title: '新增模板', scope: 'page', type: 'primary', icon: 'lucide:plus' },
    { key: 'view', title: '详情', scope: 'row', icon: 'lucide:eye' },
    { key: 'edit', title: '编辑', scope: 'row', icon: 'lucide:pen', visible: canMaintainTemplate },
    { key: 'toggle', title: '启停', scope: 'row', icon: 'lucide:power', visible: canMaintainTemplate },
    { key: 'delete', title: '删除', scope: 'row', type: 'error', icon: 'lucide:trash-2', confirm: true, confirmText: '确认删除该模板？', visible: canMaintainTemplate },
  ],
}

function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as MessageTemplateListItemDto | undefined
  if (payload.scope === 'page' && payload.key === 'create') {
    templateForm.value = createDefaultForm()
    modalVisible.value = true
    return
  }
  if (payload.scope === 'row' && row) {
    if (payload.key === 'view')
      void openDetail(row)
    else if (payload.key === 'edit')
      void openEdit(row)
    else if (payload.key === 'toggle')
      void toggleStatus(row)
    else if (payload.key === 'delete')
      void removeRow(row)
  }
}

async function openDetail(row: MessageTemplateListItemDto) {
  try {
    currentDetail.value = await messageTemplateApi.detail(row.basicId)
    detailVisible.value = true
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '加载模板详情失败')
  }
}

async function openEdit(row: MessageTemplateListItemDto) {
  try {
    const detail = await messageTemplateApi.detail(row.basicId)
    if (!detail) {
      message.error('模板不存在')
      return
    }
    templateForm.value = {
      basicId: detail.basicId,
      templateCode: detail.templateCode,
      channel: detail.channel,
      templateName: detail.templateName,
      subject: detail.subject ?? null,
      content: detail.content,
      isHtml: detail.isHtml,
      description: detail.description ?? null,
      status: detail.status,
      sort: detail.sort,
      remark: detail.remark ?? null,
    }
    modalVisible.value = true
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '加载模板失败')
  }
}

async function toggleStatus(row: MessageTemplateListItemDto) {
  try {
    await messageTemplateApi.updateStatus({
      basicId: row.basicId,
      status: row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled,
    })
    message.success('状态已更新')
    schemaPageRef.value?.reload()
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '状态更新失败')
  }
}

async function removeRow(row: MessageTemplateListItemDto) {
  try {
    await messageTemplateApi.delete(row.basicId)
    message.success('删除成功')
    schemaPageRef.value?.reload()
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '删除失败')
  }
}

async function handleSubmit() {
  const form = templateForm.value
  if (!form.templateCode.trim() && !form.basicId) {
    message.warning('请输入模板编码')
    return
  }
  if (!form.templateName.trim()) {
    message.warning('请输入模板名称')
    return
  }
  if (!form.content.trim()) {
    message.warning('请输入模板内容')
    return
  }

  submitLoading.value = true
  try {
    if (form.basicId) {
      await messageTemplateApi.update({
        basicId: form.basicId,
        templateName: form.templateName.trim(),
        subject: toStr(form.subject) ?? null,
        content: form.content,
        isHtml: form.isHtml,
        description: toStr(form.description) ?? null,
        sort: form.sort,
        remark: toStr(form.remark) ?? null,
      })
      message.success('更新成功')
    }
    else {
      await messageTemplateApi.create({
        templateCode: form.templateCode.trim(),
        channel: form.channel,
        templateName: form.templateName.trim(),
        subject: toStr(form.subject) ?? null,
        content: form.content,
        isHtml: form.isHtml,
        description: toStr(form.description) ?? null,
        status: form.status,
        sort: form.sort,
        remark: toStr(form.remark) ?? null,
      })
      message.success('创建成功')
    }
    modalVisible.value = false
    schemaPageRef.value?.reload()
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '保存失败')
  }
  finally {
    submitLoading.value = false
  }
}
</script>

<template>
  <SchemaPage ref="schemaPageRef" :schema="schema" @action="onAction">
    <!-- 编辑/新增 -->
    <NModal
      v-model:show="modalVisible"
      preset="card"
      :title="modalTitle"
      style="width: 720px"
    >
      <NForm :model="templateForm" label-placement="top">
        <div class="grid grid-cols-2 gap-x-4">
          <NFormItem label="模板编码" path="templateCode">
            <NInput
              v-model:value="templateForm.templateCode"
              :disabled="Boolean(templateForm.basicId)"
              clearable
              placeholder="如 auth-email-login-code（渠道内唯一）"
            />
          </NFormItem>
          <NFormItem label="渠道" path="channel">
            <NSelect
              v-model:value="templateForm.channel"
              :disabled="Boolean(templateForm.basicId)"
              :options="channelOptions"
            />
          </NFormItem>
          <NFormItem label="模板名称" path="templateName">
            <NInput v-model:value="templateForm.templateName" clearable placeholder="请输入模板名称" />
          </NFormItem>
          <NFormItem label="排序" path="sort">
            <NInputNumber v-model:value="templateForm.sort" :min="0" style="width: 100%" />
          </NFormItem>
        </div>
        <NFormItem label="主题模板" path="subject">
          <NInput v-model:value="templateForm.subject" clearable placeholder="邮件主题/通知标题，支持 {{ 变量 }}（短信留空）" />
        </NFormItem>
        <NFormItem label="内容模板（Scriban 语法，如 {{ code }}）" path="content">
          <NInput
            v-model:value="templateForm.content"
            type="textarea"
            :autosize="{ minRows: 8, maxRows: 18 }"
            placeholder="模板内容，支持 {{ 变量 }} / {{ if }} / {{ for }}"
          />
        </NFormItem>
        <div class="grid grid-cols-2 gap-x-4">
          <NFormItem label="内容为 HTML" path="isHtml">
            <NSwitch v-model:value="templateForm.isHtml" />
          </NFormItem>
          <NFormItem v-if="!templateForm.basicId" label="状态" path="status">
            <NSelect v-model:value="templateForm.status" :options="statusOptions" />
          </NFormItem>
        </div>
        <NFormItem label="描述（可用变量说明）" path="description">
          <NInput v-model:value="templateForm.description" clearable placeholder="如：变量 code=验证码, brand=品牌名" />
        </NFormItem>
        <NFormItem label="备注" path="remark">
          <NInput v-model:value="templateForm.remark" clearable />
        </NFormItem>
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

    <!-- 详情 -->
    <NModal
      v-model:show="detailVisible"
      preset="card"
      title="模板详情"
      style="width: 720px"
    >
      <template v-if="currentDetail">
        <NDescriptions :column="2" label-placement="left" bordered size="small">
          <NDescriptionsItem label="模板编码">
            {{ currentDetail.templateCode }}
          </NDescriptionsItem>
          <NDescriptionsItem label="渠道">
            {{ getOptionLabel(channelOptions, currentDetail.channel) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="模板名称">
            {{ currentDetail.templateName }}
          </NDescriptionsItem>
          <NDescriptionsItem label="范围">
            {{ currentDetail.isGlobal ? '全局' : '租户' }}
          </NDescriptionsItem>
          <NDescriptionsItem label="主题模板" :span="2">
            {{ currentDetail.subject || '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem label="描述" :span="2">
            {{ currentDetail.description || '-' }}
          </NDescriptionsItem>
        </NDescriptions>
        <div class="mt-3 text-xs opacity-70">
          内容模板
        </div>
        <pre class="mt-1 max-h-80 overflow-auto rounded-lg border border-[hsl(var(--border))] bg-[hsl(var(--muted)/40%)] p-3 text-xs leading-relaxed whitespace-pre-wrap">{{ currentDetail.content }}</pre>
      </template>
    </NModal>
  </SchemaPage>
</template>
