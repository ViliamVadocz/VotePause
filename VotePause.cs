using HarmonyLib;

namespace VotePause;

public class VotePause
{

    [HarmonyPatch(typeof(UIChatController), nameof(UIChatController.Event_Server_OnChatCommand))]
    public static class UIChatControllerEventServerOnChatCommandPatch
    {
        [HarmonyPrefix]
        public static void Event_Server_OnChatCommand_Patch(
            Il2CppSystem.Collections.Generic.Dictionary<string, Il2CppSystem.Object> message)
        {
            Plugin.Log.LogInfo($"Patch: Event_Server_OnChatCommand_Patch was called.");
            if (message == null)
            {
                Plugin.Log.LogError("Received null message.");
                return;
            }

            Plugin.Log.LogInfo($"message dict: {message.ToString()}");

            ulong clientId = (ulong)Il2CppSystem.Convert.ToInt64(message["clientId"]);
            Plugin.Log.LogInfo($"clientId: {clientId}");
            string command = message["command"].ToString();
            Plugin.Log.LogInfo($"command: {command}");

            PlayerManager playerManager = NetworkBehaviourSingleton<PlayerManager>.Instance;
            GameManager gameManager = NetworkBehaviourSingleton<GameManager>.Instance;

            if (gameManager == null)
            {
                Plugin.Log.LogError("Game Manager was null.");
                return;
            }
            if (playerManager == null)
            {
                Plugin.Log.LogError("Player manager was null.");
                return;
            }

            Player player = playerManager.GetPlayerByClientId(clientId);
            Plugin.Log.LogInfo($"player: {player}");
            if (command.Equals("/votepause") || command.Equals("/vp"))
            {
                Plugin.Log.LogInfo($"PAUSING!!!");
                gameManager.Server_Pause();
            }
            else if (command.Equals("/voteresume") || command.Equals("/vr"))
            {
                Plugin.Log.LogInfo($"RESUMING!!!");
                gameManager.Server_Resume();
            }
        }
    }
}
