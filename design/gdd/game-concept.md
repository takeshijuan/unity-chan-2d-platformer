# Game Concept: 職業オーブのレガシー / *Unity-chan and the Class Orbs*

*Created: 2026-04-23*
*Status: Draft — Brainstorm Phase Complete*
*Review Mode: Full*

---

## Elevator Pitch

> **It's a 2D 横スクロール・メトロイドヴァニア where you master instant class-switching combos in a forgotten kingdom to reclaim its memory.**
>
> Unityちゃんが古代の「職業オーブ」を集めながら、**剣士・弓士・魔法使い**などを瞬時に切替えて攻略する 2D メトロイドヴァニア。剣士オーブで壁を切り、魔法使いオーブで結界を解き、弓士オーブで遠隔ギミックを起動する。**職業そのものが"鍵"であり、"武器"であり、"遊びの深さ"である**。

---

## Core Identity

| Aspect | Detail |
| ---- | ---- |
| **Genre** | 2D 横スクロール・メトロイドヴァニア（アクションアドベンチャー） |
| **Platform** | PC（Steam / Itch.io / Epic Games Store） |
| **Target Audience** | メトロイドヴァニア愛好者＋MapleStory系 2D ARPG ファン＋Unityちゃんファン（詳細は Target Player Profile） |
| **Player Count** | Single-player |
| **Session Length** | 30〜60 分（メトロイドヴァニア標準セッション） |
| **Monetization** | Premium（買い切り）、Tier 2b で Early Access 想定 |
| **Estimated Scope** | **Large（18〜24ヶ月、solo）** — 段階リリース戦略（Demo → EA → Full） |
| **Comparable Titles** | *Hollow Knight*（探索）、*Mega Man X*（武器切替）、*MapleStory*（爽快2Dアクション） |

---

## Core Fantasy

**「状況を読んで最適な姿に変身する、万能冒険者」**

プレイヤーは古代に滅びた王国を歩む Unityちゃんとして、戦況やパズルに応じて **一瞬で職業を切替え、全く違う戦い方** をできる自分自身に酔う。強い敵の前でも「手札の組合せ」で勝てる実感、遠くの謎めいた扉を見つけたとき「あの職業があれば開けそう」と直感する心理、そして **切替という動詞そのものが視覚・聴覚で気持ちいい** 体験。

これは「選ばれた勇者の剣を振るう」物語ではなく、「自分の手札を選び、組み立て、切り回す」 **戦術的爽快感** の物語。MapleStory 的な「2D 横スクでキャラが気持ちよく動く」快感と、Mega Man X 的な「武器の切替で世界が別の見え方をする」知的興奮の融合を目指す。

---

## Unique Hook

**「*Mega Man X* の武器切替 AND ALSO *Metroid* 系の能力ゲート × 職業の個性」**

ジャンル通例のメトロイドヴァニア（1 つの能力 = 1 つのゲート）と異なり、**本作では "能力" が "職業" として層状に重なる**。プレイヤーは剣士で壁を切り開き、その壁の裏で魔法使いに切替えて結界を解き、更に弓士に切替えて対岸のスイッチを射抜く。**職業切替が探索とアクションのリズムそのものになる**。

1 つのメカニクスで、戦闘の深み・探索の鍵・物語の切り口を同時に成立させる — これが設計の核。

---

## Player Experience Analysis (MDA Framework)

### Target Aesthetics（プレイヤーが何を感じるか）

