using System;
using System.Collections;
using System.Collections.Generic;
using TacticalBattle.Core;
using UnityEngine;
using UnityEngine.UI;

public class BattleHUD : MonoBehaviour
{
    public static BattleHUD Instance;

    [Header("Canvas Reference")]
    public Canvas canvas;

    // --- PAINEL DE INFORMAÇÕES DO JOGADOR (Top-Left - Estilo Digimon Survive) ---
    private GameObject playerInfoCard;
    private Image avatarImage;
    private Text unitNameText;
    private Text unitLevelText;
    private Text unitStageText;
    private Text unitRoleText;
    private RectTransform hpFillRect;
    private Image hpFillImage;
    private Text hpValueText;
    private RectTransform spFillRect;
    private Image spFillImage;
    private Text spValueText;
    private float maxBarWidth = 175f;

    // --- PAINEL DE MENU DE AÇÕES NA VERTICAL (Top-Left - Estilo Digimon Survive Imagem 3) ---
    private GameObject actionMenuContainer;
    private List<ActionMenuItemUI> actionMenuItems = new List<ActionMenuItemUI>();
    private readonly string[] actionNames = new string[]
    {
        "Move",
        "Attack",
        "Item",
        "Evolution",
        "Talk",
        "End Turn"
    };
    private readonly string[] actionIcons = new string[]
    {
        "⤢", // Move
        "⚡", // Attack
        "🏺", // Item
        "📈", // Evolution
        "💬", // Talk
        "⏭"  // End Turn
    };

    // --- CONTADOR DE TURNOS (Top-Right) ---
    private GameObject turnCounterPanel;
    private Text turnCounterText;

    // --- BARRA INFERIOR DE COMANDOS (Bottom Prompt) ---
    private GameObject bottomPromptBar;
    private Text promptTitleText;
    private Text promptControlsText;

    private Font hudFont;
    private Unit cachedCurrentUnit;

    void Awake()
    {
        Instance = this;

        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("BattleCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                canvasObj.AddComponent<GraphicRaycaster>();
            }
        }

        HideLegacyScenePanels();

        hudFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (hudFont == null) hudFont = Font.CreateDynamicFontFromOSFont("Arial", 14);

