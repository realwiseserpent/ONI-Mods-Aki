using FUtility;
using HarmonyLib;
using KSerialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TUNING;
using UnityEngine;

namespace StarterColony
{
	public class SC_ColonyManager : KMonoBehaviour
	{
		public static SC_ColonyManager Instance { get; private set; }
		[Serialize] public bool open;
		public int dupeCount = 50;

		private string countStr = "";
		private string[] traitOptions;
		private TraitEntry[] traitEntries;

		private bool showTraitSelection;
		private int selectedTrait = -1;
		private bool regenerate = true;
		private float bionics = 0.1f;
		private bool bionicsDlc = false;

		public int nameGen = (int)NamingScheme.RandomSurName;

		private int dupePerScheduleBlock = 8;
		private string dupePerScheduleBlockStr = "8";

		public bool isRegenerating;

		public static Tag startGear = TagManager.Create("SC_StartGear");

		private Texture2D blackBg;
		private Texture2D lightBg;

		private static HashSet<string> disabledTraitsDefault = [
			"Flatulence",
			"Allergies",
			"Narcolepsy"
			];

		public enum NamingScheme
		{
			None,
			RandomSurName,
			Numerical,
			Interest,
			//HighestStat
		}

		private static Dictionary<string, int> numericalCounters;

		private List<Schedule> createdSchedules;

		static int[] values = [1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1];
		static string[] symbols = ["M", "CM", "D", "CD", "C", "XC", "L", "XL", "X", "IX", "V", "IV", "I"];

		public static bool isEditingSchedules;

		static string ToRoman(int num)
		{
			var result = "";

			for (var i = 0; i < values.Length; i++)
			{
				while (num >= values[i])
				{
					result += symbols[i];
					num -= values[i];
				}
			}

			return result;
		}

		private static Dictionary<HashedString, string> skillSuffixes = new Dictionary<HashedString, string>()
		{
			{ "Art", "Artist" },
			{ "Basekeeping", "Janitor" },
			{ "Building", "Builder" },
			{ "Cooking", "Chef" },
			{ "Farming", "Farmer" },
			{ "Hauling", "Supplier" },
			{ "MedicalAid", "Doctor" },
			{ "Ranching", "Rancher" },
			{ "Research", "Smarts" },
			{ "Rocketry", "Spaceman" },
			{ "Mining", "Moleman" },
			{ "Suits", "Suits" },
			{ "Technicals", "Operator" }
		};

		private static List<string> NAMES = [
			"Dupe",
			"Red", "Blue", "Yellow", "Green", "Bird", "The Great", "Smith", "Pip", "Bammoth", "Slick", "Puft",
			"Pawbert", "Solo", "Kirk", "Picard", "Data", "Starr", "Crumbs", "Deckard", "Reese", "Ripley", "Stark", "Banner", "Ender", "Who", "Wallace", "Tyrell", "Lightyear", "Fuschia", "the the Boy", "Jinwoo", "Yesman", "Gullible", "Marmalade", "Unicorn", "Iplier",
			"Maxwell", "Woby", "Chester", "Worcestershire", "Merm", "Pengull", "Slurtle", "Bearger", "Varg", "Fox", "Doydoy", "Wobster", "Piko", "Hutch", "Mango", "Ananas", "Doe", "Dupeman", "Dork", "Polka", "Dot", "Lime", "Lemon", "Melon", "Hazelnut", "Mozzarella", "Gem", "Diamond", "McDupeFace", "Pepperoni", "Updog", "Stunseed"
			];

		private static Dictionary<string, HashSet<string>> NAMES_BY_DUPE;

		private static string DEFAULT_SKILL = "";

		private static string GetSuffixForSkill(HashedString skillId)
		{
			if (skillSuffixes.TryGetValue(skillId, out var suffix))
				return suffix;

			return DEFAULT_SKILL;
		}

		private static List<string> nameSuffixes;

