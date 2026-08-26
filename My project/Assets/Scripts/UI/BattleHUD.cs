using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleHUD : MonoBehaviour
{
    public static BattleHUD Instance;

    [Header("UI Containers (Opcional - Criados automaticamente se nulos)")]
    public Canvas canvas;
    public RectTransform topTurnBanner;
    public RectTransform bottomControlsPanel;
    public RectTransform targetInfoPanel;

    // Textos do Top Banner
    Text roundText;
    Text unitNameText;
    Text unitCategoryText;
    Text unitHpSpText;

    // Textos de Controles Contextuais
    Text stateTitleText;
    Text controlsBodyText;

    // Textos do Painel de Inspeção do Alvo
    Text targetNameText;
    Text targetStatsText;

    Font hudFont;

    void Awake()
    {
        Instance = this;
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }

        hudFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (hudFont == null) hudFont = Font.CreateDynamicFontFromOSFont("Arial", 14);

        BuildHUDHierarchy();
    }

    void Start()
    {
        if (BattleController.Instance != null)
        {
            BattleController.Instance.OnTurnStart += UpdateTurnBanner;
            BattleController.Instance.OnBattleEnd += OnBattleEnd;
        }

        UpdateControlsPrompt("AGUARDANDO INÍCIO", "Pressione PLAY para carregar o combate.");
    }

    void OnDestroy()
    {
        if (BattleController.Instance != null)
        {
            BattleController.Instance.OnTurnStart -= UpdateTurnBanner;
            BattleController.Instance.OnBattleEnd -= OnBattleEnd;
        }
    }

    void Update()
    {
        // Atualiza inspeção de tile/unidade sob o cursor em tempo real
        UpdateInspectorPanel();
    }

    // Cria os painéis de UI no Canvas automaticamente se não existirem
    void BuildHUDHierarchy()
    {
        if (canvas == null) return;

        // 1. TOP TURN BANNER
        if (topTurnBanner == null)
        {
            GameObject bannerObj = CreateUIPanel(canvas.transform, "TopTurnBanner", 
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), 
                new Vector2(0, -35), new Vector2(-40, 55), new Color(0.05f, 0.08f, 0.15f, 0.88f));
            topTurnBanner = bannerObj.GetComponent<RectTransform>();

            // Texto da Rodada
            roundText = CreateUIText(bannerObj.transform, "RoundText", "RODADA 1", 13, FontStyle.Bold, 
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), 
                new Vector2(15, 0), new Vector2(110, 40), new Color(0f, 0.9f, 1f));

            // Nome da Unidade e Time
            unitNameText = CreateUIText(bannerObj.transform, "UnitNameText", "Turno: Aethel", 16, FontStyle.Bold, 
                new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), 
                new Vector2(130, 0), new Vector2(250, 40), Color.white);

            // Categoria e Protocolo
            unitCategoryText = CreateUIText(bannerObj.transform, "UnitCategoryText", "[SYSTEM | FIREWALL]", 13, FontStyle.Bold, 
                new Vector2(0.5f, 0.5f), new Vector2(0.8f, 0.5f), new Vector2(0.5f, 0.5f), 
                new Vector2(0, 0), new Vector2(240, 40), new Color(0.4f, 0.9f, 1f));

            // HP e SP
            unitHpSpText = CreateUIText(bannerObj.transform, "UnitHpSpText", "HP: 120/120  |  SP: 60/60", 14, FontStyle.Bold, 
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), 
                new Vector2(-15, 0), new Vector2(230, 40), new Color(0.3f, 1f, 0.5f), TextAnchor.MiddleRight);
        }

        // 2. BOTTOM CONTROLS PANEL (Guia de Comandos Básicos)
        if (bottomControlsPanel == null)
        {
            GameObject controlsObj = CreateUIPanel(canvas.transform, "BottomControlsPanel", 
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), 
                new Vector2(20, 20), new Vector2(330, 115), new Color(0.04f, 0.07f, 0.12f, 0.92f));
            bottomControlsPanel = controlsObj.GetComponent<RectTransform>();

            // Título do Modo / Estado
            stateTitleText = CreateUIText(controlsObj.transform, "StateTitle", "COMANDOS BÁSICOS", 12, FontStyle.Bold, 
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), 
                new Vector2(12, -14), new Vector2(-24, 24), new Color(0f, 0.9f, 1f), TextAnchor.UpperLeft);

            // Lista de Teclas e Ações
            controlsBodyText = CreateUIText(controlsObj.transform, "ControlsBody", 
                "• [◄ / ► ou A / D] : Navegar Opções\n• [ESPAÇO / ENTER] : Confirmar\n• [X / ESC] : Modo Livre (Grid)", 
                12, FontStyle.Normal, 
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), 
                new Vector2(12, 10), new Vector2(-24, -36), new Color(0.9f, 0.95f, 1f), TextAnchor.UpperLeft);
        }

        // 3. TARGET INSPECTOR PANEL (Canto Superior Direito)
        if (targetInfoPanel == null)
        {
            GameObject targetObj = CreateUIPanel(canvas.transform, "TargetInspectorPanel", 
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), 
                new Vector2(-20, -100), new Vector2(240, 80), new Color(0.05f, 0.08f, 0.14f, 0.85f));
            targetInfoPanel = targetObj.GetComponent<RectTransform>();

            targetNameText = CreateUIText(targetObj.transform, "TargetName", "Tile: (0, 0, 0)", 13, FontStyle.Bold, 
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), 
                new Vector2(10, -10), new Vector2(-20, 24), new Color(0f, 0.9f, 1f), TextAnchor.UpperLeft);

            targetStatsText = CreateUIText(targetObj.transform, "TargetStats", "Nenhuma unidade selecionada.", 11, FontStyle.Normal, 
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), 
                new Vector2(10, 8), new Vector2(-20, -32), new Color(0.85f, 0.9f, 0.95f), TextAnchor.UpperLeft);
        }
    }

    // Atualiza o banner do turno ativo
    public void UpdateTurnBanner(Unit unit)
    {
        if (unit == null) return;

        if (roundText != null && BattleController.Instance != null)
        {
            roundText.text = $"RODADA {BattleController.Instance.roundCount}";
        }

        if (unitNameText != null)
        {
            string teamBadge = unit.team == Team.Player ? "<color=#00FFFF>[JOGADOR]</color>" : "<color=#FF3366>[INIMIGO]</color>";
            unitNameText.text = $"Turno: {unit.unitName} {teamBadge}";
        }

        if (unitCategoryText != null)
        {
            string catColor = GetCategoryHexColor(unit.category);
            unitCategoryText.text = $"<color={catColor}>[{unit.category.ToString().ToUpper()}]</color> | <color=#FFCC00>[{unit.protocol.ToString().ToUpper()}]</color>";
        }

        if (unitHpSpText != null && unit.stats != null)
        {
            int hp = unit.stats.GetStat(StatEnum.HP);
            int maxHp = unit.stats.GetStat(StatEnum.MaxHp);
            int sp = unit.stats.GetStat(StatEnum.SP);
            int maxSp = unit.stats.GetStat(StatEnum.MaxSp);
            unitHpSpText.text = $"HP: <color=#33FF88>{hp}/{maxHp}</color>  |  SP: <color=#33CCFF>{sp}/{maxSp}</color>";
        }
    }

    // Atualiza os prompts contextuais de comandos na tela
    public void UpdateControlsPrompt(string title, string body)
    {
        if (stateTitleText != null)
        {
            stateTitleText.text = $"<b>{title.ToUpper()}</b>";
        }
        if (controlsBodyText != null)
        {
            controlsBodyText.text = body;
        }
    }

    void UpdateInspectorPanel()
    {
        if (Selector.Instance == null || Selector.Instance.tile == null) return;

        TileLogic tile = Selector.Instance.tile;

        if (targetNameText != null)
        {
            targetNameText.text = $"Tile: ({tile.pos.x}, {tile.pos.y}) [Piso: {tile.floor?.name ?? "Base"}]";
        }

        if (targetStatsText != null)
        {
            if (tile.content != null)
            {
                Unit u = tile.content.GetComponent<Unit>();
                if (u != null)
                {
                    int hp = u.stats.GetStat(StatEnum.HP);
                    int maxHp = u.stats.GetStat(StatEnum.MaxHp);
                    int sp = u.stats.GetStat(StatEnum.SP);
                    int spd = u.stats.GetStat(StatEnum.SPEED);
                    string side = u.team == Team.Player ? "<color=#00FFFF>Aliado</color>" : "<color=#FF3366>Inimigo</color>";
                    targetStatsText.text = $"<b>{u.unitName}</b> ({side})\n" +
                                          $"Tipo: {u.category} | Olhando: {u.facing}\n" +
                                          $"HP: {hp}/{maxHp} | SP: {sp} | VEL: {spd}";
                    return;
                }
            }
            targetStatsText.text = "<color=#888888>Nenhuma unidade neste tile.</color>";
        }
    }

    void OnBattleEnd(Team winner)
    {
        if (unitNameText != null)
        {
            unitNameText.text = $"<color=#FFCC00>FIM DA BATALHA! Vencedor: {winner}</color>";
        }
        UpdateControlsPrompt("BATALHA CONCLUÍDA", "Combate finalizado com sucesso.");
    }

    string GetCategoryHexColor(FunctionalCategory cat)
    {
        switch (cat)
        {
            case FunctionalCategory.Social: return "#FFD700";       // Dourado
            case FunctionalCategory.Navi: return "#00D4FF";         // Ciano
            case FunctionalCategory.Tool: return "#FF8833";         // Laranja
            case FunctionalCategory.Game: return "#00FF66";         // Verde
            case FunctionalCategory.Entertainment: return "#FF33CC";// Magenta
            case FunctionalCategory.Life: return "#33FF99";         // Esmeralda
            case FunctionalCategory.System: return "#AA66FF";       // Violeta
            default: return "#FFFFFF";
        }
    }

    #region UI Helper Methods
    GameObject CreateUIPanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta, Color bgColor)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Outline));
        panel.transform.SetParent(parent, false);

        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;

        Image img = panel.GetComponent<Image>();
        img.color = bgColor;

        Outline outline = panel.GetComponent<Outline>();
        outline.effectColor = new Color(0f, 0.8f, 1f, 0.4f);
        outline.effectDistance = new Vector2(1f, -1f);

        return panel;
    }

    Text CreateUIText(Transform parent, string name, string initialText, int fontSize, FontStyle style, 
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta, 
        Color textColor, TextAnchor alignment = TextAnchor.MiddleLeft)
    {
        GameObject textObj = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Shadow));
        textObj.transform.SetParent(parent, false);

        RectTransform rt = textObj.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;

        Text txt = textObj.GetComponent<Text>();
        txt.font = hudFont;
        txt.text = initialText;
        txt.fontSize = fontSize;
        txt.fontStyle = style;
        txt.color = textColor;
        txt.alignment = alignment;
        txt.supportRichText = true;

        Shadow shadow = textObj.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
        shadow.effectDistance = new Vector2(1f, -1f);

        return txt;
    }
    #endregion
}
