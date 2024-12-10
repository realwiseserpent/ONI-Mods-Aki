using HarmonyLib;
using ONITwitchLib;
using UnityEngine;

namespace SpookyPumpkinSO.Integration.TwitchMod
{
	public class HiddenRotFoodEvent() : HiddenEventBase(ID)
	{
		public const string ID = "RotFood";
		public const float CHANCE = 0.33f;

		public override Danger GetDanger() => Danger.Medium;

		public override void Run()
		{
			for (int i = 0; i < Components.Edibles.Items.Count; i++)
			{
				var food = Components.Edibles.Items[i];

				if (food.FoodInfo.CanRot
					&& !food.HasTag(GameTags.Preserved)
					&& Random.value < CHANCE
					&& food.TryGetComponent(out PrimaryElement primaryElement))
				{
					if (primaryElement.Mass > 1f)
					{
						var mass = primaryElement.Mass * 0.33f;
						var chunk = food.GetComponent<Pickupable>().Take(mass);

						Rot(chunk.gameObject);
					}
					else
						Rot(food.gameObject);
				}
			}

			ToastManager.InstantiateToast(STRINGS.UI.SPOOKYPUMPKIN.TWITCHEVENTS.TRICKORTREAT.TRICK, STRINGS.UI.SPOOKYPUMPKIN.TWITCHEVENTS.ROTFOOD.TOAST_BODY);
		}

		private static void Rot(GameObject food)
		{
			if (food == null)
				return;

			var rottable = food.GetSMI<Rottable.Instance>();

			if (rottable == null)
				return;


            Traverse traverse = Traverse.Create(rottable).Field("smi");
            KAnim.PlayMode s = (KAnim.PlayMode)traverse.GetValue();

            KPrefabID component = food.GetComponent<KPrefabID>();
			if (!component.HasAnyTags(new Tag[3]
			{
				GameTags.Preserved,
				GameTags.Dehydrated,
				GameTags.Entombed
			}))
				return;
                //if (rottable.IsInsideState((Rottable)rottable.sm.Preserved))
				//return;

            rottable.RotValue = 1f;
			rottable.GoTo(food.GetComponent<Rottable>().Spoiled);

			Game.Instance.SpawnFX(SpawnFXHashes.OxygenEmissionBubbles, Grid.PosToCell(rottable), 0);
		}

		public override int GetNiceness() => Intent.EVIL;
	}
}