		private static string GetName(string originalName, MinionIdentity identity, NamingScheme scheme)
		{
			if (originalName == null)
				originalName = "Bob";

			if (numericalCounters == null)
			{
				Log.Warning("numerical counters is null");
				numericalCounters = new Dictionary<string, int>();
			}

			var resume = identity.GetComponent<MinionResume>();
			var suffixes = new List<string>();

			switch (scheme)
			{
				case NamingScheme.Numerical:
					if (!numericalCounters.TryGetValue(originalName, out var number))
					{
						numericalCounters.Add(originalName, 0);
						return originalName; // I does not need
					}

					numericalCounters[originalName]++;
					return $"{originalName} {ToRoman(number + 2)}";

				case NamingScheme.Interest:
					foreach (var aptitude in resume.AptitudeBySkillGroup)
					{
						if (aptitude.Value > 0.0f)
						{
							suffixes.Add(GetSuffixForSkill(aptitude.Key));
						}
					}

					if (suffixes.Count > 0)
						originalName += $" {suffixes.Join(null, "-")}";

					return originalName;
				/*
								case NamingScheme.HighestStat:

													foreach (var aptitude in resume.GetAttributes().AttributeTable)
													{
														if (aptitude.Value > 0.0f)
														{
															suffixes.Add(GetSuffixForSkill(aptitude.Key));
														}
													}

													if (suffixes.Count > 0)
														originalName += $" {suffixes.Join(null, "-")}";
									return originalName;
								*/
				case NamingScheme.RandomSurName:

					NAMES_BY_DUPE ??= [];

					if (!NAMES_BY_DUPE.ContainsKey(originalName))
					{
						NAMES_BY_DUPE.Add(originalName, [.. NAMES]);
					}

					if (NAMES_BY_DUPE[originalName].Count == 0)
					{
						NAMES_BY_DUPE[originalName] = [.. NAMES];
					}

					var name = NAMES_BY_DUPE[originalName].GetRandom();
					NAMES_BY_DUPE.Remove(name); ;

					return $"{originalName} {name}";
				case NamingScheme.None:
				default:
					return originalName;
			}
		}

		private List<TraitEntry> traitsForbidden;

		private GUIStyle darkBox;

		private class TraitEntry
		{
			public string traitName;
			public HashedString traitId;
			public int traitIdx;
		}

		public override void OnPrefabInit()
		{
			Instance = this;

			Components.LiveMinionIdentities.OnAdd += OnDupeSPawned;
		}

		private static bool isGameReady;

		private void OnDupeSPawned(MinionIdentity identity)
		{
			isGameReady = true;
		}

		public override void OnCleanUp()
		{
			Instance = null;
		}

		public override void OnSpawn()
		{
			nameGenCount = Enum.GetNames(typeof(NamingScheme)).Length;

			var tex = new Texture2D(1, 1);
			tex.SetPixel(0, 0, new Color(0, 0, 0, 0.92f));
			tex.Apply();

			blackBg = tex;

			var tex2 = new Texture2D(1, 1);
			tex2.SetPixel(0, 0, new Color(1, 1, 1, 0.3f));
			tex2.Apply();

			lightBg = tex2;

			bionicsDlc = DlcManager.IsContentSubscribed(DlcManager.DLC3_ID);

			countStr = dupeCount.ToString();
			base.OnSpawn();

			var traits = DUPLICANTSTATS.BADTRAITS
				.Union(DUPLICANTSTATS.GOODTRAITS)
				.Select(x => x.id)
				.ToList();

			var traitsDb = Db.Get().traits;

			traitsForbidden = [];
			foreach (var disabled in disabledTraitsDefault)
			{
				var idx = traits.IndexOf(disabled);
				if (idx != -1)
				{
					var traitEntry = traitsDb.TryGet(disabled);
					if (traitEntry != null)
					{
						traitsForbidden.Add(new TraitEntry()
						{
							traitIdx = idx,
							traitName = traitEntry.GetName(),
							traitId = traitEntry.IdHash
						});
					}
				}
			}

			traitOptions = new string[traits.Count];
			traitEntries = new TraitEntry[traits.Count];

			for (var i = 0; i < traits.Count; i++)
			{
				var trait = traits[i];
				var t = traitsDb.TryGet(trait);
				traitOptions[i] = Util.StripTextFormatting(t.Name);
				traitEntries[i] = new TraitEntry()
				{
					traitId = t.IdHash,
					traitIdx = i,
					traitName = t.GetName(),
				};
			}
		}

