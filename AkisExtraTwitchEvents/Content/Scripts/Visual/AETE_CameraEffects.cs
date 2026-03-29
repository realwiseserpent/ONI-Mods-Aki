using UnityEngine;

namespace Twitchery.Content.Scripts.Visual
{
	// an only use effect one at a time, but it goes above all of the screen, UI and all
	public class AETE_CameraEffects : KMonoBehaviour
	{
		public Material material;
		public static AETE_CameraEffects Instance;
		public bool active;
		public float durationSeconds = ModTuning.EGG_DURATION;
		public float elapsed = 0;

		public override void OnPrefabInit()
		{
			base.OnPrefabInit();
			Instance = this;
			material = ModAssets.Materials.eggMaterial;

			cam = Camera.main;

			rt = new RenderTexture(Screen.width, Screen.height, 24);
			screenTex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
		}

		RenderTexture rt;
		Camera cam;
		Texture2D screenTex;

		void OnGUI()
		{
			if (!active)
				return;

			if (Event.current.type != EventType.Repaint)
				return;

			screenTex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
			screenTex.Apply();

			Graphics.DrawTexture(
				new Rect(0, 0, Screen.width, Screen.height),
				screenTex,
				material
			);
		}

		public void Activate()
		{
			elapsed = 0;
			active = true;
			AudioUtil.PlaySound(ModAssets.Sounds.EGG_SMASH, ModAssets.GetSFXVolume() * 0.8f);

			//cam.targetTexture = rt;
		}


		void Update()
		{
			if (active)
			{
				elapsed += Time.deltaTime;

				if (elapsed > durationSeconds)
				{
					active = false;
					return;
				}

				var t = elapsed / durationSeconds;
				var offset = new Vector2(-0.09f, t);
				material.SetTextureOffset("_DisplacementTex", offset);
				material.SetTextureOffset("_Egg", offset);
				material.SetFloat("_Droopyness", t * 0.5f);
			}
		}

		public override void OnCleanUp()
		{
			base.OnCleanUp();
			Instance = null;
		}
	}
}
