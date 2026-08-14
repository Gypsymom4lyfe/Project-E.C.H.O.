using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GenesisAR.UI
{
    /// <summary>
    /// Minimal chat bridge that lets the player send text to Agent Buzz and
    /// displays a lightweight in-app response in a conversation log.
    /// </summary>
    public class AgentBuzzChatUI : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("TMP input field where the player types a message to Buzz.")]
        public TMP_InputField playerInputField;

        [Tooltip("Button that sends the current input text to Buzz.")]
        public Button sendButton;

        [Tooltip("TMP text element that renders the conversation transcript.")]
        public TextMeshProUGUI conversationLogText;

        [Header("Behavior")]
        [Tooltip("When true, Enter submits messages from the input field.")]
        public bool submitOnEnter = true;

        [Tooltip("When true, clears the input field after a message is sent.")]
        public bool clearInputAfterSend = true;

        [Tooltip("Optional canned intro line shown on start.")]
        [TextArea(2, 4)]
        public string introLine = "Buzz: Online and ready. Ask me anything about this world.";

        [Tooltip("Maximum number of chat lines retained in the on-screen transcript.")]
        [Min(1)]
        public int maxTranscriptLines = 40;

        private readonly Queue<string> _transcriptLines = new Queue<string>();

        private void Awake()
        {
            if (sendButton != null)
                sendButton.onClick.AddListener(SendCurrentInput);

            if (playerInputField != null)
                playerInputField.onSubmit.AddListener(HandleInputSubmitted);
        }

        private void Start()
        {
            if (!string.IsNullOrWhiteSpace(introLine))
                AppendLog(introLine);

            if (playerInputField == null)
                Debug.LogWarning("[AgentBuzzChatUI] playerInputField is not assigned.", this);

            if (sendButton == null)
                Debug.LogWarning("[AgentBuzzChatUI] sendButton is not assigned.", this);

            if (conversationLogText == null)
                Debug.LogWarning("[AgentBuzzChatUI] conversationLogText is not assigned.", this);
        }

        private void OnDestroy()
        {
            if (sendButton != null)
                sendButton.onClick.RemoveListener(SendCurrentInput);

            if (playerInputField != null)
                playerInputField.onSubmit.RemoveListener(HandleInputSubmitted);
        }

        private void HandleInputSubmitted(string _)
        {
            if (!submitOnEnter)
                return;

            SendCurrentInput();
        }

        public void SendCurrentInput()
        {
            if (playerInputField == null)
                return;

            string message = playerInputField.text.Trim();
            if (string.IsNullOrEmpty(message))
                return;

            AppendLog($"You: {message}");
            AppendLog($"Buzz: {GenerateBuzzReply(message)}");

            if (clearInputAfterSend)
                playerInputField.text = string.Empty;

            if (playerInputField.isActiveAndEnabled)
                StartCoroutine(ReactivateInputNextFrame());
        }

        /// <summary>
        /// Returns a lightweight canned response using simple keyword matching
        /// against the player's latest message.
        /// </summary>
        private string GenerateBuzzReply(string playerMessage)
        {
            string lower = playerMessage.ToLowerInvariant();

            if (lower.Contains("weather"))
                return "Current weather is stable. Check your telemetry panel for live atmospheric details.";

            if (lower.Contains("water"))
                return "Hydro readings look normal. You can sync water telemetry in the journal.";

            if (lower.Contains("animal") || lower.Contains("creature") || lower.Contains("species"))
                return "Life-sign clusters are active. Try scanning nearby fauna to add new logs.";

            if (lower.Contains("help"))
                return "Try asking about weather, water, species, or telemetry sync.";

            return "Acknowledged. I logged your request. Ask for help to see suggested topics.";
        }

        private void AppendLog(string line)
        {
            if (conversationLogText == null || string.IsNullOrEmpty(line))
                return;

            _transcriptLines.Enqueue(line);

            while (_transcriptLines.Count > maxTranscriptLines)
                _transcriptLines.Dequeue();

            conversationLogText.text = string.Join("\n", _transcriptLines);
        }

        private IEnumerator ReactivateInputNextFrame()
        {
            yield return null;

            if (playerInputField != null && playerInputField.isActiveAndEnabled)
                playerInputField.ActivateInputField();
        }
    }
}