		private Vector2 scroll;
		private int nameGenCount;

		void OnGUI()
		{
			if (!open || !Game.Instance.baseAlreadyCreated)
				return;

			darkBox = new GUIStyle(GUI.skin.box);
			darkBox.normal.background = blackBg;

			var lightBox = new GUIStyle(GUI.skin.box);
			lightBox.normal.background = lightBg;

			var height = 600.0f;
			var width = 315.0f;

			var x = Screen.width - (330.0f + width);
			var y = (Screen.height - height) / 2;

			GUILayout.BeginArea(new Rect(x, y, width, height), darkBox);


			GUILayout.Space(24);
			CloseButton(width, 0);

			//GUILayout.BeginArea(new Rect(x + 5, y + 5, width - 10, 200), lightBox);

			ColonySize();
			Bionics();

			//GUILayout.EndArea();

			GUILayout.Space(10);
			GUILayout.Label("Naming style:");

			GUILayout.BeginHorizontal();

			if (GUILayout.Button("<"))
			{
				nameGen--;
				if (nameGen == -1)
					nameGen = nameGenCount - 1;
			}

			GUILayout.Label(((NamingScheme)nameGen).ToString());

			GUILayout.Space(10);
			if (GUILayout.Button(">"))
			{
				nameGen++;
				if (nameGen >= nameGenCount)
					nameGen = 0;
			}


			GUILayout.EndHorizontal();

			GUI.contentColor = Color.gray;

			if (nameGen == (int)NamingScheme.None)
				GUILayout.Label("Rowan, Rowan, Rowan");
			else if (nameGen == (int)NamingScheme.Numerical)
				GUILayout.Label("Rowan, Rowan II, Rowan III");
			else if (nameGen == (int)NamingScheme.RandomSurName)
				GUILayout.Label("Rowan Smith, Rowan Dupe, Rowan Diamond");
			else if (nameGen == (int)NamingScheme.Interest)
				GUILayout.Label("Rowan Digger, Rowan Builder-Suits, Rowan Operator");
			//else if (nameGen == (int)NamingScheme.HighestStat)
			//	GUILayout.Label("Rowan Fast, Rowan Strong, Rowan Artsy");

			GUI.contentColor = Color.white;


			Traits();
			Regenerate();

			var currentAliveDupes = Components.LiveMinionIdentities.Count;

			ShowChangesPreview(currentAliveDupes);

			if (GUILayout.Button("Set Colony Size"))
			{
				isRegenerating = true;
				isEditingSchedules = true;

				var traitsDb = Db.Get().traits;
				traitsForbidden ??= [];
				var forbiddenTraits = traitsForbidden.Select(t =>
				{
					var trait = traitsDb.TryGet(t.traitId);
					if (trait == null)
					{
						Debug.LogWarning("null trait " + t.traitName);
						return null;
					}

					return trait.Id;
				})
					.Where(t => t != null)
					.ToList();

				if (currentAliveDupes < dupeCount || regenerate)
				{
					var cell = -1;
					var telepad = GameUtil.GetActiveTelepad();
					if (telepad == null)
						cell = SelectTool.Instance.selectedCell;
					else
						cell = Grid.PosToCell(telepad);

					var attempts = 512;

					var dupesToSpawn = regenerate ? dupeCount : dupeCount - currentAliveDupes;
					var dupesToKill = new List<MinionIdentity>(Components.LiveMinionIdentities.items);

					var minions = new List<MinionIdentity>();

					while (minions.Count < dupesToSpawn && attempts-- > 0)
					{
						if (SpawnDupe(cell, forbiddenTraits, out var spawnedDupe))
						{
							minions.Add(spawnedDupe);
						}
					}

					StartCoroutine(PostProcessCoroutine(minions));

					if (regenerate && dupesToKill.Count > 0)
					{
						for (var i = dupesToKill.Count - 1; i >= 0; i--)
						{
							StartCoroutine(KillCoroutine((dupesToKill[i])));
						}
					}
				}
				else if (currentAliveDupes > dupeCount)
				{
					var dupesToKill = currentAliveDupes - dupeCount;
					for (var i = 0; i < dupesToKill; i++)
					{
						var target = Components.LiveMinionIdentities.items.GetRandom();
						StartCoroutine(KillCoroutine(target));
					}
				}

				isRegenerating = false;
				isEditingSchedules = false;

				if (ScheduleScreen.Instance != null)
					ScheduleScreen.Instance.OnSchedulesChanged(ScheduleManager.Instance.schedules);
			}

			//SeparatorLine();
			Separator();

			MaxDupesPerSchedule();

			var scheduleCount = Mathf.CeilToInt(Components.LiveMinionIdentities.Count / dupePerScheduleBlock) + 1;

			if (GUILayout.Button("Auto Schedule"))
			{
				AutoScheduleDupes(scheduleCount);
			}

			var size = GUI.skin.label.fontSize;
			GUI.skin.label.fontSize = 10;
			GUILayout.Label($"The default schedule will be duplicated {scheduleCount} times, and shifted, then all dupes distributed.");
			GUI.skin.label.fontSize = size;

			if (DlcManager.IsContentSubscribed(DlcManager.DLC3_ID))
			{
				GUI.contentColor = Color.gray;
				GUILayout.Label("Bionic schedules not implemented yet.");
				GUI.contentColor = Color.white;
			}

			//SeparatorLine();
			Separator();

			GUI.contentColor = Color.gray;
			GUILayout.Label("You can always re-open this dialog from Pause menu -> Settings -> enable Colony Spawn Settings");
			GUI.contentColor = Color.white;

			GUILayout.EndArea();
		}

