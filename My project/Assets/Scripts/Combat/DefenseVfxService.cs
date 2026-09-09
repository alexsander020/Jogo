using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Serviço responsável por instanciar e gerenciar os efeitos visuais (VFX) de Defesa
/// e as setas indicadoras de direção para o Jogador e a Oponente (IA).
/// </summary>
public static class DefenseVfxService
{
    private const string PREFAB_PATH = "Assets/Art/Battle_Elements/Defesa/Mobile Force Field/Prefabs/ForceField.prefab";
    private const string MAT_PLAYER_PATH = "Assets/Art/Battle_Elements/Defesa/Mobile Force Field/Materials/ForceField_1.mat";
    private const string MAT_ENEMY_PATH = "Assets/Art/Battle_Elements/Defesa/Mobile Force Field/Materials/ForceField_2.mat";

    private static GameObject cachedPrefab;
    private static Material cachedPlayerMat;
    private static Material cachedEnemyMat;

    // Rastreia a instância ativa do VFX de defesa por unidade
    private static readonly Dictionary<Unit, DefenseVfxInstance> activeInstances = new Dictionary<Unit, DefenseVfxInstance>();

    public class DefenseVfxInstance
    {
        public Unit owner;
        public GameObject rootObject;
        public GameObject forceFieldObj;
        public GameObject arrowObj;
        public Renderer shieldRenderer;
        public Material shieldMaterial;
        public FacingDirection currentFacing;
        public Coroutine pulseRoutine;
        public Vector3 baseScale;
    }

    /// <summary>
    /// Carrega os recursos necessários (Prefab e Materiais)
    /// </summary>
    private static void EnsureResourcesLoaded()
    {
#if UNITY_EDITOR
        if (cachedPrefab == null)
        {
            cachedPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
        }
        if (cachedPlayerMat == null)
        {
            cachedPlayerMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(MAT_PLAYER_PATH);
        }
        if (cachedEnemyMat == null)
        {
            cachedEnemyMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(MAT_ENEMY_PATH);
        }
#endif
        if (cachedPrefab == null)
        {
            cachedPrefab = Resources.Load<GameObject>("ForceField");
        }
        if (cachedPlayerMat == null)
        {
            cachedPlayerMat = Resources.Load<Material>("ForceField_1");
        }
        if (cachedEnemyMat == null)
        {
            cachedEnemyMat = Resources.Load<Material>("ForceField_2");
        }
    }

