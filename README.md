# MarkdownToPdf

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![GitHub last commit](https://img.shields.io/github/last-commit/HawkSonTK/MarkdownToPdf)
![GitHub issues](https://img.shields.io/github/issues/HawkSonTK/MarkdownToPdf)

## 概要

MarkdownToPdf は、Markdownファイルを一括で PDF に変換するためのシンプルなツールです。
**複数ファイル対応**や **Mermaid図のレンダリング** など、ドキュメント作成を効率化します。

---

## 特徴

* 複数の Markdown ファイルを一括 PDF 変換
* **リアルタイムプレビュー機能** - 変換前にブラウザで内容を確認
* **ファイル切り替え機能** - プレビュー画面で前/次のファイルに移動
* **個別PDF変換** - プレビュー画面から選択したファイルのみをPDF化
* Mermaid 図やコードブロックを含む Markdown に対応
* サブフォルダ検索対応
* 直感的なGUIインターフェース
* MITライセンスで商用利用OK

---

## インストール方法

```bash
git clone https://github.com/HawkSonTK/MarkdownToPdf.git
cd MarkdownToPdf
```

ビルド方法や実行ファイルの作成手順は `docs/INSTALL.md`（作成予定）に記載します。

---

## 使い方

### GUI版（推奨）

1. **フォルダ選択**: 「フォルダ選択」ボタンでMarkdownファイルを含むフォルダを選択
2. **オプション設定**: 必要に応じて「サブフォルダも検索」をチェック
3. **ファイル確認**: 下部リストで変換対象のファイル一覧を確認

### プレビュー機能

* **ファイル選択**: リストから任意のMarkdownファイルをクリック
* **プレビュー表示**: 「プレビュー」ボタンでリアルタイムプレビューを表示
  - Mermaidダイアグラムも正しく表示
  - 1024×648サイズの見やすい画面
* **ファイル切り替え**: プレビュー画面で「← 前」「次 →」ボタンで他のファイルに移動
* **個別変換**: プレビュー画面の「PDF変換」ボタンで表示中のファイルのみをPDF化
* **一括変換**: メイン画面の「変換開始」ボタンで全ファイルを一括変換

### CLI版（従来方式）

```bash
MarkdownToPdf.exe -i ./input -o ./output
```

**オプション例:**

* `-i` 入力フォルダ（Markdownファイル格納場所）
* `-o` 出力フォルダ（PDF生成先）
* `--mermaid` Mermaidレンダリングを有効化

---

## ライセンス

このプロジェクトは [MIT License](LICENSE) の下で公開されています。
Copyright (c) 2025 **HawkSon (HawkSonTK)**.

---

## 免責事項

本ツールの利用により生じた損害や不具合について、
作者は一切責任を負いません。自己責任でご利用ください。

---

## 作者

* **HawkSon (HawkSonTK)**

  * GitHub: [https://github.com/HawkSonTK](https://github.com/HawkSonTK)
