using KSerialization;
using System.Collections;
using UnityEngine;

namespace Twitchery.Content.Scripts
{
	[SerializationConfig(MemberSerialization.OptIn)]
	public class MidasEntityContainer : KMonoBehaviour, ISim200ms
	{
		[MyCmpReq] protected MinionStorage minionStorage;
		[MyCmpReq] protected Storage storage;
		[MyCmpReq] protected KBatchedAnimController kbac;
		[MyCmpReq] protected SymbolOverrideController controller;
		[MyCmpReq] protected KBoxCollider2D collider;

		[Serialize] public float timeRemaining;
		[Serialize] public bool releaseOnTimer;
		[Serialize] public string sprite;
		[Serialize] public bool storedAnything;
		[Serialize] public bool storedMinion;
		[Serialize] public HashedString currentAnim;
		[Serialize] public float positionPercent;

		private bool restoreAnim = true;

		public static readonly Tag[] ignoreTags =
		[
			TTags.midased,
			TTags.midasSafe,
			GameTags.Stored,
			GameTags.Dead,
			GameTags.Minions.Models.Bionic
		];

		public virtual StatusItem GetStatusItem() => TStatusItems.GoldStruckStatus;

		public virtual Color GetOverlayColor() => Color.yellow;

		public override void OnSpawn()
		{
			base.OnSpawn();
			GetComponent<KSelectable>().AddStatusItem(GetStatusItem(), this);

			if (restoreAnim && (storedAnything || storedMinion))
				StartCoroutine(RestoreAnim());

			Mod.midasContainers.Add(this);
		}

		public override void OnCleanUp()
		{
			base.OnCleanUp();
			Mod.midasContainers.Remove(this);
		}

		public virtual void StoreMinion(MinionIdentity identity, float duration)
		{
			if (storedMinion)
				return;

			if (Components.LiveMinionIdentities.GetWorldItems(this.GetMyWorldId()).Count <= 1)
			{
				PopFXManager.Instance.SpawnFX(PopFXManager.Instance.sprite_Building, STRINGS.UI.AKIS_EXTRA_TWITCH_EVENTS.RESISTED, identity.transform);

				return;
			}

			StartCoroutine(StoreCoroutine(identity, duration));
		}


		private float elapsedTime = 0.0f;

		private IEnumerator StoreCoroutine(MinionIdentity identity, float duration)
		{
			elapsedTime = 0.0f;

			var reactionMonitor = identity.GetSMI<ReactionMonitor.Instance>();

			if (reactionMonitor == null)
			{
				Object.Destroy(gameObject);
				yield return null;
			}

			while (reactionMonitor.IsInsideState(reactionMonitor.sm.reacting))
			{
				elapsedTime += Time.deltaTime;

				if (elapsedTime > 10.0f)
				{
					// waited a long time but the dupe is too busy to be frozen
					Object.Destroy(gameObject);
					yield return null;
				}

				yield return SequenceUtil.waitForEndOfFrame;
			}

			Store(identity.gameObject, duration, false);
			minionStorage.SerializeMinion(identity.gameObject);

			storedMinion = true;
			restoreAnim = false;

			Mod.midasContainersWithDupes.Add(this);
		}

		public virtual void StoreCritter(GameObject critter, float duration)
		{
			if (storedAnything)
				return;

			Store(critter, duration, false);
			storage.Store(critter, true);

			storedAnything = true;
			restoreAnim = false;
		}

		private IEnumerator RestoreAnim()
		{
			var item = Release(false, "Restoring Animation");

			yield return new WaitForEndOfFrame();

			if (item != null)
			{
				if (item.TryGetComponent(out MinionIdentity identity))
					minionStorage.SerializeMinion(identity.gameObject);
				else
					storage.Store(item, true);

				Store(item, timeRemaining, true);
			}
		}

		private void Store(GameObject go, float duration, bool force)
		{
			if (!force && go.HasAnyTags(ignoreTags))
				return;

			CopyAnim(go);
			CopyCollider(go);
			timeRemaining = duration;
			releaseOnTimer = duration > 0;

			if (TryGetComponent(out KSelectable kSelectable))
				kSelectable.SetName(go.GetProperName());

			if (go.TryGetComponent(out KPrefabID kPrefabID))
				kPrefabID.AddTag(TTags.midased, true);

			storedAnything = true;
		}

