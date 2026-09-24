using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gerenciador estático central de efeitos visuais de ataque (Free Slash VFX).
/// Mapeia e executa os prefabs de corte e projétil para as habilidades do jogo.
/// </summary>
public static class AttackVfxDatabase
{
    private static readonly Dictionary<string, GameObject> cachedPrefabs = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);

    private const string PREFABS_BASE_PATH = "Assets/Art/Battle_Elements/Attack_Animation/Free Slash VFX/Prefabs/";
    private const string PROJECTILES_BASE_PATH = "Assets/Art/Battle_Elements/Attack_Animation/Free Slash VFX/Prefabs/Projectiles/";
    private const string WATER_SPELL_BASE_PATH = "Assets/Art/Battle_Elements/Attack_Animation/FlexUnit/WaterSpell/Prefabs/";

    // Mapeamento canônico de IDs de Habilidade -> Nome do Prefab
    private static readonly Dictionary<string, string> skillIdToVfxMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        // 1. Ataques Físicos e Cortes Básicos
        { "attack_basic", "Slash VFX" },
        { "quarantine_lock", "Slash VFX" },
        { "frame_skip", "Slash VFX" },
        { "error_bite", "Multiple Slashes" },
        { "tool_strike", "Slash Earth VFX" },
        { "white_tiger_metal_rend", "Multiple Slashes" },
        { "data_devourer", "Multiple Slashes" },

        // 2. Fogo / Overclock
        { "firewall_flare", "Slash Projectile VFX Fire" },
        { "eruption_blast", "Slash Projectile VFX Fire" },
        { "vermilion_sound_inferno", "Slash Fire VFX" },
        { "overheat_carnage", "Slash Fire VFX" },

        // 3. Água / Marinho
        { "water_jet", "Slash Water VFX" },
        { "water jet", "Slash Water VFX" },
        { "waterjet", "Slash Water VFX" },
        { "jato_de_agua", "Slash Water VFX" },
        { "jato de água", "Slash Water VFX" },
        { "jato de agua", "Slash Water VFX" },
        { "water_wall", "WaterSpell2" },
        { "water wall", "WaterSpell2" },
        { "waterwall", "WaterSpell2" },
        { "parede_de_agua", "WaterSpell2" },
        { "parede de água", "WaterSpell2" },
        { "parede de agua", "WaterSpell2" },
        { "hydro_quarantine", "Slash Water VFX" },
        { "tsunami_barrier", "Slash Water VFX" },
        { "abyssal_dominion", "Slash Water VFX" },
        { "depth_cleanse", "Slash Water VFX" },

        // 4. Elétrico / Trovão / Sistema
        { "spark_zap", "Slash Projectile VFX Eletric" },
        { "voltage_snare", "Slash Eletric VFX" },
        { "overclock_beam", "Slash Projectile VFX Eletric" },
        { "azure_dragon_surge", "Slash Eletric VFX" },

        // 5. Terra / Construção / Natureza
        { "structural_blast", "Slash Projectile VFX Earth" },
        { "terraforming_grid", "Slash Earth VFX" },
        { "basalt_tile", "Slash Earth VFX" },
        { "polygon_trap", "Slash Earth VFX" },

        // 6. Impactos Pesados e Esmagamentos
        { "golden_hoard_press", "Multiple Impact" },
        { "system_freeze_slam", "Multiple Impact" },
        { "sonar_press", "Impact" }
    };

    // Aliases amigáveis (Português e Inglês) para fácil referência
    private static readonly Dictionary<string, string> aliasMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        // Cortes
        { "Corte", "Slash VFX" },
        { "Corte Basico", "Slash VFX" },
        { "Corte Neutro", "Slash VFX" },
        { "Corte Fogo", "Slash Fire VFX" },
        { "Corte Chama", "Slash Fire VFX" },
        { "Corte Agua", "Slash Water VFX" },
        { "Water Jet", "Slash Water VFX" },
        { "Jato de Agua", "Slash Water VFX" },
        { "Jato de Água", "Slash Water VFX" },
        { "Corte Eletrico", "Slash Eletric VFX" },
        { "Corte Raio", "Slash Eletric VFX" },
        { "Corte Terra", "Slash Earth VFX" },
        { "Corte Multiplo", "Multiple Slashes" },
        { "Combo Cortes", "Multiple Slashes" },

        // Projéteis
        { "Projetil Fogo", "Slash Projectile VFX Fire" },
        { "Projetil Agua", "Slash Projectile VFX Water" },
        { "Projetil Eletrico", "Slash Projectile VFX Eletric" },
        { "Projetil Raio", "Slash Projectile VFX Eletric" },
        { "Projetil Terra", "Slash Projectile VFX Earth" },

        // Impactos
        { "Impacto", "Impact" },
        { "Impacto Multiplo", "Multiple Impact" },
        { "Impacto Pesado", "Multiple Impact" },

        // Feitiços e Magias Aquáticas (FlexUnit WaterSpell)
        { "WaterSpell2", "WaterSpell2" },
        { "Water Spell 2", "WaterSpell2" },
        { "WaterSpell", "WaterSpell2" },
        { "Water Wall", "WaterSpell2" },
        { "Parede de Agua", "WaterSpell2" },
        { "Parede de Água", "WaterSpell2" }
    };

    /// <summary>
    /// Retorna o prefab do efeito visual mais adequado para a habilidade informada.
    /// </summary>
    public static GameObject GetVfxPrefab(SkillData skill)
    {
        if (skill != null && skill.attackVfxPrefab != null)
        {
            return skill.attackVfxPrefab;
        }

        string vfxName = ResolveVfxName(skill);
        return LoadPrefab(vfxName);
    }

    /// <summary>
    /// Determina o nome do prefab correspondente com base nas propriedades da habilidade.
    /// </summary>
    public static string ResolveVfxName(SkillData skill)
    {
        if (skill == null) return "Slash VFX";

        // 1. Se a habilidade possui um nome de VFX explicitamente definido
        if (!string.IsNullOrEmpty(skill.attackVfxName))
        {
            if (aliasMap.TryGetValue(skill.attackVfxName, out string canonical))
            {
                return canonical;
            }
            return skill.attackVfxName;
        }

        // 2. Mapeamento direto pelo ID da habilidade
        if (!string.IsNullOrEmpty(skill.id) && skillIdToVfxMap.TryGetValue(skill.id, out string mappedName))
        {
            return mappedName;
        }

        // 2b. Mapeamento direto pelo Nome da habilidade
        if (!string.IsNullOrEmpty(skill.skillName) && skillIdToVfxMap.TryGetValue(skill.skillName, out string mappedByName))
        {
            return mappedByName;
        }

        bool isRanged = skill.maxRange > 1;
        string nameLower = (skill.skillName ?? "").ToLowerInvariant().Trim();
        string descLower = (skill.description ?? "").ToLowerInvariant().Trim();
        string idLower = (skill.id ?? "").ToLowerInvariant().Trim();

        // 2c. Regra explícita para Water Jet / Jato de Água
        if (idLower == "water_jet" || idLower == "waterjet" || nameLower == "water jet" || nameLower == "jato de água" || nameLower == "jato de agua")
        {
            return "Slash Water VFX";
        }

        // 2d. Regra explícita para Water Wall / Parede de Água
        if (idLower == "water_wall" || idLower == "waterwall" || nameLower == "water wall" || nameLower == "parede de água" || nameLower == "parede de agua")
        {
            return "WaterSpell2";
        }

        // 3. Fallback inteligente por palavras-chave (Nome e Descrição)
        // Fogo / Chamas
        if (nameLower.Contains("fogo") || nameLower.Contains("chama") || nameLower.Contains("flame") ||
            nameLower.Contains("fire") || descLower.Contains("fogo") || descLower.Contains("chama") ||
            nameLower.Contains("rugido destrutivo") || nameLower.Contains("overheat"))
        {
            return isRanged ? "Slash Projectile VFX Fire" : "Slash Fire VFX";
        }

        // Água / Gelo / Oceano
        if (nameLower.Contains("água") || nameLower.Contains("agua") || nameLower.Contains("water") ||
            nameLower.Contains("hydro") || descLower.Contains("água") || descLower.Contains("alagado") ||
            nameLower.Contains("tsunami") || nameLower.Contains("mar"))
        {
            return isRanged ? "Slash Projectile VFX Water" : "Slash Water VFX";
        }

        // Elétrico / Raio / Relâmpago / Sistema
        if (nameLower.Contains("elétric") || nameLower.Contains("eletric") || nameLower.Contains("raio") ||
            nameLower.Contains("spark") || nameLower.Contains("volt") || nameLower.Contains("quântico") ||
            descLower.Contains("elétric") || descLower.Contains("descarga"))
        {
            return isRanged ? "Slash Projectile VFX Eletric" : "Slash Eletric VFX";
        }

        // Terra / Rocha / Espinho / Construtor
        if (nameLower.Contains("terra") || nameLower.Contains("earth") || nameLower.Contains("pedra") ||
            nameLower.Contains("rocha") || nameLower.Contains("espinho") || descLower.Contains("rocha") ||
            nameLower.Contains("basalt") || nameLower.Contains("polygon"))
        {
            return isRanged ? "Slash Projectile VFX Earth" : "Slash Earth VFX";
        }

        // Múltiplos ataques / Garras rápidas / Combo
        if (nameLower.Contains("combo") || nameLower.Contains("multi") || nameLower.Contains("furios") ||
            nameLower.Contains("rend") || nameLower.Contains("devour"))
        {
            return "Multiple Slashes";
        }

        // Impactos e Esmagamentos
        if (nameLower.Contains("slam") || nameLower.Contains("press") || nameLower.Contains("impacto") ||
            nameLower.Contains("esmagar"))
        {
            return "Impact";
        }

        // 4. Fallback padrão: Corte neutro
        return isRanged ? "Slash Projectile VFX Earth" : "Slash VFX";
    }

    /// <summary>
    /// Carrega o GameObject prefab pelo nome (com cache e suporte a Editor/Resources).
    /// </summary>
    public static GameObject LoadPrefab(string prefabName)
    {
        if (string.IsNullOrEmpty(prefabName)) return null;

        if (cachedPrefabs.TryGetValue(prefabName, out GameObject cached) && cached != null)
        {
            return cached;
        }

        GameObject prefab = null;

#if UNITY_EDITOR
        // 1. Tenta carregar na pasta principal de Prefabs
        string directPath = $"{PREFABS_BASE_PATH}{prefabName}.prefab";
        prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(directPath);

        // 2. Tenta na subpasta Projectiles se não encontrou
        if (prefab == null)
        {
            string projPath = $"{PROJECTILES_BASE_PATH}{prefabName}.prefab";
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(projPath);
        }

        // 3. Tenta na pasta FlexUnit/WaterSpell se não encontrou
        if (prefab == null)
        {
            string waterSpellPath = $"{WATER_SPELL_BASE_PATH}{prefabName}.prefab";
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(waterSpellPath);
        }

        // 4. Busca genérica se o nome não bater exatamente
        if (prefab == null)
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets($"{prefabName} t:Prefab");
            foreach (var guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null) break;
            }
        }