    /// <summary>
    /// Exibe o preview do VFX de defesa na unidade durante a escolha da direção.
    /// </summary>
    public static void ShowDefensePreview(Unit unit, FacingDirection initialFacing)
    {
        if (unit == null) return;

        EnsureResourcesLoaded();

        // Se já existir uma instância anterior, limpa
        ClearDefenseVfx(unit);

        // Cria o contêiner raiz do VFX vinculado à unidade
        GameObject root = new GameObject($"DefenseVfx_{unit.unitName}");
        root.transform.SetParent(unit.transform, false);
        root.transform.localPosition = new Vector3(0f, 0.45f, 0f);

        // 1. Instancia o Campo de Força (ForceField)
        GameObject fieldObj = null;
        Renderer rend = null;
        Vector3 targetScale = new Vector3(0.60f, 0.72f, 0.60f);

        if (cachedPrefab != null)
        {
            fieldObj = GameObject.Instantiate(cachedPrefab, root.transform);
            fieldObj.name = "ForceField_Mesh";
            fieldObj.transform.localPosition = Vector3.zero;
            fieldObj.transform.localRotation = Quaternion.identity;
            fieldObj.transform.localScale = targetScale;

            // Remove colisores para não interferir com cliques de tile ou raycasts
            var colliders = fieldObj.GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                col.enabled = false;
                GameObject.Destroy(col);
            }

            rend = fieldObj.GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                Material matToUse = (unit.team == Team.Player) ? cachedPlayerMat : cachedEnemyMat;
                if (matToUse != null)
                {
                    rend.material = matToUse;
                }

                int baseOrder = (unit.spriteRenderer != null) ? unit.spriteRenderer.sortingOrder : 300;
                rend.sortingLayerID = (unit.spriteRenderer != null) ? unit.spriteRenderer.sortingLayerID : 0;
                rend.sortingOrder = baseOrder + 10;
            }
        }
        else
        {
            // Fallback: esfera translúcida se prefab não carregar
            fieldObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fieldObj.transform.SetParent(root.transform, false);
            fieldObj.transform.localScale = targetScale;
            var col = fieldObj.GetComponent<Collider>();
            if (col != null) GameObject.Destroy(col);
            rend = fieldObj.GetComponent<Renderer>();

            if (rend != null)
            {
                int baseOrder = (unit.spriteRenderer != null) ? unit.spriteRenderer.sortingOrder : 300;
                rend.sortingLayerID = (unit.spriteRenderer != null) ? unit.spriteRenderer.sortingLayerID : 0;
                rend.sortingOrder = baseOrder + 10;
            }
        }

        // 2. Cria a Seta Direcional Holográfica
        Color teamColor = (unit.team == Team.Player) 
            ? new Color(0.2f, 0.85f, 1f, 0.95f) 
            : new Color(1f, 0.35f, 0.2f, 0.95f);

        GameObject arrow = CreateDirectionalArrow(root.transform, teamColor, unit);

        DefenseVfxInstance instance = new DefenseVfxInstance
        {
            owner = unit,
            rootObject = root,
            forceFieldObj = fieldObj,
            arrowObj = arrow,
            shieldRenderer = rend,
            shieldMaterial = rend != null ? rend.material : null,
            currentFacing = initialFacing,
            baseScale = targetScale
        };

        activeInstances[unit] = instance;

        // Atualiza orientação inicial
        UpdateDefenseDirection(unit, initialFacing);

        // Inicia suave animação de flutuação / rotação
        instance.pulseRoutine = unit.StartCoroutine(AnimateShieldRoutine(instance));
    }

    /// <summary>
    /// Atualiza a direção do VFX de Defesa e da Seta indicadora.
    /// </summary>
    public static void UpdateDefenseDirection(Unit unit, FacingDirection newFacing)
    {
        if (unit == null || !activeInstances.TryGetValue(unit, out DefenseVfxInstance instance))
        {
            return;
        }

        instance.currentFacing = newFacing;

        // Calcula vetor de direção na tela / grid isométrico
        Vector3 worldDir = GetFacingWorldVector(newFacing);

        // Posiciona e rotaciona a seta indicadora na direção correspondente
        if (instance.arrowObj != null)
        {
            float distance = 0.52f; // Posiciona logo na borda da bolha de defesa
            instance.arrowObj.transform.localPosition = new Vector3(worldDir.x * distance, worldDir.y * distance, -0.1f);

            float angle = Mathf.Atan2(worldDir.y, worldDir.x) * Mathf.Rad2Deg;
            instance.arrowObj.transform.localRotation = Quaternion.Euler(0f, 0f, angle - 90f);
        }

        // Leve inclinação do ForceField para dar sensação de defesa direcional
        if (instance.forceFieldObj != null)
        {
            Vector3 tiltAxis = Vector3.Cross(Vector3.forward, worldDir);
            instance.forceFieldObj.transform.localRotation = Quaternion.AngleAxis(15f, tiltAxis);
        }
    }

    /// <summary>
    /// Confirma a postura de defesa (quando o turno é finalizado com sucesso).
    /// </summary>
    public static void ConfirmDefenseStance(Unit unit)
    {
        if (unit == null) return;

        if (activeInstances.TryGetValue(unit, out DefenseVfxInstance instance))
        {
            // Pequeno pulso de confirmação visual
            unit.StartCoroutine(ConfirmFlashRoutine(instance));
        }
        else
        {
            // Se ainda não estava ativo, cria o VFX na postura final
            ShowDefensePreview(unit, unit.facing);
        }
    }

    /// <summary>
    /// Remove e limpa o VFX de defesa associado à unidade.
    /// </summary>
    public static void ClearDefenseVfx(Unit unit)
    {
        if (unit == null) return;

        if (activeInstances.TryGetValue(unit, out DefenseVfxInstance instance))
        {
            if (instance.pulseRoutine != null && unit != null)
            {
                unit.StopCoroutine(instance.pulseRoutine);
            }

            if (instance.rootObject != null)
            {
                GameObject.Destroy(instance.rootObject);
            }

            activeInstances.Remove(unit);
        }
    }

    /// <summary>
    /// Dispara um efeito visual de impacto no escudo quando um golpe é absorvido pela defesa.
    /// </summary>
    public static void PlayDefenseImpactVfx(Unit unit)
    {
        if (unit == null) return;

        if (activeInstances.TryGetValue(unit, out DefenseVfxInstance instance))
        {
            unit.StartCoroutine(ImpactPulseRoutine(instance));
        }
    }

    /// <summary>
    /// Calcula o vetor de direção normalizado correspondente a FacingDirection no grid isométrico.
    /// </summary>
    public static Vector3 GetFacingWorldVector(FacingDirection dir)
    {
        Vector3Int cellOffset = DirectionUtils.DirectionToVector(dir);
        if (Board.instance != null && Board.instance.grid != null)
        {
            Vector3 origin = Board.instance.grid.CellToWorld(Vector3Int.zero);
            Vector3 dest = Board.instance.grid.CellToWorld(cellOffset);
            Vector3 v = dest - origin;
            v.z = 0f;
            if (v.sqrMagnitude > 0.001f)
            {
                return v.normalized;
            }
        }

        // Fallback isométrico caso o grid não esteja acessível
        return dir switch
        {
            FacingDirection.North => new Vector3(0.707f, 0.707f, 0f).normalized,
            FacingDirection.East  => new Vector3(0.707f, -0.707f, 0f).normalized,
            FacingDirection.South => new Vector3(-0.707f, -0.707f, 0f).normalized,
            FacingDirection.West  => new Vector3(-0.707f, 0.707f, 0f).normalized,
            _ => Vector3.down
        };
    }

    /// <summary>
    /// Constrói proceduralmente um objeto visual de seta holográfica indicadora.
    /// </summary>
    private static GameObject CreateDirectionalArrow(Transform parent, Color arrowColor, Unit unit)
    {
        GameObject arrowRoot = new GameObject("DefenseArrow");
        arrowRoot.transform.SetParent(parent, false);

        // Cria a malha da seta (chevron / ponta de lança direcionada para +Y local)
        MeshFilter mf = arrowRoot.AddComponent<MeshFilter>();
        MeshRenderer mr = arrowRoot.AddComponent<MeshRenderer>();

        Mesh mesh = new Mesh();
        mesh.name = "ArrowMesh";

        // Vértices de uma seta estilizada e nítida
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(0f, 0.24f, 0f),        // 0: Ponta frontal
            new Vector3(-0.16f, -0.09f, 0f),   // 1: Asa esquerda
            new Vector3(0f, -0.01f, 0f),        // 2: Centro reentrante
            new Vector3(0.16f, -0.09f, 0f),     // 3: Asa direita
            new Vector3(-0.07f, -0.18f, 0f),   // 4: Base haste esquerda
            new Vector3(0.07f, -0.18f, 0f)      // 5: Base haste direita
        };

        // Triângulos frente e verso para visualização impecável
        int[] triangles = new int[]
        {
            // Frente
            0, 1, 2,
            0, 2, 3,
            1, 4, 2,
            2, 5, 3,
            // Verso
            0, 2, 1,
            0, 3, 2,
            1, 2, 4,
            2, 3, 5
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        mf.sharedMesh = mesh;

        // Material aditivo emissivo para brilho holográfico
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        Material mat = new Material(shader);
        mat.color = arrowColor;
        mr.material = mat;

        int baseOrder = (unit != null && unit.spriteRenderer != null) ? unit.spriteRenderer.sortingOrder : 300;
        mr.sortingLayerID = (unit != null && unit.spriteRenderer != null) ? unit.spriteRenderer.sortingLayerID : 0;
        mr.sortingOrder = baseOrder + 25; // Acima do escudo e do sprite da unidade

        return arrowRoot;
    }

    private static IEnumerator AnimateShieldRoutine(DefenseVfxInstance instance)
    {
        float timer = 0f;
        while (instance != null && instance.rootObject != null)
        {
            timer += Time.deltaTime;

            // Suave oscilação de respiração (breathing effect)
            float breathe = 1f + Mathf.Sin(timer * 3f) * 0.04f;
            if (instance.forceFieldObj != null)
            {
                instance.forceFieldObj.transform.localScale = instance.baseScale * breathe;
            }

            // Suave pulso na seta direcional
            if (instance.arrowObj != null)
            {
                float arrowPulse = 1f + Mathf.Sin(timer * 5f) * 0.12f;
                instance.arrowObj.transform.localScale = Vector3.one * arrowPulse;
            }

            yield return null;
        }
    }

    private static IEnumerator ConfirmFlashRoutine(DefenseVfxInstance instance)
    {
        if (instance == null || instance.forceFieldObj == null) yield break;

        Vector3 originalScale = instance.baseScale;
        Vector3 flashScale = originalScale * 1.25f;

        float elapsed = 0f;
        float duration = 0.18f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            if (instance.forceFieldObj != null)
            {
                instance.forceFieldObj.transform.localScale = Vector3.Lerp(flashScale, originalScale, t);
            }
            yield return null;
        }

        if (instance.forceFieldObj != null)
        {
            instance.forceFieldObj.transform.localScale = originalScale;
        }
    }

    private static IEnumerator ImpactPulseRoutine(DefenseVfxInstance instance)
    {
        if (instance == null || instance.forceFieldObj == null) yield break;

        Vector3 originalScale = instance.baseScale;
        Vector3 impactScale = originalScale * 1.35f;

        float elapsed = 0f;
        float duration = 0.22f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            if (instance.forceFieldObj != null)
            {
                instance.forceFieldObj.transform.localScale = Vector3.Lerp(impactScale, originalScale, t);
            }
            yield return null;
        }

        if (instance.forceFieldObj != null)
        {
            instance.forceFieldObj.transform.localScale = originalScale;
        }
    }
}
