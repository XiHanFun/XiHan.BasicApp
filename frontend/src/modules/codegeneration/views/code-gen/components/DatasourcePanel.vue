<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<!--
  代码生成 / 数据源页签
  接口取自后端契约（codegen-datasources.*）：测试连接要修改权限；删除逐项拒绝仍被表配置引用的数据源；口令与连接串不回显，编辑时留空即不改
-->
<script setup lang="ts">
import type { DatasourceRow, DatasourceStatus } from './use-datasources'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaPageInstance } from '~/components'
import type { SelectOption } from '~/types'
import { XhFieldControl, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFormRoot, XhSwitch, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, h, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { deleteConfirmText, renderTag, SchemaPage, XEditModal, XInput, XNumberInput, XSelect } from '~/components'
import { toast } from '~/composables'
import { codegenDatasources } from '~/contracts'
import { throwOnFailed } from '~/request'
import { DATABASE_TYPE_OPTIONS, databaseTypeLabel, datasourcesResource, useDatasourceEditor } from './use-datasources'

defineOptions({ name: 'CodegenDatasourcePanel' })

const { t } = useI18n()
const editor = useDatasourceEditor()
const resource = datasourcesResource()

/** 编辑弹窗的保存钮靠这个 id 关联到表单，点它才会走整表提交 */
const editFormId = useId()
const schemaPageRef = ref<SchemaPageInstance | null>(null)
const testingId = ref<string | null>(null)

function reload() {
  void schemaPageRef.value?.reload()
}

const asRow = (row: object) => row as DatasourceRow

function statusLabel(status: DatasourceStatus): string {
  return status === 'Active'
    ? t('common.statuses.enabled')
    : t('common.statuses.disabled')
}

const statusOptions = computed<SelectOption[]>(() => (['Active', 'Disabled'] as const).map(status => ({
  label: statusLabel(status),
  value: status,
})))
const databaseTypeOptions = computed<SelectOption[]>(() => DATABASE_TYPE_OPTIONS.map(option => ({ label: option.label, value: option.value })))

const fields = computed<ListFieldSchema[]>(() => [
  // 仅搜索（不作为列）
  {
    key: 'keyword',
    title: t('codegeneration.code_gen.datasource.col_source_name'),
    dataType: 'string',
    visible: false,
    searchable: true,
    searchPlaceholder: t('codegeneration.code_gen.datasource.search_placeholder'),
    order: 0,
  },
  {
    key: 'sourceName',
    title: t('codegeneration.code_gen.datasource.col_source_name'),
    dataType: 'string',
    minWidth: 160,
    fixed: 'left',
    sortable: true,
    order: 1,
    render: (row) => {
      const r = asRow(row)
      return h('div', { class: 'ds-name' }, [
        h('span', { class: 'ds-name__text' }, r.sourceName),
        r.isDefault
          ? h(XhTagRoot, { variant: 'subtle', tone: 'info' }, () => h(XhTagLabel, () => t('codegeneration.code_gen.common.default_tag')))
          : null,
      ])
    },
  },
  {
    key: 'databaseType',
    title: t('codegeneration.code_gen.datasource.col_database'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    options: databaseTypeOptions.value,
    searchPlaceholder: t('codegeneration.code_gen.datasource.filter_database_type'),
    width: 110,
    order: 2,
    render: row => databaseTypeLabel(asRow(row).databaseType),
  },
  {
    key: 'host',
    title: t('codegeneration.code_gen.datasource.col_host'),
    dataType: 'string',
    minWidth: 140,
    sortable: true,
    order: 3,
    render: row => `${asRow(row).host}:${asRow(row).port}`,
  },
  {
    key: 'databaseName',
    title: t('codegeneration.code_gen.datasource.col_database_name'),
    dataType: 'string',
    minWidth: 120,
    sortable: true,
    order: 4,
  },
  {
    key: 'lastTestResult',
    title: t('codegeneration.code_gen.datasource.col_connection'),
    dataType: 'enum',
    width: 90,
    sortable: true,
    order: 5,
    render: (row) => {
      const r = asRow(row)
      if (!r.lastTestAt)
        return renderTag(t('codegeneration.code_gen.datasource.tag_untested'), 'neutral')
      return r.lastTestResult
        ? renderTag(t('codegeneration.code_gen.datasource.tag_normal'), 'success')
        : renderTag(t('codegeneration.code_gen.datasource.tag_failed'), 'danger')
    },
  },
  {
    key: 'status',
    title: t('codegeneration.code_gen.common.status'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    options: statusOptions.value,
    searchPlaceholder: t('codegeneration.code_gen.common.status'),
    width: 90,
    order: 6,
    render: row => renderTag(statusLabel(asRow(row).status), asRow(row).status === 'Active'
      ? 'success'
      : 'danger'),
  },
  {
    key: 'order',
    title: t('codegeneration.code_gen.datasource.form_sort'),
    dataType: 'number',
    width: 80,
    sortable: true,
    order: 7,
  },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'codegeneration.code-gen.datasource',
  pageName: t('codegeneration.code_gen.tabs.datasource'),
  rowKey: 'id',
  fields: fields.value,
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    {
      key: 'create',
      title: t('codegeneration.code_gen.datasource.add'),
      scope: 'page',
      tone: 'brand',
      icon: 'lucide:plus',
      permission: 'codegeneration.codegen-datasources.create',
    },
    {
      key: 'test',
      title: t('codegeneration.code_gen.datasource.action_test'),
      scope: 'row',
      tone: 'info',
      icon: 'lucide:plug',
      permission: 'codegeneration.codegen-datasources.update',
      disabled: row => testingId.value === asRow(row).id,
    },
    {
      key: 'edit',
      title: t('common.actions.edit'),
      scope: 'row',
      icon: 'lucide:pencil',
      permission: 'codegeneration.codegen-datasources.update',
    },
    {
      key: 'delete',
      title: t('common.actions.delete'),
      scope: 'row',
      tone: 'danger',
      icon: 'lucide:trash-2',
      confirm: true,
      confirmText: row => deleteConfirmText(t, asRow(row).sourceName),
      permission: 'codegeneration.codegen-datasources.delete',
    },
  ],
}))

function onAction(payload: SchemaActionPayload) {
  const row = payload.row
    ? asRow(payload.row)
    : undefined
  switch (payload.key) {
    case 'create':
      editor.startCreate()
      break
    case 'test':
      if (row)
        void handleTest(row)
      break
    case 'edit':
      if (row)
        void handleEdit(row)
      break
    case 'delete':
      if (row)
        void handleDelete(row)
      break
  }
}

async function handleTest(row: DatasourceRow) {
  testingId.value = row.id
  try {
    const result = await codegenDatasources.testConnection({ id: row.id })
    if (result.success)
      toast.success(t('codegeneration.code_gen.datasource.test_success', { ms: result.elapsedMilliseconds }))
    else
      toast.danger(result.message || t('codegeneration.code_gen.datasource.test_failed'))
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('codegeneration.code_gen.datasource.test_error'))
  }
  finally {
    testingId.value = null
  }
}

