# DropInHeroes — Guia de Documentação para LLMs

> **Ponto de entrada.** Leia este arquivo primeiro em qualquer sessão de IA (ou onboarding de dev) que vá ler ou alterar este projeto. Ele resume o jogo, os padrões transversais **obrigatórios**, o glossário de domínio, um **snapshot verificado** do estado atual, e o **índice** dos documentos aprofundados.
>
> Prosa em PT-BR; identificadores, tipos e caminhos em inglês (convenção do projeto). Ver também `CLAUDE.md` na raiz (regras de operação para alterações de código).

---

## Como usar este guia

> **⚠️ Antes de qualquer sessão de trabalho, leia também `Docs/heuristicas-de-projeto.md`** — normas de operação, invariantes que quebram o build, regras de uso do Unity MCP e critérios de "pronto". Elas valem em toda fase e não estão repetidas aqui.
>
> **Para saber em que fase o projeto está e o que já foi decidido:** `Docs/design/roadmap.md`.

- **Só precisa de contexto rápido?** Leia §1–§2 (jogo + loops) e §5 (glossário).
- **Vai alterar código?** Leia §4 (padrões transversais — inclui footguns que quebram o build) e o doc **P** relevante (§7) antes de tocar qualquer coisa.
- **Vai criar conteúdo (personagem, skill, passiva, item)?** Comece por **P1 · Composição de conteúdo de combate** — é o padrão central que mantém tudo geral.
- **Está confiando em uma afirmação de estado (assets, Resources, roster)?** Confira o §6 (snapshot verificado) e a data. O código muda; re-verifique via Unity MCP se a data estiver velha.

---

## 1. O que é o jogo

**DropInHeroes** é um **auto-battler 2D** com uma camada meta **roguelite de "Tower run"**. O jogador monta um time de heróis, depois escala uma _ladder_ fixa de fases inimigas. Cada fase é o ciclo:

**Preparação** (comprar/upar um herói numa loja, arrastar heróis de uma _bench_ para um _board_ definido por tilemap) → **Batalha** (auto-resolvida: unidades miram, movem-se via NavMesh, usam ataque base / supreme / passiva **sem input por turno**) → **Resolução** (quem perde a fase perde 1 de HP; player vs. torre).

Unidades são **data-driven** a partir de `CharacterData` (ScriptableObject: 25 stats, 3 slots de skill, tags e capabilities em flags). A batalha termina quando as unidades **não-summon** de um time são zeradas, ou por _timeout_ (resolvido por _tiebreaker_). O run acaba em vitória/derrota quando a ladder se esgota ou um dos HPs de fase chega a 0.

**Gênero:** auto-battler / auto-chess 2D com estrutura de run roguelite (draft de time, posicionamento em board, economia de loja, upgrade de _rank_).

**Fantasia:** um "técnico/drafter" escalando um esquadrão de heróis peculiares (Guliver, Marda, Rikurby, Darulito, Hami) cujas passivas distintas — tanques que compartilham dano, atacantes que escalam com debuff, brutamontes que invocam, casters de totem que se curam — criam sinergias emergentes, com os "Supremes" carregados por energia como o clímax momento-a-momento.

---

## 2. Loops centrais

1. **Loop de combate** (por unidade, por frame): `CombatModule` FSM → `LookingForTarget` (`FocusModule` resolve alvo: taunt > estratégia > mais próximo) → `MovementModule.MoveTo` (NavMesh) → `Attacking` (`SkillsModule.ExecuteSkill` toca a anim e retorna duração) → impacto num **Animation Event** `OnAttackHitFrame` (`VisualModule.OnAttackHit` → `ActiveSkill.OnHit` da skill modular aplica dano/cura/status) → energia regenera até encher → **Supreme** preempta o ataque base → repete até `Dead`/`Victory`.
2. **Loop de fase** (Tower run, por fase da ladder): `RunPrepPhase` (respawn da última formação, loja, drag bench→board, confirmar/timeout) → `RunBattlePhase` (snapshot da formação, spawn de inimigos, `CombatController.StartCombat`, aguarda `OnCombatEnded` ou _tiebreak_ por timer) → `ResolvePhase` (decrementa HP do perdedor, devolve unidades ao pool, reseta câmera/UI) → próxima fase.
3. **Loop de run/meta** (jogo inteiro): `MainMenu` → `TowerPicks` (draft de heróis → `TowerRunData`) → `Loading` (`DataManager` inicializa catálogo) → `TowerRun` (Prep+Loja → Batalha → Resolução) × `PhaseCount`, condicionado a `playerHP>0 && towerHP>0` → fim (Vitória/Derrota). Bench/rank persistem entre fases; `UnitController` **não** (pooled + re-spawnado).