| Aesthetic | Priority | How We Deliver It |
| ---- | ---- | ---- |
| **Discovery**（探索・発見） | **1 (Primary)** | メトロイドヴァニアの構造、職業で開くゲート、隠しエリア、環境から次の鍵を示唆する視覚ヒント |
| **Challenge**（障害・熟達） | **2** | 切替コンボの上達曲線、ボス戦での判断力、職業の組合せパズル |
| **Sensation**（感覚刺激） | **3** | 爽快なヒット感、切替の"花"演出、色彩の爆発、2Dでの気持ちいい機動力 |
| **Narrative**（物語） | **4 (Supporting)** | 軽いフレーバーテキスト、Unityちゃん視点の発見メモ、世界観理解のための最小限の説明 |
| **Fantasy**（役割没入） | Supporting | 万能冒険者としてのUnityちゃん、職業変身の視覚的説得力 |
| **Expression**（自己表現） | Supporting | コンボルート選択、攻略順の自由、自分流プレイスタイル |
| **Fellowship**（社会性） | N/A | シングルプレイ |
| **Submission**（低ストレス） | N/A | 本作は集中型体験 |

### Key Dynamics（創発する振る舞い）

- **"もう一戦やりたい"**：職業切替コンボが楽しいため、同じ敵でも別コンボを試したくなる
- **"あの扉が気になる"**：次の職業で何が開くかを常に頭に置いたプレイ
- **"組合せを開発する"**：剣→弓→魔の順序や空中切替の組合せをプレイヤー自身が発見
- **"100%への旅"**：マップ探索率・全オーブ取得・隠しエリアへのコンプリート欲
- **"スピードラン"**：職業コンボの最適解を探す上級者の遊び

### Core Mechanics（実装する5つのシステム）

1. **瞬時職業切替システム** — R1/L1 でいつでも切替、1 ボタン即時、戦闘中もOK、切替自体が視覚/聴覚の報酬
2. **職業別アビリティ** — 剣士（近接連撃・壁破壊）、弓士（遠隔射撃・ギミック起動）、魔法使い（範囲攻撃・結界解除）、4 職目（Tier 3 で決定）
3. **メトロイドヴァニアマップ** — 複数ゾーン、シームレス接続、バックトラック誘発、ショートカット解放、セーブポイント（ベンチ方式）
4. **コンボ入力システム** — 先行入力バッファ（4〜6 フレーム）、空中キャンセル、職業間コンボ受け渡し
5. **発見フィードバック** — 新オーブ取得時の特別演出、マップ更新、環境への色彩変化

---

## Player Motivation Profile

### Primary Psychological Needs Served

| Need | How This Game Satisfies It | Strength |
| ---- | ---- | ---- |
| **Autonomy** | 職業取得順のある程度の自由、同じ壁に複数の解法、コンボを自分流にビルド、100% 達成ルート選択 | **Core** |
| **Competence** | 切替コンボの熟達曲線、ボス戦で判断力が磨かれる実感、探索で世界の全貌が見えていく | **Core** |
| **Relatedness** | 職業オーブに紐づく軽い先人ロア、Unityちゃんが拠点 NPC と軽く交流、世界の記憶を取り戻す感覚 | **Supporting** |

### Player Type Appeal (Bartle Taxonomy)

- [x] **Explorers**（探索・理解・秘密発見） — **Core**：メトロイドヴァニア構造、隠し通路、環境ヒント読解
- [x] **Achievers**（目標達成・収集・進行） — **Core**：ボス撃破、マップ100%、全オーブ収集、コンボマスター
- [x] **Killers/Competitors**（腕の成長・挑戦） — **Supporting**：爽快戦闘、自分の腕が伸びる実感、スピードラン文化への間接的訴求
- [ ] **Socializers** — Minimal（シングルプレイ、マルチなし、SNS共有は外部機能に委ねる）

### Flow State Design

- **Onboarding curve**：最初の 10 分で「走る・ジャンプ・攻撃・切替」の 4 動作を覚え、切替で何かが起こる最初の体験を提供
- **Difficulty scaling**：ゾーン 1 は単一職業で突破可能、ゾーン 2 から 2 職業併用が要求、ゾーン 3 で 3 職業のコンボを自然に学ぶ設計
- **Feedback clarity**：ヒットストップ、エフェクト、SE、スコア表示（ダメージ数）で「自分が何をしたか」が常に明確
- **Recovery from failure**：死亡時は最寄りのベンチに戻るだけ（リソース喪失なし／軽微）、アイテム全没収のソウルライクにはしない

