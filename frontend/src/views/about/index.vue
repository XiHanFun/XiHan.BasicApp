<script setup lang="ts">
// 系统设置 - 关于系统：复用核心关于页（packages/views/_core/about），保持单一实现，避免重复维护
import CoreAboutPage from '~/views/_core/about/index.vue'

defineOptions({ name: 'SettingAboutPage' })
</script>

<template>
  <CoreAboutPage />
</template>