---

## 3. Mapa subsistema → pasta

| Pasta | Responsabilidade | Ver |
|---|---|---|
| `Assets/Scripts/Core/` | Boot: cena de Loading, `DataManager` handoff, `SceneNames` | D2 |
| `Assets/Scripts/Data/` | Camada data-driven: `DataManager`, `GameDataCatalog`, `DataRegistry<T>`, `CharacterData` | P5 |
| `Assets/Scripts/Combat/` | Hub `CombatController` (orquestra a batalha) | D3 |
| `Assets/Scripts/Combat/Unit/` | `UnitController` + `IUnitModule` (12 módulos), pooling, config, tags/capabilities | P6, P2 |
| `Assets/Scripts/Combat/Unit/Stats/` | `Stat`, `Resource`, `StatType`, `StatDefinitionCatalog` | P3 |
| `Assets/Scripts/Combat/Unit/Skills/` | 3 slots; bases `ActiveSkill`/`PassiveSkill` + `Core/Modular*`; `Modules/` (`SkillModules`+`Targets`); `Characters/<Nome>/` (skills finas SO-thin); `Hooks/PassiveHooks`; `SkillContext` | **P1**, P4 |
| `Assets/Scripts/Combat/Damage/` | `DamageCalculator`, `HealCalculator`, `TypeAdvantage`, `Scaling`, effect chance | P1, D3 |
| `Assets/Scripts/Combat/Effects/` | `VfxPlayer`, `ClipPlayer`, `CombatAura`, contact/skill effects | P1 |
| `Assets/Scripts/Combat/Projectiles/` | `Projectile`, `ProjectilePool` | P1 |
| `Assets/Scripts/Combat/Targeting/` | `TargetQuery` (side/filter/criterion/shape), `TargetSelection` | P1 |
| `Assets/Scripts/Combat/Rules/` | `CombatItem` → `CombatRule` → `CombatRulesRegistry` | P3 |
| `Assets/Scripts/Combat/Preparation/` | Board, drag/snap/validate/swap, `PreparationConfig` | D5 |
| `Assets/Scripts/Tower/` | Meta run: fases, bench, loja, rank, ladder, timers, tiebreak | D4 |
| `Assets/Scripts/UI/` | Telas (MainMenu, Loading), sistema de ability-text/tooltip | P5 |
| `Assets/Scripts/Utils/Debug/` | `DebugManager` / `DebugConfig` (log por categoria) | P6 |
| `Assets/Data/` | Todos os `.asset` de config (personagens, tower, catálogos) | P5 |
| `Assets/Art/`, `Assets/Animations/` | Arte e clipes de animação por personagem | pipeline-personagens.md |

---

## 4. Padrões transversais (obrigatórios)

Estes padrões atravessam todos os subsistemas. **Alguns são load-bearing — quebrá-los quebra o build ou o runtime silenciosamente.** Cada um tem um doc **P** de aprofundamento (§7).

