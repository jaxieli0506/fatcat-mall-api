# Fat Cat 購物商城 — 後端 API

> 資展國際（iSpan）結訓專題・4 人團隊・2025/07 – 2025/08
> 以 **ASP.NET Core 8 Web API** 打造的電商平台後端，涵蓋會員、商品、訂單、金流、物流、直播帶貨與即時客服。

<!-- TODO: 放一張 Swagger 或前台畫面截圖 / GIF -->
<!-- ![demo](docs/demo.gif) -->

## 技術棧

| 類別 | 使用技術 |
|---|---|
| 框架 | ASP.NET Core 8 Web API、C# |
| 資料庫 | SQL Server、Entity Framework Core 8（Database First，46 個資料表實體） |
| 驗證 / 安全 | JWT Bearer、BCrypt 密碼雜湊、OAuth 2.0 第三方登入（Google / Facebook / LINE） |
| 即時通訊 | SignalR（線上客服聊天、直播間推播精選商品） |
| 第三方服務 | 綠界 ECPay（金流、超商門市地圖）、AWS IVS（直播 / 聊天室）、Gmail SMTP（MailKit） |
| 文件 / 測試 | Swagger (OpenAPI) |
| 協作 | Git 分支協作（每人每週一個分支，統一合併至 `dev`） |

## 系統功能

- **前台**：會員註冊登入、第三方登入、商品瀏覽 / 搜尋 / 分類 / 標籤、購物車、收藏、下單與綠界付款、超商取貨、商品評論與檢舉、客訴、優惠券、通知中心、直播觀看
- **後台**：管理員登入與權限、會員管理、商品上架、訂單 / 出貨管理、評論審核、活動與優惠券、操作紀錄、營運數據分析（會員 / 商品 / 銷售）、直播開播

## 🙋‍♀️ 我負責的部分

> 在團隊中負責 **會員驗證系統、第三方登入、通知系統、管理員後台**，並擔任 **Git 整合負責人**，每週合併 4 位成員的分支到 `dev`。

### 1. 會員驗證與 JWT
- 實作註冊、登入、修改密碼、忘記密碼 / 重設密碼流程
- 以 `JwtService` 簽發 Token，前台會員與後台管理員使用不同的 Claims（含 Role）
- 密碼以 **BCrypt** 雜湊儲存；為相容資料庫中既有的明文測試資料，登入時若偵測到明文密碼，**驗證成功後自動升級為雜湊**
- 相關檔案：[`AuthController`](api-backend/Controllers/AuthController.cs)、[`AuthService`](api-backend/Services/AuthService.cs)、[`JwtService`](api-backend/Services/JwtService.cs)

### 2. 第三方登入（Google / Facebook / LINE）
- 串接三家 OAuth 2.0 登入流程，以 `ExternalLogin` 資料表記錄綁定關係
- 支援「第三方帳號直接登入」以及「已登入會員在個人頁面綁定第三方帳號」
- 相關檔案：[`ExternalLoginController`](api-backend/Controllers/ExternalLoginController.cs)、[`ExternalLoginService`](api-backend/Services/ExternalLoginService.cs)

### 3. Email 寄送
- 使用 MailKit 透過 Gmail SMTP 寄送註冊驗證信與重設密碼信
- 相關檔案：[`EmailService`](api-backend/Services/EmailService.cs)

### 4. 會員地址管理 + 7-11 門市地圖
- 個人頁面的收件地址 CRUD
- 串接綠界電子地圖，讓使用者選擇超商取貨門市並接收回傳結果
- 相關檔案：[`AddressController`](api-backend/Controllers/AddressController.cs)、[`AddressEcpayController`](api-backend/Controllers/AddressEcpayController.cs)、[`AddressService`](api-backend/Services/AddressService.cs)

### 5. 通知系統
- 管理員可發送通知給全體會員、全體管理員或指定會員；以 `Notification`（內容）+ `NotificationReceiver`（收件人與已讀狀態）兩張表拆分設計
- 事件觸發通知：評論檢舉審核結果、新商品上架
- 會員 / 管理員查詢個人通知、標記已讀
- 相關檔案：[`NotificationsController`](api-backend/Controllers/NotificationsController.cs)

### 6. 管理員後台
- 管理員列表（查詢分頁、新增、編輯、軟刪除），已刪除名單與復原僅限「超級管理員」角色（`[Authorize(Roles = ...)]`）
- 會員列表與搜尋、訂單明細
- 相關檔案：[`AdminsController`](api-backend/Controllers/AdminsController.cs)、[`AdminMembersController`](api-backend/Controllers/AdminMembersController.cs)

### 7. 團隊 Git 整合
- 規劃分支命名（成員代號 + 週次，如 `jj7`、`rr7`），每週將各分支合併進 `dev` 並處理衝突
- 原始團隊 repo 共 270 個 commits，個人貢獻 110 個（含合併）

## 專案架構

```
api-backend/
├── Controllers/     # 40 個 API Controller
├── Services/        # 商業邏輯（介面 + 實作，透過 DI 注入）
├── Hubs/            # SignalR ChatHub
├── Models/          # EF Core 實體（Database First 產生）
├── rrDTOs/          # 資料傳輸物件
├── ViewModels/
├── wwwroot/         # 商品圖片、上傳檔案
└── Program.cs       # DI、JWT、CORS、SignalR 設定
```

請求流程：`Controller` → `Service（介面）` → `TeamDb2Context (EF Core)` → SQL Server

## 本機執行

**需求**：.NET 8 SDK、SQL Server / LocalDB

1. 還原資料庫：在 SSMS 對「資料庫」按右鍵 →「還原資料庫」→ 選擇裝置 [`TeamDb.bak`](TeamDb.bak)，**目的資料庫名稱設為 `TeamDb2`**（內含測試用假資料）
2. 修改 [`appsettings.json`](api-backend/appsettings.json) 的連線字串（預設為 LocalDB）
3. 以 user-secrets 設定金鑰（不要直接寫進 appsettings.json）：
   ```bash
   cd api-backend
   dotnet user-secrets set "Jwt:Key" "<至少32字元的隨機字串>"
   dotnet user-secrets set "ExternalLogin:Google:ClientId" "<...>"
   dotnet user-secrets set "ExternalLogin:Google:ClientSecret" "<...>"
   dotnet user-secrets set "SmtpSettings:SenderEmail" "<...>"
   dotnet user-secrets set "SmtpSettings:Password" "<Gmail 應用程式密碼>"
   # Facebook / LINE / AWS IVS 依需求設定，完整欄位見 appsettings.json
   ```
4. 執行
   ```bash
   dotnet run --project api-backend
   ```
5. 開啟 `https://localhost:7017/swagger`

> 金流使用綠界官方公開的**測試**商店代號，不會產生真實交易。

## 團隊成員

| 代號 | 成員 | 負責範圍 |
|---|---|---|
| jj | **佳馨（本人）** | 會員驗證、第三方登入、通知、管理員後台、Git 整合 |
| rr | 如謙 | <!-- TODO --> |
| yy | 玥彤 | <!-- TODO --> |
| zz | 梓瑋 | <!-- TODO --> |

> 本 repo 為團隊專題最終版本（`dev` 分支）的整理版，已移除所有金鑰設定。原始團隊 repo 因含開發期間的設定檔而設為私人。
