<!--
  代码生成 / 代码生成页（只在平台一侧）
  一页四签：表配置、数据源、模板、生成历史；各页签按自己资源的读权限显示（菜单按表配置的读权限下发）
-->
<script setup lang="ts">
import { XhTabsContent, XhTabsIndicator, XhTabsList, XhTabsRoot, XhTabsTrigger } from '@xihan-ui/vue'
import { computed, ref, watchEffect } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '~/stores'
import DatasourcePanel from './components/DatasourcePanel.vue'
import HistoryPanel from './components/HistoryPanel.vue'
import TablePanel from './components/TablePanel.vue'
import TemplatePanel from './components/TemplatePanel.vue'

defineOptions({ name: 'CodegenerationCodeGenView' })

type CodeGenTab = 'table' | 'datasource' | 'template' | 'history'

const { t } = useI18n()
const auth = useAuthStore()

const tabs = computed(() => ({
  table: auth.can('codegeneration.codegen-tables.read'),
  datasource: auth.can('codegeneration.codegen-datasources.read'),
  template: auth.can('codegeneration.codegen-templates.read'),
  history: auth.can('codegeneration.codegen-histories.read'),
}))

const activeTab = ref<CodeGenTab>('table')

// 当前页签没有读权限时落到第一个看得见的页签
watchEffect(() => {
  if (tabs.value[activeTab.value])
    return
  const first = (Object.keys(tabs.value) as CodeGenTab[]).find(tab => tabs.value[tab])
  if (first)
    activeTab.value = first
})
</script>

<template>
  <div class="code-gen">
    <!-- 面板内容各不相同，标签与面板手摆而不喂 collection -->
    <XhTabsRoot v-model:value="activeTab" variant="line" class="code-gen__tabs">
      <XhTabsList>
        <XhTabsTrigger v-if="tabs.table" value="table">
          {{ t('codegeneration.code_gen.tabs.table') }}
        </XhTabsTrigger>
        <XhTabsTrigger v-if="tabs.datasource" value="datasource">
          {{ t('codegeneration.code_gen.tabs.datasource') }}
        </XhTabsTrigger>
        <XhTabsTrigger v-if="tabs.template" value="template">
          {{ t('codegeneration.code_gen.tabs.template') }}
        </XhTabsTrigger>
        <XhTabsTrigger v-if="tabs.history" value="history">
          {{ t('codegeneration.code_gen.tabs.history') }}
        </XhTabsTrigger>
        <XhTabsIndicator />
      </XhTabsList>
      <XhTabsContent v-if="tabs.table" value="table">
        <TablePanel />
      </XhTabsContent>
      <XhTabsContent v-if="tabs.datasource" value="datasource">
        <DatasourcePanel />
      </XhTabsContent>
      <XhTabsContent v-if="tabs.template" value="template">
        <TemplatePanel />
      </XhTabsContent>
      <XhTabsContent v-if="tabs.history" value="history">
        <HistoryPanel />
      </XhTabsContent>
    </XhTabsRoot>
  </div>
</template>

<style scoped>
.code-gen {
  display: flex;
  flex-direction: column;
  height: 100%;
  padding: var(--xh-space-3);
  box-sizing: border-box;
  overflow: hidden;
}

.code-gen__tabs {
  flex: 1;
  min-height: 0;
}

.code-gen__tabs :deep([data-scope='tabs'][data-part='content']) {
  flex: 1;
  min-height: 0;
  padding-top: var(--xh-space-2);
  box-sizing: border-box;
}
</style>
