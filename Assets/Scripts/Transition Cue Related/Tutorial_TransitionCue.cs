using UnityEngine;
using TMPro;

public class Tutorial_TransitionCue : MonoBehaviour
{
    [SerializeField]private TMP_Text buttonCountText;
    float pressCount = 0;


    [Header("Start Arrival Cue Infos")]
    [Tooltip("Name of the child transform in the FBX model where the cue should appear")]
    [SerializeField] private string startArrivalAnchorName = "startArrivalAnchor";
    [SerializeField] private Color startArrivalPrimaryColor = new Color(0.8f, 0.4f, 0f);
    [SerializeField] private string startArrivalLabel = "";
    [SerializeField] private Texture2D startArrivalScreenshotDisplayed;
    [SerializeField] private string startArrivalDescription = "Click mich";
    [SerializeField] private string startArrivalButtonText = "Oder mich";

    GameObject startArrivalCue;
    Transform startArrivalAnchor;

    public void IncreaseCount()
    {
        //
        this.pressCount += 1;
        buttonCountText.text=pressCount.ToString();
    }

    void Start()
    {
        //CreateStartArrivalCue(startArrivalAnchor);
    }


    // CUE INFO:
    // This cue spawns in the face of the user when starting a new application that has this script (i.e., G64 or Bib),
    // confronting them with orders to follow the arrow
    /*void CreateStartArrivalCue(Transform StartArrivalAnchor)
    {
        
            // Base
            TransitionCueConfig StartArrivalCueConfig = TransitionCueConfig.CreateARConfig(
                parent: StartArrivalAnchor,
                onInteract: () =>
                {
                    IncreaseCount();
                }
            );

            // Details
            StartArrivalCueConfig.alwaysExpanded = true;
            StartArrivalCueConfig.primaryColor = startArrivalPrimaryColor;
            StartArrivalCueConfig.expandedDescription = startArrivalDescription;
            StartArrivalCueConfig.screenshotTexture = startArrivalScreenshotDisplayed;

            // (Effectively not used if alwaysExpanded)
            StartArrivalCueConfig.label = startArrivalLabel;
            StartArrivalCueConfig.buttonText = startArrivalButtonText;

            startArrivalCue = TransitionCueFactory.CreateCue(StartArrivalCueConfig);
        
    }*/
}
