﻿using FUtility.BuildingUtil;
using TUNING;
using UnityEngine;
using static DecorPackB.STRINGS.BUILDINGS.PREFABS.DECORPACKB_GIANTFOSSILDISPLAY;
using static FUtility.Consts;

namespace DecorPackB.Buildings.FossilDisplay
{
    internal class GiantFossilDisplayConfig : IBuildingConfig, IModdedBuilding
    {
        public static string ID = Mod.PREFIX + "GiantFossilDisplay";

        public MBInfo Info => new MBInfo(ID, BUILD_CATEGORY.FURNITURE, TECH.DECOR.ENVIRONMENTAL_APPRECIATION, MarbleSculptureConfig.ID);

        public override BuildingDef CreateBuildingDef()
        {
            BuildingDef def = BuildingTemplates.CreateBuildingDef(
               ID,
               6,
               5,
               "giant_fossil_display_kanim",
               BUILDINGS.HITPOINTS.TIER2,
               BUILDINGS.CONSTRUCTION_TIME_SECONDS.TIER4,
               new float[2] { 800f, 1f },
               new string[]
               {
                   ModAssets.Tags.Fossil.ToString(),
                   ModAssets.Tags.FossilNodule.ToString()
               },
               BUILDINGS.MELTING_POINT_KELVIN.TIER1,
               BuildLocationRule.Anywhere,//BuildLocationRule.OnFloor,
               new EffectorValues(Mod.Settings.FossilDisplay.BaseDecor.Amount, Mod.Settings.FossilDisplay.BaseDecor.Range),
               NOISE_POLLUTION.NONE
           );

            def.Floodable = false;
            def.Overheatable = false;
            def.AudioCategory = AUDIO_CATEGORY.PLASTIC;
            def.BaseTimeUntilRepair = -1f;
            def.ViewMode = OverlayModes.Decor.ID;
            def.DefaultAnimState = "base";
            def.PermittedRotations = PermittedRotations.FlipH;

            return def;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            go.AddTag(GameTags.Decoration);
            go.AddTag(ModAssets.Tags.FossilBuilding);
            go.AddOrGet<BuildingComplete>().isArtable = true;
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            Settings.Config.FossilDisplayConfig config = Mod.Settings.FossilDisplay;
            go.AddComponent<GiantFossilDisplay>();

            Assemblable assemblable = go.AddComponent<Assemblable>();

            assemblable.stages.Add(new Artable.Stage("Default", NAME, "base", -5, false, Artable.Status.Ready));

            assemblable.stages.Add(new Artable.Stage("T-Rex", VARIANT.TREX.NAME, "trex", 15, true, Artable.Status.Great));
            assemblable.stages.Add(new Artable.Stage("Livayatan", VARIANT.LIVYATAN.NAME, "livayatan", 15, true, Artable.Status.Great));
            assemblable.stages.Add(new Artable.Stage("Para", VARIANT.LIVYATAN.NAME, "para", 15, true, Artable.Status.Great));
        }
    }
}