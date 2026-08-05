using System;
using System.Collections.Generic;
using UnityEngine;

namespace DropInHeroes.Data
{

    /// <summary>
    /// Uma build de personagem = 1 artefato + pontos alocados na árvore de stats compartilhada.
    /// A default (sempre "Padrão", sem nome próprio) é autorada no <see cref="CharacterData"/>; as 3
    /// custom são nomeadas pelo jogador e salvas em disco por <see cref="BuildStore"/>. Viewer-first:
    /// descreve a build para o preview; a aplicação em combate é fase 2.
    /// </summary>
    [Serializable]
    public class CharacterBuild
    {
        // Nome só das builds CUSTOM (o jogador batiza). A default não usa — a UI mostra "Padrão".
        // Oculto do Inspector: só a CharacterData.defaultBuild é desenhada, e ela não tem nome próprio.
        [HideInInspector] public string label = string.Empty;
        [Tooltip("ID do ArtifactData equipado (via DataManager.GetArtifact). Vazio = sem artefato.")]
        public string artifactId = string.Empty;
        [Tooltip("Pontos alocados por nó na StatTreeData compartilhada. Autore pela ferramenta (Stat Tree Editor ▸ modo Build).")]
        public List<TreeAllocation> allocations = new List<TreeAllocation>();
    }

    /// <summary>Pontos que a build investiu num nó da árvore. Cada ponto = statPointValue do stat do nó.</summary>
    [Serializable]
    public struct TreeAllocation
    {
        public string nodeId;
        public int points;
    }
}
