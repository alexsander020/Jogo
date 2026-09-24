using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gerencia a barreira visual e funcional contínua de [Water Wall / Parede de Água].
/// Mantém o VFX WaterSpell2 em loop contínuo ao redor do personagem no centro por 4 turnos,
/// dissipando-se suavemente ao término dos 4 turnos.
/// </summary>
public class WaterWallService : MonoBehaviour
{
    private static WaterWallService _instance;
    public static WaterWallService Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("WaterWallService");
                _instance = go.AddComponent<WaterWallService>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    public static readonly List<WaterWallBarrier> activeBarriers = new List<WaterWallBarrier>();

    /// <summary>
    /// Verifica se uma habilidade corresponde a Water Wall ou Parede de Água.
    /// </summary>
    public static bool IsWaterWallSkill(SkillData skill)
    {
        if (skill == null) return false;
        string idLower = (skill.id ?? "").ToLowerInvariant().Trim();
        string nameLower = (skill.skillName ?? "").ToLowerInvariant().Trim();

        return idLower == "water_wall" || idLower == "waterwall" || idLower == "parede_de_agua" ||
               nameLower == "water wall" || nameLower == "waterwall" ||
               nameLower == "parede de água" || nameLower == "parede de agua";
    }

    /// <summary>
    /// Cria ou renova a barreira de Parede de Água ao redor do personagem por 4 turnos.
    /// </summary>
    public static WaterWallBarrier CreateWaterWallBarrier(Unit caster, Unit target, SkillData skill, int durationTurns = 4)
    {
        EnsureService();

        // O personagem protegido no centro: se o alvo for aliado é o alvo, caso contrário é o próprio conjurador
        Unit centerUnit = (target != null && caster != null && target.team == caster.team) ? target : caster;
        if (centerUnit == null) centerUnit = target != null ? target : caster;

        Vector3Int centerCoord = centerUnit != null && centerUnit.currentTile != null 
            ? centerUnit.currentTile.pos 
            : (centerUnit != null ? centerUnit.gridPosition : Vector3Int.zero);

        Vector3 centerWorld = centerUnit != null ? centerUnit.transform.position : Vector3.zero;

        // Se já existir uma barreira ativa na mesma coordenada central ou no mesmo personagem, renova
        var existing = activeBarriers.Find(b => b != null && !b.isExpiring && (b.centerUnit == centerUnit || b.centerPos == centerCoord));
        if (existing != null)
        {
            existing.Renew(durationTurns);
            Debug.Log($"[WaterWallService] Barreira de Água em {centerCoord} renovada para {durationTurns} turnos.");
            return existing;
        }

        GameObject barrierContainer = new GameObject($"WaterWallBarrier_{centerCoord.x}_{centerCoord.y}");
        barrierContainer.transform.position = centerWorld;

        var barrier = barrierContainer.AddComponent<WaterWallBarrier>();
        barrier.Initialize(caster, centerUnit, centerCoord, centerWorld, durationTurns);
        activeBarriers.Add(barrier);

        Debug.Log($"[WaterWallService] Barreira de Água criada em {centerCoord} com {(centerUnit != null ? centerUnit.unitName : "personagem")} no centro por {durationTurns} turnos.");
        return barrier;
    }

    /// <summary>
    /// Remove todas as barreiras ativas imediatamente (ex: fim de batalha).
    /// </summary>
    public static void ClearAllBarriers()
    {
        for (int i = activeBarriers.Count - 1; i >= 0; i--)
        {
            if (activeBarriers[i] != null)
            {
                activeBarriers[i].DismissImmediate();
            }
        }
        activeBarriers.Clear();
    }

    private static void EnsureService()
    {
        if (_instance == null)
        {
            var go = new GameObject("WaterWallService");
            _instance = go.AddComponent<WaterWallService>();
            DontDestroyOnLoad(go);
        }
    }

    void OnDestroy()
    {
        ClearAllBarriers();
    }
}

