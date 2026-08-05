---
name: criar-personagem
description: Pipeline completo de criação de personagem novo para DropInHeroes 100% via chat — arte 32-bit e animações via Ludo MCP, import/rig no Unity, CharacterData/skills/catálogo, scripts de mecânica reaproveitando os sistemas modulares, e validação em runtime. Use quando o usuário enviar um conceito de personagem (imagem de referência + ideias de habilidades/lore) e pedir para criar/integrar o personagem (completo ou parcial). Also matches "novo personagem", "create character", "gerar personagem via ludo".
---

# Criar Personagem — conceito → jogo, via Ludo + Unity MCP

Pipeline em 7 fases (0–6) com **checkpoints de aprovação do usuário** entre cada uma. O usuário pode
entrar em qualquer fase (ex.: já tem a arte → começar na Fase 2; só quer mecânica → Fase 4).
Pergunte SEMPRE qual o escopo desejado antes de começar. **A Fase 6 (Lições Aprendidas) roda automática no fim.**

## Princípios (não negociáveis)

1. **Checkpoints**: nunca atravesse uma fase inteira sem mostrar o resultado intermediário.
   Geração por IA custa créditos e é subjetiva — o usuário escolhe entre candidatos.
2. **Créditos Ludo + THROTTLE (crítico)**: `createImage`/`generateWithStyle`/`editImage`/`generatePose` ≈ 0.5 crédito/variação;
   `animateSprite` cobra por segundo (blitz 1.9/s, mínimo 4s ≈ 7.6 créditos por animação).
   Um personagem completo (9 animações × tentativas) passa de 100 créditos. **Antes de cada
   lote de geração, diga o custo estimado e confirme.** ⚠️ **NUNCA dispare uma RAJADA de `animateSprite`
   (ex.: 8 de uma vez) — rate-limita a conta INTEIRA do Ludo (até o `generatePose`, antes rápido, para de
   responder). Dispare 1–2 por vez, aguarde cada lote completar (~3 min/clipe) antes do próximo. `animateSprite`
   dá TIMEOUT no cliente mas COMPLETA no servidor → recuperar por `getSpriteResults` filtrando `request_id`
   (não é erro). Espera não-bloqueante: `sleep N` em background (`Start-Sleep`/sleep foreground é bloqueado).
   Se a conta travar, PARE de disparar e espere o rate-limit resetar — cada chamada sob throttle prolonga.**
3. **Reaproveitar sistemas SEMPRE** (regra de ouro do projeto): skill = classe fina compondo MÓDULOS em
   código (`SkillModules.*` sobre `Targets.*`), o SO só com números (SO-thin). Só criar módulo/base NOVA
   quando o comportamento for genuinamente único — e aí de forma GENÉRICA e reutilizável, nunca "if personagem".
4. **Verificar antes de assumir**: paths mudam (re-`Glob`), a arquitetura evolui (confira as
   memórias de sessão e o código atual). A mecânica canônica é a **Fase 4 desta skill** (padrão
   SO-thin/módulos); a §5 do `Docs/pipeline-personagens.md` aponta pra cá, e §0–§4 e §6
   [arte/rig/import] daquele doc continuam válidos.
5. **Honestidade**: o que não der para validar em runtime, marcar "não testado em runtime".
6. **Entrevista antes de gerar** (ver Fase 0): a coleta NÃO é um formulário mínimo — é uma
   entrevista que esgota TODA dúvida do conceito que o prompt inicial não respondeu, em três
   domínios (papel/escopo, direção de animação/visual, mecânica + implementação técnica). Nunca
   assuma o *feel* de uma animação, um VFX, ou como uma mecânica mapeia na arquitetura: pergunte.
7. **Auditoria de modularidade na implementação** (ver Fase 4): ao codificar cada habilidade,
   avalie se a estrutura está de fato bem modular ou se há algo hardcoded / individual demais que
   deveria virar sistema reutilizável. Achou candidato à generalização → **proponha ao usuário**
   (CLAUDE.md: perguntar antes de refatorar) em vez de seguir com "if personagem".
8. **Livro-razão de custo Ludo** (ver Fase 5): registre CADA geração (tipo, modelo, segundos/
   variações, tentativas, créditos) num livro-razão ao longo do pipeline. No fim, entregue a
   estimativa total + sugestões concretas de como a skill pouparia créditos e ganharia consistência
   nas próximas execuções.

---

## Fase 0 — Entrevista do conceito (esgotar dúvidas)

**Setup (antes de tudo):** (a) confirme o **Ludo conectado** com `validateApiKeyEndpoint` (grátis) — se
falhar, o Ludo não subiu (checar config/`/mcp`) e as Fases 1–2 estão bloqueadas, avise o usuário; (b) peça
a arte de referência **como URL hospedada**, não arquivo local. ⚠️ **Alimentar arte LOCAL no Ludo é
impraticável** (validado): os tools só aceitam URL ou base64; o base64 de arte em resolução usável tem
25k–140k chars, que eu teria de reproduzir literalmente na chamada (pesado, transcrição pouco confiável).
**Peça ao usuário para hospedar** os sprites (arrastar num chat do **Discord** → copiar link `cdn.discordapp.com/...png`,
ou `i.imgur.com/xxx.png`; evitar Google Drive que redireciona). Depois do 1º passo, TUDO vira URL do Ludo e flui
liso. (Imagem colada no chat também NÃO vira arquivo acessível.) Fallbacks se ele não puder hospedar: base64
bem reduzido (via arquivo, não stdout que trunca em 30k) OU recriar por texto (`createImage`, diverge da arte dele).

Receba o que o usuário tiver (nome, referência anime, lore, ideias de habilidades) e **entreviste-o
com `AskUserQuestion` em lotes** até não restar dúvida material. **Pergunte SÓ o que o prompt
inicial NÃO respondeu** — não re-pergunte o que já veio. Cubra os três domínios abaixo; itere
enquanto houver lacuna que afete **arte, número ou arquitetura**. Não avance com lacuna aberta.

