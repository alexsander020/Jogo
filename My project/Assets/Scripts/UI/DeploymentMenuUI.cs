using System;
using System.Collections.Generic;
using TacticalBattle.Appmon;
using TacticalBattle.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Menu de Seleção e Posicionamento de Criaturas Pré-Batalha de altíssima qualidade visual (Padrão Digimon Survive / Tactical RPG AAA).
/// Apresenta:
/// - Chassi de smartphone futurista com vidro translúcido, notch com lente/speaker e status bar (5G, Wi-Fi, relógio digital, bateria).
/// - Banner de cabeçalho com indicador de slots de desdobramento (luzes quadradas neon) e contador dinâmico.
/// - Barra de abas de filtro rápido de nível ([TODOS], [LV 10-19], [LV 20-29], [LV 30-39], [LV 40-50]) com destaque ativo iluminado.
/// - Botão de ordenação e dicas de equipe.
/// - Grid 5x4 com 20 slots de cards arredondados, miniaturas oficiais dos 30 Appmons, badges de rank/raridade, badges de nível,
///   cursor dourado com cantos angulares cibernéticos pulsantes e overlay esmeralda com checkmark analítico.
/// - Slots vazios com acabamento holográfico translúcido (ghost frames).
/// - Card de detalhes inferior completo: grande retrato em moldura de vidro com losango da categoria, barras de HP e SP com brilho especular,
///   painel de estatísticas completas (ATK, DEF, INT, SPI, SPD, CRT, MOV, Protocolo) e caixa de habilidade passiva em destaque neon.
/// - Barra inferior de comandos com badges de atalhos e botão "INICIAR BATALHA ⚔" incandescente.
/// </summary>
public class DeploymentMenuUI : MonoBehaviour
{
    public static DeploymentMenuUI Instance;

    public Canvas canvas;
    public Font menuFont;

    [Header("Configurações de Seleção")]
    public int maxDeployable = 4;
    public List<AppmonData> allAvailableAppmons = new List<AppmonData>();
    public List<AppmonData> displayedAppmons = new List<AppmonData>();
    public List<AppmonData> selectedAppmons = new List<AppmonData>();

    // Callbacks de Eventos
    public Action<AppmonData> OnUnitToggled;
    public Action OnBattleStartRequested;
    public Action<AppmonData> OnUnitHovered;

    // Grid e Paginação
    private const int COLS = 5;
    private const int ROWS = 4;
    private const int PAGE_SIZE = COLS * ROWS;
    private int currentPage = 0;
    private int cursorX = 0;
    private int cursorY = 0;

    // Ordenação e Filtragem de Nível
    public enum SortCriteria { Level, Rank, Name, Category }
    private SortCriteria currentSort = SortCriteria.Level;

    public enum LevelFilterRange { All, Lv10_19, Lv20_29, Lv30_39, Lv40_Plus }
    private LevelFilterRange currentLevelFilter = LevelFilterRange.All;

    // Elementos da UI
    private GameObject rootPhoneContainer;
    private Image phoneChassisImage;
    private Text counterText;
    private Image[] deploySlotLights;
    private Text instructionText;
    private Text sortCriteriaText;
    private Text pageIndicatorText;
    private Text clockText;

    // Barra de Abas de Filtro de Nível
    private LevelTabUI[] filterTabs = new LevelTabUI[5];

    // Slots da Grid 5x4
    private UnitSlotUI[,] gridSlots = new UnitSlotUI[COLS, ROWS];

    // Card Inferior de Detalhes Completo
    private Text detailNameText;
    private Text detailLevelText;
    private Text detailStageText;
    private Text detailMobilityText;
    private Text detailCategoryText;
    private Text detailStatsGridText;
    private Text detailPassiveText;
    private Image detailAvatarImage;
    private Image detailCatDiamondImage;
    private RectTransform detailHpBarFill;
    private Text detailHpValText;
    private RectTransform detailSpBarFill;
    private Text detailSpValText;

    // Barra Inferior de Comandos
    private GameObject bottomPromptBar;
    private Button startBattleButton;
    private Text startBattleButtonText;
    private Image startBattleButtonGlow;

    // Elementos de Animação
    private float pulseTimer = 0f;

    // Paleta de Cores Cyber Digimon Survive
    private readonly Color colCyanNeon = new Color(0.0f, 0.95f, 1.0f, 1.0f);
    private readonly Color colAmberGold = new Color(1.0f, 0.88f, 0.15f, 1.0f);
    private readonly Color colEmeraldGreen = new Color(0.12f, 0.98f, 0.45f, 1.0f);
    private readonly Color colCrimsonRed = new Color(1.0f, 0.28f, 0.35f, 1.0f);
    private readonly Color colDarkBg = new Color(0.04f, 0.07f, 0.12f, 0.98f);
    private readonly Color colSlotCardBg = new Color(0.07f, 0.11f, 0.17f, 0.94f);
    private readonly Color colSlotBorderNormal = new Color(0.20f, 0.32f, 0.48f, 0.70f);

