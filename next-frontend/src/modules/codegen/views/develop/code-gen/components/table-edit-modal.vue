<script setup lang="ts">
import type {
  CodeGenTableUpdateDto,
  DatabaseType,
  EnableStatus,
  GenerationScope,
  GenType,
  TemplateType,
} from '../../../../api'
import type {
  ApiId,
} from '@/api'
import { XhFieldControl, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFormFieldGroup, XhFormRoot, XhSpinner } from '@xihan-ui/vue'
import { computed, ref, useId, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { STATUS_OPTIONS } from '@/constants'
import { XEditModal, XInput, XSelect } from '~/components'
import { toast } from '~/composables'
import { useEnumOptions } from '~/hooks'
import {
  codeGenTableApi,
  DATABASE_TYPE_OPTIONS,
  DatabaseType as DatabaseTypeEnum,
  ENABLED_ACTION_OPTIONS,
  EnableStatus as EnableStatusEnum,
  GEN_TYPE_OPTIONS,
  GENERATION_SCOPE_OPTIONS,
  GenerationScope as GenerationScopeEnum,
  GenType as GenTypeEnum,
  TABLE_TEMPLATE_TYPE_OPTIONS,
  TemplateType as TemplateTypeEnum,
} from '../../../../api'

defineOptions({ name: 'CodeGenTableEditModal' })

const props = defineProps<{
  show: boolean
  tableId: ApiId | null
}>()

const emit = defineEmits<{
  'update:show': [value: boolean]
  'saved': []
}>()

const { t } = useI18n()

/** 编辑弹窗的保存钮靠这个 id 关联到表单，点它才会走整表校验 */
const editFormId = useId()

const statusEnumOptions = useEnumOptions('EnableStatus', STATUS_OPTIONS)

interface TableFormModel {
  basicId: string
  tableName: string
  tableComment?: string | null
  className: string
  namespace?: string | null
  moduleName?: string | null
  businessName?: string | null
  functionName?: string | null
  author?: string | null
  templateType: TemplateType
  // 生成方式：决定列表「更多」里出现哪一个生成动作
  genType: GenType
  generationScope: GenerationScope
  // 包含操作以字符串数组建模（多选控件）；提交时 join 为逗号分隔串，空/全等价全开
  enabledActions: string[]
  genPath?: string | null
  // 上级菜单：M1 仅随详情回传（不写死 null），M3 接入菜单树选择控件
  parentMenuId?: ApiId | null
  primaryKeyColumn?: string | null
  treeParentColumn?: string | null
  treeNameColumn?: string | null
  masterTableId?: ApiId | null
  masterForeignKey?: string | null
  databaseType: DatabaseType
  dataSourceId?: ApiId | null
  status: EnableStatus
  remark?: string | null
}

/**
 * 全部可裁剪写操作（列表/详情为读取基线，不在此列）。
 * 必须声明在 form 之前：form 的初值由 createDefaultForm() 求得，而它引用本常量，
 * 声明晚于调用点会落进暂时性死区，setup 直接抛 ReferenceError、整个弹窗渲染不出来。
 */
const ALL_ACTIONS = ['create', 'update', 'delete']

const loading = ref(false)
const submitLoading = ref(false)
const editingStatus = ref<EnableStatus | null>(null)
const form = ref<TableFormModel>(createDefaultForm())

/** 本表列（供主键列/树列/子表外键列下拉选择，来源为详情随附的 columns） */
const columnOptions = ref<{ label: string, value: string }[]>([])
/** 其他表（供主子表的主表选择） */
const tableOptions = ref<{ label: string, value: ApiId }[]>([])

const isTreeTemplate = computed(() => form.value.templateType === TemplateTypeEnum.Tree)
const isMasterDetailTemplate = computed(() => form.value.templateType === TemplateTypeEnum.MasterDetail)

/** 后端逗号分隔串 → 多选数组；null/空视为全开（全部勾选） */
function parseEnabledActions(raw?: string | null): string[] {
  if (!raw) {
    return [...ALL_ACTIONS]
  }
  const selected = raw.split(',').map(item => item.trim()).filter(Boolean)
  return ALL_ACTIONS.filter(action => selected.includes(action))
}

function createDefaultForm(): TableFormModel {
  return {
    basicId: '',
    tableName: '',
    tableComment: null,
    className: '',
    namespace: null,
    moduleName: null,
    businessName: null,
    functionName: null,
    author: null,
    templateType: TemplateTypeEnum.Single,
    genType: GenTypeEnum.Zip,
    generationScope: GenerationScopeEnum.All,
    enabledActions: [...ALL_ACTIONS],
    genPath: null,
    parentMenuId: null,
    primaryKeyColumn: null,
    treeParentColumn: null,
    treeNameColumn: null,
    masterTableId: null,
    masterForeignKey: null,
    databaseType: DatabaseTypeEnum.MySql,
    dataSourceId: null,
    status: EnableStatusEnum.Enabled,
    remark: null,
  }
}

watch(
  () => props.show,
  (visible) => {
    if (!visible) {
      return
    }
    // 同步进入加载态并清掉上一行残留：否则弹窗会先渲染一版旧数据、再整体消失等待，高度连跳两次
    loading.value = true
    form.value = createDefaultForm()
    columnOptions.value = []
    if (props.tableId) {
      void loadDetail()
    }
  },
)

async function loadDetail() {
  if (!props.tableId) {
    loading.value = false
    return
  }
  loading.value = true
  try {
    const detail = await codeGenTableApi.detail(props.tableId)
    if (!detail) {
      toast.error(t('develop.code_gen.table_edit.not_found'))
      emit('update:show', false)
      return
    }
    editingStatus.value = detail.status
    columnOptions.value = (detail.columns ?? []).map(column => ({
      label: column.columnComment ? `${column.columnName}（${column.columnComment}）` : column.columnName,
      value: column.columnName,
    }))
    form.value = {
      basicId: detail.basicId,
      tableName: detail.tableName,
      tableComment: detail.tableComment ?? null,
      className: detail.className,
      namespace: detail.namespace ?? null,
      moduleName: detail.moduleName ?? null,
      businessName: detail.businessName ?? null,
      functionName: detail.functionName ?? null,
      author: detail.author ?? null,
      templateType: detail.templateType,
      genType: detail.genType,
      generationScope: detail.generationScope ?? GenerationScopeEnum.All,
      enabledActions: parseEnabledActions(detail.enabledActions),
      genPath: detail.genPath ?? null,
      parentMenuId: detail.parentMenuId ?? null,
      primaryKeyColumn: detail.primaryKeyColumn ?? null,
      treeParentColumn: detail.treeParentColumn ?? null,
      treeNameColumn: detail.treeNameColumn ?? null,
      masterTableId: detail.masterTableId ?? null,
      masterForeignKey: detail.masterForeignKey ?? null,
      databaseType: detail.databaseType,
      dataSourceId: detail.dataSourceId ?? null,
      status: detail.status,
      remark: detail.remark ?? null,
    }
    if (form.value.templateType === TemplateTypeEnum.MasterDetail) {
      void ensureTableOptions()
    }
  }
  catch (error) {
    toast.error((error as Error)?.message || t('develop.code_gen.table_edit.load_failed'))
  }
  finally {
    loading.value = false
  }
}

/** 惰性加载其他表列表（主子表选择主表用）；排除本表自身 */
async function ensureTableOptions() {
  if (tableOptions.value.length > 0) {
    return
  }
  try {
    const tables = await codeGenTableApi.options()
    tableOptions.value = tables
      .filter(item => item.basicId !== form.value.basicId)
      .map(item => ({ label: `${item.tableName}（${item.className}）`, value: item.basicId }))
  }
  catch {
    tableOptions.value = []
  }
}

// 切换模板类型：清空与新类型无关的结构字段，主子表时惰性拉取表列表
watch(() => form.value.templateType, (type) => {
  if (type !== TemplateTypeEnum.Tree) {
    form.value.treeParentColumn = null
    form.value.treeNameColumn = null
  }
  if (type !== TemplateTypeEnum.MasterDetail) {
    form.value.masterTableId = null
    form.value.masterForeignKey = null
  }
  else {
    void ensureTableOptions()
  }
})

function validateForm() {
  if (!form.value.tableName.trim()) {
    toast.warning(t('develop.code_gen.table_edit.validate_table_name'))
    return false
  }
  if (!form.value.className.trim()) {
    toast.warning(t('develop.code_gen.table_edit.validate_class_name'))
    return false
  }
  if (isTreeTemplate.value) {
    if (!form.value.treeParentColumn) {
      toast.warning(t('develop.code_gen.table_edit.validate_tree_parent_column'))
      return false
    }
    if (!form.value.treeNameColumn) {
      toast.warning(t('develop.code_gen.table_edit.validate_tree_name_column'))
      return false
    }
  }
  if (isMasterDetailTemplate.value) {
    if (!form.value.masterTableId) {
      toast.warning(t('develop.code_gen.table_edit.validate_master_table'))
      return false
    }
    if (!form.value.masterForeignKey) {
      toast.warning(t('develop.code_gen.table_edit.validate_master_foreign_key'))
      return false
    }
  }
  return true
}

async function handleSubmit() {
  if (!validateForm()) {
    return
  }
  submitLoading.value = true
  try {
    const updateInput: CodeGenTableUpdateDto = {
      basicId: form.value.basicId,
      tableName: form.value.tableName.trim(),
      tableComment: form.value.tableComment,
      className: form.value.className.trim(),
      namespace: form.value.namespace,
      moduleName: form.value.moduleName,
      businessName: form.value.businessName,
      functionName: form.value.functionName,
      author: form.value.author,
      templateType: form.value.templateType,
      genType: form.value.genType,
      generationScope: form.value.generationScope,
      // 全部/空数组都提交为空串，后端归一化为全开
      enabledActions: form.value.enabledActions.length === ALL_ACTIONS.length ? '' : form.value.enabledActions.join(','),
      genPath: form.value.genPath,
      parentMenuId: form.value.parentMenuId,
      primaryKeyColumn: form.value.primaryKeyColumn,
      treeParentColumn: isTreeTemplate.value ? form.value.treeParentColumn : null,
      treeNameColumn: isTreeTemplate.value ? form.value.treeNameColumn : null,
      masterTableId: isMasterDetailTemplate.value ? form.value.masterTableId : null,
      masterForeignKey: isMasterDetailTemplate.value ? form.value.masterForeignKey : null,
      databaseType: form.value.databaseType,
      dataSourceId: form.value.dataSourceId,
      options: null,
      status: form.value.status,
      remark: form.value.remark,
    }
    await codeGenTableApi.update(updateInput)
    if (editingStatus.value !== form.value.status) {
      await codeGenTableApi.updateStatus({
        basicId: form.value.basicId,
        remark: t('develop.code_gen.table_edit.update_status_remark'),
        status: form.value.status,
      })
    }
    toast.success(t('common.messages.save_success'))
    emit('saved')
    emit('update:show', false)
  }
  catch (error) {
    toast.error((error as Error)?.message || t('common.messages.save_failed'))
  }
  finally {
    submitLoading.value = false
  }
}
</script>

<template>
  <XEditModal
    :show="show"
    :title="t('develop.code_gen.table_edit.title')"
    :loading="submitLoading"
    :form-id="editFormId"
    @update:show="emit('update:show', $event)"
  >
    <!-- 表单常驻、加载期用 NSpin 遮罩：v-if 摘挂会让弹窗高度在打开瞬间连跳两次 -->
    <div class="xh-loading-stage">
      <div v-if="loading" class="xh-loading-stage__veil">
        <XhSpinner />
      </div>
      <XhFormRoot
        :id="editFormId"
        v-model:values="form"
        validate-on="blur"
        class="xh-edit-form-grid"
        @submit="handleSubmit"
      >
        <XhFormFieldGroup value="tableName">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.code_gen.table_edit.form_table_name') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.tableName" clearable />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="className">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.code_gen.table_edit.form_class_name') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.className" clearable />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="namespace">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.code_gen.table_edit.form_namespace') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.namespace" clearable />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="moduleName">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.code_gen.table_edit.form_module_name') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.moduleName" clearable />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="businessName">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.code_gen.table_edit.form_business_name') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.businessName" clearable />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="functionName">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.code_gen.table_edit.form_function_name') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.functionName" clearable />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="author">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.code_gen.table_edit.form_author') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.author" clearable />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="templateType">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.code_gen.table_edit.form_template_type') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect v-model:value="form.templateType" :options="TABLE_TEMPLATE_TYPE_OPTIONS" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="genType">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.code_gen.table_edit.form_gen_type') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect v-model:value="form.genType" :options="GEN_TYPE_OPTIONS" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="generationScope">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.code_gen.table_edit.form_generation_scope') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect v-model:value="form.generationScope" :options="GENERATION_SCOPE_OPTIONS" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="enabledActions">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.code_gen.table_edit.form_enabled_actions') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect
                v-model:value="form.enabledActions"
                multiple
                :max-tag-count="3"
                :options="ENABLED_ACTION_OPTIONS"
                :placeholder="t('develop.code_gen.table_edit.form_enabled_actions_placeholder')"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="databaseType">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.code_gen.table_edit.form_database_type') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect v-model:value="form.databaseType" :options="DATABASE_TYPE_OPTIONS" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="genPath">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.code_gen.table_edit.form_gen_path') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.genPath" clearable :placeholder="t('develop.code_gen.table_edit.form_gen_path_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="primaryKeyColumn">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.code_gen.table_edit.form_primary_key_column') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect
                v-model:value="form.primaryKeyColumn"
                clearable
                filterable
                :options="columnOptions"
                :placeholder="t('develop.code_gen.table_edit.form_column_placeholder')"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <template v-if="isTreeTemplate">
          <XhFormFieldGroup value="treeParentColumn">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('develop.code_gen.table_edit.form_tree_parent_column') }}</XhFieldLabel>
              <XhFieldControl>
                <XSelect
                  v-model:value="form.treeParentColumn"
                  clearable
                  filterable
                  :options="columnOptions"
                  :placeholder="t('develop.code_gen.table_edit.form_column_placeholder')"
                />
              </XhFieldControl>
              <XhFieldErrorText />
            </XhFieldRoot>
          </XhFormFieldGroup>
          <XhFormFieldGroup value="treeNameColumn">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('develop.code_gen.table_edit.form_tree_name_column') }}</XhFieldLabel>
              <XhFieldControl>
                <XSelect
                  v-model:value="form.treeNameColumn"
                  clearable
                  filterable
                  :options="columnOptions"
                  :placeholder="t('develop.code_gen.table_edit.form_column_placeholder')"
                />
              </XhFieldControl>
              <XhFieldErrorText />
            </XhFieldRoot>
          </XhFormFieldGroup>
        </template>
        <template v-if="isMasterDetailTemplate">
          <XhFormFieldGroup value="masterTableId">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('develop.code_gen.table_edit.form_master_table') }}</XhFieldLabel>
              <XhFieldControl>
                <XSelect
                  v-model:value="form.masterTableId"
                  clearable
                  filterable
                  :options="tableOptions"
                  :placeholder="t('develop.code_gen.table_edit.form_master_table_placeholder')"
                />
              </XhFieldControl>
              <XhFieldErrorText />
            </XhFieldRoot>
          </XhFormFieldGroup>
          <XhFormFieldGroup value="masterForeignKey">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('develop.code_gen.table_edit.form_master_foreign_key') }}</XhFieldLabel>
              <XhFieldControl>
                <XSelect
                  v-model:value="form.masterForeignKey"
                  clearable
                  filterable
                  :options="columnOptions"
                  :placeholder="t('develop.code_gen.table_edit.form_column_placeholder')"
                />
              </XhFieldControl>
              <XhFieldErrorText />
            </XhFieldRoot>
          </XhFormFieldGroup>
        </template>
        <XhFormFieldGroup value="status">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('common.fields.status') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect v-model:value="form.status" :options="statusEnumOptions" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="tableComment" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.code_gen.table_edit.form_table_comment') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.tableComment" clearable :rows="2" type="textarea" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
      </XhFormRoot>
    </div>
  </XEditModal>
</template>
