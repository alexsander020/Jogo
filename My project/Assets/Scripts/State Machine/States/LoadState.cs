using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadState : State
{
    public override void Enter()
    {
        StartCoroutine(LoadSequence());
    }

    IEnumerator LoadSequence()
    {
        // 1. Inicializa o tabuleiro e pisos
        yield return StartCoroutine(Board.instance.InitSequence(this));
        yield return null;

        // 2. Instancia as unidades inimigas no tabuleiro
        if (MapLoader.instance != null)
        {
            MapLoader.instance.SpawnEnemyUnits();
        }
        yield return null;

        // 3. Centraliza a câmera perfeitamente no tabuleiro
        Camera mainCam = Camera.main;
        if (mainCam != null && mainCam.GetComponent<TacticalCameraController>() == null)
        {
            mainCam.gameObject.AddComponent<TacticalCameraController>();
        }

        if (TacticalCameraController.Instance != null)
        {
            TacticalCameraController.Instance.CenterOnBoard();
        }
        yield return null;

        // 4. Entra no estado de seleção e posicionamento de criaturas pré-batalha (Smartphone Digimon Survive)
        machine.ChangeTo<DeploySelectionState>();
    }
}
