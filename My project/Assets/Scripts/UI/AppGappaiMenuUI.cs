using System;
using System.Collections.Generic;
using TacticalBattle.Appmon;
using TacticalBattle.Core;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Interface de Alta Fidelidade do Sistema de Fusão em Batalha (App Gappai)
/// reproduzindo a estética cibernética do Nintendo 3DS (Digimon Universe: Appli Monsters).
/// </summary>
public class AppGappaiMenuUI : MonoBehaviour
{
    public static AppGappaiMenuUI Instance;

    private Canvas canvas;
    private GameObject rootContainer;
    private Font uiFont;

    // Estado da seleção
    private Unit mainTurnUnit;
    private AppmonData mainAppmonData;
    private List<Unit> fieldAlliesInRange = new List<Unit>();
    private int selectedIndex = 0;
    private const int GRID_COLS = 4;
    private const int GRID_ROWS = 4;
    private const int TOTAL_SLOTS = GRID_COLS * GRID_ROWS; // Grade 4x4 = 16 slots

    // Ação ao fechar / executar
    public Action<bool> OnClosed;

    // Elementos do Topo (Cockpit de Fusão Dual-Slot)
    private Image mainSlotPortrait;
    private Image mainSlotCatBadgeBg;
    private Text mainSlotCatBadgeText;
    private Text mainSlotNameText;
    private Text mainSlotRankText;
    private Text mainSlotProtocolText;

    private Image connectorBgImage;
    private Text connectorGappaiText;
    private Text connectorSubText;
    private Text connectorPulseArrowsText;

    private GameObject targetEmptyBox;
    private Image targetSlotBorder;
    private Image targetSlotPortrait;
    private Image targetSlotCatBadgeBg;
    private Text targetSlotCatBadgeText;
    private Text targetSlotNameText;
    private Text targetSlotRankText;
    private Text targetSlotProtocolText;

    // Elementos do Centro Esquerdo (Grade de Seleção 4x4)
    private List<GappaiSlotUI> slotUIList = new List<GappaiSlotUI>();
    private Text allyCountText;

    // Elementos do Centro Direito (Projeção Holográfica // Blueprint Deck)
    private GameObject blueprintCompatibleRoot;
    private Image blueprintResultPortrait;
    private Text blueprintResultNameText;
    private Text blueprintResultRankText;
    private Text blueprintResultTypeProtocolText;
    private Text blueprintStatsText;
    private Text blueprintSkillTitleText;
    private Text blueprintSkillDescText;
    private Text blueprintPassiveText;
    private Text blueprintDurationText;

    private GameObject blueprintIncompatibleRoot;
    private Text blueprintIncompatibleTitleText;
    private Text blueprintIncompatibleDescText;
    private Text blueprintIncompatibleGuideTitleText;
    private Text blueprintIncompatibleGuideText;

    private GameObject blueprintEmptyRoot;

    // Elementos do Rodapé (Ações e Comandos)
    private Button confirmButton;
    private Image confirmButtonImage;
    private Text confirmButtonMainText;
    private Text confirmButtonSubText;
    private Button backButton;

    private float pulseTimer = 0f;
    private static Dictionary<string, Sprite> s_spriteCache = new Dictionary<string, Sprite>();

    public bool IsOpen => rootContainer != null && rootContainer.activeSelf;

    public class GappaiSlotUI
    {
        public int index;
        public GameObject root;
        public RectTransform rectTransform;
        public Image backgroundImage;
        public Image borderImage;
        public Image iconImage;
        public Image categoryBadgeBg;
        public Text categoryBadgeText;
        public Text nameText;
        public Text distanceText;
        public GameObject hpBarTrackObj;
        public Image hpBarFillImage;
        public Text hpText;
        public GameObject compatibilityBadgeObj;
        public Image compatibilityBadgeBg;
        public Text compatibilityBadgeText;
        public GameObject cursorHighlightObj;
        public bool isCompatible;
        public bool hasUnit;
        public Unit unit;
    }

    void Awake()
    {
        Instance = this;
        EnsureCanvas();
        BuildUI();
        Hide();
    }

    private Font GetBestFont()
    {
        if (uiFont != null) return uiFont;
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (uiFont == null) uiFont = Font.CreateDynamicFontFromOSFont("Segoe UI", 16);
        if (uiFont == null) uiFont = Font.CreateDynamicFontFromOSFont("Arial", 16);
        return uiFont;
    }

    private void EnsureCanvas()
    {
        if (canvas == null)
        {
            GameObject cObj = new GameObject("AppGappaiCanvas");
            cObj.layer = 5;
            canvas = cObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;

            CanvasScaler scaler = cObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            scaler.dynamicPixelsPerUnit = 2.5f;

            cObj.AddComponent<GraphicRaycaster>();
        }

        var existingEventSystem = FindFirstObjectByType<EventSystem>();
        if (existingEventSystem == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }
#if ENABLE_INPUT_SYSTEM
        else
        {
            var standalone = existingEventSystem.GetComponent<StandaloneInputModule>();
            if (standalone != null) Destroy(standalone);
            if (existingEventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
            {
                existingEventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
        }
#endif
    }

    // =========================================================================
    // CONTROLE DE FLUXO E ABERTURA
    // =========================================================================

    public void Open(Unit currentUnit, List<Unit> alliesInRange, Action<bool> onClosedCallback)
    {
        mainTurnUnit = currentUnit;
        fieldAlliesInRange = alliesInRange != null ? new List<Unit>(alliesInRange) : new List<Unit>();
        OnClosed = onClosedCallback;

        // Identifica AppmonData do monstro principal
        mainAppmonData = null;
        if (mainTurnUnit != null)
        {
            var comp = mainTurnUnit.GetComponent<AppmonCharacter>();
            if (comp != null && comp.appmonData != null) mainAppmonData = comp.appmonData;
            else mainAppmonData = AppmonDatabase.Get(mainTurnUnit.unitName);
        }

        // Se houver aliados no alcance, seleciona preferencialmente o primeiro compatível
        if (fieldAlliesInRange.Count > 0)
        {
            int compatibleIdx = -1;
            for (int i = 0; i < fieldAlliesInRange.Count; i++)
            {
                AppmonData partnerData = GetUnitAppmonData(fieldAlliesInRange[i]);
                if (AppGappaiService.CheckCompatibility(mainAppmonData, partnerData, out _, out _))
                {
                    compatibleIdx = i;
                    break;
                }
            }
            selectedIndex = compatibleIdx >= 0 ? compatibleIdx : 0;
        }
        else
        {
            selectedIndex = -1;
        }

        EnsureCanvas();
        if (rootContainer == null) BuildUI();

        Show();
        RefreshUI();
    }

    public void Show()
    {
        if (canvas != null && canvas.gameObject != null) canvas.gameObject.SetActive(true);
        if (rootContainer != null) rootContainer.SetActive(true);
    }

    public void Hide()
    {
        if (canvas != null && canvas.gameObject != null) canvas.gameObject.SetActive(false);
        if (rootContainer != null) rootContainer.SetActive(false);
    }

    void Update()
    {
        if (!IsOpen) return;

        pulseTimer += Time.deltaTime;
        HandleInput();
        UpdateAnimations();
    }

    public void Navigate(int dirX, int dirY)
    {
        if (fieldAlliesInRange.Count == 0) return;

        if (selectedIndex < 0)
        {
            selectedIndex = 0;
            RefreshUI();
            return;
        }

        int curCol = selectedIndex % GRID_COLS;
        int curRow = selectedIndex / GRID_COLS;

        curCol += dirX;
        curRow += dirY;

        // Clamp ou wrap nas 4 colunas e linhas
        if (curCol < 0) curCol = GRID_COLS - 1;
        else if (curCol >= GRID_COLS) curCol = 0;

        if (curRow < 0) curRow = GRID_ROWS - 1;
        else if (curRow >= GRID_ROWS) curRow = 0;

        int newIdx = curRow * GRID_COLS + curCol;
        if (newIdx < fieldAlliesInRange.Count)
        {
            selectedIndex = newIdx;
            RefreshUI();
        }
        else
        {
            // Se caiu em slot vazio, seleciona o último aliado válido
            selectedIndex = fieldAlliesInRange.Count - 1;
            RefreshUI();
        }
    }

    private void HandleInput()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.escapeKey.wasPressedThisFrame || kb.xKey.wasPressedThisFrame)
        {
            CancelSelection();
            return;
        }

        if (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.zKey.wasPressedThisFrame)
        {
            ConfirmSelection();
            return;
        }

        if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) Navigate(0, -1);
        else if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) Navigate(0, 1);
        else if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) Navigate(-1, 0);
        else if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) Navigate(1, 0);