		private void AutoScheduleDupes(int scheduleCount)
		{
			isEditingSchedules = true;

			var minions = Components.LiveMinionIdentities.Items
				.Select(i => i.GetComponent<Schedulable>())
				.ToList();

			if (createdSchedules != null)
			{
				foreach (var schedule in createdSchedules)
					ScheduleManager.Instance.DeleteSchedule(schedule);
			}

			createdSchedules = [];

			var defaultSchedule = ScheduleManager.Instance.GetSchedules().FirstOrDefault();

			var blockPerDay = Mathf.CeilToInt(24.0f / scheduleCount);

			for (var scheduleIndex = 0; scheduleIndex < scheduleCount; scheduleIndex++)
			{
				Schedule schedule = null;

				if (defaultSchedule != null)
				{
					schedule = ScheduleManager.Instance.DuplicateSchedule(defaultSchedule);
					schedule.name = $"Auto - {scheduleIndex + 1}";
					schedule.alarmActivated = false;
				}
				else
				{
					Log.Debug("no default schedule");
					schedule = ScheduleManager.Instance.AddSchedule(Db.Get().ScheduleGroups.allGroups, $"Auto - {scheduleIndex + 1}", false);
				}

				createdSchedules.Add(schedule);

				for (var k = 0; k < blockPerDay * scheduleIndex; k++)
				{
					schedule.RotateBlocks(true, 0);
				}

				Log.Debug($"scheduling {dupePerScheduleBlock} per block");

				for (var j = 0; j < dupePerScheduleBlock; j++)
				{
					var index = j + dupePerScheduleBlock * scheduleIndex;
					Log.Debug($"index {index}");
					if (minions.Count > index)
					{
						Log.Debug("assigned " + index);
						///StartCoroutine(ScheduleCoroutine(schedule, minions[index]));
						///

						ScheduleManager.Instance.GetSchedule(minions[index]).Unassign(minions[index]);
						schedule.Assign(minions[index]);
					}
				}
			}

			isEditingSchedules = false;

			if (ScheduleScreen.Instance != null)
				ScheduleScreen.Instance.OnSchedulesChanged(ScheduleManager.Instance.schedules);
		}