    // Mapeamento de Ícones para os 30 Appmons
    private static readonly Dictionary<string, string> appmonIconMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "Data-Viper", "UI_Skill_Icon_Recon" },
        { "Glitch-Hound", "UI_Skill_Icon_Claw" },
        { "Sound-Beat", "skill_042" },
        { "Craft-Craft", "UI_Skill_Icon_Pound" },
        { "Shitakumon", "UI_Skill_Icon_Slide" },
        { "Flame-Log", "skill_007" },
        { "Volt-Plug", "skill_021" },
        { "Shadow-Cam", "skill_033" },
        { "Bio-Patch", "UI_Skill_Icon_Heal" },
        { "Magnet-Core", "skill_025" },
        { "Hydro-Vipermon", "UI_Skill_Icon_Beam" },
        { "Sonic-Debugger", "UI_Skill_Icon_Dash" },
        { "Architectmon", "UI_Skill_Icon_Reflect" },
        { "Magma-Logmon", "skill_008" },
        { "Electro-Cammon", "skill_022" },
        { "Bio-Magnetmon", "skill_050" },
        { "Poseidon-Vipermon", "skill_012" },
        { "Omega-Debugger", "UI_Skill_Icon_Slash" },
        { "Dreadnoughtmon", "UI_Skill_Icon_Blackhole" },
        { "Genbu-Architectmon", "UI_Skill_Icon_Reflect" },
        { "Seiryu-Vipermon", "skill_011" },
        { "Suzaku-Beatmon", "skill_009" },
        { "Byakko-Houndmon", "UI_Skill_Icon_Claw" },
        { "Lucifermon", "skill_031" },
        { "Beelzebumon", "skill_014" },
        { "Mammonmon", "skill_028" },
        { "Belphemon", "skill_030" },
        { "Satanmon", "skill_018" },
        { "Leviathanmon", "skill_013" },
        { "Asmodeusmon", "skill_036" }
    };

    // Cache de Sprites Procedurais
    private static Dictionary<string, Sprite> s_spriteCache = new Dictionary<string, Sprite>();

    void Awake()
    {
        Instance = this;
        menuFont = GetBestFont();
    }

    private Font GetBestFont()
    {
        if (menuFont != null) return menuFont;
        menuFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (menuFont == null) menuFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (menuFont == null) menuFont = Font.CreateDynamicFontFromOSFont("Segoe UI", 16);
        if (menuFont == null) menuFont = Font.CreateDynamicFontFromOSFont("Arial", 16);
        if (menuFont == null)
        {
            var anyText = FindFirstObjectByType<Text>();
            if (anyText != null && anyText.font != null) menuFont = anyText.font;
        }
        return menuFont;
    }

    public void Initialize(int maxDeploy, List<string> preselectedNames = null)
    {
        this.maxDeployable = maxDeploy;

        // Carrega todos os Appmon cadastrados
        allAvailableAppmons = new List<AppmonData>(AppmonDatabase.GetAll());
        selectedAppmons.Clear();

        currentLevelFilter = LevelFilterRange.All;
        currentSort = SortCriteria.Level;
        currentPage = 0;
        cursorX = 0;
        cursorY = 0;

        ApplySortingAndFiltering();

        // Nenhuma criatura é selecionada automaticamente se preselectedNames for nulo
        if (preselectedNames != null && preselectedNames.Count > 0)
        {
            foreach (var name in preselectedNames)
            {
                var app = allAvailableAppmons.Find(a => a.name.Equals(name, StringComparison.OrdinalIgnoreCase) || a.id.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (app != null && !selectedAppmons.Contains(app))
                {
                    selectedAppmons.Add(app);
                }
            }
        }

        EnsureCanvas();
        BuildUI();
        RefreshUI();
    }

    private void EnsureCanvas()
    {
        if (canvas == null)
        {
            GameObject cObj = new GameObject("DeploymentCanvas");
            cObj.layer = 5;
            canvas = cObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;

            CanvasScaler scaler = cObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            scaler.dynamicPixelsPerUnit = 2.5f; // Super-amostragem nítida de fontes (Retina / High-DPI)

            cObj.AddComponent<GraphicRaycaster>();
        }

        // Garante EventSystem na cena para cliques e hover
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }
    }

    public void Show()
    {
        if (canvas != null && canvas.gameObject != null) canvas.gameObject.SetActive(true);
        if (rootPhoneContainer != null) rootPhoneContainer.SetActive(true);
        if (bottomPromptBar != null) bottomPromptBar.SetActive(true);
        RefreshUI();
    }

    public void Hide()
    {
        if (canvas != null && canvas.gameObject != null) canvas.gameObject.SetActive(false);
        if (rootPhoneContainer != null) rootPhoneContainer.SetActive(false);
        if (bottomPromptBar != null) bottomPromptBar.SetActive(false);
    }

    void Update()
    {
        if (rootPhoneContainer == null || !rootPhoneContainer.activeSelf) return;

        pulseTimer += Time.deltaTime;

        HandleInput();
        UpdateMicroAnimations();
    }

    private void UpdateMicroAnimations()
    {
        // 1. Pulso suave na moldura do cursor focado
        float cursorPulse = 0.80f + 0.20f * Mathf.Sin(pulseTimer * 6.5f);
        if (gridSlots != null && cursorX >= 0 && cursorX < COLS && cursorY >= 0 && cursorY < ROWS)
        {
            var activeSlot = gridSlots[cursorX, cursorY];
            if (activeSlot != null && activeSlot.focusHighlightImg != null)
            {
                Color c = colAmberGold;
                c.a = cursorPulse;
                activeSlot.focusHighlightImg.color = c;
            }
        }

        // 2. Pulso incandescente no botão "Iniciar Batalha" quando pronto
        if (startBattleButtonGlow != null && selectedAppmons.Count > 0)
        {
            float btnPulse = 0.40f + 0.60f * Mathf.PingPong(pulseTimer * 2.5f, 1.0f);
            Color gc = colEmeraldGreen;
            gc.a = btnPulse;
            startBattleButtonGlow.color = gc;
        }

        // 3. Atualização de Relógio Digital
        if (clockText != null)
        {
            clockText.text = DateTime.Now.ToString("HH:mm");
        }
    }

    // =========================================================================
    // ENTRADA E CONTROLES
    // =========================================================================
    private void HandleInput()
    {
        int moveX = 0;
        int moveY = 0;
        bool confirm = false;
        bool switchPageLeft = false;
        bool switchPageRight = false;
        bool toggleSort = false;
        bool toggleFilter = false;
        bool startBattle = false;

#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        var gp = Gamepad.current;

        if (kb != null)
        {
            if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) moveX = -1;
            if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) moveX = 1;
            if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) moveY = -1;
            if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) moveY = 1;

            if (kb.spaceKey.wasPressedThisFrame || kb.zKey.wasPressedThisFrame) confirm = true;
            if (kb.qKey.wasPressedThisFrame) switchPageLeft = true;
            if (kb.eKey.wasPressedThisFrame) switchPageRight = true;
            if (kb.tabKey.wasPressedThisFrame || kb.cKey.wasPressedThisFrame) toggleSort = true;
            if (kb.fKey.wasPressedThisFrame) toggleFilter = true;
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) startBattle = true;

            // Atalhos numéricos 1 a 5 para abas de filtro
            if (kb.digit1Key.wasPressedThisFrame) SetLevelFilter(LevelFilterRange.All);
            if (kb.digit2Key.wasPressedThisFrame) SetLevelFilter(LevelFilterRange.Lv10_19);
            if (kb.digit3Key.wasPressedThisFrame) SetLevelFilter(LevelFilterRange.Lv20_29);
            if (kb.digit4Key.wasPressedThisFrame) SetLevelFilter(LevelFilterRange.Lv30_39);
            if (kb.digit5Key.wasPressedThisFrame) SetLevelFilter(LevelFilterRange.Lv40_Plus);
        }

        if (gp != null)
        {
            if (gp.dpad.left.wasPressedThisFrame || gp.leftStick.left.wasPressedThisFrame) moveX = -1;
            if (gp.dpad.right.wasPressedThisFrame || gp.leftStick.right.wasPressedThisFrame) moveX = 1;
            if (gp.dpad.up.wasPressedThisFrame || gp.leftStick.up.wasPressedThisFrame) moveY = -1;
            if (gp.dpad.down.wasPressedThisFrame || gp.leftStick.down.wasPressedThisFrame) moveY = 1;

            if (gp.buttonSouth.wasPressedThisFrame) confirm = true;
            if (gp.leftShoulder.wasPressedThisFrame) switchPageLeft = true;
            if (gp.rightShoulder.wasPressedThisFrame) switchPageRight = true;
            if (gp.buttonNorth.wasPressedThisFrame) toggleSort = true;
            if (gp.buttonWest.wasPressedThisFrame) toggleFilter = true;
            if (gp.startButton.wasPressedThisFrame) startBattle = true;
        }
