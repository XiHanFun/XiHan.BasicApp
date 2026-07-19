<script lang="ts" setup>
import { NButton, NResult } from 'naive-ui'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { goHome } from './use-fallback-actions'

defineOptions({ name: 'NotFoundPage' })

const router = useRouter()
const { t } = useI18n()
</script>

<template>
  <div class="flex-col-center h-full min-h-[400px]">
    <NResult status="404" :title="t('error.not_found')" :description="t('error.not_found_desc')">
      <!-- 间距用外层 flex gap，不要给 NButton 挂 ml-*：
           naive-ui 运行时注入的样式是无层级的，永远压过 Tailwind 的 @layer utilities，
           .n-button 自带的 margin 会把 ml-3 吃掉（三个错误页此前都贴在一起） -->
      <template #footer>
        <div class="flex gap-3 justify-center">
          <NButton type="primary" @click="goHome(router)">
            {{ t('error.back_home') }}
          </NButton>
          <NButton @click="router.back()">
            {{ t('error.back_prev') }}
          </NButton>
        </div>
      </template>
    </NResult>
  </div>
</template>
