/**
 * 代码生成模块枚举（与后端全局 JsonStringEnumConverter 序列化值一致，成员名即 wire value）。
 *
 * 后端无 NumericEnumConverter 覆盖，故全部枚举走字符串名（如 'Single'/'MySql'）。
 * 与 ../shared 的 EnableStatus 字符串枚举约定一致。
 */

/** 代码生成模板类型 */
export enum TemplateType {
  Single = 'Single',
  Tree = 'Tree',
  MasterDetail = 'MasterDetail',
}

/** 代码生成状态 */
export enum GenStatus {
  NotGenerated = 'NotGenerated',
  Generated = 'Generated',
  Failed = 'Failed',
}

/** 生成代码方式 */
export enum GenType {
  Zip = 'Zip',
  CustomPath = 'CustomPath',
  Preview = 'Preview',
}

/** 模板引擎类型（Razor 已移除：需运行时编译能力、框架不支持） */
export enum TemplateEngine {
  Scriban = 'Scriban',
  T4 = 'T4',
}

/** 数据库类型 */
export enum DatabaseType {
  MySql = 'MySql',
  SqlServer = 'SqlServer',
  PostgreSql = 'PostgreSql',
  Oracle = 'Oracle',
  Sqlite = 'Sqlite',
}

/** 表单显示类型 */
export enum HtmlType {
  Input = 'Input',
  Textarea = 'Textarea',
  Select = 'Select',
  Radio = 'Radio',
  Checkbox = 'Checkbox',
  DatePicker = 'DatePicker',
  DateTimePicker = 'DateTimePicker',
  TimePicker = 'TimePicker',
  ImageUpload = 'ImageUpload',
  FileUpload = 'FileUpload',
  Editor = 'Editor',
  InputNumber = 'InputNumber',
  Switch = 'Switch',
  TreeSelect = 'TreeSelect',
}

/** 查询方式 */
export enum QueryType {
  Equal = 'Equal',
  NotEqual = 'NotEqual',
  GreaterThan = 'GreaterThan',
  GreaterThanOrEqual = 'GreaterThanOrEqual',
  LessThan = 'LessThan',
  LessThanOrEqual = 'LessThanOrEqual',
  Like = 'Like',
  LikeLeft = 'LikeLeft',
  LikeRight = 'LikeRight',
  Between = 'Between',
  In = 'In',
  NotIn = 'NotIn',
}

/** 字典选择器类型（列可选项来源：系统字典/枚举/常量；关联不入生成代码，仅表单渲染值） */
export enum DictSelectorType {
  DictSelector = 'DictSelector',
  EnumSelector = 'EnumSelector',
  ConstSelector = 'ConstSelector',
}

/** 模板类型选项（label 取自后端 [Description]） */
export const TEMPLATE_TYPE_OPTIONS = [
  { label: '单表', value: TemplateType.Single },
  { label: '树表', value: TemplateType.Tree },
  { label: '主子表', value: TemplateType.MasterDetail },
]

/** 生成状态选项 */
export const GEN_STATUS_OPTIONS = [
  { label: '未生成', value: GenStatus.NotGenerated },
  { label: '已生成', value: GenStatus.Generated },
  { label: '生成失败', value: GenStatus.Failed },
]

/** 生成方式选项 */
export const GEN_TYPE_OPTIONS = [
  { label: '压缩包下载', value: GenType.Zip },
  { label: '自定义路径', value: GenType.CustomPath },
  { label: '预览', value: GenType.Preview },
]

/** 模板引擎选项 */
export const TEMPLATE_ENGINE_OPTIONS = [
  { label: 'Scriban', value: TemplateEngine.Scriban },
  { label: 'T4', value: TemplateEngine.T4 },
]

/** 数据库类型选项 */
export const DATABASE_TYPE_OPTIONS = [
  { label: 'MySQL', value: DatabaseType.MySql },
  { label: 'SQL Server', value: DatabaseType.SqlServer },
  { label: 'PostgreSQL', value: DatabaseType.PostgreSql },
  { label: 'Oracle', value: DatabaseType.Oracle },
  { label: 'SQLite', value: DatabaseType.Sqlite },
]

/** 表单显示类型选项 */
export const HTML_TYPE_OPTIONS = [
  { label: '文本框', value: HtmlType.Input },
  { label: '文本域', value: HtmlType.Textarea },
  { label: '下拉框', value: HtmlType.Select },
  { label: '单选框', value: HtmlType.Radio },
  { label: '复选框', value: HtmlType.Checkbox },
  { label: '日期控件', value: HtmlType.DatePicker },
  { label: '日期时间控件', value: HtmlType.DateTimePicker },
  { label: '时间控件', value: HtmlType.TimePicker },
  { label: '图片上传', value: HtmlType.ImageUpload },
  { label: '文件上传', value: HtmlType.FileUpload },
  { label: '富文本编辑器', value: HtmlType.Editor },
  { label: '数字输入框', value: HtmlType.InputNumber },
  { label: '开关', value: HtmlType.Switch },
  { label: '树形选择', value: HtmlType.TreeSelect },
]

/** 查询方式选项 */
export const QUERY_TYPE_OPTIONS = [
  { label: '等于', value: QueryType.Equal },
  { label: '不等于', value: QueryType.NotEqual },
  { label: '大于', value: QueryType.GreaterThan },
  { label: '大于等于', value: QueryType.GreaterThanOrEqual },
  { label: '小于', value: QueryType.LessThan },
  { label: '小于等于', value: QueryType.LessThanOrEqual },
  { label: '模糊查询', value: QueryType.Like },
  { label: '左模糊', value: QueryType.LikeLeft },
  { label: '右模糊', value: QueryType.LikeRight },
  { label: '范围查询', value: QueryType.Between },
  { label: '包含', value: QueryType.In },
  { label: '不包含', value: QueryType.NotIn },
]

/** 字典选择器类型选项 */
export const DICT_SELECTOR_TYPE_OPTIONS = [
  { label: '系统字典', value: DictSelectorType.DictSelector },
  { label: '枚举类型', value: DictSelectorType.EnumSelector },
  { label: '常量数组', value: DictSelectorType.ConstSelector },
]

/** 生成产物写入策略（机器拥有 vs 人类拥有） */
export enum ArtifactWriteMode {
  /** 总是覆盖（机器文件，禁止手工编辑） */
  AlwaysOverwrite = 'AlwaysOverwrite',
  /** 仅首次创建（人类文件，重新生成时跳过） */
  WriteOnce = 'WriteOnce',
}

/** 写入策略选项 */
export const ARTIFACT_WRITE_MODE_OPTIONS = [
  { label: '机器文件（总是覆盖）', value: ArtifactWriteMode.AlwaysOverwrite },
  { label: '人类文件（仅首次创建）', value: ArtifactWriteMode.WriteOnce },
]
