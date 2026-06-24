<script setup lang="ts">
import type {
  ApiId,
  MessageTemplateDetailDto,
  MessageTemplateListItemDto,
  PageResult,
} from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
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
import { useI18n } from 'vue-i18n'
import {
  createPageRequest,
  EnableStatus,
  MessageChannel,
  messageTemplateApi,
  querySortsFromSchema,
} from '@/api'
import { SchemaPage } from '~/components'
import { useUserStore } from '~/stores'
import { getOptionLabel } from '~/utils'

defineOptions({ name: 'MessageTemplatePage' })

const { t } = useI18n()
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

const channelOptions = computed(() => [
  { label: t('message.template.channel_site'), value: MessageChannel.SiteNotification },
  { label: t('message.template.channel_email'), value: MessageChannel.Email },
  { label: t('message.template.channel_sms'), value: MessageChannel.Sms },
])

const statusOptions = computed(() => [
  { label: t('message.template.status_enabled'), value: EnableStatus.Enabled },
  { label: t('message.template.status_disabled'), value: EnableStatus.Disabled },
])

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
const modalTitle = computed(() => (templateForm.value.basicId ? t('message.template.edit_title') : t('message.template.add_title')))

const detailVisible = ref(false)
const currentDetail = ref<MessageTemplateDetailDto | null>(null)

// ── 字段单一事实源：列 + 搜索 ─────────────────────
const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('message.template.col_keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('message.template.search_keyword_placeholder'), order: 0 },
  { key: 'templateCode', title: t('message.template.col_template_code'), dataType: 'string', minWidth: 200, order: 10, sortable: true },
  {
    key: 'channel',
    title: t('message.template.col_channel'),
    dataType: 'enum',
    searchable: true,
    sortable: true,
    options: channelOptions.value,
    searchPlaceholder: t('message.template.search_channel_placeholder'),
    width: 100,
    order: 11,
    render: row => getOptionLabel(channelOptions.value, (row as unknown as MessageTemplateListItemDto).channel),
  },
  { key: 'templateName', title: t('message.template.col_template_name'), dataType: 'string', minWidth: 140, order: 12, sortable: true },
  { key: 'subject', title: t('message.template.col_subject'), dataType: 'string', minWidth: 200, order: 13, sortable: true },
  {
    key: 'isGlobal',
    title: t('message.template.col_scope'),
    dataType: 'enum',
    width: 90,
    order: 14,
    render: (row) => {
      const isGlobal = (row as unknown as MessageTemplateListItemDto).isGlobal
      return h(NTag, { size: 'small', round: true, bordered: false, type: isGlobal ? 'info' : 'default' }, () => isGlobal ? t('message.template.scope_global') : t('message.template.scope_tenant'))
    },
  },
  {
    key: 'isHtml',
    title: t('message.template.col_is_html'),
    dataType: 'enum',
    sortable: true,
    width: 80,
    order: 15,
    render: row => (row as unknown as MessageTemplateListItemDto).isHtml ? t('common.statuses.yes') : t('common.statuses.no'),
  },
  { key: 'description', title: t('message.template.col_description'), dataType: 'string', minWidth: 220, order: 16, sortable: true },
  {
    key: 'status',
    title: t('message.template.col_status'),
    dataType: 'enum',
    searchable: true,
    sortable: true,
    dictionaryCode: 'EnableStatus',
    options: statusOptions.value,
    searchPlaceholder: t('message.template.search_status_placeholder'),
    width: 90,
    order: 17,
    render: row => h(NTag, { size: 'small', round: true, bordered: false, type: (row as unknown as MessageTemplateListItemDto).status === EnableStatus.Enabled ? 'success' : 'error' }, () => (row as unknown as MessageTemplateListItemDto).status === EnableStatus.Enabled ? t('message.template.status_enabled') : t('message.template.status_disabled')),
  },
  { key: 'sort', title: t('message.template.col_sort'), dataType: 'number', width: 80, order: 18, sortable: true },
])

function toStr(v: unknown): string | undefined {
  return (v as string | undefined)?.trim() || undefined
}

const schema = computed<PageSchema>(() => ({
  pageCode: 'message.template',
  exportPermission: 'saas:message-template:export',
  pageName: t('message.template.page_name'),
  statusPermission: 'saas:message-template:status',
  rowKey: 'basicId',
  scrollX: 1500,
  fields: fields.value,
  resource: {
    page: (params) => {
      const f = params.filters
      return messageTemplateApi.page({
        ...createPageRequest({
          page: { pageIndex: params.page, pageSize: params.pageSize },
          conditions: { sorts: querySortsFromSchema(params.sortField, params.sortOrder) },
        }),
        keyword: toStr(f.keyword),
        channel: (f.channel as MessageChannel | undefined) ?? undefined,
        status: (f.status as EnableStatus | undefined) ?? undefined,
      }) as unknown as Promise<PageResult<Record<string, unknown>>>
    },
    updateStatus: (id, enabled) => messageTemplateApi.updateStatus({ basicId: id, status: enabled ? EnableStatus.Enabled : EnableStatus.Disabled }),
  },
  actions: [
    { key: 'create', title: t('message.template.action_create'), scope: 'page', type: 'primary', icon: 'lucide:plus' },
    { key: 'view', title: t('message.template.action_view'), scope: 'row', icon: 'lucide:eye' },
    { key: 'edit', title: t('message.template.action_edit'), scope: 'row', icon: 'lucide:pen', visible: canMaintainTemplate },
    { key: 'toggle', title: t('message.template.action_toggle'), scope: 'row', icon: 'lucide:power', visible: canMaintainTemplate },
    { key: 'delete', title: t('message.template.action_delete'), scope: 'row', type: 'error', icon: 'lucide:trash-2', confirm: true, confirmText: t('message.template.confirm_delete'), visible: canMaintainTemplate },
  ],
}))

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
    message.error((e as Error)?.message || t('message.template.msg_load_detail_failed'))
  }
}

