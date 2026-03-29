using FUtility.FUI;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace StarterColony.Patches
{
	public class GameOptionsScreenPatch
	{
		[HarmonyPatch(typeof(GameOptionsScreen), "OnSpawn")]
		public class GameOptionsScreen_OnSpawn_Patch
		{
			public static void Prefix(GameOptionsScreen __instance)
			{
				var line = __instance.sandboxButton.transform.parent;

				var colonyButton = Object.Instantiate(line);
				colonyButton.gameObject.SetActive(true);

				//var button = colonyButton.GetComponentInChildren<KButton>();

				/*				button.onClick += () =>
								{
									SC_ColonyManager.Instance.open = true;
								};*/
				var button = colonyButton.GetComponentInChildren<KButton>();
				var toggle = button.gameObject.AddComponent<FToggleUpdated>();
				toggle.mark = colonyButton.transform.Find("Checkbox/CheckMark").GetComponent<Image>();
				toggle.OnChange += OnToggled;

				colonyButton.transform.SetParent(line.parent);
				colonyButton.transform.localScale = line.transform.localScale;

				colonyButton.GetComponentInChildren<LocText>().text = "Colony Spawn Settings";
			}

			private static void OnToggled(bool on)
			{
				SC_ColonyManager.Instance.open = on;
			}
		}

		public class FToggleUpdated : FToggle2
		{
			private bool previousState;

			protected override void OnSpawn()
			{
				base.OnSpawn();
				SetOnWithoutTrigger(SC_ColonyManager.Instance.open);
			}

			void Update()
			{
				if (SC_ColonyManager.Instance.open != previousState)
				{
					SetOnWithoutTrigger(SC_ColonyManager.Instance.open);
					previousState = SC_ColonyManager.Instance.open;
				}
			}
		}
	}
}
