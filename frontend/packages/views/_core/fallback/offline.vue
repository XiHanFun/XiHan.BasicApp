<script lang="ts" setup>
import { NButton, NResult } from 'naive-ui'
import { onMounted, onUnmounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'

defineOptions({ name: 'OfflinePage' })

const router = useRouter()
const { t } = useI18n()
const isOnline = ref(navigator.onLine)

function handleOnline() {
  isOnline.value = true
}
function handleOffline() {
  isOnline.value = false
}

onMounted(() => {
  window.addEventListener('online', handleOnline)
  window.addEventListener('offline', handleOffline)
})
onUnmounted(() => {
  window.removeEventListener('online', handleOnline)
  window.removeEventListener('offline', handleOffline)
})
</script>

<template>
  <div class="flex-col-center h-full min-h-[400px]">
    <NResult
      status="warning"
      :title="t('error.offline')"
      :description="t('error.offline_desc')"
    >
      <template #footer>
        <NButton v-if="isOnline" type="primary" @click="router.replace('/')">
          {{ t('error.back_home') }}
        </NButton>
        <NButton v-else @click="router.go(0)">
          {{ t('error.retry') }}
        </NButton>
      </template>
    </NResult>
  </div>
</template>
