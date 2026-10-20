<!--
  打印模板的新增、编辑弹窗：只改元数据（名称、编码、数据源、状态、开放给租户、排序、备注），版面在设计器里改。
  新增时建一份空白版面的模板；编辑时沿用详情里的版面与行版本。
-->
<script setup lang="ts">
import type { PrintTemplateDetailDto, PrintTemplateScope } from '../../../../api/print-template.types'
import type { PrintTemplateFormModel } from './models'
import type { PrintDocument } from '~/printing/designer'
import { computed, ref, shallowRef, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { XEditModal } from '~/components'
import { toast } from '~/composables'
import { parsePrintTemplateJson, PrintTemplateFormatError } from '~/printing'
import { createEmptyPrintDocument } from '~/printing/designer'
import { printTemplateApi } from '../../../../api/print-template'
import {
  createDefaultPrintTemplateForm,
  isPrintTemplateFormComplete,
  toPrintTemplateCreateInput,
  toPrintTemplateForm,
  toPrintTemplateUpdateInput,
} from './print-template-form'
import MetadataForm from './PrintTemplateMetadataForm.vue'

defineOptions({ name: 'PrintTemplateMetadataModal' })

const props = defineProps<{
  /** 编辑的模板详情；null 是新增 */
  detail: PrintTemplateDetailDto | null
  globalMode: boolean
  scope: PrintTemplateScope
  show: boolean
}>()

const emit = defineEmits<{
  'saved': [detail: PrintTemplateDetailDto, created: boolean]
  'update:show': [value: boolean]
}>()

const { t } = useI18n()
const form = ref<PrintTemplateFormModel>(createDefaultPrintTemplateForm())
/** 模板的版面：数据源一栏拿它比对字段，提示数据源里没登记的字段 */
const template = shallowRef<PrintDocument>(createEmptyPrintDocument())
const formKey = ref(0)
const saving = ref(false)
const complete = computed(() => isPrintTemplateFormComplete(form.value))
const title = computed(() => props.detail
  ? t('setting.print_template.edit_title', { code: props.detail.templateCode })
  : t('setting.print_template.add_title'))

watch(() => props.show, (show) => {
  if (show)
    reset()
})

/** 每次打开按详情重填一份表单，取消不留痕。 */
function reset(): void {
  form.value = props.detail ? toPrintTemplateForm(props.detail) : createDefaultPrintTemplateForm()
  template.value = props.detail ? readTemplate(props.detail) : createEmptyPrintDocument()
  formKey.value += 1
}

/** 读出模板的版面；旧格式读不出来时当空白版面，只是不提示字段差异，版面本身不动。 */
function readTemplate(detail: PrintTemplateDetailDto): PrintDocument {
  try {
    return parsePrintTemplateJson(detail.templateJson, detail.templateCode)
  }
  catch (error) {
    if (error instanceof PrintTemplateFormatError)
      return createEmptyPrintDocument()
    throw error
  }
}

/** 保存进行中不许关；取消、关闭钮与 Esc 都走这里。 */
function updateShow(value: boolean): void {
  if (!value && saving.value)
    return
  emit('update:show', value)
}

/** 新增建一份空白版面的模板，编辑沿用详情里的版面与行版本；接口成功才关弹窗。 */
async function save(): Promise<void> {
  if (saving.value || !complete.value)
    return
  const context = { scope: props.scope, globalMode: props.globalMode }
  const created = !props.detail
  saving.value = true
  try {
    const saved = props.detail
      ? await printTemplateApi.update(toPrintTemplateUpdateInput(props.detail, form.value, props.detail.templateJson, context))
      : await printTemplateApi.create(toPrintTemplateCreateInput(form.value, JSON.stringify(createEmptyPrintDocument()), context))
    toast.success(t('setting.print_template.save_success'))
    emit('update:show', false)
    emit('saved', saved, created)
  }
  catch (error) {
    toast.danger((error as Error).message || t('setting.print_template.save_failed'))
  }
  finally {
    saving.value = false
  }
}
</script>

<template>
  <XEditModal
    :show="show"
    :title="title"
    :loading="saving"
    :save-disabled="!complete"
    @update:show="updateShow"
    @save="save"
  >
    <MetadataForm
      :key="formKey"
      v-model="form"
      :editing="Boolean(detail)"
      :global-mode="globalMode"
      :template="template"
    />
  </XEditModal>
</template>
