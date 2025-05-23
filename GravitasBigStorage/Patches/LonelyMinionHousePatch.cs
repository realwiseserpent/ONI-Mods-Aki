﻿using GravitasBigStorage.Content;
using HarmonyLib;

namespace GravitasBigStorage.Patches
{
	public class LonelyMinionHousePatch
	{
		[HarmonyPatch(typeof(LonelyMinionHouse.Instance), "OnCompleteStorySequence")]
		public class LonelyMinionHouse_Instance_OnCompleteStorySequence_Patch
		{
			public static void Postfix(LonelyMinionHouse.Instance __instance)
			{
				if (__instance.gameObject.TryGetComponent(out GBS_UnlockableTechs unlockable))
					unlockable.OnStoryComplete();
			}
		}
	}
}
