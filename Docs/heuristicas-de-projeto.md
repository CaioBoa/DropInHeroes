# Heurísticas de projeto — DropInHeroes

> **O que é este documento.** As normas que valem em **toda sessão de trabalho**, independente da fase. Enquanto `Docs/README.md` descreve *como o projeto é* e `Docs/design/roadmap.md` descreve *para onde ele vai*, este arquivo descreve *como se trabalha nele*.
>
> **Para o assistente:** leia este arquivo antes de escrever qualquer plano de fase ou tocar em código. Uma decisão de plano que viole uma invariante da §2 está errada, mesmo que pareça razoável isoladamente.
>
> **Para o dev:** este arquivo é a memória externa do projeto. Quando uma decisão for tomada numa sessão e valer para as próximas, ela vem para cá.

---

## 1. Ordem de leitura ao iniciar uma sessão

| Ordem | Arquivo | Por quê |
|---|---|---|
| 1 | `CLAUDE.md` (raiz) | Regras de operação e comunicação obrigatória |
| 2 | `Docs/README.md` | Arquitetura, glossário, padrões transversais |
| 3 | `Docs/heuristicas-de-projeto.md` | Este arquivo — normas de trabalho |
| 4 | `Docs/design/roadmap.md` | Fase atual e decisões já fechadas |
| 5 | O plano específico da `F.x` em curso, em `Docs/plans/` | Escopo da sessão |
| 6 | `Docs/pipeline-personagens.md` | Só se a sessão tocar arte ou animação |

**Nunca confie num snapshot de estado sem verificar a data.** O `Docs/README.md` §6 tem um snapshot datado e já foi marcado como parcialmente desatualizado uma vez. Se a data estiver velha e a afirmação importar para a decisão, **re-verifique via Unity MCP antes de agir**.

---

## 2. Invariantes — quebrar qualquer uma destas é bug, não escolha

Estas são load-bearing. Algumas quebram o build silenciosamente.

1. **Namespaces `DropInHeroes.*` sempre BLOCK-scoped.** Namespace *file-scoped* quebra silenciosamente o importador de scripts do Unity 6000.4.x — os componentes viram "missing script" em prefabs e cenas, sem erro de compilação. Imposto por `.editorconfig` e `Assets/csc.rsp`.
2. **Zero código por personagem.** Nenhum comportamento de combate pode referenciar um personagem específico. Módulo novo só entra se for genérico (sem nome de personagem no código) e utilizável por 2-3 personagens hipotéticos.
3. **SO-thin:** muda **O QUE** → código; muda **QUANTO** → ScriptableObject. O `.asset` carrega só números (scaling, intensidade por rank, duração, chance). Estrutura (alvo, stat, tipo, quais módulos, ordem) mora em código.
4. **Módulos de unidade são classes C# puras**, não MonoBehaviours. Componentes só onde o ciclo de vida ou a interface visual exigem.
5. **Stats e dano alterados por modifier id-keyed reversível.** Nunca valor cru. Reversão limpa por id.
6. **`[RuntimeInitializeOnLoadMethod(SubsystemRegistration)] ResetStatics`** em toda classe com estado estático. Sem isso, Play com domain reload desabilitado retém estado velho.
7. **Pooling em vez de `Instantiate`/`Destroy` em runtime.** `OnDestroy` é só scene-unload.
8. **`async/await` + `TaskCompletionSource`** para orquestração, não coroutines. `CancellationToken` propagado.
9. **Contrato de reflexão por convenção.** `CharacterData.GetBaseStat(StatType.X)` resolve o campo `baseX`; o markup de ability-text `{v:field}` lê por reflexão. **Ambos retornam 0 em silêncio num mismatch de nome.** Renomear um stat exige renomear o campo correspondente.
10. **Stats percentuais em base 100** (crit, lifesteal, accuracy etc.): 100 = 100%. O consumo divide por 100. Scaling, modifier-percent e chance ficam fora dessa regra.
11. **Configs globais via `GameConfig.Active`**, não `Resources.Load`. Os assets vivem em `Data/Config`; o único agregador é `Data/Resources/GameConfig.asset`.
12. **Sem alocação no hot path.** Nada de `new`, boxing ou LINQ por frame. Lookups cacheados na inicialização; parâmetros de animação por hash; queries de física non-alloc com buffer reutilizado.
13. **Arte de personagem nasce no formato canônico.** Canvas, PPU e pivô são únicos por classe e vivem em `CharacterArtFormat`. O `CharacterArtPostprocessor` impõe o import; `Tools ▸ DropInHeroes ▸ Validar arte de personagem` confere o disco. **Nunca ajuste PPU, pivô ou `AnimClipTuning` para consertar enquadramento** — reenquadre a arte. `AnimClipTuning` só sobrevive para efeito artístico declarado, e `visualScale` só para direção de arte. Foi assim que o projeto acumulou 54 dimensões, 46 pivôs e 72 correções manuais.

