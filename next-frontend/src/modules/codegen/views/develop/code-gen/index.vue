<script setup lang="ts">
import { XhTabsContent, XhTabsList, XhTabsRoot, XhTabsTrigger } from '@xihan-ui/vue'
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import DatasourcePanel from './components/datasource-panel.vue'
import HistoryPanel from './components/history-panel.vue'
import TablePanel from './components/table-panel.vue'
import TemplatePanel from './components/template-panel.vue'

defineOptions({ name: 'DevelopCodeGenPage' })

const { t } = useI18n()

const activeTab = ref<'table' | 'datasource' | 'template' | 'history'>('table')
</script>

<template>
  <div class="code-gen">
    <!-- 面板内容各不相同，标签与面板手摆而不喂 collection -->
    <XhTabsRoot v-model:value="activeTab" variant="line" class="code-gen__tabs">
      <XhTabsList>
        <XhTabsTrigger value="table">
          {{ t('develop.code_gen.tabs.table') }}
        </XhTabsTrigger>
        <XhTabsTrigger value="datasource">
          {{ t('develop.code_gen.tabs.datasource') }}
        </XhTabsTrigger>
        <XhTabsTrigger value="template">
          {{ t('develop.code_gen.tabs.template') }}
        </XhTabsTrigger>
        <XhTabsTrigger value="history">
          {{ t('develop.code_gen.tabs.history') }}
        </XhTabsTrigger>
      </XhTabsList>
      <XhTabsContent value="table">
        <TablePanel />
      </XhTabsContent>
      <XhTabsContent value="datasource">
        <DatasourcePanel />
      </XhTabsContent>
      <XhTabsContent value="template">
        <TemplatePanel />
      </XhTabsContent>
      <XhTabsContent value="history">
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
  padding: 12px;
  box-sizing: border-box;
  overflow: hidden;
}

.code-gen__tabs {
  flex: 1;
  min-height: 0;
}

.code-gen__tabs :deep(.n-tabs-pane-wrapper) {
  flex: 1;
  min-height: 0;
}

.code-gen__tabs :deep(.n-tab-pane) {
  height: 100%;
  padding-top: 8px;
  box-sizing: border-box;
}
</style>
