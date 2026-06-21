<script setup lang="ts">
import { NTabPane, NTabs } from 'naive-ui'
import { ref } from 'vue'
import DatasourcePanel from './components/datasource-panel.vue'
import HistoryPanel from './components/history-panel.vue'
import TablePanel from './components/table-panel.vue'
import TemplatePanel from './components/template-panel.vue'

defineOptions({ name: 'DevelopCodeGenPage' })

const activeTab = ref<'table' | 'datasource' | 'template' | 'history'>('table')
</script>

<template>
  <div class="code-gen">
    <NTabs v-model:value="activeTab" animated class="code-gen__tabs" type="line">
      <NTabPane name="table" tab="表配置">
        <TablePanel />
      </NTabPane>
      <NTabPane name="datasource" tab="数据源">
        <DatasourcePanel />
      </NTabPane>
      <NTabPane name="template" tab="模板">
        <TemplatePanel />
      </NTabPane>
      <NTabPane name="history" tab="生成历史">
        <HistoryPanel />
      </NTabPane>
    </NTabs>
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
