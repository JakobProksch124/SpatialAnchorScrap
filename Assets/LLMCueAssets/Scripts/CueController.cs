using TMPro;
using UnityEngine;

/// <summary>
/// Applies a CueConfig to all cue parts: theme (entry blue / arrival green),
/// invitation text, status-box text, buttons (Enter+dismiss vs single Close),
/// which answer cards may appear, and the assistant note. Runs on Start and can
/// be re-applied at edit time (right-click component -> Apply Config).
/// </summary>
public class CueController : MonoBehaviour
{
    [SerializeField] private CueConfig config;
    [SerializeField] private CueTheme theme;
    [SerializeField] private CueInvitationCard invitation;
    [SerializeField] private CueDockUI dock;
    [SerializeField] private CueAnswerRow answerRow;
    [SerializeField] private CueLLMClient llm;
    [SerializeField] private Transform cueRoot;
    [SerializeField] private GameObject enterButton;
    [SerializeField] private GameObject dismissButton;   // entry: "Jetzt nicht"
    [SerializeField] private GameObject closeButton;      // arrival: "Schließen"
    [SerializeField] private TMP_Text enterLabel;
    [SerializeField] private TMP_Text dismissLabel;
    [SerializeField] private TMP_Text closeLabel;

    private void Start() => Apply();

    [ContextMenu("Apply Config")]
    public void Apply()
    {
        if (config == null) return;
        var arrival = config.IsArrival;

        if (theme) theme.CurrentMode = arrival ? CueTheme.Mode.Arrival : CueTheme.Mode.Entry;

        if (invitation)
            invitation.Configure(
                arrival ? config.arrivalTitle : config.entryTitle,
                arrival ? config.arrivalHighlight : config.entryHighlight,
                arrival ? config.arrivalReason : config.entryReason);

        if (dock) dock.SetTexts(config.waitingText, config.listeningText, config.thinkingText);

        SetButtonMode(arrival, config.enterLabel, config.dismissLabel, config.closeLabel);

        if (answerRow) answerRow.SetCards(config.cards);

        if (llm)
        {
            llm.ExtraInstruction = config.assistantNote;
            llm.ContextText = config.contextText;
            llm.SetCards(config.cards);
            llm.OffersTransition = !config.IsArrival; // arrival cues have no transition to start
            if (answerRow) llm.IsCardShown = answerRow.IsShown; // prompt marks already-visible cards
        }

        // EXPERIMENT (panels-through-button): small icon buttons that toggle the answer panels
        // without the voice interface. Fully data-driven from config.cards; global on/off and
        // tuning via the single asset Resources/CuePanelButtonsSettings. Runtime only, so the
        // edit-time "Apply Config" context menu never bakes them into a prefab.
        if (Application.isPlaying && answerRow != null && invitation != null &&
            config.cards != null && config.cards.Count > 0)
        {
            var pbs = CuePanelButtonsSettings.Load();
            if (pbs != null && pbs.featureEnabled)
                CuePanelButtonRow.Attach((RectTransform)invitation.transform.parent, answerRow,
                    invitation, theme, config.cards, pbs);
        }
    }
    
    
    /// <summary>Entry shows Enter + dismiss; arrival shows a single Close. Labels from config.</summary>
    void SetButtonMode(bool arrival, string enter, string dismiss, string close)
    {
        if (enterButton) enterButton.SetActive(!arrival);
        if (dismissButton)
        {
            dismissButton.SetActive(!arrival);
            // "Jetzt nicht" never closes the cue, but the ATTEMPT must be logged (study data).
            if (!arrival && dismissButton.GetComponent<DismissButtonFunctionality>() == null)
                dismissButton.AddComponent<DismissButtonFunctionality>();
        }
        if (closeButton) closeButton.SetActive(arrival);
        if (enterLabel) enterLabel.text = enter;
        if (dismissLabel) dismissLabel.text = dismiss;
        if (closeLabel) closeLabel.text = close;
    }
}
