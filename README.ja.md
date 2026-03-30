<!-- mcp-name: io.github.pierre3/zap-mcp -->
# dotnet-zap-mcp

[![NuGet](https://img.shields.io/nuget/v/dotnet-zap-mcp.svg)](https://www.nuget.org/packages/dotnet-zap-mcp)
[![NuGet Downloads](https://img.shields.io/nuget/dt/dotnet-zap-mcp.svg)](https://www.nuget.org/packages/dotnet-zap-mcp)
[![Release](https://github.com/pierre3/dotnet-zap-mcp/actions/workflows/release.yml/badge.svg)](https://github.com/pierre3/dotnet-zap-mcp/actions/workflows/release.yml)

[OWASP ZAP](https://www.zaproxy.org/) 用の MCP (Model Context Protocol) サーバーです。AI エージェント (Claude、GitHub Copilot など) が MCP を介して ZAP の脆弱性スキャンを操作できるようにします。

## 特徴

- OWASP ZAP を制御する 45 の MCP ツール（スキャン、アラート、スパイダー、Ajax スパイダー、コンテキスト、認証、レポートなど）
- 組み込みの Docker Compose 管理（1 回のツール呼び出しで ZAP を起動/停止）
- 設定不要のセットアップ：API キーの自動生成と Docker アセットの自動展開
- MCP 互換のあらゆるクライアントで動作（Claude Desktop、VS Code など）

## インストール

```bash
dotnet tool install -g dotnet-zap-mcp
```

### 前提条件

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)（組み込みの ZAP コンテナ管理に必要）

## 設定

### 設定不要（推奨）

設定は不要です。エージェントが `DockerComposeUp` を呼び出すと、自動的に以下が実行されます：
1. Docker アセットを `~/.zap-mcp/docker/` に展開
2. ランダムな API キーを生成
3. `localhost:8090` で ZAP コンテナを起動
4. ZAP が正常に起動するまで待機

#### データの永続化

ZAP コンテナは 2 つの Docker 名前付きボリュームを使用します：

| ボリューム | コンテナパス | 用途 |
|-----------|-------------|------|
| `zap-home` | `/home/zap/.ZAP` | ZAP 設定、コンテキスト、セッション、スキャンポリシー（再起動後も保持） |
| `zap-data` | `/zap/wrk/data` | レポート、セッションファイル、コンテキストのインポート/エクスポート用共有ディレクトリ |

初回起動時にテンプレートの `config.xml` が `zap-home` にコピーされます。2 回目以降の起動では API キーのみ更新され、ZAP を通じて行った変更（コンテキスト、認証設定、スキャンポリシーなど）はすべて保持されます。

`zap-data` ボリュームには以下が含まれます：
- `reports/` — 生成されたスキャンレポート
- `sessions/` — 保存された ZAP セッション
- `contexts/` — エクスポートされたコンテキストファイル

### Claude Desktop / Claude Code

MCP 設定に以下を追加してください：

```json
{
  "mcpServers": {
    "zap": {
      "command": "zap-mcp"
    }
  }
}
```

### VS Code (Copilot)

`.vscode/mcp.json` に以下を追加してください：

```json
{
  "servers": {
    "zap": {
      "command": "zap-mcp"
    }
  }
}
```

### 既存の ZAP インスタンスを使用する場合

既に ZAP インスタンスが稼働している場合は、環境変数で接続情報を指定してください：

```json
{
  "mcpServers": {
    "zap": {
      "command": "zap-mcp",
      "env": {
        "ZAP_BASE_URL": "http://localhost:8090",
        "ZAP_API_KEY": "your-api-key"
      }
    }
  }
}
```

## 利用可能なツール

### Docker 管理

| ツール | パラメータ | 説明 |
|-------|-----------|------|
| `DockerComposeUp` | — | ZAP コンテナを起動し、正常起動を待機 |
| `DockerComposeDown` | — | ZAP コンテナを停止・削除 |
| `DockerComposeStatus` | — | コンテナのステータスを確認 |
| `DockerComposeLogs` | `tail` | 直近のコンテナログを取得 |

### ZAP コア

| ツール | パラメータ | 説明 |
|-------|-----------|------|
| `GetVersion` | — | ZAP への接続を確認 |
| `GetHosts` | — | 記録されたホストの一覧 |
| `GetSites` | — | 記録されたサイトの一覧 |
| `GetUrls` | `baseUrl` | 指定ベース URL の記録された URL 一覧 |

### スパイダー

| ツール | パラメータ | 説明 |
|-------|-----------|------|
| `StartSpider` | `url`, `maxChildren`, `recurse`, `subtreeOnly`, `contextName` | ページのクロール・検出を行うスパイダースキャンを開始 |
| `GetSpiderStatus` | `scanId` | スパイダーの進捗を確認（0-100%） |
| `GetSpiderResults` | `scanId` | スパイダーが検出した URL を取得 |
| `StopSpider` | `scanId` | 実行中のスパイダースキャンを停止 |

### アクティブスキャン

| ツール | パラメータ | 説明 |
|-------|-----------|------|
| `StartActiveScan` | `url`, `recurse`, `inScopeOnly`, `scanPolicyName`, `contextId` | アクティブ脆弱性スキャンを開始 |
| `GetActiveScanStatus` | `scanId` | アクティブスキャンの進捗を確認（0-100%） |
| `StopActiveScan` | `scanId` | 実行中のアクティブスキャンを停止 |

### パッシブスキャン

| ツール | パラメータ | 説明 |
|-------|-----------|------|
| `GetPassiveScanStatus` | — | パッシブスキャンの進捗を確認（残りレコード数） |

### アラート

| ツール | パラメータ | 説明 |
|-------|-----------|------|
| `GetAlertsSummary` | `baseUrl` | リスクレベル別のアラート件数 |
| `GetAlerts` | `baseUrl`, `start`, `count`, `riskId` | ページネーションとリスクフィルター付きの詳細アラート一覧 |

### レポート

| ツール | パラメータ | 説明 |
|-------|-----------|------|
| `GetHtmlReport` | — | HTML スキャンレポートを生成 |
| `GetJsonReport` | — | JSON スキャンレポートを生成 |
| `GetXmlReport` | — | XML スキャンレポートを生成 |

### コンテキスト管理

| ツール | パラメータ | 説明 |
|-------|-----------|------|
| `GetContextList` | — | ZAP に定義されたすべてのコンテキストを一覧表示 |
| `GetContext` | `contextName` | コンテキストの詳細を取得（スコープパターンなど） |
| `CreateContext` | `contextName` | 新しいコンテキストを作成 |
| `RemoveContext` | `contextName` | コンテキストを削除 |
| `IncludeInContext` | `contextName`, `regex` | コンテキストスコープに URL 包含パターンを追加 |
| `ExcludeFromContext` | `contextName`, `regex` | コンテキストスコープに URL 除外パターンを追加 |
| `ImportContext` | `contextFilePath` | コンテキストファイルを ZAP にインポート |
| `ExportContext` | `contextName`, `contextFilePath` | コンテキストをファイルにエクスポート |

### 認証

| ツール | パラメータ | 説明 |
|-------|-----------|------|
| `GetAuthenticationMethod` | `contextId` | コンテキストに設定された認証方式を取得 |
| `SetAuthenticationMethod` | `contextId`, `authMethodName`, `authMethodConfigParams` | 認証方式を設定（フォーム認証、JSON 認証、スクリプト認証、HTTP 認証） |
| `SetLoggedInIndicator` | `contextId`, `loggedInIndicatorRegex` | ログイン状態を示す正規表現パターンを設定 |
| `SetLoggedOutIndicator` | `contextId`, `loggedOutIndicatorRegex` | ログアウト状態を示す正規表現パターンを設定 |

### ユーザー

| ツール | パラメータ | 説明 |
|-------|-----------|------|
| `GetUsersList` | `contextId` | コンテキストの全ユーザーを一覧表示 |
| `CreateUser` | `contextId`, `name` | 新しいユーザーを作成 |
| `RemoveUser` | `contextId`, `userId` | ユーザーを削除 |
| `SetAuthenticationCredentials` | `contextId`, `userId`, `authCredentialsConfigParams` | ユーザーの認証情報を設定（ユーザー名/パスワード） |
| `SetUserEnabled` | `contextId`, `userId`, `enabled` | ユーザーの有効/無効を切り替え |

### 強制ユーザー

| ツール | パラメータ | 説明 |
|-------|-----------|------|
| `SetForcedUser` | `contextId`, `userId` | コンテキストの強制ユーザーを設定 |
| `SetForcedUserModeEnabled` | `enabled` | 強制ユーザーモードのグローバルな有効/無効を切り替え |
| `GetForcedUserStatus` | `contextId` | 強制ユーザーモードのステータスと現在の強制ユーザーを取得 |

### Ajax スパイダー

| ツール | パラメータ | 説明 |
|-------|-----------|------|
| `StartAjaxSpider` | `url`, `inScope`, `contextName`, `subtreeOnly` | JavaScript を多用するアプリ向けに Ajax スパイダーを開始 |
| `StartAjaxSpiderAsUser` | `contextName`, `userId`, `url`, `subtreeOnly` | 特定ユーザーとして Ajax スパイダーを開始 |
| `GetAjaxSpiderStatus` | — | Ajax スパイダーのステータスを取得（実行中/停止） |
| `GetAjaxSpiderResults` | — | Ajax スパイダーの結果サマリーを取得 |
| `StopAjaxSpider` | — | Ajax スパイダーを停止 |

## 一般的なワークフロー

1. エージェントが `DockerComposeUp` を呼び出して ZAP を起動
2. ブラウザ/Playwright を ZAP プロキシ経由で使用するよう設定（`http://127.0.0.1:8090`）
3. プロキシ経由で対象アプリケーションを閲覧
4. エージェントが `StartSpider` でアプリケーションをクロールし、`GetSpiderStatus` で進捗を監視
5. エージェントが `GetPassiveScanStatus` でパッシブスキャンの完了を待機
6. エージェントが主要ページで `StartActiveScan` を実行し、`GetActiveScanStatus` で進捗を監視
7. エージェントが `GetAlertsSummary` と `GetAlerts` で脆弱性の検出結果を取得
8. エージェントが `GetHtmlReport` または `GetJsonReport` でスキャンレポートを生成
9. 完了後、エージェントが `DockerComposeDown` を呼び出し

> **注意:** ZAP の設定（コンテキスト、認証、スキャンポリシー）は `zap-home` Docker ボリュームに永続化されます。設定を失うことなくコンテナの停止・再起動が可能です。

## ライセンス

[MIT](LICENSE)
