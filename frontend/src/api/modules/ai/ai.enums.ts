/**
 * AI 模块前端选项。
 *
 * Provider 后端为自由字符串（对应框架 AiProviderOptions.Provider），此处仅提供一组常见的
 * OpenAI 兼容供应商建议值，配合 NSelect 的 tag+filterable 允许用户自定义录入。
 */

/** AI 提供商建议项（自由字符串，可自定义；均走 OpenAI 兼容协议，配对应 BaseUrl + Model） */
export const AI_PROVIDER_OPTIONS = [
  { label: 'OpenAI', value: 'OpenAI' },
  { label: 'DeepSeek', value: 'DeepSeek' },
  { label: 'Azure OpenAI', value: 'Azure' },
  { label: 'Moonshot', value: 'Moonshot' },
  { label: '自建/vLLM', value: 'SelfHosted' },
  { label: '自定义', value: 'Custom' },
]
