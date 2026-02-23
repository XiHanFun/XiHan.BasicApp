<script lang="ts" setup>
import { NButton, NResult } from 'naive-ui'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'

defineOptions({ name: 'ServerErrorPage' })

const router = useRouter()
const { t } = useI18n()
</script>

<template>
  <div class="flex-col-center h-full min-h-[400px]">
    <NResult status="500" :title="t('error.server_error')" :description="t('error.server_error_desc')">
      <template #footer>
        <NButton type="primary" @click="router.replace('/')">
          {{ t('error.back_home') }}
        </NButton>
        <NButton class="ml-3" @click="router.go(0)">
          {{ t('error.refresh') }}
        </NButton>
      </template>
    </NResult>
  </div>
</template>
