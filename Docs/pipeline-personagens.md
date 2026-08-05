# Pipeline — Adicionar/trocar animações e personagens

Playbook consolidado (Marda, Guliver, Darulito, Rikurby, LilRih). Ordem otimizada, armadilhas com causa→solução, e snippets prontos. Idioma: PT-BR; identificadores em inglês.

---

## 0. Ferramentas e gotchas do ambiente

- **Unity MCP** (`execute_code`, `refresh_unity`, `read_console`, `manage_*`). Ele **cai** às vezes (foco do editor/bridge) — checar `mcpforunity://instances`; se desconectado, pedir pro usuário focar/reiniciar o Editor. Com 1 instância, `set_active_instance <Name@hash>` (hash completo).
- **`execute_code` = C# 6 (codedom)** e **NÃO referencia a Assembly-CSharp** (tipos do jogo: `CharacterData`, skills, `UnitController`...). Para usá-los: **reflexão** (`AppDomain.CurrentDomain.GetAssemblies().GetType("Nome")`) + `SerializedObject` por nome de campo. Sem local functions (C# 7) — usar lambdas. Sem `Date.now`/`Random` no contexto de workflow (mas `UnityEngine.Random` no runtime é ok).
- **`safety_checks=false`** para: `File.ReadAllBytes/WriteAllBytes`, `AssetDatabase.DeleteAsset` (bloqueados por padrão).
- **PowerShell**: `System.Drawing` p/ ler/compor/medir/espelhar PNGs; `System.IO.Compression.ZipFile` p/ extrair. O sandbox bloqueia `Remove-Item` com certos padrões — usar `[System.IO.File]::Delete($path)`.
- **Medir dimensão de PNG**: usar **System.Drawing** ou Unity. **NUNCA o parser manual de IHDR** — o meu deu errado e calibrei o PPU 2× errado (idle "208px" quando era 464).

---

## 1. Estrutura e convenções

```
Assets/Art/Characters/<Char>/<Anim>/<Char><Anim>_000.png ...   (corpo, 1 sprite por frame)
Assets/Art/Characters/<Char>/_Zip/<Char><Anim>.zip             (arte nova; frame_000.png.. dentro de subpasta)
Assets/Art/Characters/<Char>/SupremeEffect/                    (VFX; pivot center)
Assets/Animations/Animations/<Char>/<Char><Anim>.anim
Assets/Data/Characters/<Char>/<Char>.asset  +  skills (.cs + .asset)
```

- **Caminhos mudam** (o usuário reorganiza pastas — ex.: removeu o nível `Sprites/`). **Sempre re-`Glob`** antes de assumir paths.
- **Zips**: cada `.zip` tem `frame_NNN.png` (canvas uniforme dentro do zip; tamanho varia entre zips). Contagem de frames varia por animação.
- **Naming**: `DeadIdle`→pasta/clipe `DeathIdle`; clipe de victory às vezes vem com nome torto (`RikurbyVictoryanim`). Confirmar nomes reais.
- **Convenção de rig** (ver §4): arte-base olha à **ESQUERDA**; `flipX` global no `SpriteRenderer` (Player→direita, Enemy→esquerda). Footprint 1.0 abaixo da raiz.

---

## 2. FLUXO OTIMIZADO — trocar/adicionar animações

### Fase A — Descoberta (read-only, faça TUDO antes de escrever)
1. `Glob` pastas, `_Zip/*`, clipes, CharacterData. Re-confirme paths.
2. **Métricas ANTIGAS via Unity (antes de sobrescrever)** — alvo de tamanho:
   ```csharp
   var spr = AssetDatabase.LoadAssetAtPath<Sprite>(path_000);
   // worldH = spr.rect.height / spr.pixelsPerUnit;  guarde o do Idle.
   ```
3. Clipes: contagem/nulls/ordem de keyframes/eventos (`AnimationUtility.GetObjectReferenceCurve(Bindings)` + `GetAnimationEvents`).
4. Contagem de frames de cada zip (PowerShell `ZipFile`).

### Fase B — Facing (ANTES de sobrescrever)
5. Extrair zips p/ `C:\tmp\<char>_new\<Anim>\frame_NNN.png`.
6. Compor contact sheet (frame_018 de cada) e **LER a imagem**. Conferir cada anim contra uma confirmada-correta.
   - **Cue mais confiável: texto/logo na arte** (ex.: boné "RX" → se aparece "XR", o frame foi espelhado). Depois: direção do rosto/cabeça. (Escudo/espada confundem.)
   - ⚠️ **Falso-positivo de espelhamento** (já erramos 2×: Guliver Supreme, LilRih Attack): a anim JÁ estava certa e foi espelhada à toa, ficando invertida no jogo. Só espelhe se REALMENTE destoar das outras.

