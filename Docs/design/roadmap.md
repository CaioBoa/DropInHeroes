# Roadmap — DropInHeroes

> **O que é este documento.** O plano geral do projeto, do estado atual até o beta fechado. Ele define **fases (F0–F7)**, o que cada uma entrega, e em que ordem. Não é um plano de execução: cada `F.x` ganha seu **plano específico** sob demanda, e é nesse plano que as decisões de implementação são tomadas.
>
> **Como usar.** Antes de pedir um plano de `F.x`, leia: `Docs/README.md` (arquitetura), `Docs/heuristicas-de-projeto.md` (normas que valem em toda sessão), e a fase correspondente aqui. Um plano de `F.x` que contradiga qualquer um dos três está errado.
>
> Última revisão: 2026-08-03. Prazos são estimativa para **um dev solo**, e devem ser recalibrados ao fim de cada fase.

---

## 1. Visão do produto

Auto-battler 2D com preparação estratégica e combate auto-resolvido, jogado entre amigos.

- **PVP** — 2 a 4 jogadores, formato TFT: rodadas pareadas, **combate sempre 1v1**. Último de pé vence.
- **PVE** — versão mais automatizada do mesmo fluxo, com menos input do jogador, usada para farmar progressão.
- **Progressão** — equipamento farmável, nível e ascensão. **Vale no PVP**: o farm existe para alimentá-lo.
- **Distribuição** — beta privado na Steam, 100% free, para amigos e convidados.

O **Tower Run atual é a base do fluxo de jogo** e o protótipo do PVP. Ele não é descartável: `TowerRunController`, `TowerShop` e `TowerBench` evoluem para o orquestrador de rodadas do PVP. Eventualmente ele morre como modo próprio, mas não nesta janela.

---

## 2. Estado verificado do projeto

Auditoria sobre 215 arquivos / 20.030 linhas, Unity 6000.4.6f1, URP 2D.

**Preservar — está bem construído:**
- Módulos de unidade como classes C# puras (`IUnitModule`), não MonoBehaviours
- Caminho único de dano: `DamageApplier.Apply` → `DamageCalculator.Calculate` (estático puro)
- `Stat` com modifiers nomeados flat/percent e fold determinístico
- `TargetQuery` componível e non-alloc, compartilhado entre foco e skills
- Pooling consistente; `UnitSpawn` já serializável como `{characterId, spawnPosition, rank}`

**Não existe:** rede, persistência de conta, testes, áudio, localização, recompensas, inimigos PVE dedicados, asmdefs próprios, uso de Addressables.

**Riscos estruturais imediatos:**
- Repositório não reconstituível: 69.655 arquivos sujos sobre 5 commits, `.git` de 1,2 GB sem LFS, assets críticos untracked
- `StatType` é enum sem valores explícitos serializado como int — inserir um stat no meio reescreve saves
- IDs de conteúdo auto-gerados do nome do asset (`OnValidate`) — estáveis por acidente
- `companyName: DefaultCompany` define o caminho de save
- `EditorBuildSettings` referencia cenas inexistentes (`Test.unity`, `TestBattle.unity`)

---

## 3. Decisões fechadas

| # | Decisão |
|---|---|
| 1 | Jogo **100% free**, sem monetização |
| 2 | **Steam** como identidade, save (Auto-Cloud) e transporte (P2P). **Sem servidor próprio** |
| 3 | Alvo final = **beta privado** para amigos e convidados |
| 4 | **PVP síncrono, 2 a 4 jogadores, formato TFT** — rodadas pareadas, **combate sempre 1v1**, host-autoritativo |
| 5 | **Progressão de conta vale no PVP** |
| 6 | **Skill tree é legado** — builds descartadas, sem migração |
| 7 | **Todo personagem = 100% módulos prontos, zero código novo.** Não existem personagens de assinatura |
| 8 | **Balanceamento automatizado não é necessário** — o beta pode sair desbalanceado |
| 9 | **Roster: 40 a 60 no beta**, definido conforme a produção avança |
| 10 | **Polimento de código e UI/UX são fases reais**, pré-requisito do multiplayer |
| 11 | **Tower Run é a base do PVP** e do fluxo geral; PVE é a variante automatizada |

**Consequências que não podem ser reabertas sem revisitar a decisão de origem:**
- (8) ⇒ o desacoplamento do dano do `AnimationEvent` está **fora do plano**. Perdeu seus três justificadores (netcode determinístico, servidor autoritativo, simulação de balanceamento). Volta apenas se o loop de farm exigir aceleração real.
- (4) ⇒ combate permanece 1v1. O enum `Team` binário **continua correto no nível da batalha**; slots de jogador existem no nível da *partida*.
- (2) ⇒ Steam **não é servidor**: não roda código nem responde query. Acúmulo de farm offline é calculado no cliente a partir de timestamp.

