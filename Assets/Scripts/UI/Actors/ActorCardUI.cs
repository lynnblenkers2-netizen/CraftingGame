using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

[RequireComponent(typeof(CanvasGroup))]
public class ActorCardUI : MonoBehaviour
{
    [Header("UI Refs")]
    public Image portrait;
    public TextMeshProUGUI nameLabel;
    public TextMeshProUGUI roleLabel;
    public TextMeshProUGUI stateLabel;
    public TextMeshProUGUI taskDescriptionLabel;
    [FormerlySerializedAs("statementLabel")] public TextMeshProUGUI thoughtLabel;
    public TextMeshProUGUI spiritPerDayLabel;
    public TextMeshProUGUI levelLabel;
    public TextMeshProUGUI happinessLabel;
    public Button btnChooseForage;
    public Button btnChooseSell;
    public Button btnChooseResearch;
    public Button btnChooseCraft;
    public Button btnChooseTavern;
    public Button btnMoveBackpack;
    public ActorCraftingPanelUI craftingPanel;

    [Header("Statement Log")]
    public Button statementLogButton;
    public Button statementLogCloseButton;
    public GameObject statementLogPanel;
    public ScrollRect statementLogScroll;
    public Transform statementLogContent;
    public GameObject logEntryPrefab;
    public CanvasGroup statementLogCanvasGroup;
    [SerializeField] bool verboseLogging;

    [Header("Embedded Backpack")]
    public InventoryUI backpackUI; // InventoryUI-Komponente in der Card

    [Header("Localized Strings")]
    public string statusIdleText = "Idle";
    public string statusTravelingText = "Traveling";
    public string statusWorkingText = "Working";
    public string statusReturningText = "Returning";
    public string statusPausedText = "Paused";
    public string taskIdleText = "Idle";
    public string foragePrefix = "Forage: {0}";
    public string sellPrefix = "Sell: {0}";
    public string researchPrefix = "Research: {0}";
    public string craftPrefix = "Craft: {0}";
    public string tavernPrefix = "Tavern: {0}";
    public string unknownTaskPrefix = "{0}";
    public string spiritPerDayPrefix = "Spirit / Day: {0}";

    // Runtime
    ActorInstance actor;
    ActorsPanelUI panel;
    GlobalInventoryService global;
    static bool warmedOptionPopup;
    string lastNonThoughtMessage;
    

    public void SetServices(GlobalInventoryService g)
    {
        global = g;
        if (backpackUI) backpackUI.SetAutoInventoryService(null);
    }

    void Awake()
    {
        // Warm the option popup once so first click doesn't incur setup
        if (!warmedOptionPopup)
        {
            warmedOptionPopup = true;
            OptionPopup.Warm();
        }
    }

    // -------- Public API --------
    /// <summary>Wird von ActorsPanelUI nach dem Instantiate aufgerufen.</summary>
    public void Bind(ActorInstance a, ActorsPanelUI p)
    {
        if (actor != null) actor.OnStatement -= HandleStatement;
        actor = a;
        panel = p;
        if (craftingPanel == null) craftingPanel = panel?.craftingPanel;
        actor?.EnsureBackpackCapacity();
        if (actor != null) actor.OnStatement += HandleStatement;

        // Header
        if (actor?.def)
        {
            if (portrait)
                portrait.sprite = actor.def.portraitOverride ? actor.def.portraitOverride : actor.def.role?.portrait;
            if (nameLabel) nameLabel.text = actor.def.displayName;
            if (roleLabel) roleLabel.text = actor.def.role ? actor.def.role.displayName : "-";
            if (spiritPerDayLabel) spiritPerDayLabel.text = string.Format(spiritPerDayPrefix, Mathf.Max(0, actor.def.spiritPerDay));
        }
        else
        {
            if (portrait) portrait.sprite = null;
            if (nameLabel) nameLabel.text = "-";
            if (roleLabel) roleLabel.text = "-";
            if (spiritPerDayLabel) spiritPerDayLabel.text = string.Format(spiritPerDayPrefix, 0);
        }

        EnsureStatementUI();

        // Backpack an UI binden
        if (backpackUI)
        {
            backpackUI.ownerType = ItemSlotUI.OwnerType.Backpack;
            backpackUI.SetInventory(actor.backpack);
            var slotUIs = backpackUI.GetSlotUIs();
            if (slotUIs != null)
            {
                foreach (var slot in slotUIs)
                {
                    if (slot) slot.Init(actor.backpack, slot.index, ItemSlotUI.OwnerType.Backpack);
                }
            }
        }

        // Buttons → Popups
        WireButtons();
        RefreshStateText(); // erste Anzeige
        UpdateStatsUI();
        PopulateStatementLog();
    }

