using System;
using System.Collections.Generic;
using TacticalBattle.Appmon;
using TacticalBattle.AppLink;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Interface do Sistema de App-Link reproduzindo com fidelidade a tela de toque
/// do Nintendo 3DS (Digimon Universe: Appli Monsters).
/// </summary>
public class AppLinkMenuUI : MonoBehaviour
{
    public static AppLinkMenuUI Instance;

    private Canvas canvas;
    private GameObject rootContainer;
    private Font uiFont;

    // Estado da seleção
    private Unit currentFieldUnit;
    private List<AppmonData> bagAppmons = new List<AppmonData>();
    private int selectedIndex = 0;
    private int currentPage = 0;
    private const int COLS = 5;
    private const int ROWS = 3;
    private const int SLOTS_PER_PAGE = COLS * ROWS; // 15 chips por página (5x3)

    // Ação ao fechar
    public Action<bool> OnClosed; // bool didPerformAction

    // Elementos do Chassi / Moldura 3DS
    private GameObject consoleFrame;
    private Text counterText;
    private Text statusToggleText;

    // Elementos do Top Deck (Cyan Header)
    private Image mainSlotPortrait;
    private Text mainSlotCatBadge;
    private Text mainSlotNameText;
    private Text mainSlotRankText;

    private Image targetSlotPortrait;
    private Text targetSlotCatBadge;
    private GameObject targetEmptyBox;
    private Text targetSlotRankText;
    private Image targetSlotBorder;
    private Text instructionSubtitleText;

    // Elementos do Monitor Central Verde Matrix (5x3 Grid)
    private List<BagSlotUI> slotUIList = new List<BagSlotUI>();
    private Text pageIndicatorText;
    private Button prevPageBtn;
    private Button nextPageBtn;

    // Elementos do Rodapé (Botão Confirmar e Comandos)
    private Button confirmLinkButton;
    private Text confirmButtonMainText;
    private Text confirmButtonSubText;
    private Button unlinkButton;
    private Button backButton;

    private float pulseTimer = 0f;

    public bool IsOpen => rootContainer != null && rootContainer.activeSelf;

    public class BagSlotUI
    {
        public int index;
        public GameObject root;
        public RectTransform rectTransform;
        public Image backgroundImage;
        public Image borderImage;
        public Image iconImage;
        public Image categoryBadgeImage;
        public Text categoryBadgeText;
        public Text nameText;
        public GameObject mainStampObj; // Carimbo diagonal vermelho "PRINCIPAL"
        public GameObject linkedTagObj;
        public Text linkedTagText;
        public GameObject cursorHighlightObj;
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
            GameObject cObj = new GameObject("AppLinkCanvas");
            cObj.layer = 5;
            canvas = cObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 998;

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
            // Garante que o EventSystem utilize o módulo do New Input System para cliques funcionarem
            var standalone = existingEventSystem.GetComponent<StandaloneInputModule>();
            if (standalone != null)
            {
                Destroy(standalone);
            }
            if (existingEventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
            {
                existingEventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
        }
#endif
    }

    public void Open(Unit fieldUnit, Action<bool> onClosedCallback = null)
    {
        currentFieldUnit = fieldUnit;
        OnClosed = onClosedCallback;

        // Limpa referências órfãs antes de abrir a Bag
        AppLinkService.CleanStaleLinks();

        // Coleta Appmons da reserva (Bag) estritamente filtrados para esta unidade:
        // Appmons já vinculados a outros combatentes NÃO aparecem nesta lista!
        bagAppmons = AppLinkService.GetBagAppmons(currentFieldUnit);

        // Localiza dados do Appmon de campo (seja por componente ou nome na base)
        AppmonData fieldData = currentFieldUnit != null ? currentFieldUnit.GetComponent<AppmonCharacter>()?.appmonData : null;
        if (fieldData == null && currentFieldUnit != null)
        {
            fieldData = AppmonDatabase.Get(currentFieldUnit.unitName);
        }

        // Se o Appmon de campo estiver na lista, garante que ele apareça no primeiro slot como "PRINCIPAL"
        if (fieldData != null && !bagAppmons.Exists(a => a.id.Equals(fieldData.id, StringComparison.OrdinalIgnoreCase)))
        {
            bagAppmons.Insert(0, fieldData);
        }

        // Se a unidade já possui um parceiro vinculado, seleciona-o por padrão
        if (currentFieldUnit != null && currentFieldUnit.IsLinked && currentFieldUnit.linkedBagAppmon != null)
        {
            int foundIdx = bagAppmons.FindIndex(a => a.id.Equals(currentFieldUnit.linkedBagAppmon.id, StringComparison.OrdinalIgnoreCase) ||
                                                     a.name.Equals(currentFieldUnit.linkedBagAppmon.name, StringComparison.OrdinalIgnoreCase));
            selectedIndex = (foundIdx >= 0) ? foundIdx : 0;
        }
        else
        {
            // Unidade SEM vínculo prévio: começa sem seleção automática (-1)
            // O TargetSlot começará com o visual padrão vazio até que o jogador escolha intencionalmente um Appmon.
            selectedIndex = -1;
        }

        currentPage = (selectedIndex >= 0) ? (selectedIndex / SLOTS_PER_PAGE) : 0;

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
        if (bagAppmons.Count == 0) return;

        // Se ainda não havia seleção (abriu vazio), a primeira navegação foca no primeiro parceiro disponível
        if (selectedIndex < 0)
        {
            selectedIndex = (bagAppmons.Count > 1) ? 1 : 0;
            currentPage = selectedIndex / SLOTS_PER_PAGE;
            RefreshUI();
            return;
        }

        int totalOnPage = Mathf.Min(SLOTS_PER_PAGE, bagAppmons.Count - (currentPage * SLOTS_PER_PAGE));
        if (totalOnPage <= 0) return;

        int curSlot = selectedIndex % SLOTS_PER_PAGE;

        if (dirX > 0)
        {
            if (curSlot + 1 < totalOnPage) selectedIndex++;
            else if (HasNextPage()) { NextPage(); selectedIndex = currentPage * SLOTS_PER_PAGE; }
            RefreshUI();
        }
        else if (dirX < 0)
        {
            if (curSlot - 1 >= 0) selectedIndex--;
            else if (HasPrevPage()) { PrevPage(); selectedIndex = (currentPage * SLOTS_PER_PAGE) + SLOTS_PER_PAGE - 1; }
            RefreshUI();
        }
        else if (dirY > 0) // Baixo (+COLS)
        {
            if (curSlot + COLS < totalOnPage) selectedIndex += COLS;
            else if (HasNextPage()) { NextPage(); selectedIndex = currentPage * SLOTS_PER_PAGE + (curSlot % COLS); }
            RefreshUI();
        }
        else if (dirY < 0) // Cima (-COLS)
        {
            if (curSlot - COLS >= 0) selectedIndex -= COLS;
            else if (HasPrevPage()) { PrevPage(); selectedIndex = (currentPage * SLOTS_PER_PAGE) + (curSlot % COLS); }
            RefreshUI();
        }
    }

