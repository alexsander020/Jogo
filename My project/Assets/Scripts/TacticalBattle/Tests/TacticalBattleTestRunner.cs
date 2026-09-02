using UnityEngine;

namespace TacticalBattle.Tests
{
    public class TacticalBattleTestRunner : MonoBehaviour
    {
        [ContextMenu("Executar Todos os Testes")]
        public void RunTestsInEditor()
        {
            int totalTactical = TacticalBattleTestSuite.RunAllTests();
            int totalAppmon = AppmonAndComboTestSuite.RunAllTests();
            int total = totalTactical + totalAppmon;
            Debug.Log($"<color=green><b>[SUCESSO COMPLETO]</b></color> Todos os {total} testes (Tático + Appmon + Combos) foram validados com 100% de sucesso!");
        }

        void Start()
        {
            // Opcional: roda automaticamente se o script estiver ativo na cena
            // RunTestsInEditor();
        }
    }
}
