<script lang="ts" setup>
import { NButton, NResult } from 'naive-ui'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'

defineOptions({ name: 'ForbiddenPage' })

const router = useRouter()
const { t } = useI18n()
</script>

<template>
  <div class="flex-col-center h-full min-h-[400px]">
    <NResult status="403" :title="t('error.forbidden')" :description="t('error.forbidden_desc')">
      <template #footer>
        <NButton type="primary" @click="router.replace('/')">
          {{ t('error.back_home') }}
        </NButton>
        <NButton class="ml-3" @click="router.back()">
          {{ t('error.back_prev') }}
        </NButton>
      </template>
    </NResult>
  </div>
</template>
