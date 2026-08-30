using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DamagePopupService : MonoBehaviour
{
    public static DamagePopupService Instance;

    private Font popupFont;

    void Awake()
    {
        if (Instance == null) Instance = this;
        popupFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (popupFont == null) popupFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    public static void ShowDamage(Vector3 worldPos, int damage, AttackOrientation orientation = AttackOrientation.Frontal, bool isCritical = false, bool hasAdvantage = false)
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("DamagePopupService");
            Instance = go.AddComponent<DamagePopupService>();
        }

        Instance.SpawnPopup(worldPos, damage, orientation, isCritical, hasAdvantage);
    }

    public static void ShowHeal(Vector3 worldPos, int amount)
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("DamagePopupService");
            Instance = go.AddComponent<DamagePopupService>();
        }

        Instance.SpawnHealPopup(worldPos, amount);
    }

    private void SpawnPopup(Vector3 worldPos, int damage, AttackOrientation orientation, bool isCritical, bool hasAdvantage)
    {
        GameObject popupObj = new GameObject("DamagePopup", typeof(RectTransform), typeof(Canvas));
        popupObj.transform.position = worldPos + Vector3.up * 0.7f;

        Canvas canvas = popupObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 500;

        RectTransform rt = popupObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 60);
        rt.localScale = Vector3.one * 0.012f;

        // Container de texto
        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(Outline), typeof(Shadow));
        textObj.transform.SetParent(popupObj.transform, false);

        Text txt = textObj.GetComponent<Text>();
        txt.font = popupFont;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;

        string label = $"-{damage}";
        Color textColor = new Color(0.95f, 0.95f, 0.95f, 1f); // Branco padrão

        if (orientation == AttackOrientation.Backstab || isCritical)
        {
            label = $"-{damage} CRÍTICO!";
            textColor = new Color(1f, 0.85f, 0.1f, 1f); // Dourado Vibrante
            txt.fontSize = 32;
        }
        else if (orientation == AttackOrientation.Flank)
        {
            label = $"-{damage} FLANCO";
            textColor = new Color(1f, 0.65f, 0.15f, 1f); // Laranja Âmbar
            txt.fontSize = 28;
        }
        else if (hasAdvantage)
        {
            label = $"-{damage} EFICAZ!";
            textColor = new Color(0.15f, 0.9f, 1f, 1f); // Ciano Brilhante
            txt.fontSize = 28;
        }
        else
        {
            txt.fontSize = 26;
        }

        txt.text = label;
        txt.color = textColor;

        Outline outline = textObj.GetComponent<Outline>();
        outline.effectColor = new Color(0.02f, 0.04f, 0.08f, 0.95f);
        outline.effectDistance = new Vector2(1.8f, -1.8f);

        Shadow shadow = textObj.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
        shadow.effectDistance = new Vector2(2f, -2f);

        StartCoroutine(AnimateAndDestroy(popupObj, txt));
    }

    private void SpawnHealPopup(Vector3 worldPos, int amount)
    {
        GameObject popupObj = new GameObject("HealPopup", typeof(RectTransform), typeof(Canvas));
        popupObj.transform.position = worldPos + Vector3.up * 0.7f;

        Canvas canvas = popupObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 500;

        RectTransform rt = popupObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 60);
        rt.localScale = Vector3.one * 0.012f;

        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(Outline));
        textObj.transform.SetParent(popupObj.transform, false);

        Text txt = textObj.GetComponent<Text>();
        txt.font = popupFont;
        txt.fontStyle = FontStyle.Bold;
        txt.fontSize = 26;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.text = $"+{amount}";
        txt.color = new Color(0.2f, 1f, 0.5f, 1f); // Verde Esmeralda

        Outline outline = textObj.GetComponent<Outline>();
        outline.effectColor = new Color(0.02f, 0.1f, 0.04f, 0.95f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        StartCoroutine(AnimateAndDestroy(popupObj, txt));
    }

    private IEnumerator AnimateAndDestroy(GameObject popupObj, Text txt)
    {
        float duration = 0.85f;
        float elapsed = 0f;
        Vector3 startPos = popupObj.transform.position;
        Vector3 targetPos = startPos + Vector3.up * 0.65f;
        Vector3 startScale = popupObj.transform.localScale;

        // Billboard em direção à câmera principal
        Camera mainCam = Camera.main;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Flutuação ascendente com curva suave
            popupObj.transform.position = Vector3.Lerp(startPos, targetPos, Mathf.SmoothStep(0f, 1f, t));

            // Efeito de impacto no início (punch scale)
            if (t < 0.25f)
            {
                float punch = 1f + Mathf.Sin(t / 0.25f * Mathf.PI) * 0.35f;
                popupObj.transform.localScale = startScale * punch;
            }
            else
            {
                popupObj.transform.localScale = startScale;
            }

            // Fade out nos últimos 40% do tempo
            if (t > 0.6f)
            {
                float fadeT = (t - 0.6f) / 0.4f;
                Color c = txt.color;
                c.a = Mathf.Lerp(1f, 0f, fadeT);
                txt.color = c;
            }

            if (mainCam != null)
            {
                popupObj.transform.rotation = mainCam.transform.rotation;
            }

            yield return null;
        }

        Destroy(popupObj);
    }
}
