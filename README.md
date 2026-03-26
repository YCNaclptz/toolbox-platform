# Toolbox Platform

後端微服務平台，提供統一的 API Gateway、集中式身份驗證，以及可擴展的業務服務架構。

## 架構

```
Client → Gateway (YARP) ─┬→ Auth.Api      → SQL Server (auth schema)
                         └→ SwearJar.Api   → SQL Server (swearjar schema)
```

| 服務 | 說明 | Port |
|------|------|------|
| **Gateway** | YARP 反向代理，統一路由入口 | 7200 |
| **Auth.Api** | 身份驗證、使用者與應用程式管理 | 7201 |
| **SwearJar.Api** | 髒話罰款紀錄 CRUD 與統計 | 7202 |
| **Platform.Shared** | 共用函式庫（JWT、Base Entity） | — |

## 技術棧

- **.NET 10** / ASP.NET Core Web API / EF Core
- **YARP** 反向代理
- **JWT** 身份驗證（Access Token + Refresh Token）
- **SQL Server**（各服務獨立 Schema）
- **Azure Blob Storage** / Azurite（頭像儲存）
- **Azure Key Vault**（生產環境機密管理）
- **Scalar** OpenAPI 文件

## 本地開發

### 前置需求

- .NET 10.0 SDK
- SQL Server
- Docker（用於 Azurite）

### 啟動服務

```bash
# 終端 1 — Auth.Api（會自動啟動 Azurite Docker 容器）
cd Auth.Api && dotnet run

# 終端 2 — SwearJar.Api
cd SwearJar.Api && dotnet run

# 終端 3 — Gateway
cd Gateway && dotnet run
```

開發環境會自動執行資料庫 Migration。

### 預設帳號

| 帳號 | 密碼 | 備註 |
|------|------|------|
| admin | admin123 | 首次登入需更改密碼 |

### API 文件

- Gateway：`https://localhost:7200`
- Auth.Api：`https://localhost:7201/scalar`
- SwearJar.Api：`https://localhost:7202/scalar`

## 部署

透過 GitHub Actions 自動部署至 Azure App Service，觸發條件為 push 到 `main` 分支。

### 所需 Secrets / Variables

| 類型 | 名稱 | 說明 |
|------|------|------|
| Secret | `AZURE_PUBLISH_PROFILE_GATEWAY` | Gateway 發佈設定檔 |
| Secret | `AZURE_PUBLISH_PROFILE_AUTH` | Auth.Api 發佈設定檔 |
| Secret | `AZURE_PUBLISH_PROFILE_SWEARJAR` | SwearJar.Api 發佈設定檔 |
| Variable | `AZURE_APP_NAME_GATEWAY` | Gateway App Service 名稱 |
| Variable | `AZURE_APP_NAME_AUTH` | Auth.Api App Service 名稱 |
| Variable | `AZURE_APP_NAME_SWEARJAR` | SwearJar.Api App Service 名稱 |

## 專案結構

```
toolbox-platform/
├── Gateway/              # YARP API Gateway
├── Auth.Api/             # 身份驗證服務
├── SwearJar.Api/         # 髒話罰款紀錄服務
├── Platform.Shared/      # 共用函式庫
├── appsettings.Shared.json  # 共用設定
├── docker-compose.yml    # Azurite 本地儲存模擬
└── SwearJar.sln
```