/// <summary>
/// Instância física da Barreira de Água.
/// Mantém WaterSpell2 em loop contínuo perfeitamente centralizado no personagem por 4 turnos.
/// </summary>
public class WaterWallBarrier : MonoBehaviour
{
    public Unit caster;
    public Unit centerUnit;
    public Vector3Int centerPos;
    public Vector3 centerWorldPos;

    public int remainingTurns = 4;
    public int maxTurns = 4;
    public bool isExpiring = false;

    [Header("Ajuste de Rotação e Posicionamento Isométrico")]
    [Tooltip("Inclinação no eixo X (ex: -30 para perspectiva isométrica padrão 2:1, ou -35 a -45 para visualização mais aberta de cima)")]
    public Vector3 barrierRotation = new Vector3(-30f, 0f, 0f);
    public Vector3 barrierOffset = new Vector3(0f, 0.05f, 0f);
    public float barrierScale = 1.15f;

    private GameObject centerInstance;
    private readonly List<GameObject> spawnedVfxInstances = new List<GameObject>();
    private readonly List<WaterSpellLooper> loopers = new List<WaterSpellLooper>();
    private readonly Dictionary<Vector3Int, TerrainType> previousTerrains = new Dictionary<Vector3Int, TerrainType>();

    public void Initialize(Unit casterUnit, Unit targetUnit, Vector3Int centerCoord, Vector3 centerWorld, int turns)
    {
        caster = casterUnit;
        centerUnit = targetUnit != null ? targetUnit : casterUnit;
        centerPos = centerCoord;
        centerWorldPos = centerWorld;
        remainingTurns = turns > 0 ? turns : 4;
        maxTurns = remainingTurns;

        SpawnBarrierAreaVfx();
        ApplyFloodedTerrain();
        HookTurnEvents();
    }

    public void Renew(int turns)
    {
        remainingTurns = turns > 0 ? turns : 4;
        maxTurns = remainingTurns;
    }

    void Update()
    {
        if (isExpiring) return;

        // Se o personagem se mover para outro tile, atualiza o terreno alagado mantendo-o no centro
        if (centerUnit != null && centerUnit.currentTile != null)
        {
            Vector3Int newPos = centerUnit.currentTile.pos;
            if (newPos != centerPos)
            {
                RestoreTerrain();
                centerPos = newPos;
                centerWorldPos = centerUnit.transform.position;
                ApplyFloodedTerrain();
            }
        }
    }

    void LateUpdate()
    {
        if (isExpiring || centerInstance == null) return;

        // Sincroniza rotação e escala em tempo real (permite ajuste visual dinâmico via Inspector)
        centerInstance.transform.rotation = Quaternion.Euler(barrierRotation);
        centerInstance.transform.localScale = Vector3.one * barrierScale;

        var follower = centerInstance.GetComponent<SmoothFollowTarget>();
        if (follower != null)
        {
            follower.offset = barrierOffset;
            follower.rotationEuler = barrierRotation;
        }
    }

    private void SpawnBarrierAreaVfx()
    {
        GameObject prefab = AttackVfxDatabase.LoadPrefab("WaterSpell2");
        if (prefab == null)
        {
            Debug.LogWarning("[WaterWallBarrier] Prefab 'WaterSpell2' não encontrado!");
            return;
        }

        // Instância Única e Centralizada diretamente no personagem (o personagem fica no centro da cúpula de água)
        Vector3 charSpawnPos = centerUnit != null 
            ? centerUnit.transform.position + barrierOffset
            : centerWorldPos + barrierOffset;

        Quaternion initialRotation = Quaternion.Euler(barrierRotation);
        centerInstance = Instantiate(prefab, charSpawnPos, initialRotation, transform);
        centerInstance.name = "WaterWall_Center";
        centerInstance.transform.localScale = Vector3.one * barrierScale;
        centerInstance.transform.rotation = initialRotation;
        AttachLooper(centerInstance);
        spawnedVfxInstances.Add(centerInstance);

        // Seguidor suave para que o personagem permaneça sempre no centro mesmo ao andar
        if (centerUnit != null)
        {
            var follower = centerInstance.AddComponent<SmoothFollowTarget>();
            follower.target = centerUnit.transform;
            follower.offset = barrierOffset;
            follower.rotationEuler = barrierRotation;
        }
    }

