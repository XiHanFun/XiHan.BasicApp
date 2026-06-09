# Domain/Entities/Enums — 实体专属枚举

本目录存放**与单一实体强绑定**的枚举，命名为 `<实体>.Enum.cs`（如 `SysUser.Enum.cs` 的 `UserGender`、`SysAccessLog.Enum.cs` 的 `AccessResult`）。命名空间：`XiHan.BasicApp.Saas.Domain.Entities`。

跨多个实体共享的枚举请放 `Domain/Enums`（共享枚举区），归属判定约定详见该目录的 `README.md`。

枚举值必须**显式赋值**，避免序号漂移影响前后端枚举同步。
