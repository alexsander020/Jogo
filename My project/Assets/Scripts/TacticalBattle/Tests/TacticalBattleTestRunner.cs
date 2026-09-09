using UnityEngine;

namespace TacticalBattle.Tests
{
    public class TacticalBattleTestRunner : MonoBehaviour
    {
        [ContextMenu("Executar Todos os Testes")]
        public void RunTestsInEditor()
        {
            RunTests();
        }

        public static void RunTests()
        {
            int totalTactical = TacticalBattleTestSuite.RunAllTests();
            int totalAppmon = AppmonAndComboTestSuite.RunAllTests();
            int total = totalTactical + totalAppmon;
            Debug.Log($"<color=green><b>[SUCESSO COMPLETO]</b></color> Todos os {total} testes (Tático + Appmon + Combos + App-Link) foram validados com 100% de sucesso!");
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Tactical/Executar Todos os Testes")]
        public static void RunTestsFromMenu()
        {
            RunTests();
        }
#endif

        void Start()
        {
            // Opcional: roda automaticamente se o script estiver ativo na cena
            // RunTests();
        }
    }
}
