import type { Tone } from '@xihan-ui/kernel'
import type { VNodeChild } from 'vue'
import {
  XhDialogDescription,
  XhPasswordInputCapsLockIndicator,
  XhPasswordInputControl,
  XhPasswordInputInput,
  XhPasswordInputRoot,
  XhPasswordInputVisibilityTrigger,
  XhTextFieldInput,
  XhTextFieldRoot,
} from '@xihan-ui/vue'
import { h } from 'vue'
import { dialogService } from './ui-service'

/**
 * 命令式取值弹窗：在弹窗里问一到几个值，确认后连值一起交回。
 *
 * 正文是若干个文本字段，其余（排队、忙态、Esc、初始落焦、语气）都由组件库的对话框服务承担，
 * 与 confirm 共用同一个宿主，两者不会叠在一起。
 */

export interface PromptField {
  /** 取值的键，onOk 收到的对象按此索引 */
  key: string
  label?: string
  placeholder?: string
  /** password 档带明暗切换与大写锁定提示 */
  type?: 'text' | 'password'
  /** 打开时的初值 */
  value?: string
  /** 只读字段：给出结果供选中复制 */
  readOnly?: boolean
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
  /** 标题旁的类型徽记 */
  badge?: 'info' | 'success' | 'warning' | 'error'
  /**
   * 确认回调。返回 false 表示校验未通过，弹窗保持打开；
   * 抛错同理，另由调用方自行提示。
   */
  onOk?: (values: Record<string, string>) => boolean | void | Promise<boolean | void>
}

function renderField(field: PromptField, value: Record<string, string>): VNodeChild {
  const ariaLabel = field.label ?? field.placeholder ?? field.key
  const onUpdate = (next: string): void => {
    value[field.key] = next
  }

  if (field.type === 'password') {
    return h(XhPasswordInputRoot, {
      'key': field.key,
      'value': value[field.key] ?? '',
      'placeholder': field.placeholder,
      'readOnly': field.readOnly,
      'onUpdate:value': onUpdate,
    }, () => [
      h(XhPasswordInputControl, null, () => [
        h(XhPasswordInputInput, { 'aria-label': ariaLabel }),
        h(XhPasswordInputVisibilityTrigger),
      ]),
      h(XhPasswordInputCapsLockIndicator),
    ])
  }

  return h(XhTextFieldRoot, {
    'key': field.key,
    'value': value[field.key] ?? '',
    'placeholder': field.placeholder,
    'readOnly': field.readOnly,
    'onUpdate:value': onUpdate,
  }, () => h(XhTextFieldInput, { 'type': 'text', 'aria-label': ariaLabel }))
}

function renderBody(options: PromptOptions, value: Record<string, string>): VNodeChild {
  return [
    options.description ? h(XhDialogDescription, () => options.description) : null,
    h('div', { class: 'x-prompt__fields' }, options.fields.map(field => renderField(field, value))),
  ]
}

/** 打开取值弹窗；确认走完 onOk 才 resolve 那份值，取消/Esc resolve null。 */
export function prompt(options: PromptOptions): Promise<Record<string, string> | null> {
  return dialogService().prompt<Record<string, string>>({
    title: options.title,
    tone: options.tone,
    badge: options.badge,
    okText: options.okText,
    cancelText: options.cancelText,
    initialValue: Object.fromEntries(options.fields.map(f => [f.key, f.value ?? ''])),
    initialFocus: '.x-prompt__fields input',
    body: value => renderBody(options, value),
    onOk: options.onOk ? value => options.onOk!({ ...value }) : undefined,
  })
}