		private void CopyCollider(GameObject go)
		{
			if (go.TryGetComponent(out KBoxCollider2D collider))
			{
				this.collider.offset = collider.offset;
				this.collider.size = collider.size;
			}
		}

		protected virtual void OnAnimationUpdated(GameObject original)
		{
		}

		private void CopyAnim(GameObject original)
		{
			original.TryGetComponent(out KBatchedAnimController originalKbac);

			kbac.SwapAnims(originalKbac.animFiles);

			original.TryGetComponent(out KBatchedAnimController mKbac);
			original.TryGetComponent(out SymbolOverrideController mController);

			if (mKbac.OverrideAnimFiles != null)
			{
				foreach (var animOverride in mKbac.OverrideAnimFiles)
				{
					kbac.AddAnimOverrides(animOverride.file, animOverride.priority);
				}
			}

			foreach (var symbol in mController.symbolOverrides)
			{
				controller.AddSymbolOverride(symbol.targetSymbol, symbol.sourceSymbol);
			}

			if (mKbac.animFiles != null)
			{
				foreach (var mAnim in mKbac.animFiles)
				{
					foreach (var anim in kbac.animFiles)
					{
						if (anim?.data.name == anim.name)
						{
							var build = anim.GetData().build;

							foreach (var symbol in build.symbols)
							{
								kbac.SetSymbolVisiblity(symbol.hash, mKbac.GetSymbolVisiblity(symbol.hash));
							}

							break;
						}
					}
				}
			}

			if (!currentAnim.IsValid)
			{
				currentAnim = originalKbac.currentAnim;
				positionPercent = originalKbac.GetPositionPercent();
			}

			kbac.Play(currentAnim, KAnim.PlayMode.Paused);
			kbac.SetPositionPercent(positionPercent);
			kbac.animScale = originalKbac.animScale;
			kbac.TintColour = GetOverlayColor();
			kbac.offset = originalKbac.offset;
			kbac.FlipX = originalKbac.flipX;
			kbac.FlipY = originalKbac.flipY;
			kbac.Rotation = originalKbac.Rotation;

			OnAnimationUpdated(original);
		}

		public GameObject Release(bool removeTag, string reason)
		{
			Log.Debug($"releasing stored item {reason}");
			if (removeTag)
			{
				foreach (var item in storage.items)
					item.RemoveTag(TTags.midased);
			}

			if (storage.items != null && storage.items.Count > 0)
			{
				var go = storage.items[0];
				storage.DropAll();

				return go;
			}

			if (minionStorage.serializedMinions.Count > 0)
			{
				if (minionStorage.serializedMinions.Count > 1)
					Log.Warning("There is somehow multiple dupes in this midas state. ignoring all but the first");

				var minionGo = minionStorage.DeserializeMinion(minionStorage.serializedMinions[0].id, transform.position);

				if (removeTag)
					minionGo.RemoveTag(TTags.midased);

				Mod.midasContainersWithDupes.Remove(this);

				return minionGo;
			}

			return null;
		}

		public bool HasStoredItem() => storedAnything || storedMinion;

		public void Sim200ms(float dt)
		{
			var storedItem = storedAnything && !storedMinion;

			if (storedItem && storage.IsEmpty())
			{
				Release(true, "Empty Storage for item");
				Util.KDestroyGameObject(gameObject);

				return;
			}


			if (storedMinion && (minionStorage.serializedMinions == null || minionStorage.serializedMinions.Count == 0))
			{
				Release(true, "Empty MinionStorage for minion");
				Util.KDestroyGameObject(gameObject);

				return;
			}

			timeRemaining -= dt;

			if (releaseOnTimer && timeRemaining <= 0)
			{
				Release(true, "Duration Expired");
				Util.KDestroyGameObject(gameObject);
			}

			kbac.TintColour = GetOverlayColor(); // need to reapply because the game randomly clears it
		}

		public string GetTimeLeft() => GameUtil.GetFormattedTime(timeRemaining);
	}
}