#endif

        if (moveX != 0 || moveY != 0)
        {
            cursorX = Mathf.Clamp(cursorX + moveX, 0, COLS - 1);
            cursorY = Mathf.Clamp(cursorY + moveY, 0, ROWS - 1);
            RefreshUI();
            NotifyHoveredAppmon();
        }

        if (switchPageLeft) PreviousPage();
        if (switchPageRight) NextPage();
        if (toggleSort) CycleSort();
        if (toggleFilter) CycleLevelFilter();

        if (confirm) ToggleCurrentAppmon();
        if (startBattle) TryStartBattle();
    }

    public void OnSlotHovered(int col, int row)
    {
        if (cursorX != col || cursorY != row)
        {
            cursorX = col;
            cursorY = row;
            RefreshUI();
            NotifyHoveredAppmon();
        }
    }

    public void OnSlotClicked(int col, int row)
    {
        cursorX = col;
        cursorY = row;
        ToggleCurrentAppmon();
    }

    private void NotifyHoveredAppmon()
    {
        int index = (currentPage * PAGE_SIZE) + (cursorY * COLS) + cursorX;
        if (index >= 0 && index < displayedAppmons.Count)
        {
            OnUnitHovered?.Invoke(displayedAppmons[index]);
        }
    }

    public void NextPage()
    {
        int maxPages = Mathf.Max(1, Mathf.CeilToInt((float)displayedAppmons.Count / PAGE_SIZE));
        if (currentPage < maxPages - 1)
        {
            currentPage++;
            RefreshUI();
            NotifyHoveredAppmon();
        }
    }

    public void PreviousPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            RefreshUI();
            NotifyHoveredAppmon();
        }
    }

    public void CycleSort()
    {
        currentSort = (SortCriteria)(((int)currentSort + 1) % 4);
        ApplySortingAndFiltering();
        RefreshUI();
        NotifyHoveredAppmon();
    }

    public void CycleLevelFilter()
    {
        currentLevelFilter = (LevelFilterRange)(((int)currentLevelFilter + 1) % 5);
        currentPage = 0;
        cursorX = 0;
        cursorY = 0;
        ApplySortingAndFiltering();
        RefreshUI();
        NotifyHoveredAppmon();
    }

    public void SetLevelFilter(LevelFilterRange filter)
    {
        if (currentLevelFilter != filter)
        {
            currentLevelFilter = filter;
            currentPage = 0;
            cursorX = 0;
            cursorY = 0;
            ApplySortingAndFiltering();
            RefreshUI();
            NotifyHoveredAppmon();
        }
    }

    private void ApplySortingAndFiltering()
    {
        // 1. Filtragem por Nível
        displayedAppmons.Clear();
        foreach (var app in allAvailableAppmons)
        {
            int lvl = Mathf.Clamp(app.spd / 3, 10, 50);
            bool match = currentLevelFilter switch
            {
                LevelFilterRange.Lv10_19 => (lvl >= 10 && lvl <= 19),
                LevelFilterRange.Lv20_29 => (lvl >= 20 && lvl <= 29),
                LevelFilterRange.Lv30_39 => (lvl >= 30 && lvl <= 39),
                LevelFilterRange.Lv40_Plus => (lvl >= 40),
                _ => true
            };
            if (match) displayedAppmons.Add(app);
        }

        // 2. Ordenação
        switch (currentSort)
        {
            case SortCriteria.Level:
                displayedAppmons.Sort((a, b) => b.spd.CompareTo(a.spd));
                break;
            case SortCriteria.Rank:
                displayedAppmons.Sort((a, b) => b.rank.CompareTo(a.rank));
                break;
            case SortCriteria.Name:
                displayedAppmons.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
                break;
            case SortCriteria.Category:
                displayedAppmons.Sort((a, b) => a.primaryCategory.CompareTo(b.primaryCategory));
                break;
        }

        // 3. Atualiza os textos de ordenação
        if (sortCriteriaText != null)
        {
            sortCriteriaText.text = $"{currentSort} ▾";
        }

        // 4. Atualiza as abas de filtro de nível
        UpdateTabHighlights();
    }

    private void UpdateTabHighlights()
    {
        if (filterTabs == null) return;

        for (int i = 0; i < filterTabs.Length; i++)
        {
            var tab = filterTabs[i];
            if (tab == null) continue;

            bool isActive = (tab.filterRange == currentLevelFilter);
            if (isActive)
            {
                tab.bgImage.sprite = GetRoundedRectSprite((int)tab.size.x, (int)tab.size.y, 8f, new Color(0.0f, 0.45f, 0.65f, 0.95f), colCyanNeon, 2f);
                tab.titleText.color = Color.white;
            }
            else
            {
                tab.bgImage.sprite = GetRoundedRectSprite((int)tab.size.x, (int)tab.size.y, 8f, new Color(0.08f, 0.12f, 0.18f, 0.85f), new Color(0.20f, 0.32f, 0.45f, 0.60f), 1f);
                tab.titleText.color = new Color(0.70f, 0.82f, 0.95f, 0.85f);
            }
        }
    }

    public void ToggleCurrentAppmon()
    {
        int index = (currentPage * PAGE_SIZE) + (cursorY * COLS) + cursorX;
        if (index >= 0 && index < displayedAppmons.Count)
        {
            ToggleAppmon(displayedAppmons[index]);
        }
    }

    public void ToggleAppmon(AppmonData appmon)
    {
        if (appmon == null) return;

        if (selectedAppmons.Contains(appmon))
        {
            selectedAppmons.Remove(appmon);
            OnUnitToggled?.Invoke(appmon);
        }
        else
        {
            if (selectedAppmons.Count < maxDeployable)
            {
                selectedAppmons.Add(appmon);
                OnUnitToggled?.Invoke(appmon);
            }
            else
            {
                Debug.Log($"[Deploy] Limite máximo de {maxDeployable} criaturas atingido!");
            }
        }

        RefreshUI();
    }

    public void TryStartBattle()
    {
        if (selectedAppmons.Count > 0)
        {
            OnBattleStartRequested?.Invoke();
        }
        else
        {
            Debug.LogWarning("[Deploy] Escolha pelo menos 1 criatura para iniciar a batalha!");
        }
    }

    // =========================================================================
    // CONSTRUÇÃO PROCEDURAL DA INTERFACE
    // =========================================================================
    private void BuildUI()
    {
        if (rootPhoneContainer != null) Destroy(rootPhoneContainer);
        if (bottomPromptBar != null) Destroy(bottomPromptBar);

        // --- 1. MOLDURA DO SMARTPHONE (Lado Esquerdo da Tela) ---
        // 660px de largura x 970px de altura posicionado a 40px acima da base para NUNCA encostar na barra inferior!
        rootPhoneContainer = CreateUIPanel(canvas.transform, "Smartphone_Root",
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(45f, 40f), new Vector2(660f, 970f), Color.white);

        phoneChassisImage = rootPhoneContainer.GetComponent<Image>();
        phoneChassisImage.sprite = GetPhoneChassisSprite();

        // Tela interna com acabamento escuro translúcido
        GameObject screen = CreateUIPanel(rootPhoneContainer.transform, "PhoneScreen",
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(-26f, -32f), Color.clear);

        // --- 2. BARRA DE STATUS SUPERIOR (Sinal 5G, Câmera Notch, Relógio, Bateria 95%) ---
        BuildPhoneStatusBar(screen.transform);

        // --- 3. CABEÇALHO "AVAILABLE UNITS", SLOTS INDICADORES E CONTADOR ---
        BuildHeaderBar(screen.transform);

        // --- 4. BARRA DE ABAS DE FILTRO DE NÍVEL (TODOS, 10-19, 20-29, 30-39, 40-50) ---
        BuildLevelFilterTabBar(screen.transform);

        // --- 5. SUB-CABEÇALHO: ORDENAÇÃO E DICA DINÂMICA ---
        BuildSubHeaderBar(screen.transform);

        // --- 6. GRID 5x4 DE APPMONS COM NAVEGAÇÃO ---
        BuildUnitGrid(screen.transform);

        // --- 7. CARD INFERIOR DE DETALHES COMPLETO DO APPMON FOCADO ---
        BuildDetailCard(screen.transform);

        // --- 8. BARRA DE COMANDOS DA BASE DA TELA ---
        BuildBottomControlsBar(canvas.transform);
    }

    private void BuildPhoneStatusBar(Transform parent)
    {
        GameObject bar = CreateUIPanel(parent, "StatusBar",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -6f), new Vector2(-28f, 26f), Color.clear);

        // Notch / Câmera Central
        GameObject notch = CreateUIPanel(bar.transform, "CameraNotch",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(110f, 14f), new Color(0.02f, 0.03f, 0.06f, 0.98f));
        notch.GetComponent<Image>().sprite = GetRoundedRectSprite(110, 14, 6f, new Color(0.02f, 0.03f, 0.06f, 0.98f), new Color(0.15f, 0.22f, 0.32f, 0.6f), 1f);

        // Lente da câmera
        GameObject lens = CreateUIPanel(notch.transform, "Lens",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-26f, 0f), new Vector2(8f, 8f), Color.white);
        lens.GetComponent<Image>().sprite = GetCircleSprite(8, new Color(0.12f, 0.35f, 0.55f, 0.95f), new Color(0.0f, 0.8f, 1.0f, 0.8f), 1f);

        // Alto-falante fino
        GameObject speaker = CreateUIPanel(notch.transform, "Speaker",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(14f, 0f), new Vector2(42f, 3f), new Color(0.20f, 0.28f, 0.38f, 0.8f));

        // Sinal 5G e Wi-Fi (Esquerda)
        CreateUIText(bar.transform, "NetworkSignal", "📶 5G  🛜", 12, FontStyle.Bold,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(8f, 0f), new Vector2(90f, 22f), new Color(0.85f, 0.94f, 1.0f, 0.90f), TextAnchor.MiddleLeft);

        // Relógio Digital (Centro-Direita)
        clockText = CreateUIText(bar.transform, "DigitalClock", DateTime.Now.ToString("HH:mm"), 12, FontStyle.Bold,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-120f, 0f), new Vector2(60f, 22f), new Color(0.85f, 0.94f, 1.0f, 0.90f), TextAnchor.MiddleCenter);

        // Bateria 95% (Direita)
        CreateUIText(bar.transform, "Battery", "95% 🗲 [====]", 12, FontStyle.Bold,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-8f, 0f), new Vector2(110f, 22f), new Color(0.85f, 0.94f, 1.0f, 0.90f), TextAnchor.MiddleRight);
    }

    private void BuildHeaderBar(Transform parent)
    {
        GameObject header = CreateUIPanel(parent, "HeaderBar",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -36f), new Vector2(-20f, 54f), Color.white);

        header.GetComponent<Image>().sprite = GetRoundedRectSprite(610, 54, 12f, new Color(0.08f, 0.12f, 0.18f, 0.95f), new Color(0.0f, 0.85f, 1.0f, 0.55f), 1.5f);

        // Título "AVAILABLE UNITS"
        CreateUIText(header.transform, "Title", "◆  AVAILABLE UNITS", 22, FontStyle.Bold,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(18f, 0f), new Vector2(250f, 34f), Color.white, TextAnchor.MiddleLeft);

        // Indicadores Visuais de Slots de Unidade [ ■ ■ □ □ ]
        deploySlotLights = new Image[4];
        float lightStartX = 290f;
        for (int i = 0; i < 4; i++)
        {
            GameObject lightObj = CreateUIPanel(header.transform, $"SlotLight_{i}",
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(lightStartX + (i * 22f), 0f), new Vector2(16f, 16f), Color.white);
            deploySlotLights[i] = lightObj.GetComponent<Image>();
            deploySlotLights[i].sprite = GetRoundedRectSprite(16, 16, 4f, new Color(0.15f, 0.22f, 0.30f, 0.8f), new Color(0.3f, 0.4f, 0.5f, 0.5f), 1f);
        }

        // Contador numérico estilizado [ 0 / 4 ]
        counterText = CreateUIText(header.transform, "Counter", "0 / 4", 28, FontStyle.Bold,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-18f, 0f), new Vector2(110f, 38f), colCyanNeon, TextAnchor.MiddleRight);
    }

    private void BuildLevelFilterTabBar(Transform parent)
    {
        GameObject tabStrip = CreateUIPanel(parent, "LevelFilterTabStrip",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -96f), new Vector2(-20f, 36f), Color.clear);

        string[] tabNames = { "TODOS", "LV 10-19", "LV 20-29", "LV 30-39", "LV 40-50" };
        LevelFilterRange[] ranges = {
            LevelFilterRange.All,
            LevelFilterRange.Lv10_19,
            LevelFilterRange.Lv20_29,
            LevelFilterRange.Lv30_39,
            LevelFilterRange.Lv40_Plus
        };

        float tabW = 116f;
        float spacing = 6f;
        float totalW = (tabW * 5) + (spacing * 4);
        float startX = -(totalW * 0.5f) + (tabW * 0.5f);

        for (int i = 0; i < 5; i++)
        {
            int idx = i;
            GameObject tabObj = CreateUIPanel(tabStrip.transform, $"Tab_{tabNames[i]}",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(startX + (i * (tabW + spacing)), 0f), new Vector2(tabW, 34f), Color.white);

            LevelTabUI tabUI = new LevelTabUI();
            tabUI.filterRange = ranges[i];
            tabUI.size = new Vector2(tabW, 34f);
            tabUI.bgImage = tabObj.GetComponent<Image>();
            tabUI.bgImage.sprite = GetRoundedRectSprite((int)tabW, 34, 8f, new Color(0.08f, 0.12f, 0.18f, 0.85f), new Color(0.20f, 0.32f, 0.45f, 0.60f), 1f);

            tabUI.titleText = CreateUIText(tabObj.transform, "TabLabel", tabNames[i], 13, FontStyle.Bold,
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero, Color.white, TextAnchor.MiddleCenter);

            Button btn = tabObj.AddComponent<Button>();
            btn.onClick.AddListener(() => SetLevelFilter(ranges[idx]));

            filterTabs[i] = tabUI;
        }

        UpdateTabHighlights();
    }

    private void BuildSubHeaderBar(Transform parent)
    {
        GameObject subHeader = CreateUIPanel(parent, "SubHeaderBar",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -138f), new Vector2(-20f, 32f), Color.clear);

        // Botão de Ordenação (Sort Capsule)
        GameObject sortBox = CreateUIPanel(subHeader.transform, "SortBox",
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(0f, 0f), new Vector2(135f, 30f), Color.white);

        sortBox.GetComponent<Image>().sprite = GetRoundedRectSprite(135, 30, 15f, new Color(0.12f, 0.18f, 0.28f, 0.95f), new Color(0.0f, 0.85f, 1.0f, 0.75f), 1.5f);

        sortCriteriaText = CreateUIText(sortBox.transform, "SortText", "Level ▾", 13, FontStyle.Bold,
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, Color.white, TextAnchor.MiddleCenter);

        Button sortBtn = sortBox.AddComponent<Button>();
        sortBtn.onClick.AddListener(CycleSort);

        // Dica de instrução dinâmica (Direita)
        instructionText = CreateUIText(subHeader.transform, "InstructionText", "Select 4 more.", 14, FontStyle.Bold,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-4f, 0f), new Vector2(300f, 30f), colAmberGold, TextAnchor.MiddleRight);
    }

    private void BuildUnitGrid(Transform parent)
    {
        GameObject gridArea = CreateUIPanel(parent, "GridArea",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -175f), new Vector2(590f, 420f), Color.clear);

        // Botões laterais [LB] e [RB]
        GameObject btnLB = CreateUIPanel(parent, "Btn_LB",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f),
            new Vector2(14f, -385f), new Vector2(24f, 52f), Color.white);
        btnLB.GetComponent<Image>().sprite = GetRoundedRectSprite(24, 52, 6f, new Color(0.10f, 0.15f, 0.22f, 0.90f), new Color(0.35f, 0.48f, 0.65f, 0.8f), 1f);
        CreateUIText(btnLB.transform, "LblLB", "LB", 11, FontStyle.Bold,
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, Color.white, TextAnchor.MiddleCenter);
        btnLB.AddComponent<Button>().onClick.AddListener(PreviousPage);

        GameObject btnRB = CreateUIPanel(parent, "Btn_RB",
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            new Vector2(-14f, -385f), new Vector2(24f, 52f), Color.white);
        btnRB.GetComponent<Image>().sprite = GetRoundedRectSprite(24, 52, 6f, new Color(0.10f, 0.15f, 0.22f, 0.90f), new Color(0.35f, 0.48f, 0.65f, 0.8f), 1f);
        CreateUIText(btnRB.transform, "LblRB", "RB", 11, FontStyle.Bold,
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, Color.white, TextAnchor.MiddleCenter);
        btnRB.AddComponent<Button>().onClick.AddListener(NextPage);

        // Configura dimensões da Grid 5x4 ampliadas
        float slotWidth = 106f;
        float slotHeight = 96f;
        float spacingX = 8f;
        float spacingY = 6f;

        float startX = -((COLS - 1) * (slotWidth + spacingX)) / 2f;
        float startY = 150f;

        for (int y = 0; y < ROWS; y++)
        {
            for (int x = 0; x < COLS; x++)
            {
                float posX = startX + (x * (slotWidth + spacingX));
                float posY = startY - (y * (slotHeight + spacingY));

                UnitSlotUI slot = BuildSlot(gridArea.transform, x, y, new Vector2(posX, posY), new Vector2(slotWidth, slotHeight));
                gridSlots[x, y] = slot;
            }
        }

        // Indicador de Página (Bolinhas ● ○)
        pageIndicatorText = CreateUIText(parent, "PageIndicator", "● ○", 16, FontStyle.Bold,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -602f), new Vector2(160f, 24f), colCyanNeon, TextAnchor.MiddleCenter);
    }

    private UnitSlotUI BuildSlot(Transform parent, int col, int row, Vector2 pos, Vector2 size)
    {
        UnitSlotUI slot = new UnitSlotUI();
        slot.col = col;
        slot.row = row;

        // Container do Card com cantos arredondados
        slot.root = CreateUIPanel(parent, $"Slot_{col}_{row}",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            pos, size, Color.white);
        slot.baseBorderImg = slot.root.GetComponent<Image>();
        slot.baseBorderImg.sprite = GetRoundedRectSprite((int)size.x, (int)size.y, 10f, colSlotCardBg, colSlotBorderNormal, 1.5f);

        // Moldura de Foco / Cursor pulsante com cantos angulares cibernéticos
        slot.focusHighlight = CreateUIPanel(slot.root.transform, "FocusGlow",
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(14f, 14f), Color.white);
        slot.focusHighlightImg = slot.focusHighlight.GetComponent<Image>();
        slot.focusHighlightImg.sprite = GetCyberCornerBracketSprite((int)size.x + 14, (int)size.y + 14);
        slot.focusHighlight.SetActive(false);

        // Faixa de Categoria no topo do card (accent line)
        GameObject topStripe = CreateUIPanel(slot.root.transform, "TopStripe",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -3f), new Vector2(-16f, 3f), colCyanNeon);
        slot.categoryStripeImg = topStripe.GetComponent<Image>();

        // Retrato / Ícone do Appmon (Centralizado)
        GameObject iconObj = CreateUIPanel(slot.root.transform, "AvatarIcon",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 4f), new Vector2(58f, 58f), Color.white);
        slot.avatarImage = iconObj.GetComponent<Image>();
        slot.avatarImage.preserveAspect = true;

        // Badge de Rank (Canto Superior Esquerdo)
        GameObject badgeObj = CreateUIPanel(slot.root.transform, "RankBadge",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(4f, -4f), new Vector2(24f, 18f), Color.white);
        badgeObj.GetComponent<Image>().sprite = GetRoundedRectSprite(24, 18, 5f, new Color(0.04f, 0.07f, 0.12f, 0.90f), colCyanNeon, 1f);
        slot.badgeText = CreateUIText(badgeObj.transform, "BadgeTxt", "★", 12, FontStyle.Bold,
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, colAmberGold, TextAnchor.MiddleCenter);

        // Etiqueta de Nível "LV 25" (Canto Inferior Direito)
        slot.levelText = CreateUIText(slot.root.transform, "LevelText", "LV 25", 13, FontStyle.Bold,
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-5f, 3f), new Vector2(54f, 18f), Color.white, TextAnchor.MiddleRight);

        // Nome curto no rodapé
        slot.nameText = CreateUIText(slot.root.transform, "ShortName", "Appmon", 11, FontStyle.Bold,
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(5f, 3f), new Vector2(60f, 18f), new Color(0.85f, 0.92f, 1.0f), TextAnchor.MiddleLeft);

        // Overlay do Checkmark Verde Vibrante ("✓") quando selecionado
        slot.checkmarkOverlay = CreateUIPanel(slot.root.transform, "CheckmarkOverlay",
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-4f, -4f), new Vector2(28f, 28f), Color.white);
        slot.checkmarkOverlay.GetComponent<Image>().sprite = GetCheckmarkBadgeSprite(28);
        slot.checkmarkOverlay.SetActive(false);

        // Interação de Mouse: Hover e Clique via Listener Dedicado
        var listener = slot.root.AddComponent<SlotPointerListener>();
        listener.col = col;
        listener.row = row;
        listener.menu = this;

        return slot;
    }

    private void BuildDetailCard(Transform parent)
    {
        // Card de Informações Detalhadas (Base do Smartphone - 600x280px)
        GameObject card = CreateUIPanel(parent, "DetailCard",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 16f), new Vector2(600f, 280f), Color.white);

        card.GetComponent<Image>().sprite = GetRoundedRectSprite(600, 280, 14f, new Color(0.04f, 0.08f, 0.14f, 0.98f), new Color(0.0f, 0.85f, 1.0f, 0.65f), 2f);

        // --- RETRATO DO APPMON (Esquerda - 140x205px) ---
        GameObject avatarBox = CreateUIPanel(card.transform, "AvatarFrame",
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(18f, 0f), new Vector2(140f, 210f), Color.white);
        avatarBox.GetComponent<Image>().sprite = GetRoundedRectSprite(140, 210, 12f, new Color(0.02f, 0.05f, 0.10f, 0.98f), colCyanNeon, 2f);

        GameObject innerAvatar = CreateUIPanel(avatarBox.transform, "AvatarImage",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(118f, 118f), Color.white);
        detailAvatarImage = innerAvatar.GetComponent<Image>();
        detailAvatarImage.preserveAspect = true;

        // Losango de Categoria sobreposto no retrato
        GameObject diamondObj = CreateUIPanel(avatarBox.transform, "CatDiamondOverlay",
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-8f, 8f), new Vector2(32f, 32f), Color.white);
        detailCatDiamondImage = diamondObj.GetComponent<Image>();
        detailCatDiamondImage.sprite = GetCategoryDiamondSprite(32, colCyanNeon);

        // --- DADOS TEXTUAIS (Direita - infoLeft = 175f) ---
        float infoLeft = 175f;

        // Linha 1: Nome e Nível (GRANDES, NÍTIDOS E COM ALTO CONTRASTE)
        detailNameText = CreateUIText(card.transform, "DetailName", "Data-Viper", 26, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(infoLeft, -14f), new Vector2(280f, 32f), Color.white, TextAnchor.MiddleLeft);

        detailLevelText = CreateUIText(card.transform, "DetailLevel", "LV 25", 22, FontStyle.Bold,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-20f, -14f), new Vector2(110f, 32f), colAmberGold, TextAnchor.MiddleRight);

        // Linha 2: Estágio, Categoria e Mobilidade
        detailStageText = CreateUIText(card.transform, "DetailStage", "Common / Nami", 14, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(infoLeft, -46f), new Vector2(180f, 20f), new Color(0.85f, 0.92f, 1.0f), TextAnchor.MiddleLeft);

        detailCategoryText = CreateUIText(card.transform, "DetailCategory", "[Security / Vacina]", 14, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(infoLeft + 185f, -46f), new Vector2(180f, 20f), colCyanNeon, TextAnchor.MiddleLeft);

        detailMobilityText = CreateUIText(card.transform, "DetailMobility", "MOV: 3", 14, FontStyle.Bold,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-20f, -46f), new Vector2(90f, 20f), colEmeraldGreen, TextAnchor.MiddleRight);

        // --- BARRA DE HP ---
        float barWidth = 240f;
        CreateUIText(card.transform, "HpLabel", "HP", 15, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(infoLeft, -74f), new Vector2(32f, 18f), colCyanNeon, TextAnchor.MiddleLeft);

        GameObject hpBg = CreateUIPanel(card.transform, "HpBg",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(infoLeft + 36f, -76f), new Vector2(barWidth, 15f), Color.white);
        hpBg.GetComponent<Image>().sprite = GetRoundedRectSprite((int)barWidth, 15, 5f, new Color(0.02f, 0.05f, 0.08f, 0.95f), new Color(0.2f, 0.35f, 0.5f, 0.6f), 1f);

        GameObject hpFill = CreateUIPanel(hpBg.transform, "HpFill",
            new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
            Vector2.zero, new Vector2(barWidth, 0f), Color.white);
        detailHpBarFill = hpFill.GetComponent<RectTransform>();
        hpFill.GetComponent<Image>().sprite = GetBarFillSprite((int)barWidth, 15, new Color(0.0f, 0.85f, 1.0f), new Color(0.1f, 1.0f, 0.6f));

        detailHpValText = CreateUIText(card.transform, "HpVal", "120 / 120", 14, FontStyle.Bold,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-20f, -74f), new Vector2(100f, 18f), Color.white, TextAnchor.MiddleRight);

        // --- BARRA DE SP / MP ---
        CreateUIText(card.transform, "SpLabel", "SP", 15, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(infoLeft, -98f), new Vector2(32f, 18f), colAmberGold, TextAnchor.MiddleLeft);

        GameObject spBg = CreateUIPanel(card.transform, "SpBg",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(infoLeft + 36f, -100f), new Vector2(barWidth, 15f), Color.white);
        spBg.GetComponent<Image>().sprite = GetRoundedRectSprite((int)barWidth, 15, 5f, new Color(0.02f, 0.05f, 0.08f, 0.95f), new Color(0.35f, 0.3f, 0.15f, 0.6f), 1f);

        GameObject spFill = CreateUIPanel(spBg.transform, "SpFill",
            new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
            Vector2.zero, new Vector2(barWidth, 0f), Color.white);
        detailSpBarFill = spFill.GetComponent<RectTransform>();
        spFill.GetComponent<Image>().sprite = GetBarFillSprite((int)barWidth, 15, new Color(1.0f, 0.65f, 0.0f), new Color(1.0f, 0.90f, 0.2f));

        detailSpValText = CreateUIText(card.transform, "SpVal", "60 / 60", 14, FontStyle.Bold,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-20f, -98f), new Vector2(100f, 18f), Color.white, TextAnchor.MiddleRight);

        // --- GRID DE ESTATÍSTICAS RPG COMPLETAS (ATK, DEF, INT, SPI, SPD, CRT) ---
        GameObject statsPill = CreateUIPanel(card.transform, "StatsPillBox",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
            new Vector2(infoLeft, -124f), new Vector2(-20f, 26f), Color.white);
        statsPill.GetComponent<Image>().sprite = GetRoundedRectSprite(400, 26, 6f, new Color(0.02f, 0.05f, 0.09f, 0.95f), new Color(0.18f, 0.28f, 0.42f, 0.70f), 1f);

        detailStatsGridText = CreateUIText(statsPill.transform, "StatsText", "ATK: 45  DEF: 55  INT: 40  SPI: 50  SPD: 52  CRT: 5%", 13, FontStyle.Bold,
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0.88f, 0.94f, 1.0f), TextAnchor.MiddleCenter);

        // --- HABILIDADE PASSIVA EXCLUSIVA (CONTAINER PRÓPRIO DE ALTO DESTAQUE) ---
        GameObject passBox = CreateUIPanel(card.transform, "PassiveBox",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
            new Vector2(infoLeft, -156f), new Vector2(-20f, 108f), Color.white);
        passBox.GetComponent<Image>().sprite = GetRoundedRectSprite(400, 108, 8f, new Color(0.02f, 0.04f, 0.08f, 0.90f), new Color(0.0f, 0.85f, 1.0f, 0.40f), 1f);

        detailPassiveText = CreateUIText(passBox.transform, "DetailPassive", "Passiva: [Cód. Defensivo] Reduz dano à distância em 15%.", 14, FontStyle.Normal,
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            new Vector2(12f, -8f), new Vector2(-24f, -16f), new Color(0.92f, 0.96f, 1.0f, 0.95f), TextAnchor.UpperLeft);
        detailPassiveText.horizontalOverflow = HorizontalWrapMode.Wrap;
        detailPassiveText.lineSpacing = 1.15f;
    }

    private void BuildBottomControlsBar(Transform parent)
    {
        bottomPromptBar = CreateUIPanel(parent, "DeploymentBottomBar",
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
            Vector2.zero, new Vector2(0f, 65f), Color.white);

        bottomPromptBar.GetComponent<Image>().sprite = GetRoundedRectSprite(1920, 65, 0f, new Color(0.04f, 0.07f, 0.12f, 0.98f), new Color(0.0f, 0.85f, 1.0f, 0.60f), 2f);

        // Dicas de Teclado e Controle (GRANDES E NÍTIDAS COM ATALHOS)
        string controlsText = " ✥ WASD: Navegar   |   [LB][RB] (Q/E): Página   |   [1-5 / F]: Filtrar Nível   |   (A) Espaço: Colocar/Retirar   |   (Y) Tab: Ordenar";
        CreateUIText(bottomPromptBar.transform, "ControlsHints", controlsText, 15, FontStyle.Bold,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(35f, 0f), new Vector2(1300f, 36f), new Color(0.90f, 0.95f, 1.0f), TextAnchor.MiddleLeft);

        // Botão "INICIAR BATALHA ⚔" com pulso neon
        GameObject btnObj = CreateUIPanel(bottomPromptBar.transform, "BtnStartBattle",
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-35f, 0f), new Vector2(300f, 48f), Color.white);

        btnObj.GetComponent<Image>().sprite = GetRoundedRectSprite(300, 48, 24f, new Color(0.08f, 0.50f, 0.28f, 0.98f), colEmeraldGreen, 2.5f);

        // Camada de brilho pulsante
        GameObject glowObj = CreateUIPanel(btnObj.transform, "BtnGlow",
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(12f, 12f), Color.white);
        startBattleButtonGlow = glowObj.GetComponent<Image>();
        startBattleButtonGlow.sprite = GetCursorGlowSprite(312, 60, 26f);

        startBattleButtonText = CreateUIText(btnObj.transform, "BtnTxt", "⚔  INICIAR BATALHA  ⚔", 16, FontStyle.Bold,
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, Color.white, TextAnchor.MiddleCenter);

        startBattleButton = btnObj.AddComponent<Button>();
        startBattleButton.onClick.AddListener(TryStartBattle);
    }

    // =========================================================================
    // ATUALIZAÇÃO E SINCRONIZAÇÃO DOS DADOS
    // =========================================================================
    public void RefreshUI()
    {
        // 1. Contador de Seleção e Luzes de Slot
        if (counterText != null)
        {
            counterText.text = $"{selectedAppmons.Count} / {maxDeployable}";
            counterText.color = selectedAppmons.Count >= maxDeployable ? colEmeraldGreen : colAmberGold;
        }

        if (deploySlotLights != null)
        {
            for (int i = 0; i < deploySlotLights.Length; i++)
            {
                if (deploySlotLights[i] == null) continue;
                bool isFilled = (i < selectedAppmons.Count);
                if (isFilled)
                {
                    deploySlotLights[i].sprite = GetRoundedRectSprite(16, 16, 4f, colEmeraldGreen, Color.white, 1f);
                }
                else
                {
                    deploySlotLights[i].sprite = GetRoundedRectSprite(16, 16, 4f, new Color(0.12f, 0.18f, 0.26f, 0.8f), new Color(0.25f, 0.35f, 0.45f, 0.5f), 1f);
                }
            }
        }

        // 2. Dica Dinâmica
        if (instructionText != null)
        {
            int remaining = maxDeployable - selectedAppmons.Count;
            if (remaining > 0)
            {
                instructionText.text = $"Select {remaining} more.";
                instructionText.color = colAmberGold;
            }
            else
            {
                instructionText.text = "Equipe Completa! ✓";
                instructionText.color = colEmeraldGreen;
            }
        }

        // 3. Indicador de Página
        int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)displayedAppmons.Count / PAGE_SIZE));
        if (pageIndicatorText != null)
        {
            string pStr = "";
            for (int i = 0; i < totalPages; i++)
            {
                pStr += (i == currentPage) ? "● " : "○ ";
            }
            pageIndicatorText.text = pStr.TrimEnd();
        }

        // 4. Renderização dos 20 Slots da Grid
        int pageOffset = currentPage * PAGE_SIZE;

        for (int y = 0; y < ROWS; y++)
        {
            for (int x = 0; x < COLS; x++)
            {
                UnitSlotUI slot = gridSlots[x, y];
                if (slot == null) continue;

                int appmonIndex = pageOffset + (y * COLS) + x;
                bool isHovered = (x == cursorX && y == cursorY);

                if (appmonIndex < displayedAppmons.Count)
                {
                    AppmonData app = displayedAppmons[appmonIndex];
                    slot.root.SetActive(true);

                    // Ícone oficial de SkillIconDatabase
                    Sprite iconSprite = GetAppmonSprite(app);
                    slot.avatarImage.sprite = iconSprite;
                    slot.avatarImage.color = Color.white;

                    // Cor de Categoria na linha superior
                    Color catColor = GetCategoryColor(app.primaryCategory);
                    slot.categoryStripeImg.color = catColor;

                    // Badge de Rank
                    slot.badgeText.text = GetRankBadge(app.rank);

                    // Nível do Appmon
                    int level = Mathf.Clamp(app.spd / 3, 10, 50);
                    slot.levelText.text = $"LV {level}";

                    // Nome curto
                    slot.nameText.text = app.name;

                    // Checkmark de Unidade em Campo
                    bool isSelected = selectedAppmons.Contains(app);
                    slot.checkmarkOverlay.SetActive(isSelected);

                    // Borda diferenciada quando colocada em campo
                    if (isSelected)
                    {
                        slot.baseBorderImg.sprite = GetRoundedRectSprite(106, 96, 10f, new Color(0.06f, 0.18f, 0.14f, 0.95f), colEmeraldGreen, 2f);
                    }
                    else
                    {
                        slot.baseBorderImg.sprite = GetRoundedRectSprite(106, 96, 10f, colSlotCardBg, colSlotBorderNormal, 1.5f);
                    }

                    // Moldura de Foco do Cursor
                    slot.focusHighlight.SetActive(isHovered);
                }
                else
                {
                    // Slot fantasma / vazio com borda sutil translúcida (estilo Cyber Terminal)
                    slot.root.SetActive(true);
                    slot.avatarImage.color = Color.clear;
                    slot.categoryStripeImg.color = Color.clear;
                    slot.badgeText.text = "";
                    slot.levelText.text = "";
                    slot.nameText.text = "";
                    slot.checkmarkOverlay.SetActive(false);
                    slot.focusHighlight.SetActive(isHovered);
                    slot.baseBorderImg.sprite = GetRoundedRectSprite(106, 96, 10f, new Color(0.04f, 0.07f, 0.11f, 0.45f), new Color(0.15f, 0.22f, 0.32f, 0.35f), 1f);
                }
            }
        }

        // 5. Atualiza Card de Detalhes com a criatura focada
        int currentFocusedIndex = pageOffset + (cursorY * COLS) + cursorX;
        if (currentFocusedIndex >= 0 && currentFocusedIndex < displayedAppmons.Count)
        {
            UpdateDetailCard(displayedAppmons[currentFocusedIndex]);
        }
        else
        {
            ClearDetailCard();
        }

        // 6. Botão Iniciar Batalha
        if (startBattleButton != null)
        {
            bool canStart = selectedAppmons.Count > 0;
            startBattleButton.interactable = canStart;
            if (startBattleButtonText != null)
            {
                startBattleButtonText.color = canStart ? Color.white : new Color(0.6f, 0.6f, 0.6f, 0.4f);
            }
            if (startBattleButtonGlow != null)
            {
                startBattleButtonGlow.gameObject.SetActive(canStart);
            }
        }
    }

    private void ClearDetailCard()
    {
        if (detailNameText != null) detailNameText.text = "---";
        if (detailLevelText != null) detailLevelText.text = "";
        if (detailStageText != null) detailStageText.text = "";
        if (detailMobilityText != null) detailMobilityText.text = "";
        if (detailCategoryText != null) detailCategoryText.text = "";
        if (detailStatsGridText != null) detailStatsGridText.text = "";
        if (detailPassiveText != null) detailPassiveText.text = "Nenhuma criatura corresponde ao filtro de nível selecionado.";
        if (detailHpValText != null) detailHpValText.text = "";
        if (detailSpValText != null) detailSpValText.text = "";
        if (detailAvatarImage != null) detailAvatarImage.color = Color.clear;
        if (detailCatDiamondImage != null) detailCatDiamondImage.color = Color.clear;
    }

    private void UpdateDetailCard(AppmonData app)
    {
        if (app == null) return;

        if (detailNameText != null) detailNameText.text = app.name;
        int level = Mathf.Clamp(app.spd / 3, 10, 50);
        if (detailLevelText != null) detailLevelText.text = $"LV {level}";

        if (detailStageText != null) detailStageText.text = GetRankDisplayName(app.rank);
        if (detailMobilityText != null) detailMobilityText.text = $"MOV: {app.mov}";

        Color catColor = GetCategoryColor(app.primaryCategory);

        if (detailCategoryText != null)
        {
            string typeStr = app.IsDualType ? $"[{app.primaryCategory} / {app.secondaryCategory}]" : $"[{app.primaryCategory}]";
            detailCategoryText.text = $"{typeStr}  ⛨ {app.protocol}";
            detailCategoryText.color = catColor;
        }

        if (detailAvatarImage != null)
        {
            detailAvatarImage.sprite = GetAppmonSprite(app);
            detailAvatarImage.color = Color.white;
        }

        if (detailCatDiamondImage != null)
        {
            detailCatDiamondImage.sprite = GetCategoryDiamondSprite(32, catColor);
            detailCatDiamondImage.color = Color.white;
        }

        if (detailHpValText != null) detailHpValText.text = $"{app.hp} / {app.hp}";
        if (detailSpValText != null) detailSpValText.text = $"{app.mp} / {app.mp}";

        if (detailHpBarFill != null) detailHpBarFill.sizeDelta = new Vector2(240f, 0f);
        if (detailSpBarFill != null) detailSpBarFill.sizeDelta = new Vector2(240f, 0f);

        // Painel de Estatísticas RPG completas
        if (detailStatsGridText != null)
        {
            detailStatsGridText.text = $"ATK <color=#FF8888>{app.atk}</color>  DEF <color=#88FF88>{app.def}</color>  INT <color=#88CCFF>{app.intStat}</color>  SPI <color=#DDAAFF>{app.spi}</color>  SPD <color=#FFEE88>{app.spd}</color>  CRT <color=#FFBB44>{app.crt}%</color>";
        }

        // Habilidade Passiva Exclusiva
        if (detailPassiveText != null)
        {
            detailPassiveText.text = $"<color=#00F5FF><b>⚡ PASSIVA: [{app.passiveName}]</b></color>\n{app.passiveDescription}";
        }
    }

    private Sprite GetAppmonSprite(AppmonData app)
    {
        if (app == null) return null;

        string iconKey = null;
        if (appmonIconMap.TryGetValue(app.name, out string mapped))
        {
            iconKey = mapped;
        }

        Sprite sp = SkillIconDatabase.GetSkillIcon(iconKey, app.primaryCategory);
        return sp;
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

    private string GetRankDisplayName(EvolutionRank rank)
    {
        return rank switch
        {
            EvolutionRank.Standard => "Common / Nami",
            EvolutionRank.Super => "Super Appmon",
            EvolutionRank.Ultimate => "Ultimate Appmon",
            EvolutionRank.Celestial => "Celestial Guardian",
            EvolutionRank.Demon => "Demon of Sins",
            _ => "Standard"
        };
    }

    private string GetRankBadge(EvolutionRank rank)
    {
        return rank switch
        {
            EvolutionRank.Standard => "★",
            EvolutionRank.Super => "★S",
            EvolutionRank.Ultimate => "★U",
            EvolutionRank.Celestial => "★G",
            EvolutionRank.Demon => "★D",
            _ => "★"
        };
    }

    // =========================================================================
    // GERADORES DE SPRITES PROCEDURAIS DE ALTA DEFINIÇÃO (SDF)
    // =========================================================================
    public static Sprite GetRoundedRectSprite(int width, int height, float cornerRadius, Color fillColor, Color borderColor, float borderThickness)
    {
        string key = $"RRect_{width}_{height}_{cornerRadius}_{fillColor.GetHashCode()}_{borderColor.GetHashCode()}_{borderThickness}";
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
                if (dist > 1.0f)
                {
                    c = Color.clear;
                }
                else if (dist > 0.0f)
                {
                    float alpha = (1.0f - dist);
                    c = new Color(borderColor.r, borderColor.g, borderColor.b, borderColor.a * alpha);
                }
                else if (dist >= -borderThickness)
                {
                    c = borderColor;
                }
                else
                {
                    // Preenchimento com suave gradiente vertical
                    float normY = (float)y / height;
                    Color gradientFill = Color.Lerp(fillColor * 0.85f, fillColor * 1.15f, normY);
                    gradientFill.a = fillColor.a;
                    c = gradientFill;
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

    public static Sprite GetCyberCornerBracketSprite(int width, int height)
    {
        string key = $"CyberBrackets_{width}_{height}";
        if (s_spriteCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[width * height];
        Color gold = new Color(1.0f, 0.88f, 0.15f, 1.0f);
        float armLength = 18f;
        float thick = 3f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool isLeft = x < thick;
                bool isRight = x >= width - thick;
                bool isBottom = y < thick;
                bool isTop = y >= height - thick;

                bool nearCornerX = (x < armLength) || (x >= width - armLength);
                bool nearCornerY = (y < armLength) || (y >= height - armLength);

                bool isBracket = (isLeft && nearCornerY) || (isRight && nearCornerY) ||
                                 (isBottom && nearCornerX) || (isTop && nearCornerX);

                pixels[y * width + x] = isBracket ? gold : Color.clear;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        Sprite sp = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
        s_spriteCache[key] = sp;
        return sp;
    }

    public static Sprite GetCheckmarkBadgeSprite(int size)
    {
        string key = $"CheckBadge_{size}";
        if (s_spriteCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[size * size];
        float radius = (size * 0.5f) - 1.5f;
        float center = size * 0.5f;

        Color circleCol = new Color(0.08f, 0.85f, 0.35f, 0.98f);
        Color borderCol = new Color(0.85f, 1.0f, 0.90f, 1.0f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center + 0.5f;
                float dy = y - center + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy) - radius;

                Color c;
                if (dist > 1.0f)
                {
                    c = Color.clear;
                }
                else if (dist > 0.0f)
                {
                    c = new Color(borderCol.r, borderCol.g, borderCol.b, (1.0f - dist));
                }
                else if (dist >= -1.5f)
                {
                    c = borderCol;
                }
                else
                {
                    // Traçado analítico do checkmark '✓'
                    float nx = (float)x / size;
                    float ny = (float)y / size;

                    float dSeg1 = DistToSegment(nx, ny, 0.28f, 0.50f, 0.45f, 0.30f);
                    float dSeg2 = DistToSegment(nx, ny, 0.45f, 0.30f, 0.75f, 0.72f);
                    float dCheck = Mathf.Min(dSeg1, dSeg2);

                    if (dCheck < 0.075f)
                    {
                        c = Color.white;
                    }
                    else
                    {
                        c = circleCol;
                    }
                }

                pixels[y * size + x] = c;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        Sprite sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        s_spriteCache[key] = sp;
        return sp;
    }

    public static Sprite GetCursorGlowSprite(int width, int height, float cornerRadius)
    {
        string key = $"CursorGlow_{width}_{height}_{cornerRadius}";
        if (s_spriteCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[width * height];
        float halfW = width * 0.5f;
        float halfH = height * 0.5f;
        float glowThickness = 5.0f;
        float r = cornerRadius;

        Color glowCol = new Color(1.0f, 0.90f, 0.15f, 0.95f);

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

                Color c = Color.clear;
                if (Mathf.Abs(dist) < glowThickness)
                {
                    float falloff = 1.0f - (Mathf.Abs(dist) / glowThickness);
                    c = new Color(glowCol.r, glowCol.g, glowCol.b, glowCol.a * Mathf.Pow(falloff, 1.4f));
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

    public static Sprite GetBarFillSprite(int width, int height, Color colA, Color colB)
    {
        string key = $"BarFill_{width}_{height}_{colA.GetHashCode()}_{colB.GetHashCode()}";
        if (s_spriteCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            float ny = (float)y / height;
            for (int x = 0; x < width; x++)
            {
                float nx = (float)x / width;
                Color baseCol = Color.Lerp(colA, colB, nx);

                // Brilho superior de vidro
                if (ny > 0.60f)
                {
                    baseCol = Color.Lerp(baseCol, Color.white, 0.40f);
                }

                pixels[y * width + x] = baseCol;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        Sprite sp = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
        s_spriteCache[key] = sp;
        return sp;
    }

    public static Sprite GetCategoryDiamondSprite(int size, Color catCol)
    {
        string key = $"Diamond_{size}_{catCol.GetHashCode()}";
        if (s_spriteCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[size * size];
        float half = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            float dy = Mathf.Abs(y - half + 0.5f);
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs(x - half + 0.5f);
                float dist = (dx + dy) - (half - 1.5f);

                Color c;
                if (dist > 1.0f) c = Color.clear;
                else if (dist > 0.0f) c = new Color(catCol.r, catCol.g, catCol.b, 1.0f - dist);
                else if (dist >= -1.5f) c = Color.white;
                else c = catCol;

                pixels[y * size + x] = c;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        Sprite sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        s_spriteCache[key] = sp;
        return sp;
    }

    public static Sprite GetCircleSprite(int size, Color fillColor, Color borderColor, float borderThick)
    {
        string key = $"Circle_{size}_{fillColor.GetHashCode()}_{borderColor.GetHashCode()}";
        if (s_spriteCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[size * size];
        float radius = size * 0.5f - 1.0f;
        float center = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center + 0.5f;
                float dy = y - center + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy) - radius;

                Color c;
                if (dist > 1.0f) c = Color.clear;
                else if (dist > 0.0f) c = new Color(borderColor.r, borderColor.g, borderColor.b, (1.0f - dist));
                else if (dist >= -borderThick) c = borderColor;
                else c = fillColor;

                pixels[y * size + x] = c;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        Sprite sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        s_spriteCache[key] = sp;
        return sp;
    }

    private static Sprite GetPhoneChassisSprite()
    {
        return GetRoundedRectSprite(660, 970, 32f, new Color(0.03f, 0.05f, 0.08f, 0.98f), new Color(0.0f, 0.85f, 1.0f, 0.70f), 2.5f);
    }

    private static float DistToSegment(float px, float py, float x1, float y1, float x2, float y2)
    {
        float l2 = (x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1);
        if (l2 == 0) return Mathf.Sqrt((px - x1) * (px - x1) + (py - y1) * (py - y1));
        float t = Mathf.Clamp01(((px - x1) * (x2 - x1) + (py - y1) * (y2 - y1)) / l2);
        float projX = x1 + t * (x2 - x1);
        float projY = y1 + t * (y2 - y1);
        return Mathf.Sqrt((px - projX) * (px - projX) + (py - projY) * (py - projY));
    }

    // =========================================================================
    // UTILITÁRIOS DE CRIAÇÃO DA UI (COM SHADOW E ALTO CONTRASTE)
    // =========================================================================
    private GameObject CreateUIPanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.layer = 5;
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;

        Image img = obj.GetComponent<Image>();
        img.color = color;

        return obj;
    }

    private Text CreateUIText(Transform parent, string name, string text, int fontSize, FontStyle style, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta, Color color, TextAnchor alignment = TextAnchor.MiddleCenter)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Shadow));
        obj.layer = 5;
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;

        Text txt = obj.GetComponent<Text>();
        txt.font = GetBestFont();
        txt.text = text;
        txt.fontSize = fontSize;
        txt.fontStyle = style;
        txt.color = color;
        txt.alignment = alignment;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        txt.raycastTarget = false;

        // Adiciona sombra projetada de alto contraste em todos os textos para legibilidade perfeita
        Shadow shadow = obj.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.90f);
        shadow.effectDistance = new Vector2(1.5f, -1.5f);

        return txt;
    }

    private class UnitSlotUI
    {
        public int col;
        public int row;
        public GameObject root;
        public Image baseBorderImg;
        public GameObject focusHighlight;
        public Image focusHighlightImg;
        public Image categoryStripeImg;
        public Image avatarImage;
        public Text badgeText;
        public Text levelText;
        public Text nameText;
        public GameObject checkmarkOverlay;
    }

    private class LevelTabUI
    {
        public LevelFilterRange filterRange;
        public Vector2 size;
        public Image bgImage;
        public Text titleText;
    }
}

/// <summary>
/// Listener para detecção de passagem do mouse (hover) e clique direto nos slots do grid.
/// </summary>
public class SlotPointerListener : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    public int col;
    public int row;
    public DeploymentMenuUI menu;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (menu != null)
        {
            menu.OnSlotHovered(col, row);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (menu != null)
        {
            menu.OnSlotClicked(col, row);
        }
    }
}