---

## Core Loop

### Moment-to-Moment（30 秒ループ）

```
敵発見 → 接近（職業A: 剣士で打ち上げ）→ 空中で職業Bに瞬時切替
       → 追撃コンボ（職業B: 弓士で落下中の敵を撃ち落とし）
       → 落ち際で職業Cに切替（魔法使い: 範囲魔法で周囲の敵も巻き込む）
       → 爽快一段落、次の部屋へ
```

ドーパミン源：ヒット感 × 切替の華 × コンボ完了。**この 30 秒で失敗した時点でゲーム全体が失敗**するほど、核となる体験。

### Short-Term（5〜15 分ループ）

```
新エリア入室 → ザコ掃討でリソース回収 → 探索で新オーブ/ショートカット発見
             → "この先は別職業が要りそう" という発見 → 考える → 試す → 解ける快感
             → 一つ先の部屋へ
```

「もう 1 部屋」心理の引力：**「次のオーブで何ができるか」**。

### Session-Level（30〜120 分ループ）

```
新ゾーン進出 → ボス戦（全職業の判断力テスト）→ 新オーブ獲得
           → 既存エリアに戻る（バックトラック）→ 以前届かなかった場所へ到達
           → 物語の断片 / 隠しルート発見 → セーブ → 次ゾーン
```

自然な区切り：ボス撃破後、新オーブ獲得後、ベンチでのセーブ時。

### Long-Term Progression（日〜週）

```
職業 3〜4 種類の完全アンロック → 各職業のスキル強化（小規模）
                        → マップ 100% 回収 → 主筋クリア（4-6 時間）
                        → 100% エンディング / 真エンディング（8-10 時間）
```

### Retention Hooks

- **Curiosity**：「あの扉は◯◯オーブなら開く？」「マップの隅にある点は？」「ボスが落とした謎のメモは？」
- **Investment**：発見したエリアの思い出、自分で開発したコンボルート、100% 直前の達成感
- **Social**：シングルゲームだが、スピードランやチャレンジの外部共有文化に乗れる（YouTube/ Twitch 向き）
- **Mastery**：切替コンボの熟達、全ボスノーダメージチャレンジ、タイムアタック

---

## Game Pillars

### Pillar 1: 「切替が、花になる」(Class Switch as Flourish)

職業切替は戦闘・探索どちらでも **1 ボタン即時**、切替そのものが視覚・聴覚の報酬になる。

*Design test*：「切替を遅らせる／コスト化する」提案が出たら、このピラーは NO を言う。

### Pillar 2: 「一歩ごとに、次の鍵が見える」(Every Step Hints the Next Orb)

プレイヤーはいつ止めても **"次にどのオーブで何が開くか"** が頭にある状態を維持できる。オーブには軽いフレーバーテキスト（Unityちゃんの発見メモ風）が付き、世界観理解を補助するが、**物語の主役はあくまで Unityちゃんの現在の冒険**。

*Design test (複合)*：
- 1 時間プレイして「次の目標が思い浮かばない」瞬間があれば、そのエリアは失格。
- フレーバーテキストが目標理解の邪魔をしたら失格（発見体験を物語が邪魔しない）。

### Pillar 3: 「可愛いけど、歯ごたえある」(Cute and Crunchy)

Unityちゃん／AI 生成キャラのかわいさを保ちつつ、戦闘のヒット感・重みで"硬さ"を持たせる。

*Design test (ケース分岐)*：
- **戦闘ヒット感・打撃感** → 爽快さ優先（重くする）
- **移動 / ジャンプ / 待機ポーズ / 勝利演出 / 表情** → かわいさ優先（軽くする、跳ねさせる）
- 判断不能なケース → 「モーションは重く、表情と SE は軽く」で両立

### Pillar 4: 「1 職業で深く、全部でもっと深く」(Deep Alone, Deeper Together)

