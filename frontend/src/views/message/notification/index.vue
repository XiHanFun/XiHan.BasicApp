<script setup lang="ts">
import { NCard, NEmpty } from 'naive-ui'

defineOptions({ name: 'MessageNotificationPage' })
</script>

<template>
  <div class="placeholder-page">
    <NCard :bordered="false" title="通知管理" class="placeholder-card">
      <div class="placeholder-body">
        <NEmpty description="通知管理功能建设中" />
      </div>
    </NCard>
  </div>
</template>

<style scoped>
.placeholder-page {
  display: flex;
  flex-direction: column;
  gap: 12px;
  height: 100%;
}

.placeholder-card {
  flex: 1;
}

.placeholder-body {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 320px;
}
</style>
