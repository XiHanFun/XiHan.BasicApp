<script setup lang="ts">
import { useData, withBase } from "vitepress";
import { computed } from "vue";

const { page } = useData();

// 构建期每页都落了一份同路径的 .md，这里指向它；开发服务器由 /__markdown/ 按需生成同一份
const href = computed(() => withBase(
  import.meta.env.DEV
    ? `/__markdown/${page.value.relativePath}`
    : `/${page.value.relativePath}`,
));
</script>

<template>
  <div class="xh-page-markdown">
    <a :href="href" target="_blank" rel="noreferrer">取本页 Markdown</a>
  </div>
</template>

<style scoped>
.xh-page-markdown {
  display: flex;
  justify-content: flex-end;
  margin-block-start: 8px;
  font-size: 13px;
}

.xh-page-markdown a {
  color: var(--vp-c-text-2);
  text-decoration: none;
  transition: color 0.15s;
}

.xh-page-markdown a:hover {
  color: var(--vp-c-brand-1);
}
</style>
