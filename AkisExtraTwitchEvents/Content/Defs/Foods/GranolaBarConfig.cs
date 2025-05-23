﻿using UnityEngine;

namespace Twitchery.Content.Defs.Foods
{
	public class GranolaBarConfig : IEntityConfig
	{
		public const string ID = "AkisExtraTwitchEvents_GranolaBar";

		public GameObject CreatePrefab()
		{
			var prefab = EntityTemplates.CreateLooseEntity(
				ID,
				STRINGS.ITEMS.FOOD.AKISEXTRATWITCHEVENTS_GRANOLABAR.NAME,
				STRINGS.ITEMS.FOOD.AKISEXTRATWITCHEVENTS_GRANOLABAR.DESC,
				1f,
				true,
				Assets.GetAnim("aete_granolabar_kanim"),
				"object",
				Grid.SceneLayer.Creatures,
				EntityTemplates.CollisionShape.RECTANGLE,
				1f,
				0.45f,
				true);

			EntityTemplates.ExtendEntityToFood(prefab, TFoodInfos.granolaBar);

#if SUPERPIP
			prefab.AddOrGet<RegularPipEdible>().kcalPerKg = TFoodInfos.granolaBar.CaloriesPerUnit;
#endif

			return prefab;
		}

		public string[] GetDlcIds() => null;

		public void OnPrefabInit(GameObject _) { }

		public void OnSpawn(GameObject _) { }
	}
}