        BuildDigimonSurviveHUD();
    }

    void Start()
    {
        HideLegacyScenePanels();

        if (BattleController.Instance != null)
        {
            BattleController.Instance.OnTurnStart += UpdateTurnBanner;
            BattleController.Instance.OnBattleEnd += OnBattleEnd;
            if (BattleController.Instance.currentUnit != null)
            {
                UpdateTurnBanner(BattleController.Instance.currentUnit);
            }
        }

        UpdateControlsPrompt("MENU DE AÇÕES", "• [W / S ou SETAS] : Selecionar Ação    [ESPAÇO / ENTER] : Confirmar    [X / ESC] : Modo Livre");
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
        HideLegacyScenePanels();

        if (cachedCurrentUnit == null && BattleController.Instance != null)
        {
            if (BattleController.Instance.currentUnit != null)
            {
                UpdateTurnBanner(BattleController.Instance.currentUnit);
            }
        }
    }

    void HideLegacyScenePanels()
    {
        if (StateMachineController.Instance != null)
        {
            if (StateMachineController.Instance.ChooseActionPanel != null)
            {
                StateMachineController.Instance.ChooseActionPanel.gameObject.SetActive(false);
            }
            if (StateMachineController.Instance.chaooseActionSelected != null)
            {
                StateMachineController.Instance.chaooseActionSelected.gameObject.SetActive(false);
            }
            if (StateMachineController.Instance.ChooseActionButton != null)
            {
                foreach (var btn in StateMachineController.Instance.ChooseActionButton)
                {
                    if (btn != null && btn.gameObject.activeSelf) btn.gameObject.SetActive(false);
                }
            }
        }

        var legacyPanels = FindObjectsByType<PanelPositioner>(FindObjectsSortMode.None);
        foreach (var p in legacyPanels)
        {
            if (p != null && p.gameObject != gameObject && p.gameObject.activeSelf)
            {
                p.gameObject.SetActive(false);
            }
        }
    }

    #region Digimon Survive HUD Construction

    void BuildDigimonSurviveHUD()
    {
        if (canvas == null) return;

        try
        {
            // 1. PAINEL DE INFORMAÇÕES DO JOGADOR (Top-Left)
            BuildPlayerInfoCard();

            // 2. PAINEL DE MENU DE AÇÕES NA VERTICAL (Top-Left diretamente abaixo do Card)
            BuildActionMenu();

            // 3. INDICADOR DE RODADA / TURNO (Top-Right)
            BuildTurnCounter();

            // 4. BARRA DE COMANDOS DA BASE (Bottom Prompt)
            BuildBottomPromptBar();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BattleHUD] Erro ao construir UI: {ex.Message}\n{ex.StackTrace}");
        }
    }

    void BuildPlayerInfoCard()
    {
        // Container principal no Top-Left: Pivot (0, 1), Anchor (0, 1)
        playerInfoCard = CreateUIPanel(canvas.transform, "PlayerInfoCard",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(25, -25), new Vector2(380, 102), new Color(0.06f, 0.09f, 0.14f, 0.88f));

        // Moldura ciano externa
        CreateUIPanel(playerInfoCard.transform, "CardBorder",
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0f, 0.75f, 0.95f, 0.35f));

        // Fundo interno
        GameObject innerBg = CreateUIPanel(playerInfoCard.transform, "CardInnerBg",
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(-4, -4), new Color(0.05f, 0.08f, 0.13f, 0.95f));

        // --- ROW 1: HEADER (Nome e Nível) ---
        unitNameText = CreateUIText(innerBg.transform, "UnitName", "Agumon", 18, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(14, -6), new Vector2(230, 24), Color.white, TextAnchor.MiddleLeft);

        unitLevelText = CreateUIText(innerBg.transform, "UnitLevel", "LV3", 15, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(265, -6), new Vector2(100, 24), new Color(0.98f, 0.88f, 0.45f), TextAnchor.MiddleRight);

        // Linha divisória
        CreateUIDivider(innerBg.transform, new Vector2(10, -32), new Vector2(356, 1.5f), new Color(0.2f, 0.4f, 0.6f, 0.5f));

        // --- ROW 2: AVATAR / RETRATO (Esquerda) ---
        GameObject avatarFrame = CreateUIPanel(innerBg.transform, "AvatarFrame",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(12, -38), new Vector2(54, 54), new Color(0.15f, 0.3f, 0.45f, 0.8f));

        GameObject avatarInner = CreateUIPanel(avatarFrame.transform, "AvatarInner",
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(-2, -2), new Color(0.03f, 0.05f, 0.09f, 0.95f));

        avatarImage = avatarInner.GetComponent<Image>();
        avatarImage.color = new Color(1f, 0.85f, 0.3f, 0.9f);

        // --- ROW 2.1: STAGE & ROLE ---
        unitStageText = CreateUIText(innerBg.transform, "UnitStage", "Rookie", 13, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(74, -38), new Vector2(75, 18), Color.white, TextAnchor.MiddleLeft);

        unitRoleText = CreateUIText(innerBg.transform, "UnitRole", "All-Rounder", 13, FontStyle.Normal,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(158, -38), new Vector2(210, 18), new Color(0.9f, 0.95f, 1f), TextAnchor.MiddleLeft);

        // --- ROW 2.2: BARRA DE HP ---
        CreateUIText(innerBg.transform, "HP_Label", "HP", 11, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(74, -58), new Vector2(25, 14), new Color(0.2f, 0.9f, 1f), TextAnchor.MiddleLeft);

        GameObject hpBg = CreateUIPanel(innerBg.transform, "HP_Bg",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(104, -60), new Vector2(maxBarWidth, 11), new Color(0.08f, 0.12f, 0.18f, 1f));
        
        GameObject hpFill = CreateUIPanel(hpBg.transform, "HP_Fill",
            new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
            Vector2.zero, new Vector2(maxBarWidth, 0), new Color(0.12f, 0.68f, 0.98f, 1f)); // Azul Ciano Digimon
        hpFillRect = hpFill.GetComponent<RectTransform>();
        hpFillImage = hpFill.GetComponent<Image>();

        hpValueText = CreateUIText(innerBg.transform, "HP_Value", "387", 12, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(285, -58), new Vector2(80, 14), Color.white, TextAnchor.MiddleRight);

        // --- ROW 2.3: BARRA DE SP ---
        CreateUIText(innerBg.transform, "SP_Label", "SP", 11, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(74, -76), new Vector2(25, 14), new Color(1f, 0.85f, 0.2f), TextAnchor.MiddleLeft);

        GameObject spBg = CreateUIPanel(innerBg.transform, "SP_Bg",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(104, -78), new Vector2(maxBarWidth, 11), new Color(0.08f, 0.12f, 0.18f, 1f));
        
        GameObject spFill = CreateUIPanel(spBg.transform, "SP_Fill",
            new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
            Vector2.zero, new Vector2(maxBarWidth, 0), new Color(0.96f, 0.72f, 0.12f, 1f)); // Dourado Digimon
        spFillRect = spFill.GetComponent<RectTransform>();
        spFillImage = spFill.GetComponent<Image>();

        spValueText = CreateUIText(innerBg.transform, "SP_Value", "36", 12, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(285, -76), new Vector2(80, 14), Color.white, TextAnchor.MiddleRight);
    }

    void BuildActionMenu()
    {
        // Painel Vertical na Esquerda diretamente abaixo do Card de Informações (Top-Left)
        actionMenuContainer = CreateUIPanel(canvas.transform, "ActionMenuPanel",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(25, -132), new Vector2(220, 260), new Color(0f, 0f, 0f, 0f));

        actionMenuItems.Clear();

        for (int i = 0; i < actionNames.Length; i++)
        {
            float yPos = -i * 42f;
            GameObject itemObj = CreateUIPanel(actionMenuContainer.transform, $"ActionItem_{i}",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(0, yPos), new Vector2(0, 38), new Color(0.05f, 0.08f, 0.14f, 0.80f));

            // Borda do botão
            GameObject borderObj = CreateUIPanel(itemObj.transform, "ItemBorder",
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero, new Color(0.2f, 0.4f, 0.6f, 0.35f));

            GameObject innerItem = CreateUIPanel(itemObj.transform, "ItemInner",
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(-2, -2), new Color(0.05f, 0.08f, 0.14f, 0.90f));

            // Ícone circular na esquerda
            GameObject iconBg = CreateUIPanel(innerItem.transform, "IconBg",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(6, -5), new Vector2(26, 26), new Color(0.08f, 0.12f, 0.2f, 0.95f));

            Text iconText = CreateUIText(iconBg.transform, "IconText", actionIcons[i], 14, FontStyle.Bold,
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero, Color.white, TextAnchor.MiddleCenter);

            // Nome da Ação
            Text labelText = CreateUIText(innerItem.transform, "LabelText", actionNames[i], 15, FontStyle.Bold,
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f),
                new Vector2(40, 0), new Vector2(-44, 0), new Color(0.9f, 0.95f, 1f), TextAnchor.MiddleLeft);

            ActionMenuItemUI itemUI = new ActionMenuItemUI
            {
                container = itemObj,
                backgroundImage = innerItem.GetComponent<Image>(),
                borderImage = borderObj.GetComponent<Image>(),
                iconBackground = iconBg.GetComponent<Image>(),
                iconText = iconText,
                labelText = labelText
            };

            actionMenuItems.Add(itemUI);
        }

        UpdateActionMenuSelection(0, null);
    }

    void BuildTurnCounter()
    {
        turnCounterPanel = CreateUIPanel(canvas.transform, "TurnCounterPanel",
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-25, -25), new Vector2(110, 80), new Color(0.05f, 0.08f, 0.14f, 0.85f));

        CreateUIText(turnCounterPanel.transform, "TurnTitle", "TURNO", 12, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -10), new Vector2(0, 20), new Color(0.3f, 0.8f, 1f), TextAnchor.MiddleCenter);

        turnCounterText = CreateUIText(turnCounterPanel.transform, "TurnNumber", "1", 26, FontStyle.Bold,
            new Vector2(0f, 0f), new Vector2(1f, 0.65f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, Color.white, TextAnchor.MiddleCenter);
    }

    void BuildBottomPromptBar()
    {
        bottomPromptBar = CreateUIPanel(canvas.transform, "BottomPromptBar",
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0, 15), new Vector2(-50, 45), new Color(0.04f, 0.06f, 0.10f, 0.92f));

        promptTitleText = CreateUIText(bottomPromptBar.transform, "PromptTitle", "SELEÇÃO DE AÇÃO", 12, FontStyle.Bold,
            new Vector2(0f, 0f), new Vector2(0.3f, 1f), new Vector2(0f, 0.5f),
            new Vector2(20, 0), new Vector2(0, 0), new Color(0f, 0.85f, 1f), TextAnchor.MiddleLeft);

        promptControlsText = CreateUIText(bottomPromptBar.transform, "PromptControls", 
            "• [W / S ou SETAS] : Selecionar Ação    [ESPAÇO / ENTER] : Confirmar    [X / ESC] : Modo Livre", 12, FontStyle.Normal,
            new Vector2(0.3f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f),
            new Vector2(-20, 0), new Vector2(0, 0), new Color(0.9f, 0.95f, 1f), TextAnchor.MiddleRight);
    }

    #endregion

    #region Public HUD Updates

    public void UpdateTurnBanner(Unit unit)
    {
        if (unit == null) return;
        cachedCurrentUnit = unit;

        try
        {
            if (turnCounterText != null && BattleController.Instance != null)
            {
                turnCounterText.text = BattleController.Instance.roundCount.ToString();
            }

            if (unitNameText != null)
            {
                unitNameText.text = unit.unitName;
            }

            if (unitStageText != null)
            {
                unitStageText.text = unit.rank == EvolutionRank.Standard ? "Rookie" : unit.rank.ToString();
            }

            if (unitRoleText != null)
            {
                unitRoleText.text = unit.category.ToString();
                unitRoleText.color = GetCategoryColor(unit.category);
            }

            if (unit.stats != null)
            {
                int hp = unit.stats.GetStat(StatEnum.HP);
                int maxHp = unit.stats.GetStat(StatEnum.MaxHp);
                int sp = unit.stats.GetStat(StatEnum.SP);
                int maxSp = unit.stats.GetStat(StatEnum.MaxSp);

                if (hpValueText != null) hpValueText.text = $"{hp}";
                if (hpFillRect != null)
                {
                    float ratio = maxHp > 0 ? Mathf.Clamp01((float)hp / maxHp) : 1f;
                    hpFillRect.sizeDelta = new Vector2(maxBarWidth * ratio, 0);
                }

                if (spValueText != null) spValueText.text = $"{sp}";
                if (spFillRect != null)
                {
                    float ratio = maxSp > 0 ? Mathf.Clamp01((float)sp / maxSp) : 1f;
                    spFillRect.sizeDelta = new Vector2(maxBarWidth * ratio, 0);
                }
            }

            if (avatarImage != null && unit.spriteRenderer != null && unit.spriteRenderer.sprite != null)
            {
                avatarImage.sprite = unit.spriteRenderer.sprite;
                avatarImage.color = Color.white;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[BattleHUD] Aviso ao atualizar banner: {ex.Message}");
        }
    }

    public void UpdateActionMenuSelection(int selectedIndex, Unit unit = null)
    {
        if (unit != null) cachedCurrentUnit = unit;

        for (int i = 0; i < actionMenuItems.Count; i++)
        {
            bool isSelected = (i == selectedIndex);
            var item = actionMenuItems[i];
            if (item.container == null) continue;

            bool isOptionAvailable = true;
            if (i == 0 && cachedCurrentUnit != null && !cachedCurrentUnit.CanMove())
            {
                isOptionAvailable = false;
            }

            if (isSelected)
            {
                // Destaque DOURADO / AMARELO VIBRANTE (Estilo Digimon Survive Imagem 3)
                item.backgroundImage.color = new Color(0.98f, 0.82f, 0.0f, 0.98f); // #F7D000 Amarelo Ouro
                item.labelText.color = new Color(0.08f, 0.08f, 0.08f, 1f); // Texto Preto em negrito
                item.labelText.fontStyle = FontStyle.Bold;

                item.iconBackground.color = new Color(0.12f, 0.12f, 0.12f, 0.95f);
                item.iconText.color = new Color(0.98f, 0.82f, 0.0f, 1f);

                if (item.borderImage != null)
                {
                    item.borderImage.color = new Color(1f, 0.95f, 0.4f, 0.9f);
                }
            }
            else
            {
                // Não selecionado: Vidro Escuro Translúcido
                float alpha = isOptionAvailable ? 0.80f : 0.35f;
                item.backgroundImage.color = new Color(0.05f, 0.08f, 0.14f, alpha);
                item.labelText.color = isOptionAvailable ? new Color(0.9f, 0.95f, 1f, 0.9f) : new Color(0.5f, 0.55f, 0.6f, 0.6f);
                item.labelText.fontStyle = FontStyle.Bold;

                item.iconBackground.color = new Color(0.08f, 0.12f, 0.2f, alpha);
                item.iconText.color = isOptionAvailable ? Color.white : new Color(0.6f, 0.6f, 0.6f, 0.6f);

                if (item.borderImage != null)
                {
                    item.borderImage.color = new Color(0.2f, 0.4f, 0.6f, 0.3f);
                }
            }
        }
    }

    public void ShowActionMenu(bool show)
    {
        if (actionMenuContainer != null)
        {
            actionMenuContainer.SetActive(show);
        }
    }

    public void UpdateControlsPrompt(string title, string body)
    {
        if (promptTitleText != null) promptTitleText.text = $"<b>{title.ToUpper()}</b>";
        if (promptControlsText != null) promptControlsText.text = body;
    }

    void OnBattleEnd(Team winner)
    {
        if (unitNameText != null)
        {
            unitNameText.text = $"VITORIOSO: {winner.ToString().ToUpper()}";
        }
        UpdateControlsPrompt("FIM DE COMBATE", "Batalha finalizada com sucesso.");
    }

    Color GetCategoryColor(FunctionalCategory cat)
    {
        return cat switch
        {
            FunctionalCategory.Social => new Color(1f, 0.85f, 0.2f),
            FunctionalCategory.Navi => new Color(0f, 0.85f, 1f),
            FunctionalCategory.Tool => new Color(1f, 0.55f, 0.2f),
            FunctionalCategory.Game => new Color(0.2f, 1f, 0.4f),
            FunctionalCategory.Entertainment => new Color(1f, 0.3f, 0.8f),
            FunctionalCategory.Life => new Color(0.3f, 1f, 0.7f),
            FunctionalCategory.System => new Color(0.7f, 0.4f, 1f),
            _ => Color.white
        };
    }

    #endregion

    #region UI Helper Methods

    GameObject CreateUIPanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta, Color bgColor)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;

        Image img = obj.GetComponent<Image>();
        img.color = bgColor;

        return obj;
    }

    Text CreateUIText(Transform parent, string name, string text, int fontSize, FontStyle style, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta, Color color, TextAnchor alignment = TextAnchor.MiddleCenter)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;

        Text txt = obj.GetComponent<Text>();
        txt.font = hudFont;
        txt.text = text;
        txt.fontSize = fontSize;
        txt.fontStyle = style;
        txt.color = color;
        txt.alignment = alignment;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        txt.raycastTarget = false;

        return txt;
    }

    void CreateUIDivider(Transform parent, Vector2 anchoredPos, Vector2 sizeDelta, Color color)
    {
        GameObject obj = new GameObject("Divider", typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;

        Image img = obj.GetComponent<Image>();
        img.color = color;
    }

    #endregion

    private class ActionMenuItemUI
    {
        public GameObject container;
        public Image backgroundImage;
        public Image borderImage;
        public Image iconBackground;
        public Text iconText;
        public Text labelText;
    }
}
