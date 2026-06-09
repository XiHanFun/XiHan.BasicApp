# Domain/Enums — 跨域共享枚举

本目录存放**被多个实体 / DTO / 服务共享引用**的枚举（如 `EnableStatus`、`ValidityStatus`、`AuthorizationGrantSource`）。命名空间：`XiHan.BasicApp.Saas.Domain.Enums`。

## 与 `Domain/Entities/Enums` 的区别

| 目录 | 归属 | 命名空间 | 文件命名 | 示例 |
|---|---|---|---|---|
| `Domain/Enums`（本目录） | 跨实体共享 | `...Domain.Enums` | `<枚举名>.cs` | `EnableStatus`、`ValidityStatus` |
| `Domain/Entities/Enums` | 与单一实体强绑定 | `...Domain.Entities` | `<实体>.Enum.cs` | `SysUser` 的 `UserGender`、`SysAccessLog` 的 `AccessResult` |

## 归属判定约定（对应审查报告 B10）

- 新增枚举若**仅服务单一实体**，放 `Domain/Entities/Enums`，命名 `<实体>.Enum.cs`。
- 一旦某 `Domain/Entities/Enums` 下的枚举**被第二个实体引用**，应**上移到本目录**，避免实体间通过枚举文件形成隐式耦合。
- 枚举值必须**显式赋值**（避免序号漂移影响前后端同步）。

> 注：审查报告 A4 建议将本目录语义化重命名为 `SharedEnums`，但 `EnableStatus` 等被全模块约 230 处引用，全量命名空间变更成本/风险较高；当前以本 README 明确归属判定作为低风险缓解，重命名列为可选的大范围重构。
