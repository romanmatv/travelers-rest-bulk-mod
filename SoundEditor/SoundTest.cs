using System.Collections.Generic;
using AlmenaraGames;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace SoundEditor;

public class SoundTest : SampleSubModBase
{
    public new static void Awake(Harmony _harmony, ConfigFile configFile, ManualLogSource logger)
    {
        BaseSetup(_harmony, configFile, logger, nameof(SoundTest));


        BaseFinish(typeof(SoundTest));
    }

    private static readonly Dictionary<string, AudioObject> AudioObjectDictionary = new();

    // Helper to validate audio name compared to audio sound
    public static void TestSound(string name, float f)
    {
        if (!AudioObjectDictionary.TryGetValue(name, out var value)) return;

        value.volume = f;
        _log.LogInfo("testing " + value.name + " vol=" + value.volume);
        MultiAudioManager.PlayAudioObject(value, PlayerController.GetPlayer(1).transform);
    }


    [HarmonyPatch(typeof(AudioObject), nameof(AudioObject.Awake))]
    [HarmonyPrefix]
    private static void AudioObject_Awake_Prefix(AudioObject __instance, ref float ___volume)
    {
        var audioObjectVolumeBinding = Config.Bind($"SoundLevel-{__instance.name}", __instance.volume,
            $"Game sound '{__instance.name}' volume level from 0.00 (0%) to 1.00 (100%).");

        // if the volume is changed from default update volume
        if (!Mathf.Approximately(audioObjectVolumeBinding.Value, __instance.volume))
        {
            ___volume = audioObjectVolumeBinding.Value;
        }

        AudioObjectDictionary.Add(__instance.name, __instance);
    }