### Invariantes novas — multiplayer (valem a partir do F1)

13. **Nenhum singleton mutável no caminho de combate.** Toda referência a estado de partida passa por contexto explícito. O host simula **2 batalhas simultâneas** numa rodada de 4 jogadores.
14. **Toda entidade de partida tem dono explícito.** Nada de resolver build, loadout ou time a partir de estado global.
15. **Cálculo de stat e dano permanece função pura de dados serializáveis.** É o que torna um servidor futuro um porte, e não uma reescrita.

---

## 3. Unity MCP — regras de operação

O Editor é a fonte da verdade. O disco pode mentir; o `Docs/README.md` pode estar velho; a memória do assistente pode estar desatualizada. **Verifique no Editor vivo.**

### Quando NÃO agir

- **Play mode ativo, ou outro agente trabalhando:** apenas aguarde e retente. **Nunca** pare o Play nem force edições — você destrói trabalho em andamento.
- **Cena com alterações não salvas:** reabrir a cena (`OpenScene`) **descarta** as edições não salvas do Editor. Cheque `isDirty` antes de reabrir e peça ao dev para salvar (Ctrl+S).

### Armadilhas conhecidas

| Situação | O que acontece | O que fazer |
|---|---|---|
| Criar um `.cs` novo com `Write` | `refresh_unity` **não** pega o arquivo; o script fica invisível ao Unity | Usar `create_script`, ou `AssetDatabase.ImportAsset` + `Refresh` via `execute_code` |
| Setar um array de object-refs (`Campo[]`) | Setar o array inteiro de uma vez **falha em silêncio** | Setar índice a índice: `campo.Array.data[i]` |
| Verificar UI por screenshot em edit mode | `game_view` **não** captura overlay UI; `scene_view` captura | Usar `scene_view`; render temporário e limpar depois |
| Mexer em Walkable / InvisibleWall | Movimento usa NavMeshPlus **assado no Editor**, não em runtime. `m_NavMeshData` nulo → "no valid NavMesh" e as unidades não andam | Rebake após qualquer alteração de navegação |
| Auditar referência quebrada com `SerializedProperty.NextVisible(true)` | **Falso negativo grave.** Arrays ocultos não são percorridos — o `m_TileAssetArray` de um Tilemap é invisível. Numa auditoria real da `TowerRun`, `NextVisible` reportou **1** quebra onde havia **86** | Sempre `Next(true)` em auditoria de integridade |
| Apagar arte que "ninguém usa" | Tilemap referencia Tile assets **por GUID**, sem erro de compilação nem de console. Apagar a pasta de tiles faz o chão sumir silenciosamente | Antes de apagar qualquer pasta de arte, cruzar os GUIDs dela contra `.unity`/`.prefab`/`.asset` vivos |
| Restaurar arquivo em pasta cujo `.meta` foi apagado | O Unity recria o `.meta` da pasta com **GUID novo** | Restaurar também os `.meta` de pasta a partir de `HEAD` |

### Verificação antes de afirmar

Nunca diga "feito" sem ter validado o que dá para validar. Mudanças que dependem de runtime, UI, animação ou física vêm com a observação explícita **"não testado em runtime — necessário rodar para validar"**.

---

## 4. Organização de código

- **`Assets/Scripts/` = código. `Assets/Data/` = asset.** Sem exceção.
- **Um arquivo, uma responsabilidade.** Se uma classe acumula seções não relacionadas, divida.
- **Hubs delegam, não implementam.** `UnitController` é um hub fino; `CombatController` orquestra.
- **Comportamento por flags, sem branch por tipo.** `UnitCapability` e `UnitTag` decidem o que a unidade faz. Summons e totens funcionam sem código específico — mantenha assim.
- **Reaproveite tipos e enums existentes** em vez de criar paralelos.
- **Não introduza abstração antes de existirem 2-3 casos reais.**
- **Não duplique APIs** (legacy + nova) sem motivo concreto. Não preserve compatibilidade com código legado quando dá para simplesmente atualizar o código.
- **Nomes descritivos > comentários.** Comente apenas o **porquê** (constraint, invariante, workaround), nunca o **quê**.
- **Sem `// TODO` órfão.** Placeholder explícito ou remove.
- **Idioma:** prosa, comentários e logs em PT-BR; identificadores, tipos e caminhos em inglês.
- **Diffs pequenos e focados.** Não reformate código não relacionado à mudança.

