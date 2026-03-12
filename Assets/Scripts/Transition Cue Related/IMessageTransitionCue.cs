using UnityEngine;
using UnityEngine.Video;

public class IMessageTransitionCue : MonoBehaviour
{
    [Header("IMessage Cue Infos")]
    [Tooltip("Name of the child transform in the FBX model where the cue should appear")]
    [SerializeField] private string iMessageAnchorName = "iMessageAnchor";
    [SerializeField] private Color iMessagePrimaryColor = new Color(0.8f, 0.4f, 0f);
    [SerializeField] private string iMessageLabel = "VR";
    [SerializeField] private Texture2D iMessageScreenshotDisplayed;
    [SerializeField] private string iMessageDescription = "Welcome to the VR lecture!";
    [SerializeField] private string iMessageButtonText = "Start Video";
    [SerializeField] private bool iMessageAlwaysExpand = false;
    [Tooltip("only used, when start arrival cue is set to blunt")]
    [SerializeField] private Transform iMessageAnchor;
    [SerializeField] private Collider iMessageTrigger;
    [SerializeField] private AudioSource notificationSoundPlayer;
    [SerializeField] private AudioSource readNotificationPlayer;
    [SerializeField] private float iMessageDisappearDelay = 12f;
    [SerializeField] private float readNotificationDelay = 2f;

    [Header(" Visual Voice Cue Infos")]
    [Tooltip("Name of the child transform in the FBX model where the cue should appear")]
    [SerializeField] private string visualVoiceAnchorName = "leaveHMDAnchor";
    [SerializeField] private Color visualVoicePrimaryColor = new Color(0.8f, 0.4f, 0f);
    [SerializeField] private string visualVoiceLabel = "R";
    [SerializeField] private string visualVoiceDescription = "Take off the headmounted display";
    [SerializeField] private string visualVoiceButtonText = "";
    [SerializeField] private bool visualVoiceAlwaysExpand = false;
    [SerializeField] private VideoClip visualVoicevideoClip;
    
    [SerializeField]private Transform visualVoiceAnchor;

    private bool visualVoiceIsBland = false;

    private GameObject visualVoiceCue;


    private bool willReadNotification = true;
    
    private GameObject iMessageCue;
    private bool hasTriggered = false;
    private bool iMessageIsBland = false;

    void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        hasTriggered = true;
        CreateIMessageArrivalCue(iMessageAnchor);
        
    }

    void CreateIMessageArrivalCue(Transform iMessageAnchor)
    {
        TransitionCueConfig iMessageCueConfig = TransitionCueConfig.CreateARConfig(
            parent: iMessageAnchor,
            onInteract: () =>
             {
                iMessageCue.SetActive(false);
                 readNotification();
             }
        );


        iMessageCueConfig.onCollide = (other) =>
        {
            iMessageCue.SetActive(false);
            readNotification();
        };

        iMessageCueConfig.alwaysExpanded = iMessageAlwaysExpand;
        iMessageCueConfig.isBland = iMessageIsBland;
        iMessageCueConfig.isArrival = true;
        iMessageCueConfig.primaryColor = iMessagePrimaryColor;
        iMessageCueConfig.expandedDescription = iMessageDescription;
        iMessageCueConfig.screenshotTexture = iMessageScreenshotDisplayed;
        iMessageCueConfig.label = iMessageLabel;
        iMessageCueConfig.buttonText = iMessageButtonText;

        iMessageCue = TransitionCueFactory.CreateCue(iMessageCueConfig);
        UnityEngine.Debug.Log("iMessage cue created!");
        notificationSoundPlayer.Play();

        if (!iMessageIsBland)
        {
            Invoke(nameof(readNotification), readNotificationDelay);
        }
        else
        {
            willReadNotification = false;
        }
    }

    // CUE INFO:
    // This cue is placed at the doors of any vr room and allows the player to exit the vr room and return to the ar-supported world
    void CreateVisualVoiceCue(Transform visualVoiceAnchor)
    {
        if (!visualVoiceIsBland)
        {
            // Base (Same basic configuration for enhanced as well as minimal cues
            TransitionCueConfig visualVoiceCueConfig = TransitionCueConfig.CreateARConfig(
            parent: visualVoiceAnchor,
            onInteract: () =>
            {
                visualVoiceCue.SetActive(false);
            }
        );

            // Details for enhanced cues
            visualVoiceCueConfig.alwaysExpanded = visualVoiceAlwaysExpand;
            visualVoiceCueConfig.primaryColor = visualVoicePrimaryColor;
            visualVoiceCueConfig.expandedDescription = visualVoiceDescription;
            visualVoiceCueConfig.videoClip = visualVoicevideoClip;
            visualVoiceCueConfig.isVoiceCue = true;
            // (Effectively not used if alwaysExpanded)
            visualVoiceCueConfig.label = visualVoiceLabel;
            visualVoiceCueConfig.buttonText = visualVoiceButtonText;
            visualVoiceCue = TransitionCueFactory.CreateCue(visualVoiceCueConfig);
        }
        else
        {
            readNotificationPlayer.Play();
        }
    }


    void setCueInvisible()
    {
        iMessageCue.SetActive(false);
    }

    void readNotification()
    {
        if (willReadNotification)
        {
            if (iMessageCue != null)
            {
                Destroy(iMessageCue);

            }
            CreateVisualVoiceCue(visualVoiceAnchor);
            this.willReadNotification = false;
        }
    }

    public void SetIsBland(bool iMessageIsBland)
    {
        this.iMessageIsBland = iMessageIsBland;
    }

}
