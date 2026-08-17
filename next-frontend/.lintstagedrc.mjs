/**
 * 暂存文件的检查任务。
 *
 * 文件路径是作为命令行参数下发的，而这些命令要过 cmd.exe，单条命令上限 8191 字符；
 * 一次提交涉及几百个文件时会直接撞上限、任务被杀。所以按字符长度切批下发——
 * 按个数切不行，路径长短差很多，同样的个数可能超限也可能远没用满。
 */

/** 单条命令的字符预算，留出命令名与 npx 包装的余量。 */
const MAX_COMMAND_LENGTH = 6000

/**
 * 把文件列表切成多条不超长的命令。
 * @param {string} command 命令前缀
 * @param {string[]} files 本次暂存的文件
 * @returns {string[]} 逐批的完整命令
 */
function batched(command, files) {
  const commands = []
  let batch = []
  let length = command.length

  for (const file of files) {
    const argument = `"${file}"`
    if (batch.length > 0 && length + argument.length + 1 > MAX_COMMAND_LENGTH) {
      commands.push(`${command} ${batch.join(' ')}`)
      batch = []
      length = command.length
    }
    batch.push(argument)
    length += argument.length + 1
  }

  if (batch.length > 0)
    commands.push(`${command} ${batch.join(' ')}`)

  return commands
}

export default {
  '*.{ts,vue,js,mjs}': files => [
    ...batched('oxlint --fix', files),
    ...batched('eslint --fix', files),
  ],
  '*.{json,css,html,md,yaml}': files => batched('prettier --write', files),
}