    // List of all sounds with default vol and minMax range
    private static string _originalSounds = """
                                            Ambience Day1, , vol=0.3, (1,1)
                                            Ambience Day2, , vol=0.3, (1,1)
                                            Ambience Day3, , vol=0.3, (1,1)
                                            Ambience Day4, , vol=0.3, (1,1)
                                            Ambience Night1, , vol=0.3, (1,1)
                                            Ambience Night2, , vol=0.3, (1,1)
                                            Ambience Night3, , vol=0.3, (1,1)
                                            Ambience Winter, , vol=1, (1,1)
                                            BirdExplosion, , vol=0.4, (1,1)
                                            BirdRes, , vol=0.3, (1,1)
                                            CatAngry, , vol=0.2, (1,1)
                                            CatMeow, , vol=0.2, (1,1)
                                            CatPurr, , vol=0.2, (1,1)
                                            DuckSound, , vol=0.4, (1,1)
                                            ChickenHitSound, , vol=0.4, (1,1)
                                            ChickenSound, , vol=0.4, (1,1)
                                            CowHitSound, , vol=0.4, (1,1)
                                            CowSound, , vol=0.2, (1,1)
                                            PigHitSound, , vol=0.4, (1,1)
                                            PigSound, , vol=0.4, (1,1)
                                            SheepHitSound, , vol=0.4, (1,1)
                                            SheepSound, , vol=0.4, (1,1)
                                            OwlExplosion, , vol=0.4, (1,1)
                                            OwlSound, , vol=0.4, (1,1)
                                            ParrotSound, , vol=0.4, (1,1)
                                            ParrotWingsSound, , vol=0.3, (1,1)
                                            Seagull, , vol=0.2, (1,1)
                                            TurkeySound, , vol=0.6, (1,1)
                                            Bell, , vol=0.2, (1,1)
                                            BellMuted, , vol=0.5, (1,1)
                                            BellRinging, , vol=0.8, (1,1)
                                            Carriage, , vol=0.7, (1,1)
                                            ChestJunk, , vol=0.7, (1,1)
                                            ChoppingWood, , vol=0.5, (1,1)
                                            CleanFloor, , vol=0.4, (1,1)
                                            ClothClose, , vol=0.5, (1,1)
                                            ClothOpen, , vol=0.5, (1,1)
                                            Cookie, , vol=0.6, (1,1)
                                            CreepyGirl, , vol=0.4, (1,1)
                                            DarkAmbience, , vol=0.1, (1,1)
                                            DoorClose, , vol=1, (1,1)
                                            DoorOpen, , vol=1, (1,1)
                                            DoorStoneSlide, , vol=1, (1,1)
                                            ExhalationBellows, , vol=0.2, (1,1)
                                            Fail, , vol=0.5, (1,1)
                                            Hammer, , vol=0.6, (1,1)
                                            Saw, , vol=0.3, (1,1)
                                            ThrowRock, , vol=0.3, (1,1)
                                            TrainingHit, , vol=0.4, (1,1)
                                            FirePlace02, , vol=0.7, (1,1)
                                            FirePlace03, , vol=0.7, (1,1)
                                            BanjoBigSuccess, , vol=0.5, (1,1)
                                            BanjoSuccess, , vol=0.5, (1,1)
                                            ExclamationFishing, , vol=0.5, (1,1)
                                            Fountain, , vol=0.1, (1,1)
                                            GassBlink, , vol=0.4, (1,1)
                                            GassDisappear, , vol=0.6, (1,1)
                                            GrassImpact, , vol=0.4, (1,1)
                                            GrinderLoop, , vol=0.05, (1,1)
                                            HeavyPunch, , vol=0.5, (1,1)
                                            Horse Whip, , vol=0.5, (1,1)
                                            InhalationBellows, , vol=0.15, (1,1)
                                            Lava, , vol=0.05, (1,1)
                                            MagicStone, , vol=0.05, (0.8,1.2)
                                            Mai Angry, , vol=0.6, (0.8,1.2)
                                            Mai Appear Clones, , vol=0.6, (0.8,1.2)
                                            Mai Appear, , vol=0.6, (0.8,1.2)
                                            Mai Cast, , vol=0.6, (0.8,1.2)
                                            Mai FIre, , vol=1, (0.8,1.2)
                                            Mai Transform, , vol=0.6, (0.8,1.2)
                                            MakingBed, , vol=0.7, (1,1)
                                            MetalDoorClose, , vol=1, (1,1)
                                            MetalDoorCreak, , vol=1, (1,1)
                                            MetalDoorOpen, , vol=1, (1,1)
                                            MusicBox, , vol=0.4, (1,1)
                                            MusicBoxClose, , vol=1, (1,1)
                                            NPCTalkLoop, , vol=0.25, (1,1)
                                            Objective, , vol=0.5, (1,1)
                                            FishChop, , vol=0.2, (1,1)
                                            HammerForge, , vol=0.4, (1,1)
                                            Male_Effort_Short, , vol=0.4, (1,1)
                                            Rummage, , vol=0.2, (1,1)
                                            SearchingBox, , vol=0.5, (1,1)
                                            Sharpen, , vol=0.2, (1,1)
                                            ShopClose, , vol=0.4, (1,1)
                                            Spit, , vol=0.5, (1,1)
                                            WoodCharge, , vol=0.4, (1,1)
                                            WoodDestroy, , vol=0.4, (1,1)
                                            PickaxeMining, , vol=0.5, (1,1)
                                            PlayerrExplosion, , vol=0.4, (1,1)
                                            PouringBeer, , vol=1, (1,1)
                                            Reel, , vol=0.5, (1,1)
                                            ReelLaunch, , vol=0.5, (1,1)
                                            Reel_Woosh, , vol=1, (1,1)
                                            Shovel, , vol=0.5, (1,1)
                                            SnowHit, , vol=0.4, (1,1)
                                            SnowmanSmash, , vol=0.4, (1,1)
                                            SoldierMovement, , vol=0.4, (1,1)
                                            StepsDirt, , vol=0.25, (1,1)
                                            StepsGrass, , vol=0.15, (1,1)
                                            StepsSnow, , vol=0.15, (1,1)
                                            StepsStone, , vol=0.25, (1,1)
                                            StepsWater, , vol=0.25, (1,1)
                                            StepsWood, , vol=0.35, (1,1)
                                            Stone Sharp, , vol=0.12, (1,1)
                                            Success, , vol=0.3, (1,1)
                                            CustomerLeavesHappy, , vol=0.4, (1,1)
                                            CustomerLeavesSad, , vol=0.4, (1,1)
                                            DeskBell, , vol=0.25, (1,1)
                                            Exclaim, , vol=0.4, (1,1)
                                            OpenFermenter, , vol=1, (1,1)
                                            OpenMetal, , vol=1, (1,1)
                                            OpenWood, , vol=1, (1,1)
                                            PickUpItem, , vol=1, (1,1)
                                            TavernRepair, , vol=0.3, (1,1)
                                            Tavern_Crowd, , vol=0, (1,1)
                                            Tavern_SmallCrowd, , vol=0, (1,1)
                                            TreeFalling, , vol=0.5, (1,1)
                                            Tutorial Creak 1, , vol=0.5, (1,1)
                                            Tutorial Creak 2, , vol=0.5, (1,1)
                                            WaterBoiling, , vol=0.05, (1,1)
                                            WaterBubbling, , vol=0.3, (1,1)
                                            WaterDrop, , vol=0.5, (1,1)
                                            Water_SmallSplash, , vol=1, (1,1)
                                            Whispers, , vol=0.1, (1,1)
                                            Whoosh, , vol=0.7, (1,1)
                                            Wolves, , vol=0.7, (1,1)
                                            WoodLightHit, , vol=0.7, (1,1)
                                            WoodSawLoop, , vol=0.05, (1,1)
                                            scratching, , vol=0.5, (0.8,1.2)
                                            Intro Music, , vol=0.28, (1,1)
                                            ScrollOpening, , vol=0.4, (1,1)
                                            ScrollPage, , vol=0.4, (1,1)
                                            Signature, , vol=0.4, (1,1)
                                            Voice-Intro-1, , vol=1, (1,1)
                                            Voice-Intro-10, , vol=1, (1,1)
                                            Voice-Intro-11, , vol=1, (1,1)
                                            Voice-Intro-12, , vol=1, (1,1)
                                            Voice-Intro-13, , vol=1, (1,1)
                                            Voice-Intro-14, , vol=1, (1,1)
                                            Voice-Intro-15, , vol=1, (1,1)
                                            Voice-Intro-16, , vol=1, (1,1)
                                            Voice-Intro-17, , vol=1, (1,1)
                                            Voice-Intro-18, , vol=1, (1,1)
                                            Voice-Intro-19, , vol=1, (1,1)
                                            Voice-Intro-2, , vol=1, (1,1)
                                            Voice-Intro-20, , vol=1, (1,1)
                                            Voice-Intro-3, , vol=1, (1,1)
                                            Voice-Intro-4, , vol=1, (1,1)
                                            Voice-Intro-5, , vol=1, (1,1)
                                            Voice-Intro-6, , vol=1, (1,1)
                                            Voice-Intro-7, , vol=1, (1,1)
                                            Voice-Intro-8, , vol=1, (1,1)
                                            Voice-Intro-9, , vol=1, (1,1)
                                            """;
}