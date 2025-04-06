using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace VotePause;


public class VotePause
{
    private static Dictionary<ulong, DateTime> pauseVotes = [];
    private static Dictionary<ulong, DateTime> resumeVotes = [];

    private const uint timeoutSeconds = 60;

    private static uint playersNeeded(uint totalPlayers)
    {
        return (totalPlayers + 1) / 2;
    }

    [HarmonyPatch(typeof(UIChatController), nameof(UIChatController.Event_Server_OnChatCommand))]
    public static class UIChatControllerEventServerOnChatCommandPatch
    {
        [HarmonyPrefix]
        public static void Event_Server_OnChatCommand_Patch(
            Il2CppSystem.Collections.Generic.Dictionary<string, Il2CppSystem.Object> message)
        {
            if (message == null) return;
            if (!message.ContainsKey("command")) return;
            if (!message.ContainsKey("clientId")) return;

            PlayerManager playerManager = NetworkBehaviourSingleton<PlayerManager>.Instance;
            GameManager gameManager = NetworkBehaviourSingleton<GameManager>.Instance;
            UIChat uiChat = NetworkBehaviourSingleton<UIChat>.Instance;

            if (playerManager == null) return;
            if (gameManager == null) return;
            if (uiChat == null) return;

            uint totalPlayers = (uint)playerManager.GetPlayers(false).Count;
            uint needed = playersNeeded(totalPlayers);

            string command = message["command"].ToString().ToLower();
            ulong clientId = (ulong)Il2CppSystem.Convert.ToInt64(message["clientId"]);
            DateTime now = DateTime.UtcNow;

            switch (command)
            {
                case "/votepause":
                case "/vp":
                    pauseVotes[clientId] = now;
                    pauseVotes = pauseVotes
                        .Where(pair => now.Subtract(pair.Value).Seconds < timeoutSeconds)
                        .ToDictionary(pair => pair.Key, pair => pair.Value);
                    if (pauseVotes.Count >= needed)
                    {
                        uiChat.Server_SendSystemChatMessage($"<color=orange><b>VotePause</b></color> Vote passed - pausing! ({pauseVotes.Count}/{needed})");
                        gameManager.Server_Pause();
                        pauseVotes.Clear();
                    }
                    else
                    {
                        uiChat.Server_SendSystemChatMessage(
                            $"<color=orange><b>VotePause</b></color> Vote to <b>pause</b> in progress ({pauseVotes.Count}/{needed})."
                            + "Use <b>/votepause</b> or <b>/vp</b> to vote."
                        );
                    }
                    break;
                case "/voteresume":
                case "/vr":
                    resumeVotes[clientId] = now;
                    resumeVotes = resumeVotes
                        .Where(pair => now.Subtract(pair.Value).Seconds < timeoutSeconds)
                        .ToDictionary(pair => pair.Key, pair => pair.Value);
                    if (resumeVotes.Count >= needed)
                    {
                        uiChat.Server_SendSystemChatMessage($"<color=orange><b>VotePause</b></color> Vote passed - resuming! ({resumeVotes.Count}/{needed})");
                        gameManager.Server_Resume();
                        resumeVotes.Clear();
                    }
                    else
                    {
                        uiChat.Server_SendSystemChatMessage(
                            $"<color=orange><b>VotePause</b></color> Vote to <b>resume</b> in progress ({resumeVotes.Count}/{needed})."
                            + "Use <b>/voteresume</b> or <b>/vr</b> to vote."
                        );
                    }
                    break;
                default: return;
            }
        }
    }
}
