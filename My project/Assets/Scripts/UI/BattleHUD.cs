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
    private float maxBarWidth = 110f;

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

    // --- CONTADOR DE TURNOS & FILA DE TURNOS EM LOSANGO (Top-Right - Estilo Digimon Survive Imagem 1 e 2) ---
    private GameObject turnTimelineRoot;
    private Text turnCounterText;
    private DiamondQueueItemUI activeUnitDiamond;
    private List<DiamondQueueItemUI> upcomingDiamonds = new List<DiamondQueueItemUI>();
    private const int MAX_UPCOMING_SLOTS = 10;

    // --- BARRA INFERIOR DE COMANDOS (Bottom Prompt) ---
    private GameObject bottomPromptBar;
    private Text promptTitleText;
    private Text promptControlsText;

    // --- BANNER DE PREVISÃO DE COMBATE (Estilo Digimon Survive - Top Center) ---
    private GameObject combatForecastRoot;
    
    // Atacante (Card Esquerdo)
    private Text fcAttackerName;
    private Text fcAttackerLevel;
    private Image fcAttackerAvatar;
    private Text fcAttackerStage;
    private Text fcAttackerRole;
    private Text fcAttackerHpText;
    private RectTransform fcAttackerHpFill;
    private Text fcAttackerSpText;
    private RectTransform fcAttackerSpFill;
    private Text fcAttackerSkillName;
    private Text fcAttackerSkillCost;
    
    // Centro (Resolução de Combate)
    private Text fcMultiplierText;
    private Text fcDamageText;
    private Text fcAccuracyText;
    private Text fcCounterattackText;
    private Text fcOrientationText;
    private Text fcCritChanceText;
    private Text fcAttackerCatIcon;
    private Text fcDefenderCatIcon;
    
    // Defensor (Card Direito)
    private Text fcDefenderName;
    private Text fcDefenderLevel;
    private Image fcDefenderAvatar;
    private Text fcDefenderStage;
    private Text fcDefenderRole;
    private Text fcDefenderHpText;
    private RectTransform fcDefenderHpFill;
    private Text fcDefenderSpText;
    private RectTransform fcDefenderSpFill;
    private Text fcDefenderResistances;

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
            BattleController.Instance.OnTurnStart += OnTurnChanged;
            BattleController.Instance.OnTurnEnd += OnTurnEnded;
            BattleController.Instance.OnBattleEnd += OnBattleEnd;
            if (BattleController.Instance.currentUnit != null)
            {
                UpdateTurnBanner(BattleController.Instance.currentUnit);
            }
            else
            {
                UpdateTurnTimeline(null);
            }
        }

        UpdateControlsPrompt("MENU DE AÇÕES", "• [W / S ou SETAS] : Selecionar Ação    [ESPAÇO / ENTER] : Confirmar    [X / ESC] : Modo Livre");
    }

    void OnDestroy()
    {
        if (BattleController.Instance != null)
        {
            BattleController.Instance.OnTurnStart -= OnTurnChanged;
            BattleController.Instance.OnTurnEnd -= OnTurnEnded;
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
            else
            {
                UpdateTurnTimeline(null);
            }
        }

        // Pulso de brilho suave no losango da unidade ativa (Efeito AAA estilo Digimon Survive)
        if (activeUnitDiamond != null && activeUnitDiamond.borderImage != null && activeUnitDiamond.rootObj != null && activeUnitDiamond.rootObj.activeSelf)
        {
            float pulse = 0.72f + Mathf.PingPong(Time.time * 2.0f, 0.28f);
            Color c = activeUnitDiamond.borderImage.color;
            c.a = pulse;
            activeUnitDiamond.borderImage.color = c;
        }

        // Sincroniza contador de turnos / rodada
        if (turnCounterText != null && BattleController.Instance != null)
        {
            int currentRound = BattleController.Instance.roundCount > 0 ? BattleController.Instance.roundCount : 1;
            string roundStr = currentRound.ToString();
            if (turnCounterText.text != roundStr)
            {
                turnCounterText.text = roundStr;
            }
        }
    }

    void OnTurnChanged(Unit unit)
    {
        UpdateTurnBanner(unit);
    }

    void OnTurnEnded(Unit unit)
    {
        UpdateTurnTimeline(null);
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

            // 3. INDICADOR DE RODADA / LINHA DO TEMPO EM LOSANGO (Top-Right - Estilo Digimon Survive Imagem 1)
            BuildDiamondTurnTimeline();

            // 4. BARRA DE COMANDOS DA BASE (Bottom Prompt)
            BuildBottomPromptBar();

            // 5. BANNER DE PREVISÃO DE COMBATE (Estilo Digimon Survive - Top Center)
            BuildCombatForecastBanner();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BattleHUD] Erro ao construir UI: {ex.Message}\n{ex.StackTrace}");
        }
    }

    void BuildPlayerInfoCard()
    {
        // Container principal no Top-Left: Pivot (0, 1), Anchor (0, 1) - Estilo compacto Digimon Survive
        playerInfoCard = CreateUIPanel(canvas.transform, "PlayerInfoCard",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(20, -20), new Vector2(265, 74), new Color(0.08f, 0.11f, 0.15f, 0.85f));

        // Moldura fina translúcida
        CreateUIPanel(playerInfoCard.transform, "CardBorder",
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0.35f, 0.42f, 0.50f, 0.45f));

        // Fundo interno
        GameObject innerBg = CreateUIPanel(playerInfoCard.transform, "CardInnerBg",
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(-2, -2), new Color(0.07f, 0.09f, 0.13f, 0.95f));

        // --- ROW 1: HEADER (Nome e Nível) ---
        unitNameText = CreateUIText(innerBg.transform, "UnitName", "Agumon", 14, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(10, -5), new Vector2(150, 18), Color.white, TextAnchor.MiddleLeft);

        unitLevelText = CreateUIText(innerBg.transform, "UnitLevel", "LV3", 12, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(165, -5), new Vector2(90, 18), new Color(0.92f, 0.92f, 0.92f), TextAnchor.MiddleRight);

        // Linha divisória sutil
        CreateUIDivider(innerBg.transform, new Vector2(6, -24), new Vector2(251, 1), new Color(0.3f, 0.38f, 0.48f, 0.40f));

        // --- ROW 2: AVATAR / RETRATO (Esquerda) ---
        GameObject avatarFrame = CreateUIPanel(innerBg.transform, "AvatarFrame",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(8, -27), new Vector2(42, 42), new Color(0.12f, 0.16f, 0.22f, 0.85f));

        GameObject avatarInner = CreateUIPanel(avatarFrame.transform, "AvatarInner",
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(-2, -2), new Color(0.03f, 0.05f, 0.08f, 0.95f));

        avatarImage = avatarInner.GetComponent<Image>();
        avatarImage.color = new Color(1f, 0.85f, 0.3f, 0.9f);

        // --- ROW 2.1: STAGE & ROLE ---
        unitStageText = CreateUIText(innerBg.transform, "UnitStage", "Rookie", 10, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(56, -26), new Vector2(55, 14), Color.white, TextAnchor.MiddleLeft);

        unitRoleText = CreateUIText(innerBg.transform, "UnitRole", "All-Rounder", 10, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(115, -26), new Vector2(140, 14), new Color(0.80f, 0.88f, 0.96f), TextAnchor.MiddleLeft);

        // --- LOSANGO DECORATIVO DE STATUS (Estilo Digimon Survive) ---
        GameObject hpDiamond = CreateUIPanel(innerBg.transform, "HP_Diamond",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f),
            new Vector2(64, -54), new Vector2(11, 11), new Color(0.06f, 0.10f, 0.16f, 0.95f));
        hpDiamond.transform.localEulerAngles = new Vector3(0, 0, 45f);
        CreateUIPanel(hpDiamond.transform, "DiamondBorder",
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(1.5f, 1.5f), new Color(0.2f, 0.75f, 0.95f, 0.80f));
        CreateUIText(hpDiamond.transform, "DiamondIcon", "◈", 8, FontStyle.Bold,
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0.2f, 0.85f, 1f), TextAnchor.MiddleCenter);

        // --- ROW 2.2: BARRA DE HP ---
        CreateUIText(innerBg.transform, "HP_Label", "HP", 9, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(76, -46), new Vector2(18, 12), new Color(0.2f, 0.85f, 1f), TextAnchor.MiddleLeft);

        GameObject hpBg = CreateUIPanel(innerBg.transform, "HP_Bg",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(96, -47), new Vector2(maxBarWidth, 8), new Color(0.04f, 0.07f, 0.12f, 0.95f));
        
        GameObject hpFill = CreateUIPanel(hpBg.transform, "HP_Fill",
            new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
            Vector2.zero, new Vector2(maxBarWidth, 0), new Color(0.08f, 0.68f, 0.98f, 1f)); // Azul Ciano Digimon
        hpFillRect = hpFill.GetComponent<RectTransform>();
        hpFillImage = hpFill.GetComponent<Image>();

        hpValueText = CreateUIText(innerBg.transform, "HP_Value", "387", 10, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(210, -46), new Vector2(48, 12), Color.white, TextAnchor.MiddleRight);

        // --- ROW 2.3: BARRA DE SP ---
        CreateUIText(innerBg.transform, "SP_Label", "SP", 9, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(76, -58), new Vector2(18, 12), new Color(1f, 0.75f, 0.15f), TextAnchor.MiddleLeft);

        GameObject spBg = CreateUIPanel(innerBg.transform, "SP_Bg",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(96, -59), new Vector2(maxBarWidth, 8), new Color(0.04f, 0.07f, 0.12f, 0.95f));
        
        GameObject spFill = CreateUIPanel(spBg.transform, "SP_Fill",
            new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
            Vector2.zero, new Vector2(maxBarWidth, 0), new Color(0.96f, 0.68f, 0.08f, 1f)); // Dourado Digimon
        spFillRect = spFill.GetComponent<RectTransform>();
        spFillImage = spFill.GetComponent<Image>();

        spValueText = CreateUIText(innerBg.transform, "SP_Value", "36", 10, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(210, -58), new Vector2(48, 12), Color.white, TextAnchor.MiddleRight);
    }

    void BuildActionMenu()
    {
        // Painel Vertical na Esquerda diretamente abaixo do Card de Informações (Top-Left)
        actionMenuContainer = CreateUIPanel(canvas.transform, "ActionMenuPanel",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(20, -100), new Vector2(145, 175), new Color(0f, 0f, 0f, 0f));

        actionMenuItems.Clear();

        for (int i = 0; i < actionNames.Length; i++)
        {
            float yPos = -i * 28f;
            GameObject itemObj = CreateUIPanel(actionMenuContainer.transform, $"ActionItem_{i}",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(0, yPos), new Vector2(0, 25), new Color(0.04f, 0.07f, 0.11f, 0.55f));

            // Borda sutil do botão
            GameObject borderObj = CreateUIPanel(itemObj.transform, "ItemBorder",
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero, new Color(0.25f, 0.32f, 0.40f, 0.25f));

            GameObject innerItem = CreateUIPanel(itemObj.transform, "ItemInner",
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(-2, -2), new Color(0.04f, 0.07f, 0.11f, 0.85f));

            // Ícone circular na esquerda
            GameObject iconBg = CreateUIPanel(innerItem.transform, "IconBg",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(4, -2.5f), new Vector2(20, 20), new Color(0.10f, 0.15f, 0.22f, 0.75f));

            Text iconText = CreateUIText(iconBg.transform, "IconText", actionIcons[i], 11, FontStyle.Bold,
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero, new Color(0.82f, 0.88f, 0.94f, 0.90f), TextAnchor.MiddleCenter);

            // Nome da Ação
            Text labelText = CreateUIText(innerItem.transform, "LabelText", actionNames[i], 12, FontStyle.Bold,
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f),
                new Vector2(28, 0), new Vector2(-30, 0), new Color(0.80f, 0.85f, 0.92f, 0.85f), TextAnchor.MiddleLeft);

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

    void BuildDiamondTurnTimeline()
    {
        // Container principal da timeline no Top-Right
        turnTimelineRoot = CreateUIPanel(canvas.transform, "TurnTimelineRoot",
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-15, -15), new Vector2(100, 420), new Color(0, 0, 0, 0));

        // 1. LOSANGO DO CABEÇALHO DO TURNO ("Turno 1") - Fixo no topo direito
        GameObject turnDiamond = CreateUIPanel(turnTimelineRoot.transform, "TurnDiamondHeader",
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            new Vector2(-32, -32), new Vector2(36, 36), new Color(0.10f, 0.08f, 0.06f, 0.88f));
        turnDiamond.transform.localEulerAngles = new Vector3(0, 0, 45f);

        // Borda fina translúcida do losango de turno (1px outline)
        CreateUIPanel(turnDiamond.transform, "TurnBorder",
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(2, 2), new Color(0.75f, 0.70f, 0.65f, 0.45f));

        // Container de texto nivelado no mesmo centro (garante que nada tampe o texto)
        GameObject textContainer = new GameObject("TextContainer", typeof(RectTransform));
        textContainer.transform.SetParent(turnTimelineRoot.transform, false);
        RectTransform textContainerRt = textContainer.GetComponent<RectTransform>();
        textContainerRt.anchorMin = new Vector2(1f, 1f);
        textContainerRt.anchorMax = new Vector2(1f, 1f);
        textContainerRt.pivot = new Vector2(0.5f, 0.5f);
        textContainerRt.anchoredPosition = new Vector2(-32, -32);
        textContainerRt.sizeDelta = new Vector2(36, 36);

        CreateUIText(textContainer.transform, "TurnTitle", "Turno", 9, FontStyle.Bold,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 5), new Vector2(36, 12), new Color(0.92f, 0.90f, 0.88f), TextAnchor.MiddleCenter);

        turnCounterText = CreateUIText(textContainer.transform, "TurnNumber", "1", 14, FontStyle.Bold,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, -5), new Vector2(36, 16), Color.white, TextAnchor.MiddleCenter);

        // 2. LOSANGO DA UNIDADE ATIVA (Posicionado abaixo-esquerda, tocando o losango de turno sem cobri-lo)
        activeUnitDiamond = CreateDiamondQueueItem(turnTimelineRoot.transform, "ActiveUnitDiamond",
            new Vector2(-58, -70), new Vector2(36, 36), true);

        // 3. LOSANGOS DAS PRÓXIMAS UNIDADES NA FILA (Cascata contínua em zigue-zague com 10 slots)
        upcomingDiamonds.Clear();
        Vector2[] slotOffsets = new Vector2[]
        {
            new Vector2(-38, -98),
            new Vector2(-54, -118),
            new Vector2(-38, -138),
            new Vector2(-54, -158),
            new Vector2(-38, -178),
            new Vector2(-54, -198),
            new Vector2(-38, -218),
            new Vector2(-54, -238),
            new Vector2(-38, -258),
            new Vector2(-54, -278),
        };

        for (int i = 0; i < slotOffsets.Length; i++)
        {
            var item = CreateDiamondQueueItem(turnTimelineRoot.transform, $"UpcomingDiamond_{i}",
                slotOffsets[i], new Vector2(22, 22), false);
            upcomingDiamonds.Add(item);
        }
    }

    DiamondQueueItemUI CreateDiamondQueueItem(Transform parent, string name, Vector2 pos, Vector2 size, bool isActive)
    {
        // Raiz do losango rotacionada a 45°
        GameObject rootObj = new GameObject(name, typeof(RectTransform));
        rootObj.transform.SetParent(parent, false);

        RectTransform rootRt = rootObj.GetComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(1f, 1f);
        rootRt.anchorMax = new Vector2(1f, 1f);
        rootRt.pivot = new Vector2(0.5f, 0.5f);
        rootRt.anchoredPosition = pos;
        rootRt.sizeDelta = size;
        rootRt.localEulerAngles = new Vector3(0, 0, 45f);

        // Borda fina elegante (1.5px para ativo, 1px para fila)
        GameObject borderObj = CreateUIPanel(rootObj.transform, "Border",
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            Vector2.zero, isActive ? new Vector2(2.5f, 2.5f) : new Vector2(1.5f, 1.5f),
            isActive ? new Color(0f, 0.85f, 1f, 0.85f) : new Color(1f, 1f, 1f, 0.35f));
        Image borderImg = borderObj.GetComponent<Image>();

        // Máscara com fundo escuro fumê translúcido
        GameObject maskObj = new GameObject("DiamondMask", typeof(RectTransform), typeof(Image), typeof(Mask));
        maskObj.transform.SetParent(rootObj.transform, false);

        RectTransform maskRt = maskObj.GetComponent<RectTransform>();
        maskRt.anchorMin = new Vector2(0f, 0f);
        maskRt.anchorMax = new Vector2(1f, 1f);
        maskRt.pivot = new Vector2(0.5f, 0.5f);
        maskRt.anchoredPosition = Vector2.zero;
        maskRt.sizeDelta = Vector2.zero;

        Image maskImg = maskObj.GetComponent<Image>();
        maskImg.color = isActive ? new Color(0.08f, 0.10f, 0.14f, 0.80f) : new Color(0.08f, 0.07f, 0.06f, 0.70f);

        Mask maskComp = maskObj.GetComponent<Mask>();
        maskComp.showMaskGraphic = true;

        // Imagem do Retrato do Digimon (contra-rotacionada a -45° e ampliada para focar no rosto/corpo superior)
        GameObject portraitObj = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
        portraitObj.transform.SetParent(maskObj.transform, false);

        RectTransform portraitRt = portraitObj.GetComponent<RectTransform>();
        portraitRt.anchorMin = new Vector2(0.5f, 0.5f);
        portraitRt.anchorMax = new Vector2(0.5f, 0.5f);
        portraitRt.pivot = new Vector2(0.5f, 0.5f);
        portraitRt.anchoredPosition = isActive ? new Vector2(0, -1.5f) : new Vector2(0, -1f);
        portraitRt.sizeDelta = isActive ? new Vector2(58, 58) : new Vector2(36, 36);
        portraitRt.localEulerAngles = new Vector3(0, 0, -45f);

        Image portraitImg = portraitObj.GetComponent<Image>();
        portraitImg.preserveAspect = true;
        portraitImg.raycastTarget = false;

        // Texto de Fallback (iniciais do Digimon se não houver sprite)
        GameObject textObj = new GameObject("FallbackText", typeof(RectTransform), typeof(Text));
        textObj.transform.SetParent(maskObj.transform, false);

        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0f, 0f);
        textRt.anchorMax = new Vector2(1f, 1f);
        textRt.pivot = new Vector2(0.5f, 0.5f);
        textRt.anchoredPosition = Vector2.zero;
        textRt.sizeDelta = Vector2.zero;
        textRt.localEulerAngles = new Vector3(0, 0, -45f);

        Text fallbackTxt = textObj.GetComponent<Text>();
        fallbackTxt.font = hudFont;
        fallbackTxt.fontSize = isActive ? 10 : 8;
        fallbackTxt.fontStyle = FontStyle.Bold;
        fallbackTxt.color = Color.white;
        fallbackTxt.alignment = TextAnchor.MiddleCenter;
        fallbackTxt.raycastTarget = false;
        textObj.SetActive(false);

        return new DiamondQueueItemUI
        {
            rootObj = rootObj,
            borderImage = borderImg,
            bgImage = maskImg,
            portraitImage = portraitImg,
            fallbackText = fallbackTxt
        };
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

    void BuildCombatForecastBanner()
    {
        // Container raiz do Banner de Previsão de Combate (Top Center)
        combatForecastRoot = CreateUIPanel(canvas.transform, "CombatForecastRoot",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -14), new Vector2(810, 88), new Color(0, 0, 0, 0));

        // 1. CARD DO ATACANTE (Esquerda - 270x86)
        GameObject attackerCard = CreateUIPanel(combatForecastRoot.transform, "AttackerCard",
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(0, 0), new Vector2(270, 86), new Color(0.08f, 0.11f, 0.15f, 0.88f));

        CreateUIPanel(attackerCard.transform, "AttackerBorder",
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0.35f, 0.42f, 0.50f, 0.45f));

        GameObject atkInner = CreateUIPanel(attackerCard.transform, "Inner",
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(-2, -2), new Color(0.07f, 0.09f, 0.13f, 0.95f));

        // Header Atacante
        fcAttackerName = CreateUIText(atkInner.transform, "Name", "MagnaAngemon", 13, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(8, -4), new Vector2(160, 16), Color.white, TextAnchor.MiddleLeft);

        fcAttackerLevel = CreateUIText(atkInner.transform, "Level", "LV64", 11, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(170, -4), new Vector2(90, 16), new Color(0.92f, 0.92f, 0.92f), TextAnchor.MiddleRight);

        CreateUIDivider(atkInner.transform, new Vector2(6, -21), new Vector2(256, 1), new Color(0.3f, 0.38f, 0.48f, 0.40f));

        // Avatar Atacante
        GameObject atkAvatarFrame = CreateUIPanel(atkInner.transform, "AvatarFrame",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(6, -24), new Vector2(38, 38), new Color(0.12f, 0.16f, 0.22f, 0.85f));
        GameObject atkAvatarInner = CreateUIPanel(atkAvatarFrame.transform, "AvatarInner",
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(-2, -2), new Color(0.03f, 0.05f, 0.08f, 0.95f));
        fcAttackerAvatar = atkAvatarInner.GetComponent<Image>();

        // Stage & Role Atacante
        fcAttackerStage = CreateUIText(atkInner.transform, "Stage", "Ultimate", 9, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(48, -23), new Vector2(65, 13), Color.white, TextAnchor.MiddleLeft);

        fcAttackerRole = CreateUIText(atkInner.transform, "Role", "Special", 9, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(115, -23), new Vector2(145, 13), new Color(0.80f, 0.88f, 0.96f), TextAnchor.MiddleLeft);

        // Barras HP / SP Atacante
        CreateUIText(atkInner.transform, "HP_Label", "HP", 8, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(68, -38), new Vector2(16, 10), new Color(0.2f, 0.85f, 1f), TextAnchor.MiddleLeft);

        GameObject atkHpBg = CreateUIPanel(atkInner.transform, "HP_Bg",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(86, -39), new Vector2(105, 7), new Color(0.04f, 0.07f, 0.12f, 0.95f));
        GameObject atkHpFill = CreateUIPanel(atkHpBg.transform, "HP_Fill",
            new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
            Vector2.zero, new Vector2(105, 0), new Color(0.08f, 0.68f, 0.98f, 1f));
        fcAttackerHpFill = atkHpFill.GetComponent<RectTransform>();

        fcAttackerHpText = CreateUIText(atkInner.transform, "HP_Val", "1631", 9, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(195, -38), new Vector2(65, 10), Color.white, TextAnchor.MiddleRight);

        CreateUIText(atkInner.transform, "SP_Label", "SP", 8, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(68, -48), new Vector2(16, 10), new Color(1f, 0.75f, 0.15f), TextAnchor.MiddleLeft);

        GameObject atkSpBg = CreateUIPanel(atkInner.transform, "SP_Bg",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(86, -49), new Vector2(105, 7), new Color(0.04f, 0.07f, 0.12f, 0.95f));
        GameObject atkSpFill = CreateUIPanel(atkSpBg.transform, "SP_Fill",
            new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
            Vector2.zero, new Vector2(105, 0), new Color(0.96f, 0.68f, 0.08f, 1f));
        fcAttackerSpFill = atkSpFill.GetComponent<RectTransform>();

        fcAttackerSpText = CreateUIText(atkInner.transform, "SP_Val", "379", 9, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(195, -48), new Vector2(65, 10), Color.white, TextAnchor.MiddleRight);

        // Sub-barra de habilidade Atacante
        GameObject atkSkillBar = CreateUIPanel(atkInner.transform, "SkillBar",
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0, 3), new Vector2(-10, 18), new Color(0.04f, 0.06f, 0.09f, 0.90f));

        fcAttackerSkillName = CreateUIText(atkSkillBar.transform, "SkillName", "⚡ Ataque Físico", 9, FontStyle.Bold,
            new Vector2(0f, 0f), new Vector2(0.7f, 1f), new Vector2(0f, 0.5f),
            new Vector2(6, 0), Vector2.zero, new Color(0.95f, 0.95f, 0.95f), TextAnchor.MiddleLeft);

        fcAttackerSkillCost = CreateUIText(atkSkillBar.transform, "SkillCost", "SP 0", 9, FontStyle.Bold,
            new Vector2(0.7f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f),
            new Vector2(-6, 0), Vector2.zero, new Color(0.96f, 0.75f, 0.15f), TextAnchor.MiddleRight);

        // 2. CHEVRON 1 (» Esquerdo)
        CreateUIText(combatForecastRoot.transform, "ChevronLeft", "»", 22, FontStyle.Bold,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(285, 0), new Vector2(24, 30), new Color(0.88f, 0.92f, 0.96f, 0.90f), TextAnchor.MiddleCenter);

        // 3. CENTRO: RESOLUÇÃO DE COMBATE (210x86)
        GameObject centerBox = CreateUIPanel(combatForecastRoot.transform, "ForecastCenterBox",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(210, 86), new Color(0.06f, 0.08f, 0.12f, 0.90f));

        CreateUIPanel(centerBox.transform, "CenterBorder",
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0.35f, 0.42f, 0.50f, 0.45f));

        GameObject centerInner = CreateUIPanel(centerBox.transform, "Inner",
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(-2, -2), new Color(0.05f, 0.07f, 0.10f, 0.95f));

        // Coluna Esquerda: Ícones de Atributo e Multiplicador
        fcAttackerCatIcon = CreateUIText(centerInner.transform, "AtkIcon", "◈", 14, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f),
            new Vector2(18, -16), new Vector2(20, 20), new Color(0f, 0.85f, 1f), TextAnchor.MiddleCenter);

        CreateUIText(centerInner.transform, "Arrow", "►", 10, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f),
            new Vector2(36, -16), new Vector2(14, 14), Color.white, TextAnchor.MiddleCenter);

        fcDefenderCatIcon = CreateUIText(centerInner.transform, "DefIcon", "◈", 14, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f),
            new Vector2(54, -16), new Vector2(20, 20), new Color(1f, 0.55f, 0.2f), TextAnchor.MiddleCenter);

        fcMultiplierText = CreateUIText(centerInner.transform, "Multiplier", "x1.50 ATK", 9, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f),
            new Vector2(36, -34), new Vector2(68, 14), new Color(0.15f, 0.9f, 1f), TextAnchor.MiddleCenter);

        // Coluna Direita: Damage / Accuracy / Counterattack
        CreateUIText(centerInner.transform, "DmgLabel", "Damage", 9, FontStyle.Normal,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(80, -6), new Vector2(70, 14), new Color(0.85f, 0.88f, 0.92f), TextAnchor.MiddleLeft);

        fcDamageText = CreateUIText(centerInner.transform, "DamageVal", "918", 15, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(200, -5), new Vector2(60, 18), Color.white, TextAnchor.MiddleRight);

        CreateUIText(centerInner.transform, "AccLabel", "Accuracy", 9, FontStyle.Normal,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(80, -22), new Vector2(70, 13), new Color(0.85f, 0.88f, 0.92f), TextAnchor.MiddleLeft);

        fcAccuracyText = CreateUIText(centerInner.transform, "AccVal", "100%", 9, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(200, -22), new Vector2(50, 13), Color.white, TextAnchor.MiddleRight);

        CreateUIText(centerInner.transform, "CounterLabel", "Counterattack", 8, FontStyle.Normal,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(80, -36), new Vector2(80, 12), new Color(0.75f, 0.78f, 0.82f), TextAnchor.MiddleLeft);

        fcCounterattackText = CreateUIText(centerInner.transform, "CounterVal", "0%", 8, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(200, -36), new Vector2(40, 12), Color.white, TextAnchor.MiddleRight);

        // Sub-box inferior do Centro (Orientação e Crítico)
        GameObject centerSubBox = CreateUIPanel(centerInner.transform, "SubBox",
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0, 3), new Vector2(-10, 20), new Color(0.04f, 0.06f, 0.09f, 0.90f));

        fcOrientationText = CreateUIText(centerSubBox.transform, "Orientation", "Flank Attack", 9, FontStyle.Bold,
            new Vector2(0f, 0f), new Vector2(0.55f, 1f), new Vector2(0f, 0.5f),
            new Vector2(6, 0), Vector2.zero, new Color(1f, 0.65f, 0.15f), TextAnchor.MiddleLeft);

        fcCritChanceText = CreateUIText(centerSubBox.transform, "Crit", "Critical Hit 4%", 9, FontStyle.Bold,
            new Vector2(0.55f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f),
            new Vector2(-6, 0), Vector2.zero, new Color(1f, 0.85f, 0.15f), TextAnchor.MiddleRight);

        // 4. CHEVRON 2 (» Direito)
        CreateUIText(combatForecastRoot.transform, "ChevronRight", "»", 22, FontStyle.Bold,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-285, 0), new Vector2(24, 30), new Color(0.88f, 0.92f, 0.96f, 0.90f), TextAnchor.MiddleCenter);

        // 5. CARD DO DEFENSOR (Direita - 270x86)
        GameObject defenderCard = CreateUIPanel(combatForecastRoot.transform, "DefenderCard",
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(0, 0), new Vector2(270, 86), new Color(0.08f, 0.11f, 0.15f, 0.88f));

        CreateUIPanel(defenderCard.transform, "DefenderBorder",
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0.35f, 0.42f, 0.50f, 0.45f));

        GameObject defInner = CreateUIPanel(defenderCard.transform, "Inner",
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(-2, -2), new Color(0.07f, 0.09f, 0.13f, 0.95f));

        // Header Defensor
        fcDefenderName = CreateUIText(defInner.transform, "Name", "Palmon", 13, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(8, -4), new Vector2(160, 16), Color.white, TextAnchor.MiddleLeft);

        fcDefenderLevel = CreateUIText(defInner.transform, "Level", "LV44", 11, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(170, -4), new Vector2(90, 16), new Color(0.92f, 0.92f, 0.92f), TextAnchor.MiddleRight);

        CreateUIDivider(defInner.transform, new Vector2(6, -21), new Vector2(256, 1), new Color(0.3f, 0.38f, 0.48f, 0.40f));

        // Avatar Defensor
        GameObject defAvatarFrame = CreateUIPanel(defInner.transform, "AvatarFrame",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(6, -24), new Vector2(38, 38), new Color(0.12f, 0.16f, 0.22f, 0.85f));
        GameObject defAvatarInner = CreateUIPanel(defAvatarFrame.transform, "AvatarInner",
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(-2, -2), new Color(0.03f, 0.05f, 0.08f, 0.95f));
        fcDefenderAvatar = defAvatarInner.GetComponent<Image>();

        // Stage & Role Defensor
        fcDefenderStage = CreateUIText(defInner.transform, "Stage", "Rookie", 9, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(48, -23), new Vector2(65, 13), Color.white, TextAnchor.MiddleLeft);

        fcDefenderRole = CreateUIText(defInner.transform, "Role", "Special", 9, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(115, -23), new Vector2(145, 13), new Color(0.80f, 0.88f, 0.96f), TextAnchor.MiddleLeft);

        // Barras HP / SP Defensor
        CreateUIText(defInner.transform, "HP_Label", "HP", 8, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(68, -38), new Vector2(16, 10), new Color(0.2f, 0.85f, 1f), TextAnchor.MiddleLeft);

        GameObject defHpBg = CreateUIPanel(defInner.transform, "HP_Bg",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(86, -39), new Vector2(105, 7), new Color(0.04f, 0.07f, 0.12f, 0.95f));
        GameObject defHpFill = CreateUIPanel(defHpBg.transform, "HP_Fill",
            new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
            Vector2.zero, new Vector2(105, 0), new Color(0.08f, 0.68f, 0.98f, 1f));
        fcDefenderHpFill = defHpFill.GetComponent<RectTransform>();

        fcDefenderHpText = CreateUIText(defInner.transform, "HP_Val", "2021", 9, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(195, -38), new Vector2(65, 10), Color.white, TextAnchor.MiddleRight);

        CreateUIText(defInner.transform, "SP_Label", "SP", 8, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(68, -48), new Vector2(16, 10), new Color(1f, 0.75f, 0.15f), TextAnchor.MiddleLeft);

        GameObject defSpBg = CreateUIPanel(defInner.transform, "SP_Bg",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(86, -49), new Vector2(105, 7), new Color(0.04f, 0.07f, 0.12f, 0.95f));
        GameObject defSpFill = CreateUIPanel(defSpBg.transform, "SP_Fill",
            new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
            Vector2.zero, new Vector2(105, 0), new Color(0.96f, 0.68f, 0.08f, 1f));
        fcDefenderSpFill = defSpFill.GetComponent<RectTransform>();

        fcDefenderSpText = CreateUIText(defInner.transform, "SP_Val", "203", 9, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(195, -48), new Vector2(65, 10), Color.white, TextAnchor.MiddleRight);

        // Sub-barra de afinidades Defensor
        GameObject defAffinityBar = CreateUIPanel(defInner.transform, "AffinityBar",
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0, 3), new Vector2(-10, 18), new Color(0.04f, 0.06f, 0.09f, 0.90f));

        fcDefenderResistances = CreateUIText(defAffinityBar.transform, "Resistances", "🔥 -50  💧 +50  ⚡ +50  🛡 +100", 8, FontStyle.Bold,
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0.90f, 0.92f, 0.95f), TextAnchor.MiddleCenter);

        combatForecastRoot.SetActive(false);
    }

    public void ShowCombatForecastBanner(bool show, CombatForecast forecast = default)
    {
        if (combatForecastRoot == null) return;

        if (show && forecast.attacker != null && forecast.defender != null)
        {
            if (playerInfoCard != null) playerInfoCard.SetActive(false);
            combatForecastRoot.SetActive(true);

            // Popula Atacante
            if (fcAttackerName != null) fcAttackerName.text = forecast.attacker.unitName;
            if (fcAttackerStage != null) fcAttackerStage.text = forecast.attacker.rank.ToString();
            if (fcAttackerRole != null) fcAttackerRole.text = $"{forecast.attacker.category} / {forecast.attacker.protocol}";
            if (fcAttackerAvatar != null)
            {
                fcAttackerAvatar.sprite = forecast.attacker.GetPortraitSprite();
                fcAttackerAvatar.color = Color.white;
            }

            int atkHp = forecast.attacker.stats != null ? forecast.attacker.stats.GetStat(StatEnum.HP) : 100;
            int atkMaxHp = forecast.attacker.stats != null ? forecast.attacker.stats.GetStat(StatEnum.MaxHp) : 100;
            if (fcAttackerHpText != null) fcAttackerHpText.text = atkHp.ToString();
            if (fcAttackerHpFill != null)
            {
                float pct = atkMaxHp > 0 ? (float)atkHp / atkMaxHp : 1f;
                fcAttackerHpFill.sizeDelta = new Vector2(105f * Mathf.Clamp01(pct), 0);
            }

            int atkSp = forecast.attacker.stats != null ? forecast.attacker.stats.GetStat(StatEnum.SP) : 50;
            int atkMaxSp = forecast.attacker.stats != null ? forecast.attacker.stats.GetStat(StatEnum.MaxSp) : 50;
            if (fcAttackerSpText != null) fcAttackerSpText.text = atkSp.ToString();
            if (fcAttackerSpFill != null)
            {
                float pct = atkMaxSp > 0 ? (float)atkSp / atkMaxSp : 1f;
                fcAttackerSpFill.sizeDelta = new Vector2(105f * Mathf.Clamp01(pct), 0);
            }

            // Popula Centro
            if (fcAttackerCatIcon != null) fcAttackerCatIcon.color = GetCategoryColor(forecast.attacker.category);
            if (fcDefenderCatIcon != null) fcDefenderCatIcon.color = GetCategoryColor(forecast.defender.category);
            if (fcMultiplierText != null)
            {
                fcMultiplierText.text = $"x{forecast.categoryMultiplier:0.00} ATK";
                if (forecast.hasCategoryAdvantage) fcMultiplierText.color = new Color(0.15f, 0.9f, 1f);
                else if (forecast.hasCategoryDisadvantage) fcMultiplierText.color = new Color(1f, 0.35f, 0.35f);
                else fcMultiplierText.color = Color.white;
            }

            if (fcDamageText != null) fcDamageText.text = forecast.finalDamage.ToString();
            if (fcAccuracyText != null) fcAccuracyText.text = "100%";
            if (fcCounterattackText != null) fcCounterattackText.text = "0%";

            if (fcOrientationText != null)
            {
                switch (forecast.orientation)
                {
                    case AttackOrientation.Backstab:
                        fcOrientationText.text = "Backstab Attack (x1.50)";
                        fcOrientationText.color = new Color(1f, 0.85f, 0.15f);
                        break;
                    case AttackOrientation.Flank:
                        fcOrientationText.text = "Flank Attack (x1.25)";
                        fcOrientationText.color = new Color(1f, 0.65f, 0.15f);
                        break;
                    default:
                        fcOrientationText.text = "Frontal Attack (x1.00)";
                        fcOrientationText.color = Color.white;
                        break;
                }
            }

            if (fcCritChanceText != null)
            {
                fcCritChanceText.text = forecast.isCritical ? "Critical Hit 100%" : (forecast.attacker.category == FunctionalCategory.Game ? "Critical Hit 25%" : "Critical Hit 4%");
            }

            // Popula Defensor
            if (fcDefenderName != null) fcDefenderName.text = forecast.defender.unitName;
            if (fcDefenderStage != null) fcDefenderStage.text = forecast.defender.rank.ToString();
            if (fcDefenderRole != null) fcDefenderRole.text = $"{forecast.defender.category} / {forecast.defender.protocol}";
            if (fcDefenderAvatar != null)
            {
                fcDefenderAvatar.sprite = forecast.defender.GetPortraitSprite();
                fcDefenderAvatar.color = Color.white;
            }

            int defHp = forecast.defenderCurrentHp;
            int defMaxHp = forecast.defender.stats != null ? forecast.defender.stats.GetStat(StatEnum.MaxHp) : 100;
            if (fcDefenderHpText != null) fcDefenderHpText.text = $"{defHp} → <color=#00E5FF>{forecast.defenderRemainingHp}</color>";
            if (fcDefenderHpFill != null)
            {
                float pct = defMaxHp > 0 ? (float)forecast.defenderRemainingHp / defMaxHp : 1f;
                fcDefenderHpFill.sizeDelta = new Vector2(105f * Mathf.Clamp01(pct), 0);
            }

            int defSp = forecast.defender.stats != null ? forecast.defender.stats.GetStat(StatEnum.SP) : 50;
            int defMaxSp = forecast.defender.stats != null ? forecast.defender.stats.GetStat(StatEnum.MaxSp) : 50;
            if (fcDefenderSpText != null) fcDefenderSpText.text = defSp.ToString();
            if (fcDefenderSpFill != null)
            {
                float pct = defMaxSp > 0 ? (float)defSp / defMaxSp : 1f;
                fcDefenderSpFill.sizeDelta = new Vector2(105f * Mathf.Clamp01(pct), 0);
            }

            if (fcDefenderResistances != null)
            {
                string adv = forecast.hasCategoryAdvantage ? $"<color=#00E5FF>Fraqueza a {forecast.attacker.category} (+50%)</color>" : $"{forecast.defender.category} ({forecast.defender.protocol})";
                fcDefenderResistances.text = adv;
            }
        }
        else
        {
            combatForecastRoot.SetActive(false);
            if (playerInfoCard != null) playerInfoCard.SetActive(true);
        }
    }

    #endregion

    #region Public HUD Updates

    public void UpdateTurnBanner(Unit unit)
    {
        if (unit == null) return;
        cachedCurrentUnit = unit;

        try
        {
            UpdateTurnTimeline(unit);

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

            if (avatarImage != null)
            {
                Sprite portrait = unit.GetPortraitSprite();
                if (portrait != null)
                {
                    avatarImage.sprite = portrait;
                    avatarImage.color = unit.spriteRenderer != null ? unit.spriteRenderer.color : Color.white;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[BattleHUD] Aviso ao atualizar banner: {ex.Message}");
        }
    }

    public void UpdateTurnTimeline(Unit activeUnit)
    {
        if (BattleController.Instance == null) return;

        // 1. Contador do Turno
        int round = 1;
        if (BattleController.Instance != null && BattleController.Instance.roundCount > 0)
        {
            round = BattleController.Instance.roundCount;
        }
        if (turnCounterText != null)
        {
            turnCounterText.text = round.ToString();
        }

        // 2. Unidade Ativa no Losango Maior
        if (activeUnit == null) activeUnit = BattleController.Instance.currentUnit;
        if (activeUnitDiamond != null && activeUnitDiamond.rootObj != null)
        {
            if (activeUnit != null)
            {
                activeUnitDiamond.rootObj.SetActive(true);
                Sprite portrait = activeUnit.GetPortraitSprite();
                if (portrait != null)
                {
                    activeUnitDiamond.portraitImage.sprite = portrait;
                    activeUnitDiamond.portraitImage.color = activeUnit.spriteRenderer != null ? activeUnit.spriteRenderer.color : Color.white;
                    activeUnitDiamond.portraitImage.gameObject.SetActive(true);
                    if (activeUnitDiamond.fallbackText != null) activeUnitDiamond.fallbackText.gameObject.SetActive(false);
                }
                else
                {
                    activeUnitDiamond.portraitImage.gameObject.SetActive(false);
                    if (activeUnitDiamond.fallbackText != null)
                    {
                        activeUnitDiamond.fallbackText.text = GetUnitInitials(activeUnit.unitName);
                        activeUnitDiamond.fallbackText.gameObject.SetActive(true);
                    }
                }

                // Destaque de borda por Time
                Color activeBorderColor = activeUnit.team == Team.Player 
                    ? new Color(0f, 0.85f, 1f, 0.95f) 
                    : new Color(1f, 0.35f, 0.35f, 0.95f);
                if (activeUnitDiamond.borderImage != null) activeUnitDiamond.borderImage.color = activeBorderColor;
            }
            else
            {
                activeUnitDiamond.rootObj.SetActive(false);
            }
        }

        // 3. Fila de Próximas Unidades
        List<Unit> displayQueue = new List<Unit>();
        if (BattleController.Instance.turnQueue != null)
        {
            foreach (var u in BattleController.Instance.turnQueue)
            {
                if (u != null && u.gameObject.activeInHierarchy && !displayQueue.Contains(u))
                {
                    displayQueue.Add(u);
                }
            }
        }

        // Se a fila atual tiver menos unidades que os slots disponíveis, adiciona a prévia da próxima rodada
        if (displayQueue.Count < upcomingDiamonds.Count && BattleController.Instance.allUnits != null)
        {
            List<Unit> nextRoundUnits = new List<Unit>(BattleController.Instance.allUnits.FindAll(u => u != null && u.gameObject.activeInHierarchy));
            nextRoundUnits.Sort((a, b) =>
            {
                int speedA = a.stats != null ? a.stats.GetStat(StatEnum.SPEED) : 10;
                int speedB = b.stats != null ? b.stats.GetStat(StatEnum.SPEED) : 10;
                return speedB.CompareTo(speedA);
            });

            foreach (var u in nextRoundUnits)
            {
                if (displayQueue.Count >= upcomingDiamonds.Count) break;
                displayQueue.Add(u);
            }
        }

        // 4. Atualiza os Losangos da Cascata
        for (int i = 0; i < upcomingDiamonds.Count; i++)
        {
            var item = upcomingDiamonds[i];
            if (item == null || item.rootObj == null) continue;

            if (i < displayQueue.Count)
            {
                Unit queueUnit = displayQueue[i];
                item.rootObj.SetActive(true);

                Sprite portrait = queueUnit.GetPortraitSprite();
                if (portrait != null)
                {
                    item.portraitImage.sprite = portrait;
                    item.portraitImage.color = queueUnit.spriteRenderer != null ? queueUnit.spriteRenderer.color : Color.white;
                    item.portraitImage.gameObject.SetActive(true);
                    if (item.fallbackText != null) item.fallbackText.gameObject.SetActive(false);
                }
                else
                {
                    item.portraitImage.gameObject.SetActive(false);
                    if (item.fallbackText != null)
                    {
                        item.fallbackText.text = GetUnitInitials(queueUnit.unitName);
                        item.fallbackText.gameObject.SetActive(true);
                    }
                }

                Color borderColor = queueUnit.team == Team.Player 
                    ? new Color(0.2f, 0.75f, 1f, 0.40f) 
                    : new Color(1f, 0.35f, 0.35f, 0.40f);
                if (item.borderImage != null) item.borderImage.color = borderColor;
            }
            else
            {
                item.rootObj.SetActive(false);
            }
        }
    }

    private string GetUnitInitials(string name)
    {
        if (string.IsNullOrEmpty(name)) return "??";
        if (name.Length <= 2) return name.ToUpper();
        string[] parts = name.Split(' ');
        if (parts.Length > 1 && parts[0].Length > 0 && parts[1].Length > 0)
        {
            return $"{parts[0][0]}{parts[1][0]}".ToUpper();
        }
        return name.Substring(0, 2).ToUpper();
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
                // Destaque DOURADO / AMARELO VIBRANTE com texto preto puro (Estilo Digimon Survive)
                item.backgroundImage.color = new Color(0.98f, 0.84f, 0.0f, 0.98f); // #FDD835 Amarelo Ouro
                item.labelText.color = new Color(0.06f, 0.06f, 0.06f, 1f); // Texto Preto em negrito
                item.labelText.fontStyle = FontStyle.Bold;

                item.iconBackground.color = new Color(0.88f, 0.72f, 0.0f, 0.60f);
                item.iconText.color = new Color(0.06f, 0.06f, 0.06f, 1f);

                if (item.borderImage != null)
                {
                    item.borderImage.color = new Color(1f, 0.92f, 0.35f, 0.90f);
                }
            }
            else
            {
                // Não selecionado: Vidro Escuro Translúcido
                float alpha = isOptionAvailable ? 0.55f : 0.25f;
                item.backgroundImage.color = new Color(0.04f, 0.07f, 0.11f, alpha);
                item.labelText.color = isOptionAvailable ? new Color(0.80f, 0.85f, 0.92f, 0.85f) : new Color(0.40f, 0.45f, 0.50f, 0.45f);
                item.labelText.fontStyle = FontStyle.Bold;

                item.iconBackground.color = new Color(0.10f, 0.15f, 0.22f, alpha);
                item.iconText.color = isOptionAvailable ? new Color(0.82f, 0.88f, 0.94f, 0.90f) : new Color(0.40f, 0.45f, 0.50f, 0.45f);

                if (item.borderImage != null)
                {
                    item.borderImage.color = new Color(0.25f, 0.32f, 0.40f, 0.25f);
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

    private class DiamondQueueItemUI
    {
        public GameObject rootObj;
        public Image borderImage;
        public Image bgImage;
        public Image portraitImage;
        public Text fallbackText;
    }
}