3〜4 職業はそれぞれ単独でも遊びが成立するが、組合せで新しい可能性が爆発する。

*Design test*：「新職業を 5 つ目として追加」vs「既存 4 職業のコンボ拡張」で迷ったら **後者**。

### Anti-Pillars（本作は "これを" やらない）

- **NOT ローグライク周回ベース**：本作は 1 本通しの物語・探索体験。ランダム生成や死亡リセットで進行を壊さない。Story & Discovery の一貫性を守るため。
- **NOT 複雑なビルド/装備最適化ゲーム**：巨大スキルツリー・装備構築を主軸にしない。コンボそのものが遊び。爽快感を UI/計算の複雑さで薄めないため。
- **NOT 農業サイクルを主軸**：スローライフ/生産サイクルをゲームプレイの主軸にしない（拠点 NPC 対話・休憩・軽いフレーバー獲得は Story 表現手段として可）。Months scope で戦闘・探索の完成度集中のため。
- **NOT マルチプレイ/ネットワーク機能**：ソロ体験として完成。Months scope と実装コストの両立不可。
- **NOT ソウルライク難易度**：参照 DNA（メープル/白猫/原神）は全て爽快アクション帯。Pillar 3 の"歯ごたえ"は **操作レスポンスの重量感** であって **失敗コスト** ではない。

---

## Inspiration and References

| Reference | What We Take From It | What We Do Differently | Why It Matters |
| ---- | ---- | ---- | ---- |
| *MapleStory*（メープルストーリー） | 2D 横スクロール ARPG の爽快な打撃感、職業差別化の哲学 | シングルプレイ、職業切替（MMO ではない） | ユーザー最大の愛着タイトル、体験の DNA |
| *原神* | 発見時の"wow"モーメント演出、キャラクター切替による戦術多様性 | 2D、オープンワールドではない | Discovery ピラーの模範 |
| *白猫プロジェクト* | キャラの滑らかな操作感、タッチ的直感 UI | PC、瞬時切替 | 操作感のリファレンス |
| *ルーンファクトリー* | 戻ってくる理由、キャラクター温かさ、拠点感 | 農業サイクルは採用しない | 拠点 NPC 対話の参考 |
| *Mega Man X* | 武器切替アクション、アビリティでのゲート開放 | 職業として層化、アニメ調、短期クリア | ユニークフックの直接の起源 |
| *Hollow Knight* | メトロイドヴァニア設計、ベンチセーブ、環境ストーリーテリング | 明るく爽快なトーン、簡潔な物語 | マップ設計・セーブ設計の模範 |
| *Ori and the Blind Forest* | 2D 美麗、感情的な操作感、視覚効果 | ポップ・ダンジョン系寄り、物語軽量 | アートの野心値 |

**Non-game inspirations**: 日本の RPG ライトノベル風プロット、浮世絵のレイヤード色面構成（Direction C からの吸収要素）、京都アニメーションのキャラクター表現（表情の可愛さ ↔ アクションの硬さ両立）。

---

## Target Player Profile

| Attribute | Detail |
| ---- | ---- |
| **Age range** | 18〜40 歳（コアは 20 代〜30 代） |
| **Gaming experience** | Mid-core 〜 Hardcore（メトロイドヴァニア or 2D ARPG 経験者） |
| **Time availability** | 平日 30〜60 分、週末 2 時間以上 |
| **Platform preference** | PC（Steam 主力）、Steam Deck 対応歓迎 |
| **Current games they play** | *Hollow Knight*、*MapleStory*、*原神*、*Mega Man X Legacy*、*Ori*、*Dead Cells* |
| **What they're looking for** | 「職業切替の深い 2D アクション」という未充足のジャンル。コンテンツ量ではなく、**操作感と設計の深さ** で勝負するインディ |
| **What would turn them away** | ソウルライク難易度への逸脱、AI アート品質が統一されていない印象、マルチプレイ前提、F2P/課金モデル |

---

## Technical Considerations

