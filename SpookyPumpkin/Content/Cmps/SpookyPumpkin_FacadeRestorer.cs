using FUtility;
using HarmonyLib;
using KSerialization;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpookyPumpkinSO.Content.Cmps
{
	// forces the game to save without modded facades, to loading the save without my mod doesn't softlock it
	public class SpookyPumpkin_FacadeRestorer : KMonoBehaviour
	{
		[Serialize] public string facadeID;
		[SerializeField] public Func<SpookyPumpkin_FacadeRestorer, string> playAnimCb;

		[MyCmpReq] public BuildingFacade buildingFacade;
		[MyCmpReq] public KBatchedAnimController kbac;

        protected override void OnSpawn()
		{
			base.OnSpawn();

			Mod.facadeRestorers.Add(this);

			if (!facadeID.IsNullOrWhiteSpace())
			{
				var facade = Db.GetBuildingFacades().TryGet(facadeID);

				if (facade != null)
				{
					buildingFacade.ApplyBuildingFacade(facade);

					if (playAnimCb != null)
						kbac.Play(playAnimCb(this));
				}
				else
					Log.Warning($"tried to restore facade {facadeID}, but it no longer seems to exist. restoring to default.");
			}
		}

        protected override void OnCleanUp()
		{
			base.OnCleanUp();
			Mod.facadeRestorers.Remove(this);
		}

		public void OnSaveGame()
		{
			if (SPFacades.myFacades.Contains(buildingFacade.CurrentFacade))
			{
				facadeID = buildingFacade.CurrentFacade;

                Traverse traverse = Traverse.Create(buildingFacade).Field("currentFacade");
                traverse.SetValue(null);

                //buildingFacade.CurrentFacade = null;
			}
			else
				facadeID = null;
		}

		public void AfterSave()
		{
			if (facadeID != null)
			{
                Traverse traverse = Traverse.Create(buildingFacade).Field("currentFacade");
                traverse.SetValue(facadeID);
                //buildingFacade.CurrentFacade = facadeID;
			}
		}
	}
}
