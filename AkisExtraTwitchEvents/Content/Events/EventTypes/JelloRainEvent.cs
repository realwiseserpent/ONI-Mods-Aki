﻿using ONITwitchLib;
using Twitchery.Content.Scripts;
using UnityEngine;

namespace Twitchery.Content.Events.EventTypes
{
	public class JelloRainEvent() : TwitchEventBase(ID)
	{
		public const string ID = "JelloRain";

		public override int GetWeight() => Consts.EventWeight.Uncommon;

		public override Danger GetDanger() => Danger.Small;

		public override void Run()
		{
			var go = new GameObject("jello cloud spawner");

			var rain = go.AddComponent<LiquidRainSpawner>();

			var danger = (float)AkisTwitchEvents.MaxDanger / 6f;
			var minKcal = Mathf.Lerp(5000, 30000, danger);
			var maxKcal = Mathf.Lerp(10000, 60000, danger);

			rain.totalAmountRangeKg = (minKcal / TFoodInfos.JELLO_KCAL_PER_KG, maxKcal / TFoodInfos.JELLO_KCAL_PER_KG);
			rain.durationInSeconds = 240;
			rain.dropletMassKg = 0.01f;
			rain.spawnRadius = 10;

			rain.AddElement(Elements.Jello);

			go.SetActive(true);

			AudioUtil.PlaySound(ModAssets.Sounds.SPLAT, ModAssets.GetSFXVolume() * 0.15f); // its loud
			GameScheduler.Instance.Schedule("jello rain", 3f, _ =>
			{
				rain.StartRaining();
			});

			DiscoveredResources.Instance.Discover(Elements.Jello.Tag);

			ToastManager.InstantiateToastWithPosTarget(
				STRINGS.AETE_EVENTS.JELLORAIN.TOAST,
				STRINGS.AETE_EVENTS.JELLORAIN.DESC,
				ONITwitchLib.Utils.PosUtil.ClampedMouseCellWorldPos());
		}
	}
}
