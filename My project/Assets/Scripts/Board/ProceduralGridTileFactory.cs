using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fábrica de sprites e texturas de alta definição para o Grid e Cursor no estilo Digimon Survive.
/// Gera molduras isométricas neon com cantos arredondados, preenchimento translúcido e chevrons de alvo.
/// </summary>
public static class ProceduralGridTileFactory
{
    private static Sprite s_movementSprite;
    private static Sprite s_attackSprite;
    private static Sprite s_skillSprite;
    private static Sprite s_targetSprite;
    private static Sprite s_invalidSprite;
    private static Sprite s_chevronSprite;
    private static Sprite s_deploySprite;

    public static Sprite DeployTile => s_deploySprite ??= CreateIsometricTile(
        coreColor: new Color(0.98f, 0.95f, 0.20f, 1.0f),     // Amarelo Dourado / Neon (Digimon Survive Spawn Tile)
        glowColor: new Color(0.95f, 0.85f, 0.10f, 0.90f),
        innerFillColor: new Color(0.98f, 0.95f, 0.20f, 0.22f)
    );

    public static Sprite MovementTile => s_movementSprite ??= CreateIsometricTile(
        coreColor: new Color(0.15f, 0.95f, 1.0f, 1.0f),     // Ciano Neon Elétrico (#00F0FF)
        glowColor: new Color(0.0f, 0.70f, 1.0f, 0.85f),
        innerFillColor: new Color(0.05f, 0.65f, 0.95f, 0.22f) // Preenchimento suave estilo vidro
    );

    public static Sprite AttackTile => s_attackSprite ??= CreateIsometricTile(
        coreColor: new Color(1.0f, 0.75f, 0.15f, 1.0f),     // Âmbar / Laranja Incandescente (#FFAA10)
        glowColor: new Color(1.0f, 0.45f, 0.05f, 0.85f),
        innerFillColor: new Color(1.0f, 0.50f, 0.05f, 0.24f)
    );

    public static Sprite SkillTile => s_skillSprite ??= CreateIsometricTile(
        coreColor: new Color(0.20f, 1.0f, 0.55f, 1.0f),     // Verde Esmeralda Neon
        glowColor: new Color(0.05f, 0.85f, 0.40f, 0.85f),
        innerFillColor: new Color(0.10f, 0.80f, 0.40f, 0.22f)
    );

    public static Sprite TargetTile => s_targetSprite ??= CreateIsometricTile(
        coreColor: new Color(0.95f, 0.40f, 1.0f, 1.0f),     // Violeta / Magenta Digimon Survive (#E040FB)
        glowColor: new Color(0.70f, 0.15f, 1.0f, 0.90f),
        innerFillColor: new Color(0.85f, 0.20f, 1.0f, 0.30f)
    );

    public static Sprite InvalidTile => s_invalidSprite ??= CreateIsometricTile(
        coreColor: new Color(1.0f, 0.25f, 0.25f, 1.0f),     // Vermelho Crimson
        glowColor: new Color(0.90f, 0.10f, 0.10f, 0.80f),
        innerFillColor: new Color(0.80f, 0.10f, 0.10f, 0.22f)
    );

    public static Sprite TargetChevron => s_chevronSprite ??= CreateFloatingChevron(
        coreColor: new Color(1.0f, 0.95f, 0.40f, 1.0f),     // Amarelo Dourado Brilhante
        glowColor: new Color(1.0f, 0.70f, 0.10f, 0.90f)
    );

    /// <summary>
    /// Gera uma textura e sprite isométrico 2:1 com bordas arredondadas e contorno neon anti-aliased.
    /// </summary>
    public static Sprite CreateIsometricTile(
        Color coreColor,
        Color glowColor,
        Color innerFillColor,
        int width = 256,
        int height = 128,
        float cornerRadius = 0.20f,
        float outlineThickness = 0.075f,
        float glowSoftness = 0.14f)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[width * height];
        float invW = 1.0f / width;
        float invH = 1.0f / height;

        float boxHalfSize = 0.68f; // Tamanho base no espaço rotacionado
        float r = cornerRadius * boxHalfSize;

