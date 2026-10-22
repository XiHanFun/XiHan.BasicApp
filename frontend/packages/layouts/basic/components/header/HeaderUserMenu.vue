<script setup lang="ts">
import type { AppDropdownOption } from '~/types'
import {
  XhMenuContent,
  XhMenuGroup,
  XhMenuGroupLabel,
  XhMenuItem,
  XhMenuItemIcon,
  XhMenuItemText,
  XhMenuPositioner,
  XhMenuRoot,
  XhMenuSeparator,
  XhMenuTrigger,
} from '@xihan-ui/vue'
import { computed } from 'vue'
import { XUserAvatar } from '~/components'
import { VNodeRender } from '~/components/common/VNodeRender'
import XihanIconButton from '../XihanIconButton.vue'

/**
 * 顶栏的账号菜单：触发钮只有头像，悬停（或点击、键盘）展开；浮层第一行是头像与名字，分隔线下是账号操作。
 */
defineOptions({ name: 'HeaderUserMenu' })

const props = defineProps<{
  avatar?: null | string
  nickname?: null | string
  username?: null | string
  options: AppDropdownOption[]
}>()

const emit = defineEmits<{ select: [key: string] }>()

const displayName = computed(() => props.nickname || props.username || '')
/** 有昵称时第二行补账号名，与昵称相同就不重复 */
const accountName = computed(() => props.nickname && props.username && props.username !== props.nickname ? props.username : '')
</script>

<template>
  <XhMenuRoot
    open-on-hover
    placement="bottom-end"
    @item-select="(details: { value: string }) => emit('select', details.value)"
  >
    <XhMenuTrigger as-child>
      <XihanIconButton class="ml-1" :aria-label="displayName">
        <XUserAvatar :size="24" :avatar="props.avatar" :name="displayName" />
      </XihanIconButton>
    </XhMenuTrigger>
    <XhMenuPositioner>
      <XhMenuContent>
        <XhMenuGroup value="account">
          <XhMenuGroupLabel>
            <span class="flex min-w-0 items-center gap-2">
              <!-- 头像只是装饰：名字已在旁边，免得分组的可及名把头像里的缩写再念一遍 -->
              <XUserAvatar :size="32" :avatar="props.avatar" :name="displayName" aria-hidden="true" />
              <span class="flex min-w-0 flex-col">
                <span class="truncate text-sm font-medium text-foreground">{{ displayName }}</span>
                <span v-if="accountName" class="truncate text-xs text-muted-foreground">{{ accountName }}</span>
              </span>
            </span>
          </XhMenuGroupLabel>
          <XhMenuSeparator />
          <template v-for="option in props.options" :key="option.key">
            <XhMenuSeparator v-if="option.type === 'divider'" />
            <XhMenuItem v-else :value="option.key" :disabled="option.disabled">
              <XhMenuItemIcon v-if="option.icon">
                <VNodeRender :content="option.icon()" />
              </XhMenuItemIcon>
              <XhMenuItemText>
                <VNodeRender v-if="typeof option.label === 'function'" :content="option.label()" />
                <template v-else>
                  {{ option.label }}
                </template>
              </XhMenuItemText>
            </XhMenuItem>
          </template>
        </XhMenuGroup>
      </XhMenuContent>
    </XhMenuPositioner>
  </XhMenuRoot>
</template>
