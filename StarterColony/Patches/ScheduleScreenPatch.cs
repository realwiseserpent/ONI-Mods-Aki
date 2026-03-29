using HarmonyLib;

namespace StarterColony.Patches
{
	public class ScheduleScreenPatch
	{
		[HarmonyPatch(typeof(ScheduleScreen), "OnSchedulesChanged")]
		public class ScheduleScreen_OnSchedulesChanged_Patch
		{
			public static bool Prefix() => !SC_ColonyManager.isEditingSchedules;
		}
	}
}
