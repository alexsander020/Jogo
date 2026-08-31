using System.Collections;
using UnityEngine;

public class DefenseShield : MonoBehaviour
{
    [Header("Configurações do Escudo de Força (Defesa)")]
    public float baseScale = 0.40f;
    public Color shieldColor = new Color(0.15f, 0.70f, 1f, 0.90f);
    public Color shieldFrontColor = new Color(0.20f, 0.85f, 1f, 0.95f);

    private GameObject shieldInstance;
    private MeshRenderer shieldRenderer;
    private Material shieldMatInstance;
    private FacingDirection currentDirection = FacingDirection.South;

    public FacingDirection CurrentDirection => currentDirection;

    public void ShowShield(FacingDirection dir)
    {
        currentDirection = dir;
        EnsureShieldInstance();

        if (shieldInstance != null)
        {
            shieldInstance.SetActive(true);
            UpdatePositionAndRotation(dir);
        }
    }

    public void HideShield()
    {
        if (shieldInstance != null)
        {
            shieldInstance.SetActive(false);
        }
    }

    public void UpdateDirection(FacingDirection dir)
    {
        currentDirection = dir;
        if (shieldInstance != null && shieldInstance.activeSelf)
        {
            UpdatePositionAndRotation(dir);
        }
    }

    private void UpdatePositionAndRotation(FacingDirection dir)
    {
        if (shieldInstance == null) return;

        Vector3 offset = Vector3.zero;
        Quaternion rot = Quaternion.identity;

        // Posição e rotação do escudo de acordo com a direção que a unidade escolheu se defender
        switch (dir)
        {
            case FacingDirection.North: // Norte (+Y - Costas)
                offset = new Vector3(0f, 0.30f, 0.05f);
                rot = Quaternion.Euler(-30f, 180f, 0f);
                break;

            case FacingDirection.South: // Sul (-Y - Frente)
                offset = new Vector3(0f, -0.25f, -0.15f);
                rot = Quaternion.Euler(30f, 0f, 0f);
                break;

            case FacingDirection.East: // Leste (+X - Direita)
                offset = new Vector3(0.30f, -0.05f, -0.10f);
                rot = Quaternion.Euler(0f, -90f, 0f);
                break;

            case FacingDirection.West: // Oeste (-X - Esquerda)
                offset = new Vector3(-0.30f, -0.05f, -0.10f);
                rot = Quaternion.Euler(0f, 90f, 0f);
                break;
        }

        shieldInstance.transform.localPosition = offset;
        shieldInstance.transform.localRotation = rot;
        shieldInstance.transform.localScale = Vector3.one * baseScale;
    }

    public void TriggerHitReaction()
    {
        // Animação desativada conforme solicitado
    }

    private void EnsureShieldInstance()
    {
        if (shieldInstance != null) return;

        GameObject prefab = LoadForceFieldPrefab();
        if (prefab != null)
        {
            shieldInstance = Instantiate(prefab, transform);
            shieldInstance.name = "Defense_ForceField_Shield";

            // Remove colliders do prefab para não bloquear cliques no grid
            Collider[] colliders = shieldInstance.GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                if (col != null) Destroy(col);
            }

            shieldRenderer = shieldInstance.GetComponentInChildren<MeshRenderer>();
            if (shieldRenderer != null)
            {
                shieldMatInstance = shieldRenderer.material;
                if (shieldMatInstance != null)
                {
                    shieldMatInstance.color = shieldColor;
                }
            }
        }
        else
        {
            CreateProceduralShieldFallback();
        }
    }

    private GameObject LoadForceFieldPrefab()
    {
        GameObject prefab = Resources.Load<GameObject>("Prefabs/ForceField");
        if (prefab == null)
        {
            prefab = Resources.Load<GameObject>("ForceField");
        }

#if UNITY_EDITOR
        if (prefab == null)
        {
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Battle_Elements/Defesa/Mobile Force Field/Prefabs/ForceField.prefab");
        }
#endif
        return prefab;
    }

    private void CreateProceduralShieldFallback()
    {
        shieldInstance = GameObject.CreatePrimitive(PrimitiveType.Quad);
        shieldInstance.name = "Defense_Procedural_Shield";
        shieldInstance.transform.SetParent(transform, false);

        Collider col = shieldInstance.GetComponent<Collider>();
        if (col != null) Destroy(col);

        shieldRenderer = shieldInstance.GetComponent<MeshRenderer>();
    }
}
