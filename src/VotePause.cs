using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace VotePause;

public class VotePause
{
    private const string MESSAGE_PREFIX = $"<color=orange><b>{Mod.NAME}</b></color>";
    private const string HELP_MESSAGE = $"commands:\n"
        + "* <b>/votepause</b> (/vp) - Vote to pause\n"
        + "* <b>/voteresume</b> (/vr) - Vote to resume";


    private const uint TIMEOUT_SECONDS = 60;
    private static Dictionary<ulong, DateTime> pauseVotes = [];
    private static Dictionary<ulong, DateTime> resumeVotes = [];

    private static uint PlayersNeeded(uint totalPlayers)
    {
        return (totalPlayers + 1) / 2;
    }

    private static void sendMessage(UIChat uiChat, object message)
    {
        uiChat.Server_SendSystemChatMessage($"{MESSAGE_PREFIX} {message}");
    }
    private static void sendMessage(UIChat uiChat, object message, ulong clientId)
    {
        uiChat.Server_SendSystemChatMessage($"{MESSAGE_PREFIX} {message}", clientId);
    }

    [HarmonyPatch(typeof(UIChatController), "Event_Server_OnChatCommand")]
    public static class UIChatControllerEventServerOnChatCommandPatch
    {
        static readonly FieldInfo _uiChat = typeof(UIChatController).GetField("uiChat", BindingFlags.Instance | BindingFlags.NonPublic);

        [HarmonyPrefix]
        public static void Event_Server_OnChatCommand(
            UIChatController __instance,
            Dictionary<string, object> message
        )
        {
            if (__instance == null) return;
            if (message == null) return;
            if (!message.ContainsKey("command")) return;
            if (!message.ContainsKey("clientId")) return;

            PlayerManager playerManager = NetworkBehaviourSingleton<PlayerManager>.Instance;
            GameManager gameManager = NetworkBehaviourSingleton<GameManager>.Instance;
            UIChat uiChat = (UIChat)_uiChat.GetValue(__instance);

            if (playerManager == null) return;
            if (gameManager == null) return;
            if (uiChat == null) return;

            uint totalPlayers = (uint)playerManager.GetPlayers(false).Count;
            uint needed = PlayersNeeded(totalPlayers);

            string command = message["command"].ToString().ToLower();
            ulong clientId = (ulong)message["clientId"];
            DateTime now = DateTime.UtcNow;

            switch (command)
            {
                case "/help":
                    sendMessage(uiChat, HELP_MESSAGE, clientId);
                    break;
                case "/votepause":
                case "/vp":
                    Mod.LogDebug($"ClientID {clientId} voted to pause at {now}.");
                    pauseVotes = pauseVotes
                        .Where(pair => now.Subtract(pair.Value).Seconds < TIMEOUT_SECONDS)
                        .ToDictionary(pair => pair.Key, pair => pair.Value);
                    bool alreadyVotedPause = pauseVotes.ContainsKey(clientId);
                    pauseVotes[clientId] = now;
                    if (pauseVotes.Count >= needed)
                    {
                        Mod.LogDebug($"Vote to pause passed. [{pauseVotes.Count}/{needed}]");
                        sendMessage(uiChat, $" Vote passed - pausing! ({pauseVotes.Count}/{needed})");
                        gameManager.Server_Pause();
                        pauseVotes.Clear();
                    }
                    else if (!alreadyVotedPause)
                    {
                        sendMessage(uiChat,
                            $"Vote to <b>pause</b> in progress ({pauseVotes.Count}/{needed})."
                            + " Use <b>/votepause</b> or <b>/vp</b> to vote."
                        );
                    }
                    else
                    {
                        Mod.LogDebug($"{clientId} tried to vote to pause but they already voted recently.");
                        sendMessage(uiChat, $" You already voted to <b>pause</b> recently.", clientId);
                    }
                    break;
                case "/voteresume":
                case "/vr":
                    Mod.LogDebug($"ClientID {clientId} voted to resume at {now}.");
                    resumeVotes = resumeVotes
                        .Where(pair => now.Subtract(pair.Value).Seconds < TIMEOUT_SECONDS)
                        .ToDictionary(pair => pair.Key, pair => pair.Value);
                    bool alreadyVotedResume = resumeVotes.ContainsKey(clientId);
                    resumeVotes[clientId] = now;
                    if (resumeVotes.Count >= needed)
                    {
                        Mod.LogDebug($"Vote to resume passed. [{resumeVotes.Count}/{needed}]");
                        sendMessage(uiChat, $" Vote passed - resuming! ({resumeVotes.Count}/{needed})");
                        gameManager.Server_Resume();
                        resumeVotes.Clear();
                    }
                    else if (!alreadyVotedResume)
                    {
                        Mod.LogDebug($"Vote to resume in progress. [{resumeVotes.Count}/{needed}]");
                        sendMessage(uiChat,
                            $"Vote to <b>resume</b> in progress ({resumeVotes.Count}/{needed})."
                            + " Use <b>/voteresume</b> or <b>/vr</b> to vote."
                        );
                    }
                    else
                    {
                        Mod.LogDebug($"{clientId} tried to vote to resume but they already voted recently.");
                        sendMessage(uiChat, $" You already voted to <b>resume</b> recently.", clientId);
                    }
                    break;
                default: return;
            }
        }
    }
}
