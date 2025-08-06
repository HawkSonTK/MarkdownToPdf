# Markdown + Mermaid → PDF 一括変換ツール (C# + WebView2) 仕様書

## 1. 概要

* 指定フォルダ内のすべての `.md` ファイルを PDF に変換する GUI ツール。
* **Mermaid記法**を含む場合は WebView2 の JavaScript レンダリングを利用して図を生成し、そのまま PDF に出力。
* **GUI操作で完結、CLI操作不要**。

## 2. 想定環境

* Windows 10 以降 (WebView2 Runtime インストール済み)
* .NET 6 以上 (C# WPF or WinForms)
* 外部ライブラリ。

  * **Markdig** (マークダウン → HTML 変換)
  * **mermaid.min.js** (Mermaid レンダリング)
  * **Microsoft.Web.WebView2** (PDF出力)

## 3. 機能要件

### 3.1 メイン機能

1. **フォルダ選択**

   * GUIで Markdown ファイルを含むフォルダを選択。
   * サブフォルダ検索のオン/オフ切替。
2. **ファイル一覧表示**

   * 選択フォルダ内のMarkdownファイル一覧をリスト表示。
   * 相対パス形式でファイル構造を表示。
3. **リアルタイムプレビュー機能**

   * 選択したMarkdownファイルを1024×648サイズの専用ウィンドウでプレビュー。
   * WebView2によるMermaidダイアグラムのリアルタイムレンダリング。
   * PDF変換と同じエンジンを使用し、変換結果と同一の表示。
4. **ファイル切り替え機能**

   * プレビューウィンドウ内で「← 前」「次 →」ボタンによるファイル移動。
   * ファイル名表示とナビゲーションボタンの有効/無効制御。
5. **個別PDF変換**

   * プレビュー画面から表示中のファイルのみをPDF変換。
   * 変換進行状況をメイン画面のログに表示。
6. **一括変換**

   * 指定フォルダ内の `.md` ファイルを順番に処理し、同名の `.pdf` を出力。
7. **Mermaid対応**

   * Markdown の \`\`\`mermaid ブロックを HTML に埋め込み、WebView2 でレンダリング。
8. **PDF保存**

   * `PrintToPdfAsync()` を用いて、レンダリング結果を PDF として保存。

### 3.2 ユーザーインターフェース

* **メインウィンドウ構成**

  * フォルダ選択エリア
    * \[フォルダ選択] ボタン
    * 選択パス表示テキストボックス
  * オプション設定
    * \[サブフォルダも検索] チェックボックス
  * Markdownファイル一覧 (ListBox)
  * 操作ボタン
    * \[変換開始] ボタン
    * \[プレビュー] ボタン
  * 処理状況のログ表示 (ListBox)
  * 進行状況バー

* **プレビューウィンドウ構成 (1024×648)**

  * ファイル名表示エリア
  * WebView2プレビューエリア
  * 操作ボタン
    * \[← 前] ボタン - 前のファイルに移動
    * \[次 →] ボタン - 次のファイルに移動
    * \[PDF変換] ボタン - 表示中ファイルの個別変換
    * \[更新] ボタン - 現在ファイルの再読み込み
    * \[閉じる] ボタン - プレビューウィンドウを閉じる

### 3.3 出力仕様

* PDF は `元ファイル名.pdf` として元フォルダに保存。
* A4 縦、マージン 10mm、デフォルトフォントは "Meiryo"。

### 3.4 エラー処理

* Mermaid レンダリング失敗時：コードブロックをテキストとして PDF 化。
* Markdown が空の場合はスキップ。
* 処理中エラーはログに記録。

## 4. 技術仕様

### 4.1 Markdown → HTML

* **Markdig** で変換。
* HTML テンプレート例：

  ```html
  <html>
  <head>
    <meta charset="UTF-8">
    <script src="mermaid.min.js"></script>
    <script>mermaid.initialize({ startOnLoad: true });</script>
    <style>body { font-family: Meiryo; padding: 20px; }</style>
  </head>
  <body>
    {{BODY}}
  </body>
  </html>
  ```

### 4.2 PDF生成

* `await webView.CoreWebView2.PrintToPdfAsync("output.pdf");`

## 5. 非機能要件

* **軽量化**：EXE サイズは 30 MB 以下を目標。
* **処理速度**：Markdown 10 ファイル (1MB 各) を 5 秒以内で PDF 化。

## 6. 操作フロー

### 6.1 基本的な使用方法

1. アプリケーション起動
2. \[フォルダ選択] でMarkdownファイルを含むフォルダを選択
3. 必要に応じて \[サブフォルダも検索] をチェック
4. ファイル一覧で変換対象を確認

### 6.2 プレビュー機能の使用

1. ファイル一覧から任意のMarkdownファイルを選択
2. \[プレビュー] ボタンでプレビューウィンドウを開く
3. プレビューウィンドウで内容を確認
   * Mermaidダイアグラムも正しく表示される
   * \[← 前] \[次 →] で他のファイルに移動可能
4. 必要に応じて \[PDF変換] で個別変換

### 6.3 一括変換

1. メイン画面で \[変換開始] ボタンをクリック
2. 全てのMarkdownファイルが順次処理される
3. 変換状況がログエリアに表示される

## 7. 技術実装詳細

### 7.1 プレビュー機能

* **PreviewWindow.xaml**: プレビュー専用ウィンドウ
* **MarkdownConverter.ConvertToHtml()**: PDF変換と同じHTML生成エンジン
* **WebView2**: Mermaidレンダリングと同じWebView2エンジン
* **一時HTMLファイル**: PDF生成と同じNavigate方式でのロード

### 7.2 ファイル管理

* **MainWindow.markdownFiles[]**: 検索されたMarkdownファイルの配列
* **PreviewWindow.fileList[]**: プレビュー用ファイルリスト
* **PreviewWindow.currentIndex**: 現在表示中のファイルインデックス

## 8. 今後の拡張案

* フォルダ監視モード (Markdown 更新時に自動 PDF 化)
* CSS テーマ切替
* PDF 結合 (複数 Markdown を 1 PDF に)
* プレビューウィンドウのサイズ調整機能
* ファイル検索・フィルタリング機能
