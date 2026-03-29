using HarmonyLib;

namespace StarterColony.Patches
{
	public class GameFlowManagerPatch
	{
		[HarmonyPatch(typeof(GameFlowManager.StatesInstance), "CheckForGameOver")]
		public class GameFlowManager_StatesInstance_CheckForGameOver_Patch
		{
			public static bool Prefix()
			{
				if (Game.Instance.GameStarted() && SC_ColonyManager.Instance != null && SC_ColonyManager.Instance.isRegenerating)
					return false;

				return true;
			}
		}
	}
}