---

## 4. Decisões abertas

| # | Decisão | Prazo para decidir |
|---|---|---|
| A | **Loja no PVP** — pool compartilhado (tensão econômica de auto chess) ou lojas independentes (o que já existe) | Antes do F5.4 |
| B | **Layout de tabuleiros na rodada** — 2 batalhas simultâneas em arenas separadas, ou uma arena por par com câmera dedicada | Antes do F4.2 |
| C | **Roster final: 40 ou 60** | Durante o F6, conforme o custo real por personagem |

---

## 5. Fases

### F0 · Fundação e confiabilidade — 2-3 meses

*Nada abaixo é planejável enquanto o projeto não for reconstituível e verificável.*

| | Item | Prazo |
|---|---|---|
| ~~**F0.1**~~ | ✅ **CONCLUÍDO 2026-08-04** — **Reprodutibilidade.** Git LFS adotado (831 objetos), 13 commits temáticos, `EditorBuildSettings` corrigido, `StatType`/`DataCategory` com valores explícitos, validador de ID, identidade `NeekoBingo`/`com.NeekoBingo.dropinheroes`. Clone limpo verificado. **Achado crítico:** a migração havia quebrado o Tilemap da `TowerRun` (61 de 106 tiles mortos) — reparado. Ver `Docs/plans/F0.1-reprodutibilidade.md`. ⏸️ Push retido até o F0.11 | ~~1-2 sem~~ |
| **F0.2** | **Build distribuível** — Sandbox fora da build; `DebugConfig` em Resources para filtrar os 167 call sites de log; CanvasScaler consistente (MainMenu/Loading em 800×600 contra 1920×1080 das demais); `Application.Quit` e tela de opções, **ambos ausentes hoje** | 3-5 sem |
| **F0.3** | **asmdefs** — `Sim` / `Presentation` / `Data` / `Modes`. Os módulos já são classes puras; o corte natural existe. Quebra os usings cruzados de UI | 1 sem |
| **F0.4** | **RNG semeado + suíte de regressão** — 7 call sites de `Random`, nenhum `InitState` no projeto. Teste: mesma formação + mesma seed → mesmo log de dano | 1-2 sem |
| **F0.5** | **Estabilização** — varredura de bugs contra a suíte; `TimeoutResolver` que sempre favorece `Team.Player`; build limpo sem warning; os 3 danos pré-existentes achados no F0.1 (Animator inexistente em `TowerRun`/`Sandbox`, 2 sprites de retrato faltando) | 2-3 sem |
| ~~**F0.11**~~ | ✅ **CONCLUÍDO 2026-08-04** — **Padronização e peso de personagem.** 2.632 sprites levados a formato canônico: **54 dimensões → 2**, **PPU 185-310 → 90**, **~46 pivôs → 3**, **72 `AnimClipTuning` → 0**, **44% RGBA32 → 0**, **589 MB → 151 MB**. `CharacterArtPostprocessor` impõe o formato em todo import futuro; validador em `Tools ▸ DropInHeroes`. Ver `Docs/plans/F0.11-peso-de-personagem.md`. ⏸️ Só o **push** segue pendente (decisão D4) | ~~2-3 sem~~ |

**Prova de saída:** um clone limpo em outra máquina gera um `.exe` que 3 pessoas de fora jogam do menu ao fim de uma run, sem erro no log.

> **Por que o F0.11 vem antes do push.** Medido no F0.1: a arte de personagem é **3 a 5,7× oversampled** (Vela é armazenada em 640 px e renderiza em 111 px — 33× em área). São 589 MB para 7 personagens, ~5 GB projetados para 60, contra 1 GB de cota gratuita do GitHub LFS. Como `origin/main` ainda está no commit anterior ao F0.1, **nada dessa arte foi enviado ainda** — otimizar antes do primeiro push mantém a cota limpa de graça. Depois de enviada, a arte pesada fica no LFS remoto para sempre.

> "Resolução completa de bugs" é um **portão com critério verificável**, não uma fase de duração indefinida. Bug fixing sem suíte de regressão, numa base prestes a ser refatorada, é trabalho que se desfaz.

---

### F1 · Refatoração estrutural — 3-4 meses

#### F1.1 · Vocabulário de módulos: zero código por personagem — 6-10 sem