		private IEnumerator PostProcessCoroutine(List<MinionIdentity> minions)
		{
			yield return SequenceUtil.waitForEndOfFrame;
			PostProcessDupes(minions);
		}

		private void PostProcessDupes(List<MinionIdentity> minions)
		{
			numericalCounters = [];

			foreach (var minion in minions)
			{
				if (minion != null)
					minion.SetName(GetName(minion.name, minion, (NamingScheme)nameGen));
			}
		}

		private void ShowChangesPreview(int currentAliveDupes)
		{
			if (currentAliveDupes < dupeCount)
			{
				GUILayout.Label($"{dupeCount - currentAliveDupes} dupes will be spawned.");
			}
			else if (currentAliveDupes == dupeCount)
			{
				GUILayout.Label($"No changes.");
			}
			else
			{
				GUILayout.Label($"{currentAliveDupes - dupeCount} dupes will be removed.");
			}
		}

		private void Regenerate()
		{
			regenerate = GUILayout.Toggle(regenerate, "Re-generate existing dupes");
		}

		private void Traits()
		{
			if (!showTraitSelection && GUILayout.Button("Add Forbidden Trait"))
			{
				showTraitSelection = true;
			}

			if (showTraitSelection && traitOptions != null)
			{
				scroll = GUILayout.BeginScrollView(scroll, false, true, GUILayout.Height(200));

				selectedTrait = GUILayout.SelectionGrid(selectedTrait, traitOptions, 1);
				if (selectedTrait != -1)
				{
					if (!traitsForbidden.Any(t => t.traitIdx == selectedTrait))
					{
						traitsForbidden.Add(traitEntries[selectedTrait]);
					}

					showTraitSelection = false;
					selectedTrait = -1;
				}

				GUILayout.EndScrollView();

			}

			Separator();

			for (var i = traitsForbidden.Count - 1; i >= 0; i--)
			{
				var trait = traitsForbidden[i];
				GUILayout.BeginHorizontal();

				GUILayout.Label(trait.traitName);
				GUI.contentColor = Constants.NEGATIVE_COLOR;
				if (GUILayout.Button("X"))
				{
					traitsForbidden.Remove(trait);
					selectedTrait = -1;
				}

				GUI.contentColor = Color.white;

				GUILayout.EndHorizontal();
			}

			Separator();
		}

		private void Bionics()
		{
			if (bionicsDlc)
			{
				GUILayout.Label($"Chance of Bionic: {GameUtil.GetFormattedPercent(bionics * 100.0f)}");
				bionics = GUILayout.HorizontalSlider(bionics, 0.0f, 1.0f, []);
			}
		}

		private void MaxDupesPerSchedule()
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label("Max Dupes per schedule:");

			dupePerScheduleBlockStr = GUILayout.TextField(dupePerScheduleBlockStr, 10);

			if (int.TryParse(dupePerScheduleBlockStr, out var result))
			{
				if (result < 1)
				{
					result = 1;
					dupePerScheduleBlockStr = 1.ToString();
				}
				else if (result > 999)
				{
					result = 999;
					dupePerScheduleBlockStr = 999.ToString();
				}

				dupePerScheduleBlock = result;
			}
			else
			{
				GUI.contentColor = Constants.NEGATIVE_COLOR;
				GUILayout.TextArea("Not a number");
				GUI.contentColor = Color.white;
			}

			GUILayout.EndHorizontal();
		}

		private void ColonySize()
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label("Colony size:");

			countStr = GUILayout.TextField(countStr, 10);

