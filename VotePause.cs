using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace VotePause;


public class VotePause
{
    const string messagePrefix = "<color=orange><b>VotePause</b></color>";
    const string helpMessage = $"{messagePrefix} commands:\n"
        + "* <b>/votepause</b> (/vp) - Vote to pause\n"
        + "* <b>/voteresume</b> (/vr) - Vote to resume";

    private static Dictionary<ulong, DateTime> pauseVotes = [];
    private static Dictionary<ulong, DateTime> resumeVotes = [];

    private const uint timeoutSeconds = 60;

    private static uint PlayersNeeded(uint totalPlayers)
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
            uint needed = PlayersNeeded(totalPlayers);

            string command = message["command"].ToString().ToLower();
            ulong clientId = (ulong)Il2CppSystem.Convert.ToInt64(message["clientId"]);
            DateTime now = DateTime.UtcNow;

            switch (command)
            {
                case "/help":
                    uiChat.Server_SendClientSystemChatMessage(helpMessage, clientId);
                    break;
                case "/votepause":
                case "/vp":
                    Plugin.Log.LogDebug($"ClientID {clientId} voted to pause at {now}.");
                    pauseVotes = pauseVotes
                        .Where(pair => now.Subtract(pair.Value).Seconds < timeoutSeconds)
                        .ToDictionary(pair => pair.Key, pair => pair.Value);
                    bool alreadyVotedPause = pauseVotes.ContainsKey(clientId);
                    pauseVotes[clientId] = now;
                    if (pauseVotes.Count >= needed)
                    {
                        Plugin.Log.LogDebug($"Vote to pause passed. [{pauseVotes.Count}/{needed}]");
                        uiChat.Server_SendSystemChatMessage($"{messagePrefix} Vote passed - pausing! ({pauseVotes.Count}/{needed})");
                        gameManager.Server_Pause();
                        pauseVotes.Clear();
                    }
                    else if (!alreadyVotedPause)
                    {
                        uiChat.Server_SendSystemChatMessage(
                            $"{messagePrefix} Vote to <b>pause</b> in progress ({pauseVotes.Count}/{needed})."
                            + " Use <b>/votepause</b> or <b>/vp</b> to vote."
                        );
                    }
                    else
                    {
                        Plugin.Log.LogDebug($"{clientId} tried to vote to pause but they already voted recently.");
                        uiChat.Server_SendClientSystemChatMessage($"{messagePrefix} You already voted to <b>pause</b> recently.", clientId);
                    }
                    break;
                case "/voteresume":
                case "/vr":
                    Plugin.Log.LogDebug($"ClientID {clientId} voted to resume at {now}.");
                    resumeVotes = resumeVotes
                        .Where(pair => now.Subtract(pair.Value).Seconds < timeoutSeconds)
                        .ToDictionary(pair => pair.Key, pair => pair.Value);
                    bool alreadyVotedResume = resumeVotes.ContainsKey(clientId);
                    resumeVotes[clientId] = now;
                    if (resumeVotes.Count >= needed)
                    {
                        Plugin.Log.LogDebug($"Vote to resume passed. [{resumeVotes.Count}/{needed}]");
                        uiChat.Server_SendSystemChatMessage($"{messagePrefix} Vote passed - resuming! ({resumeVotes.Count}/{needed})");
                        gameManager.Server_Resume();
                        resumeVotes.Clear();
                    }
                    else if (!alreadyVotedResume)
                    {
                        Plugin.Log.LogDebug($"Vote to resume in progress. [{resumeVotes.Count}/{needed}]");
                        uiChat.Server_SendSystemChatMessage(
                            $"{messagePrefix} Vote to <b>resume</b> in progress ({resumeVotes.Count}/{needed})."
                            + " Use <b>/voteresume</b> or <b>/vr</b> to vote."
                        );
                    }
                    else
                    {
                        Plugin.Log.LogDebug($"{clientId} tried to vote to resume but they already voted recently.");
                        uiChat.Server_SendClientSystemChatMessage($"{messagePrefix} You already voted to <b>resume</b> recently.", clientId);
                    }
                    break;
                default: return;
            }
        }
    }
}