    void WireButtons()
    {
        bool isIdle = actor == null || actor.state == ActorState.Idle;
        bool canAssign = isIdle;
        bool canForage = actor?.Role?.canForage == true;
        bool canSell = actor?.Role?.canSell == true;
        bool canResearch = actor?.Role?.canResearch == true;
        bool canCraft = actor?.Role?.canCraft == true;
        bool canTavern = actor?.Role?.canVisitTavern == true;
        ConfigureButton(btnChooseForage, canForage, canAssign && canForage, OnChooseForage);
        ConfigureButton(btnChooseSell, canSell, canAssign && canSell, OnChooseSell);
        ConfigureButton(btnChooseResearch, canResearch, canAssign && canResearch, OnChooseResearch);
        ConfigureButton(btnChooseCraft, canCraft, canAssign && canCraft, OnChooseCraft);
        ConfigureButton(btnChooseTavern, canTavern, canAssign && canTavern, OnChooseTavern);
        if (btnMoveBackpack)
        {
            btnMoveBackpack.gameObject.SetActive(true);
            btnMoveBackpack.interactable = actor?.backpack != null;
            btnMoveBackpack.onClick.RemoveAllListeners();
            if (btnMoveBackpack.interactable)
                btnMoveBackpack.onClick.AddListener(MoveBackpackToInventory);
        }
    }

    void ConfigureButton(Button btn, bool visible, bool interactable, UnityEngine.Events.UnityAction handler)
    {
        if (!btn) return;
        btn.gameObject.SetActive(visible);
        btn.interactable = interactable;
        btn.onClick.RemoveAllListeners();
        if (interactable && handler != null)
            btn.onClick.AddListener(handler);
    }

    void OnDestroy()
    {
        if (actor != null) actor.OnStatement -= HandleStatement;
    }

    void Update()
    {
        if (actor == null) return;
        RefreshStateText();
        RefreshThoughtUI();
    }

    void RefreshStateText()
    {
        if (!stateLabel) return;
        if (actor == null) { stateLabel.text = "-"; return; }

        string txt = GetStatusText(actor.state);
        if (actor.remainingDays > 0.001f) txt += $" ({actor.remainingDays:0.0}d)";
        stateLabel.text = txt;
        UpdateTaskDescription();
        UpdateStatsUI();
        UpdateBackpackInteractivity();
        UpdateAssignmentButtons();
    }

    void UpdateTaskDescription()
    {
        if (!taskDescriptionLabel)
            return;

        if (actor == null || string.IsNullOrEmpty(actor.taskType) || actor.taskAsset == null)
        {
            taskDescriptionLabel.text = taskIdleText;
            return;
        }

        taskDescriptionLabel.text = actor.taskType switch
        {
            "forage" => string.Format(foragePrefix, ((ForageArea)actor.taskAsset)?.displayName ?? "Unknown"),
            "sell" => string.Format(sellPrefix, ((SellRoute)actor.taskAsset)?.displayName ?? "Unknown"),
            "research" => string.Format(researchPrefix, ((ResearchDomain)actor.taskAsset)?.displayName ?? "Unknown"),
            "craft" => string.Format(craftPrefix, ((CraftPlan)actor.taskAsset)?.displayName ?? "Unknown"),
            "tavern" => string.Format(tavernPrefix, ((TavernVisit)actor.taskAsset)?.displayName ?? "Unknown"),
            _ => string.Format(unknownTaskPrefix, actor.taskType)
        };
    }

    void EnsureStatementUI()
    {
        Log("EnsureStatementUI");
        HideLogPanelImmediate();

        if (statementLogButton)
        {
            statementLogButton.onClick.RemoveAllListeners();
            statementLogButton.onClick.AddListener(() => ToggleLogPanel(true));
        }
        if (statementLogCloseButton)
        {
            statementLogCloseButton.onClick.RemoveAllListeners();
            statementLogCloseButton.onClick.AddListener(() => ToggleLogPanel(false));
        }
    }