| Consideration | Assessment |
| ---- | ---- |
| **Recommended Engine** | **Unity 6.3 LTS (6000.3.x)** — ユーザー Unity 経験あり、Unityちゃん本家、URP 2D が本作の 2D メトロイドヴァニアに最適（`docs/engine-reference/unity/VERSION.md` で pin、`/architecture-review 2026-04-27` で revision） |
| **Render Pipeline** | **Universal RP 2D Renderer** — 2D Lights / Shadows / Sprite Masking 標準搭載 |
| **Input** | Input System 1.8+（Steam Input 対応、Action Rebinding UI） |
| **Camera** | Cinemachine 3（2D Confiner Extension、CinemachineCamera 新 API） |
| **Animation** | **2D Animation 10+（Skeletal）+ PSD Importer** — Art Pipeline 案C Hybrid に最適 |
| **Physics** | Unity 2D + **Kinematic Rigidbody2D + 自作 CharacterController2D**（メトロイドヴァニアでの挙動精度のため） |
| **Tilemap** | Unity 2D Tilemap + Tilemap Extras（Rule Tile、Animated Tile） |
| **Asset Management** | Addressables 2.0（Scene 単位の動的ロード、AI 生成アセットのバリアント管理） |
| **Serialization** | Newtonsoft.Json for Unity（公式）+ Steam Cloud（Steamworks.NET `ISteamRemoteStorage`） |
| **Key Technical Challenges** | (1) 職業切替 1 フレーム同期 — 解決可能（ScriptableObject + SpriteLibrary で 0.7-0.8ms / 切替、ADR-0001 Performance Implications 参照、`/architecture-review 2026-04-27` で revision）/ (2) AI 生成スプライト一貫性 — 案C Hybrid で根本的に回避 / (3) UCL 2.0 の衣装改変可否 — Unity Japan への事前照会必須 |
| **Art Style** | **2D Skeletal（Unityちゃん公式ベース）+ AI 生成武器/衣装/VFX 差分（案C Hybrid）** |
| **Art Pipeline Complexity** | Medium — 公式素材で骨格、AI 生成で装飾差分。パイプライン確立 1-2 日 |
| **Audio Needs** | Moderate — Unity 標準 AudioSource + AudioMixer で十分（Wwise/FMOD はスコープ外）、SE プール化、BGM はゾーン単位、戦闘/探索で AudioMixer Snapshot 切替 |
| **Networking** | None（シングルプレイ） |
| **Content Volume** | Tier 2: 3 ゾーン / 部屋 15-20 / 3 職業 / ボス 2 体 / プレイ時間 2-3 時間（Demo）〜 Early Access 5-6 時間 |
| **Procedural Systems** | なし（全ハンドクラフト、Anti-Pillar 1 により） |

### AI コンテンツ利用の注意

- Steam パートナー登録時、**AI Generated Content disclosure form** の提出が必要（2024-2025 以降のポリシー）
- ChatGPT / AI 生成資産の使用ログをプロジェクト開始時から残す
- AI 生成はキャラ装飾・UI・背景タイル・VFX リファレンスに限定（案C Hybrid の範囲）

---

## Visual Identity Anchor

> 本セクションは Art Bible の種。アートアセット制作すべての判断基準。

### Direction: **「ポップ・ダンジョン + オーブ・ルミナ」Hybrid**

Direction B（ポップ・ダンジョン、MapleStory 系の太縁取り × 高彩度）をベースに、Direction A（オーブ・ルミナ、職業オーブが世界を照らす）の "**切替時に画面が新オーブ色に一瞬染まる**" 演出を吸収したハイブリッド。

### One-Line Visual Rule

> **"Every sprite must feel like it jumped out of a sticker sheet — and on class switch, the whole screen briefly bathes in the new orb's color."**

（すべてのスプライトはステッカーシートから飛び出てきたような存在感を持ち、職業切替時には画面全体が一瞬新オーブの色に染まる）

### Supporting Principles