    private void AttachLooper(GameObject go)
    {
        var looper = go.AddComponent<WaterSpellLooper>();
        looper.StartLooping();
        loopers.Add(looper);
    }

    private void ApplyFloodedTerrain()
    {
        if (Board.instance == null || Board.instance.tiles == null) return;

        // Cobre toda a área 3x3 com Terreno Alagado (Flooded) centrada no personagem
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                Vector3Int coord = new Vector3Int(centerPos.x + dx, centerPos.y + dy, centerPos.z);
                TileLogic tile = Board.GetTile(coord);
                if (tile != null)
                {
                    if (!previousTerrains.ContainsKey(coord))
                    {
                        previousTerrains[coord] = tile.terrainType;
                    }
                    tile.terrainType = TerrainType.Flooded;
                }
            }
        }
    }

    private void RestoreTerrain()
    {
        if (Board.instance == null || Board.instance.tiles == null) return;

        foreach (var kvp in previousTerrains)
        {
            TileLogic tile = Board.GetTile(kvp.Key);
            if (tile != null && tile.terrainType == TerrainType.Flooded)
            {
                tile.terrainType = kvp.Value;
            }
        }
        previousTerrains.Clear();
    }

    private void HookTurnEvents()
    {
        if (BattleController.Instance != null)
        {
            BattleController.Instance.OnTurnEnd += HandleTurnEnd;
            BattleController.Instance.OnBattleEnd += HandleBattleEnd;
        }
    }

    private void UnhookTurnEvents()
    {
        if (BattleController.Instance != null)
        {
            BattleController.Instance.OnTurnEnd -= HandleTurnEnd;
            BattleController.Instance.OnBattleEnd -= HandleBattleEnd;
        }
    }

    private void HandleTurnEnd(Unit currentUnit)
    {
        if (isExpiring) return;

        // O turno da barreira é consumido ao final de cada turno do personagem protegido (ou do conjurador)
        bool isOwnerTurn = (centerUnit != null && currentUnit == centerUnit) || 
                           (caster != null && currentUnit == caster);

        // Fallback: se ambos os conjuradores já morreram ou saíram de campo, decrementa a cada turno
        if (!isOwnerTurn && (caster == null || !caster.IsAlive) && (centerUnit == null || !centerUnit.IsAlive))
        {
            isOwnerTurn = true;
        }

        if (isOwnerTurn)
        {
            remainingTurns--;
            Debug.Log($"[WaterWallBarrier] Fim de turno de {(currentUnit != null ? currentUnit.unitName : "Unidade")}. Restam {remainingTurns} turnos da Parede de Água em {centerPos}.");

            // Ao final de 4 turnos da ativação da habilidade, encerra e dissipa suavemente
            if (remainingTurns <= 0)
            {
                Expire();
            }
        }
    }

    private void HandleBattleEnd(Team winner)
    {
        DismissImmediate();
    }

    /// <summary>
    /// Desaparece suavemente após o término dos 4 turnos da ativação.
    /// </summary>
    public void Expire()
    {
        if (isExpiring) return;
        isExpiring = true;

        UnhookTurnEvents();
        RestoreTerrain();
        WaterWallService.activeBarriers.Remove(this);

        Debug.Log($"[WaterWallBarrier] 4 turnos concluídos! Parede de Água em {centerPos} dissipando suavemente.");

        // Notifica o looper para parar o loop e executar a animação suave de afundamento
        foreach (var looper in loopers)
        {
            if (looper != null)
            {
                looper.StopLoopingAndFade();
            }
        }

        // Destrói o container após a conclusão suave da animação de WaterSpellFinish (~2.66s)
        Destroy(gameObject, 2.7f);
    }

    /// <summary>
    /// Destruição imediata sem delay (para reinício de cena ou fim de batalha).
    /// </summary>
    public void DismissImmediate()
    {
        UnhookTurnEvents();
        RestoreTerrain();
        WaterWallService.activeBarriers.Remove(this);
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        UnhookTurnEvents();
        RestoreTerrain();
        WaterWallService.activeBarriers.Remove(this);
    }
}

