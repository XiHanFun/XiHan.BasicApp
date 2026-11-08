<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<!--
  流程定义的新建、编辑弹窗：只改元数据（审批业务与编码、名称、分类、描述），节点与连线在设计器里改
  新建时建一份空白流程的草稿；编辑时只改定义文档（WorkflowJson）里的名称、分类、描述，编码（定义名）创建后不可改
-->
<script setup lang="ts">
import type { WorkflowBusinessType, WorkflowDefinitionDetail, WorkflowDefinitionMetadata } from './definition-metadata'
import { XhFieldControl, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFormFieldGroup, XhFormRoot } from '@xihan-ui/vue'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { XEditModal, XFieldLabel, XInput, XSelect } from '~/components'
import { toast } from '~/composables'
import { workflowDefinitions } from '~/contracts'
import {
  applyDefinitionMetadata,
  bindDefinitionBusiness,
  createDefinitionJson,
  emptyDefinitionMetadata,
  isDefinitionMetadataComplete,
  isWorkflowName,
  readDefinitionMetadata,
} from './definition-metadata'

defineOptions({ name: 'WorkflowDefinitionMetadataModal' })

const props = defineProps<{
  /** 编辑的草稿详情；null 是新建 */
  detail: WorkflowDefinitionDetail | null
  businessTypes: readonly WorkflowBusinessType[]
  show: boolean
}>()

const emit = defineEmits<{
  'saved': [detail: WorkflowDefinitionDetail, created: boolean]
  'update:show': [value: boolean]
}>()

const { t } = useI18n()
const form = ref<WorkflowDefinitionMetadata>(emptyDefinitionMetadata())
const formKey = ref(0)
const saving = ref(false)
const editing = computed(() => Boolean(props.detail))
const complete = computed(() => isDefinitionMetadataComplete(form.value))
/** 新建时编码填了但不合定义名语法 */
const codeInvalid = computed(() => !editing.value && form.value.code.trim() !== '' && !isWorkflowName(form.value.code))
const title = computed(() => editing.value
  ? t('workflow.definitions.edit_title')
  : t('workflow.definitions.create_title'))
const boundBusiness = computed(() => props.businessTypes.find(item => item.code === form.value.code.trim()) ?? null)
const businessOptions = computed(() => props.businessTypes.map(item => ({ label: `${t(item.nameKey)}（${item.code}）`, value: item.code })))

watch(() => props.show, (show) => {
  if (show)
    reset()
})

/** 每次打开按详情重填一份表单，取消不留痕；详情里的定义读不出来就提示并关掉 */
function reset(): void {
  try {
    form.value = props.detail
      ? readDefinitionMetadata(props.detail.document)
      : emptyDefinitionMetadata()
    formKey.value += 1
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('workflow.designer.err_parse'))
    emit('update:show', false)
  }
}

/** 选审批业务：编码换成业务编码，名称空着时带上业务名 */
function selectBusiness(code: string | number | (string | number)[] | null): void {
  const business = typeof code === 'string'
    ? props.businessTypes.find(item => item.code === code)
    : undefined
  if (business)
    form.value = bindDefinitionBusiness(form.value, business, t(business.nameKey))
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
      ? await workflowDefinitions.update({
          id: detail.id,
          document: applyDefinitionMetadata(detail.document, form.value),
        })
      : await workflowDefinitions.create({
          document: createDefinitionJson(form.value, props.businessTypes, key => t(key)),
        })
    toast.success(detail
      ? t('workflow.definitions.msg_updated')
      : t('workflow.definitions.msg_created'))
    emit('update:show', false)
    emit('saved', saved, !detail)
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('workflow.definitions.err_save'))
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
          <XFieldLabel :help="boundBusiness ? t('workflow.designer.flow_business_tip', { name: t(boundBusiness.nameKey), outcomes: boundBusiness.outcomes.join(' / ') }) : undefined">
            {{ t('workflow.designer.flow_business') }}
          </XFieldLabel>
          <XhFieldControl>
            <XSelect
              :value="boundBusiness?.code ?? null"
              :options="businessOptions"
              :placeholder="t('workflow.designer.flow_business_placeholder')"
              @update:value="selectBusiness"
            />
          </XhFieldControl>
          <XhFieldErrorText />
        </XhFieldRoot>
      </XhFormFieldGroup>

      <XhFormFieldGroup name="code">
        <!-- 只在语法不对时写 invalid，其余交给表单（写了 false 会盖掉表单的必填判定） -->
        <XhFieldRoot required :invalid="codeInvalid || undefined">
          <XFieldLabel :help="editing ? t('workflow.definitions.code_immutable') : undefined">
            {{ t('workflow.definitions.code') }}
          </XFieldLabel>
          <XhFieldControl>
            <XInput
              v-model:value="form.code"
              :disabled="editing"
              :max-length="128"
              :placeholder="t('workflow.designer.flow_code_placeholder')"
            />
          </XhFieldControl>
          <XhFieldErrorText v-if="codeInvalid">
            {{ t('workflow.definitions.code_invalid') }}
          </XhFieldErrorText>
          <XhFieldErrorText v-else />
        </XhFieldRoot>
      </XhFormFieldGroup>

      <XhFormFieldGroup name="name">
        <XhFieldRoot required>
          <XhFieldLabel>{{ t('workflow.definitions.name') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.name" :max-length="100" />
          </XhFieldControl>
          <XhFieldErrorText />
        </XhFieldRoot>
      </XhFormFieldGroup>

      <XhFormFieldGroup name="category">
        <XhFieldRoot>
          <XhFieldLabel>{{ t('workflow.definitions.category') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.category" :max-length="100" />
          </XhFieldControl>
          <XhFieldErrorText />
        </XhFieldRoot>
      </XhFormFieldGroup>

      <XhFormFieldGroup name="description">
        <XhFieldRoot>
          <XhFieldLabel>{{ t('workflow.definitions.description') }}</XhFieldLabel>
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