#### Principle 1: Silhouette-First Character Design（シルエット優先）
- 原則：各職業は **3 秒以内にシルエットだけで判別可能**
- Design Test：キャラをソリッドブラックで塗りつぶした状態で職業が判別できるか？
- 剣士 = 大剣を背負う台形、魔法使い = 三角帽子 + ローブの逆三角、弓士 = S 字曲線の細身

#### Principle 2: High-Key Chromatic Contrast（ハイキー彩度コントラスト）
- 原則：隣接要素の色相差は **最低 30°**（MapleStory 法則）、補色・分裂補色配色を積極採用
- Design Test：HUD の職業アイコンと背景の間に視認性のある色相差があるか？

#### Principle 3: Class-Switch Color Wash（切替時の画面色彩爆発、Direction A から吸収）
- 原則：切替瞬間、画面四隅から内側へ **0.1〜0.2 秒の放射状グラデーション** が新オーブ色で広がってフェードアウト
- Design Test：切替を連打したとき、画面の色が混ざって見えるのではなく **リズムとして読める** か？
- Pillar 1「切替が、花になる」の視覚的主役

### Color Philosophy

**ベースパレット（環境）**：
- Sky Ivory: `#F9F5E7` (HSL: 41°, 77%, 94%) — UI 背景・メニュー
- Parchment: `#E8D5B7` (HSL: 35°, 59%, 82%) — プラットフォーム・壁
- Moss Stone: `#8FAF5F` (HSL: 86°, 31%, 53%) — 地面タイル・植生

**職業識別カラー**：
- Swordmaster Red: `#E63946` (HSL: 356°, 77%, 55%) — 力・情熱・前衛戦士
- Mage Cobalt: `#457B9D` (HSL: 205°, 40%, 44%) — 知性・冷静・神秘
- Archer Amber: `#E9C46A` (HSL: 42°, 77%, 67%) — 俊敏・自然・遠隔
- Class-Switch Flash: `#FFFFFF` 縁取りパーティクル爆発 + 職業色放射

### パイプライン互換性

- **Unityちゃん公式素材（オレンジ髪・水色アクセント）との親和性**：Mage Cobalt と補色関係で視認性良好
- **AI 生成スプライトのスタイル漂流対策**：**統一縁取り後処理**（黒縁 2-3px）で表面的不統一を吸収
- **UCL 2.0 との親和性**：ベースキャラのオレンジ髪・水色アクセントはそのまま活かし、衣装オーバーレイのみで職業差を出す（改変範囲最小化）

### 解像度・アニメ仕様

| カテゴリ | 解像度 | アニメ FPS |
|---|---|---|
| キャラクター | 128×128 px/フレーム | 8〜12 fps |
| 敵キャラ | 96×96 px/フレーム | 8 fps |
| 背景タイル | 64×64 px | N/A |
| UI 要素 | 基準 48px → スケール | N/A |

---

## Risks and Open Questions

### Design Risks

- **R-D1**：職業が "鍵" に収束して Story 層が後景に退くリスク → Pillar 2 の Design Test で継続監視、フレーバーテキストは各オーブに必ず付与
- **R-D2**：コンボ深度の単調化 → Tier 1 プレイテストで多様性確認、必要なら空中キャンセル/ダッシュキャンセル等を追加
- **R-D3**：4 職業のバランス崩壊で片方依存になる → 各ゾーンで職業の「得意領域」を明確化、万能職業を作らない

### Technical Risks