---

## 5. O que "profissional" significa aqui

"Código perfeito" não é critério de parada — é infalsificável, e num projeto solo é o tipo de meta que consome meses sem produzir entregável. Cada fase fecha contra critérios verificáveis:

| Dimensão | Critério |
|---|---|
| **Build** | Compila limpo, sem warning. Um clone limpo em outra máquina produz o mesmo `.exe` |
| **Testes** | Cálculo de stat e de dano cobertos. Mesma formação + mesma seed → mesmo log de dano |
| **Estado** | Nenhum singleton mutável no caminho de combate; toda entidade de partida com dono explícito |
| **Runtime** | Uma sessão de 30 minutos sem exceção no log |
| **Editor** | Nenhum "missing script"; nenhuma referência quebrada em prefab ou cena |
| **Repositório** | Working tree limpo ao fim da sessão; nada crítico untracked |
| **UI** | Escala corretamente em 1920×1080 e 1280×720; nenhum texto cortado; nenhum estado sem feedback |

---

## 6. Sequenciamento — erros que este projeto já esteve prestes a cometer

1. **Não polir tela cujos sistemas ainda vão mudar.** Polimento entra depois que os sistemas daquela área congelaram. Polir duas vezes é a forma mais cara de trabalhar.
2. **Não deletar antes do substituto estar jogável.** Vale especialmente para a skill tree e seus editor tools. Reescrever o próprio trabalho recente é a causa mais comum de desistência solo.
3. **Não refatorar sem suíte de regressão.** Refatoração cega numa base sem teste é como o projeto acumula bugs invisíveis.
4. **Custo linear no roster vem primeiro.** Qualquer coisa cujo custo cresça com o número de personagens (atlas, import, memória, texto de habilidade, UI de lista) é feita **antes** do 8º personagem, não depois do 25º.
5. **Não adicionar funcionalidade além do que a tarefa pede.** Sem validação para cenários que não podem ocorrer. Validar apenas em fronteiras.
6. **Oportunidade de refatoração encontrada no meio de uma implementação vira pergunta**, não código. Aponte e siga a tarefa.

---

## 7. Higiene de sessão

- **Commitar ao fim de cada sessão com entregável.** O projeto já esteve com 69.655 arquivos sujos sobre 5 commits — estado em que nada é reconstituível e um checkout errado apaga meses.
- **Assets binários passam por Git LFS.**
- **Ao remover ou renomear campo serializado:** alertar sobre referências quebradas em prefabs e cenas, e instruir a religação. Considerar `FormerlySerializedAs`.
- **Ao gerar arte via Ludo:** os URLs **expiram em 7 dias**. Baixar e commitar imediatamente. Travar prompt, seed e imagem de referência no repositório — sem isso, personagens futuros não casam visualmente com os existentes.
- **Toda alteração fecha com:** resumo em PT-BR dos arquivos tocados e o porquê, e o guia de aplicação no Editor (ou a declaração explícita de que nenhuma ação manual é necessária).

---

## 8. Convenções de conteúdo

- **Rig:** a arte olha para a **esquerda**; `flipX` segue disso. `Footprint` em -1.0. Pivô conforme `Docs/pipeline-personagens.md`.
- **Impacto:** o dano nasce do AnimationEvent `OnAttackHitFrame`. Todo clipe de ataque precisa do evento, ou a unidade não causa dano.
- **IDs de conteúdo são contrato**, não conveniência. Renomear um asset não pode mudar o id. Um id publicado nunca muda.
- **Ludo:** não disparar vários `animateSprite` de uma vez — rate-limita a conta inteira. 1-2 por vez, com espera; recuperar por `getSpriteResults`. Exibir imagens geradas por **link clicável**, nunca abrindo na tela do dev.

---

## 9. Manutenção deste documento

Quando uma decisão for tomada e valer para as próximas sessões:

- **Decisão de produto ou escopo** → `Docs/design/roadmap.md` §3
- **Norma de trabalho ou invariante técnica** → este arquivo
- **Fato de arquitetura** → `Docs/README.md`

Uma norma que não está escrita não sobrevive à troca de sessão.