			if (int.TryParse(countStr, out var result))
			{
				if (result < 1)
				{
					result = 1;
					countStr = 1.ToString();
				}
				else if (result > 1000)
				{
					result = 1000;
					countStr = 1000.ToString();
				}

				if (result > 100)
				{
					GUI.contentColor = Constants.NEGATIVE_COLOR;
					GUILayout.Label("This is a terrible idea...");
					GUI.contentColor = Color.white;
				}

				dupeCount = result;
			}
			else
			{
				GUI.contentColor = Constants.NEGATIVE_COLOR;
				GUILayout.TextArea("Not a number");
				GUI.contentColor = Color.white;
			}

			GUILayout.EndHorizontal();
		}

		private void CloseButton(float x, float y)
		{
			if (GUI.Button(new Rect(x - 25, y, 20, 20), "X"))
				open = false;
		}

		// delay to prevent colony over screen
		private IEnumerator ScheduleCoroutine(Schedule schedule, Schedulable schedulable)
		{
			yield return SequenceUtil.waitForEndOfFrame;

			if (schedulable == null || schedule == null)
				yield return null;

			Log.Debug("assigning " + schedule.name);
		}

		public void Separator(int height = 1, int padding = 6)
		{
			GUILayout.Space(padding);

			var rect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(height));
			GUI.DrawTexture(rect, lightBg);

			GUILayout.Space(padding);
		}

		// delay to prevent colony over screen
		private IEnumerator KillCoroutine(MinionIdentity identity)
		{
			yield return SequenceUtil.waitForEndOfFrame;

			var paused = SpeedControlScreen.Instance.IsPaused;
			SpeedControlScreen.Instance.Unpause(false);

			Kill(identity);

			if (paused)
				SpeedControlScreen.Instance.Pause(false);
		}

		private void Kill(MinionIdentity identity)
		{
			if (Game.IsQuitting())
				return;

			/*			var equipment = identity.GetEquipment();
						if (equipment != null)
							equipment.UnequipAll();
			*/

			if (identity == null)
				return;

			foreach (var storage in identity.GetComponents<Storage>())
			{
				for (var i = storage.items.Count - 1; i >= 0; i--)
				{
					var item = storage.items[i];
					if (item.GetComponent<KPrefabID>().HasTag(startGear))
					{
						item.DeleteObject();
						storage.items.Remove(item);
					}
					else
					{
						storage.Drop(item);
					}
				}
			}

			Game.Instance.SpawnFX(SpawnFXHashes.BuildingFreeze, transform.position, 0);
			Util.KDestroyGameObject(identity.gameObject);
		}

		private bool SpawnDupe(int cell, List<string> forbiddenTraits, out MinionIdentity spawnedDupe)
		{
			spawnedDupe = null;
			var isBionic = false;
			if (bionicsDlc && UnityEngine.Random.value < bionics)
				isBionic = true;

			var model = isBionic ? GameTags.Minions.Models.Bionic : GameTags.Minions.Models.Standard;

			var stats = new MinionStartingStats(model, false);

			Log.Debug("generated dupe " + stats.Traits.Join());

			if (stats.Traits.Any(t => forbiddenTraits.Contains(t.Id)))
			{
				Log.Debug("has forbidden trait ");
				return false;
			}

			var prefab = Assets.GetPrefab(isBionic ? BionicMinionConfig.ID : MinionConfig.ID);
			var gameObject = Util.KInstantiate(prefab);
			gameObject.name = prefab.name;
			Immigration.Instance.ApplyDefaultPersonalPriorities(gameObject);
			var posCbc = Grid.CellToPosCBC(cell, Grid.SceneLayer.Move);
			gameObject.transform.SetLocalPosition(posCbc);
			gameObject.SetActive(true);

			stats.Apply(gameObject);

			foreach (var storage in gameObject.GetComponents<Storage>())
			{
				foreach (var item in storage.items)
					item.GetComponent<KPrefabID>().AddTag(startGear, true);
			}

			if (gameObject.TryGetComponent(out MinionIdentity identity))
				spawnedDupe = identity;

			return true;
		}
	}
}