        for (int y = 0; y < height; y++)
        {
            float v = (y + 0.5f) * invH * 2.0f - 1.0f; // [-1, 1]

            for (int x = 0; x < width; x++)
            {
                float u = (x + 0.5f) * invW * 2.0f - 1.0f; // [-1, 1]

                // Rotação em 45 graus para espaço quadrado
                float rx = (u + v) * 0.70710678f;
                float ry = (-u + v) * 0.70710678f;

                // SDF (Signed Distance Function) para retângulo com cantos arredondados
                float qx = Mathf.Abs(rx) - (boxHalfSize - r);
                float qy = Mathf.Abs(ry) - (boxHalfSize - r);

                float extX = Mathf.Max(qx, 0.0f);
                float extY = Mathf.Max(qy, 0.0f);
                float dist = Mathf.Sqrt(extX * extX + extY * extY) + Mathf.Min(Mathf.Max(qx, qy), 0.0f) - r;

                Color pixelColor;

                if (dist > glowSoftness)
                {
                    // Fora do alcance do glow
                    pixelColor = Color.clear;
                }
                else if (dist > 0.0f)
                {
                    // Brilho externo (Outer Glow Falloff)
                    float t = dist / glowSoftness;
                    float glowAlpha = Mathf.Pow(1.0f - t, 2.2f) * glowColor.a;
                    pixelColor = new Color(glowColor.r, glowColor.g, glowColor.b, glowAlpha);
                }
                else if (dist >= -outlineThickness)
                {
                    // Borda / Contorno Neon brilhante (Core Outline)
                    float t = (dist + outlineThickness) / outlineThickness;
                    // Interpola suavemente do brilho interno para o núcleo neon
                    Color edgeColor = Color.Lerp(innerFillColor, coreColor, Mathf.SmoothStep(0f, 1f, t));
                    pixelColor = new Color(edgeColor.r, edgeColor.g, edgeColor.b, Mathf.Max(coreColor.a, 0.95f));
                }
                else
                {
                    // Centro translúcido estilo vidro com leve iluminação periférica
                    float innerDist = (-dist - outlineThickness) / (boxHalfSize - outlineThickness);
                    float fillAlpha = Mathf.Lerp(innerFillColor.a * 1.35f, innerFillColor.a * 0.45f, Mathf.Clamp01(innerDist));
                    pixelColor = new Color(innerFillColor.r, innerFillColor.g, innerFillColor.b, fillAlpha);
                }

                pixels[y * width + x] = pixelColor;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        // 80 pixels por unidade no tamanho base (80x40) -> 256/80 * 100 = 320 PPU para casar com 1 unidade no grid
        float pixelsPerUnit = (float)width / 0.81f; 
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
    }

    /// <summary>
    /// Cria o marcador visual de Chevron duplo estilizado (\/\/) apontando para baixo (Digimon Survive).
    /// </summary>
    public static Sprite CreateFloatingChevron(Color coreColor, Color glowColor, int width = 96, int height = 96)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[width * height];
        float invW = 1.0f / width;
        float invH = 1.0f / height;

        for (int y = 0; y < height; y++)
        {
            float ny = (y + 0.5f) * invH; // [0, 1]

            for (int x = 0; x < width; x++)
            {
                float nx = (x + 0.5f) * invW; // [0, 1]
                float centeredX = Mathf.Abs(nx - 0.5f) * 2.0f; // 0 no centro, 1 nas bordas

                // Distância da linha em V do Chevron 1 (Superior)
                float vShape1 = 0.55f + centeredX * 0.35f;
                float d1 = Mathf.Abs(ny - vShape1);

                // Distância da linha em V do Chevron 2 (Inferior)
                float vShape2 = 0.30f + centeredX * 0.35f;
                float d2 = Mathf.Abs(ny - vShape2);

                float minDist = Mathf.Min(d1, d2);
                float thickness = 0.08f;
                float glowSoft = 0.12f;

                Color pixelColor;

                if (centeredX > 0.82f)
                {
                    // Recorte lateral
                    pixelColor = Color.clear;
                }
                else if (minDist <= thickness)
                {
                    // Núcleo brilhante
                    float t = 1.0f - (minDist / thickness);
                    pixelColor = Color.Lerp(glowColor, coreColor, t);
                    pixelColor.a = 1.0f;
                }
                else if (minDist <= thickness + glowSoft)
                {
                    // Glow suave
                    float t = (minDist - thickness) / glowSoft;
                    float alpha = Mathf.Pow(1.0f - t, 2.0f) * glowColor.a;
                    pixelColor = new Color(glowColor.r, glowColor.g, glowColor.b, alpha);
                }
                else
                {
                    pixelColor = Color.clear;
                }

                pixels[y * width + x] = pixelColor;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.0f), 100.0f);
    }
}
