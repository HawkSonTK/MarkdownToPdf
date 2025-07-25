# サンプルMarkdown (Mermaidモリモリ版)

このファイルは、Markdownの基本記法とMermaidの各種図をテストするためのサンプルです。

## 見出しとテキスト
- リスト例 1
- リスト例 2
- **強調**、*斜体*、~~取り消し線~~

## コードブロック
```csharp
for (int i = 0; i < 3; i++) {
    Console.WriteLine($"Count: {i}");
}
```

---

## Mermaid サンプル集

### フローチャート (基本)
```mermaid
graph TD
  A[Start] --> B{条件判定}
  B -->|Yes| C[成功]
  B -->|No| D[失敗]
```

### フローチャート (LR方向)
```mermaid
graph LR
  A[開始] --> B[処理1]
  B --> C[処理2]
  C --> D[終了]
```

### シーケンス図
```mermaid
sequenceDiagram
  participant ユーザー
  participant サーバー
  ユーザー->>サーバー: リクエスト送信
  サーバー-->>ユーザー: レスポンス返却
```

### クラス図
```mermaid
classDiagram
  class 車 {
    +String メーカー
    +String モデル
    +走る()
  }
  車 <|-- スポーツカー
```

### 状態遷移図
```mermaid
stateDiagram-v2
  [*] --> 停止
  停止 --> 起動
  起動 --> 実行中
  実行中 --> 停止
```

### ガントチャート
```mermaid
gantt
  title プロジェクト計画
  dateFormat  YYYY-MM-DD
  section 計画
  タスク1 :done,    des1, 2025-07-01,2025-07-05
  タスク2 :active,  des2, 2025-07-06, 3d
  section 実行
  タスク3 :         des3, after des2, 5d
```

### ER図
```mermaid
erDiagram
  USER ||--o{ ORDER : places
  ORDER ||--|{ LINE-ITEM : contains
  USER {
    string name
    string email
  }
  ORDER {
    int orderNumber
    string date
  }
```
