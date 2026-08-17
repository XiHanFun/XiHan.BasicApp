import type { Tone } from '@xihan-ui/kernel'
import type { App } from 'vue'
import {
  XhButton,
  XhDialogContent,
  XhDialogDescription,
  XhDialogRoot,
  XhDialogTitle,
  XhTextFieldInput,
  XhTextFieldRoot,
} from '@xihan-ui/vue'
import { createApp, defineComponent, h, reactive } from 'vue'

/**
 * 命令式取值弹窗：在弹窗里问一到几个值，确认后连值一起交回。
 *
 * 组件库的命令式对话框只收 string 标题与正文，塞不进输入框；而「改用户名要连当前密码一起填」
 * 这类场景全站有好几处。这里照库里 dialog-service 的路子自挂一个宿主应用，
 * 内容换成若干个文本字段。
 *
 * 与 confirm 一致的两点：onOk 返回 Promise 时确认钮进入 pending 并拦住关闭，
 * 拒绝或返回 false 则保持打开以便改了重试。
 */

export interface PromptField {
  /** 取值的键，onOk 收到的对象按此索引 */
  key: string
  label?: string
  placeholder?: string
  type?: 'text' | 'password'
  /** 打开时的初值 */
  value?: string
}

export interface PromptOptions {
  title: string
  /** 标题下的一句说明 */
  description?: string
  fields: PromptField[]
  okText?: string
  cancelText?: string
  /** 确认钮语气；不可逆操作传 danger */
  tone?: Tone
  /**
   * 确认回调。返回 false 表示校验未通过，弹窗保持打开；
   * 抛错同理，另由调用方自行提示。
   */
  onOk?: (values: Record<string, string>) => boolean | void | Promise<boolean | void>
}

interface PromptState {
  open: boolean
  busy: boolean
  spec: PromptOptions | null
  values: Record<string, string>
  resolve: ((ok: boolean) => void) | null
}

let mounted: { app: App, holder: HTMLElement, state: PromptState } | null = null

function ensureHost(): { state: PromptState } {
  if (mounted)
    return mounted

  const state = reactive<PromptState>({ open: false, busy: false, spec: null, values: {}, resolve: null })

  function settle(ok: boolean): void {
    state.resolve?.(ok)
    state.resolve = null
  }

  function close(ok: boolean): void {
    settle(ok)
    state.open = false
  }

  async function confirm(): Promise<void> {
    if (state.busy || !state.spec)
      return
    if (!state.spec.onOk) {
      close(true)
      return
    }
    state.busy = true
    try {
      const passed = await state.spec.onOk({ ...state.values })
      state.busy = false
      // 显式返回 false = 校验未通过，留在原地让用户改
      if (passed === false)
        return
    }
    catch {
      state.busy = false
      return
    }
    close(true)
  }

  const Host = defineComponent({
    name: 'XPromptServiceHost',
    setup() {
      return () => {
        const spec = state.spec
        return h(XhDialogRoot, {
          'open': state.open,
          'modal': true,
          'closeOnEscape': !state.busy,
          'closeOnInteractOutside': false,
          'onUpdate:open': (open: boolean) => {
            if (!open)
              close(false)
          },
        }, () => (spec
          ? h(XhDialogContent, { class: 'x-prompt' }, () => [
              h(XhDialogTitle, () => spec.title),
              spec.description ? h(XhDialogDescription, () => spec.description) : null,
              h('div', { class: 'x-prompt__fields' }, spec.fields.map(field =>
                h(XhTextFieldRoot, {
                  'key': field.key,
                  'value': state.values[field.key] ?? '',
                  'placeholder': field.placeholder,
                  'onUpdate:value': (next: string) => {
                    state.values[field.key] = next
                  },
                }, () => h(XhTextFieldInput, { type: field.type ?? 'text' })))),
              h('div', { class: 'x-prompt__actions' }, [
                h(XhButton, {
                  variant: 'outline',
                  disabled: state.busy,
                  onClick: () => close(false),
                }, () => spec.cancelText ?? '取消'),
                h(XhButton, {
                  variant: 'solid',
                  tone: spec.tone ?? 'brand',
                  loading: state.busy,
                  onClick: confirm,
                }, () => spec.okText ?? '确定'),
              ]),
            ])
          : null))
      }
    },
  })

  const holder = document.createElement('div')
  document.body.appendChild(holder)
  const app = createApp(Host)
  app.mount(holder)
  mounted = { app, holder, state }
  return mounted
}

/** 打开取值弹窗；确认走完 onOk 才 resolve(true)，取消/Esc resolve(false) */
export function prompt(options: PromptOptions): Promise<boolean> {
  const { state } = ensureHost()
  state.spec = options
  state.values = Object.fromEntries(options.fields.map(f => [f.key, f.value ?? '']))
  state.busy = false
  state.open = true
  return new Promise<boolean>((resolve) => {
    state.resolve = resolve
  })
}

/** 卸载宿主（热更新与测试用） */
export function disposePromptService(): void {
  if (!mounted)
    return
  mounted.app.unmount()
  mounted.holder.remove()
  mounted = null
}
