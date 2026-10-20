<!--
  打印模板的元数据表单：名称、编码（创建后不可修改）、数据源、新建时的初始状态、
  全局模板是否允许租户使用、排序与备注。放在列表页的新增、编辑弹窗里。
-->
<script setup lang="ts">
import type { PrintTemplateFormModel } from './models'
import type { PrintDocument } from '~/printing/designer'
import {
  XhFieldControl,
  XhFieldDescription,
  XhFieldErrorText,
  XhFieldLabel,
  XhFieldRoot,
  XhFormFieldGroup,
  XhFormRoot,
  XhSwitch,
} from '@xihan-ui/vue'
import { useI18n } from 'vue-i18n'
import { EnableStatus } from '@/api'
import { XInput, XNumberInput } from '~/components'
import PrintTemplateDataSourceSettings from './PrintTemplateDataSourceSettings.vue'

defineOptions({ name: 'PrintTemplateMetadataForm' })

defineProps<{
  editing: boolean
  globalMode: boolean
  template: PrintDocument
}>()

const model = defineModel<PrintTemplateFormModel>({ required: true })
const { t } = useI18n()
</script>

<template>
  <XhFormRoot v-model:values="(model as unknown as Record<string, unknown>)" validate-on="blur">
    <XhFormFieldGroup name="templateName">
      <XhFieldRoot required>
        <XhFieldLabel>{{ t('setting.print_template.template_name') }}</XhFieldLabel>
        <XhFieldControl>
          <XInput
            v-model:value="model.templateName"
            :max-length="100"
            :placeholder="t('setting.print_template.template_name_placeholder')"
          />
        </XhFieldControl>
        <XhFieldErrorText />
      </XhFieldRoot>
    </XhFormFieldGroup>

    <XhFormFieldGroup name="templateCode">
      <XhFieldRoot required>
        <XhFieldLabel>{{ t('setting.print_template.template_code') }}</XhFieldLabel>
        <XhFieldControl>
          <XInput
            v-model:value="model.templateCode"
            :disabled="editing"
            :max-length="100"
            :placeholder="t('setting.print_template.template_code_placeholder')"
          />
        </XhFieldControl>
        <XhFieldDescription v-if="editing">
          {{ t('setting.print_template.template_code_immutable') }}
        </XhFieldDescription>
        <XhFieldErrorText />
      </XhFieldRoot>
    </XhFormFieldGroup>

    <PrintTemplateDataSourceSettings v-model="model" :template="template" />

    <!-- 状态只在新建时定；已有模板在列表页启停 -->
    <XhFormFieldGroup v-if="!editing" name="status">
      <XhFieldRoot>
        <XhFieldLabel>{{ t('setting.print_template.initial_status') }}</XhFieldLabel>
        <XhFieldControl>
          <XhSwitch
            :checked="model.status === EnableStatus.Enabled"
            @update:checked="model.status = $event ? EnableStatus.Enabled : EnableStatus.Disabled"
          />
        </XhFieldControl>
        <XhFieldDescription>{{ t('setting.print_template.initial_status_help') }}</XhFieldDescription>
      </XhFieldRoot>
    </XhFormFieldGroup>

    <XhFormFieldGroup v-if="globalMode" name="allowTenantUse">
      <XhFieldRoot>
        <XhFieldLabel>{{ t('setting.print_template.allow_tenant_use') }}</XhFieldLabel>
        <XhFieldControl>
          <XhSwitch v-model:checked="model.allowTenantUse" />
        </XhFieldControl>
        <XhFieldDescription>{{ t('setting.print_template.allow_tenant_use_help') }}</XhFieldDescription>
      </XhFieldRoot>
    </XhFormFieldGroup>

    <XhFormFieldGroup name="sort">
      <XhFieldRoot>
        <XhFieldLabel>{{ t('setting.print_template.sort') }}</XhFieldLabel>
        <XhFieldControl>
          <XNumberInput v-model:value="model.sort" :min="0" class="w-full" />
        </XhFieldControl>
        <XhFieldErrorText />
      </XhFieldRoot>
    </XhFormFieldGroup>

    <XhFormFieldGroup name="remark">
      <XhFieldRoot>
        <XhFieldLabel>{{ t('setting.print_template.remark') }}</XhFieldLabel>
        <XhFieldControl>
          <XInput
            v-model:value="model.remark"
            type="textarea"
            :max-length="500"
            :placeholder="t('setting.print_template.remark_placeholder')"
            :autosize="{ minRows: 3, maxRows: 6 }"
          />
        </XhFieldControl>
        <XhFieldErrorText />
      </XhFieldRoot>
    </XhFormFieldGroup>
  </XhFormRoot>
</template>
