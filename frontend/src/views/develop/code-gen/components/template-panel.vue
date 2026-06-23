<script setup lang="ts">
import type { DataTableColumns } from 'naive-ui'
import type {
  CodeGenTemplateCreateDto,
  CodeGenTemplateListItemDto,
  CodeGenTemplateUpdateDto,
  TemplateEngine,
  TemplateType,
} from '@/api'
import {
  NButton,
  NDataTable,
  NForm,
  NFormItem,
  NIcon,
  NInput,
  NInputNumber,
  NModal,
  NPagination,
  NPopconfirm,
  NSelect,
  NSpace,
  NTag,
  useMessage,
} from 'naive-ui'
import { computed, h, onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  codeGenTemplateApi,
  createPageRequest,
  EnableStatus,
  TEMPLATE_ENGINE_OPTIONS,
  TEMPLATE_TYPE_OPTIONS,
  TemplateEngine as TemplateEngineEnum,
  TemplateType as TemplateTypeEnum,
} from '@/api'
import { Icon } from '~/components'
import { STATUS_OPTIONS } from '~/constants'
import { getOptionLabel } from '~/utils'

defineOptions({ name: 'CodeGenTemplatePanel' })

interface TemplateFormModel {
  basicId?: string
  templateCode: string
  templateName: string
  templateDescription?: string | null
  templateGroup?: string | null
  templateType: TemplateType
  templateEngine: TemplateEngine
  templateContent?: string | null
  fileNameExpression?: string | null
  filePathExpression?: string | null
  fileExtension?: string | null
  sort: number
  isEnabled: boolean
  status: EnableStatus
  remark?: string | null
}

const { t } = useI18n()
const message = useMessage()

