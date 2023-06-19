﻿using System.Collections.Generic;
using Twitchery.Utils;
using UnityEngine;
using static ResearchTypes;

namespace Twitchery.Content
{
    public class Elements
    {
        public static ElementInfo
            Jello = new("Jello", "aete_jello_kanim", Element.State.Liquid, Color.green),
            FrozenJello = ElementInfo.Solid("FrozenJello", Color.green),
            Rice = ElementInfo.Solid("Rice", Color.white),
            Soap = ElementInfo.Solid("Soap", Color.cyan),
            LiquidSoap = ElementInfo.Liquid("LiquidSoap", Color.cyan),
            Tea = ElementInfo.Liquid("Tea", Color.red),
            IceTea = ElementInfo.Solid("FrozenTea", Color.red);

        internal static void RegisterSubstances(List<Substance> list)
        {
            var frozenJello = FrozenJello.CreateSubstance();
            frozenJello.material.SetFloat("_WorldUVScale", 2.5f);
            list.Add(frozenJello);

            foreach (var element in ElementUtil.elements)
            {
                if (!element.isInitialized)
                {
                    list.Add(element.CreateSubstance());
                }
            }

            SetAtmosphereModifiers();
        }

        // Food sterilization/rotting modifier
        private static void SetAtmosphereModifiers()
        {
            Rottable.AtmosphereModifier.Add((int)Soap.SimHash, Rottable.RotAtmosphereQuality.Sterilizing);
            Rottable.AtmosphereModifier.Add((int)LiquidSoap.SimHash, Rottable.RotAtmosphereQuality.Sterilizing);
        }

        internal static ElementsAudio.ElementAudioConfig[] CreateAudioConfigs(ElementsAudio instance)
        {
            return new[]
            {
                ElementUtil.CopyElementAudioConfig(SimHashes.SlimeMold, Jello),
                ElementUtil.CopyElementAudioConfig(SimHashes.Sand, Rice),
                ElementUtil.CopyElementAudioConfig(SimHashes.BleachStone, Soap),
                ElementUtil.CopyElementAudioConfig(SimHashes.Ice, IceTea),
            };
        }
    }
}