#else
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.X))
        {
            CancelSelection();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Z))
        {
            ConfirmSelection();
            return;
        }

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) Navigate(0, -1);
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) Navigate(0, 1);
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) Navigate(-1, 0);
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) Navigate(1, 0);
#endif
    }

    private void UpdateAnimations()
    {
        // Pulso suave do Conector de Fusão (Ciano / Azul Elétrico)
        if (connectorBgImage != null)
        {
            float pulse = 0.5f + Mathf.Sin(pulseTimer * 4.5f) * 0.5f;
            Color c1 = new Color(0.0f, 0.85f, 1.0f, 1.0f);
            Color c2 = new Color(0.1f, 0.45f, 1.0f, 1.0f);
            connectorBgImage.color = Color.Lerp(c1, c2, pulse);
        }

        // Animação de setas pulsantes no conector
        if (connectorPulseArrowsText != null)
        {
            int phase = Mathf.FloorToInt((pulseTimer * 4f) % 4);
            connectorPulseArrowsText.text = phase switch
            {
                0 => "▶   ▶   ▶",
                1 => " ▶  ▶  ▶ ",
                2 => "  ▶  ▶  ▶",
                _ => "▶   ▶   ▶"
            };
        }

        // Borda do slot alvo pulsa suavemente quando vazio
        if (targetSlotBorder != null && targetEmptyBox != null && targetEmptyBox.activeSelf)
        {
            float glow = 0.55f + Mathf.Sin(pulseTimer * 4f) * 0.45f;
            targetSlotBorder.color = new Color(0f, 0.85f, 1f, glow);
        }

        // Cursor de seleção na grade pulsa em dourado
        if (selectedIndex >= 0 && selectedIndex < slotUIList.Count)
        {
            var slot = slotUIList[selectedIndex];
            if (slot != null && slot.cursorHighlightObj != null && slot.cursorHighlightObj.activeSelf)
            {
                Image cursorImg = slot.cursorHighlightObj.GetComponent<Image>();
                if (cursorImg != null)
                {
                    float glow = 0.70f + Mathf.Sin(pulseTimer * 6.5f) * 0.30f;
                    cursorImg.color = new Color(1.0f, 0.85f, 0.20f, glow);
                }
            }
        }
    }

    public void OnSlotClicked(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= fieldAlliesInRange.Count) return;

        if (selectedIndex == slotIndex)
        {
            // Duplo clique confirma
            ConfirmSelection();
        }
        else
        {
            selectedIndex = slotIndex;
            RefreshUI();
        }
    }

    public void ConfirmSelection()
    {
        if (selectedIndex < 0 || selectedIndex >= fieldAlliesInRange.Count)
        {
            Debug.LogWarning("[AppGappaiMenuUI] Nenhum parceiro selecionado!");
            return;
        }

        Unit partnerUnit = fieldAlliesInRange[selectedIndex];
        AppmonData partnerData = GetUnitAppmonData(partnerUnit);

        bool isCompatible = AppGappaiService.CheckCompatibility(mainAppmonData, partnerData, out AppmonData fusionResult, out string reason);

        if (!isCompatible || fusionResult == null)
        {
            // Regra Estrita: Se a função não puder acontecer, NADA pode acontecer!
            Debug.LogWarning($"[AppGappaiMenuUI] Fusão bloqueada: {mainTurnUnit.unitName} + {partnerUnit.unitName} é incompatível ({reason}). Nenhuma ação executada.");
            return;
        }

        Debug.Log($"[AppGappaiMenuUI] Confirmando FUSÃO: {mainTurnUnit.unitName} + {partnerUnit.unitName} ➔ {fusionResult.name}");
        AppGappaiService.ExecuteFusion(mainTurnUnit, partnerUnit, fusionResult);
        CloseScreen(didPerformAction: true);
    }

    public void CancelSelection()
    {
        CloseScreen(didPerformAction: false);
    }

    public void CloseScreen(bool didPerformAction)
    {
        Hide();
        OnClosed?.Invoke(didPerformAction);
    }

    // =========================================================================
    // ATUALIZAÇÃO DOS ELEMENTOS DA UI
    // =========================================================================

    public void RefreshUI()
    {
        // 1. Atualiza Slot Esquerdo (MAIN)
        if (mainAppmonData != null)
        {
            mainSlotNameText.text = mainAppmonData.name;
            mainSlotRankText.text = $"[ {mainAppmonData.rank.ToString().ToUpper()} ]";
            mainSlotProtocolText.text = $"• PROTOCOLO: {mainAppmonData.protocol} •";
            mainSlotPortrait.sprite = GetAppmonSprite(mainAppmonData, mainTurnUnit);

            Color catColor = GetCategoryColor(mainAppmonData.primaryCategory);
            mainSlotCatBadgeBg.color = catColor;
            mainSlotCatBadgeText.text = GetCategorySymbol(mainAppmonData.primaryCategory);
        }
        else if (mainTurnUnit != null)
        {
            mainSlotNameText.text = mainTurnUnit.unitName;
            mainSlotRankText.text = $"[ {mainTurnUnit.rank.ToString().ToUpper()} ]";
            mainSlotProtocolText.text = "• PROTOCOLO: DIGITAL •";
            mainSlotPortrait.sprite = mainTurnUnit.GetPortraitSprite();

            Color catColor = GetCategoryColor(mainTurnUnit.category);
            mainSlotCatBadgeBg.color = catColor;
            mainSlotCatBadgeText.text = GetCategorySymbol(mainTurnUnit.category);
        }

        // 2. Identifica Aliado Selecionado
        Unit selectedPartner = (selectedIndex >= 0 && selectedIndex < fieldAlliesInRange.Count)
            ? fieldAlliesInRange[selectedIndex]
            : null;

        if (allyCountText != null)
        {
            allyCountText.text = $"[ {fieldAlliesInRange.Count} DETECTADOS NO RAIO 4x4 ]";
        }

        if (selectedPartner != null)
        {
            AppmonData partnerData = GetUnitAppmonData(selectedPartner);
            targetEmptyBox.SetActive(false);
            targetSlotPortrait.gameObject.SetActive(true);
            targetSlotCatBadgeBg.gameObject.SetActive(true);

            targetSlotPortrait.sprite = GetAppmonSprite(partnerData, selectedPartner);

            FunctionalCategory partnerCat = partnerData != null ? partnerData.primaryCategory : selectedPartner.category;
            Color partnerCatCol = GetCategoryColor(partnerCat);
            targetSlotCatBadgeBg.color = partnerCatCol;
            targetSlotCatBadgeText.text = GetCategorySymbol(partnerCat);

            targetSlotNameText.text = selectedPartner.unitName;
            string rankStr = (partnerData != null ? partnerData.rank.ToString() : selectedPartner.rank.ToString()).ToUpper();
            targetSlotRankText.text = $"[ {rankStr} ]";

            string protocolStr = partnerData != null ? partnerData.protocol.ToString() : "DIGITAL";
            targetSlotProtocolText.text = $"• PROTOCOLO: {protocolStr} •";

            // Validação oficial de compatibilidade
            bool isCompatible = AppGappaiService.CheckCompatibility(mainAppmonData, partnerData, out AppmonData fusionResult, out string reason);

            if (isCompatible && fusionResult != null)
            {
                // --- COMPATÍVEL ---
                targetSlotBorder.color = new Color(0.0f, 1.0f, 0.55f, 1.0f); // Borda Verde Esmeralda Neon

                // Exibe Blueprint Holográfico de Sucesso
                blueprintCompatibleRoot.SetActive(true);
                blueprintIncompatibleRoot.SetActive(false);
                blueprintEmptyRoot.SetActive(false);

                blueprintResultPortrait.sprite = GetAppmonSprite(fusionResult);
                blueprintResultNameText.text = fusionResult.name;
                blueprintResultRankText.text = $"★ {fusionResult.rank.ToString().ToUpper()} APPMON ★";

                Color resCatCol = GetCategoryColor(fusionResult.primaryCategory);
                string catHex = ColorUtility.ToHtmlStringRGB(resCatCol);
                blueprintResultTypeProtocolText.text = $"<color=#{catHex}>[{fusionResult.primaryCategory.ToString().ToUpper()}]</color>   •   <color=#80D8FF>[PROTOCOLO: {fusionResult.protocol}]</color>";

                // Stats Grid
                blueprintStatsText.text =
                    $"<b>HP:</b>  <color=#00FFAA>{fusionResult.hp}</color>    <b>MP:</b>  <color=#80D8FF>{fusionResult.mp}</color>\n" +
                    $"<b>ATK:</b> <color=#FFB74D>{fusionResult.atk}</color>    <b>DEF:</b> <color=#81C784>{fusionResult.def}</color>\n" +
                    $"<b>SPD:</b> <color=#BA68C8>{fusionResult.spd}</color>    <b>MOV:</b> <color=#FFF176>{fusionResult.mov} Tiles</color>";

                // Habilidade e Passiva
                if (fusionResult.skills != null && fusionResult.skills.Count > 0)
                {
                    var sigSkill = fusionResult.skills[0];
                    blueprintSkillTitleText.text = $"⚡ Habilidade Principal: <b>{sigSkill.skillName}</b>";
                    blueprintSkillDescText.text = string.IsNullOrEmpty(sigSkill.description)
                        ? "Habilidade de ataque avançada gerada pela fusão."
                        : sigSkill.description;
                }
                else
                {
                    blueprintSkillTitleText.text = "⚡ Habilidades:";
                    blueprintSkillDescText.text = "Herda todas as técnicas das criaturas progenitoras.";
                }

                if (!string.IsNullOrEmpty(fusionResult.passiveName))
                {
                    blueprintPassiveText.text = $"🛡 Passiva: <color=#FFE082><b>{fusionResult.passiveName}</b></color> — {fusionResult.passiveDescription}";
                }
                else
                {
                    blueprintPassiveText.text = "🛡 Passiva: Algoritmo de fusão temporário estabilizado.";
                }

                blueprintDurationText.text = "⏱ <b>Duração:</b> Válido por Esta Batalha <i>(IsTemporaryFusion = true)</i>";

                // Botão Confirmar Verde Ativado
                confirmButton.interactable = true;
                confirmButtonImage.sprite = GetGlossyPillSprite(340, 60, new Color(0.10f, 0.88f, 0.35f), new Color(0.02f, 0.58f, 0.20f), new Color(1f, 1f, 1f, 0.9f));
                confirmButtonMainText.text = "★ EXECUTAR FUSÃO GAPPAI [ENTER] ★";
                confirmButtonSubText.text = "CONFIRMAR FUSÃO DE APPMON";
            }
            else
            {
                // --- INCOMPATÍVEL ---
                targetSlotBorder.color = new Color(0.85f, 0.25f, 0.25f, 0.9f); // Borda Vermelha

                blueprintCompatibleRoot.SetActive(false);
                blueprintIncompatibleRoot.SetActive(true);
                blueprintEmptyRoot.SetActive(false);

                blueprintIncompatibleTitleText.text = "🔒 FUSÃO BLOQUEADA // INCOMPATÍVEL";
                blueprintIncompatibleDescText.text =
                    $"Os monstros <b>{mainAppmonData?.name}</b> e <b>{partnerData?.name}</b> não possuem fórmula de fusão cadastrada no Compêndio de Personagens.\n" +
                    $"<color=#FF8A80>Segundo as regras, se a fusão não puder acontecer, nenhuma ação será executada.</color>";

                blueprintIncompatibleGuideTitleText.text = $"📖 Fórmulas Oficiais Cadastradas para {mainAppmonData?.name}:";
                blueprintIncompatibleGuideText.text = GetCompendiumRecipesFor(mainAppmonData);

                // Botão Confirmar Bloqueado
                confirmButton.interactable = false;
                confirmButtonImage.sprite = GetGlossyPillSprite(340, 60, new Color(0.20f, 0.22f, 0.26f), new Color(0.12f, 0.14f, 0.17f), new Color(0.50f, 0.30f, 0.30f));
                confirmButtonMainText.text = "✖ FUSÃO INDISPONÍVEL [BLOQUEADO]";
                confirmButtonSubText.text = "COMBINAÇÃO INCOMPATÍVEL";
            }
        }
        else
        {
            // --- NENHUM ALVO SELECIONADO ---
            targetEmptyBox.SetActive(true);
            targetSlotPortrait.gameObject.SetActive(false);
            targetSlotCatBadgeBg.gameObject.SetActive(false);
            targetSlotNameText.text = "---";
            targetSlotRankText.text = "[ ALVO 4x4 ]";
            targetSlotProtocolText.text = "• SELEÇÃO PENDENTE •";
            targetSlotBorder.color = new Color(0f, 0.85f, 1f, 0.7f);

            blueprintCompatibleRoot.SetActive(false);
            blueprintIncompatibleRoot.SetActive(false);
            blueprintEmptyRoot.SetActive(true);

            confirmButton.interactable = false;
            confirmButtonImage.sprite = GetGlossyPillSprite(340, 60, new Color(0.20f, 0.22f, 0.26f), new Color(0.12f, 0.14f, 0.17f), new Color(0.4f, 0.4f, 0.4f));
            confirmButtonMainText.text = "SELECIONE UM ALVO NA GRADE";
            confirmButtonSubText.text = "ESCOLHA UM ALVO NO TABULEIRO";
        }

        // 3. Atualiza os 16 Slots da Grade 4x4
        Vector3Int originPos = mainTurnUnit != null ? mainTurnUnit.gridPosition : Vector3Int.zero;

        for (int i = 0; i < slotUIList.Count; i++)
        {
            var slot = slotUIList[i];
            if (slot == null || slot.root == null) continue;

            if (i < fieldAlliesInRange.Count)
            {
                Unit ally = fieldAlliesInRange[i];
                AppmonData allyData = GetUnitAppmonData(ally);
                slot.hasUnit = true;
                slot.unit = ally;

                slot.root.SetActive(true);
                slot.iconImage.gameObject.SetActive(true);
                slot.iconImage.sprite = GetAppmonSprite(allyData, ally);

                FunctionalCategory cat = allyData != null ? allyData.primaryCategory : ally.category;
                Color catColor = GetCategoryColor(cat);
                slot.categoryBadgeBg.color = catColor;
                slot.categoryBadgeText.text = GetCategorySymbol(cat);

                slot.nameText.text = ally.unitName;

                // Distância relativa
                int dist = Mathf.Abs(ally.gridPosition.x - originPos.x) + Mathf.Abs(ally.gridPosition.y - originPos.y);
                slot.distanceText.text = $"⌖ {dist} Tiles";

                // HP Bar e Valores
                int hp = ally.stats != null ? ally.stats.GetStat(StatEnum.HP) : 100;
                int maxHp = ally.stats != null ? ally.stats.GetStat(StatEnum.MaxHp) : 100;
                float hpRatio = Mathf.Clamp01(maxHp > 0 ? (float)hp / maxHp : 1f);
                slot.hpText.text = $"HP {hp}/{maxHp}";

                if (slot.hpBarFillImage != null)
                {
                    slot.hpBarFillImage.fillAmount = hpRatio;
                    slot.hpBarFillImage.color = hpRatio > 0.5f
                        ? new Color(0.1f, 0.85f, 0.35f)
                        : (hpRatio > 0.2f ? new Color(1.0f, 0.75f, 0.1f) : new Color(0.95f, 0.2f, 0.2f));
                }

                // Validação de compatibilidade para o filtro visual do chip
                bool compatible = AppGappaiService.CheckCompatibility(mainAppmonData, allyData, out AppmonData res, out _);
                slot.isCompatible = compatible;

                if (compatible)
                {
                    // Compatível: Borda verde brilhante com selo Gappai OK
                    slot.borderImage.sprite = GetRoundedRectSprite(168, 102, 12f, new Color(0.02f, 0.18f, 0.12f, 0.95f), new Color(0.0f, 1.0f, 0.55f, 1.0f), 2.5f);
                    slot.compatibilityBadgeObj.SetActive(true);
                    slot.compatibilityBadgeBg.color = new Color(0.0f, 0.70f, 0.35f, 0.95f);
                    slot.compatibilityBadgeText.text = $"★ Gappai OK ➔ {res?.name}";
                    slot.compatibilityBadgeText.color = Color.white;
                }
                else
                {
                    // Incompatível: Borda escura com selo Incompatível
                    slot.borderImage.sprite = GetRoundedRectSprite(168, 102, 12f, new Color(0.06f, 0.08f, 0.12f, 0.88f), new Color(0.45f, 0.30f, 0.30f, 0.60f), 1.5f);
                    slot.compatibilityBadgeObj.SetActive(true);
                    slot.compatibilityBadgeBg.color = new Color(0.35f, 0.15f, 0.15f, 0.85f);
                    slot.compatibilityBadgeText.text = "✖ Incompatível";
                    slot.compatibilityBadgeText.color = new Color(0.95f, 0.70f, 0.70f);
                }

                // Cursor de seleção ativo
                bool isSelected = (i == selectedIndex);
                slot.cursorHighlightObj.SetActive(isSelected);
            }
            else
            {
                // Slot vazio da matriz 4x4
                slot.hasUnit = false;
                slot.unit = null;
                slot.root.SetActive(true);
                slot.iconImage.gameObject.SetActive(false);
                slot.nameText.text = "---";
                slot.distanceText.text = "";
                slot.hpText.text = "LIVRE";
                if (slot.hpBarFillImage != null) slot.hpBarFillImage.fillAmount = 0f;
                slot.categoryBadgeText.text = "•";
                slot.categoryBadgeBg.color = new Color(0.2f, 0.25f, 0.3f, 0.5f);
                slot.compatibilityBadgeObj.SetActive(false);
                slot.cursorHighlightObj.SetActive(false);
                slot.borderImage.sprite = GetRoundedRectSprite(168, 102, 12f, new Color(0.03f, 0.05f, 0.08f, 0.50f), new Color(0.18f, 0.25f, 0.35f, 0.30f), 1.0f);
            }
        }
    }

    private string GetCompendiumRecipesFor(AppmonData appmon)
    {
        if (appmon == null) return "Nenhuma criatura ativa.";
        var recipes = AppGappaiDatabase.GetAllRecipes();
        var matches = new List<string>();

        foreach (var r in recipes)
        {
            if (r.parentA.Equals(appmon.name, StringComparison.OrdinalIgnoreCase))
            {
                matches.Add($"• {appmon.name} + <color=#00FFAA><b>{r.parentB}</b></color> ➔ <color=#FFD54F><b>{r.resultId}</b></color> ({r.resultRank})");
            }
            else if (r.parentB.Equals(appmon.name, StringComparison.OrdinalIgnoreCase))
            {
                matches.Add($"• {appmon.name} + <color=#00FFAA><b>{r.parentA}</b></color> ➔ <color=#FFD54F><b>{r.resultId}</b></color> ({r.resultRank})");
            }
        }

        if (matches.Count == 0)
        {
            return "Nenhuma fórmula de fusão registrada no compêndio para este Appmon.";
        }

        return string.Join("\n", matches);
    }

    private AppmonData GetUnitAppmonData(Unit unit)
    {
        if (unit == null) return null;
        var comp = unit.GetComponent<AppmonCharacter>();
        if (comp != null && comp.appmonData != null) return comp.appmonData;
        return AppmonDatabase.Get(unit.unitName);
    }

    private Sprite GetAppmonSprite(AppmonData data, Unit unit = null)
    {
        if (unit != null)
        {
            Sprite s = unit.GetPortraitSprite();
            if (s != null) return s;
        }

        if (data != null)
        {
            string cleanId = data.id.Replace("-", "").Replace(" ", "").ToLower();
            Sprite loaded = Resources.Load<Sprite>($"Appmon/{cleanId}");
            if (loaded != null) return loaded;
        }

        return ProceduralGridTileFactory.GappaiTile;
    }

    private Color GetCategoryColor(FunctionalCategory cat)
    {
        return cat switch
        {
            FunctionalCategory.Security => new Color(0.0f, 0.85f, 1.0f, 1.0f),       // Ciano Neon
            FunctionalCategory.System => new Color(0.98f, 0.25f, 0.25f, 1.0f),      // Vermelho Crimson
            FunctionalCategory.Tool => new Color(1.0f, 0.70f, 0.15f, 1.0f),        // Âmbar Dourado
            FunctionalCategory.Entertainment => new Color(0.85f, 0.35f, 1.0f, 1.0f),// Violeta Neon
            FunctionalCategory.Life => new Color(0.20f, 0.95f, 0.45f, 1.0f),       // Verde Esmeralda
            FunctionalCategory.Social => new Color(1.0f, 0.85f, 0.25f, 1.0f),     // Amarelo Sol
            FunctionalCategory.Game => new Color(0.35f, 0.55f, 1.0f, 1.0f),        // Azul Cobalto
            _ => new Color(0.40f, 0.50f, 0.65f, 1.0f)
        };
    }

    private string GetCategorySymbol(FunctionalCategory cat)
    {
        return cat switch
        {
            FunctionalCategory.Social => "💬",
            FunctionalCategory.Navi => "🧭",
            FunctionalCategory.Tool => "🛠",
            FunctionalCategory.Game => "🎮",
            FunctionalCategory.Entertainment => "🎵",
            FunctionalCategory.Life => "🌱",
            FunctionalCategory.System => "⚙",
            _ => "★"
        };
    }

    // =========================================================================
    // CONSTRUÇÃO PROCEDURAL DA INTERFACE
    // =========================================================================

    private void BuildUI()
    {
        // 1. Contêiner Raiz com backdrop escurecido translúcido
        rootContainer = CreateUIPanel(canvas.transform, "AppGappaiRoot",
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0.01f, 0.02f, 0.05f, 0.92f));

        // 2. Chassi do Painel de Fusão em Batalha (1300 x 910 px)
        GameObject chassis = CreateUIPanel(rootContainer.transform, "GappaiChassis",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(1300f, 910f), new Color(0.04f, 0.07f, 0.12f, 1.0f));
        chassis.GetComponent<Image>().sprite = GetRoundedRectSprite(1300, 910, 20f, new Color(0.04f, 0.07f, 0.12f, 1f), new Color(0.0f, 0.85f, 1.0f, 0.9f), 2.5f);

        // Molduras cibernéticas nos cantos do chassi
        CreateCornerAccents(chassis.transform, 1300, 910);

        // Barra Superior de Título do Console
        BuildTitleHeader(chassis.transform);

        // 3. Topo (Cockpit de Fusão Dual-Slot)
        BuildTopCombinationDeck(chassis.transform);

        // 4. Centro Esquerdo (Grade de Seleção 4x4)
        BuildMatrixSelectionGrid(chassis.transform);

        // 5. Centro Direito (Projeção Holográfica // Blueprint Deck)
        BuildHolographicBlueprintDeck(chassis.transform);

        // 6. Rodapé (Botões de Ação e Legenda)
        BuildFooterControls(chassis.transform);
    }

    private void CreateCornerAccents(Transform parent, float width, float height)
    {
        float hw = width * 0.5f - 8f;
        float hh = height * 0.5f - 8f;

        Vector2[] corners = {
            new Vector2(-hw, hh), new Vector2(hw, hh),
            new Vector2(-hw, -hh), new Vector2(hw, -hh)
        };

        foreach (var pos in corners)
        {
            GameObject corner = CreateUIPanel(parent, "Corner",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                pos, new Vector2(24f, 24f), Color.white);
            corner.GetComponent<Image>().sprite = GetCyberCornerBracketSprite(24, 24);
        }
    }

    private void BuildTitleHeader(Transform parent)
    {
        GameObject titleBar = CreateUIPanel(parent, "TitleHeader",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -8f), new Vector2(1270f, 32f), new Color(0.02f, 0.12f, 0.22f, 0.95f));
        titleBar.GetComponent<Image>().sprite = GetRoundedRectSprite(1270, 32, 6f, new Color(0.02f, 0.12f, 0.22f, 0.95f), new Color(0.0f, 0.70f, 1f, 0.6f), 1f);

        CreateUIText(titleBar.transform, "SystemTag", "APPLI DRIVE SYSTEM // GAPPAI LINK", 11, FontStyle.Bold,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(14f, 0f), new Vector2(300f, 20f), new Color(0.0f, 0.90f, 1f), TextAnchor.MiddleLeft);

        CreateUIText(titleBar.transform, "MainTitle", "★ SISTEMA DE FUSÃO TEMPORÁRIA (APP GAPPAI) ★", 13, FontStyle.Bold,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(480f, 22f), Color.white, TextAnchor.MiddleCenter);

        CreateUIText(titleBar.transform, "RadarStatus", "RAIO: 4x4 TILES  •  ● RADAR ATIVO", 11, FontStyle.Bold,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-14f, 0f), new Vector2(300f, 20f), new Color(1f, 0.85f, 0.2f), TextAnchor.MiddleRight);
    }

    private void BuildTopCombinationDeck(Transform parent)
    {
        // Cockpit Superior (1270 x 155 px)
        GameObject topBar = CreateUIPanel(parent, "TopDeck",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -44f), new Vector2(1270f, 155f), new Color(0.02f, 0.16f, 0.32f, 0.95f));
        topBar.GetComponent<Image>().sprite = GetRoundedRectSprite(1270, 155, 14f, new Color(0.02f, 0.16f, 0.32f, 0.95f), new Color(0.0f, 0.90f, 1f, 0.8f), 2f);

        // --- SLOT ESQUERDO: APPMON ATIVO DO TURNO ("MAIN") ---
        GameObject leftSlot = CreateUIPanel(topBar.transform, "MainSlot",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-280f, 0f), new Vector2(380f, 135f), new Color(0.02f, 0.09f, 0.18f, 0.95f));
        leftSlot.GetComponent<Image>().sprite = GetRoundedRectSprite(380, 135, 12f, new Color(0.02f, 0.09f, 0.18f, 0.95f), new Color(0.0f, 0.85f, 1f, 0.9f), 2f);

        // Carimbo "PRINCIPAL"
        GameObject mainStamp = CreateUIPanel(leftSlot.transform, "MainStamp",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(10f, -8f), new Vector2(100f, 22f), new Color(0.90f, 0.18f, 0.18f, 0.95f));
        mainStamp.GetComponent<Image>().sprite = GetRoundedRectSprite(100, 22, 6f, new Color(0.90f, 0.18f, 0.18f, 0.95f), Color.white, 1.5f);
        CreateUIText(mainStamp.transform, "Txt", "PRINCIPAL", 11, FontStyle.Bold,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, Color.white, TextAnchor.MiddleCenter);

        // Retrato Esquerdo
        GameObject mainPortObj = CreateUIPanel(leftSlot.transform, "Portrait",
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(14f, -6f), new Vector2(96f, 96f), Color.white);
        mainSlotPortrait = mainPortObj.GetComponent<Image>();

        // Badge de Categoria
        GameObject mainCatObj = CreateUIPanel(leftSlot.transform, "CatBadge",
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(80f, 12f), new Vector2(28f, 28f), Color.white);
        mainSlotCatBadgeBg = mainCatObj.GetComponent<Image>();
        mainSlotCatBadgeBg.sprite = GetCircleSprite(28, new Color(0.0f, 0.70f, 1f), Color.white, 2f);
        mainSlotCatBadgeText = CreateUIText(mainCatObj.transform, "Txt", "★", 13, FontStyle.Normal,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, Color.white, TextAnchor.MiddleCenter);

        // Textos do Slot Esquerdo
        mainSlotNameText = CreateUIText(leftSlot.transform, "Name", "Main Appmon", 16, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
            new Vector2(124f, -14f), new Vector2(-130f, 26f), Color.white, TextAnchor.MiddleLeft);

        mainSlotRankText = CreateUIText(leftSlot.transform, "Rank", "[ STANDARD ]", 12, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
            new Vector2(124f, -42f), new Vector2(-130f, 20f), new Color(0.0f, 0.90f, 1f), TextAnchor.MiddleLeft);

        mainSlotProtocolText = CreateUIText(leftSlot.transform, "Protocol", "• PROTOCOLO: FIREWALL •", 10, FontStyle.Normal,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
            new Vector2(124f, -66f), new Vector2(-130f, 18f), new Color(0.8f, 0.9f, 1f, 0.8f), TextAnchor.MiddleLeft);

        // --- CONECTOR CENTRAL DE FUSÃO (DIGITAL MATRIX CONDUIT) ---
        GameObject conduitRoot = CreateUIPanel(topBar.transform, "ConduitRoot",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 0f), new Vector2(230f, 130f), Color.clear);

        connectorBgImage = CreateUIPanel(conduitRoot.transform, "PillBadge",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 10f), new Vector2(190f, 52f), new Color(0.0f, 0.85f, 1f, 1f)).GetComponent<Image>();
        connectorBgImage.sprite = GetRoundedRectSprite(190, 52, 26f, new Color(0.0f, 0.85f, 1f, 1f), Color.white, 2.5f);

        connectorGappaiText = CreateUIText(connectorBgImage.transform, "GappaiTxt", "FUSÃO GAPPAI", 16, FontStyle.Bold,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -1f), new Vector2(180f, 26f), new Color(0.02f, 0.12f, 0.28f), TextAnchor.MiddleCenter);

        connectorPulseArrowsText = CreateUIText(conduitRoot.transform, "Arrows", "▶   ▶   ▶", 13, FontStyle.Bold,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -28f), new Vector2(180f, 20f), new Color(0.0f, 1.0f, 0.85f), TextAnchor.MiddleCenter);

        connectorSubText = CreateUIText(conduitRoot.transform, "SubTxt", "CONECTOR SINÉRGICO", 9, FontStyle.Bold,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 44f), new Vector2(200f, 16f), new Color(0.7f, 0.9f, 1f), TextAnchor.MiddleCenter);

        // --- SLOT DIREITO: MONSTRO ALVO / PARCEIRO SELECIONADO ---
        GameObject rightSlot = CreateUIPanel(topBar.transform, "TargetSlot",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(280f, 0f), new Vector2(380f, 135f), new Color(0.02f, 0.09f, 0.18f, 0.95f));
        targetSlotBorder = rightSlot.GetComponent<Image>();
        targetSlotBorder.sprite = GetRoundedRectSprite(380, 135, 12f, new Color(0.02f, 0.09f, 0.18f, 0.95f), new Color(0.0f, 0.85f, 1f, 0.9f), 2f);

        // Carimbo "PARCEIRO"
        GameObject partnerStamp = CreateUIPanel(rightSlot.transform, "PartnerStamp",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(10f, -8f), new Vector2(120f, 22f), new Color(0.12f, 0.55f, 0.95f, 0.95f));
        partnerStamp.GetComponent<Image>().sprite = GetRoundedRectSprite(120, 22, 6f, new Color(0.12f, 0.55f, 0.95f, 0.95f), Color.white, 1.5f);
        CreateUIText(partnerStamp.transform, "Txt", "PARCEIRO", 11, FontStyle.Bold,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, Color.white, TextAnchor.MiddleCenter);

        // Caixa de Alvo Vazio (Wireframe)
        targetEmptyBox = CreateUIPanel(rightSlot.transform, "EmptyBox",
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(14f, -6f), new Vector2(96f, 96f), new Color(0.03f, 0.12f, 0.22f, 0.7f));
        targetEmptyBox.GetComponent<Image>().sprite = GetRoundedRectSprite(96, 96, 12f, new Color(0.03f, 0.12f, 0.22f, 0.7f), new Color(0.0f, 0.60f, 0.9f, 0.5f), 1.5f);
        CreateUIText(targetEmptyBox.transform, "Prompt", "[ ? ]\nSELECIONE\nNA GRADE", 10, FontStyle.Bold,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0.0f, 0.85f, 1f), TextAnchor.MiddleCenter);

        // Retrato Direito (Quando selecionado)
        GameObject targetPortObj = CreateUIPanel(rightSlot.transform, "Portrait",
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(14f, -6f), new Vector2(96f, 96f), Color.white);
        targetSlotPortrait = targetPortObj.GetComponent<Image>();
        targetSlotPortrait.gameObject.SetActive(false);

        // Badge de Categoria Direita
        GameObject targetCatObj = CreateUIPanel(rightSlot.transform, "CatBadge",
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(80f, 12f), new Vector2(28f, 28f), Color.white);
        targetSlotCatBadgeBg = targetCatObj.GetComponent<Image>();
        targetSlotCatBadgeBg.sprite = GetCircleSprite(28, new Color(0.0f, 0.70f, 1f), Color.white, 2f);
        targetSlotCatBadgeText = CreateUIText(targetCatObj.transform, "Txt", "★", 13, FontStyle.Normal,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, Color.white, TextAnchor.MiddleCenter);
        targetSlotCatBadgeBg.gameObject.SetActive(false);

        // Textos do Slot Direito
        targetSlotNameText = CreateUIText(rightSlot.transform, "Name", "---", 16, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
            new Vector2(124f, -14f), new Vector2(-130f, 26f), Color.white, TextAnchor.MiddleLeft);

        targetSlotRankText = CreateUIText(rightSlot.transform, "Rank", "[ ALVO 4x4 ]", 12, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
            new Vector2(124f, -42f), new Vector2(-130f, 20f), new Color(0.0f, 0.90f, 1f), TextAnchor.MiddleLeft);

        targetSlotProtocolText = CreateUIText(rightSlot.transform, "Protocol", "• SELEÇÃO PENDENTE •", 10, FontStyle.Normal,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
            new Vector2(124f, -66f), new Vector2(-130f, 18f), new Color(0.8f, 0.9f, 1f, 0.8f), TextAnchor.MiddleLeft);
    }

    private void BuildMatrixSelectionGrid(Transform parent)
    {
        // Painel Esquerdo: Matriz 4x4 (725 x 520 px)
        GameObject gridPanel = CreateUIPanel(parent, "GridPanel",
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(15f, -30f), new Vector2(725f, 520f), new Color(0.03f, 0.06f, 0.11f, 0.95f));
        gridPanel.GetComponent<Image>().sprite = GetRoundedRectSprite(725, 520, 16f, new Color(0.03f, 0.06f, 0.11f, 0.95f), new Color(0.0f, 0.65f, 0.95f, 0.7f), 2f);

        // Cabeçalho da Grade
        GameObject gridHeader = CreateUIPanel(gridPanel.transform, "GridHeader",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -8f), new Vector2(705f, 32f), new Color(0.02f, 0.14f, 0.25f, 0.95f));
        gridHeader.GetComponent<Image>().sprite = GetRoundedRectSprite(705, 32, 6f, new Color(0.02f, 0.14f, 0.25f, 0.95f), new Color(0.0f, 0.80f, 1f, 0.5f), 1f);

        CreateUIText(gridHeader.transform, "Title", "MATRIZ TÁTICA // COMBATENTES NO ALCANCE 4x4", 12, FontStyle.Bold,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(12f, 0f), new Vector2(380f, 20f), Color.white, TextAnchor.MiddleLeft);

        allyCountText = CreateUIText(gridHeader.transform, "Count", "[ 0 DETECTADOS ]", 11, FontStyle.Bold,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-12f, 0f), new Vector2(250f, 20f), new Color(0.0f, 1.0f, 0.85f), TextAnchor.MiddleRight);

        // Grade 4x4 de Chips (168px x 102px cada)
        slotUIList.Clear();

        float chipW = 168f;
        float chipH = 102f;
        float gapX = 10f;
        float gapY = 8f;

        float totalW = GRID_COLS * chipW + (GRID_COLS - 1) * gapX;
        float totalH = GRID_ROWS * chipH + (GRID_ROWS - 1) * gapY;
        float startX = -totalW * 0.5f + chipW * 0.5f;
        float startY = totalH * 0.5f - chipH * 0.5f - 18f;

        for (int row = 0; row < GRID_ROWS; row++)
        {
            for (int col = 0; col < GRID_COLS; col++)
            {
                int index = row * GRID_COLS + col;
                float posX = startX + (col * (chipW + gapX));
                float posY = startY - (row * (chipH + gapY));

                GameObject chipRoot = CreateUIPanel(gridPanel.transform, $"Slot_{index}",
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(posX, posY), new Vector2(chipW, chipH), new Color(0.04f, 0.08f, 0.14f, 0.95f));

                Image borderImg = chipRoot.GetComponent<Image>();
                borderImg.sprite = GetRoundedRectSprite((int)chipW, (int)chipH, 12f, new Color(0.04f, 0.08f, 0.14f, 0.95f), new Color(0.0f, 0.65f, 1f, 0.6f), 1.5f);

                // Botão para suporte a clique de mouse
                Button chipBtn = chipRoot.AddComponent<Button>();
                int capturedIdx = index;
                chipBtn.onClick.AddListener(() => OnSlotClicked(capturedIdx));

                // Retrato no lado esquerdo do chip
                GameObject portObj = CreateUIPanel(chipRoot.transform, "Portrait",
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(32f, 6f), new Vector2(52f, 52f), Color.white);
                Image iconImg = portObj.GetComponent<Image>();

                // Badge de Categoria
                GameObject catObj = CreateUIPanel(chipRoot.transform, "CatBadge",
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(6f, -6f), new Vector2(20f, 20f), Color.white);
                Image catBg = catObj.GetComponent<Image>();
                catBg.sprite = GetCircleSprite(20, new Color(0.0f, 0.65f, 0.9f), Color.white, 1f);
                Text catTxt = CreateUIText(catObj.transform, "Txt", "★", 10, FontStyle.Normal,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    Vector2.zero, Vector2.zero, Color.white, TextAnchor.MiddleCenter);

                // Nome do Monstro
                Text nameTxt = CreateUIText(chipRoot.transform, "Name", "Appmon", 11, FontStyle.Bold,
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                    new Vector2(62f, -8f), new Vector2(-68f, 18f), Color.white, TextAnchor.MiddleLeft);

                // Tag de Distância
                Text distTxt = CreateUIText(chipRoot.transform, "Dist", "⌖ 1 Tile", 9, FontStyle.Normal,
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                    new Vector2(62f, -24f), new Vector2(-68f, 14f), new Color(0.0f, 0.85f, 1f, 0.8f), TextAnchor.MiddleLeft);

                // Barra de HP Visual
                GameObject hpTrackObj = CreateUIPanel(chipRoot.transform, "HpTrack",
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                    new Vector2(62f, -40f), new Vector2(-70f, 6f), new Color(0.05f, 0.10f, 0.16f, 1f));
                hpTrackObj.GetComponent<Image>().sprite = GetRoundedRectSprite(96, 6, 3f, new Color(0.05f, 0.10f, 0.16f), Color.gray, 0.5f);

                GameObject hpFillObj = CreateUIPanel(hpTrackObj.transform, "HpFill",
                    new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f),
                    Vector2.zero, Vector2.zero, new Color(0.1f, 0.85f, 0.35f));
                Image hpFillImg = hpFillObj.GetComponent<Image>();
                hpFillImg.type = Image.Type.Filled;
                hpFillImg.fillMethod = Image.FillMethod.Horizontal;
                hpFillImg.fillAmount = 1f;

                Text hpTxt = CreateUIText(chipRoot.transform, "HpTxt", "HP 100/100", 9, FontStyle.Bold,
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                    new Vector2(62f, -48f), new Vector2(-68f, 14f), new Color(0.85f, 0.95f, 1f), TextAnchor.MiddleLeft);

                // Badge de Compatibilidade na Base do Chip
                GameObject compatBadge = CreateUIPanel(chipRoot.transform, "CompatBadge",
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 13f), new Vector2(156f, 20f), new Color(0f, 0.70f, 0.35f, 0.95f));
                Image compatBg = compatBadge.GetComponent<Image>();
                compatBg.sprite = GetRoundedRectSprite(156, 20, 6f, new Color(0f, 0.70f, 0.35f, 0.95f), Color.white, 1f);

                Text compatTxt = CreateUIText(compatBadge.transform, "Txt", "★ Gappai OK", 9, FontStyle.Bold,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    Vector2.zero, Vector2.zero, Color.white, TextAnchor.MiddleCenter);

                // Moldura Dourada do Cursor de Seleção
                GameObject cursorHighlight = CreateUIPanel(chipRoot.transform, "CursorHighlight",
                    new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
                    Vector2.zero, new Vector2(8f, 8f), Color.white);
                cursorHighlight.GetComponent<Image>().sprite = GetCyberCornerBracketSprite((int)chipW + 8, (int)chipH + 8);
                cursorHighlight.SetActive(false);

                slotUIList.Add(new GappaiSlotUI
                {
                    index = index,
                    root = chipRoot,
                    rectTransform = chipRoot.GetComponent<RectTransform>(),
                    backgroundImage = chipRoot.GetComponent<Image>(),
                    borderImage = borderImg,
                    iconImage = iconImg,
                    categoryBadgeBg = catBg,
                    categoryBadgeText = catTxt,
                    nameText = nameTxt,
                    distanceText = distTxt,
                    hpBarTrackObj = hpTrackObj,
                    hpBarFillImage = hpFillImg,
                    hpText = hpTxt,
                    compatibilityBadgeObj = compatBadge,
                    compatibilityBadgeBg = compatBg,
                    compatibilityBadgeText = compatTxt,
                    cursorHighlightObj = cursorHighlight
                });
            }
        }
    }

    private void BuildHolographicBlueprintDeck(Transform parent)
    {
        // Painel Direito: Projeção Holográfica // Blueprint Deck (525 x 520 px)
        GameObject bpPanel = CreateUIPanel(parent, "BlueprintPanel",
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-15f, -30f), new Vector2(525f, 520f), new Color(0.02f, 0.07f, 0.14f, 0.95f));
        bpPanel.GetComponent<Image>().sprite = GetRoundedRectSprite(525, 520, 16f, new Color(0.02f, 0.07f, 0.14f, 0.95f), new Color(0.0f, 0.85f, 1f, 0.8f), 2f);

        // Cabeçalho da Projeção Holográfica
        GameObject bpHeader = CreateUIPanel(bpPanel.transform, "Header",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -8f), new Vector2(505f, 32f), new Color(0.01f, 0.20f, 0.38f, 0.95f));
        bpHeader.GetComponent<Image>().sprite = GetRoundedRectSprite(505, 32, 6f, new Color(0.01f, 0.20f, 0.38f, 0.95f), new Color(0.0f, 0.95f, 1f, 0.7f), 1f);

        CreateUIText(bpHeader.transform, "Title", "PROJEÇÃO HOLOGRÁFICA // RESULTADO DA FUSÃO", 12, FontStyle.Bold,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(480f, 20f), Color.white, TextAnchor.MiddleCenter);

        // -------------------------------------------------------------
        // ESTADO 1: COMPATÍVEL (BLUEPRINT DE FUSÃO BEM-SUCEDIDA)
        // -------------------------------------------------------------
        blueprintCompatibleRoot = CreateUIPanel(bpPanel.transform, "CompatibleBlueprint",
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -16f), new Vector2(-24f, -56f), Color.clear);

        // Retrato da Nova Criatura Fundida com Moldura Dourada Neon
        GameObject resultPortFrame = CreateUIPanel(blueprintCompatibleRoot.transform, "PortraitFrame",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(12f, -10f), new Vector2(110f, 110f), new Color(0.02f, 0.15f, 0.25f, 0.95f));
        resultPortFrame.GetComponent<Image>().sprite = GetRoundedRectSprite(110, 110, 16f, new Color(0.02f, 0.15f, 0.25f, 0.95f), new Color(0.0f, 1.0f, 0.65f, 1f), 3f);

        GameObject resPortObj = CreateUIPanel(resultPortFrame.transform, "Portrait",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(98f, 98f), Color.white);
        blueprintResultPortrait = resPortObj.GetComponent<Image>();

        // Título e Ranks do Resultado
        blueprintResultNameText = CreateUIText(blueprintCompatibleRoot.transform, "ResultName", "Super Appmon", 20, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
            new Vector2(136f, -12f), new Vector2(-145f, 28f), new Color(0.0f, 1.0f, 0.65f), TextAnchor.MiddleLeft);

        blueprintResultRankText = CreateUIText(blueprintCompatibleRoot.transform, "ResultRank", "★ SUPER APPMON ★", 13, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
            new Vector2(136f, -42f), new Vector2(-145f, 22f), new Color(1.0f, 0.85f, 0.25f), TextAnchor.MiddleLeft);

        blueprintResultTypeProtocolText = CreateUIText(blueprintCompatibleRoot.transform, "TypeProtocol", "[SECURITY] • [FIREWALL]", 11, FontStyle.Normal,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
            new Vector2(136f, -66f), new Vector2(-145f, 20f), Color.white, TextAnchor.MiddleLeft);

        // Grade de Estatísticas da Nova Criatura
        GameObject statsBox = CreateUIPanel(blueprintCompatibleRoot.transform, "StatsBox",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -135f), new Vector2(470f, 90f), new Color(0.03f, 0.10f, 0.18f, 0.95f));
        statsBox.GetComponent<Image>().sprite = GetRoundedRectSprite(470, 90, 10f, new Color(0.03f, 0.10f, 0.18f, 0.95f), new Color(0.0f, 0.75f, 1f, 0.6f), 1.5f);

        blueprintStatsText = CreateUIText(statsBox.transform, "StatsTxt", "HP: 280  |  ATK: 95  |  DEF: 110", 12, FontStyle.Normal,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(440f, 74f), Color.white, TextAnchor.MiddleLeft);

        // Caixa de Habilidade Principal
        GameObject skillBox = CreateUIPanel(blueprintCompatibleRoot.transform, "SkillBox",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -238f), new Vector2(470f, 95f), new Color(0.02f, 0.12f, 0.22f, 0.90f));
        skillBox.GetComponent<Image>().sprite = GetRoundedRectSprite(470, 95, 10f, new Color(0.02f, 0.12f, 0.22f, 0.90f), new Color(0.15f, 0.85f, 0.45f, 0.7f), 1.5f);

        blueprintSkillTitleText = CreateUIText(skillBox.transform, "SkillTitle", "⚡ Habilidade: Hydro Quarantine", 12, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
            new Vector2(12f, -8f), new Vector2(-24f, 22f), new Color(0.0f, 1.0f, 0.65f), TextAnchor.MiddleLeft);

        blueprintSkillDescText = CreateUIText(skillBox.transform, "SkillDesc", "Dano em área e aprisionamento em terreno alagado.", 11, FontStyle.Normal,
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f),
            new Vector2(12f, 8f), new Vector2(-24f, -34f), new Color(0.90f, 0.95f, 1.0f), TextAnchor.UpperLeft);

        // Caixa de Passiva
        GameObject passiveBox = CreateUIPanel(blueprintCompatibleRoot.transform, "PassiveBox",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -345f), new Vector2(470f, 50f), new Color(0.02f, 0.10f, 0.18f, 0.90f));
        passiveBox.GetComponent<Image>().sprite = GetRoundedRectSprite(470, 50, 8f, new Color(0.02f, 0.10f, 0.18f, 0.90f), new Color(1f, 0.80f, 0.2f, 0.6f), 1.2f);

        blueprintPassiveText = CreateUIText(passiveBox.transform, "PassiveTxt", "🛡 Passiva: Domínio Aquático", 11, FontStyle.Normal,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(445f, 38f), Color.white, TextAnchor.MiddleLeft);

        // Faixa de Duração
        blueprintDurationText = CreateUIText(blueprintCompatibleRoot.transform, "DurationTxt", "⏱ Validade: Esta Batalha (IsTemporaryFusion = true)", 11, FontStyle.Bold,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 10f), new Vector2(460f, 24f), new Color(1.0f, 0.85f, 0.25f), TextAnchor.MiddleCenter);

        // -------------------------------------------------------------
        // ESTADO 2: INCOMPATÍVEL (BLOQUEIO ESTREITO // NADA PODE ACONTECER)
        // -------------------------------------------------------------
        blueprintIncompatibleRoot = CreateUIPanel(bpPanel.transform, "IncompatibleBlueprint",
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -16f), new Vector2(-24f, -56f), Color.clear);
        blueprintIncompatibleRoot.SetActive(false);

        // Banner de Alerta
        GameObject alertBox = CreateUIPanel(blueprintIncompatibleRoot.transform, "AlertBox",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -10f), new Vector2(470f, 110f), new Color(0.20f, 0.05f, 0.05f, 0.95f));
        alertBox.GetComponent<Image>().sprite = GetRoundedRectSprite(470, 110, 12f, new Color(0.20f, 0.05f, 0.05f, 0.95f), new Color(1.0f, 0.30f, 0.30f, 0.9f), 2.5f);

        blueprintIncompatibleTitleText = CreateUIText(alertBox.transform, "Title", "🔒 FUSÃO BLOQUEADA // INCOMPATÍVEL", 15, FontStyle.Bold,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -14f), new Vector2(440f, 26f), new Color(1.0f, 0.35f, 0.35f), TextAnchor.MiddleCenter);

        blueprintIncompatibleDescText = CreateUIText(alertBox.transform, "Desc", "Combinação inexistente no compêndio. Nenhuma ação pode ser executada.", 11, FontStyle.Normal,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 12f), new Vector2(440f, 54f), Color.white, TextAnchor.MiddleCenter);

        // Painel Guia com Fórmulas Válidas do Compêndio
        GameObject guideBox = CreateUIPanel(blueprintIncompatibleRoot.transform, "GuideBox",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -135f), new Vector2(470f, 240f), new Color(0.04f, 0.09f, 0.16f, 0.95f));
        guideBox.GetComponent<Image>().sprite = GetRoundedRectSprite(470, 240, 12f, new Color(0.04f, 0.09f, 0.16f, 0.95f), new Color(0.0f, 0.75f, 1f, 0.6f), 1.5f);

        blueprintIncompatibleGuideTitleText = CreateUIText(guideBox.transform, "Title", "📖 Fórmulas Oficiais Cadastradas no Compêndio:", 12, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
            new Vector2(14f, -10f), new Vector2(-28f, 24f), new Color(0.0f, 0.90f, 1f), TextAnchor.MiddleLeft);

        blueprintIncompatibleGuideText = CreateUIText(guideBox.transform, "List", "• Data-Viper + Shitakumon ➔ Hydro-Vipermon", 12, FontStyle.Normal,
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f),
            new Vector2(14f, 12f), new Vector2(-28f, -44f), Color.white, TextAnchor.UpperLeft);

        CreateUIText(blueprintIncompatibleRoot.transform, "Hint", "Dica: Selecione outro combatente na grade ou pressione [ESC] para manter seu turno.", 11, FontStyle.Italic,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 10f), new Vector2(460f, 24f), new Color(0.7f, 0.8f, 0.9f), TextAnchor.MiddleCenter);

        // -------------------------------------------------------------
        // ESTADO 3: NENHUM ALVO SELECIONADO
        // -------------------------------------------------------------
        blueprintEmptyRoot = CreateUIPanel(bpPanel.transform, "EmptyBlueprint",
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -16f), new Vector2(-24f, -56f), Color.clear);

        CreateUIText(blueprintEmptyRoot.transform, "EmptyTxt",
            "⌖ RADAR DE COMBATE ATIVO\n\n" +
            "Selecione um monstro aliado na grade 4x4 ao lado\n" +
            "para testar a compatibilidade de fusão no compêndio.", 13, FontStyle.Bold,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(400f, 150f), new Color(0.0f, 0.80f, 1f, 0.8f), TextAnchor.MiddleCenter);
    }

    private void BuildFooterControls(Transform parent)
    {
        // Barra Inferior de Comandos (1270 x 75 px)
        GameObject footerBar = CreateUIPanel(parent, "FooterControls",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 14f), new Vector2(1270f, 75f), Color.clear);

        // Botão Central Confirmar Fusão
        confirmButton = CreateButton(footerBar.transform, "ConfirmBtn", "", 0,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 0f), new Vector2(400f, 58f), () => ConfirmSelection());
        confirmButtonImage = confirmButton.GetComponent<Image>();
        confirmButtonImage.sprite = GetGlossyPillSprite(400, 58, new Color(0.10f, 0.88f, 0.35f), new Color(0.02f, 0.58f, 0.20f), Color.white);

        confirmButtonSubText = CreateUIText(confirmButton.transform, "SubTxt", "CONFIRMAR FUSÃO DE APPMON", 9, FontStyle.Bold,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -8f), new Vector2(360f, 14f), Color.white, TextAnchor.MiddleCenter);

        confirmButtonMainText = CreateUIText(confirmButton.transform, "MainTxt", "★ EXECUTAR FUSÃO GAPPAI [ENTER] ★", 15, FontStyle.Bold,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -6f), new Vector2(380f, 26f), Color.white, TextAnchor.MiddleCenter);

        // Botão Voltar / Cancelar [ESC]
        backButton = CreateButton(footerBar.transform, "BackBtn", "◀ VOLTAR [ESC]", 12,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-15f, 0f), new Vector2(160f, 48f), () => CancelSelection());
        backButton.GetComponent<Image>().sprite = GetRoundedRectSprite(160, 48, 10f, new Color(0.08f, 0.12f, 0.18f, 0.95f), new Color(0.35f, 0.45f, 0.60f, 0.8f), 1.5f);

        // Legenda de Comandos no Canto Esquerdo
        CreateUIText(footerBar.transform, "ControlsLegend",
            "[W / A / S / D ou SETAS] : Navegar Grade 4x4\n[MOUSE] : Seleção Direta    [ESC] : Voltar ao Menu", 10, FontStyle.Normal,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(15f, 0f), new Vector2(380f, 36f), new Color(0.7f, 0.85f, 0.95f, 0.8f), TextAnchor.MiddleLeft);
    }

    // =========================================================================
    // UTILITÁRIOS PROCEDURAIS DE UI E SPRITES
    // =========================================================================

    private GameObject CreateUIPanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta, Color color)
    {
        GameObject go = new GameObject(name);
        go.layer = 5;
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;

        Image img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = true;
        return go;
    }

    private Text CreateUIText(Transform parent, string name, string content, int fontSize, FontStyle style,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta, Color color, TextAnchor align)
    {
        GameObject go = new GameObject(name);
        go.layer = 5;
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;

        Text txt = go.AddComponent<Text>();
        txt.font = GetBestFont();
        txt.text = content;
        txt.fontSize = fontSize;
        txt.fontStyle = style;
        txt.color = color;
        txt.alignment = align;
        txt.raycastTarget = false;
        return txt;
    }

    private Button CreateButton(Transform parent, string name, string label, int fontSize,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta, Action onClick)
    {
        GameObject go = CreateUIPanel(parent, name, anchorMin, anchorMax, pivot, anchoredPos, sizeDelta, Color.white);
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = go.GetComponent<Image>();
        btn.onClick.AddListener(() => onClick?.Invoke());

        if (!string.IsNullOrEmpty(label))
        {
            CreateUIText(go.transform, "Label", label, fontSize, FontStyle.Bold,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero, Color.white, TextAnchor.MiddleCenter);
        }

        return btn;
    }

    private Sprite GetRoundedRectSprite(int width, int height, float cornerRadius, Color fillColor, Color borderColor, float borderThickness)
    {
        string key = $"RRect_{width}_{height}_{cornerRadius}_{fillColor}_{borderColor}_{borderThickness}";
        if (s_spriteCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[width * height];
        float halfW = width * 0.5f;
        float halfH = height * 0.5f;
        float r = Mathf.Min(cornerRadius, Mathf.Min(halfW, halfH));

        for (int y = 0; y < height; y++)
        {
            float py = y - halfH + 0.5f;
            for (int x = 0; x < width; x++)
            {
                float px = x - halfW + 0.5f;
                float qx = Mathf.Abs(px) - (halfW - r);
                float qy = Mathf.Abs(py) - (halfH - r);
                float extX = Mathf.Max(qx, 0.0f);
                float extY = Mathf.Max(qy, 0.0f);
                float dist = Mathf.Sqrt(extX * extX + extY * extY) + Mathf.Min(Mathf.Max(qx, qy), 0.0f) - r;

                Color c;
                if (dist > 1.0f) c = Color.clear;
                else if (dist > 0.0f) c = new Color(borderColor.r, borderColor.g, borderColor.b, borderColor.a * (1.0f - dist));
                else if (dist >= -borderThickness) c = borderColor;
                else
                {
                    float normY = (float)y / height;
                    c = Color.Lerp(fillColor * 0.90f, fillColor * 1.10f, normY);
                    c.a = fillColor.a;
                }
                pixels[y * width + x] = c;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        Sprite sp = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
        s_spriteCache[key] = sp;
        return sp;
    }

    private Sprite GetGlossyPillSprite(int width, int height, Color topColor, Color botColor, Color outlineColor)
    {
        string key = $"GlossyPill_{width}_{height}_{topColor}_{botColor}_{outlineColor}";
        if (s_spriteCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[width * height];
        float radius = height * 0.5f;
        float capLeft = radius;
        float capRight = width - radius;

        for (int y = 0; y < height; y++)
        {
            float normY = (float)y / height;
            for (int x = 0; x < width; x++)
            {
                float dist;
                if (x < capLeft)
                {
                    float dx = x - capLeft;
                    float dy = y - radius;
                    dist = Mathf.Sqrt(dx * dx + dy * dy) - radius;
                }
                else if (x > capRight)
                {
                    float dx = x - capRight;
                    float dy = y - radius;
                    dist = Mathf.Sqrt(dx * dx + dy * dy) - radius;
                }
                else
                {
                    dist = Mathf.Abs(y - radius) - radius;
                }

                if (dist > 0.5f) pixels[y * width + x] = Color.clear;
                else if (dist >= -2.0f) pixels[y * width + x] = outlineColor;
                else
                {
                    Color baseCol = Color.Lerp(botColor, topColor, normY);
                    if (normY > 0.55f && dist < -4f)
                    {
                        float gloss = (normY - 0.55f) / 0.45f;
                        baseCol = Color.Lerp(baseCol, Color.white, gloss * 0.55f);
                    }
                    pixels[y * width + x] = baseCol;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        Sprite sp = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
        s_spriteCache[key] = sp;
        return sp;
    }

    private Sprite GetCircleSprite(int size, Color fillColor, Color borderColor, float borderThickness)
    {
        string key = $"Circ_{size}_{fillColor}_{borderColor}_{borderThickness}";
        if (s_spriteCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[size * size];
        float radius = (size * 0.5f) - 1.0f;
        float center = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center + 0.5f;
                float dy = y - center + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy) - radius;

                if (dist > 1.0f) pixels[y * size + x] = Color.clear;
                else if (dist > 0.0f) pixels[y * size + x] = new Color(borderColor.r, borderColor.g, borderColor.b, borderColor.a * (1.0f - dist));
                else if (dist >= -borderThickness) pixels[y * size + x] = borderColor;
                else pixels[y * size + x] = fillColor;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        Sprite sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        s_spriteCache[key] = sp;
        return sp;
    }

    private Sprite GetCyberCornerBracketSprite(int width, int height)
    {
        string key = $"CornerBracket_{width}_{height}";
        if (s_spriteCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

        int cornerLen = Mathf.Min(16, Mathf.Min(width, height) / 3);
        int thick = 3;
        Color gold = new Color(1.0f, 0.85f, 0.15f, 1.0f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool isTop = (y >= height - thick) && (x < cornerLen || x >= width - cornerLen);
                bool isBottom = (y < thick) && (x < cornerLen || x >= width - cornerLen);
                bool isLeft = (x < thick) && (y < cornerLen || y >= height - cornerLen);
                bool isRight = (x >= width - thick) && (y < cornerLen || y >= height - cornerLen);

                if (isTop || isBottom || isLeft || isRight)
                {
                    pixels[y * width + x] = gold;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        Sprite sp = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
        s_spriteCache[key] = sp;
        return sp;
    }
}