/// <summary>
/// Controla o loop contínuo de partículas, shaders e animação do WaterSpell2.
/// </summary>
public class WaterSpellLooper : MonoBehaviour
{
    private ParticleSystem[] particleSystems;
    private Animator[] animators;
    private Coroutine loopRoutine;
    private bool isLooping = false;

    void Awake()
    {
        particleSystems = GetComponentsInChildren<ParticleSystem>();
        animators = GetComponentsInChildren<Animator>();
    }

    public void StartLooping()
    {
        isLooping = true;

        // Configura todos os sistemas de partículas (gotas, estrelas no chão) para modo contínuo (loop)
        foreach (var ps in particleSystems)
        {
            if (ps != null)
            {
                var main = ps.main;
                main.loop = true;
                if (!ps.isPlaying)
                {
                    ps.Play(true);
                }
            }
        }

        // Dispara a subida inicial da água (WaterSpellStart)
        foreach (var anim in animators)
        {
            if (anim != null)
            {
                anim.enabled = true;
                anim.speed = 1f;
                anim.Play("WaterSpellStart", 0, 0f);
            }
        }

        if (loopRoutine != null)
        {
            StopCoroutine(loopRoutine);
        }
        loopRoutine = StartCoroutine(MaintainLoopRoutine());
    }

    private IEnumerator MaintainLoopRoutine()
    {
        // Aguarda a subida suave da parede d'água (WaterSpellStart atinge ápice em ~1.33s)
        float elapsed = 0f;
        while (isLooping && elapsed < 1.33f)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Enquanto estiver ativo nos 4 turnos:
        // Mantém os Animators congelados no ápice (OffsetTop = 0.27, Opacity = 1).
        // Isso impede a transição prematura para WaterSpellFinish e previne qualquer piscamento ou reinício abrupto.
        // Os shaders de água (Water e SpellCircle) continuam ondulando em tempo real na GPU (_Time),
        // e os ParticleSystems (ThrowingParticles e FloatingParticles) continuam girando e emitindo em loop contínuo!
        while (isLooping)
        {
            foreach (var anim in animators)
            {
                if (anim != null && anim.enabled)
                {
                    anim.speed = 0f;
                }
            }
            yield return new WaitForSeconds(0.25f);
        }
    }

    /// <summary>
    /// Para o loop: cessa a emissão de partículas e executa a animação de finalização suave (água submergindo no solo).
    /// </summary>
    public void StopLoopingAndFade()
    {
        isLooping = false;
        if (loopRoutine != null)
        {
            StopCoroutine(loopRoutine);
            loopRoutine = null;
        }

        // Para emissão contínua para deixar as partículas existentes esvaecerem suavemente
        foreach (var ps in particleSystems)
        {
            if (ps != null)
            {
                var main = ps.main;
                main.loop = false;
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        // Executa o splash final de água submergindo suavemente no chão
        foreach (var anim in animators)
        {
            if (anim != null && anim.enabled)
            {
                anim.speed = 1f;
                anim.Play("WaterSpellFinish", 0, 0f);
            }
        }
    }
}

/// <summary>
/// Garante que o VFX central permaneça ao redor do personagem mesmo se ele se mover.
/// </summary>
public class SmoothFollowTarget : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 0.05f, 0);
    public Vector3 rotationEuler = new Vector3(-30f, 0f, 0f);

    void LateUpdate()
    {
        if (target != null)
        {
            transform.position = target.position + offset;
            transform.rotation = Quaternion.Euler(rotationEuler);
        }
    }
}