#endif

        // 4. Fallback runtime via Resources
        if (prefab == null)
        {
            prefab = Resources.Load<GameObject>($"FreeSlashVFX/{prefabName}");
        }
        if (prefab == null)
        {
            prefab = Resources.Load<GameObject>($"WaterSpell/{prefabName}");
        }
        if (prefab == null)
        {
            prefab = Resources.Load<GameObject>(prefabName);
        }

        if (prefab != null)
        {
            cachedPrefabs[prefabName] = prefab;
        }
        else
        {
            Debug.LogWarning($"[AttackVfxDatabase] Prefab de VFX '{prefabName}' não foi encontrado!");
        }

        return prefab;
    }

    /// <summary>
    /// Instancia e executa o efeito visual de ataque sincronizado entre o atacante e o alvo.
    /// </summary>
    public static void PlaySkillVfx(Unit attacker, Unit target, SkillData skill, Action onHitCallback = null)
    {
        if (target == null) return;

        GameObject prefab = GetVfxPrefab(skill);
        if (prefab == null)
        {
            onHitCallback?.Invoke();
            return;
        }

        Vector3 attackerPos = attacker != null ? attacker.transform.position : target.transform.position;
        Vector3 targetPos = target.transform.position;
        Vector3 direction = (targetPos - attackerPos).normalized;
        if (direction == Vector3.zero) direction = Vector3.forward;

        // Regra especial: Parede de Água / Water Wall cria barreira em loop de 4 turnos ao redor do personagem no centro
        if (WaterWallService.IsWaterWallSkill(skill))
        {
            int turns = skill != null && skill.statusDurationTurns > 0 ? skill.statusDurationTurns : 4;
            WaterWallService.CreateWaterWallBarrier(attacker, target, skill, turns);
            onHitCallback?.Invoke();
            return;
        }

        bool isProjectile = prefab.name.Contains("Projectile");

        if (isProjectile && attacker != null && attacker != target)
        {
            // Executa voo de projétil
            attacker.StartCoroutine(RunProjectileRoutine(prefab, attackerPos, targetPos, direction, onHitCallback));
        }
        else
        {
            // Executa corte direto ou magia no alvo
            SpawnDirectSlash(prefab, targetPos, direction);
            onHitCallback?.Invoke();
        }
    }

    private static void SpawnDirectSlash(GameObject prefab, Vector3 targetPos, Vector3 direction)
    {
        bool isGroundSpell = prefab.name.IndexOf("WaterSpell", StringComparison.OrdinalIgnoreCase) >= 0;

        // Centraliza o corte: feitiços de solo (como WaterSpell) se erguem a partir do chão do alvo
        Vector3 spawnPos = isGroundSpell ? new Vector3(targetPos.x, targetPos.y + 0.1f, targetPos.z) : targetPos + new Vector3(0, 0.45f, 0);

        // Orienta a rotação: feitiços de solo usam inclinação isométrica (-30 graus no eixo X), outros alinham com atacante
        Quaternion rotation = isGroundSpell ? Quaternion.Euler(-30f, 0f, 0f) : Quaternion.LookRotation(direction, Vector3.up);

        GameObject instance = GameObject.Instantiate(prefab, spawnPos, rotation);
        instance.transform.localScale = Vector3.one;

        // Dispara todos os ParticleSystems filhos
        var particles = instance.GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in particles)
        {
            ps.Play(true);
        }

        // Aciona Animators filhos se existirem (ex: WaterSpellAnimator)
        var animators = instance.GetComponentsInChildren<Animator>();
        foreach (var anim in animators)
        {
            anim.enabled = true;
            anim.Play(0, -1, 0f);
        }

        // Auto-destruição limpa após conclusão do efeito
        float destroyDelay = isGroundSpell ? 2.5f : 1.6f;
        GameObject.Destroy(instance, destroyDelay);
    }

    private static IEnumerator RunProjectileRoutine(GameObject prefab, Vector3 startPos, Vector3 targetPos, Vector3 direction, Action onHitCallback)
    {
        Vector3 spawnPos = startPos + new Vector3(0, 0.45f, 0);
        Vector3 endPos = targetPos + new Vector3(0, 0.45f, 0);
        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);

        GameObject instance = GameObject.Instantiate(prefab, spawnPos, rotation);
        instance.transform.localScale = Vector3.one;

        float flightDuration = 0.22f;
        float elapsed = 0f;

        while (elapsed < flightDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / flightDuration);
            if (instance != null)
            {
                instance.transform.position = Vector3.Lerp(spawnPos, endPos, t);
            }
            yield return null;
        }

        if (instance != null)
        {
            instance.transform.position = endPos;
        }

        // Dispara impacto no alvo
        SpawnHitImpact(targetPos);
        onHitCallback?.Invoke();

        // Destrói o projétil após 0.4s adicionais para deixar rastros desaparecerem
        if (instance != null)
        {
            var particles = instance.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particles)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            GameObject.Destroy(instance, 0.5f);
        }
    }

    private static void SpawnHitImpact(Vector3 targetPos)
    {
        GameObject impactPrefab = LoadPrefab("Impact");
        if (impactPrefab != null)
        {
            Vector3 impactPos = targetPos + new Vector3(0, 0.45f, 0);
            GameObject impactInstance = GameObject.Instantiate(impactPrefab, impactPos, Quaternion.identity);
            GameObject.Destroy(impactInstance, 1.2f);
        }
    }
}
