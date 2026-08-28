using UnityEngine;

namespace TacticalBattle.Tests
{
    public class TacticalBattleTestRunner : MonoBehaviour
    {
        [ContextMenu("Executar Todos os Testes")]
        public void RunTestsInEditor()
        {
            int total = TacticalBattleTestSuite.RunAllTests();
            Debug.Log($"<color=green><b>[SUCESSO]</b></color> Todos os {total} testes unitários foram validados!");
        }

        void Start()
        {
            // Opcional: roda automaticamente se o script estiver ativo na cena
            // RunTestsInEditor();
        }
    }
}
