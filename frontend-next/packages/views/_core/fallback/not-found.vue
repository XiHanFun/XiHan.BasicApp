<script lang="ts" setup>
import { XhButton, XhEmptyStateAction, XhEmptyStateDescription, XhEmptyStateIndicator, XhEmptyStateRoot, XhEmptyStateTitle } from '@xihan-ui/vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { Icon } from '~/iconify'
import { goHome } from './use-fallback-actions'

defineOptions({ name: 'NotFoundPage' })

const router = useRouter()
const { t } = useI18n()
</script>

<template>
  <div class="flex-col-center h-full min-h-[400px]">
    <XhEmptyStateRoot status="404" size="lg" live="off">
      <XhEmptyStateIndicator>
        <Icon icon="lucide:file-question" width="48" height="48" />
      </XhEmptyStateIndicator>
      <XhEmptyStateTitle>{{ t('error.not_found') }}</XhEmptyStateTitle>
      <XhEmptyStateDescription>{{ t('error.not_found_desc') }}</XhEmptyStateDescription>
      <XhEmptyStateAction>
        <XhButton variant="solid" tone="brand" @click="goHome(router)">
          {{ t('error.back_home') }}
        </XhButton>
        <XhButton variant="outline" @click="router.back()">
          {{ t('error.back_prev') }}
        </XhButton>
      </XhEmptyStateAction>
    </XhEmptyStateRoot>
  </div>
</template>