async function handleEdit(row: DatasourceRow) {
  try {
    await editor.startEdit(row.id)
  }
  catch (error) {
    toast.danger((error as Error).message || t('codegeneration.code_gen.datasource.load_detail_failed'))
  }
}

async function handleDelete(row: DatasourceRow) {
  try {
    throwOnFailed(await codegenDatasources.delete({
      ids: [row.id],
    }))
    toast.success(t('common.messages.delete_success'))
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.delete_failed'))
  }
}

const modalTitle = computed(() => editor.isEdit.value
  ? t('codegeneration.code_gen.datasource.modal_edit_title')
  : t('codegeneration.code_gen.datasource.modal_add_title'))

async function onSubmit() {
  if (await editor.submit(t('codegeneration.code_gen.datasource.update_status_remark'))) {
    toast.success(t('common.messages.save_success'))
    reload()
  }
  else if (editor.submitError.value) {
    toast.danger(editor.submitError.value)
  }
}
</script>

<template>
  <SchemaPage ref="schemaPageRef" :schema="schema" @action="onAction">
    <XEditModal
      v-model:show="editor.open.value"
      :title="modalTitle"
      :loading="editor.submitting.value"
      :form-id="editFormId"
    >
      <XhFormRoot :id="editFormId" class="xh-edit-form-grid" novalidate @submit="onSubmit">
        <XhFieldRoot :invalid="!!editor.fieldError('sourceName')" required>
          <XhFieldLabel>{{ t('codegeneration.code_gen.datasource.form_source_name') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.sourceName" name="sourceName" clearable :placeholder="t('codegeneration.code_gen.datasource.form_source_name_placeholder')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('sourceName')">
            {{ editor.fieldError('sourceName') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('databaseType')">
          <XhFieldLabel>{{ t('codegeneration.code_gen.datasource.form_database_type') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect v-model:value="editor.form.databaseType" name="databaseType" :options="databaseTypeOptions" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('databaseType')">
            {{ editor.fieldError('databaseType') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('host')" required>
          <XhFieldLabel>{{ t('codegeneration.code_gen.datasource.form_host') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.host" name="host" clearable :placeholder="t('codegeneration.code_gen.datasource.form_host_placeholder')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('host')">
            {{ editor.fieldError('host') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('port')">
          <XhFieldLabel>{{ t('codegeneration.code_gen.datasource.form_port') }}</XhFieldLabel>
          <XhFieldControl>
            <XNumberInput v-model:value="editor.form.port" name="port" :min="0" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('port')">
            {{ editor.fieldError('port') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('databaseName')" required>
          <XhFieldLabel>{{ t('codegeneration.code_gen.datasource.form_database_name') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.databaseName" name="databaseName" clearable />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('databaseName')">
            {{ editor.fieldError('databaseName') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('userName')" required>
          <XhFieldLabel>{{ t('codegeneration.code_gen.datasource.form_user_name') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.userName" name="userName" clearable autocomplete="off" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('userName')">
            {{ editor.fieldError('userName') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('password')">
          <XhFieldLabel>{{ editor.isEdit.value ? t('codegeneration.code_gen.datasource.form_password_edit') : t('codegeneration.code_gen.datasource.form_password') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.password" name="password" clearable type="password" autocomplete="new-password" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('password')">
            {{ editor.fieldError('password') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('connectionTimeout')">
          <XhFieldLabel>{{ t('codegeneration.code_gen.datasource.form_connection_timeout') }}</XhFieldLabel>
          <XhFieldControl>
            <XNumberInput v-model:value="editor.form.connectionTimeout" name="connectionTimeout" :min="0" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('connectionTimeout')">
            {{ editor.fieldError('connectionTimeout') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('order')">
          <XhFieldLabel>{{ t('codegeneration.code_gen.datasource.form_sort') }}</XhFieldLabel>
          <XhFieldControl>
            <XNumberInput v-model:value="editor.form.order" name="order" :min="0" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('order')">
            {{ editor.fieldError('order') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('status')">
          <XhFieldLabel>{{ t('codegeneration.code_gen.common.status') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect v-model:value="editor.form.status" name="status" :options="statusOptions" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('status')">
            {{ editor.fieldError('status') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('isDefault')">
          <XhFieldLabel>{{ t('codegeneration.code_gen.datasource.form_is_default') }}</XhFieldLabel>
          <XhFieldControl>
            <XhSwitch v-model:checked="editor.form.isDefault" name="isDefault" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('isDefault')">
            {{ editor.fieldError('isDefault') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('connectionString')" class="xh-span-2">
          <XhFieldLabel>{{ editor.isEdit.value ? t('codegeneration.code_gen.datasource.form_connection_string_edit') : t('codegeneration.code_gen.datasource.form_connection_string') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput
              v-model:value="editor.form.connectionString"
              name="connectionString"
              clearable
              :placeholder="editor.editing.value?.hasConnectionString ? t('codegeneration.code_gen.datasource.connection_string_set') : t('codegeneration.code_gen.datasource.form_connection_string_placeholder')"
              :rows="2"
              type="textarea"
            />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('connectionString')">
            {{ editor.fieldError('connectionString') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('sourceDescription')" class="xh-span-2">
          <XhFieldLabel>{{ t('codegeneration.code_gen.datasource.form_description') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.sourceDescription" name="sourceDescription" clearable :rows="2" type="textarea" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('sourceDescription')">
            {{ editor.fieldError('sourceDescription') }}
          </XhFieldErrorText>
        </XhFieldRoot>
      </XhFormRoot>
    </XEditModal>
  </SchemaPage>
</template>

<style scoped>
:deep(.ds-name) {
  display: flex;
  align-items: center;
  gap: var(--xh-space-1);
  min-width: 0;
}

:deep(.ds-name__text) {
  font-weight: 500;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
</style>