1. **Composição > lógica bespoke** (**→ P1**). **Skills/passivas** = classe fina `Modular*` compondo `SkillModules.*` (dano→`Damage`, cura→`Heal`, status→`Status`, escudo→`Shield`…) sobre `Targets.*`, em CÓDIGO; o `.asset` só números (SO-thin: "muda O QUE→código, muda QUANTO→SO"). **Artefatos** (`ArtifactData`, `Data/Items/`) ainda compõem `SkillEffect` (`DealDamageEffect`/`GrantShieldEffect`/…) via `TargetQuery`. Código/módulo novo só quando o comportamento é genuinamente único — e sempre genérico/reutilizável.
2. **Composição de módulos > herança** (**→ P6**). `UnitController` é um hub fino com `IUnitModule` (classes C# puras, **não** components) indexadas por `Type`, `new`adas no `Awake` em ordem de dependência; cross-refs cacheadas no `Initialize`, nunca por frame.
3. **⚠️ Namespaces BLOCK-scoped obrigatórios (`DropInHeroes.*`)** (**→ P6**). Namespace _file-scoped_ **quebra silenciosamente** o importador de scripts do Unity 6000.4.x (vira "missing script"). Imposto por `.editorconfig` + `Assets/csc.rsp`. Exceção: `Tower/BenchEntry.cs` está no namespace **global**.
4. **Config data-driven como ScriptableObject** (**→ P5**) com `[CreateAssetMenu]` sob `menuName "Game/..."`. Designers editam `.asset`; código lê. Nada de balanceamento hardcoded.
5. **Comportamento por flags, sem branch por tipo** (**→ P2**). `UnitCapability` (Move/Attack/UseSupreme/GainEnergy) e `UnitTag` (Hero/Summon/Totem/Boss) gate participação, energia, contagem de vitória, matching de rule e filtros. Summons/totens reduzidos funcionam com **zero código novo**.
6. **Stats/dano alterados por modifier id-keyed reversível** (**→ P3**). `AddModifier`/`ModifyStat`/`CombatRule` com id; nunca valor cru. Reversão limpa por id.
7. **Passivas via hooks ou registro de modifier** (**→ P4**). `PassiveHooks` (event bus: onDamageTaken/Dealt, onEnergyFull…) ou registro id-keyed; ciclo `Initialize`/`Clear`/`Deactivate`/`Reactivate`; ids GUID-suffixed.
8. **Pooling em vez de `Instantiate`/`Destroy` em runtime** (**→ P6**). `UnitPool`, `ProjectilePool`, host de `VfxPlayer`. `OnDestroy` é só _scene-unload_.
9. **`async/await` + `TaskCompletionSource`** para orquestração (boot, run, gate de prep), **não coroutines**. `CancellationToken` propagado.
10. **⚠️ `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)] ResetStatics`** em toda classe com estado estático. Load-bearing: sem isso, Play com _domain reload_ desabilitado retém estado velho.
11. **⚠️ Binding por reflexão baseado em convenção (cacheado 1×)** (**→ P3/P5**). `CharacterData.GetBaseStat(StatType.X)` → campo `baseX`; ability-text `{v:field}` lê por reflexão. **Retornam 0 silenciosamente** num mismatch de nome. Renomear stat exige renomear o campo `baseX`.
12. **Regras de hot-path (ver `CLAUDE.md`)** (**→ P6**). Sem alloc/LINQ/boxing por frame; buffers `List` reutilizáveis do caller; `sqrMagnitude`; `StringToHash` cacheado; reflexão cacheada no init.

---

## 5. Glossário de domínio

| Termo | Significado |
|---|---|
| **Supreme** | Ultimate no slot `supremeSkill` (`ActiveSkill`). Carregado por Energia; pronto quando a Energia enche (latch `supremeReady`), preempta o ataque base, zera a Energia ao usar. Só unidades com `UnitCapability.UseSupreme` mostram barra de energia e podem usar. |
| **Focus** | Alvo de combate atual. Resolvido a cada retarget por `FocusModule`: taunt (forçado) > estratégia plugável de maior prioridade > default `Closest`, com fallback de stealth. `IsValidFocus` decide manter-vs-retarget. |
| **Footprint** | O círculo colorido de posicionamento sob cada unidade (`UnitFootprint` + `CircleCollider2D`), gerido por `FootprintModule`. Sua posição-mundo (`GetCurrentWorldPosition`) é a **fonte da verdade** para posicionamento/validação/overlap — **não** o transform da unidade nem o mouse. |
| **Totem** | Summon estacionário (`UnitTag.Totem`) com só a capability `UseSupreme` — não anda nem ataca. O Supreme do Darulito o invoca (cap 1, atrás de si; recast substitui o anterior); o totem regenera energia e **autocasta o próprio supremo** (`TotemBolivianoSupreme`: dano mágico em área + purifica aliados). |
| **Nature / CharacterNature** | Enum em `PassiveSkill` (None/Neutral/Good/Evil) categorizando o alinhamento da passiva. |
| **Type / CharacterType** | Tipo(s) da unidade (None, Fairy, Classic, Dark, Hero, Art, Chaos, Chill, Aura, Order, Revolution, Favela). `CharacterData` tem primary+secondary; `TypeAdvantage` dá +5% de dano por par vantajoso attacker→defender. |
| **Rank** | Nível de upgrade por-run (1..`maxRank`, default 3) em `BenchEntry`, mostrado como `R{rank}`. ⚠️ **Rank NÃO multiplica stats** (o `RankScaler` foi removido) — melhora **só a passiva**, escalada em código via `RankTiers`/`RankScaledStatMod` (arrays de valor por rank). |
| **Ladder / TowerLadderData** | O `TowerPhase[]` ordenado que define a composição inimiga de cada fase (`UnitSpawn[]` de characterId/spawnPosition/rank). `PhaseCount` define a duração do run. |
| **Bench / TowerBench** | Roster por-run de heróis comprados como `BenchEntry` (Character, Rank, DeployedController, LastBoardPosition, WasDeployed). `BuyOrUpgrade` preenche slot vazio em Rank 1 ou upa um existente. |
| **Board / BoardManager** | Conjunto de unidades do player posicionadas na fase (cap `PreparationConfig.maxUnitsOnField`, default 3). Dispara `OnUnitAdded/Removed/BoardFull/BoardAvailable`; `GetAllUnits()` alimenta o roster de combate. |
| **Rule / CombatRule** | Buff/debuff tag-matched e id-reversível (`StatModifierRule`/`DamageModifierRule`) produzido por um `CombatItem` e auto-aplicado às unidades que casam (incluindo summons tardios) por `CombatRulesRegistry`. |
| **On-hit (rider)** | Efeito rodado em **todo** golpe do dono, registrado no `OnHitModule`: por código via `ModularOnHitPassive` (skills — ex.: Hami ganha Attack por acerto) ou por `SkillEffect` de **artefato** (`ArtifactData.onHitEffects`/`summonOnHitEffects`). |
| **SkillEffect** | Bloco de efeito reutilizável como ScriptableObject (`DealDamage`, `Heal`, `GainStat`, `GrantShield`, `RedirectAllyDamage`…) com sua própria `TargetQuery`. **Hoje é o vocabulário dos ARTEFATOS** (`ArtifactData` on-hit/battle-start/summon-on-hit) — as skills de personagem migraram para módulos em código (`SkillModules.*`). Unit-stateless (compartilhado). |
| **TargetQuery** | Seletor serializável e componível = `TargetSide` + `TargetFilter` + `TargetCriterion` + count + shape (`RadiusMode`: None/AroundSelf/AroundTarget/Line). `Resolve()` preenche um buffer do caller; usado igual por skill effects e auto-focus. |
| **PassiveHooks** | Barramento de eventos por-unidade (`onBattleStart/End`, `onDamageTaken/Dealt`, `onHealReceived`, `onBeforeAttack/AfterAttack`, `onEnergyFull`, `onSupremeUsed`, `onUnitDeath`) que acopla passivas hook-style ao ciclo de combate sem polling. |
| **Stat** | Atributo numérico mutável = `(manual + modifiers flat + providers dinâmicos) × max(0, 1 + soma percent)`, com clamp opcional. 25 `StatType` (Attack..Dodge). Modifiers são name/id-keyed para revert limpo. |
| **Resource** | Pool de valor atual (Health, Energy) limitado por um `Stat` de máximo (teto dinâmico). Dispara `OnValueChanged`/`OnDepleted`; `Health.OnDepleted` inicia a pipeline de morte. |
| **Capability / UnitCapability** | `[Flags]` Move/Attack/UseSupreme/GainEnergy/All em `CharacterData`; gate do que a unidade faz sem código por-tipo. |
| **Tag / UnitTag** | `[Flags]` None/Hero/Summon/Totem/Boss em `CharacterData`; dirige contagem de vitória (summons excluídos), matching de rule/item e filtros de targeting. |
| **Summon** | Unidade spawnada em combate via `CombatController.SpawnSummon` (totem do Darulito, LilRih do Rikurby), com dono, stats snapshotados por % do dono. Nunca conta pra vitória; force-killed quando as unidades reais do time morrem. |
| **Damage share** | Redirecionamento passivo (Guliver) onde uma fração do dano de entrada de um aliado é reroteada a um protetor resolvido dinamicamente; só a maior entrada, não-encadeável, ignora dano True. |
| **Phase HP (playerHP / towerHP)** | Os dois pools de HP do run (default 3 cada). Vitória de fase do player decrementa `towerHP`; derrota decrementa `playerHP`. Run acaba quando um chega a 0 ou a ladder se esgota. |
| **Ability text markup** | Rich-text TMP nas descrições de skill: `{kw:key}` (cor/explicação de keyword), `{v:field:Stat:fmt}` (valor numérico por reflexão com fmt pct/pctFrac/flat/sec), `{shift:...}` (detalhe só com SHIFT). |

---

## 6. Snapshot verificado — 2026-07-01 (Unity 6000.4.6f1, via Unity MCP)

Estado atual confirmado contra o editor vivo e o disco. **Re-verifique se esta data estiver velha.**

- **Roster do catálogo (`GameDataCatalog.asset`) = 5 heróis**, todos `UnitTag.Hero`: `darulito`, `guliver`, `marda`, `rikurby`, `hami` (ids em minúsculo).
  - **Todos com `primaryType = Classic`** e `secondaryType = None`. ⇒ o sistema de `TypeAdvantage` existe mas **praticamente não dispara** com os dados atuais (todos do mesmo tipo).
  - Summons (`DarulitoTotem`, `LilRih`) **não** estão no catálogo — são assets referenciados por skills, fora do roster jogável.
  - ⚠️ **DESATUALIZADO pela migração SO-thin (2026-07-09):** hoje **TODAS** as skills são classes finas `Modular*` em `Skills/Characters/<Nome>/` — Hami inclusive (`HamiAttack`/`HamiSupreme`/`HamiPassive`); e o roster cresceu (Pedro, Vela). O formato antigo (`Composite*`, `Aura*`) foi removido. **Re-verifique o roster/estado atual** — este snapshot é de 2026-07-01. Ver memória `so-thin-code-modules`.
- **`Assets/Resources/` contém só 2 assets**: `StatDefinitionCatalog.asset` e `KeywordCatalog.asset`.
  - `Resources.Load` de `DebugConfig`, `PreparationConfig`, `GameDataCatalog` retorna **null** ⇒ esses sistemas fazem **fallback silencioso**: logging vira não-filtrado/plain; `PreparationConfig` usa valores default. (`GameDataCatalog` é referenciado direto no `DataManager`, não via Resources — ok.)
- **Cenas em Build Settings** (habilitadas): `MainMenu`, `Loading`, `TowerPicks`, `TowerRun`. `Test` e `TestBattle` existem mas estão **desabilitadas** (entradas soltas; `SceneNames` não as declara).
- **`UnitController`** compõe **12 módulos**: Visual, Drag, Footprint, Stats, Status, Focus, Skills, Movement, Combat, HealthBar, StatusStrip, EnergyBar.

---

## 7. Índice de documentos

Organizado em **duas famílias**: **Padrões** (como construir, de forma geral) e **Intenções & Design** (o porquê e a direção). A referência detalhada (o que cada classe faz) vive **dentro** desses docs como apoio — não há docs de referência subsistema-a-subsistema separados. **Escritos sob demanda** — peça pelo código (ex.: *"escreve o P3"*) ou tema.

### Tier 1 — Padrões (prescritivo: "o jeito canônico de construir, mantendo tudo geral")

| Cód | Documento | Status | Pergunta que responde |
|---|---|:---:|---|
| **P1** | `patterns/content-composition.md` | ⬜ | Como construir skills/passivas **compondo MÓDULOS em código** (classe fina `Modular*` + `SkillModules.Damage/Heal/Status/Shield…` sobre `Targets.*`; SO-thin); artefatos ainda via `SkillEffect`. Quando compor vs. criar módulo novo vs. bespoke. |
| **P2** | `patterns/behavior-via-flags.md` | ⬜ | Como dar comportamento por `UnitCapability`/`UnitTag` sem branch por personagem (summons/totens/bosses sem código específico). |
| **P3** | `patterns/stats-modifiers-and-rules.md` | ⬜ | Como alterar stat/dano de forma **reversível** (modifier id-keyed, `CombatRule`, `StatusEffect`) — nunca valor cru. |
| **P4** | `patterns/passives-and-hooks.md` | ⬜ | Como escrever passivas via `PassiveHooks` / registro de modifier, e o ciclo de vida `Initialize`/`Clear`/`Deactivate`/`Reactivate`. |
| **P5** | `patterns/data-driven-authoring.md` | ⬜ | Como adicionar conteúdo (personagem/stat/item/config) via **asset + catálogo**, não código; o contrato de reflexão `baseX`. |
| **P6** | `patterns/architecture-principles.md` | ⬜ | Módulos>herança, hubs delegam, pooling, `ResetStatics`, hot-path, namespaces block-scoped — as regras estruturais. |

### Tier 2 — Intenções & Design (o "porquê" e a direção; marcar `[especulação]` no que for inferido/aspiracional)

| Cód | Documento | Status | Pergunta que responde |
|---|---|:---:|---|
| **D0** | `design/roadmap.md` | ✅ | **Para onde o projeto vai.** Fases F0–F7, decisões fechadas e abertas, cronograma, riscos. Ponto de partida de todo plano de fase. |
| **D1** | `design/concept-and-pillars.md` | ⬜ | O que o jogo **quer ser**? Fantasia, pilares, o que é (e o que não é) alvo de design. |
| **D2** | `design/game-loops.md` | ⬜ | Os três loops (combate/fase/run) e a **intenção** por trás de cada um. |
| **D3** | `design/combat.md` | ⬜ | Intenção do combate auto-resolvido + como flui (FSM, energia/supreme, morte, vitória). |
| **D4** | `design/tower-run.md` | ⬜ | Intenção do meta roguelite: progressão, ladder, economia (bench/loja/rank). |
| **D5** | `design/preparation-and-placement.md` | ⬜ | A preparação/board como a principal **expressão de habilidade** do jogador. |
| **D6** | `design/features/` | ⬜ | Features pontuais e sua intenção (ex.: árvore de habilidades, sinergias de tipo) — uma por arquivo. |

**Legenda:** ✅ pronto · ⬜ pendente · `[especulação]` = inferido do código ou visão ainda não implementada (marcado inline no doc).

> Os padrões de §4 são o **resumo**; os docs **P** são o **aprofundamento**. Escreva primeiro os **P** (base para tudo), depois os **D** conforme a visão for sendo definida.

---

## 8. Não duplicar

- **`Docs/heuristicas-de-projeto.md`** (✅ pronto) — **normas de trabalho**: ordem de leitura por sessão, invariantes load-bearing, regras de Unity MCP, organização de código, critérios de "pronto", higiene de repositório. Este README descreve *como o projeto é*; as heurísticas descrevem *como se trabalha nele*. Não repita as invariantes aqui — o §4 é o resumo, as heurísticas são o contrato.
- **`Docs/design/roadmap.md`** (✅ pronto) — **fases, decisões e cronograma**. Qualquer afirmação sobre escopo, prazo ou o que está dentro/fora do projeto vive lá, não aqui.
- **`Docs/pipeline-personagens.md`** (✅ pronto) — pipeline de **arte/animação** de personagem: trocar sprites a partir de `_Zip`, calibrar PPU/pivot, rebuild de clipe, eventos `OnAttackHitFrame`, convenções de rig/facing, e o uso de **Ludo** + **Unity MCP** para gerar/importar assets. Qualquer doc que toque animação/arte **referencia** este, não repete.
