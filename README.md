# Project-E.C.H.O.
Genesis AR: A Planet Reborn" is a visionary concept that taps into several growing trends: the demand for immersive experiences, the rise of AR/MR hardware, and the increasing interest in educational/creative games. Its unique blend of genetic engineering, creature creation, and ecosystem management

## Agent Buzz Chat (Minimal Setup)
To let players talk to Agent Buzz in-scene:

1. Add `AgentBuzzChatUI` to your world-space UI canvas or holographic tablet object.
2. Assign these Inspector references:
   - `Player Input Field` (`TMP_InputField`)
   - `Send Button` (`Button`)
   - `Conversation Log Text` (`TextMeshProUGUI`)
3. (Optional) Enable `Submit On Enter` for keyboard send.
4. Press Play and send a message to start the conversation.

Script location:
- `Assets/Scripts/UI/AgentBuzzChatUI.cs`
