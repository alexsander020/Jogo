using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SkillIconDatabase
{
    private static Dictionary<string, Sprite> cachedSprites = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
    private static readonly string ICONS_BASE_PATH = "Assets/Art/Battle_Elements/500FreeSkillIcons/Icons/";

    public static Sprite GetSkillIcon(string iconName, FunctionalCategory category = FunctionalCategory.System)
    {
        if (string.IsNullOrEmpty(iconName))
        {
            iconName = GetDefaultIconNameForCategory(category);
        }

        // 1. Verifica cache
        if (cachedSprites.TryGetValue(iconName, out Sprite cached) && cached != null)
        {
            return cached;
        }

        Sprite sprite = null;

        // 2. Tenta carregar via Resources
        sprite = Resources.Load<Sprite>($"SkillIcons/{iconName}");
        if (sprite == null)
        {
            sprite = Resources.Load<Sprite>(iconName);
        }

#if UNITY_EDITOR
        // 3. No editor do Unity, carrega diretamente pelo AssetDatabase
        if (sprite == null)
        {
            string assetPath = $"{ICONS_BASE_PATH}{iconName}.png";
            sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null && !iconName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                // Tenta sem prefixo ou com extensões alternativas
                string altPath = $"{ICONS_BASE_PATH}UI_Skill_Icon_{iconName}.png";
                sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(altPath);
            }
        }
#endif

        // 4. Fallback runtime via leitura direta de bytes PNG
        if (sprite == null)
        {
            sprite = LoadSpriteFromFile(iconName);
        }

        // 5. Se ainda não encontrou, tenta o ícone de fallback por categoria
        if (sprite == null && iconName != "UI_Skill_Icon_Claw")
        {
            string fallbackName = GetDefaultIconNameForCategory(category);
            if (fallbackName != iconName)
            {
                return GetSkillIcon(fallbackName, category);
            }
        }

        if (sprite != null)
        {
            cachedSprites[iconName] = sprite;
        }

        return sprite;
    }

    private static Sprite LoadSpriteFromFile(string iconName)
    {
        try
        {
            string fullPath = Path.Combine(Application.dataPath, "Art", "Battle_Elements", "500FreeSkillIcons", "Icons", $"{iconName}.png");
            if (!File.Exists(fullPath))
            {
                fullPath = Path.Combine(Application.dataPath, "Art", "Battle_Elements", "500FreeSkillIcons", "Icons", $"UI_Skill_Icon_{iconName}.png");
            }

            if (File.Exists(fullPath))
            {
                byte[] bytes = File.ReadAllBytes(fullPath);
                Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                if (texture.LoadImage(bytes))
                {
                    texture.filterMode = FilterMode.Bilinear;
                    texture.wrapMode = TextureWrapMode.Clamp;
                    return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SkillIconDatabase] Erro ao carregar ícone de arquivo {iconName}: {ex.Message}");
        }

        return null;
    }

    public static string GetDefaultIconNameForCategory(FunctionalCategory cat)
    {
        return cat switch
        {
            FunctionalCategory.Social => "UI_Skill_Icon_Buff",
            FunctionalCategory.Navi => "UI_Skill_Icon_Dash",
            FunctionalCategory.Tool => "UI_Skill_Icon_Pound",
            FunctionalCategory.Game => "UI_Skill_Icon_Slash",
            FunctionalCategory.Entertainment => "UI_Skill_Icon_Reflect",
            FunctionalCategory.Life => "UI_Skill_Icon_Heal",
            FunctionalCategory.System => "UI_Skill_Icon_PsycicAttack",
            _ => "UI_Skill_Icon_Claw"
        };
    }
}
