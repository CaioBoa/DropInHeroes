# CLAUDE.md

Instruções gerais de operação. Aplicar a qualquer alteração de código.

---

## Antes de codificar

- Leia o contexto necessário antes de editar. Não assuma estrutura sem verificar.
- Se houver mais de uma forma razoável de implementar, e a escolha afetar arquitetura, performance ou configuração de interface, **pergunte primeiro**. Apresente 2–3 opções com trade-off curto.
- Se o pedido conflita com convenção existente no projeto, aponte o conflito antes de executar.
- Decisões triviais (rename, fix óbvio, ajuste local): execute sem perguntar.
- Não invente caminhos, nomes de campos, métodos ou assets. Se algo precisa ser criado, marque explicitamente como novo.

---

## Princípios de código

### Coesão e responsabilidade
- Um arquivo, uma responsabilidade. Se uma classe acumula seções não relacionadas, divida.
- Hubs delegam; não implementam lógica de detalhe.
- Componentes do framework apenas onde o ciclo de vida ou a interface visual exige. Lógica pura como classe comum.

### Qualidade e legibilidade
- Nomes descritivos > comentários. Comente apenas o **porquê** (constraint, invariante, workaround), nunca o **quê**.
- Sem documentação que apenas repete o nome do método.
- Sem `// TODO` órfão — placeholder explícito ou remover.
- Early return > aninhamento profundo. Guard clauses no topo.
- Magic numbers → constantes nomeadas ou config externalizada.
- Reaproveite tipos/enums/estruturas existentes em vez de criar paralelos.

### Mínimo de linhas, máximo de clareza
- Composição > herança rasa.
- Expression-bodied para getters/setters triviais.
- Não duplique APIs (legacy + nova) sem motivo concreto.
- Operações dicionário: `TryGetValue` em vez de `ContainsKey` + indexer.
- Não introduza camadas de abstração antes de existirem dois ou três casos reais.

### Performance no hot path
Em qualquer código que rode por frame ou em laço quente:
1. Não alocar memória. Sem `new` evitável, sem boxing, sem LINQ.
2. Lookups de componentes/dependências cacheados em inicialização. Nunca em loop por frame.
3. Parâmetros de animação por hash, não por string.
4. Queries de física/colisão na variante non-alloc com buffer reutilizado.
5. Listas/coleções reutilizadas (`Clear`) em vez de instanciar por frame.
6. Pooling para qualquer coisa instanciada repetidamente.
7. `SetActive` em objetos/cenas inativos, em vez de carregar/descarregar em runtime.
8. Reflexão apenas em editor ou em init única e cacheada. Nunca em runtime quente.
9. Logs com interpolação só atrás de guard ou compilação condicional em hot paths.

### Boas práticas de engine
- Não misturar APIs 2D e 3D no mesmo objeto.
- Câmera em `LateUpdate`, movimento físico em `FixedUpdate`, input/animator/lookups em `Update`.
- Use o sistema de input atual da engine, não o legado.
- Eventos para acoplamento fraco; sempre desinscrever em `Cleanup`/`OnDestroy`/`OnDisable`.
- Configs ajustáveis por designer expostos como assets de configuração, não hardcoded.
- Singletons só onde realmente são necessários; checagem padrão em `Awake`; não acessar outros singletons em `Awake`.
- Campos serializados como `[SerializeField] private` com agrupamento e tooltip quando o nome não bastar; `[Range]` em valores normalizados.

---

## Convenções de estilo
- Mantenha o idioma usado no resto do código para comentários e logs. Identificadores em inglês.
- Siga o estilo (naming, indentação, ordem de membros) já presente nos arquivos próximos do que está editando.
- Não reformate código não relacionado à mudança. Diffs pequenos e focados.

---

## Comunicação obrigatória em cada alteração

### (a) Resumo do que foi alterado
Ao fim da resposta, em português, listar arquivos tocados com motivo:

```
## Resumo das alterações
- <arquivo:linha> — <o que mudou e por quê, em uma frase>
- (novo) <arquivo> — <propósito>
- (removido) <arquivo> — <motivo>
```

Foque no **porquê**. O **o que** o diff já mostra.

### (b) Guia de mudanças na interface (Editor)
Se a alteração exigir qualquer ação manual no Editor — atribuir referência em Inspector, criar asset de configuração, configurar prefab/cena, ajustar componente, definir tag/layer, criar evento de animação, popular array, etc. — liste passo a passo o que o usuário precisa fazer.

Se nada for necessário, escrever explicitamente:

```
## Aplicação no Editor
Nenhuma — mudanças puramente de código.
```

---

## Validação e honestidade
- Nunca afirme "feito" sem ter validado o que dá para validar.
- Mudanças que dependem de runtime, UI, animação ou física devem vir com a observação explícita "não testado em runtime — necessário rodar para validar".
- Ao remover ou renomear campo serializado, alertar sobre referências quebradas em prefabs/cenas e instruir religação.
- Ao tocar área desconhecida, prefira leitura adicional a suposição.

---

## Escopo das alterações
- Não adicionar funcionalidades além do que a tarefa pede.
- Não introduzir validação/error handling para cenários que não podem ocorrer. Validar apenas em fronteiras (input do usuário, APIs externas).
- Não preservar compatibilidade com código legado quando se pode simplesmente atualizar o código.
- Não tocar bibliotecas de terceiros nem pastas geradas pela engine.
- Não criar arquivos de documentação/planejamento adicionais a menos que solicitado.
- Se houverem oportunidades de refatoração identificadas durante uma implementação, perguntar ao usuário se deve implementa-la