### Bloco A — Papel, escopo e stats
| Pergunta | Por quê |
|---|---|
| **Escopo**: completo (arte+mecânica) ou parcial (quais fases)? | dimensiona o trabalho e o custo |
| **Papel**: tanque / dano físico / dano mágico / suporte? Melee ou ranged (projétil)? | orienta stats points, tipo de dano e `ModularProjectileAttack` vs `ModularBaseAttack` (melee) |
| **As 3 habilidades**: ataque (escalonamentos), passiva (o que faz + valores por rank 1/2/3), supremo (efeito + custo de energia) | Rank só melhora a PASSIVA neste jogo |
| **Invoca?** Se sim: stats fixos da invocação + o que deriva do dono (%) | aciona SummonData/SummonStatProfile |
| **Tipos** (`CharacterType` primário/secundário) e distribuição de stat points | `CharacterData.statPoints` (bruto = piso geral + pontos × statPointValue) |
| **Silhueta/tamanho** (humanoide? menor/maior?) e, se **transforma/tem formas**, a ANATOMIA de cada forma (bípede? quadrúpede? asas?) | calibração de PPU/`visualScale` + anatomia guia TODO prompt de pose (ver 2.1: ancorar humano/jeans p/ híbridos) |

### Bloco B — Direção de animação e visual (feel de cada clipe)
Não assuma o *feel*: cada clipe tem uma leitura subjetiva que muda o `motion_prompt`, a pose
inicial, o modelo (blitz/eagle) e se há VFX. Pergunte o que o prompt não deixou claro:

| Pergunta | Alimenta |
|---|---|
| **Ataque**: arma/parte usada, quantos golpes, giro/estocada/soco, rápido-seco ou pesado? | pose de guarda + `motion_prompt` do Attack + frame de impacto |
| **Corrida**: leve e ágil, pesada, flutua/rasteja, quadrúpede? | `motion_prompt` do Run + pivot/footprint |
| **Idle**: personalidade parada (respira calmo, impaciente, arma pronta, flutua)? | `motion_prompt` do Idle |
| **Vitória**: gesto específico (pose de poder, reverência, provocação)? | clipe Victory |
| **Morte**: dramática/cômica, cai pra frente/trás, explode/desfaz? | Death + `final_image` + DeathIdle |
| **Drag** (erguido pelo jogador): pose suspensa, pernas balançando, resiste? | pose + `motion_prompt` do Drag |
| **VFX por ação**: QUAIS ações levam efeito visual e com que cor/forma (impacto do ataque, aura da passiva, explosão do supremo, projétil)? | `SkillModules.Vfx` + clipes VFX/Projétil + frame do evento |
| **Coreografia do supremo**: câmera/impacto especial, salto, tela treme, invoca algo? | pose de cast + `motion_prompt` do Supreme + `SkillModules.LeapToBestAoe`/`LeapToTarget` (no `OnCast`) |
| **Projétil** (se ranged): aparência (bola de energia, flecha, foice), rastro? | clipe Projétil + `ModularProjectileAttack` (`ProjectileSpec`) |

### Bloco C — Mecânica e implementação técnica
Para CADA habilidade, feche o comportamento exato **e** proponha o mapeamento na arquitetura,
confirmando com o usuário (aqui nasce a auditoria de modularidade da Fase 4):

| Pergunta / proposta a confirmar | Por quê |
|---|---|
| **Comportamento exato + casos de borda**: alvo (único/área/aliados?), timing (on-cast/on-hit/on-impact), condição de disparo, o que acontece se 0 alvos / alvo morto / recast? | evita mecânica ambígua virar código errado |
| **Interações**: a mecânica reage a outro evento (ex.: "ao receber cura, curar invocações"; "a cada N ataques recebidos")? | define `PassiveHooks` / estado próprio |
| **Escala por rank** (só passiva): quais números mudam em 1/2/3? | `valueByRank` / `RankTiers` |
| **Mapa técnico (proponha e confirme)**: cada efeito cai num **módulo EXISTENTE** (`SkillModules.Damage/Shield/Status/Heal/StatModifier/SpawnSummon…` sobre `Targets.*`; `OnHitModule` p/ on-hit; `SummonModule`)? Precisa de um **módulo NOVO genérico** (nunca "if personagem")? Precisa de um **módulo de unidade novo** ou de **estado próprio** (contador via hooks)? | garante reaproveitamento máximo e decide o que construir ANTES de codar |

**Registre todas as respostas como a spec** do personagem (é o contrato da Fase 4). Números não
especificados: proponha defaults tunáveis e liste-os no resumo final.

---

## Fase 1 — Arte base 32-bit (Ludo)

Objetivo: a partir da referência anime, gerar o sprite-base pixel-art do personagem.

1. **Gerar — CONVERTER a referência para 32-bit** (não "estilizar por cima"): use **`editImage`** com
   `image` = a referência do usuário e o **estilo NO PROMPT** — ex.: `"redraw this character as a 32-Bit
   era pixel-art sprite (32-bit console/JRPG shading, crisp pixel edges, limited palette); keep the EXACT
   same character, outfit and colors; full body, side view, facing to the LEFT; clean/transparent
   background"`, `n=4` (opc. `augment_prompt=false` p/ controle mais firme).
   ⚠️ **NÃO use `generateWithStyle`** aqui — ele COPIA o estilo da referência (anime liso), o OPOSTO de
   converter p/ 32-bit → gera "distância"/divergência do original (erro cometido numa execução real).
   **Nuance da API (pesquisado, alta confiança):** NENHUM tool combina imagem de referência + o enum
   `art_style`. `art_style="32-Bit"` (B maiúsculo, hífen) só existe no `createImage`, que é **text-only**
   (descarta a referência). Por isso o caminho fiel é `editImage` com o estilo no prompt. Se a conversão
   não sair de lado (a ref costuma ser frontal), rode `generatePose` depois p/ a vista lateral **olhando
   à ESQUERDA** (convenção do rig — `flipX` global vira o time do player p/ a direita).
   *Sem referência nenhuma:* `createImage` com `art_style="32-Bit"`, `perspective="Side-Scroll"` + descrição detalhada.
