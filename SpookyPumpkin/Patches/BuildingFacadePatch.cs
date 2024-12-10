using Database;
using FUtility;
using HarmonyLib;
using SpookyPumpkinSO.Content;

namespace SpookyPumpkinSO.Patches
{
	public class BuildingFacadePatch
	{
		[HarmonyPatch(typeof(BuildingFacade), "ChangeBuilding")]
		public class BuildingFacade_ChangeBuilding_Patch
		{
			public static void Prefix(BuildingFacade __instance, ref object __state)
			{
				if (__instance.CurrentFacade != SPFacades.PUMPKINBED)
					return;

				if (__instance.TryGetComponent(out KBatchedAnimController kbac))
				{
                    Traverse traverse = Traverse.Create(kbac).Field("mode");
                    KAnim.PlayMode s = (KAnim.PlayMode)traverse.GetValue();

					__state = new AnimState()
					{
						anim = kbac.currentAnim,
						mode = s,//kbac.mode,
						hasValue = true
					};

					Log.Debug($"saved state: {kbac.currentAnim} {HashCache.Get().Get(kbac.currentAnim)}");
					Log.Debug(__instance.CurrentFacade);
				}
			}

			public static void Postfix(BuildingFacade __instance, ref object __state)
			{
				if (__instance.CurrentFacade != SPFacades.PUMPKINBED)
					return;

				if (__instance.TryGetComponent(out BuildingUnderConstruction _))
				{
					if (__instance.TryGetComponent(out KBatchedAnimController kbac))
					{
						kbac.Play("place");
					}
				}
				else if (__state is AnimState animState && animState.hasValue)
				{
					Log.Debug("restored state");
					if (__instance.TryGetComponent(out KBatchedAnimController kbac))
					{
						kbac.Play(HashCache.Get().Get(animState.anim), animState.mode);
					}
				}
			}
		}

		public struct AnimState
		{
			public HashedString anim;
			public KAnim.PlayMode mode;
			public bool hasValue;
		}
	}
}