    void ToggleLogPanel(bool show)
    {
        if (!statementLogPanel) return;
        Log($"ToggleLogPanel show={show}");
        statementLogPanel.SetActive(show);
        if (statementLogCanvasGroup)
        {
            statementLogCanvasGroup.alpha = show ? 1f : 0f;
            statementLogCanvasGroup.interactable = show;
            statementLogCanvasGroup.blocksRaycasts = show;
        }
        if (show && statementLogScroll)
        {
            Canvas.ForceUpdateCanvases();
            statementLogScroll.verticalNormalizedPosition = 0f;
        }
    }

    void PopulateStatementLog()
    {
        if (statementLogContent)
        {
            foreach (Transform child in statementLogContent)
                Destroy(child.gameObject);
        }


        if (actor?.statementLog != null)
        {
            foreach (var entry in actor.statementLog)
                AppendStatementEntry(entry);
            if (actor.statementLog.Count > 0)
            {
                var last = actor.statementLog[^1];
                bool isThought = ActorInstance.IsThoughtMessage(last);
                if (isThought && actor.ThoughtActive && thoughtLabel)
                    thoughtLabel.text = actor.CurrentThought;
                else if (!isThought)
                    lastNonThoughtMessage = last;
                if (thoughtLabel && !actor.ThoughtActive && !string.IsNullOrEmpty(lastNonThoughtMessage))
                    thoughtLabel.text = StripTimestamp(lastNonThoughtMessage);
            }
        }
    }

    void HideLogPanelImmediate()
    {
        if (!statementLogPanel) return;
        Log("HideLogPanelImmediate");
        statementLogPanel.SetActive(false);
        if (statementLogCanvasGroup)
        {
            statementLogCanvasGroup.alpha = 0f;
            statementLogCanvasGroup.interactable = false;
            statementLogCanvasGroup.blocksRaycasts = false;
        }
    }

    void HandleStatement(string message)
    {
        bool isThought = ActorInstance.IsThoughtMessage(message);
        if (isThought)
        {
            if (thoughtLabel) thoughtLabel.text = actor?.CurrentThought ?? StripTimestamp(ActorInstance.StripThoughtTag(message));
        }
        else
        {
            lastNonThoughtMessage = message;
            if (thoughtLabel && (actor == null || !actor.ThoughtActive))
                thoughtLabel.text = StripTimestamp(message);
        }
        AppendStatementEntry(message);
    }

    void AppendStatementEntry(string text)
    {
        if (string.IsNullOrEmpty(text) || statementLogContent == null) return;
        Log($"AppendStatementEntry {text}");
        var prefab = logEntryPrefab ? logEntryPrefab : LoadDefaultLogPrefab();
        if (!prefab) return;
        var go = Instantiate(prefab, statementLogContent);
        var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp)
        {
            // Ensure the stamped line breaks after the timestamp prefix if present
            string clean = ActorInstance.StripThoughtTag(text);
            int bracket = clean.IndexOf(']');
            if (bracket >= 0 && bracket + 1 < clean.Length)
            {
                int start = bracket + 1;
                if (clean[start] == ' ') start++;
                string ts = clean.Substring(0, bracket + 1);
                string body = clean.Substring(start);
                tmp.text = $"{ts}\n{body}";
            }
            else
            {
                tmp.text = clean;
            }
        }