**O item mais importante do plano.** Hoje os 7 personagens têm comportamento bespoke: Vela tem 343 linhas + sistema de formas próprio, Guliver tem damage share, Rikurby e Darulito têm invocações, Hami ganha Attack por acerto, Pedro tem `CombatStartPassive`.

O trabalho é **generalizar cada comportamento em módulo reutilizável e re-autorar os 7 personagens sem uma linha de C# específica**: transformação vira módulo de troca de forma com gatilho parametrizável; acúmulo de crítico vira contador genérico com limiar; damage share vira redirecionamento configurável.

**Regra permanente:** módulo novo só entra se for genérico (sem nome de personagem no código) e utilizável por pelo menos 2-3 personagens hipotéticos. *Zero código por personagem* é atingível; *zero módulo novo para sempre* não é — cada módulo é investimento amortizado sobre 60 personagens.

**Prova:** os 7 personagens re-autorados apenas em asset, com `Assets/Scripts/Combat/Unit/Skills/Characters/` vazia.

#### F1.2 · Preparação para multiplayer — 3-4 sem

Com combate sempre 1v1 (decisão 4), o escopo é menor do que seria num free-for-all:

- **Contexto de partida explícito** — numa rodada de 4 jogadores o host simula **2 batalhas simultâneas**. `CombatController` é singleton de cena (`Instance` acessado de dentro de `StatsModule.cs:304`, `SkillModules.cs:296`, `DarulitoSupreme`). Precisa suportar 2 contextos concorrentes. **Não** é preciso suportar N partidas por processo.
- **`Team` permanece binário** no nível da batalha. Slots de jogador vivem no orquestrador de rodadas.
- **`PickedBuildResolver` sem noção de dono** (`UnitController.cs:32`) — resolve build a partir de estado global; com múltiplos loadouts na cena, resolve errado.
- **`PlacementGrid` não tem conceito de lado ou metade** — só existe o hook `EnemySidePlacement`.

#### F1.3 · Loadout por slot — 2-3 sem

`StatsModule.cs:353` colapsa todo o loadout num único `AddModifier("loadout", ...)`; `LoadoutModule.cs:69` aplica stats uma vez por vida da unidade (guard `statsApplied`). Sem quebrar os dois, **nem equipamento, nem ascensão, nem troca entre rodadas funcionam**. Pré-requisito do F3.

#### F1.4 · Fluxos de Menu, Picker e Characters — 3-4 sem

Extrair lógica de negócio dos controllers de UI; reduzir acoplamento por referência de Inspector (`CharactersScreenController` tem ~20 `[SerializeField]`); separar navegação de apresentação. Feito **antes** do polimento visual, para não polir duas vezes.

**Prova de saída:** duas batalhas concorrentes com donos distintos rodam 30 minutos sem exceção; suíte de regressão passa; nenhum singleton mutável no caminho de combate.

---

### F2 · Escala de conteúdo — 2-3 meses

| | Item | Prazo |
|---|---|---|
| **F2.1** | **Memória e streaming** — SpriteAtlas; regras de import (44% dos sprites não são múltiplos de 4, logo sem compressão BC/DXT); Addressables; desamarrar o catálogo da cena de boot (`Loading.unity:654` carrega tudo para mostrar o menu). 694 MB para 7 personagens → ~6 GB para 60 | 3-5 sem |
| **F2.1b** | **Cota de LFS a longo prazo** — o grosso foi resolvido no **F0.11** (orçamento de resolução + postprocessor). Aqui fica apenas a reavaliação com o roster real na mão: se mesmo otimizado o volume se aproximar de 1 GB, contratar o Data Pack do GitHub (US$ 5/mês por 50 GB). Decisão de custo, não de engenharia | 2 dias |
| **F2.2** | **Ferramenta de autoria de personagem** — compor módulos, stats, tipos e animações sem tocar em C#, sobre o vocabulário do F1.1 | 3-4 sem |
| **F2.3** | **Texto de habilidade gerado da composição** — 60 personagens são ~180 descrições. O markup `{kw:}`/`{v:field}` já existe; falta derivar o texto dos módulos | 2-3 sem |
| **F2.4** | **Validação da pipeline** — criar 3 personagens ponta a ponta, cronometrados | 3 sem |

**Prova de saída — portão de decisão do projeto:** um personagem novo vai do conceito ao jogável em **≤1 semana, sem escrever C#**. Se os 3 de validação levarem 3 semanas cada, o alvo de 60 não é viável e a decisão C precisa ser tomada para baixo antes de qualquer produção em massa.

