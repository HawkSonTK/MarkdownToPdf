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
2. **一括変換**

   * 指定フォルダ内の `.md` ファイルを順番に処理し、同名の `.pdf` を出力。
3. **Mermaid対応**

   * Markdown の \`\`\`mermaid ブロックを HTML に埋め込み、WebView2 でレンダリング。
4. **PDF保存**

   * `PrintToPdfAsync()` を用いて、レンダリング結果を PDF として保存。

### 3.2 ユーザーインターフェース

* **ウィンドウ構成**

  * \[フォルダ選択] ボタン
  * \[変換開始] ボタン
  * 処理状況のログ表示 (ListBox)
  * 進行状況バー (任意)

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

## 6. 今後の拡張案

* フォルダ監視モード (Markdown 更新時に自動 PDF 化)
* CSS テーマ切替
* PDF 結合 (複数 Markdown を 1 PDF に)
