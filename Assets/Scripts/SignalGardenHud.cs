using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SignalGarden
{
    /// <summary>
    /// UGUI presentation for the five deterministic garden phases.
    /// The game owns rules and input; this component owns only readable state and button actions.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SignalGardenHud : MonoBehaviour
    {
        [Header("Game binding")]
        [SerializeField] private SignalGardenGame game;

        [Header("Persistent HUD")]
        [SerializeField] private GameObject objectivePanel;
        [SerializeField] private Text phaseLabel;
        [SerializeField] private Text statusLabel;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button soundButton;
        [SerializeField] private Button motionButton;
        [SerializeField] private Text soundButtonLabel;
        [SerializeField] private Text motionButtonLabel;

        [Header("Modal HUD")]
        [SerializeField] private GameObject overlay;
        [SerializeField] private Text overlayEyebrow;
        [SerializeField] private Text overlayTitle;
        [SerializeField] private Text overlayBody;
        [SerializeField] private Button overlayPrimaryButton;
        [SerializeField] private Button overlaySecondaryButton;

        private GardenPhase lastPhase;
        private string lastStatus = string.Empty;
        private bool hasRendered;
        private bool listenersBound;

        public GardenPhase DisplayedPhase
        {
            get { return game != null ? game.Phase : GardenPhase.Observe; }
        }

        public bool HasCompleteBinding
        {
            get
            {
                return game != null && objectivePanel != null && phaseLabel != null && statusLabel != null &&
                       retryButton != null && soundButton != null && motionButton != null &&
                       soundButtonLabel != null && motionButtonLabel != null && overlay != null &&
                       overlayEyebrow != null && overlayTitle != null && overlayBody != null &&
                       overlayPrimaryButton != null && overlaySecondaryButton != null;
            }
        }

        public void Configure(
            SignalGardenGame target,
            GameObject objective,
            Text phase,
            Text status,
            Button retry,
            Button sound,
            Button motion,
            Text soundLabel,
            Text motionLabel,
            GameObject modal,
            Text modalEyebrow,
            Text modalTitle,
            Text modalBody,
            Button modalPrimary,
            Button modalSecondary)
        {
            game = target;
            objectivePanel = objective;
            phaseLabel = phase;
            statusLabel = status;
            retryButton = retry;
            soundButton = sound;
            motionButton = motion;
            soundButtonLabel = soundLabel;
            motionButtonLabel = motionLabel;
            overlay = modal;
            overlayEyebrow = modalEyebrow;
            overlayTitle = modalTitle;
            overlayBody = modalBody;
            overlayPrimaryButton = modalPrimary;
            overlaySecondaryButton = modalSecondary;
            BindButtonListeners();
            Refresh(true);
        }

        public void Bind(SignalGardenGame target)
        {
            game = target;
            Refresh(true);
        }

        private void Awake()
        {
            if (game == null)
            {
                game = FindAnyObjectByType<SignalGardenGame>();
            }

            BindButtonListeners();
            Refresh(true);
        }

        private void OnEnable()
        {
            BindButtonListeners();
            Refresh(true);
        }

        private void Update()
        {
            Refresh(false);
        }

        public void OnRetryPressed()
        {
            if (game == null)
            {
                return;
            }

            game.RetryFromHud();
            Refresh(true);
        }

        public void OnSoundPressed()
        {
            if (game == null)
            {
                return;
            }

            game.ToggleSoundFromHud();
            Refresh(true);
        }

        public void OnMotionPressed()
        {
            if (game == null)
            {
                return;
            }

            game.ToggleReducedMotionFromHud();
            Refresh(true);
        }

        public void OnPrimaryOverlayPressed()
        {
            if (game == null)
            {
                return;
            }

            if (game.Phase == GardenPhase.Paused)
            {
                game.ResumeFromHud();
            }
            else if (game.Phase == GardenPhase.Verified)
            {
                game.ResetFromHud();
            }

            Refresh(true);
        }

        public void OnSecondaryOverlayPressed()
        {
            if (game == null)
            {
                return;
            }

            game.ResetFromHud();
            Refresh(true);
        }

        private void BindButtonListeners()
        {
            if (listenersBound)
            {
                return;
            }

            if (retryButton != null) retryButton.onClick.AddListener(OnRetryPressed);
            if (soundButton != null) soundButton.onClick.AddListener(OnSoundPressed);
            if (motionButton != null) motionButton.onClick.AddListener(OnMotionPressed);
            if (overlayPrimaryButton != null) overlayPrimaryButton.onClick.AddListener(OnPrimaryOverlayPressed);
            if (overlaySecondaryButton != null) overlaySecondaryButton.onClick.AddListener(OnSecondaryOverlayPressed);
            listenersBound = true;
        }

        private void OnDestroy()
        {
            if (!listenersBound)
            {
                return;
            }

            if (retryButton != null) retryButton.onClick.RemoveListener(OnRetryPressed);
            if (soundButton != null) soundButton.onClick.RemoveListener(OnSoundPressed);
            if (motionButton != null) motionButton.onClick.RemoveListener(OnMotionPressed);
            if (overlayPrimaryButton != null) overlayPrimaryButton.onClick.RemoveListener(OnPrimaryOverlayPressed);
            if (overlaySecondaryButton != null) overlaySecondaryButton.onClick.RemoveListener(OnSecondaryOverlayPressed);
        }

        private void Refresh(bool force)
        {
            if (game == null)
            {
                return;
            }

            var phase = game.Phase;
            var status = game.StatusText ?? string.Empty;
            if (!force && hasRendered && phase == lastPhase && status == lastStatus)
            {
                return;
            }

            if (objectivePanel != null) objectivePanel.SetActive(true);
            if (phaseLabel != null) phaseLabel.text = GetPhaseLabel(phase);
            if (statusLabel != null) statusLabel.text = status;
            if (soundButtonLabel != null) soundButtonLabel.text = game.SoundEnabled ? "Sound cues: On" : "Sound cues: Off";
            if (motionButtonLabel != null) motionButtonLabel.text = game.ReducedMotionEnabled ? "Motion: Reduced" : "Motion: Full";
            if (retryButton != null) retryButton.gameObject.SetActive(phase == GardenPhase.Recovery);

            var modal = phase == GardenPhase.Paused || phase == GardenPhase.Verified;
            if (overlay != null) overlay.SetActive(modal);
            if (modal)
            {
                var paused = phase == GardenPhase.Paused;
                if (overlayEyebrow != null) overlayEyebrow.text = paused ? "GARDEN AT REST" : "A SIGNAL TAKES ROOT";
                if (overlayTitle != null) overlayTitle.text = paused ? "Paused" : "The garden is awake.";
                if (overlayBody != null)
                {
                    overlayBody.text = paused
                        ? "Your turn is safe. Resume, or reset the garden for the next player."
                        : "A new player can take the next turn here.";
                }

                SetButtonLabel(overlayPrimaryButton, paused ? "Resume  /  Esc" : "Play again");
                if (overlaySecondaryButton != null)
                {
                    overlaySecondaryButton.gameObject.SetActive(paused);
                    SetButtonLabel(overlaySecondaryButton, "Restart this turn");
                }
            }

            if (!hasRendered || phase != lastPhase)
            {
                SelectInitialControl(phase);
            }

            hasRendered = true;
            lastPhase = phase;
            lastStatus = status;
        }

        private void SelectInitialControl(GardenPhase phase)
        {
            if (EventSystem.current == null)
            {
                return;
            }

            var target = phase == GardenPhase.Paused || phase == GardenPhase.Verified
                ? overlayPrimaryButton
                : phase == GardenPhase.Recovery ? retryButton : null;
            if (target == null || !target.gameObject.activeInHierarchy || !target.interactable)
            {
                return;
            }

            EventSystem.current.SetSelectedGameObject(target.gameObject);
            target.Select();
        }

        public static string GetPhaseLabel(GardenPhase phase)
        {
            switch (phase)
            {
                case GardenPhase.Routing:
                    return "ROUTING  /  HOLD TO TRACE";
                case GardenPhase.Recovery:
                    return "RECOVERY  /  TRY AGAIN";
                case GardenPhase.Verified:
                    return "VERIFIED  /  SIGNAL ROOTED";
                case GardenPhase.Paused:
                    return "PAUSED  /  TURN SAFE";
                default:
                    return "OBSERVE  /  FIND THE PATH";
            }
        }

        private static void SetButtonLabel(Button button, string value)
        {
            if (button == null)
            {
                return;
            }

            var label = button.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.text = value;
            }
        }
    }
}
