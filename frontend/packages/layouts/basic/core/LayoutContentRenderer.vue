<script lang="ts" setup>
import { useTabbarStore } from '~/stores'

interface LayoutContentRendererProps {
  transitionName: string
}

defineOptions({ name: 'LayoutContentRenderer' })

defineProps<LayoutContentRendererProps>()

const tabbarStore = useTabbarStore()
</script>

<template>
  <RouterView v-slot="{ Component, route: currentRoute }">
    <Transition :name="transitionName" mode="out-in">
      <KeepAlive :include="currentRoute.meta?.keepAlive ? [currentRoute.name as string] : []">
        <component
          :is="Component"
          :key="`${currentRoute.fullPath}_${tabbarStore.getRefreshSeed(currentRoute.fullPath)}`"
        />
      </KeepAlive>
    </Transition>
  </RouterView>
</template>