- **R-T1**：AI 生成スプライト一貫性（Critical）→ **案C Hybrid で根本回避**、モーションは Unity 公式 2D Animation で骨格アニメ
- **R-T2**：UCL 2.0 の衣装大幅改変可否（High）→ **Unity Japan への事前照会必須**、Sprite Library レイヤー分離で万一の差替コスト最小化
- **R-T3**：職業切替 1 フレーム同期（Low）→ ScriptableObject + SpriteLibrary + VFX プール化で解決可能、ADR-0001 Performance Implications で 0.7-0.8ms / 切替（cold path 1.0ms 許容）として budget 確定（`/architecture-review 2026-04-27` で revision、原典 ~0.4ms 見積から ADR 整合）
- **R-T4**：メトロイドヴァニア向けマップ設計（Medium）→ Tilemap + Cinemachine Confiner + Additive Scene Loading + ScriptableObject RuntimeSet で対応
- **R-T5**：セーブ互換性（Low）→ `schemaVersion` + マイグレーションチェーン、`.bak` 1 世代バックアップ
- **R-T6**：Steam AI 開示ポリシー → プロジェクト開始時から AI 使用ログ記録、申請フローを Tier 2b EA リリースまでに整備

### Market Risks

- **R-M1**：メトロイドヴァニア市場の競争激化（*Hollow Knight: Silksong* 等の大物が控える）→ 差別化点は「職業切替コンボ × 数ヶ月で遊べる短さ」
- **R-M2**：AI アートへの反感（プレイヤー層の一部）→ 透明性（AI 使用開示）＋ 手作業ポリッシュ強調
- **R-M3**：日本市場（Unityちゃん IP）vs 海外市場（MapleStory はアジア中心）→ 英日バイリンガル対応を Tier 2 で実装

### Scope Risks

- **R-S1**：ソロ × プロ × 数ヶ月 × 4 職業メトロイドヴァニアは市場ベンチマーク比 2〜3 倍速想定（PR 指摘）→ **Tier 2 を 8-10 ヶ月に修正済み**、Tier 2 は 3 職業 3 ゾーンに絞り、4 職目は Tier 3 送り
- **R-S2**：Metroidvania 特有の "マップ進行設計" 工数が見積もりに含まれていない（PR 指摘）→ 総工数の 20-30% を明示的に確保
- **R-S3**：AI 生成の実コスト（一貫性手直し・失敗生成・後処理）が見積の 3-5 倍（PR 指摘）→ 案C Hybrid で AI の適用範囲を絞り、隠れコストを削減
- **R-S4**：Tier 1 時点で Go/Pivot/Stop の判断基準が必要（PR 推奨）→ 3-4 ヶ月経過時点で想定進捗 60% 未満なら scope 削減を強制するルールを設定

### Open Questions

- **Q1**：4 職目は何にするか？ → Tier 2 Early Access 後のコミュニティフィードバックを反映して Tier 3 で決定
- **Q2**：UCL 2.0 で職業コスチューム化は許容されるか？ → Unity Japan への公式照会で解消
- **Q3**：BGM は自作・フリー素材・外注のどれで調達するか？ → Tier 1 プレイテスト時に仮BGM で検証、Tier 2 で決定
- **Q4**：Steam Deck 検証はいつ行うか？ → Tier 2a Demo 時点で Verified 申請を検討

---

## MVP Definition

**Core hypothesis**：**「職業を瞬時に切替えるコンボが爽快で、プレイヤーが "もう一戦やりたい" と感じるか」**

**Required for MVP (Tier 0)**：
1. 2 職業（剣士 + 弓士）の基本アクション＋切替動作
2. 職業切替の「花演出」最小実装（SE + VFX + スプライト入替）
3. 3 部屋からなる 1 ゾーン（入室→戦闘→探索→次の部屋）
4. 敵 2〜3 種（基本挙動のみ）
5. Unity 6 LTS + URP 2D + Input System + 2D Animation の環境構築
6. 切替アーキテクチャのスパイク（ScriptableObject + ClassStateMachine の抽象化）

**Explicitly NOT in MVP（Tier 0 非対象）**：
- ボス戦、UI（タイトル・メニュー・設定）、セーブ/ロード、オーブ取得演出、フレーバーテキスト、音楽、マップ
- 3 職目以降、2 ゾーン目以降、ストーリー要素、拠点 NPC

### Scope Tiers（段階リリース戦略 — PR 推奨版）