---

### F3 · Progressão — 3-4 meses

| | Item | Prazo |
|---|---|---|
| **F3.1** | **Equipamento no lugar da skill tree** — separar definição de item (ScriptableObject, molde de `ArtifactData`) de instância rolada do jogador; inventário; absorver os 5 artefatos existentes. Morrem ~640 linhas de UI e 3 dos 7 editor tools | 8-10 sem |
| **F3.2** | **Nível e ascensão** — junto de F3.1, mesmo ponto de contato. `StatBudget.ComputeBaseStat` já é fonte única do stat bruto; `StatPreviewCalculator` garante preview == combate | 4-6 sem |
| **F3.3** | **Loop de farm automático** — variante automatizada do fluxo do Tower Run: destravar a loja da FSM (`await shop.OpenAndAwaitChoice` bloqueia hoje), separar velocidade de simulação da de render, cap de framerate (`targetFrameRate` não existe e `runInBackground: 1` cozinha a máquina em farm de horas) | 4-6 sem |
| **F3.4** | **Inimigos dedicados e recompensas** — a ladder usa os próprios heróis do jogador como inimigos, e o único não-herói (`Dummy.asset`) não tem animação. `grep reward\|loot\|drop` = vazio | 4-6 sem |

**Prova:** o jogador equipa peças com valores rolados, sobe nível, aperta auto e acumula drops num resumo de coleta.

---

### F4 · Polimento — 3-4 meses

*Cada tela entra aqui só depois que seus sistemas congelaram.*

| | Item | Prazo |
|---|---|---|
| **F4.1** | **Combate — visual e técnico** — feedback de impacto, números de dano, telegrafia de supreme, transições de estado; custo de `PlayableGraph` por VFX e do `AnimatorOverrideController` por unidade | 4-5 sem |
| **F4.2** | **UI/UX de combate** — HUD, barras, strip de status, seleção, câmera; layout de rodada pareada (decisão B) | 3-4 sem |
| **F4.3** | **Tela inicial** | 2 sem |
| **F4.4** | **Characters redesenhada para escala de 60** — busca, filtro, ordenação, virtualização de lista. Hoje instancia um item por personagem do catálogo: desenhado para 7, quebra em 60 | 4-5 sem |
| **F4.5** | **Seletor de time / picker** — preparado para partida de 2 a 4 jogadores | 3-4 sem |
| **F4.6** | **Áudio** — SFX de golpe/morte/UI, música, mixer, volume. Subsistema **inteiramente ausente**; gancho natural no `OnAttackHitFrame`, já presente em 24 clipes | 4-6 sem |

---

### F5 · Multiplayer — 3-4 meses

| | Item | Prazo |
|---|---|---|
| **F5.1** | **Steamworks + Auto-Cloud** — SDK (`Steamworks.NET` ou `Facepunch.Steamworks`), Steam ID, overlay, `schemaVersion` no save. Auto-Cloud é configuração de painel, zero código | 2-3 sem |
| **F5.2** | **Lobby de 2 a 4 jogadores** — `ISteamMatchmaking`, convite por overlay, metadados de lobby | 2-3 sem |
| **F5.3** | **Host-autoritativo** — host simula as 2 batalhas da rodada e envia snapshots a ~15Hz (~4,6 KB/s); convidados interpolam. Transporte via `SteamNetworkingSockets`. **Sem framework de replicação** — o volume é pequeno e específico demais para justificar Netcode/Mirror | 6-8 sem |
| **F5.4** | **Orquestração de rodadas** — pareamento por rodada, eliminação, submissão de formação + loadout, ready-up e timeout compartilhados, espelhamento de lado | 3-4 sem |
| **F5.5** | **Ciclo feio** — queda de jogador (host declara e encerra), abandono, timeout de lobby | 1-2 sem |

---

### F6 · Produção de roster — 13-20 meses, paralelo a partir do F3

33 personagens (para 40) ou 53 (para 60), todos compostos de módulos, a ~1 semana cada.

**Não é sequencial** — roda intercalada com F3, F4 e F5 assim que o F2 provar a pipeline. É a fase mais longa e a que define a data final.

---

### F7 · Beta fechado — 1-2 meses

App Steam (US$ 100 de Steam Direct, não recuperável num jogo free), survey de conteúdo gerado por IA, chaves para convidados, ondas de feedback.

---

## 6. Cronograma

**Sistemas em série:** F0(2,5) + F1(3,5) + F2(2,5) + F3(3,5) + F4(3,5) + F5(3,5) + F7(1,5) ≈ **20-21 meses**
**Roster em paralelo a partir do mês ~8:** 13-20 meses

