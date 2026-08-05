using DropInHeroes.Combat;
using DropInHeroes.Utils;

namespace DropInHeroes.Data
{

    /// <summary>
    /// Interface base para todos os dados do jogo.
    /// Garante que todo dado tem ID único e pode ser catalogado.
    ///
    /// ⚠️ CONTRATO DE ID — o <see cref="ID"/> é a chave estável do conteúdo.
    ///   1. Um id, uma vez definido, NUNCA muda. Renomear o asset não pode mudá-lo.
    ///   2. O id é gerado do nome do asset apenas na PRIMEIRA validação, enquanto está vazio.
    ///   3. Dois assets da mesma categoria nunca compartilham id.
    /// Ids atravessam saves, catálogos e (no futuro) formação enviada pela rede: mudar um id
    /// publicado quebra dado de jogador em silêncio.
    ///
    /// Validar com <c>Tools ▸ DropInHeroes ▸ Validar IDs de conteúdo</c>.
    /// </summary>
    public interface IGameData
    {
        string ID { get; }
        DataCategory Category { get; }
    }

    /// <summary>
    /// Categoria de um dado catalogável.
    ///
    /// ⚠️ Serializado como INT nos .asset — valores explícitos, mesmas regras do
    /// <see cref="Combat.StatType"/>: nunca reordenar, nunca inserir no meio, sempre anexar no fim.
    /// </summary>
    public enum DataCategory
    {
        Character = 0,
        Artifact = 1,
        StatTree = 2,
        Status = 3

        // Próximo valor livre: 4
    }
}