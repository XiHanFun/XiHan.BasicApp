<script setup lang="ts">
import { computed } from 'vue'
import { Icon } from '~/iconify'

export interface ChatContextMenuItem {
  key: string
  label: string
  icon?: string
  /** 危险操作（红色，如撤回） */
  danger?: boolean
  /** 顶部加分隔线 */
  divided?: boolean
}

defineOptions({ name: 'ChatContextMenu' })

const props = withDefaults(defineProps<{
  x: number
  y: number
  items: ChatContextMenuItem[]
  /** 快捷表情条（空则不显示；QQ 式与菜单分离成两张卡片） */
  reactions?: string[]
}>(), {
  reactions: () => [],
})

const emit = defineEmits<{
  select: [key: string]
  react: [emoji: string]
}>()

const show = defineModel<boolean>('show', { default: false })

/** 视口边缘收敛（按估算尺寸夹取，避免菜单溢出屏幕） */
const wrapStyle = computed(() => {
  const menuWidth = 168
  const reactionsWidth = props.reactions.length
    ? props.reactions.length * 36 + 20
    : 0
  const estWidth = Math.max(menuWidth, reactionsWidth)
  const estHeight = (props.reactions.length ? 50 : 0) + props.items.length * 34 + 14
  const vw = window.innerWidth
  const vh = window.innerHeight
  return {
    left: `${Math.max(8, Math.min(props.x, vw - estWidth - 12))}px`,
    top: `${Math.max(8, Math.min(props.y, vh - estHeight - 12))}px`,
  }
})

function handleSelect(key: string) {
  show.value = false
  emit('select', key)
}

function handleReact(emoji: string) {
  show.value = false
  emit('react', emoji)
}
</script>

<template>
  <Teleport to="body">
    <div
      v-if="show"
      class="chat-ctx-overlay"
      @click="show = false"
      @contextmenu.prevent="show = false"
    />
    <Transition name="chat-ctx-fade">
      <div v-if="show" class="chat-ctx-wrap" :style="wrapStyle" @contextmenu.prevent>
        <!-- 快捷表情条（独立卡片，QQ 式） -->
        <div v-if="reactions.length" class="chat-ctx-card chat-ctx-reactions">
          <button
            v-for="emoji in reactions"
            :key="emoji"
            type="button"
            class="chat-ctx-react-btn"
            @click="handleReact(emoji)"
          >
            {{ emoji }}
          </button>
        </div>

        <!-- 操作菜单卡片 -->
        <div v-if="items.length" class="chat-ctx-card chat-ctx-menu">
          <template v-for="item in items" :key="item.key">
            <div v-if="item.divided" class="chat-ctx-divider" />
            <button
              type="button"
              class="chat-ctx-item"
              :class="{ 'chat-ctx-item--danger': item.danger }"
              @click="handleSelect(item.key)"
            >
              <Icon v-if="item.icon" :icon="item.icon" width="15" height="15" class="shrink-0" />
              <span>{{ item.label }}</span>
            </button>
          </template>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped>
.chat-ctx-overlay {
  position: fixed;
  inset: 0;
  z-index: 2999;
}

.chat-ctx-wrap {
  position: fixed;
  z-index: 3000;
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-1_5);
  align-items: flex-start;
}

.chat-ctx-card {
  background: var(--xh-bg-surface);
  border: 1px solid var(--xh-border-default);
  border-radius: var(--xh-shape-sheet);
  box-shadow:
    0 8px 30px color-mix(in srgb, var(--xh-fg-default) 10%, transparent),
    0 2px 8px color-mix(in srgb, var(--xh-fg-default) 5%, transparent);
}

.chat-ctx-reactions {
  display: flex;
  gap: var(--xh-space-0_5);
  padding: var(--xh-space-1_5) var(--xh-space-2);
}

.chat-ctx-react-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  padding: 0;
  border: none;
  border-radius: var(--xh-shape-control);
  background: transparent;
  font-size: var(--xh-font-size-lg);
  cursor: pointer;
  transition:
    background var(--xh-motion-duration-micro) var(--xh-motion-ease-enter),
    transform var(--xh-motion-duration-nudge) var(--xh-motion-ease-enter);
}

.chat-ctx-react-btn:hover {
  background: var(--xh-bg-subtle-hover);
  transform: scale(1.15);
}

.chat-ctx-menu {
  min-width: 148px;
  padding: var(--xh-space-1);
}

.chat-ctx-item {
  display: flex;
  gap: var(--xh-space-2);
  align-items: center;
  width: 100%;
  padding: var(--xh-space-1_5) var(--xh-space-7) var(--xh-space-1_5) var(--xh-space-2_5);
  border: none;
  border-radius: var(--xh-shape-control);
  background: transparent;
  color: var(--xh-fg-default);
  font-size: var(--xh-font-size-sm);
  line-height: 1.2;
  text-align: left;
  cursor: pointer;
  transition: background var(--xh-motion-duration-micro) var(--xh-motion-ease-enter);
}

.chat-ctx-item:hover {
  background: var(--xh-bg-subtle-hover);
}

.chat-ctx-item--danger {
  color: var(--xh-fg-danger);
}

.chat-ctx-divider {
  height: 1px;
  margin: 4px 8px;
  background: var(--xh-border-default);
}

.chat-ctx-fade-enter-active {
  transition:
    opacity var(--xh-motion-duration-enter) var(--xh-motion-ease-enter),
    transform var(--xh-motion-duration-move) var(--xh-motion-ease-enter);
}

.chat-ctx-fade-leave-active {
  transition: opacity var(--xh-motion-duration-exit) var(--xh-motion-ease-exit);
}

.chat-ctx-fade-enter-from {
  opacity: 0;
  transform: scale(0.96);
}

.chat-ctx-fade-leave-to {
  opacity: 0;
}
</style>
