<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<!--
  打印模板的元数据表单：名称、编码（创建后不可修改）、数据源、新建时的初始状态、
  全局模板是否允许租户使用、排序与备注。放在列表页的新增、编辑弹窗里
-->
<script setup lang="ts">
import type { PrintTemplateFormModel } from './models'
import {
  XhFieldControl,
  XhFieldErrorText,
  XhFieldLabel,
  XhFieldRoot,
  XhFormFieldGroup,
  XhFormRoot,
  XhSwitch,
} from '@xihan-ui/vue'
import { useI18n } from 'vue-i18n'
import { XFieldLabel, XInput, XNumberInput } from '~/components'
import PrintTemplateDataSourceSettings from './PrintTemplateDataSourceSettings.vue'

defineOptions({ name: 'PrintTemplateMetadataForm' })

defineProps<{
  editing: boolean
  globalMode: boolean
}>()

const model = defineModel<PrintTemplateFormModel>({ required: true })
const { t } = useI18n()
</script>

<template>
  <XhFormRoot v-model:values="(model as unknown as Record<string, unknown>)" validate-on="blur">
    <XhFormFieldGroup name="templateName">
      <XhFieldRoot required>
        <XhFieldLabel>{{ t('printing.print_templates.template_name') }}</XhFieldLabel>
        <XhFieldControl>
          <XInput
            v-model:value="model.templateName"
            :max-length="100"
            :placeholder="t('printing.print_templates.template_name_placeholder')"
          />
        </XhFieldControl>
        <XhFieldErrorText />
      </XhFieldRoot>
    </XhFormFieldGroup>

    <XhFormFieldGroup name="templateCode">
      <XhFieldRoot required>
        <XFieldLabel :help="editing ? t('printing.print_templates.template_code_immutable') : undefined">
          {{ t('printing.print_templates.template_code') }}
        </XFieldLabel>
        <XhFieldControl>
          <XInput
            v-model:value="model.templateCode"
            :disabled="editing"
            :max-length="100"
            :placeholder="t('printing.print_templates.template_code_placeholder')"
          />
        </XhFieldControl>
        <XhFieldErrorText />
      </XhFieldRoot>
    </XhFormFieldGroup>

    <PrintTemplateDataSourceSettings v-model="model" />

    <!-- 状态只在新建时定；已有模板在列表页启停 -->
    <XhFormFieldGroup v-if="!editing" name="status">
      <XhFieldRoot>
        <XFieldLabel :help="t('printing.print_templates.initial_status_help')">
          {{ t('printing.print_templates.initial_status') }}
        </XFieldLabel>
        <XhFieldControl>
          <XhSwitch
            :checked="model.status === 'Active'"
            @update:checked="model.status = $event ? 'Active' : 'Disabled'"
          />
        </XhFieldControl>
      </XhFieldRoot>
    </XhFormFieldGroup>

    <XhFormFieldGroup v-if="globalMode" name="allowTenantUse">
      <XhFieldRoot>
        <XFieldLabel :help="t('printing.print_templates.allow_tenant_use_help')">
          {{ t('printing.print_templates.allow_tenant_use') }}
        </XFieldLabel>
        <XhFieldControl>
          <XhSwitch v-model:checked="model.allowTenantUse" />
        </XhFieldControl>
      </XhFieldRoot>
    </XhFormFieldGroup>

    <XhFormFieldGroup name="order">
      <XhFieldRoot>
        <XhFieldLabel>{{ t('printing.print_templates.order') }}</XhFieldLabel>
        <XhFieldControl>
          <XNumberInput v-model:value="model.order" :min="0" class="w-full" />
        </XhFieldControl>
        <XhFieldErrorText />
      </XhFieldRoot>
    </XhFormFieldGroup>

    <XhFormFieldGroup name="remark">
      <XhFieldRoot>
        <XhFieldLabel>{{ t('printing.print_templates.remark') }}</XhFieldLabel>
        <XhFieldControl>
          <XInput
            v-model:value="model.remark"
            type="textarea"
            :max-length="500"
            :placeholder="t('printing.print_templates.remark_placeholder')"
            :autosize="{ minRows: 3, maxRows: 6 }"
          />
        </XhFieldControl>
        <XhFieldErrorText />
      </XhFieldRoot>
    </XhFormFieldGroup>
  </XhFormRoot>
</template>