    public void ConfirmSelection()
    {
        ExecuteLinkAction();
    }

    public void CancelSelection()
    {
        CloseScreen(didPerformAction: false);
    }

    public void ToggleUnlink()
    {
        if (currentFieldUnit != null && currentFieldUnit.IsLinked)
        {
            ExecuteUnlinkAction();
        }
    }

    public void PageLeft()
    {
        if (HasPrevPage()) PrevPage();
    }

    public void PageRight()
    {
        if (HasNextPage()) NextPage();
    }

    private void HandleInput()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) Navigate(-1, 0);
        else if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) Navigate(1, 0);
        else if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) Navigate(0, -1);
        else if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) Navigate(0, 1);

        if (kb.qKey.wasPressedThisFrame) PageLeft();
        if (kb.eKey.wasPressedThisFrame) PageRight();

        if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.zKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
        {
            ConfirmSelection();
        }

        if (kb.uKey.wasPressedThisFrame)
        {
            ToggleUnlink();
        }

        if (kb.escapeKey.wasPressedThisFrame || kb.xKey.wasPressedThisFrame || kb.backspaceKey.wasPressedThisFrame)
        {
            CancelSelection();
        }
#endif
    }

    private void UpdateAnimations()
    {
        // Animação de pulso nos colchetes dourados e na borda do target slot
        float pulse = 0.75f + 0.25f * Mathf.Sin(pulseTimer * 6f);

        int curSlot = selectedIndex % SLOTS_PER_PAGE;
        if (curSlot >= 0 && curSlot < slotUIList.Count)
        {
            var slot = slotUIList[curSlot];
            if (slot.cursorHighlightObj != null && slot.cursorHighlightObj.activeSelf)
            {
                var img = slot.cursorHighlightObj.GetComponent<Image>();
                if (img != null) img.color = new Color(1f, 0.90f, 0.20f, pulse);
            }
        }

        if (targetSlotBorder != null)
        {
            targetSlotBorder.color = new Color(0.0f, 0.95f, 1f, 0.70f + 0.30f * Mathf.Sin(pulseTimer * 4f));
        }
    }

    public void RefreshUI()
    {
        if (currentFieldUnit == null) return;

        AppmonData fieldData = currentFieldUnit.GetComponent<AppmonCharacter>()?.appmonData;

        // 1. Atualiza o Slot Main (Esquerda)
        if (fieldData != null)
        {
            mainSlotPortrait.sprite = GetAppmonSprite(fieldData);
            mainSlotCatBadge.text = GetCategorySymbol(fieldData.primaryCategory);
        }
        else
        {
            mainSlotPortrait.sprite = currentFieldUnit.GetPortraitSprite();
            mainSlotCatBadge.text = GetCategorySymbol(currentFieldUnit.category);
        }
        mainSlotNameText.text = currentFieldUnit.unitName;
        mainSlotRankText.text = currentFieldUnit.rank.ToString().ToUpper();

        // 2. Popula a Grade 5x3
        int startIndex = currentPage * SLOTS_PER_PAGE;
        int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)bagAppmons.Count / SLOTS_PER_PAGE));
        if (pageIndicatorText != null) pageIndicatorText.text = $"{currentPage + 1}/{totalPages}";
        if (counterText != null) counterText.text = $"{bagAppmons.Count:D3}";

        for (int i = 0; i < slotUIList.Count; i++)
        {
            int dataIdx = startIndex + i;
            var slot = slotUIList[i];

            if (dataIdx < bagAppmons.Count)
            {
                slot.root.SetActive(true);
                AppmonData app = bagAppmons[dataIdx];
                slot.index = dataIdx;

                slot.iconImage.sprite = GetAppmonSprite(app);
                slot.nameText.text = app.name;

                // Cor da Moldura do Chip segundo a Categoria
                Color catColor = GetCategoryBorderColor(app.primaryCategory);
                slot.borderImage.sprite = GetChipBorderSprite(160, 140, catColor, 3.5f);

                // Badge Circular de Categoria no Canto Superior Esquerdo
                slot.categoryBadgeImage.sprite = GetCircleSprite(28, GetCategoryBadgeBgColor(app.primaryCategory), Color.white, 1.5f);
                slot.categoryBadgeText.text = GetCategorySymbol(app.primaryCategory);

                // Verifica se este slot é o Appmon atualmente desdobrado em campo (Main)
                bool isThisFieldAppmon = (fieldData != null && app.id.Equals(fieldData.id, StringComparison.OrdinalIgnoreCase)) ||
                                         app.name.Equals(currentFieldUnit.unitName, StringComparison.OrdinalIgnoreCase);

                if (isThisFieldAppmon)
                {
                    slot.mainStampObj.SetActive(true);
                    slot.backgroundImage.color = new Color(0.12f, 0.12f, 0.14f, 0.95f);
                }
                else
                {
                    slot.mainStampObj.SetActive(false);
                    slot.backgroundImage.color = new Color(0.06f, 0.10f, 0.16f, 0.95f);
                }

                // Marcação [LINKADO] / [VINCULADO]
                bool isLinked = AppLinkService.IsAppmonLinked(app);
                bool isLinkedToThisUnit = currentFieldUnit.linkedBagAppmon != null &&
                                          currentFieldUnit.linkedBagAppmon.id.Equals(app.id, StringComparison.OrdinalIgnoreCase);

                if (isLinked)
                {
                    slot.linkedTagObj.SetActive(true);
                    if (isLinkedToThisUnit)
                    {
                        slot.linkedTagText.text = "[VINCULADO]";
                        slot.linkedTagText.color = new Color(0.0f, 0.95f, 1f);
                    }
                    else
                    {
                        Unit otherHolder = AppLinkService.GetLinkedFieldUnit(app);
                        string holderName = otherHolder != null ? otherHolder.unitName.ToUpper() : "OUTRO";
                        slot.linkedTagText.text = $"[{holderName}]";
                        slot.linkedTagText.color = new Color(1f, 0.40f, 0.40f);
                        slot.backgroundImage.color = new Color(0.09f, 0.04f, 0.05f, 0.95f);
                    }
                }
                else
                {
                    slot.linkedTagObj.SetActive(false);
                }

                // Cursor de foco e efeito Pop-up (escala aumentada como no 3DS)
                bool isFocused = (dataIdx == selectedIndex);
                slot.cursorHighlightObj.SetActive(isFocused);

                if (isFocused)
                {
                    slot.rectTransform.localScale = Vector3.one * 1.25f;
                    slot.root.transform.SetAsLastSibling();
                }
                else
                {
                    slot.rectTransform.localScale = Vector3.one;
                }
            }
            else
            {
                slot.root.SetActive(false);
            }
        }

        // 3. Atualiza o Slot Alvo de Applink (Destino indicado pela seta!)
        UpdateTargetSlot();
    }

    private void UpdateTargetSlot()
    {
        if (selectedIndex < 0 || selectedIndex >= bagAppmons.Count)
        {
            ShowEmptyTargetSlot();
            return;
        }

        AppmonData selectedApp = bagAppmons[selectedIndex];
        AppmonData fieldData = currentFieldUnit.GetComponent<AppmonCharacter>()?.appmonData;

        // Se for o próprio Main desdobrado em campo
        bool isThisFieldAppmon = (fieldData != null && selectedApp.id.Equals(fieldData.id, StringComparison.OrdinalIgnoreCase)) ||
                                 selectedApp.name.Equals(currentFieldUnit.unitName, StringComparison.OrdinalIgnoreCase);

        if (isThisFieldAppmon)
        {
            ShowEmptyTargetSlot();
            instructionSubtitleText.text = "<color=#FFAA00>O Appmon ativo em campo não pode ser linkado a si mesmo!</color>";
            confirmLinkButton.interactable = false;
            confirmButtonMainText.text = "INDISPONÍVEL";
            return;
        }

        // Projeta o Appmon selecionado no slot de destino
        targetEmptyBox.SetActive(false);
        targetSlotPortrait.gameObject.SetActive(true);
        targetSlotPortrait.sprite = GetAppmonSprite(selectedApp);

        targetSlotCatBadge.gameObject.SetActive(true);
        targetSlotCatBadge.text = GetCategorySymbol(selectedApp.primaryCategory);
        targetSlotRankText.text = selectedApp.rank.ToString().ToUpper();

        // Calcula simulação de bônus e sinergia
        var calc = AppLinkService.CalculateBonus(currentFieldUnit, selectedApp);

        bool canLink = AppLinkService.CanLink(currentFieldUnit, selectedApp, out string reason);

        if (calc.hasCompatibility)
        {
            instructionSubtitleText.text = $"<color=#00FFAA>★ SINERGIA DE CATEGORIA! (+50% AFINIDADE) ➔ {calc.GetBonusSummary()}</color>";
        }
        else
        {
            instructionSubtitleText.text = $"Bônus de Atributos: <color=#00E5FF>{calc.GetBonusSummary()}</color>";
        }

        // 1. Se for o próprio parceiro já vinculado a este personagem
        bool isCurrentlyLinkedPartner = currentFieldUnit.IsLinked && currentFieldUnit.linkedBagAppmon != null &&
                                        currentFieldUnit.linkedBagAppmon.id.Equals(selectedApp.id, StringComparison.OrdinalIgnoreCase);

        if (isCurrentlyLinkedPartner)
        {
            instructionSubtitleText.text = $"<color=#00FFAA>● PARCEIRO ATUAL VINCULADO! (Clique para Desvincular)</color>";
            confirmLinkButton.interactable = true;
            confirmButtonMainText.text = "✕  DESVINCULAR";
            if (confirmButtonSubText != null) confirmButtonSubText.text = "DESVINCULAR";
            unlinkButton.gameObject.SetActive(true);
            return;
        }

        // 2. Se o Appmon selecionado já estiver linkado a outro combatente
        Unit otherHolder = AppLinkService.GetLinkedFieldUnit(selectedApp);
        if (otherHolder != null && otherHolder != currentFieldUnit)
        {
            instructionSubtitleText.text = $"<color=#FF5555>Este Appmon já está [LINKADO] a {otherHolder.unitName}! Cada criatura deve ter um link diferente.</color>";
            confirmLinkButton.interactable = false;
            confirmButtonMainText.text = "EM USO";
            if (confirmButtonSubText != null) confirmButtonSubText.text = "";
            unlinkButton.gameObject.SetActive(currentFieldUnit.IsLinked);
            return;
        }

        // 3. Se for desdobrado em combate no campo
        if (AppLinkService.IsAppmonDeployed(selectedApp))
        {
            instructionSubtitleText.text = "<color=#FF5555>Este Appmon está em combate ativo no campo!</color>";
            confirmLinkButton.interactable = false;
            confirmButtonMainText.text = "EM CAMPO";
            if (confirmButtonSubText != null) confirmButtonSubText.text = "";
            unlinkButton.gameObject.SetActive(currentFieldUnit.IsLinked);
            return;
        }

        // 4. REGRA: Apenas 1 Link por personagem (Permite substituição / troca direta!)
        if (currentFieldUnit.IsLinked)
        {
            confirmLinkButton.interactable = true;
            confirmButtonMainText.text = "TROCAR LINK";
            if (confirmButtonSubText != null) confirmButtonSubText.text = "CONFIRMAR";

            string swapNotice = $"Substituirá {currentFieldUnit.linkedBagAppmon.name} por {selectedApp.name}.";
            if (calc.hasCompatibility)
            {
                instructionSubtitleText.text = $"<color=#00FFAA>★ SINERGIA! {swapNotice} ➔ {calc.GetBonusSummary()}</color>";
            }
            else
            {
                instructionSubtitleText.text = $"{swapNotice} Bônus: <color=#00E5FF>{calc.GetBonusSummary()}</color>";
            }
            unlinkButton.gameObject.SetActive(true);
            return;
        }

        // 5. Novo Vínculo para combatente livre
        if (canLink)
        {
            confirmLinkButton.interactable = true;
            confirmButtonMainText.text = "VINCULAR LINK";
            if (confirmButtonSubText != null) confirmButtonSubText.text = "CONFIRMAR";
            unlinkButton.gameObject.SetActive(false);
        }
        else
        {
            confirmLinkButton.interactable = false;
            confirmButtonMainText.text = "BLOQUEADO";
            if (confirmButtonSubText != null) confirmButtonSubText.text = "";
            instructionSubtitleText.text = $"<color=#FF5555>{reason}</color>";
            unlinkButton.gameObject.SetActive(false);
        }
    }

    private void ShowEmptyTargetSlot()
    {
        if (currentFieldUnit != null && currentFieldUnit.IsLinked && currentFieldUnit.linkedBagAppmon != null)
        {
            targetEmptyBox.SetActive(false);
            targetSlotPortrait.gameObject.SetActive(true);
            targetSlotPortrait.sprite = GetAppmonSprite(currentFieldUnit.linkedBagAppmon);
            targetSlotCatBadge.gameObject.SetActive(true);
            targetSlotCatBadge.text = GetCategorySymbol(currentFieldUnit.linkedBagAppmon.primaryCategory);
            targetSlotRankText.text = currentFieldUnit.linkedBagAppmon.rank.ToString().ToUpper();
            instructionSubtitleText.text = $"<color=#00FFAA>● VÍNCULO ATIVO: {currentFieldUnit.linkedBagAppmon.name} (Apenas 1 Link por personagem)</color>";
            confirmLinkButton.interactable = true;
            confirmButtonMainText.text = "✕  DESVINCULAR";
            unlinkButton.gameObject.SetActive(true);
            return;
        }

        targetEmptyBox.SetActive(true);
        targetSlotPortrait.gameObject.SetActive(false);
        targetSlotCatBadge.gameObject.SetActive(false);
        targetSlotRankText.text = "APPLINK";
        instructionSubtitleText.text = "Selecione um Appmon na grade abaixo para vincular";
        confirmLinkButton.interactable = false;
        confirmButtonMainText.text = "VINCULAR";
        unlinkButton.gameObject.SetActive(false);
    }

    private void ExecuteLinkAction()
    {
        if (currentFieldUnit == null || selectedIndex < 0 || selectedIndex >= bagAppmons.Count) return;

        AppmonData selectedApp = bagAppmons[selectedIndex];

        // Se o Appmon clicado for o parceiro atualmente vinculado, executa a desvinculação
        if (currentFieldUnit.IsLinked && currentFieldUnit.linkedBagAppmon != null &&
            currentFieldUnit.linkedBagAppmon.id.Equals(selectedApp.id, StringComparison.OrdinalIgnoreCase))
        {
            ExecuteUnlinkAction();
            return;
        }

        // SEGURANÇA: Bloqueia se o botão estiver desativado ou indisponível
        if (confirmLinkButton != null && !confirmLinkButton.interactable)
        {
            Debug.LogWarning($"[AppLinkUI] Conexão bloqueada: o Appmon {selectedApp.name} não está disponível.");
            return;
        }

        // Validação estrita de exclusividade 1-para-1
        Unit otherHolder = AppLinkService.GetLinkedFieldUnit(selectedApp);
        if (otherHolder != null && otherHolder != currentFieldUnit)
        {
            Debug.LogWarning($"[AppLinkUI] Tentativa de link duplicado bloqueada: {selectedApp.name} já pertence a {otherHolder.unitName}.");
            RefreshUI();
            return;
        }

        if (AppLinkService.ApplyLink(currentFieldUnit, selectedApp, out string error))
        {
            CloseScreen(didPerformAction: true);
        }
        else
        {
            Debug.LogWarning($"[AppLinkUI] Falha ao conectar: {error}");
            RefreshUI();
        }
    }

    private void ExecuteUnlinkAction()
    {
        if (currentFieldUnit == null || !currentFieldUnit.IsLinked) return;

        if (AppLinkService.RemoveLink(currentFieldUnit))
        {
            CloseScreen(didPerformAction: true);
        }
    }

    public void CloseScreen(bool didPerformAction)
    {
        Hide();
        OnClosed?.Invoke(didPerformAction);
    }

    private bool HasNextPage() => (currentPage + 1) * SLOTS_PER_PAGE < bagAppmons.Count;
    private bool HasPrevPage() => currentPage > 0;

    private void NextPage()
    {
        if (HasNextPage())
        {
            currentPage++;
            selectedIndex = currentPage * SLOTS_PER_PAGE;
            RefreshUI();
        }
    }

    private void PrevPage()
    {
        if (HasPrevPage())
        {
            currentPage--;
            selectedIndex = currentPage * SLOTS_PER_PAGE;
            RefreshUI();
        }
    }

    // =========================================================================
    // CONSTRUÇÃO PROCEDURAL DA INTERFACE (Estilo Nintendo 3DS Appli Monsters)
    // =========================================================================

    private void BuildUI()
    {
        // 1. Contêiner Raiz com backdrop escurecido
        rootContainer = CreateUIPanel(canvas.transform, "AppLink3DSRoot",
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0.01f, 0.02f, 0.05f, 0.88f));

        // 2. Chassi do Console Portátil (1240 x 900 px centralizado)
        consoleFrame = CreateUIPanel(rootContainer.transform, "ConsoleChassis",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(1260f, 910f), new Color(0.08f, 0.12f, 0.18f, 1f));
        consoleFrame.GetComponent<Image>().sprite = GetRoundedRectSprite(1260, 910, 24f, new Color(0.08f, 0.12f, 0.18f, 1f), new Color(0.0f, 0.85f, 1.0f, 0.8f), 2.5f);

        // 3. Bezel Esquerdo Metálico Branco (Estilo 3DS)
        BuildLeftWhiteBezel(consoleFrame.transform);

        // 4. Bezel Direito com Grip Escuro e Botão Status
        BuildRightDarkGrip(consoleFrame.transform);

        // 5. Barra Superior Azul Ciano Elétrico (Dual-Slot Deck)
        BuildTopCyanHeader(consoleFrame.transform);

        // 6. Monitor Central Verde CRT Matrix (Grade 5x3)
        BuildMatrixGreenMonitor(consoleFrame.transform);

        // 7. Barra Inferior com Botão Confirmar
        BuildBottomControls(consoleFrame.transform);
    }

    private void BuildLeftWhiteBezel(Transform parent)
    {
        GameObject leftBar = CreateUIPanel(parent, "LeftBezel",
            new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
            new Vector2(10f, 0f), new Vector2(75f, -20f), new Color(0.92f, 0.94f, 0.96f, 1f));
        leftBar.GetComponent<Image>().sprite = GetRoundedRectSprite(75, 880, 14f, new Color(0.92f, 0.94f, 0.96f, 1f), new Color(0.70f, 0.75f, 0.80f, 0.8f), 1.5f);

        // Título Vertical / Tipografia: APPMON
        Text titleTxt = CreateUIText(leftBar.transform, "TitleTxt", "A\nP\nP\nM\nO\nN", 16, FontStyle.Bold,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -25f), new Vector2(50f, 130f), new Color(0.10f, 0.20f, 0.35f), TextAnchor.UpperCenter);

        // Sub-rótulo: RESERVA
        Text subTxt = CreateUIText(leftBar.transform, "SubTxt", "RESERVA\nMOCHILA", 10, FontStyle.Bold,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -165f), new Vector2(70f, 40f), new Color(0.25f, 0.35f, 0.45f), TextAnchor.UpperCenter);

        // Linhas decorativas do chassi
        for (int i = 0; i < 4; i++)
        {
            GameObject line = CreateUIPanel(leftBar.transform, $"DecoLine_{i}",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -50f + (i * 22f)), new Vector2(45f, 4f), new Color(0.75f, 0.80f, 0.85f));
        }
    }

    private void BuildRightDarkGrip(Transform parent)
    {
        GameObject rightBar = CreateUIPanel(parent, "RightGrip",
            new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f),
            new Vector2(-10f, 0f), new Vector2(75f, -20f), new Color(0.12f, 0.16f, 0.22f, 1f));
        rightBar.GetComponent<Image>().sprite = GetRoundedRectSprite(75, 880, 14f, new Color(0.12f, 0.16f, 0.22f, 1f), new Color(0.04f, 0.06f, 0.08f, 0.9f), 2f);

        // Botão [X] STATUS OFF (Como no canto superior direito da screenshot!)
        GameObject statusBox = CreateUIPanel(rightBar.transform, "StatusToggleBox",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -25f), new Vector2(65f, 75f), new Color(0.08f, 0.12f, 0.18f));
        statusBox.GetComponent<Image>().sprite = GetRoundedRectSprite(65, 75, 8f, new Color(0.08f, 0.12f, 0.18f), new Color(0.0f, 0.85f, 1.0f, 0.7f), 1.5f);

        CreateUIText(statusBox.transform, "KeyTxt", "✕", 16, FontStyle.Bold,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -6f), new Vector2(40f, 20f), new Color(1f, 0.9f, 0.2f), TextAnchor.MiddleCenter);

        CreateUIText(statusBox.transform, "LblTxt", "STATUS", 9, FontStyle.Bold,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -3f), new Vector2(60f, 18f), Color.white, TextAnchor.MiddleCenter);

        statusToggleText = CreateUIText(statusBox.transform, "StateTxt", "OFF", 10, FontStyle.Bold,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 8f), new Vector2(40f, 18f), new Color(0.5f, 0.7f, 0.9f), TextAnchor.MiddleCenter);

        // Ranhuras horizontais de pegada (Grip texture)
        for (int i = 0; i < 8; i++)
        {
            CreateUIPanel(rightBar.transform, $"GripSlot_{i}",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -80f + (i * 24f)), new Vector2(48f, 6f), new Color(0.06f, 0.09f, 0.13f));
        }

        // Contador Digital Amarelo no canto inferior direito (ex: 006 / 015)
        GameObject counterBox = CreateUIPanel(rightBar.transform, "CounterBox",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 25f), new Vector2(65f, 40f), new Color(0.04f, 0.06f, 0.09f));
        counterBox.GetComponent<Image>().sprite = GetRoundedRectSprite(65, 40, 6f, new Color(0.04f, 0.06f, 0.09f), new Color(1f, 0.75f, 0.1f, 0.6f), 1f);

        counterText = CreateUIText(counterBox.transform, "CounterVal", "015", 16, FontStyle.Bold,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(1f, 0.85f, 0.15f), TextAnchor.MiddleCenter);
    }

    private void BuildTopCyanHeader(Transform parent)
    {
        // Barra Superior em degradê Azul Ciano Elétrico (Fiel ao 3DS!)
        GameObject topBar = CreateUIPanel(parent, "TopCyanBar",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -10f), new Vector2(1090f, 175f), new Color(0.0f, 0.48f, 0.88f, 1f));
        topBar.GetComponent<Image>().sprite = GetRoundedRectSprite(1090, 175, 16f, new Color(0.0f, 0.48f, 0.88f, 1f), new Color(0.0f, 0.95f, 1f, 0.8f), 2f);

        // --- SLOT ESQUERDO: PRINCIPAL (MAIN) ---
        GameObject mainSlotRoot = CreateUIPanel(topBar.transform, "MainSlot",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-150f, 10f), new Vector2(115f, 115f), new Color(0.02f, 0.15f, 0.30f, 0.95f));
        mainSlotRoot.GetComponent<Image>().sprite = GetRoundedRectSprite(115, 115, 16f, new Color(0.02f, 0.15f, 0.30f, 0.95f), new Color(0.0f, 0.90f, 1f, 1f), 3.5f);

        // Rótulo "PRINCIPAL" acima do slot esquerdo
        CreateUIText(topBar.transform, "MainLabel", "PRINCIPAL", 13, FontStyle.Bold,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-150f, 78f), new Vector2(100f, 24f), Color.white, TextAnchor.MiddleCenter);

        // Ícone Portrait do Main Appmon
        GameObject mainPortObj = CreateUIPanel(mainSlotRoot.transform, "Portrait",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(92f, 92f), Color.white);
        mainSlotPortrait = mainPortObj.GetComponent<Image>();

        // Badge Circular no canto superior esquerdo do Main Slot (como o 💬 do Gatchmon!)
        GameObject mainCatObj = CreateUIPanel(mainSlotRoot.transform, "CatBadge",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f),
            new Vector2(12f, -12f), new Vector2(30f, 30f), Color.white);
        mainCatObj.GetComponent<Image>().sprite = GetCircleSprite(30, new Color(0.0f, 0.70f, 1f), Color.white, 2f);
        mainSlotCatBadge = CreateUIText(mainCatObj.transform, "Txt", "💬", 14, FontStyle.Normal,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, Color.white, TextAnchor.MiddleCenter);

        mainSlotNameText = CreateUIText(mainSlotRoot.transform, "Name", "Main", 10, FontStyle.Bold,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 12f), new Vector2(110f, 18f), Color.white, TextAnchor.MiddleCenter);

        mainSlotRankText = CreateUIText(mainSlotRoot.transform, "Rank", "", 8, FontStyle.Normal,
            Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Color.clear, TextAnchor.MiddleCenter);

        // --- CONECTOR CENTRAL: OVAL "APP-LINK" ---
        GameObject connector = CreateUIPanel(topBar.transform, "ConnectorBadge",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 10f), new Vector2(130f, 42f), new Color(0.0f, 0.85f, 1.0f, 0.95f));
        connector.GetComponent<Image>().sprite = GetRoundedRectSprite(130, 42, 20f, new Color(0.0f, 0.85f, 1.0f, 0.95f), Color.white, 2f);

        CreateUIText(connector.transform, "Txt", "APP-LINK", 13, FontStyle.Bold,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0.02f, 0.15f, 0.35f), TextAnchor.MiddleCenter);

        // --- SLOT DIREITO: ALVO DA SETA (APPLINK PARTNER RECEPTACLE) ---
        GameObject targetSlotRoot = CreateUIPanel(topBar.transform, "TargetSlot",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(150f, 10f), new Vector2(115f, 115f), new Color(0.01f, 0.10f, 0.22f, 0.95f));
        targetSlotBorder = targetSlotRoot.GetComponent<Image>();
        targetSlotBorder.sprite = GetRoundedRectSprite(115, 115, 16f, new Color(0.01f, 0.10f, 0.22f, 0.95f), new Color(0.0f, 0.90f, 1f, 1f), 3.5f);

        // Rótulo superior do slot direito
        targetSlotRankText = CreateUIText(topBar.transform, "TargetLabel", "VÍNCULO", 13, FontStyle.Bold,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(150f, 78f), new Vector2(100f, 24f), Color.white, TextAnchor.MiddleCenter);

        // Caixa vazia inicial com contorno escuro brilhante (exatamente como na imagem!)
        targetEmptyBox = CreateUIPanel(targetSlotRoot.transform, "EmptyBox",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(95f, 95f), new Color(0.02f, 0.15f, 0.25f, 0.6f));
        targetEmptyBox.GetComponent<Image>().sprite = GetRoundedRectSprite(95, 95, 12f, new Color(0.02f, 0.15f, 0.25f, 0.6f), new Color(0.0f, 0.50f, 0.75f, 0.5f), 1.5f);

        // Ícone Portrait projetado
        GameObject targetPortObj = CreateUIPanel(targetSlotRoot.transform, "Portrait",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(92f, 92f), Color.white);
        targetSlotPortrait = targetPortObj.GetComponent<Image>();
        targetSlotPortrait.gameObject.SetActive(false);

        // Badge Circular no canto superior esquerdo do Target Slot
        GameObject targetCatObj = CreateUIPanel(targetSlotRoot.transform, "CatBadge",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f),
            new Vector2(12f, -12f), new Vector2(30f, 30f), Color.white);
        targetCatObj.GetComponent<Image>().sprite = GetCircleSprite(30, new Color(0.0f, 0.70f, 1f), Color.white, 2f);
        targetSlotCatBadge = CreateUIText(targetCatObj.transform, "Txt", "💬", 14, FontStyle.Normal,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, Color.white, TextAnchor.MiddleCenter);
        targetSlotCatBadge.gameObject.SetActive(false);

        // --- SUBTÍTULO: Selecione um Appmon para conectar ---
        instructionSubtitleText = CreateUIText(topBar.transform, "InstructionTxt", "Selecione um Appmon na grade abaixo para conectar", 14, FontStyle.Bold,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 8f), new Vector2(850f, 26f), Color.white, TextAnchor.MiddleCenter);
    }

    private void BuildMatrixGreenMonitor(Transform parent)
    {
        // Monitor Central Estilo Terminal CRT Verde (Como no 3DS!)
        GameObject monitorFrame = CreateUIPanel(parent, "GreenCRTMonitor",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -42f), new Vector2(1090f, 570f), new Color(0.04f, 0.38f, 0.16f, 1f));
        monitorFrame.GetComponent<Image>().sprite = GetRoundedRectSprite(1090, 570, 24f, new Color(0.04f, 0.38f, 0.16f, 1f), new Color(0.0f, 0.85f, 0.35f, 0.9f), 3f);

        // Fundo do Monitor com efeito Matrix Green & Malha Dotted
        GameObject screenInner = CreateUIPanel(monitorFrame.transform, "ScreenInner",
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(-12f, -12f), new Color(0.06f, 0.52f, 0.22f, 1f));
        screenInner.GetComponent<Image>().sprite = GetRoundedRectSprite(1078, 558, 20f, new Color(0.06f, 0.52f, 0.22f, 1f), new Color(0.20f, 0.85f, 0.40f, 0.5f), 1f);

        // --- GRADE 5 COLUNAS X 3 LINHAS = 15 CHIPS ---
        GameObject gridArea = CreateUIPanel(screenInner.transform, "ChipsGridArea",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 24f), new Vector2(1040f, 480f), Color.clear);

        slotUIList.Clear();

        float chipW = 188f;
        float chipH = 146f;
        float spacingX = 18f;
        float spacingY = 14f;

        float totalW = COLS * chipW + (COLS - 1) * spacingX;
        float totalH = ROWS * chipH + (ROWS - 1) * spacingY;
        float startX = -totalW * 0.5f + chipW * 0.5f;
        float startY = totalH * 0.5f - chipH * 0.5f;

        for (int i = 0; i < SLOTS_PER_PAGE; i++)
        {
            int r = i / COLS;
            int c = i % COLS;
            float posX = startX + c * (chipW + spacingX);
            float posY = startY - r * (chipH + spacingY);

            GameObject chipRoot = CreateUIPanel(gridArea.transform, $"Chip_{i}",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(posX, posY), new Vector2(chipW, chipH), new Color(0.06f, 0.10f, 0.16f, 0.95f));

            RectTransform chipRT = chipRoot.GetComponent<RectTransform>();

            // Moldura Colorida do Chip (3.5px)
            Image borderImg = chipRoot.GetComponent<Image>();
            borderImg.sprite = GetChipBorderSprite((int)chipW, (int)chipH, new Color(0.0f, 0.85f, 1.0f), 3.5f);

            // Badge Circular de Categoria no Canto Superior Esquerdo
            GameObject catBadgeObj = CreateUIPanel(chipRoot.transform, "CatBadge",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(18f, -18f), new Vector2(30f, 30f), Color.white);
            catBadgeObj.GetComponent<Image>().sprite = GetCircleSprite(30, new Color(0.0f, 0.70f, 1f), Color.white, 2f);

            Text catTxt = CreateUIText(catBadgeObj.transform, "CatTxt", "💬", 14, FontStyle.Normal,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero, Color.white, TextAnchor.MiddleCenter);

            // Imagem Portrait do Appmon
            GameObject portraitObj = CreateUIPanel(chipRoot.transform, "Portrait",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 8f), new Vector2(98f, 98f), Color.white);

            // Nome do Appmon na base do chip
            Text nameTxt = CreateUIText(chipRoot.transform, "Name", "Appmon", 13, FontStyle.Bold,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 6f), new Vector2(-10f, 22f), Color.white, TextAnchor.MiddleCenter);

            // Carimbo Diagonal Vermelho "PRINCIPAL" (Para o Appmon ativo em campo!)
            GameObject mainStamp = CreateUIPanel(chipRoot.transform, "MainStamp",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(130f, 38f), new Color(0.90f, 0.15f, 0.15f, 0.95f));
            mainStamp.GetComponent<Image>().sprite = GetRoundedRectSprite(130, 38, 8f, new Color(0.90f, 0.15f, 0.15f, 0.95f), Color.white, 2f);
            mainStamp.transform.localRotation = Quaternion.Euler(0f, 0f, -22f); // Inclinação diagonal

            CreateUIText(mainStamp.transform, "Txt", "PRINCIPAL", 13, FontStyle.Bold,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero, Color.white, TextAnchor.MiddleCenter);
            mainStamp.SetActive(false);

            // Badge [LINKADO] para outros Appmons da reserva já conectados
            GameObject linkedTag = CreateUIPanel(chipRoot.transform, "LinkedTag",
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero, new Color(0.02f, 0.02f, 0.02f, 0.70f));
            linkedTag.GetComponent<Image>().sprite = GetRoundedRectSprite((int)chipW, (int)chipH, 8f, new Color(0.02f, 0.02f, 0.02f, 0.70f), new Color(1f, 0.75f, 0.15f, 0.9f), 2.5f);

            Text linkedTxt = CreateUIText(linkedTag.transform, "Txt", "[LINKADO]", 15, FontStyle.Bold,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(150f, 30f), new Color(1f, 0.85f, 0.20f), TextAnchor.MiddleCenter);
            linkedTag.SetActive(false);

            // Cursor Dourado com Cantos Cyberpunk ┏ ┓ ┗ ┛
            GameObject cursorHighlight = CreateUIPanel(chipRoot.transform, "CursorHighlight",
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(12f, 12f), Color.white);
            cursorHighlight.GetComponent<Image>().sprite = GetCyberCornerBracketSprite((int)chipW + 12, (int)chipH + 12);
            cursorHighlight.SetActive(false);

            // Suporte a clique direto de mouse no chip
            int slotIdx = i;
            Button btn = chipRoot.AddComponent<Button>();
            btn.targetGraphic = borderImg;
            btn.onClick.AddListener(() =>
            {
                int targetDataIdx = (currentPage * SLOTS_PER_PAGE) + slotIdx;
                if (targetDataIdx < bagAppmons.Count)
                {
                    selectedIndex = targetDataIdx;
                    RefreshUI();
                }
            });

            slotUIList.Add(new BagSlotUI
            {
                index = i,
                root = chipRoot,
                rectTransform = chipRT,
                backgroundImage = chipRoot.GetComponent<Image>(),
                borderImage = borderImg,
                iconImage = portraitObj.GetComponent<Image>(),
                categoryBadgeImage = catBadgeObj.GetComponent<Image>(),
                categoryBadgeText = catTxt,
                nameText = nameTxt,
                mainStampObj = mainStamp,
                linkedTagObj = linkedTag,
                linkedTagText = linkedTxt,
                cursorHighlightObj = cursorHighlight
            });
        }
    }

    private void BuildBottomControls(Transform parent)
    {
        // Barra de Controles Inferior
        GameObject bottomBar = CreateUIPanel(parent, "BottomControls",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 18f), new Vector2(1090f, 70f), Color.clear);

        // --- BOTÃO CENTRAL CONFIRMAR (GLOSSY GREEN PILL - IGUAL AO 3DS!) ---
        confirmLinkButton = CreateButton(bottomBar.transform, "KetteiBtn", "", 0,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 0f), new Vector2(250f, 62f), () => ExecuteLinkAction());

        confirmLinkButton.GetComponent<Image>().sprite = GetGlossyPillSprite(250, 62, new Color(0.12f, 0.85f, 0.30f), new Color(0.04f, 0.55f, 0.18f), Color.white);

        // Legenda superior
        confirmButtonSubText = CreateUIText(confirmLinkButton.transform, "SubTxt", "CONFIRMAR", 11, FontStyle.Bold,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -8f), new Vector2(120f, 16f), Color.white, TextAnchor.MiddleCenter);

        // Texto central
        confirmButtonMainText = CreateUIText(confirmLinkButton.transform, "MainTxt", "VINCULAR LINK", 17, FontStyle.Bold,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -6f), new Vector2(220f, 32f), Color.white, TextAnchor.MiddleCenter);

        // Botão Desvincular à esquerda do botão de decisão
        unlinkButton = CreateButton(bottomBar.transform, "UnlinkBtn", "✕ DESVINCULAR [U]", 13,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-220f, 0f), new Vector2(170f, 44f), () => ExecuteUnlinkAction());
        unlinkButton.GetComponent<Image>().sprite = GetRoundedRectSprite(170, 44, 10f, new Color(0.65f, 0.15f, 0.15f, 0.95f), new Color(1f, 0.35f, 0.35f, 0.9f), 1.5f);
        unlinkButton.gameObject.SetActive(false);

        // Botão Voltar [X / ESC] no canto esquerdo
        backButton = CreateButton(bottomBar.transform, "BackBtn", "◀ VOLTAR [ESC]", 13,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(30f, 0f), new Vector2(150f, 44f), () => CloseScreen(didPerformAction: false));
        backButton.GetComponent<Image>().sprite = GetRoundedRectSprite(150, 44, 10f, new Color(0.10f, 0.15f, 0.22f, 0.95f), new Color(0.35f, 0.45f, 0.60f, 0.8f), 1.5f);

        // Paginação ◀ [Q]  1/2  [E] ▶ no canto direito
        prevPageBtn = CreateButton(bottomBar.transform, "PrevPageBtn", "◀ [Q]", 12,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-180f, 0f), new Vector2(60f, 38f), () => PrevPage());
        prevPageBtn.GetComponent<Image>().sprite = GetRoundedRectSprite(60, 38, 8f, new Color(0.12f, 0.20f, 0.30f), Color.cyan, 1f);

        pageIndicatorText = CreateUIText(bottomBar.transform, "PageIndicator", "1/2", 14, FontStyle.Bold,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-105f, 0f), new Vector2(70f, 28f), Color.white, TextAnchor.MiddleCenter);

        nextPageBtn = CreateButton(bottomBar.transform, "NextPageBtn", "[E] ▶", 12,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-30f, 0f), new Vector2(60f, 38f), () => NextPage());
        nextPageBtn.GetComponent<Image>().sprite = GetRoundedRectSprite(60, 38, 8f, new Color(0.12f, 0.20f, 0.30f), Color.cyan, 1f);
    }

    // =========================================================================
    // UTILITÁRIOS GRÁFICOS & PROCEDURAIS
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

        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        cb.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        btn.colors = cb;

        if (onClick != null) btn.onClick.AddListener(() => onClick.Invoke());

        if (!string.IsNullOrEmpty(label))
        {
            CreateUIText(go.transform, "BtnLabel", label, fontSize, FontStyle.Bold,
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero, Color.white, TextAnchor.MiddleCenter);
        }

        return btn;
    }

    private Sprite GetAppmonSprite(AppmonData app)
    {
        if (app == null) return null;
        return SkillIconDatabase.GetSkillIcon(app.name, app.primaryCategory);
    }

    private Color GetCategoryBorderColor(FunctionalCategory cat)
    {
        return cat switch
        {
            FunctionalCategory.Security => new Color(0.85f, 0.20f, 0.80f),      // Magenta / Violeta (como no 3DS!)
            FunctionalCategory.System => new Color(0.95f, 0.20f, 0.20f),        // Vermelho Vivo
            FunctionalCategory.Tool => new Color(0.15f, 0.88f, 0.35f),          // Verde Lima
            FunctionalCategory.Entertainment => new Color(1.0f, 0.55f, 0.05f),  // Laranja
            FunctionalCategory.Life => new Color(0.20f, 0.95f, 0.45f),         // Verde Esmeralda
            FunctionalCategory.Social => new Color(0.0f, 0.80f, 1.0f),         // Azul Ciano (Gatchmon)
            FunctionalCategory.Game => new Color(0.25f, 0.55f, 1.0f),          // Azul Cobalto
            FunctionalCategory.Navi => new Color(0.10f, 0.90f, 0.65f),         // Menta
            _ => new Color(0.95f, 0.80f, 0.15f)                                // Amarelo Dourado
        };
    }

    private Color GetCategoryBadgeBgColor(FunctionalCategory cat)
    {
        return cat switch
        {
            FunctionalCategory.Social => new Color(0.0f, 0.70f, 1.0f),
            FunctionalCategory.Security => new Color(0.85f, 0.20f, 0.80f),
            FunctionalCategory.System => new Color(0.95f, 0.20f, 0.20f),
            FunctionalCategory.Tool => new Color(0.15f, 0.88f, 0.35f),
            FunctionalCategory.Entertainment => new Color(1.0f, 0.55f, 0.05f),
            _ => new Color(0.20f, 0.75f, 0.40f)
        };
    }

    private string GetCategorySymbol(FunctionalCategory cat)
    {
        return cat switch
        {
            FunctionalCategory.Social => "💬",
            FunctionalCategory.Security => "⛨",
            FunctionalCategory.System => "⚔",
            FunctionalCategory.Tool => "▲",
            FunctionalCategory.Entertainment => "★",
            FunctionalCategory.Life => "♥",
            FunctionalCategory.Game => "✦",
            FunctionalCategory.Navi => "➤",
            _ => "●"
        };
    }

    // Procedural Sprites Cache
    private static readonly Dictionary<string, Sprite> s_spriteCache = new Dictionary<string, Sprite>();

    private Sprite GetRoundedRectSprite(int width, int height, float cornerRadius, Color fillColor, Color borderColor, float borderThickness)
    {
        string key = $"Round_{width}_{height}_{cornerRadius}_{fillColor}_{borderColor}_{borderThickness}";
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
                    Color gradient = Color.Lerp(fillColor * 0.88f, fillColor * 1.12f, normY);
                    gradient.a = fillColor.a;
                    c = gradient;
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

    private Sprite GetChipBorderSprite(int width, int height, Color borderColor, float thickness)
    {
        string key = $"ChipBorder_{width}_{height}_{borderColor}_{thickness}";
        if (s_spriteCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[width * height];
        float halfW = width * 0.5f;
        float halfH = height * 0.5f;
        float r = 16f;

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
                else if (dist >= -thickness) c = borderColor;
                else
                {
                    // Interior do chip: escuro brilhante com reflexo sutil
                    float normY = (float)y / height;
                    c = Color.Lerp(new Color(0.04f, 0.08f, 0.14f, 0.95f), new Color(0.08f, 0.14f, 0.22f, 0.95f), normY);
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

    private Sprite GetGlossyPillSprite(int width, int height, Color topColor, Color botColor, Color highlightColor)
    {
        string key = $"GlossyPill_{width}_{height}_{topColor}_{botColor}";
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
                float dist = 0f;
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

                if (dist > 0.5f)
                {
                    pixels[y * width + x] = Color.clear;
                }
                else
                {
                    // Borda externa escura/neon
                    if (dist >= -2.0f)
                    {
                        pixels[y * width + x] = new Color(0.02f, 0.35f, 0.12f, 1f);
                    }
                    else
                    {
                        Color baseCol = Color.Lerp(botColor, topColor, normY);
                        // Brilho no topo (Gloss crescent)
                        if (normY > 0.55f && dist < -4f)
                        {
                            float gloss = (normY - 0.55f) / 0.45f;
                            baseCol = Color.Lerp(baseCol, Color.white, gloss * 0.55f);
                        }
                        pixels[y * width + x] = baseCol;
                    }
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

        int arm = Mathf.Min(24, Mathf.Min(width, height) / 3);
        int thick = 3;
        Color neonGold = new Color(1f, 0.88f, 0.15f, 1f);

        for (int t = 0; t < thick; t++)
        {
            for (int k = 0; k < arm; k++)
            {
                // Top-Left ┏
                pixels[(height - 1 - t) * width + k] = neonGold;
                pixels[(height - 1 - k) * width + t] = neonGold;

                // Top-Right ┓
                pixels[(height - 1 - t) * width + (width - 1 - k)] = neonGold;
                pixels[(height - 1 - k) * width + (width - 1 - t)] = neonGold;

                // Bottom-Left ┗
                pixels[t * width + k] = neonGold;
                pixels[k * width + t] = neonGold;

                // Bottom-Right ┛
                pixels[t * width + (width - 1 - k)] = neonGold;
                pixels[k * width + (width - 1 - t)] = neonGold;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        Sprite sp = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
        s_spriteCache[key] = sp;
        return sp;
    }
}
