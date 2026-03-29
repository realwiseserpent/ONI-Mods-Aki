using HarmonyLib;

namespace StarterColony.Patches
{
	public class BionicUpgradeMonitorPatch
	{
		[HarmonyPatch(typeof(BionicUpgradesMonitor), "SpawnAndInstallInitialUpgrade")]
		public class BionicUpgradesMonitor_SpawnAndInstallInitialUpgrade_Patch
		{
			public static void Postfix(BionicUpgradesMonitor __instance, BionicUpgradesMonitor.Instance smi)
			{
				foreach (var storage in smi.master.gameObject.GetComponents<Storage>())
				{
					foreach (var item in storage.items)
						item.GetComponent<KPrefabID>().AddTag(SC_ColonyManager.startGear, true);
				}
			}
		}
	}
}
