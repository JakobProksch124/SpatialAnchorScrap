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
    [Tooltip("only used, when start arrival cue is set to blunt")]
    [SerializeField] private Transform iMessageAnchor;
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
    [SerializeField] private VideoClip visualVoicevideoClip;

    [SerializeField] private Transform visualVoiceAnchor;

    private GameObject visualVoiceCue;


    private bool willReadNotification = true;

    private GameObject iMessageCue;
    private bool hasTriggered = false;
    private Transform playerTransform;
    private float triggerDistance = 6f;

    private PathGenerator pathGenerator;

    [SerializeField] private string entryAnchorName = "entryAnchor";
    private Transform entryAnchor;


    void Start()
    {
        foreach (PathGenerator component in GetComponents<PathGenerator>())
        {
            if (component.GetType().Name == "PathGenerator")
            {
                pathGenerator = component;
                break;
            }
        }

        // Find entry anchor point in this building
        entryAnchor = transform.Find(entryAnchorName);
        if (entryAnchor == null)
        {
            Debug.LogWarning($"[Building_TransitionCues] Anchor '{entryAnchorName}' not found. Using this transform.");
            entryAnchor = transform;
        }


        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            playerTransform = mainCam.transform;
        }
        else
        {
            Debug.Log("[TransitionCueExpander] NO MAIN CAMERA FOUND!!!");
        }
    }

    void LateUpdate()
    {
        if (playerTransform == null)
            return;

        if (hasTriggered) return;
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            if (triggerDistance > distance )
            {
                hasTriggered = true;
                CreateIMessageArrivalCue(iMessageAnchor);
            }
    }

    void CreateIMessageArrivalCue(Transform iMessageAnchor)
    {
        TransitionCueConfig iMessageCueConfig = TransitionCueConfig.CreateARConfig(
            parent: iMessageAnchor,
            onInteract: () =>
            {
                iMessageCue.SetActive(false);
                //readNotification();
                CreateVisualVoiceCue(visualVoiceAnchor);
            },
            onClose: () =>
            {
            },
            isStandardClose: true
        );


        iMessageCueConfig.onCollide = (other) =>
        {
            iMessageCue.SetActive(false);
            //readNotification();
            CreateVisualVoiceCue(visualVoiceAnchor);
        };

        iMessageCueConfig.alwaysExpanded = true;
        iMessageCueConfig.isArrival = true;
        iMessageCueConfig.primaryColor = iMessagePrimaryColor;
        iMessageCueConfig.expandedDescription = iMessageDescription;
        iMessageCueConfig.screenshotTexture = iMessageScreenshotDisplayed;
        iMessageCueConfig.label = iMessageLabel;
        iMessageCueConfig.buttonText = iMessageButtonText;

        iMessageCue = TransitionCueFactory.CreateCue(iMessageCueConfig);
        UnityEngine.Debug.Log("iMessage cue created!");
        notificationSoundPlayer.Play();

       
    }

    // CUE INFO:
    // This cue is placed at the doors of any vr room and allows the player to exit the vr room and return to the ar-supported world
    void CreateVisualVoiceCue(Transform visualVoiceAnchor)
    {
        
            // Base (Same basic configuration for enhanced as well as minimal cues
            TransitionCueConfig visualVoiceCueConfig = TransitionCueConfig.CreateARConfig(
            parent: visualVoiceAnchor,
            onInteract: () =>
            {
                visualVoiceCue.SetActive(false);
                pathGenerator.AddInbetweenTarget(entryAnchor);
            },
            onClose: () =>
            {
            },
            isStandardClose: true
        );


            visualVoiceCueConfig.onCollide = (other) =>
            {
                visualVoiceCue.SetActive(false);
            };

            // Details for enhanced cues
            visualVoiceCueConfig.alwaysExpanded = true;
            visualVoiceCueConfig.primaryColor = visualVoicePrimaryColor;
            visualVoiceCueConfig.expandedDescription = visualVoiceDescription;
            //visualVoiceCueConfig.videoClip = visualVoicevideoClip;
            visualVoiceCueConfig.isVoiceCue = true;
            // (Effectively not used if alwaysExpanded)
            visualVoiceCueConfig.label = visualVoiceLabel;
            visualVoiceCueConfig.buttonText = visualVoiceButtonText;
            visualVoiceCue = TransitionCueFactory.CreateCue(visualVoiceCueConfig);
    }


    void setCueInvisible()
    {
        iMessageCue.SetActive(false);
    }

    void readNotification()
    {
        if (willReadNotification)
        {
            CreateVisualVoiceCue(visualVoiceAnchor);
            this.willReadNotification = false;
            if (iMessageCue != null)
            {
                Destroy(iMessageCue);

            }
        }
    }


}