const loading = ref(false)
const list = ref<CodeGenTemplateListItemDto[]>([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(20)
const queryParams = reactive({
  keyword: '',
  templateType: null as TemplateType | null,
  templateEngine: null as TemplateEngine | null,
  status: null as EnableStatus | null,
})

async function fetchData() {
  loading.value = true
  try {
    const result = await codeGenTemplateApi.page({
      ...createPageRequest({ page: { pageIndex: page.value, pageSize: pageSize.value } }),
      keyword: queryParams.keyword?.trim() || undefined,
      templateEngine: queryParams.templateEngine ?? undefined,
      templateType: queryParams.templateType ?? undefined,
      status: queryParams.status ?? undefined,
    })
    list.value = result.items
    total.value = result.page.totalCount
  }
  catch {
    message.error(t('develop.code_gen.template.query_failed'))
    list.value = []
    total.value = 0
  }
  finally {
    loading.value = false
  }
}

function handleSearch() {
  page.value = 1
  fetchData()
}

function handlePageChange(value: number) {
  page.value = value
  fetchData()
}

function handlePageSizeChange(value: number) {
  pageSize.value = value
  page.value = 1
  fetchData()
}

const columns = computed<DataTableColumns<CodeGenTemplateListItemDto>>(() => [
  {
    key: 'templateName',
    title: t('develop.code_gen.template.col_template_name'),
    minWidth: 160,
    ellipsis: { tooltip: true },
    render: (row: CodeGenTemplateListItemDto) =>
      h('div', { class: 'tpl-name' }, [
        h('span', { class: 'tpl-name__text' }, row.templateName),
        row.isBuiltIn
          ? h(NTag, { size: 'tiny', type: 'warning', round: true, bordered: false }, () => t('develop.code_gen.common.builtin_tag'))
          : null,
      ]),
  },
  {
    key: 'templateCode',
    title: t('develop.code_gen.template.col_template_code'),
    minWidth: 150,
    ellipsis: { tooltip: true },
  },
  {
    key: 'templateGroup',
    title: t('develop.code_gen.template.col_template_group'),
    width: 120,
    ellipsis: { tooltip: true },
  },
  {
    key: 'templateType',
    title: t('develop.code_gen.template.col_template_type'),
    width: 90,
    render: (row: CodeGenTemplateListItemDto) => getOptionLabel(TEMPLATE_TYPE_OPTIONS, row.templateType),
  },
  {
    key: 'templateEngine',
    title: t('develop.code_gen.template.col_template_engine'),
    width: 90,
    render: (row: CodeGenTemplateListItemDto) => getOptionLabel(TEMPLATE_ENGINE_OPTIONS, row.templateEngine),
  },
  {
    key: 'fileExtension',
    title: t('develop.code_gen.template.col_file_extension'),
    width: 90,
  },
  {
    key: 'isEnabled',
    title: t('develop.code_gen.template.col_enabled'),
    width: 72,
    align: 'center',
    render: (row: CodeGenTemplateListItemDto) =>
      h(NTag, {
        size: 'small',
        round: true,
        bordered: false,
        type: row.isEnabled ? 'success' : 'default',
      }, () => (row.isEnabled ? t('develop.code_gen.common.yes') : t('develop.code_gen.common.no'))),
  },
  {
    key: 'status',
    title: t('develop.code_gen.common.status'),
    width: 72,
    align: 'center',
    render: (row: CodeGenTemplateListItemDto) =>
      h(NTag, {
        size: 'small',
        round: true,
        bordered: false,
        type: row.status === EnableStatus.Enabled ? 'success' : 'error',
      }, () => getOptionLabel(STATUS_OPTIONS, row.status)),
  },
  {
    key: 'actions',
    title: t('develop.code_gen.common.actions'),
    width: 110,
    align: 'center',
    render: (row: CodeGenTemplateListItemDto) =>
      h(NSpace, { size: 4, justify: 'center', wrap: false }, () => [
        h(NButton, {
          ariaLabel: t('develop.code_gen.common.edit'),
          circle: true,
          quaternary: true,
          size: 'small',
          type: 'primary',
          onClick: () => handleEdit(row),
        }, { icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:pencil' })) }),
        h(NPopconfirm, { onPositiveClick: () => handleDelete(row), disabled: row.isBuiltIn }, {
          trigger: () => h(NButton, {
            ariaLabel: t('develop.code_gen.common.delete'),
            circle: true,
            quaternary: true,
            size: 'small',
            type: 'error',
            disabled: row.isBuiltIn,
            onClick: () => {
              if (row.isBuiltIn) {
                message.warning(t('develop.code_gen.template.builtin_cannot_delete'))
              }
            },
          }, { icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:trash-2' })) }),
          default: () => t('develop.code_gen.template.confirm_delete'),
        }),
      ]),
  },
])

async function handleDelete(row: CodeGenTemplateListItemDto) {
  if (row.isBuiltIn) {
    message.warning(t('develop.code_gen.template.builtin_cannot_delete'))
    return
  }
  try {
    await codeGenTemplateApi.delete(row.basicId)
    message.success(t('develop.code_gen.common.delete_success'))
    fetchData()
  }
  catch {
    message.error(t('develop.code_gen.common.delete_failed'))
  }
}

// ── 表单/弹窗 ───────────────────────────────────────────────────
const modalVisible = ref(false)
const submitLoading = ref(false)
const validating = ref(false)
const editingStatus = ref<EnableStatus | null>(null)
const form = ref<TemplateFormModel>(createDefaultForm())
const modalTitle = computed(() => (form.value.basicId ? t('develop.code_gen.template.modal_edit_title') : t('develop.code_gen.template.modal_add_title')))

function createDefaultForm(): TemplateFormModel {
  return {
    templateCode: '',
    templateName: '',
    templateDescription: null,
    templateGroup: null,
    templateType: TemplateTypeEnum.Single,
    templateEngine: TemplateEngineEnum.Scriban,
    templateContent: null,
    fileNameExpression: null,
    filePathExpression: null,
    fileExtension: null,
    sort: 100,
    isEnabled: true,
    status: EnableStatus.Enabled,
    remark: null,
  }
}

function handleAdd() {
  editingStatus.value = null
  form.value = createDefaultForm()
  modalVisible.value = true
}

async function handleEdit(row: CodeGenTemplateListItemDto) {
  try {
    const detail = await codeGenTemplateApi.detail(row.basicId)
    if (!detail) {
      message.error(t('develop.code_gen.template.not_found'))
      return
    }
    editingStatus.value = detail.status
    form.value = {
      basicId: detail.basicId,
      templateCode: detail.templateCode,
      templateName: detail.templateName,
      templateDescription: detail.templateDescription ?? null,
      templateGroup: detail.templateGroup ?? null,
      templateType: detail.templateType,
      templateEngine: detail.templateEngine,
      templateContent: detail.templateContent ?? null,
      fileNameExpression: detail.fileNameExpression ?? null,
      filePathExpression: detail.filePathExpression ?? null,
      fileExtension: detail.fileExtension ?? null,
      sort: detail.sort,
      isEnabled: detail.isEnabled,
      status: detail.status,
      remark: detail.remark ?? null,
    }
    modalVisible.value = true
  }
  catch {
    message.error(t('develop.code_gen.template.load_detail_failed'))
  }
}

async function handleValidate() {
  if (!form.value.templateContent?.trim()) {
    message.warning(t('develop.code_gen.template.validate_content_required'))
    return
  }
  validating.value = true
  try {
    const result = await codeGenTemplateApi.validate({
      templateContent: form.value.templateContent,
      templateEngine: form.value.templateEngine,
    })
    if (result.isValid) {
      message.success(t('develop.code_gen.template.validate_pass'))
    }
    else {
      message.error(t('develop.code_gen.template.validate_fail', { errors: result.errors.join('；') || t('develop.code_gen.template.validate_unknown_error') }))
    }
  }
  catch {
    message.error(t('develop.code_gen.template.validate_error'))
  }
  finally {
    validating.value = false
  }
}

function validateForm() {
  if (!form.value.templateName.trim()) {
    message.warning(t('develop.code_gen.template.validate_name_required'))
    return false
  }
  if (!form.value.basicId && !form.value.templateCode.trim()) {
    message.warning(t('develop.code_gen.template.validate_code_required'))
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
    if (form.value.basicId) {
      const updateInput: CodeGenTemplateUpdateDto = {
        basicId: form.value.basicId,
        templateName: form.value.templateName.trim(),
        templateDescription: form.value.templateDescription,
        templateGroup: form.value.templateGroup,
        templateType: form.value.templateType,
        templateEngine: form.value.templateEngine,
        templateContent: form.value.templateContent,
        fileNameExpression: form.value.fileNameExpression,
        filePathExpression: form.value.filePathExpression,
        fileExtension: form.value.fileExtension,
        isEnabled: form.value.isEnabled,
        sort: form.value.sort,
        remark: form.value.remark,
      }
      await codeGenTemplateApi.update(updateInput)
      if (editingStatus.value !== form.value.status) {
        await codeGenTemplateApi.updateStatus({
          basicId: form.value.basicId,
          remark: t('develop.code_gen.template.update_status_remark'),
          status: form.value.status,
        })
      }
    }
    else {
      const createInput: CodeGenTemplateCreateDto = {
        templateCode: form.value.templateCode.trim(),
        templateName: form.value.templateName.trim(),
        templateDescription: form.value.templateDescription,
        templateGroup: form.value.templateGroup,
        templateType: form.value.templateType,
        templateEngine: form.value.templateEngine,
        templateContent: form.value.templateContent,
        fileNameExpression: form.value.fileNameExpression,
        filePathExpression: form.value.filePathExpression,
        fileExtension: form.value.fileExtension,
        sort: form.value.sort,
        status: form.value.status,
        remark: form.value.remark,
      }
      await codeGenTemplateApi.create(createInput)
    }
    message.success(t('develop.code_gen.common.save_success'))
    modalVisible.value = false
    fetchData()
  }
  catch {
    message.error(t('develop.code_gen.common.save_failed'))
  }
  finally {
    submitLoading.value = false
  }
}

onMounted(fetchData)
</script>

<template>
  <div class="panel">
    <div class="panel__toolbar">
      <NInput
        v-model:value="queryParams.keyword"
        class="panel__kw"
        clearable
        :placeholder="t('develop.code_gen.template.search_placeholder')"
        size="small"
        @clear="handleSearch"
        @keyup.enter="handleSearch"
      />
      <NSelect
        v-model:value="queryParams.templateType"
        class="panel__filter"
        clearable
        :options="TEMPLATE_TYPE_OPTIONS"
        :placeholder="t('develop.code_gen.template.filter_template_type')"
        size="small"
        @update:value="handleSearch"
      />
      <NSelect
        v-model:value="queryParams.templateEngine"
        class="panel__filter"
        clearable
        :options="TEMPLATE_ENGINE_OPTIONS"
        :placeholder="t('develop.code_gen.template.filter_template_engine')"
        size="small"
        @update:value="handleSearch"
      />
      <NSelect
        v-model:value="queryParams.status"
        class="panel__filter"
        clearable
        :options="STATUS_OPTIONS"
        :placeholder="t('develop.code_gen.common.status')"
        size="small"
        @update:value="handleSearch"
      />
      <NButton size="small" type="primary" @click="handleSearch">
        {{ t('develop.code_gen.common.search') }}
      </NButton>
      <NButton class="panel__add" size="small" type="primary" @click="handleAdd">
        <template #icon>
          <NIcon><Icon icon="lucide:plus" /></NIcon>
        </template>
        {{ t('develop.code_gen.template.add') }}
      </NButton>
    </div>

    <div class="panel__body">
      <NDataTable
        class="panel__table"
        flex-height
        :columns="columns"
        :data="list"
        :loading="loading"
        :row-key="(row: CodeGenTemplateListItemDto) => row.basicId"
        :scroll-x="1200"
        size="small"
      />
    </div>

    <div class="panel__foot">
      <NPagination
        v-model:page="page"
        v-model:page-size="pageSize"
        :item-count="total"
        :page-sizes="[10, 20, 50, 100]"
        show-size-picker
        @update:page="handlePageChange"
        @update:page-size="handlePageSizeChange"
      />
    </div>

    <NModal
      v-model:show="modalVisible"
      :auto-focus="false"
      :bordered="false"
      :title="modalTitle"
      preset="card"
      style="width: 820px; max-width: 94vw"
    >
      <NForm :model="form" class="xh-edit-form-grid" label-placement="top">
        <NFormItem :label="t('develop.code_gen.template.form_template_code')" path="templateCode">
          <NInput
            v-model:value="form.templateCode"
            clearable
            :disabled="Boolean(form.basicId)"
            :placeholder="t('develop.code_gen.template.form_template_code_placeholder')"
          />
        </NFormItem>
        <NFormItem :label="t('develop.code_gen.template.form_template_name')" path="templateName">
          <NInput v-model:value="form.templateName" clearable />
        </NFormItem>
        <NFormItem :label="t('develop.code_gen.template.form_template_group')" path="templateGroup">
          <NInput v-model:value="form.templateGroup" clearable :placeholder="t('develop.code_gen.template.form_template_group_placeholder')" />
        </NFormItem>
        <NFormItem :label="t('develop.code_gen.template.form_template_type')" path="templateType">
          <NSelect v-model:value="form.templateType" :options="TEMPLATE_TYPE_OPTIONS" />
        </NFormItem>
        <NFormItem :label="t('develop.code_gen.template.form_template_engine')" path="templateEngine">
          <NSelect v-model:value="form.templateEngine" :options="TEMPLATE_ENGINE_OPTIONS" />
        </NFormItem>
        <NFormItem :label="t('develop.code_gen.template.form_file_extension')" path="fileExtension">
          <NInput v-model:value="form.fileExtension" clearable :placeholder="t('develop.code_gen.template.form_file_extension_placeholder')" />
        </NFormItem>
        <NFormItem :label="t('develop.code_gen.template.form_file_name_expression')" path="fileNameExpression">
          <NInput v-model:value="form.fileNameExpression" clearable :placeholder="t('develop.code_gen.template.form_file_name_expression_placeholder')" />
        </NFormItem>
        <NFormItem :label="t('develop.code_gen.template.form_file_path_expression')" path="filePathExpression">
          <NInput v-model:value="form.filePathExpression" clearable />
        </NFormItem>
        <NFormItem :label="t('develop.code_gen.template.form_sort')" path="sort">
          <NInputNumber v-model:value="form.sort" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem v-if="!form.basicId" :label="t('develop.code_gen.common.status')" path="status">
          <NSelect v-model:value="form.status" :options="STATUS_OPTIONS" />
        </NFormItem>
        <NFormItem class="xh-form-full" :label="t('develop.code_gen.template.form_template_description')" path="templateDescription">
          <NInput v-model:value="form.templateDescription" clearable :rows="2" type="textarea" />
        </NFormItem>
        <NFormItem class="xh-form-full" :label="t('develop.code_gen.template.form_template_content')" path="templateContent">
          <NInput
            v-model:value="form.templateContent"
            :placeholder="t('develop.code_gen.template.form_template_content_placeholder')"
            :rows="12"
            type="textarea"
          />
        </NFormItem>
      </NForm>

      <template #footer>
        <NSpace justify="space-between">
          <NButton :loading="validating" @click="handleValidate">
            <template #icon>
              <NIcon><Icon icon="lucide:check-check" /></NIcon>
            </template>
            {{ t('develop.code_gen.template.validate_syntax') }}
          </NButton>
          <NSpace>
            <NButton @click="modalVisible = false">
              {{ t('develop.code_gen.common.cancel') }}
            </NButton>
            <NButton :loading="submitLoading" type="primary" @click="handleSubmit">
              {{ t('develop.code_gen.common.save') }}
            </NButton>
          </NSpace>
        </NSpace>
      </template>
    </NModal>
  </div>
</template>

<style scoped>
.panel {
  display: flex;
  flex-direction: column;
  height: 100%;
  min-height: 0;
}

.panel__toolbar {
  display: flex;
  flex-shrink: 0;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
  padding-bottom: 10px;
}

.panel__kw {
  width: 220px;
}

.panel__filter {
  width: 130px;
  flex-shrink: 0;
}

.panel__add {
  margin-left: auto;
}

.panel__body {
  flex: 1;
  min-height: 0;
  display: flex;
  flex-direction: column;
}

.panel__table {
  flex: 1;
  min-height: 0;
}

.panel__foot {
  display: flex;
  flex-shrink: 0;
  justify-content: flex-end;
  padding-top: 10px;
}

.tpl-name {
  display: flex;
  align-items: center;
  gap: 6px;
  min-width: 0;
}

.tpl-name__text {
  font-weight: 500;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
</style>
