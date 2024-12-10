using FUtility;
using UnityEngine;

namespace Twitchery.Content.Scripts.RegularPipChores
{
	public class RegularPipEdible : Workable
	{
		[SerializeField] public float kcalPerKg;

        protected override void OnSpawn()
		{
			base.OnSpawn();
			SetWorkTime(10f);
			showProgressBar = false;
			synchronizeAnims = false;
			GetComponent<KSelectable>().SetStatusItem(Db.Get().StatusItemCategories.Main, Db.Get().BuildingStatusItems.Normal);
			CreateChore();
		}

		private void CreateChore()
		{
			Log.Debuglog("creating chore");
			new RegularPipEatChore(this);
		}

        protected override void OnCompleteWork(Worker worker)
		{
			base.OnCompleteWork(worker);

			new RegularPipEatChore(this);
		}
	}
}