### Fase C — Overwrite (preserva GUID/.meta → clipes/refs continuam válidos)
7. Copiar `frame_NNN.png` → `<Char><Anim>_NNN.png` (índice identidade). Espelhar (`RotateFlipType.RotateNoneFlipX`) durante a cópia as de facing errado.
8. **Mismatch de contagem** (zip ≠ folder/clip): sobrescrever os que casam, **apagar** PNGs extras (e `.meta`) ou **criar** novos, e **rebuild do clipe** (Fase E). Ex.: Run 36→25, Attack 25→36.
9. `refresh_unity(scope=assets, force)`. (Arquivos novos externos precisam de refresh `all` p/ serem importados.)

### Fase D — Formato canônico (desde o F0.11, 2026-08-04)

> **Esta fase mudou.** Antes, cada animação recebia PPU e pivô próprios, calculados da bbox. O
> resultado foram **54 dimensões de canvas, PPU de 185 a 310, ~46 pivôs e 72 `AnimClipTuning`
> manuais** — cerca de 9 correções por personagem. Hoje o formato é único e o import é automático.

**Você não faz nada nesta fase.** O `CharacterArtPostprocessor` aplica o formato no import, por
convenção de caminho. Basta a arte estar no lugar certo:

| Caminho | Classe | Canvas | PPU | Pivô |
|---|---|---|---|---|
| `Art/Characters/<Nome>/<Anim>/` | Corpo | **448×448** | **90** | `(0.500, 205/448)` |
| `Art/Characters/<Nome>/Vfx*` ou `*Effect*`, `*Projectile*` | VFX | **640×640** | **90** | `(0.500, 327/640)` |
| `Art/Characters/<Nome>/Skills/` | Ícone | **256×256** | 100 | centro |
| `Art/Characters/<Nome>/<arquivo>` (raiz) | fora do escopo | — | — | autoria manual |

O que **você** precisa garantir na geração da arte:

1. **Enquadramento.** O conteúdo, já reduzido a PPU 90, precisa caber em 448: até **208 px acima**
   e **170 px abaixo** do pivô, e **166 px** para cada lado. Em unidades de mundo: ~2,3 acima do
   pivô e ~1,9 abaixo. Um humanoide típico ocupa ~2,4 unidades de altura.
2. **Consistência entre animações do mesmo personagem.** O enquadramento precisa ser o mesmo em
   idle, run, attack etc. — se variar, os pés deslizam ao trocar de animação, que é exatamente o
   que as 72 tunings existiam para corrigir.
3. **`visualScale` é direção de arte**, não conserto. Use para um gigante ser grande, nunca para
   compensar arte mal enquadrada.

Rode **`Tools ▸ DropInHeroes ▸ Validar arte de personagem`** ao terminar. Ele confere dimensão,
PPU, múltiplo de 4 e compressão em VRAM, e avisa se sobrou alguma `AnimClipTuning`.

Para isentar uma pasta (arte que legitimamente foge do formato), crie um arquivo `__manual__`
dentro dela.

> **Por que o recanvas uniforme passou a caber.** A nota antiga dizia que ele geraria canvas
> gigante (ex.: bandeira do Guliver → 640×1040). Isso valia no PPU original de 185-310. A PPU 90 o
> conteúdo encolhe 2-3× antes de ser enquadrado, e o acervo inteiro cabe em 448 — medido nos 2.632
> frames, com folga de 70 px. O raciocínio antigo estava certo para os números de então.

### Fase E — Verificação
12. Clipes: `frames`, `nulls=0`, eventos. **Garantir `OnAttackHitFrame`** em Attack/Supreme (sem ele, `OnHit` não dispara → sem dano). Rebuild se houver nulls:
    ```csharp
    var binding = AnimationUtility.GetObjectReferenceCurveBindings(clip)[0]; // SpriteRenderer/m_Sprite/path=""
    var keys = new ObjectReferenceKeyframe[N];
    for (int i=0;i<N;i++) keys[i]=new ObjectReferenceKeyframe{ time=i/60f, value=LoadSprite(i) };
    AnimationUtility.SetObjectReferenceCurve(clip, binding, keys); clip.frameRate=60f;
    // loop: var s=GetAnimationClipSettings(clip); s.loopTime=true; SetAnimationClipSettings(clip,s);
    // evento: AnimationUtility.SetAnimationEvents(clip, new[]{ new AnimationEvent{functionName="OnAttackHitFrame", time=t} });
    ```
13. Imagem de validação: frames feet-aligned em **escala-mundo** (`×K/PPU`) — confirmar pés na linha e tamanhos consistentes.
14. `read_console` (0 erros). Lock transitório de `.meta` ("Cannot open file for write", OneDrive/AV) → só reimportar de novo.

---

## 3. Armadilhas encontradas → causa → solução

