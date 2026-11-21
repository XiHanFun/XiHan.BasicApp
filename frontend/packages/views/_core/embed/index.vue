<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script lang="ts" setup>
import { XhSpinner } from '@xihan-ui/vue'
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute } from 'vue-router'

/**
 * 内嵌菜单的页面：内容区铺满一个 iframe，地址是菜单的外部地址（路由 meta.iframeSrc）
 * 每个内嵌菜单各有一个按路由名区分的壳，地址与标题在建立时取一次：页面被缓存后切到别处，iframe 不跟着当前路由变
 * 对方站点禁止被嵌入时浏览器不显示内容，跨源拿不到失败信号，这里不另做判断
 */
defineOptions({ name: 'EmbedPage' })

const { t, te } = useI18n()
const route = useRoute()
const src = route.meta.iframeSrc as string | undefined
const titleKey = route.meta.title as string | undefined
const title = titleKey && te(titleKey)
  ? t(titleKey)
  : titleKey ?? ''
const loading = ref(Boolean(src))
</script>

<template>
  <div class="relative h-full">
    <iframe
      v-if="src"
      :src="src"
      :title="title"
      class="block h-full w-full border-0"
      allow="fullscreen; clipboard-read; clipboard-write"
      referrerpolicy="strict-origin-when-cross-origin"
      @load="loading = false"
    />
    <div v-if="loading" class="absolute inset-0 flex items-center justify-center">
      <XhSpinner :label="t('common.loading')" />
    </div>
  </div>
</template>
