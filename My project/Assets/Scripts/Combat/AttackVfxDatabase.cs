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
        { "water_jet", "Slash Projectile VFX Water" },
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
        { "Impacto Pesado", "Multiple Impact" }
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

        bool isRanged = skill.maxRange > 1;
        string nameLower = (skill.skillName ?? "").ToLowerInvariant();
        string descLower = (skill.description ?? "").ToLowerInvariant();

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

        // 3. Busca genérica se o nome não bater exatamente
        if (prefab == null)
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets($"{prefabName} t:Prefab");
            foreach (var guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("Free Slash VFX"))
                {
                    prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null) break;
                }
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

        bool isProjectile = prefab.name.Contains("Projectile");

        if (isProjectile && attacker != null && attacker != target)
        {
            // Executa voo de projétil
            attacker.StartCoroutine(RunProjectileRoutine(prefab, attackerPos, targetPos, direction, onHitCallback));
        }
        else
        {
            // Executa corte direto no alvo
            SpawnDirectSlash(prefab, targetPos, direction);
            onHitCallback?.Invoke();
        }
    }

    private static void SpawnDirectSlash(GameObject prefab, Vector3 targetPos, Vector3 direction)
    {
        // Centraliza o corte levemente elevado sobre o defensor
        Vector3 spawnPos = targetPos + new Vector3(0, 0.45f, 0);

        // Orienta a rotação na direção do atacante para o defensor com ângulo isométrico agradável
        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);

        GameObject instance = GameObject.Instantiate(prefab, spawnPos, rotation);
        instance.transform.localScale = Vector3.one;

        // Dispara todos os ParticleSystems filhos
        var particles = instance.GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in particles)
        {
            ps.Play(true);
        }

        // Auto-destruição limpa após conclusão do efeito
        GameObject.Destroy(instance, 1.6f);
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
