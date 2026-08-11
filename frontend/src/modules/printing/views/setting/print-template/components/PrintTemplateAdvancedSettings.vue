<!--
  打印模板高级设置页。
  职责：编辑引擎只读信息、初始发布范围、排序和备注等低频元数据。
-->
<script setup lang="ts">
import type { PrintTemplateFormModel } from './models'
import { NForm, NFormItem, NInput, NInputNumber, NSwitch } from 'naive-ui'
import { useI18n } from 'vue-i18n'
import { EnableStatus } from '@/api'

defineOptions({ name: 'PrintTemplateAdvancedSettings' })

defineProps<{
  editing: boolean
  globalMode: boolean
}>()

const model = defineModel<PrintTemplateFormModel>({ required: true })
const { t } = useI18n()
</script>

<template>
  <section class="advanced-settings-panel" aria-labelledby="print-template-advanced-tab">
    <div class="advanced-intro">
      <strong>{{ t('setting.print_template.advanced_settings_title') }}</strong>
      <span>{{ t('setting.print_template.advanced_settings_help') }}</span>
    </div>

    <NForm :model="model" label-placement="top">
      <NFormItem :label="t('setting.print_template.engine_version')">
        <NInput v-model:value="model.engineVersion" maxlength="32" disabled />
      </NFormItem>

      <div class="switch-settings">
        <div v-if="!editing" class="switch-setting-row">
          <div>
            <strong>{{ t('setting.print_template.initial_status') }}</strong>
            <span>{{ t('setting.print_template.initial_status_help') }}</span>
          </div>
          <NSwitch
            :value="model.status === EnableStatus.Enabled"
            @update:value="model.status = $event ? EnableStatus.Enabled : EnableStatus.Disabled"
          />
        </div>

        <div v-if="globalMode" class="switch-setting-row">
          <div>
            <strong>{{ t('setting.print_template.allow_tenant_use') }}</strong>
            <span>{{ t('setting.print_template.allow_tenant_use_help') }}</span>
          </div>
          <NSwitch v-model:value="model.allowTenantUse" />
        </div>
      </div>

      <NFormItem :label="t('setting.print_template.sort')">
        <NInputNumber v-model:value="model.sort" :min="0" class="w-full" />
      </NFormItem>

      <NFormItem :label="t('setting.print_template.remark')">
        <NInput
          v-model:value="model.remark"
          type="textarea"
          maxlength="500"
          :placeholder="t('setting.print_template.remark_placeholder')"
          :autosize="{ minRows: 4, maxRows: 8 }"
        />
      </NFormItem>
    </NForm>
  </section>
</template>

<style scoped>
.advanced-settings-panel {
  padding: 20px 22px 28px;
}

.advanced-intro {
  display: flex;
  flex-direction: column;
  gap: 4px;
  margin-bottom: 20px;
  padding: 12px 14px;
  color: #64748b;
  border: 1px solid rgba(148, 163, 184, 0.22);
  border-radius: 8px;
  background: rgba(248, 250, 252, 0.86);
}

.advanced-intro strong {
  color: #334155;
  font-size: 13px;
}

.advanced-intro span,
.switch-setting-row span {
  font-size: 12px;
  line-height: 1.6;
}

.switch-settings {
  display: flex;
  flex-direction: column;
  gap: 10px;
  margin-bottom: 18px;
}

.switch-setting-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 20px;
  padding: 12px 14px;
  color: #64748b;
  border: 1px solid rgba(148, 163, 184, 0.22);
  border-radius: 8px;
}

.switch-setting-row > div {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.switch-setting-row strong {
  color: #334155;
  font-size: 13px;
}

.dark .advanced-intro,
.dark .switch-setting-row {
  background: rgba(15, 23, 42, 0.28);
}

.dark .advanced-intro strong,
.dark .switch-setting-row strong {
  color: #e2e8f0;
}
</style>