        if (statementLogScroll)
        {
            Canvas.ForceUpdateCanvases();
            statementLogScroll.verticalNormalizedPosition = 0f;
        }
    }

    GameObject LoadDefaultLogPrefab()
    {
        if (!logEntryPrefab)
            logEntryPrefab = Resources.Load<GameObject>("UI/LogStatement");
        return logEntryPrefab;
    }

    void MoveBackpackToInventory()
    {
        if (panel?.manager == null || actor == null) return;
        panel.manager.DumpBackpackToInventory(actor);
        actor.AddStatement("Emptied backpack into player inventory.");
    }

    void UpdateBackpackInteractivity()
    {
        if (backpackUI == null || actor == null) return;
        bool canInteract = actor.state == ActorState.Idle;
        backpackUI.SetInteractable(canInteract);
    }

    void UpdateAssignmentButtons()
    {
        if (actor == null) return;
        bool canAssign = actor.state == ActorState.Idle;
        if (btnChooseForage) btnChooseForage.interactable = btnChooseForage.gameObject.activeSelf && canAssign && actor.Role?.canForage == true;
        if (btnChooseSell) btnChooseSell.interactable = btnChooseSell.gameObject.activeSelf && canAssign && actor.Role?.canSell == true;
        if (btnChooseResearch) btnChooseResearch.interactable = btnChooseResearch.gameObject.activeSelf && canAssign && actor.Role?.canResearch == true;
        if (btnChooseCraft) btnChooseCraft.interactable = btnChooseCraft.gameObject.activeSelf && canAssign && actor.Role?.canCraft == true;
        if (btnChooseTavern) btnChooseTavern.interactable = btnChooseTavern.gameObject.activeSelf && canAssign && actor.Role?.canVisitTavern == true;
        if (btnMoveBackpack)
            btnMoveBackpack.interactable = btnMoveBackpack.gameObject.activeSelf && actor.backpack != null && canAssign;
    }

    void UpdateStatsUI()
    {
        if (actor == null) return;
        if (levelLabel) levelLabel.text = $"Lv {actor.level} ({actor.xp} XP)";
        if (happinessLabel)
        {
            float happy = actor != null ? actor.EffectiveHappiness : (actor?.def != null ? actor.def.happiness : 0f);
            happinessLabel.text = $"Happy {Mathf.RoundToInt(happy * 100f)}%";
        }
    }

        // -------- Popup Handlers --------
    void OnChooseForage()
    {
        if (panel == null || panel.forageAreas == null || panel.forageAreas.Count == 0)
        {
            ToastSystem.Info("Keine Areas definiert");
            return;
        }
        OptionPopup.Show("Waehl eine Area", panel.forageAreas,
            area => area ? area.displayName : "???",
            idx => panel.AssignForage(actor, idx, false));
    }

    void OnChooseSell()
    {
        if (panel == null || panel.sellRoutes == null || panel.sellRoutes.Count == 0)
        {
            ToastSystem.Info("Keine Routen definiert");
            return;
        }
        OptionPopup.Show("Waehl eine Route", panel.sellRoutes,
            r => r ? r.displayName : "???",
            idx => panel.AssignSell(actor, idx, false));
    }

    void OnChooseResearch()
    {
        if (panel == null || panel.researchDomains == null || panel.researchDomains.Count == 0)
        {
            ToastSystem.Info("Keine Domaenen definiert");
            return;
        }
        OptionPopup.Show("Waehl eine Domaene", panel.researchDomains,
            d => d ? d.displayName : "???",
            idx => panel.AssignResearch(actor, idx, false));
    }

    void OnChooseCraft()
    {
        var targetPanel = craftingPanel != null ? craftingPanel : panel?.craftingPanel;
        if (targetPanel != null)
        {
            targetPanel.Open(actor);
            return;
        }

        if (panel == null || panel.craftPlans == null || panel.craftPlans.Count == 0)
        {
            ToastSystem.Info("Keine Craft-Plaene definiert");
            return;
        }
        OptionPopup.Show("Waehl einen Craft-Plan", panel.craftPlans,
            c => c ? c.displayName : "???",
            idx => panel.AssignCraft(actor, idx, false));
    }

    void OnChooseTavern()
    {
        if (panel == null || panel.tavernVisits == null || panel.tavernVisits.Count == 0)
        {
            ToastSystem.Info("Keine Taverne definiert");
            return;
        }
        OptionPopup.Show("Waehl eine Taverne", panel.tavernVisits,
            t => t ? t.displayName : "???",
            idx => panel.AssignTavern(actor, idx, false));
    }

    void Log(string msg)
    {
        if (!verboseLogging) return;
        Debug.Log($"[ActorCardUI:{name}] {msg}", this);
    }

    void RefreshThoughtUI()
    {
        if (thoughtLabel == null) return;
        if (actor != null && actor.ThoughtActive)
        {
            thoughtLabel.text = actor.CurrentThought;
            return;
        }
        if (!string.IsNullOrEmpty(lastNonThoughtMessage))
        {
            thoughtLabel.text = StripTimestamp(lastNonThoughtMessage);
        }
    }

    string StripTimestamp(string msg)
    {
        if (string.IsNullOrEmpty(msg)) return msg;
        int end = msg.IndexOf(']');
        if (end >= 0 && end + 1 < msg.Length)
        {
            int start = end + 1;
            if (start < msg.Length && msg[start] == ' ') start++;
            return msg.Substring(start);
        }
        return msg;
    }

    string GetStatusText(ActorState state)
    {
        return state switch
        {
            ActorState.Idle => statusIdleText,
            ActorState.Traveling => statusTravelingText,
            ActorState.Working => statusWorkingText,
            ActorState.Returning => statusReturningText,
            ActorState.Paused => statusPausedText,
            _ => state.ToString()
        };
    }
}








