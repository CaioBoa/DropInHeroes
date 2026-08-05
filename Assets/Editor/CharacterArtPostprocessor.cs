using UnityEditor;
using UnityEngine;

namespace DropInHeroes.EditorTools
{

    /// <summary>
    /// Impõe o formato canônico (<see cref="CharacterArtFormat"/>) em todo PNG importado sob
    /// <c>Assets/Art/Characters/</c>, por convenção de caminho.
    ///
    /// Existe para que a padronização conquistada no F0.11 não se perca com o roster crescendo:
    /// antes dela, cada animação nova exigia ajuste manual de PPU, pivô e escala — 9 correções
    /// por personagem, ~477 projetadas para o roster alvo. Aqui isso custa zero.
    ///
    /// Roda no PRÉ-import, então vale para arte nova e para reimport. Não redimensiona o arquivo
    /// em disco — só o import; o enquadramento em si é responsabilidade da pipeline de geração
    /// (ver Docs/pipeline-personagens.md).
    ///
    /// Escapatória: crie um arquivo <c>__manual__</c> na pasta para isentá-la.
    /// </summary>
    public class CharacterArtPostprocessor : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            var classe = CharacterArtFormat.Classificar(assetPath);
            if (classe == CharacterArtFormat.Classe.Fora) return;
            if (CharacterArtFormat.IsentoPorMarcador(assetPath)) return;

            var ti = (TextureImporter)assetImporter;

            var st = new TextureImporterSettings();
            ti.ReadTextureSettings(st);
            st.textureType = TextureImporterType.Sprite;
            st.spriteMode = (int)SpriteImportMode.Single;
            st.mipmapEnabled = false;
            st.readable = false;
            st.alphaIsTransparency = true;
            st.spritePixelsPerUnit = CharacterArtFormat.PpuDe(classe);

            var pivo = CharacterArtFormat.PivoDe(classe);
            st.spritePivot = pivo;
            // Ícone fica no centro (alinhamento nomeado); mundo usa pivô customizado nos pés.
            st.spriteAlignment = classe == CharacterArtFormat.Classe.Icone
                ? (int)SpriteAlignment.Center
                : (int)SpriteAlignment.Custom;
            ti.SetTextureSettings(st);

            // Um rect de sprite herdado de uma dimensão antiga sobrevive ao reimport e gera
            // "rect lies (partially) outside of texture". Descartar é o certo em modo Single.
            ti.spritesheet = new SpriteMetaData[0];

            var ps = ti.GetDefaultPlatformTextureSettings();
            ps.maxTextureSize = CharacterArtFormat.CanvasDe(classe);
            ps.textureCompression = TextureImporterCompression.Compressed;
            ti.SetPlatformTextureSettings(ps);
        }
    }
}
