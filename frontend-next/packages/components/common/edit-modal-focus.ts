/**
 * 编辑弹窗缺省的初始焦点：content 里第一个真正的控件
 *
 * 与组件库的缺省落点同一口径（越过关闭钮、拖动与改尺把手，不取链接），另越过字段名后面的问号提示（data-help-tip）：
 * 首个字段带说明时问号排在控件前面，焦点落在它上面既没法直接输入，提示也会跟着弹出来
 * 选中的节点接不住焦点（还没显形）时，组件库会退回它自己的缺省顺序
 */
export const FIRST_CONTROL_SELECTOR = [
  ':is(button, input, select, textarea, [tabindex], [contenteditable="true"])',
  ':not([disabled], [type="hidden"], [tabindex="-1"], [data-help-tip],',
  ' [data-scope="dialog"]:is([data-part="drag-trigger"], [data-part="resize-trigger"], [data-part="close-trigger"]))',
].join('')
