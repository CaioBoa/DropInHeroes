using UnityEditor;
using DropInHeroes.Data;

namespace DropInHeroes.Editor
{

    /// <summary>
    /// Inspector de <see cref="SummonData"/>: oculta os campos de personagem que não fazem sentido
    /// para invocação (orçamento de pontos e builds) — a base de stats de invocação é a lista de
    /// stats FIXOS declarada na própria SummonData.
    /// </summary>
    [CustomEditor(typeof(SummonData))]
    public class SummonDataEditor : UnityEditor.Editor
    {
        private static readonly string[] Hidden = { "m_Script", "statPoints", "defaultBuild" };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, Hidden);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