2. Se vier com fundo: `removeBackground` (`crop=false`).
3. **Converter webp→PNG** (Unity não importa webp): WPF `BitmapDecoder`→`PngBitmapEncoder`
   via PowerShell (codec WebP do WIC no Win11). Salvar candidatos em `C:\tmp\<char>_candidates\`.
4. **Checkpoint**: mostrar os candidatos (Read das imagens) → usuário escolhe/refina
   (`editImage` para ajustes pontuais; `generatePose` se a pose-base precisar mudar).

O sprite escolhido é o **frame-mestre** de todas as animações (consistência visual vem dele).

---

## Fase 2 — Animações (Ludo `animateSprite`)

Fluxo por clipe: **gerar pose-alvo (várias, escolher a melhor) → verificar → animar (params estruturados) →
avaliar → retry → checkpoint do usuário no fim**. Disparar `animateSprite` **1–2 por vez** (princípio 2 — throttle; NUNCA em rajada).

> Nomes EXATOS dos parâmetros: conferir o schema vivo de `animateSprite`/`generatePose` na hora
> (o MCP pode mudar). Abaixo estão os VALORES recomendados por conceito — mapeie para os campos reais.

### 2.0 Kit de clipes + tabela de parâmetros

Slots do `CharacterData`: `Idle`, `Run`, `Drag`, `Attack`, `Death`, `DeathIdle`, `Supreme`,
`Stun`, `Victory`. Extras: **projétil** (se ranged) e **VFX** (supremo/status). Invocação = kit
reduzido (Idle/Run/Attack; totem estático = só Idle + VFX).

| Clipe | Loop | Frames | Modelo | Pose inicial | Margem | Ação-base (motion prompt) |
|---|---|---|---|---|---|---|
| Idle | sim | 36 | blitz | frame-mestre | auto | `idle stance, subtle breathing, slight weight shift` |
| Run | sim | 25 | blitz | frame-mestre | auto | `running cycle, full stride, arms pumping` |
| Drag | sim | 25 | blitz | erguida (opc.) | auto | `lifted off the ground, legs dangling, gentle sway` |
| Attack | **sim** | 25–36 | blitz | **pose do GOLPE/lunge, NÃO do wind-up (2.1)** | **manual** | `{arma} strike to the left, thrust/swipe then recover, smooth repeating loop` |
| Death | não | 36 | blitz | frame-mestre | auto | `staggers, collapses to the ground defeated` (`final_image`=caído) |
| DeathIdle | sim | 16 | blitz | **último frame do Death** | auto | `lying defeated on the ground, faint motion` |
| Supreme | não | 36 | blitz | **pose de cast/charge (2.1)** | **manual** | `{efeito} dramatic charge-up then release of power` |
| Stun | sim | 16 | blitz | atordoada (opc.) | auto | `dazed and groggy, wobbling and swaying woozily, eyes closed` (NÃO "stars spinning" — cartunesco demais, reprovado) |
| Victory | sim | 25 | blitz | triunfo (opc.) | auto | `victorious celebration, triumphant` |
| Projétil | sim | 9–16 | blitz | — (VFX) | none | `{projétil} traveling forward, glowing trail` |
| VFX | não | 16–25 | eagle | — (VFX) | none | `{efeito} burst/impact, radial` |

- **Enquadramento** (fixo p/ personagem): `side view, full body, feet visible, subject centered`.
  NUNCA dar zoom no rosto. Consistência de ESCALA entre clipes é resolvida no IMPORT (PPU/pivot
  por pasta), não no prompt — mas cada clipe deve mostrar o corpo inteiro na mesma proporção.
- **Estilo** vem do frame-mestre (32-bit); **`blitz` é o DEFAULT p/ TUDO** (8/10 de estabilidade de movimento
  → menos "morphing"/membros extras e menos retries; mais barato). Inclusive o supremo. `eagle` (melhor
  visual, PIOR estabilidade) só se o movimento pedir e o blitz falhar. **Pose inicial:** ver 2.1 (pose-first
  em quase tudo; a coluna "frame-mestre" na tabela vale só p/ o Idle).
- **frame_size** = máximo/`0` (PPU ajusta no import). **crop=false** (canvas uniforme por animação
  — o rebuild de clipe do §3 assume isso). **individual_frames=true**.

### 2.1 Poses iniciais — POSE-FIRST em QUASE TODOS os clipes (default)

**Regra (validada com o usuário):** anime a partir de uma **pose-alvo da ação**, não do idle — senão a
animação fica inconsistente (o corpo "volta" pro idle no meio do clipe) e força retries caros. Para cada
clipe de ação (**Run, Attack, Drag, Stun, Victory, Supreme**): `generatePose` com **n=2–4**, **verificar
(Read) e escolher a melhor** como `initial_image`. Preferir preset da pose (`generatePose.pose` enum:
`Run (Left)`, `Attack Ready`, `Jump Preparation`… ou `Other`+descrição) — mais estável que texto livre.
- ⚠️ **TRAVAR O VISUAL no prompt da pose** (obrigatório): "change ONLY the pose; keep EXACTLY the same
  clothes/colors/identity; do NOT add gloves, armor, vest, tie, gear or props". Sem isso houve drift real:
  virou colete tático, depois luvas de boxe, depois gravata.
- ⚠️ **Formas HÍBRIDAS / anatomia especial (ex.: homem-tigre quadrúpede) = a MAIOR fonte de retry** (validado
  na Vela: corrida/ataque/supremo do tigre precisaram de 3–5 tentativas cada). O Ludo derrapa p/ (a) **animal
  completo** (perde o humano) ou (b) **bípede reto** (perde os 4 apoios) quando o prompt pede ação feroz.
  **Correção que funciona — aplicar desde a 1ª tentativa:** (1) **ancorar os elementos HUMANOS explicitamente**
  em TODO prompt: "HUMAN head and face, HUMAN hands, wearing BLUE JEANS, striped tail"; (2) proibir o desvio:
  "do NOT turn into a full animal tiger, do NOT give a full animal head/muzzle, do NOT stand upright on two
  legs"; (3) **usar a pose da anatomia-alvo como `image` de referência** (ex.: a pose quadrúpede em 4 apoios),
  não o master bípede — a referência puxa o output p/ a anatomia certa. Idem para run quadrúpede: pedir a pose
  já **em passada/mid-stride** (não com as duas mãos plantadas), senão nenhuma animação de movimento fica boa.
- **Checar (Read): pose correta + identidade/roupa preservada + facing à ESQUERDA** → retry da pose (barato,
  0.5) até acertar. Só animar depois de aprovar a pose.
- **Exceção — Death:** parte do idle/frame-mestre em TRANSIÇÃO p/ pose de morto (`animateSprite` com
  `initial_image`=idle + `final_image`=pose caído). **DeathIdle** parte da pose de morto (loop sutil). **Idle**
  usa o próprio frame-mestre.

### 2.2 Template do motion_prompt (base fixa + molde do usuário)

Montar SEMPRE nesta ordem, curto (regra Ludo: descreva só a AÇÃO; aparência já está no sprite):

```
<AÇÃO-BASE do clipe>[ + <MOLDE do usuário>]. <qualidade: velocidade/peso>. Side view, full body in frame[, weapon/effect stays inside the canvas].
```

- **AÇÃO-BASE**: coluna da tabela.
- **MOLDE do usuário**: o pedido específico dele p/ aquele clipe (ex.: Attack = "giro duplo de
  machado"; Supreme = "invoca um totem e o chão racha"). Sem pedido → só a base.
- **qualidade**: `fast, weighty` (ataque), `smooth, subtle` (idle), `heavy, dramatic` (supremo)…
- 1 ação clara por clipe; não empilhar movimentos; não repetir descrição de aparência.

### 2.3 Disparo em LOTES DE 1–2 (throttle — NUNCA rajada)

⚠️ **Disparar 8 de uma vez rate-limita a conta INTEIRA do Ludo** (erro real: só 2 passaram, o resto falhou
silencioso, e o `generatePose` travou junto — perdeu-se muito tempo). Dispare **1–2 `animateSprite` por vez**,
cada um com `request_id="<char>_<anim>_t1"`. Cada chamada dá timeout no cliente mas COMPLETA no servidor →
aguarde ~3 min (background `sleep`, non-blocking) e colete por `getSpriteResults` filtrando `request_id`; só
então o próximo par. **Pipeline:** enquanto o par N renderiza, avalie o par N-1. **DeathIdle** parte da pose
de morto (não espera o Death). Se a conta travar, PARE e espere o rate-limit resetar.

### 2.4 Avaliação + retry automático (máx 3 por clipe)

Baixar frames → contact sheet (System.Drawing) → **Read**. **Aprovado** só se passar no checklist:
1. ação integral (começo→meio→fim); 2. sem artefato (membro/dedo extra, borrão, "morphing" de
identidade, cor fora do sprite); 3. facing bate com o frame-mestre (cue: texto/logo; ⚠️
falso-positivo — só espelhar se DESTOAR das outras); 4. escala/silhueta estável entre frames
(senão o PPU no import sofre); 5. pés no chão / âncora estável; 6. ciclos: 1º×último frame casam.

Reprovou → **retry mudando UMA variável de maior impacto** conforme o defeito (não reescrever
tudo às cegas):

| Defeito observado | Ação no retry |
|---|---|
| identidade/estilo derretendo | manter `blitz`; **corrigir a POSE inicial** (keyframe > texto) |
| movimento rígido/robótico/incompleto | `blitz` → `eagle` (trocar modelo ANTES do prompt) |
| arma/efeito cortado na borda | `margem` → `manual` (maior) |
| loop feio (1º≠último) | fixar `final_image` ou ajustar `frames` |
| ação errada/ambígua | refinar o texto (incorporar feedback do usuário, se houver) |
| ação breve/invisível no jogo (poucos frames ou golpe curto) | **mais frames** (igualar o roster: 25–36, NUNCA 16) + motion que **preenche o clipe** (windup→ação→recuperação) — a cadência ESTICA o clipe (`3/Speed`s), então ação curta some |
| giro/rotação com membros extras/duplicados (ex.: braço extra nas costas no run) | pedir "coherent human body with EXACTLY two arms and two legs, no extra/duplicated arms, no limbs behind the back, no blur" |
| forma híbrida virou **animal completo** ou **bípede reto** | re-gerar a POSE ancorando humano+jeans + "do NOT become a full animal / do NOT stand upright" e usando a pose da anatomia-alvo como `image` de referência (ver 2.1) |
| **ataque não "parece ataque"** (golpe fraco/não conecta) | pose do GOLPE/lunge (não wind-up) + motion decisivo "lunge and SLAM/thrust that clearly lands on the target, then recover" + `loop=true` |
| **arma parada/rígida** na corrida (não acompanha o movimento) | re-gerar a pose com a arma **sobre o ombro** (carry de dardo) + motion "the {arma} swings and bobs with each stride" |
| **stun cartunesco** (estrelinhas/pássaros) | trocar o motion p/ "groggy woozy sway, eyes closed, no stars/cartoon effects" |

`request_id` novo por tentativa (`_t2`, `_t3`); retries também em paralelo. **Após 3 sem passar**:
escolher o MELHOR dos 3, listar o defeito residual e **pedir decisão do usuário** (aceitar / dar
input / pular clipe). ⚠️ Cada `animateSprite` recobra créditos (por segundo) — re-exportar sheet
(Adjust) é grátis, re-gerar não; confirmar orçamento antes de rodadas grandes de retry.

### 2.5 Checkpoint do usuário

⚠️ **Mandar o `video_url` de CADA animação (e de cada pose) no chat, SEM EXCEÇÃO, assim que sai** (validado:
o usuário cobrou "vários que foram feitas você não mandou" — ele avalia pelos links, não só pela minha
avaliação). **Mostrar por LINKS clicáveis** — cópia no workspace → link markdown + a `video_url` —, **NUNCA
abrindo a imagem na tela** (`Start-Process`). O `video_url` deixa avaliar o MOVIMENTO de verdade (não só o
sheet estático). Eu baixo+leio o contact sheet p/ MINHA triagem, mas o veredito é do usuário pelo vídeo. Ele
aprova, pede re-roll específico (com input) ou segue. Ao pipelinar: ao recuperar um lote, **poste os links
ANTES** de disparar o próximo.

---

## Fase 3 — Import e rig no Unity

Seguir **`Docs/pipeline-personagens.md` §1–§4 e §6** (playbook validado). Resumo executivo:

1. **Fatiar o spritesheet do Ludo em frames** (o `animateSprite` devolve `spritesheet_url` = grade única;
   `individual_frames=true` também dá URLs por frame, mas o slice local é mais robusto): grade = `ceil(sqrt(n))`
   colunas × linhas, `frameW=W/cols`, `frameH=H/rows`, recortar as primeiras `n` células (Python/PIL, preservar
   alfa). Salvar em `Assets/Art/Characters/<Char>/<Anim>/<Char><Anim>_000.png...` (re-`Glob` antes; o usuário
   reorganiza pastas). Converter webp→PNG no processo.
2. **Medir bbox de conteúdo via Unity/System.Drawing/PIL (NUNCA parser IHDR manual)**. ⚠️ **MEÇA o roster —
   não assuma `targetWorldH=2.7`**: meça a altura de CONTEÚDO (bbox alpha ÷ PPU) de um personagem existente
   calibrado (ex.: Hami ~2.31u; ⚠️ alguns são imports gigantes ~9.8u — não use como alvo) e IGUALE, senão o
   novo sai grande demais (erro real).
   - **Personagem de 1 forma / poses homogêneas:** **PPU por PASTA** (adaptativo) = `round(maxFrameH_por-frame /
     targetWorldH)` (por-frame, não a UNIÃO que infla com saltos). DeathIdle reusa o PPU do Death.
   - ⚠️ **Multi-forma / poses muito variadas (ex.: Vela humano de pé + tigre agachado):** NÃO normalize por
     clipe — cada geração do Ludo tem **escala de pixel própria** E a altura de conteúdo varia com a POSE
     (lança vertical infla o idle; agachado/deitado encurta). Normalizar por clipe deixa tudo do MESMO tamanho
     (o tigre agachado ficaria tão alto quanto o humano de pé — errado). Use **UM PPU único p/ o personagem
     inteiro** (calibrado do idle da forma-base → ~2.3u) aplicado a TODOS os clipes das 2 formas: o corpo fica
     consistente e o tigre aparece naturalmente mais baixo. Ajuste fino no fim por `visualScale`.
3. Import settings por pasta: `spriteMode=Single`, `filter=Point`, mipmaps off, `alphaIsTransparency`,
   **pivot custom**: `pivotX = centro dos PÉS` (não da união — em clipes com braço/perna estendida a união
   descentraliza; o soco puxava o centro), `pivotY = (feetY+PPU)/texH` (pés 1.0 abaixo da raiz = footprint).
   VFX/projétil: pivot **center**. Tamanho fino vs roster no fim por `CharacterData.visualScale`.
4. Criar os `.anim` em `Assets/Animations/Animations/<Char>/` (60 fps, keys em `i/60`,
   `loopTime` nos ciclos) via `AnimationUtility.SetObjectReferenceCurve`.
5. **`OnAttackHitFrame` no frame de impacto** — necessário onde o efeito é DISPARADO no golpe: o **ataque
   básico** (dano intrínseco) e **skills com efeitos em `onImpact`** (ex.: supremo-bote que causa dano no
   impacto). ⚠️ **NÃO precisa** onde os efeitos estão em `onCast` (rodam no Execute — ex.: supremo de cura/benção,
   invocação de totem). Sem o evento onde é preciso → habilidade sem efeito. Identificar o frame de impacto
   olhando o contact sheet (Vela: ataques @0.167; supremo-mordida @0.35 depois do salto).
6. Validação visual: frames feet-aligned em escala-mundo + `read_console` limpo. Ajuste fino
   de tamanho por `CharacterData.visualScale`.

---

## Fase 4 — Dados, mecânicas e cadastro

**Antes de codificar qualquer mecânica**: leia a arquitetura ATUAL (memórias `so-thin-code-modules`,
`skill-summon-roster-architecture` e `skill-naming-and-composition` + código).

**Padrão SO-thin (obrigatório — TODAS as skills seguem):** cada habilidade é UMA classe fina, nomeada por
personagem, derivando de uma base `Modular*`, em `Skills/Characters/<Nome>/`. O ScriptableObject (.asset) expõe
SÓ números (scalings, intensidades por rank, durações, chances, custo de energia) + identidade (nome/ícone/
texto/clipes). TODA a estrutura (alvo, stat, buff/debuff, tipo de dano, QUAIS módulos e em que ORDEM) vive em
CÓDIGO, compondo `SkillModules.*` sobre alvos `Targets.*`. Regra: muda O QUE faz → código; muda QUANTO → SO.
Módulos disponíveis (`Skills/Modules/SkillModules.cs`): `Damage`, `Status`, `Heal`, `Shield`, `Vfx`,
`LeapToTarget`, `LeapToBestAoe`, `PlayExtra`, `SpawnSummon`, `GainStat`, `GainEnergy`, `Cleanse`, `StatModifier`,
`StatModifierPerCount`, `StatModifierPercentOfStat`, `ApplyDamageShare`. Alvos (`Targets.cs`): `Focus`, `Self`,
`Allies(includeSelf)`, `Enemies`, `AroundSelf(r,enemies)`, `AroundTarget`, `Line`, `LowestHealth`.

Bases (`Skills/Core/`) — o que cada uma dá (modelos prontos em `Skills/Characters/`):
- **Ataque básico**: `ModularBaseAttack` (melee) — implemente `OnStrike(context, ref state)` compondo
  `SkillModules.Damage(context, Targets.Focus(context), scalings, DamageType.X, ref state)` + efeitos próprios
  (ex.: `SkillModules.Status(...)` com `chance`). Ranged: `ModularProjectileAttack` (só o `ProjectileSpec` no
  SO). Cadência é GLOBAL (Speed÷3), sem parâmetro por skill. Modelos: `GuliverAttack`, `MardaAttack` (projétil),
  `VelaSpearAttack` (dano + status on-strike), `DarulitoAttack` (multi-scaling).
- **Supremo**: `ModularSupreme` (`energyCost` herdado) — duas âncoras: `OnCast(context)` ANTES da animação
  (ex.: salto via `SkillModules.LeapToTarget`/`LeapToBestAoe`) e `OnImpact(context, ref state)` no frame de
  impacto. Componha `Damage/Heal/Status/Shield/Vfx/Cleanse/...`. Stat temporário só durante a aplicação (ex.:
  +Penetração numa onda): `SkillStat(context, stat, flat)` no início do OnImpact. ⚠️ `animationSpeedReference`
  ~2.5 (não 12 — 12 faz tocar a ~0.05×, ~10s travado). ⚠️ **Efeitos em `OnCast` rodam no Execute** (sem
  hit-frame — ex.: bênção/cura, invocação de totem); **efeitos em `OnImpact` exigem o evento `OnAttackHitFrame`
  na animação** (Fase 3.5) — sem ele, não disparam. Modelos: `PedroSupreme` (leap+dano+debuff+buff),
  `HamiSupreme` (SkillStat penetração + dano em linha), `RikurbySupreme` (leap-AoE + cura-por-atingido + stun),
  `VelaBiteSupreme` (alvo fixado no cast), `TotemBolivianoSupreme` (efeitos no OnCast, totem sem hit-frame).
- **Passiva**: `ModularPassive` — implemente `Compose(context)` compondo módulos persistentes
  (`StatModifier` flat, `StatModifierPercentOfStat` dinâmico, `StatModifierPerCount`, `ApplyDamageShare`,
  `Shield`, `Status`…); registrados no `Persistent` e AUTO-REVERTIDOS ao morrer/fim. Rank escala em código
  (`RankTiers.ValueFor(arrayPorRank, context.owner.Rank)`). Modelos: `GuliverPassive` (redução+share),
  `DarulitoPassive` (aura de resistências + regen dinâmica).
  - Passiva que REAGE a evento (contador, "ao receber cura", "ao critar", "a cada N ataques recebidos"): ainda
    `ModularPassive` — assine os `PassiveHooks` no `Compose` e registre o unsubscribe via `Persistent.Add`;
    estado próprio (contador) em campos + `OnReset`. Modelos: `RikurbyPassive` (invoca por ataques recebidos +
    partilha de cura), `VelaPassive` (transformação por crítico).
  - On-hit nativo (rider a cada ataque do dono): `ModularOnHitPassive` — `OnHitStrike(context, ref state)`
    registrado no `OnHitModule` em código. Modelo: `HamiPassive`.
  - Efeito 1× no início do combate (ex.: Vigor+escudo a todos): `CombatStartPassive` (roda `SkillEffect[]`
    no start) OU um `ModularPassive` cujo `Compose` aplica direto (ver `PedroPassive`).
  ⚠️ **Escudo a aliados no início do combate**: init two-pass (`ShieldModule.Clear` de TODAS as unidades ANTES
    de qualquer grant), senão o Clear apaga o escudo recém-dado.
- **Invocação**: `SummonData` (stats FIXOS — nunca piso/pontos) + `SummonStatProfile` (derivação do dono) +
  `SkillModules.SpawnSummon(context, data, position)` — OU `CombatController.SpawnSummon` direto quando precisar
  de despawn/recast/posicionamento próprio (ver `DarulitoSupreme`, que invoca o totem atrás e substitui o
  anterior). As skills da invocação TAMBÉM são finas (`LittleRickAttack`, `TotemBolivianoSupreme`). Totem sem
  ataque: caps `UseSupreme`, efeitos do supremo em `OnCast`.
- **TRANSFORMAÇÃO / formas (stances)** — sistema genérico: `CharacterData.forms`
  (`CharacterForm{key, baseSkill?, supremeSkill?, animationProfileKey}`) + `SkillsModule.SetForm(chave)` troca
  ATAQUE+SUPREMO ativos + perfil de animação. Skill nula na forma = mantém a base. `SetForm` é só mecânica;
  QUANDO trocar + bônus por forma = passiva bespoke `ModularPassive`. Modelo pronto: **`VelaPassive`** (acumula
  stack por crítico via hook `onCriticalHit`; ao atingir o limiar por rank, `SetForm` + troca a condição-bônus
  da forma via `SkillModules.Status`; toca o clipe `transformAnimationKey`). ⚠️ **Adiar a troca p/
  `onAfterAttack`** (nunca `SetForm` dentro do OnHit — destruiria a skill em execução). Bônus de forma por rank
  via `RankScaledStatMod[]` (stat + asPercent + valuesByRank).
- **Hook de crítico**: `PassiveHooks.onCriticalHit` (disparado pelo `DamageApplier` em `result.isCritical`;
  riders on-hit não critam). **Rank**: melhora SÓ a passiva (`RankTiers` / `RankScaledStatMod`).

⚠️ **Artefatos são um sistema SEPARADO e AINDA data-driven** — NÃO confundir com skills. `ArtifactData` guarda
`SkillEffect[]` (on-hit / on-battle-start / summon-on-hit) com `TargetQuery` no asset (`Data/Items/*`). Os
`SkillEffect` (`DealDamageEffect`, `GainEnergyEffect`, `GrantShieldEffect`, `RedirectAllyDamageEffect`, etc. em
`Effects/Skill/`) são o vocabulário VIVO dos itens — **não migrar nem remover**. Skills = módulos em código;
artefatos = `SkillEffect` SO.

### Auditoria de modularidade (obrigatória, ao codar cada mecânica)

Antes de escrever `if <personagem>` ou um campo específico demais, pare e avalie:

1. **Cabe num módulo existente?** Se um `SkillModules.*` já cobre o efeito, componha — não escreva C# novo.
   Confira o mapa técnico fechado no Bloco C da entrevista.
2. **É um novo módulo genérico ou uma exceção disfarçada?** Se precisa de código novo, ele tem de ser
   **parametrizado e reutilizável por qualquer unidade** (ex.: um `SkillModules.Shield(scalings)` genérico,
   não "escudo do Darulito"). Nome e assinatura descrevem o COMPORTAMENTO, não o herói.
3. **Já existe algo hardcoded que este personagem expõe?** Se, para atender a spec, você encostou
   em lógica individual/duplicada que já estava no código (número mágico, ramo por nome, API
   paralela), **é sinal de dívida de modularidade**. Não empilhe mais um caso — **proponha ao
   usuário** generalizar para um sistema reutilizável (efeito/módulo/config), com o trade-off curto
   (CLAUDE.md: perguntar antes de refatorar). Só siga com o atalho se o usuário recusar.
4. **Registre o achado**: toda oportunidade de generalização vira item no relatório final (Fase 5/6),
   mesmo que adiada.
5. **Verifique adversarialmente** (recomendado p/ mecânica não-trivial): rode um workflow de agentes
   (timing / regressão de código compartilhado / fidelidade dos assets à spec). Numa execução real pegou um
   BUG sistêmico de timing de escudo (order-dependent) que passaria batido no olho.

Regra prática: se dois personagens quaisquer precisariam do mesmo código, ele pertence a um bloco
genérico — não ao personagem.

### Texto das habilidades (DSL de descrição) — obrigatório ao compor cada skill

Ao criar/ajustar CADA skill, escreva também o texto no formato do projeto — com escalonamentos,
keywords e números — e valide o render. Não deixe descrição em prosa solta sem os valores.

**Onde o texto aparece**: como cada skill agora é UMA classe fina (sem sub-efeitos), a `description` da
SKILL é o texto inteiro e os `{v:campo}` leem os campos numéricos DA PRÓPRIA classe fina por reflexão.
(A concatenação de `description` de sub-efeitos via `DescribableEffects`/`OnHitEffects` só vale para
**artefatos** — `ArtifactData` + `SkillEffect[]` —, não para as skills modulares.)

**Tags** (ver `AbilityTextFormatter`):
- `{kw:chave}` — palavra-chave do `KeywordCatalog` (cor + explicação lateral). Ex.: `{kw:dano_magico}`,
  `{kw:incendiar}`. Use SÓ chaves que existem (confira o catálogo); chave inexistente renderiza o texto cru.
- `{v:campo:Stat:fmt}` — valor lido por REFLEXÃO de um campo da própria skill/efeito, colorido pela cor do Stat.
  - `campo` = caminho de reflexão para um campo da CLASSE FINA: direto (`attackScaling`, `damageAttackScaling`,
    `stunChance`, `stunDuration`, `healScaling`, `magicScaling`, `cleanseCount`) ou com índice de array/struct.
    Anda na hierarquia e **enxerga os campos `[SerializeField] private` da classe fina e das bases** (testado).
  - `Stat` = nome do `StatType` só p/ COR (ex.: `MagicalPower`, `Attack`); vazio (`::`) = sem cor.
  - `fmt`: `pct` (30→"30%", **default**), `pctFrac` (0.3→"30%"), `flat` (30→"30"), `sec` (5→"5s"),
    `invPctFrac` (0.6→"40%"), `absPctFrac` (-0.3→"30%").
- `{shift:...}` — trecho exibido só com SHIFT: prosa fora, números/detalhes dentro.

**Convenção de valor (o que o campo guarda × o fmt certo)** — a maior fonte de erro. ⚠️ **MUDOU no SO-thin:**
os campos das classes finas guardam **MULTIPLICADOR** (`attackScaling = 1.5`), não percent (150). Logo:
- Campo de scaling (multiplicador, ex.: `attackScaling`/`damageAttackScaling`/`healScaling`) → fmt **`pctFrac`**.
  `{v:attackScaling:Attack:pctFrac}` (1.5) → "150%" (testado). ⚠️ `pct` daria "2%" — errado p/ multiplicador.
- `chance` guarda 0..1 → `pctFrac`. `{v:stunChance::pctFrac}` (0.85) → "85%".
- `duration`/tempo → `sec`; contagem/`flat` cru → `flat` (`{v:cleanseCount::flat}` → "2").
- Percent negativo (debuff, ex.: `defenseReductionPercent = -0.3`) → `absPctFrac` (mostra "30%").
- Stats percentuais base 100 (penetração/efetividade/lifesteal/crit…) guardados como bruto: `flat` + `%` literal
  no texto. Ex.: `ignora {v:penetrationBonus::flat}% da Defesa Mágica`.

**Exemplo verificado (RikurbyAttack, `attackScaling=1.5`)** — use como molde:
```
Golpe causando {kw:dano_fisico} ({v:attackScaling:Attack:pctFrac} do Ataque).   → "Golpe causando Dano Físico (150% do Ataque)."
```
> As descrições dos personagens já migrados foram HARDCODED (números fixos) por atalho — ao criar/ajustar uma
> skill, prefira o DSL `{v:campo:Stat:fmt}` (a mecânica funciona nas classes finas, como acima).

**Validar SEMPRE** (não confie no template no olho): rodar via `execute_code`
`AbilityTextFormatter.Format(template, obj, KeywordCatalog, StatDefinitionCatalog, shift:true)` — `obj` = a
SKILL (p/ a desc da skill) e o EFEITO (p/ a desc do efeito) —, remover as tags de cor por regex e LER o
resultado. Conferir: número certo, keyword existe, sem `{...}` cru sobrando, em SHIFT e sem SHIFT. PT-BR no
texto; keys/identificadores em inglês.

Cadastro:
1. `CharacterData` (ou `SummonData`): id/displayName/lore, 9 clipes, sprites (defaultSprite/
   cardPortrait — gerar `portrait`/`card-art` na Ludo se pedirem), tipos, `statPoints`
   (MaxEnergy NÃO — vem do `energyCost` do supremo), skills, `visualScale`, `maxSummons`.
2. Ícones das skills (Ludo `image_type="icon"`) + **texto das habilidades na DSL do projeto**
   (ver "Texto das habilidades" acima): descrição da skill + descrição de cada efeito, com
   escalonamentos/keywords/números, validadas via `AbilityTextFormatter`.
3. **`GameDataCatalog` é curado**: adicionar SÓ o personagem jogável; invocações FORA
   (não rodar auto-scan).

---

## Fase 5 — Validação e entrega

1. Compilação limpa (`refresh_unity scope=all` p/ arquivos novos) + `read_console`.
2. Verificação estática dos assets (refs resolvem, scalings certos) via `execute_code`.
3. **Runtime em play mode** (`execute_code` OU play-test do usuário): números do ataque/passiva/supremo
   batem com a spec; invocação deriva certo; tooltip renderiza. ⚠️ **Play-test é INSUBSTITUÍVEL** — só
   rodando aparecem tamanho, VELOCIDADE de animação, ação invisível e NREs. **Mecanismo de playback** (saiba
   pra prever antes): AnimatorOverrideController (clipes do `CharacterData` sobrescrevem placeholders por
   NOME no `VisualModule`); ataque toca via `PlayAttackAnimationForDuration(3/Speed)` (ESTICA o clipe → ação
   curta some); supremo via `PlaySupremeAnimation(Speed/animationSpeedReference)` (ref alto = lento/travado).
   - **Receita p/ dirigir combate no play mode** (o loop do jogo por FORA do fluxo é frágil — o play mode faz
     bootstrap no MainMenu, o singleton do `CombatController` some ao fim do combate, e `Time.timeScale=0`
     ANTES do `StartCombat` bloqueia o `InitializeSkills`): entrar em Play → `SceneManager.LoadScene("Sandbox")`
     → num ÚNICO `execute_code` `pool.SpawnUnit`+`SetTeam(Enemy)`+`cc.StartCombat`; screenshot por
     `manage_camera(capture_source=game_view, include_image=true)`. **Transformação:** dirigir `sk.NotifyCriticalHit()`×N
     + `sk.Hooks.InvokeAfterAttack()` e capturar o antes/depois (idle humano → idle tigre confirma a troca de
     perfil). O clipe de transformação toca via `PlayExtraAnimation(transformAnimationKey)` na passiva.
4. Screenshot de validação em cena (memória `ui-screenshot-verification`) quando fizer sentido.
5. Entrega: `## Resumo das alterações` + `## Aplicação no Editor` + defaults propostos +
   **flag do que exige play-test manual** (animações/feel são sempre validação visual do usuário).
6. **Relatório de custo Ludo + melhoria da skill** (SEMPRE, ao fim de toda geração):
   - **Estimativa de créditos/tokens Ludo**: some o livro-razão (princípio 8) de TODAS as gerações
     — imagens, poses, ícones, animações, VFX — **incluindo os retries**. Fórmulas de referência:
     `createImage`/`generateWithStyle` ≈ 0.5/variação; `generatePose`/`editImage`/`removeBackground`
     ≈ 0.5; `animateSprite` = segundos × taxa do modelo (blitz 1.9/s; eagle mais caro; mínimo 4s).
     Se o MCP expuser consumo/saldo real, reporte o **delta medido** ao lado da estimativa.
   - **Tabela**: fase · chamada · modelo · qtd/segundos · tentativas · créditos · subtotal → **total**.
   - **Sugestões de economia e consistência** (o ponto principal): onde os retries queimaram créditos
     e a causa-raiz; qual **pergunta na entrevista (Fase 0)** teria evitado o gasto; qual pose/preset/
     parâmetro teria acertado de primeira; que ajuste no próprio `SKILL.md` (prompt-base, ordem de
     fases, modelo default por clipe, checklist) faria as próximas execuções **gastarem menos e
     baterem mais com o pedido**. Liste as melhorias como itens acionáveis (e ofereça aplicá-las ao
     SKILL.md se o usuário quiser).

---

## Fase 6 — Lições Aprendidas e Autocorreção (OBRIGATÓRIA e AUTOMÁTICA no fim)

⚠️ **Rode esta etapa SOZINHO ao fim de toda execução — NÃO espere o usuário pedir** (numa execução real ela
não foi automática e o usuário teve que cobrar). Revise TODO o contexto da sessão e entregue, em **bullet
points por categoria**, o que otimiza a PRÓXIMA execução da skill. Ao fim, **peça aprovação e ofereça aplicar
os ajustes no próprio `SKILL.md`** e nas memórias de sessão.

**Formato padrão da etapa (categorias fixas):**
- ✅ **O que funcionou** (padrões a manter) — ex.: composição de blocos, verificação adversarial, memória de
  sessão p/ retomar após quedas, checkpoints com o usuário.
- 🎨 **Arte/estilo** — tool/rota certos (converter a ref via `editImage`, não `generateWithStyle`), fidelidade, drift.
- ⚡ **Geração Ludo** — throttle (1–2/vez), pose-first, trava de visual na pose, mostrar por links, timeout→`getSpriteResults`.
- 🔁 **Retries desnecessários** — causa-raiz de CADA retry → como acertar de 1ª (qual pergunta da Fase 0 /
  parâmetro / pose / contagem de frames teria evitado).
- 📐 **Import/tamanho** — medir o roster (não assumir 2.7), PPU por pasta, pivot dos pés.
- 🔧 **Mecânica** — bugs sistêmicos que o personagem expôs (ex.: `animationSpeedReference=12`, two-pass do
  escudo), reuso vs código novo.
- 🔍 **Processo** — o que na própria skill/fluxo deveria mudar (INCLUSIVE se esta etapa foi pulada).
- 📒 **Custo Ludo** — o livro-razão do princípio 8 (tabela fase·chamada·qtd·créditos → total + economia possível).

Cada bullet = **causa-raiz curta + ajuste acionável**. Itens que viram edição de `SKILL.md`/memória: liste
explicitamente, peça aprovação e aplique.
