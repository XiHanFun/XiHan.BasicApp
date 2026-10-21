<!--
  流程定义的新建、编辑弹窗：只改元数据（审批业务与编码、名称、分类、描述），节点与连线在设计器里改。
  新建时建一份空白流程的草稿；编辑时只改引擎定义 JSON 里的名称、分类、描述，编码创建后不可改。
-->
<script setup lang="ts">
import type { WorkflowBusinessTypeDto, WorkflowDefinitionDetailDto } from '../../../api'
import type { WorkflowDefinitionMetadata } from './definition-metadata'
import { XhFieldControl, XhFieldDescription, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFormFieldGroup, XhFormRoot } from '@xihan-ui/vue'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { XEditModal, XInput, XSelect } from '~/components'
import { toast } from '~/composables'
import { workflowDefinitionApi } from '../../../api'
import {
  applyDefinitionMetadata,
  bindDefinitionBusiness,
  createDefinitionJson,
  emptyDefinitionMetadata,
  isDefinitionMetadataComplete,
  readDefinitionMetadata,
} from './definition-metadata'

defineOptions({ name: 'WorkflowDefinitionMetadataModal' })

const props = defineProps<{
  /** 编辑的草稿详情；null 是新建 */
  detail: WorkflowDefinitionDetailDto | null
  businessTypes: readonly WorkflowBusinessTypeDto[]
  show: boolean
}>()

const emit = defineEmits<{
  'saved': [detail: WorkflowDefinitionDetailDto, created: boolean]
  'update:show': [value: boolean]
}>()

const { t } = useI18n()
const form = ref<WorkflowDefinitionMetadata>(emptyDefinitionMetadata())
const formKey = ref(0)
const saving = ref(false)
const editing = computed(() => Boolean(props.detail))
const complete = computed(() => isDefinitionMetadataComplete(form.value))
const title = computed(() => editing.value ? t('workflow.definition.edit_title') : t('workflow.definition.create_title'))
const boundBusiness = computed(() => props.businessTypes.find(item => item.code === form.value.code.trim()) ?? null)
const businessOptions = computed(() => props.businessTypes.map(item => ({ label: `${item.name}（${item.code}）`, value: item.code })))

watch(() => props.show, (show) => {
  if (show)
    reset()
})

/** 每次打开按详情重填一份表单，取消不留痕；详情里的定义读不出来就提示并关掉 */
function reset(): void {
  try {
    form.value = props.detail ? readDefinitionMetadata(props.detail.definitionJson) : emptyDefinitionMetadata()
    formKey.value += 1
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('workflow.designer.err_parse'))
    emit('update:show', false)
  }
}

/** 选审批业务：编码换成业务编码，名称空着时带上业务名 */
function selectBusiness(code: string | number | (string | number)[] | null): void {
  const business = typeof code === 'string' ? props.businessTypes.find(item => item.code === code) : undefined
  if (business)
    form.value = bindDefinitionBusiness(form.value, business)
}

/** 保存进行中不许关；取消、关闭钮与 Esc 都走这里 */
function updateShow(value: boolean): void {
  if (!value && saving.value)
    return
  emit('update:show', value)
}

/** 新建建一份空白流程的草稿，编辑只改元数据；接口成功才关弹窗 */
async function save(): Promise<void> {
  if (saving.value || !complete.value)
    return
  const detail = props.detail
  saving.value = true
  try {
    const saved = detail
      ? await workflowDefinitionApi.updateDraft({ basicId: detail.basicId, definitionJson: applyDefinitionMetadata(detail.definitionJson, form.value) })
      : await workflowDefinitionApi.create({ definitionJson: createDefinitionJson(form.value, props.businessTypes) })
    toast.success(detail ? t('workflow.definition.msg_updated') : t('workflow.definition.msg_created'))
    emit('update:show', false)
    emit('saved', saved, !detail)
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('workflow.definition.err_save'))
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
    <XhFormRoot :key="formKey" v-model:values="(form as unknown as Record<string, unknown>)" validate-on="blur">
      <!-- 流程编码即业务编码：新建时先选要关联的审批业务，也可以不绑定直接填编码 -->
      <XhFormFieldGroup v-if="!editing" name="business">
        <XhFieldRoot>
          <XhFieldLabel>{{ t('workflow.designer.flow_business') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect
              :value="boundBusiness?.code ?? null"
              :options="businessOptions"
              :placeholder="t('workflow.designer.flow_business_placeholder')"
              @update:value="selectBusiness"
            />
          </XhFieldControl>
          <XhFieldDescription v-if="boundBusiness">
            {{ t('workflow.designer.flow_business_tip', { name: boundBusiness.name, outcomes: boundBusiness.outcomes.join(' / ') }) }}
          </XhFieldDescription>
          <XhFieldErrorText />
        </XhFieldRoot>
      </XhFormFieldGroup>

      <XhFormFieldGroup name="code">
        <XhFieldRoot required>
          <XhFieldLabel>{{ t('workflow.definition.code') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput
              v-model:value="form.code"
              :disabled="editing"
              :max-length="100"
              :placeholder="t('workflow.designer.flow_code_placeholder')"
            />
          </XhFieldControl>
          <XhFieldDescription v-if="editing">
            {{ t('workflow.definition.code_immutable') }}
          </XhFieldDescription>
          <XhFieldErrorText />
        </XhFieldRoot>
      </XhFormFieldGroup>

      <XhFormFieldGroup name="name">
        <XhFieldRoot required>
          <XhFieldLabel>{{ t('workflow.definition.name') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.name" :max-length="100" />
          </XhFieldControl>
          <XhFieldErrorText />
        </XhFieldRoot>
      </XhFormFieldGroup>

      <XhFormFieldGroup name="category">
        <XhFieldRoot>
          <XhFieldLabel>{{ t('workflow.definition.category') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.category" :max-length="100" />
          </XhFieldControl>
          <XhFieldErrorText />
        </XhFieldRoot>
      </XhFormFieldGroup>

      <XhFormFieldGroup name="description">
        <XhFieldRoot>
          <XhFieldLabel>{{ t('workflow.definition.description') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput
              v-model:value="form.description"
              type="textarea"
              :max-length="500"
              :autosize="{ minRows: 3, maxRows: 6 }"
            />
          </XhFieldControl>
          <XhFieldErrorText />
        </XhFieldRoot>
      </XhFormFieldGroup>
    </XhFormRoot>
  </XEditModal>
</template>
