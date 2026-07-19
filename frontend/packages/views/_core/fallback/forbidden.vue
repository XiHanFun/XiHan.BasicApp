<script lang="ts" setup>
import { NButton, NResult } from 'naive-ui'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { goHome } from './use-fallback-actions'

defineOptions({ name: 'ForbiddenPage' })

const router = useRouter()
const { t } = useI18n()
</script>

<template>
  <div class="flex-col-center h-full min-h-[400px]">
    <NResult status="403" :title="t('error.forbidden')" :description="t('error.forbidden_desc')">
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