| Alvo | Prazo realista |
|---|---|
| **40 personagens** | **20-26 meses** |
| **60 personagens** | **26-32 meses** |
| Beta antecipado com ~20 personagens | ~14-16 meses |

### Ondas de entrega

Um prazo único de 26-32 meses para um dev solo é o cenário em que o projeto morre de exaustão antes de qualquer problema técnico. Entregar em ondas é recomendação forte.

| Onda | Quando | O que os convidados jogam |
|---|---|---|
| **1** | ~mês 14 | Single-player completo: torre, equipamento, nível, farm, ~15-20 personagens, com áudio |
| **2** | ~mês 21 | Multiplayer 2-4 jogadores, ~30-40 personagens |
| **3** | ~mês 26-32 | Roster completo |

---

## 7. Riscos

| Risco | Mitigação |
|---|---|
| **Pipeline não chega a 1 personagem/semana** — premissa de que tudo depende | Portão explícito no F2.4. Se falhar, decidir C para baixo antes de produzir em massa |
| **Cadência** — 5 commits em 7 meses, um mês inteiro sem commit | Ondas de beta; ciclos de ~6 semanas com algo jogável no fim |
| **Reescrever o próprio trabalho recente** — skill tree + 3 editor tools marcados para demolição; causa mais comum de desistência solo | **Não deletar a árvore antes do equipamento estar jogável** |
| **Dependência da Ludo** — >100 créditos/personagem, rate limit; 60 personagens presumem o mesmo modelo. Se mudar, os novos não casam visualmente com os antigos | Travar prompts, seeds e imagens de referência no repositório |
| **Arte gerada por IA** — Steam exige declaração; proteção autoral questionável | Declarar no survey; sem ação adicional para beta privado |
| **Host pode trapacear** — save é JSON em claro e o build é Mono (dnSpy) | Aceito por decisão: beta entre amigos. Manter cálculo de stat como função pura, para que um servidor futuro seja porte e não reescrita |
| **6 GB de arte** | F2.1 antes do 8º personagem, não depois do 25º |

---

## 8. Rastreio do plano original

| Item original | Onde entrou |
|---|---|
| Resolução completa de bugs | F0.5, como portão verificável |
| Polimento do combate (visual e técnico) | F4.1 |
| Polimento UI/UX de combate | F4.2 |
| Polimento UI/UX de character selector | F4.5 |
| Polimento UI/UX da tela inicial | F4.3 |
| Polimento da tela de characters | F4.4, com redesenho para escala de 60 |
| Refatoração Menu/Picker/Characters | F1.4 |
| Refatoração do fluxo de combate | F1.2 |
| Refatoração do sistema de combate e personagens | **F1.1 — item central do plano** |
| Combate adaptado a PVP e PVE auto | F1.2 + F3.3 |
| Tela de seleção de time PVE | F4.5 |
| Fases e dificuldades PVE | F3.4 |
| Equipamentos, fim da skill tree | F3.1 |
| Níveis e ascensão | F3.2 |
| Recompensas de gold/xp/ascensão | F3.4 |
| Minimizar fases automáticas | F3.3 |
| Polimento do mapa de fases | F4 |
| Otimização de picks para PVP | F1.4 + F4.5 |
| Setup de servidor / contas | F5.1 — virou Steam, sem servidor |
| Lobbys custom + gameplay PVP | F5.2–F5.5 |
| Gacha, banners, missões diárias | **Fora do escopo** — sem monetização, vira sistema de desbloqueio, reavaliável após o beta |
| Fila normal, ranqueada, matchmaking | **Cortados** — beta privado não tem população para fila |
| Deploy live service / publicação Steam | F7, como beta fechado |

---

## 9. Como pedir um plano de fase

Um plano de `F.x` deve conter, no mínimo:

1. **Escopo fechado** — o que entra e, explicitamente, o que não entra
2. **Estado verificado** — leitura do código real via Unity MCP, não suposição; arquivo:linha para cada afirmação
3. **Decisões técnicas** — cada uma com opções, trade-off e recomendação
4. **Ordem de execução** com dependências entre itens
5. **Critério de saída verificável** — como se sabe que a fase acabou
6. **Impacto no Editor** — o que precisa ser religado, recriado ou reconfigurado à mão
7. **Riscos de regressão** — o que pode quebrar e como detectar

Antes de escrever qualquer plano de fase, ler `Docs/heuristicas-de-projeto.md`.
