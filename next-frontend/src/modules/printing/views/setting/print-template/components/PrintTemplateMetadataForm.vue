<!--
  打印模板设置分层表单。
  职责：在基础信息与高级设置之间提供渐进式导航，并按需展开可信编辑安全说明。
-->
<script setup lang="ts">
import type { PrintTemplateFormModel } from './models'
import { XhCollapsibleContent, XhCollapsibleRoot } from '@xihan-ui/vue'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/iconify'
import AdvancedSettings from './PrintTemplateAdvancedSettings.vue'
import BasicSettings from './PrintTemplateBasicSettings.vue'

defineOptions({ name: 'PrintTemplateMetadataForm' })

defineProps<{
  editing: boolean
  globalMode: boolean
  template: Record<string, unknown>
}>()

const model = defineModel<PrintTemplateFormModel>({ required: true })
const { t } = useI18n()
const activeTab = ref<'advanced' | 'basic'>('basic')
const securityExpanded = ref(false)

const completedRequiredCount = computed(() => [
  model.value.templateName,
  model.value.templateCode,
].filter(value => value.trim()).length)

/** 切换设置页；原生按钮保留键盘可访问性并避免额外路由状态。 */
function switchTab(tab: 'advanced' | 'basic'): void {
  activeTab.value = tab
}
</script>

<template>
  <div class="metadata-form">
    <div class="settings-tab-bar" role="tablist" :aria-label="t('setting.print_template.settings_sections')">
      <button
        id="print-template-basic-tab"
        type="button"
        class="settings-tab-button"
        :class="{ active: activeTab === 'basic' }"
        role="tab"
        :aria-selected="activeTab === 'basic'"
        @click="switchTab('basic')"
      >
        {{ t('setting.print_template.basic_settings') }}
      </button>
      <button
        id="print-template-advanced-tab"
        type="button"
        class="settings-tab-button"
        :class="{ active: activeTab === 'advanced' }"
        role="tab"
        :aria-selected="activeTab === 'advanced'"
        @click="switchTab('advanced')"
      >
        {{ t('setting.print_template.advanced_settings') }}
      </button>
      <span class="required-progress" :class="{ complete: completedRequiredCount === 2 }">
        <Icon width="15" height="15" :icon="completedRequiredCount === 2 ? 'tabler:circle-check' : 'tabler:circle-dashed'" />
        {{ t('setting.print_template.required_progress', { completed: completedRequiredCount, total: 2 }) }}
      </span>
    </div>

    <button
      type="button"
      class="security-notice"
      :aria-expanded="securityExpanded"
      @click="securityExpanded = !securityExpanded"
    >
      <Icon width="18" height="18" icon="tabler:shield-lock" />
      <span>{{ t('setting.print_template.security_notice_compact') }}</span>
      <span class="security-chevron" :class="{ expanded: securityExpanded }" style="display: inline-flex; font-size: 17px"><Icon icon="tabler:chevron-right" /></span>
    </button>
    <XhCollapsibleRoot :open="securityExpanded">
      <XhCollapsibleContent>
        <p class="security-detail">
          {{ t('setting.print_template.trusted_editor_warning') }}
        </p>
      </XhCollapsibleContent>
    </XhCollapsibleRoot>

    <BasicSettings
      v-if="activeTab === 'basic'"
      v-model="model"
      :editing="editing"
      :template="template"
      @next="switchTab('advanced')"
    />
    <AdvancedSettings
      v-else
      v-model="model"
      :editing="editing"
      :global-mode="globalMode"
    />
  </div>
</template>

<style scoped>
.metadata-form {
  min-height: 100%;
  background: #fff;
}

.settings-tab-bar {
  position: sticky;
  z-index: 3;
  top: 0;
  display: flex;
  height: 52px;
  align-items: stretch;
  padding: 0 22px;
  border-bottom: 1px solid rgba(148, 163, 184, 0.24);
  background: rgba(255, 255, 255, 0.96);
  backdrop-filter: blur(8px);
}

.settings-tab-button {
  position: relative;
  padding: 0 6px;
  color: #334155;
  font: inherit;
  font-size: 14px;
  cursor: pointer;
  border: 0;
  background: transparent;
}

.settings-tab-button::after {
  position: absolute;
  right: 2px;
  bottom: -1px;
  left: 2px;
  height: 2px;
  content: '';
  transition: background-color 0.2s ease;
  background: transparent;
}

.settings-tab-button + .settings-tab-button {
  margin-left: 22px;
}

.settings-tab-button:hover,
.settings-tab-button.active {
  color: #4f7cf7;
}

.settings-tab-button.active::after {
  background: #4f7cf7;
}

.settings-tab-button:focus-visible,
.security-notice:focus-visible {
  outline: 2px solid rgba(79, 124, 247, 0.5);
  outline-offset: -2px;
}

.required-progress {
  display: inline-flex;
  margin-left: auto;
  align-items: center;
  gap: 5px;
  color: #b45309;
  font-size: 12px;
  white-space: nowrap;
}

.required-progress.complete {
  color: #16a34a;
}

.security-notice {
  display: flex;
  width: calc(100% - 44px);
  min-height: 38px;
  margin: 18px 22px 0;
  padding: 8px 12px;
  align-items: center;
  gap: 8px;
  color: #92400e;
  font: inherit;
  font-size: 12px;
  text-align: left;
  cursor: pointer;
  border: 1px solid rgba(245, 158, 11, 0.3);
  border-radius: 8px;
  background: rgba(245, 158, 11, 0.08);
}

.security-notice span {
  flex: 1;
}

.security-chevron {
  transition: transform 0.2s ease;
}

.security-chevron.expanded {
  transform: rotate(90deg);
}

.security-detail {
  margin: 8px 22px 0;
  padding: 8px 12px;
  color: #64748b;
  font-size: 12px;
  line-height: 1.65;
  border-left: 2px solid rgba(245, 158, 11, 0.5);
}

.dark .metadata-form,
.dark .settings-tab-bar {
  background: rgba(24, 24, 28, 0.96);
}

.dark .settings-tab-button {
  color: #cbd5e1;
}

.dark .settings-tab-button:hover,
.dark .settings-tab-button.active {
  color: #7aa2ff;
}

.dark .security-detail {
  color: #94a3b8;
}

@media (max-width: 520px) {
  .settings-tab-bar {
    padding: 0 12px;
  }

  .settings-tab-button {
    padding: 0 10px;
  }
}
</style>
