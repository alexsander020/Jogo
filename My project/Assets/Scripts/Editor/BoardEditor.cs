using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Board))]
public class BoardEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Board board = (Board)target;

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("Ajuste e Otimização da Grade", EditorStyles.boldLabel);

        GUI.backgroundColor = new Color(0.2f, 0.85f, 0.45f);
        if (GUILayout.Button("✂ Comprimir Grid para Área Desenhada (Compress Bounds)", GUILayout.Height(34)))
        {
            board.CompressBoundsToDrawnArea();
            EditorUtility.SetDirty(board);
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(6);
        EditorGUILayout.HelpBox(
            "COMO OCULTAR A GRADE BRANCA INFINITA DA SCENE:\n" +
            "1. As linhas brancas infinitas que cobrem o vazio são o 'Scene Grid' padrão do Unity.\n" +
            "2. Na barra de ferramentas superior da janela Scene (ao lado dos modos Shaded e 2D), clique no ícone de Grade (Grid) para desativar a grade infinita do Unity.\n" +
            "3. O componente Board manterá a grade isométrica desenhada APENAS sobre a área que você pintou com tiles!",
            MessageType.Info
        );
    }
}
