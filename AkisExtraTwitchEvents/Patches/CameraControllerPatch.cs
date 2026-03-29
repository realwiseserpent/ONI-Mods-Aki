using HarmonyLib;
using Twitchery.Content.Scripts;
using Twitchery.Content.Scripts.Visual;

namespace Twitchery.Patches
{
	public class CameraControllerPatch
	{
		[HarmonyPatch(typeof(CameraController), "OnPrefabInit")]
		public class CameraController_OnPrefabInit_Patch
		{
			public static void Postfix(CameraController __instance)
			{
				var gameObject = __instance.baseCamera.gameObject;
				gameObject.AddComponent<AETE_DitherPostFx>();
				AkisTwitchEvents.Instance.eggFx = gameObject.AddComponent<AETE_EggPostFx>();
				AkisTwitchEvents.Instance.camEffects = gameObject.AddComponent<AETE_CameraEffects>();
			}
		}
	}
}