| Tier | Content | Features | Timeline（solo）|
| ---- | ---- | ---- | ---- |
| **Tier 0 MVP** | 1 ゾーン / 3 部屋 / 2 職業 / 敵 2-3 種 | コアループ＋切替動作＋切替演出 | **3-4 週間** |
| **Tier 1 Vertical Slice** | 2 ゾーン / 部屋 8-10 / 3 職業 / ボス 1 / 軽い UI | コアループ完成＋1 ボス／フレーバー 3-5 個 | **3-4 ヶ月** |
| **Tier 2a Steam Next Fest Demo** | 2 ゾーン / 部屋 10-12 / 3 職業 / ボス 1-2 / プレイ時間 60-90 分 | タイトル・セーブ・設定・リバインド UI | **6-8 ヶ月**（累計） |
| **Tier 2b Early Access 発売** | 3 ゾーン / 部屋 15-20 / 3 職業 / ボス 2 / プレイ時間 2-3 時間 | Steam 配信、Cloud Save、実績 | **9-12 ヶ月**（累計） |
| **Tier 3 Full Release** | 4 ゾーン / 部屋 30+ / **4 職業** / ボス 3-4 / プレイ時間 4-6 時間クリア、100% 8-10 時間 | エンディング、隠しエリア、真エンディング | **18-24 ヶ月**（累計） |

### Go/Pivot/Stop 判断ゲート（PR 推奨）

- **Tier 0 終了時（3-4 週）**：切替コンボが面白いか自己検証。面白くなければ Pivot または Stop。
- **Tier 1 終了時（3-4 ヶ月）**：外部テスター 5 名で "もう少し遊びたい" が引き出せるか。引き出せなければ scope 削減または Pivot。
- **Tier 2a Demo 終了時（6-8 ヶ月）**：Steam Next Fest での wishlist 獲得数で Early Access 発売判断。
- **Tier 2b EA 発売時（9-12 ヶ月）**：コミュニティフィードバックで Tier 3 の 4 職目と隠し要素の方向性決定。

---

## Next Steps

本コンセプトが Phase 2 以降で拡張されていく流れ：

- [ ] **設計承認・洗練**：creative-director による最終承認（本ドキュメントで CD-PILLARS / AD-CONCEPT-VISUAL / TD-FEASIBILITY / PR-SCOPE を通過済み、ただし全て CONCERNS 以下なので条件付き）
- [ ] **`/setup-engine`**：Unity 6 LTS を pin、`.claude/docs/technical-preferences.md` を更新、Godot pin を Unity pin に置換
- [ ] **`/art-bible`**：Visual Identity Anchor をベースに、アセット仕様・AI 生成プロンプト規約・UCL 遵守ガイドを整備（**Unity Japan への UCL 2.0 照会はこの時点で並行実施**）
- [ ] **`/design-review design/gdd/game-concept.md`**：本コンセプトの完全性検証（Full モードの director レビュー対象）
- [ ] **`/map-systems`**：職業切替システム／メトロイドヴァニアマップ／戦闘／セーブ等への分解、依存マッピング、GDD 作成順の決定
- [ ] **`/design-system [system]` ×N**：各システムの詳細 GDD 作成（最優先：職業切替、次に戦闘、次にマップ/探索）
- [ ] **`/create-architecture`**：マスター設計書 + Required ADR リスト生成
- [ ] **`/architecture-decision` ×N**：Unity バージョン、2D Animation vs Sprite Sheet、セーブ設計、等を ADR 化
- [ ] **`/gate-check pre-production`**：プロトタイプ着手前のフェーズゲート通過
- [ ] **`/prototype class-switch`**：Tier 0 MVP の実装（切替アーキテクチャのスパイク含む）
- [ ] **`/playtest-report`**：Tier 0 終了時のセルフプレイ記録＋Go/Pivot/Stop 判断
- [ ] **`/sprint-plan new`**：Tier 1 Vertical Slice のスプリント計画