async function openEdit(row: MessageTemplateListItemDto) {
  try {
    const detail = await messageTemplateApi.detail(row.basicId)
    if (!detail) {
      message.error(t('message.template.msg_not_found'))
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
    message.error((e as Error)?.message || t('message.template.msg_load_failed'))
  }
}

async function toggleStatus(row: MessageTemplateListItemDto) {
  try {
    await messageTemplateApi.updateStatus({
      basicId: row.basicId,
      status: row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled,
    })
    message.success(t('message.template.msg_status_updated'))
    schemaPageRef.value?.reload()
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || t('message.template.msg_status_update_failed'))
  }
}

async function removeRow(row: MessageTemplateListItemDto) {
  try {
    await messageTemplateApi.delete(row.basicId)
    message.success(t('message.template.msg_delete_success'))
    schemaPageRef.value?.reload()
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || t('message.template.msg_delete_failed'))
  }
}

async function handleSubmit() {
  const form = templateForm.value
  if (!form.templateCode.trim() && !form.basicId) {
    message.warning(t('message.template.msg_code_required'))
    return
  }
  if (!form.templateName.trim()) {
    message.warning(t('message.template.msg_name_required'))
    return
  }
  if (!form.content.trim()) {
    message.warning(t('message.template.msg_content_required'))
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
      message.success(t('message.template.msg_update_success'))
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
      message.success(t('message.template.msg_create_success'))
    }
    modalVisible.value = false
    schemaPageRef.value?.reload()
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || t('message.template.msg_save_failed'))
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
          <NFormItem :label="t('message.template.form_template_code')" path="templateCode">
            <NInput
              v-model:value="templateForm.templateCode"
              :disabled="Boolean(templateForm.basicId)"
              clearable
              :placeholder="t('message.template.form_template_code_placeholder')"
            />
          </NFormItem>
          <NFormItem :label="t('message.template.form_channel')" path="channel">
            <NSelect
              v-model:value="templateForm.channel"
              :disabled="Boolean(templateForm.basicId)"
              :options="channelOptions"
            />
          </NFormItem>
          <NFormItem :label="t('message.template.form_template_name')" path="templateName">
            <NInput v-model:value="templateForm.templateName" clearable :placeholder="t('message.template.form_template_name_placeholder')" />
          </NFormItem>
          <NFormItem :label="t('message.template.form_sort')" path="sort">
            <NInputNumber v-model:value="templateForm.sort" :min="0" style="width: 100%" />
          </NFormItem>
        </div>
        <NFormItem :label="t('message.template.form_subject')" path="subject">
          <NInput v-model:value="templateForm.subject" clearable :placeholder="t('message.template.form_subject_placeholder')" />
        </NFormItem>
        <NFormItem :label="t('message.template.form_content')" path="content">
          <NInput
            v-model:value="templateForm.content"
            type="textarea"
            :autosize="{ minRows: 8, maxRows: 18 }"
            :placeholder="t('message.template.form_content_placeholder')"
          />
        </NFormItem>
        <div class="grid grid-cols-2 gap-x-4">
          <NFormItem :label="t('message.template.form_is_html')" path="isHtml">
            <NSwitch v-model:value="templateForm.isHtml" />
          </NFormItem>
          <NFormItem v-if="!templateForm.basicId" :label="t('message.template.form_status')" path="status">
            <NSelect v-model:value="templateForm.status" :options="statusOptions" />
          </NFormItem>
        </div>
        <NFormItem :label="t('message.template.form_description')" path="description">
          <NInput v-model:value="templateForm.description" clearable :placeholder="t('message.template.form_description_placeholder')" />
        </NFormItem>
        <NFormItem :label="t('message.template.form_remark')" path="remark">
          <NInput v-model:value="templateForm.remark" clearable />
        </NFormItem>
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

    <!-- 详情 -->
    <NModal
      v-model:show="detailVisible"
      preset="card"
      :title="t('message.template.detail_title')"
      style="width: 720px"
    >
      <template v-if="currentDetail">
        <NDescriptions :column="2" label-placement="left" bordered size="small">
          <NDescriptionsItem :label="t('message.template.detail_template_code')">
            {{ currentDetail.templateCode }}
          </NDescriptionsItem>
          <NDescriptionsItem :label="t('message.template.detail_channel')">
            {{ getOptionLabel(channelOptions, currentDetail.channel) }}
          </NDescriptionsItem>
          <NDescriptionsItem :label="t('message.template.detail_template_name')">
            {{ currentDetail.templateName }}
          </NDescriptionsItem>
          <NDescriptionsItem :label="t('message.template.detail_scope')">
            {{ currentDetail.isGlobal ? t('message.template.scope_global') : t('message.template.scope_tenant') }}
          </NDescriptionsItem>
          <NDescriptionsItem :label="t('message.template.detail_subject')" :span="2">
            {{ currentDetail.subject || '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem :label="t('message.template.detail_description')" :span="2">
            {{ currentDetail.description || '-' }}
          </NDescriptionsItem>
        </NDescriptions>
        <div class="mt-3 text-xs opacity-70">
          {{ t('message.template.detail_content_title') }}
        </div>
        <pre class="mt-1 max-h-80 overflow-auto rounded-lg border border-[hsl(var(--border))] bg-[hsl(var(--muted)/40%)] p-3 text-xs leading-relaxed whitespace-pre-wrap">{{ currentDetail.content }}</pre>
      </template>
    </NModal>
  </SchemaPage>
</template>
