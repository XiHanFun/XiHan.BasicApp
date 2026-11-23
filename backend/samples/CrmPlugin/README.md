# CrmPlugin

进程外插件示例：短名 `crm`，清单 ID `xihan.samples.crm`，按租户在进程内存客户。它演示插件包的两份清单、前端文案与页面描述怎样写，集成测试用它走安装、启用、HTTP 调用、租户隔离、套餐门控、接口文档分组、停用与卸载的全程，前端端到端用它在插件页面里增删改查。

插件只依赖 protobuf 与 gRPC，不引用任何 XiHan 包；协议桩由 `Protocol/plugin.proto` 生成，它是协议 `xihan.plugin.v1` 的原样副本。

## 贡献

| 操作 | HTTP | 权限码 |
| --- | --- | --- |
| `crm-customers.list` | `GET /api/crm-customers` | `crm.crm-customers.read` |
| `crm-customers.create` | `POST /api/crm-customers` | `crm.crm-customers.create` |
| `crm-customers.update` | `PUT /api/crm-customers/{id}` | `crm.crm-customers.update` |
| `crm-customers.delete` | `DELETE /api/crm-customers/{id}` | `crm.crm-customers.delete` |

- 四个权限码都在租户侧，写操作审计。租户要用，平台先把它们加进租户的套餐，再授给角色。
- 页面 `crm.customers`（`/plugins/crm/customers`），声明式描述在 `pages/customers.json`：列表、新建、修改与删除各绑一个操作，前端文案在 `locales/<区域>.json`，嵌套在短名 `crm` 之下。
- 握手的消息目录（`CrmCatalog`）带失败文案、权限码显示名、操作标题、接口文档的分组名 `openapi.groups.crm.*` 与资源标签名 `openapi.tags.crm_customers.*`。
- 失败码：名称为空 `CRM_VALIDATION_001`，客户不存在或不在本租户 `CRM_NOTFOUND_001`。

## 打包

`.xhplugin` 是 zip：根上放 `xihan.manifest.json`、`basicapp.plugin.json`、`locales/`、`pages/`，构建产物放进 `runtimes/<运行时标识>/`，`basicapp.plugin.json` 的 `runtimes` 列出包里有的运行时标识。

```powershell
dotnet publish backend/samples/CrmPlugin -c Release -r win-x64 --self-contained false -o artifacts/crm/runtimes/win-x64
```

包的格式与安装前的核对见文档[插件](../../../docs/backend/plugins.md)。
