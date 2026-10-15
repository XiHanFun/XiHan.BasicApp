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
}

/* 表单挂在设置抽屉里：吸顶标签带取抽屉同一层材质的实色底，滚过的字段不会从底下透出来 */
.settings-tab-bar {
  position: sticky;
  z-index: 3;
  top: 0;
  display: flex;
  height: 52px;
  align-items: stretch;
  padding: 0 22px;
  border-bottom: 1px solid var(--xh-border-subtle);
  background: var(--xh-material-elevated-bg);
}

.settings-tab-button {
  position: relative;
  padding: 0 6px;
  color: var(--xh-fg-default);
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
  transition: background-color var(--xh-motion-duration-enter) var(--xh-motion-ease-enter);
  background: transparent;
}

.settings-tab-button + .settings-tab-button {
  margin-left: 22px;
}

.settings-tab-button:hover,
.settings-tab-button.active {
  color: var(--xh-fg-brand);
}

.settings-tab-button.active::after {
  background: var(--xh-fg-brand);
}

.settings-tab-button:focus-visible,
.security-notice:focus-visible {
  outline: var(--xh-ring-width) solid var(--xh-ring-focus);
  outline-offset: var(--xh-ring-offset);
}

.required-progress {
  display: inline-flex;
  margin-left: auto;
  align-items: center;
  gap: 5px;
  color: var(--xh-fg-warning);
  font-size: 12px;
  white-space: nowrap;
}

.required-progress.complete {
  color: var(--xh-fg-success);
}

.security-notice {
  display: flex;
  width: calc(100% - 44px);
  min-height: 38px;
  margin: 18px 22px 0;
  padding: 8px 12px;
  align-items: center;
  gap: 8px;
  /* 警示条与组件库提示条同一套兑法：淡底 12%、描边 40%、文字往前景色兑到 60% */
  color: color-mix(in oklab, var(--xh-color-warning-600) 60%, var(--xh-fg-default));
  font: inherit;
  font-size: 12px;
  text-align: left;
  cursor: pointer;
  border: 1px solid color-mix(in oklab, var(--xh-color-warning-600) 40%, var(--xh-bg-surface));
  border-radius: 8px;
  background: color-mix(in oklab, var(--xh-color-warning-600) 12%, var(--xh-bg-surface));
}

.security-notice span {
  flex: 1;
}

.security-chevron {
  transition: transform var(--xh-motion-duration-expand) var(--xh-motion-ease-enter);
}

.security-chevron.expanded {
  transform: rotate(90deg);
}

.security-detail {
  margin: 8px 22px 0;
  padding: 8px 12px;
  color: var(--xh-fg-muted);
  font-size: 12px;
  line-height: 1.65;
  border-left: 2px solid color-mix(in oklab, var(--xh-color-warning-600) 40%, var(--xh-bg-surface));
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