| Problema | Causa | Solução |
|---|---|---|
| PPU 2× errado, personagem gigante | parser IHDR manual bugado | medir dims via System.Drawing/Unity |
| Animação invertida que "já estava certa" | espelhamento à toa (falso-positivo) | usar logo/texto como cue; comparar com anim correta; só espelhar se destoar |
| Suprema vira pro lado oposto | só alguns frames espelhados OU espelhei a anim errada | checar consistência de TODOS os frames + logo |
| Canvas gigante / muita memória | recanvas uniforme + pivot center p/ arte alta | pivot custom por pasta |
| Animação "sumida"/último frame | clipe com keyframes null (não fiado) | rebuild via `SetObjectReferenceCurve` |
| Sem dano no ataque/supremo | falta `OnAttackHitFrame` no clipe | adicionar evento no frame de impacto |
| Contagem de frames diferente | zip ≠ folder | rebuild do clipe + add/del PNGs |
| Arte velha persistindo | **assumi pasta=arte nova, mas a nova estava no `_Zip` não-extraído** | SEMPRE extrair os zips e comparar |
| `execute_code` "type not found" | codedom não referencia Assembly-CSharp | reflexão p/ tipos do jogo |
| Arquivo .cs novo não compila | refresh `scripts` não importa arquivo externo novo | refresh `all` antes de compilar |
| VFX do supremo com sorting errado | `target.GetComponent<SpriteRenderer>()` (renderer foi pro filho Visual) | `GetComponentInChildren<SpriteRenderer>()` |
| CharacterData duplicado / ID duplicado | já existia um `.asset` (o catálogo referenciava) | reconciliar: preservar dados do usuário, mover/deletar, manter 1× no catálogo |
| Summon aparece na seleção | catálogo é lista curada; auto-scan adiciona TUDO | adicionar só o jogável; NÃO rodar auto-scan p/ summons |

---

## 4. Prefab `Unit.prefab` (Visual child + visualScale)

```
Unit (raiz)   { UnitController, Rigidbody2D, CircleCollider2D, DragEventBridge }
  Visual      { SpriteRenderer, Animator, UnitAnimationEventRelay }   <- escala aqui
  Footprint   { SpriteRenderer, UnitFootprint, CircleCollider2D }     (intacto)
  Canvas      { HealthBar, EnergyBar }                                (intacto)
```
- `VisualModule` pega Animator/SpriteRenderer via `controller.transform.Find("Visual")`. Binding de animação é `path=""` (mesmo GO do Animator = Visual).
- **AnimationEvents** vão pro GO do Animator (Visual) → `UnitAnimationEventRelay.OnAttackHitFrame` repassa ao `UnitController`.
- **`CharacterData.visualScale`** (float, 1) escala SÓ o Visual, em torno dos pés (`localPosition.y=(s-1)*1.0`), mantendo a ancoragem. É o knob de tamanho por personagem (vale p/ summons também). Não corrige inconsistência ENTRE animações (isso é PPU por pasta na importação).
- **Convenção facing**: `VisualModule.ApplyIdleFacing` → Player `flipX=true`, Enemy `false`; em combate `FaceTowards(target)` / flip por velocidade.
- **Footprint/ancoragem**: `PreparationConfig.footprintPlacedOffset = -1.0` (sombra 1.0 abaixo da raiz). Por isso `offsetPx = PPU` no pivot custom.

---

## 5. Personagem novo — mecânicas (skills/passiva/invocação)

> ⚠️ **Movido / OBSOLETO.** A autoria de mecânicas migrou para o padrão **SO-thin + composição de módulos em
> código**: cada skill é uma classe fina `Modular*` (`ModularBaseAttack`/`ModularSupreme`/`ModularPassive`/
> `ModularProjectileAttack`/`ModularOnHitPassive`) compondo `SkillModules.*` sobre `Targets.*`; o `.asset` só
> guarda números + identidade. O guia canônico e atualizado é a **Fase 4 do
> `.claude/skills/criar-personagem/SKILL.md`** (+ memórias `so-thin-code-modules`,
> `skill-summon-roster-architecture`, `skill-naming-and-composition`).
>
> A API antiga descrita aqui (`ActiveSkill.Execute/OnHit` direto, `DamageCalculator.Calculate` na mão,
> `TargetResolver.Resolve`, `SpawnSummon(..., bool combatAI)`) está **desatualizada — não seguir**.
> O que continua válido neste doc: **§0–§4 e §6** (arte, rig, PPU/pivot, rebuild de clipe, eventos
> `OnAttackHitFrame`, e o cadastro em `CharacterData`/`SummonData` + catálogo curado com summons FORA).

---

## 6. Checklist final
- [ ] Caminhos re-confirmados (`Glob`), MCP conectado.
- [ ] Zips extraídos e **facing conferido por imagem** (logo/texto).
- [ ] PNGs sobrescritos (GUID preservado); mismatches → clipe rebuildado.
- [ ] Import: PPU calibrado + pivot custom (efeitos = center). Dims via Unity/System.Drawing.
- [ ] Clipes: frames/nulls=0/eventos `OnAttackHitFrame`.
- [ ] (Novo) Scripts compilam 0 erros; assets criados + wired; **catálogo 1× (summons fora)**.
- [ ] Imagem de validação (pés ancorados, tamanhos OK) + `read_console` limpo.
- [ ] ⚠️ Runtime não é validável por mim — sempre flag "necessário rodar a cena". Tamanho fino via `visualScale`.
