#!/usr/bin/env node
// 把集成测试按类分成几片，给 CI 的库矩阵并行跑：每个用例都在新的临时库上起宿主，各片只跑分到的类
// 用法：node test-shard.mjs <methods.json> <片号，从 1 起> <片数> [<权重文件> <库>]
//   methods.json 是测试程序 `-list methods/json -noColor` 的输出：["命名空间.类.方法", …]（不加 -noColor，Linux 上 JSON 后面会多一个颜色复位符）
//   权重文件是 test-weights.mjs 从一轮 CI 结果算出的各库每个类的耗时；给了就按这个库的实测耗时分，没给按用例数分
//   权重文件里没有的类（新加的）按该库的平均单例耗时乘以它的方法数估；测出 0 秒的类（在这个库上整类跳过）不占重量
// 输出这一片的类全名，一行一个，交给 `--filter-class`（多个之间是或）
// 分法：类按重量从大到小、同重按名字，依次放进当前最轻的一片（同样轻取片号小的）；只看输入，哪台机器算都一样
// 每个类恰好落在一片里，全部片合起来就是全部类；分到的片没有类时失败（片数比类还多），不会悄悄跑出一片空的
import fs from 'node:fs'

const [file, indexText, countText, weightsFile, database] = process.argv.slice(2)
const index = Number(indexText)
const count = Number(countText)
if (!file || !Number.isInteger(count) || count < 1 || !Number.isInteger(index) || index < 1 || index > count || (weightsFile && !database)) {
  console.error('usage: node test-shard.mjs <methods.json> <shard 1..N> <N> [<weights.json> <database>]')
  process.exit(64)
}

const methods = JSON.parse(fs.readFileSync(file, 'utf8'))
if (!Array.isArray(methods) || methods.length === 0 || methods.some(m => typeof m !== 'string' || !m.includes('.'))) {
  console.error(`${file} is not a non-empty list of 'Namespace.Class.Method' names`)
  process.exit(1)
}

// 方法名里没有点，最后一个点之前就是类全名（嵌套类带 +）
const sizes = new Map()
for (const method of methods) {
  const type = method.slice(0, method.lastIndexOf('.'))
  sizes.set(type, (sizes.get(type) ?? 0) + 1)
}

let weightOf = type => sizes.get(type)
if (weightsFile) {
  const measured = JSON.parse(fs.readFileSync(weightsFile, 'utf8')).databases?.[database]
  if (!measured || typeof measured.perTest !== 'number' || typeof measured.classes !== 'object') {
    console.error(`${weightsFile} has no weights for '${database}'`)
    process.exit(1)
  }

  const estimated = [...sizes.keys()].filter(type => !(type in measured.classes))
  if (estimated.length > 0)
    console.error(`estimated by ${measured.perTest} s per test: ${estimated.join(', ')}`)
  weightOf = type => measured.classes[type] ?? sizes.get(type) * measured.perTest
}

const shards = Array.from({ length: count }, () => ({ weight: 0, tests: 0, types: [] }))
const ordered = [...sizes.keys()].map(type => [type, weightOf(type)]).sort(([a, x], [b, y]) => y - x || (a < b ? -1 : a > b ? 1 : 0))
for (const [type, weight] of ordered) {
  const lightest = shards.reduce((best, shard) => (shard.weight < best.weight ? shard : best))
  lightest.types.push(type)
  lightest.weight += weight
  lightest.tests += sizes.get(type)
}

for (const [i, shard] of shards.entries())
  console.error(`shard ${i + 1}/${count}: ${shard.types.length} classes, ${shard.tests} tests, weight ${Math.round(shard.weight)}`)

const mine = shards[index - 1]
if (mine.types.length === 0) {
  console.error(`shard ${index}/${count} has no test classes; use fewer shards`)
  process.exit(1)
}

console.log(mine.types.join('\n'))
