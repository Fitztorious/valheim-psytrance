using HarmonyLib;
using UnityEngine;
using UnityEngine.Networking;
using BepInEx;
using BepInEx.Configuration;
using System.Collections;
using System.Collections.Generic;
using System;

namespace psytrance
{
    [BepInPlugin(modGUID, "Psy Attack", "1.0.0")]
    [BepInProcess("valheim.exe")]

    public class psytrance : BaseUnityPlugin
    {
        private const string modGUID = "ca.fitztorious.valheim.plugins.psytrance";
        private readonly Harmony harmony = new Harmony(modGUID);

        private static ConfigEntry<float> configMusicVolume;
        private static List<AudioClip> psyTracks = new List<AudioClip>();

        void Awake()
        {
            configMusicVolume = Config.Bind("Music Controls",
                                            "MusicVolume",
                                            0.6f,
                                            "Adjust music volume during base attacks. [0 - 1.0]\nTry adjusting in increments of 0.1");

            // Download and load tracks into memory.
            LoadMusic();        

            harmony.PatchAll();
        }

        void OnDestroy()
        {
            harmony.UnpatchSelf();
        }

        [HarmonyPatch(typeof(MusicMan), "Awake")]
        class MusicManPatch
        {
            [HarmonyPostfix]
            static void SetPsyMusic()
            {
                UpdatePatch.RandomPsy();
            }
        }

        [HarmonyPatch(typeof(RandEventSystem), "StartRandomEvent")]
        class UpdatePatch
        {
            [HarmonyPrefix]
            public static void RandomPsy()
            {
                // m_music  is a [list of NamedMusic].
                // Each NamedMusic has a property m_clips [list of AudioClips].
                // Each NamedMusic has a property m_name [string].
                // There is only 1 audio clip at position [0] in each NamedMusic at this time.
                // The goal is to replace m_clips[0] where m_name is one of the random events.

                //m_name [string]
                //m_clips[0] [AudioClip]

                //CombatEventL1
                //ForestIsMovingLv1 (UnityEngine.AudioClip)
                //CombatEventL2
                //ForestIsMovingLv2 (UnityEngine.AudioClip)
                //CombatEventL3
                //ForestIsMovingLv3 (UnityEngine.AudioClip)
                //CombatEventL4
                //ForestIsMovingLv4 (UnityEngine.AudioClip)

                // This  will run after the new tracks are loaded into a list.
                // We can replace all the event audio clips in musicList with psyTracks.
                // Below are the tracks and positions we need to replace.

                //  index position : song name in [musicList]
                // 2 : menu
                // 4 : CombatEventL1
                // 5 : CombatEventL2
                // 6 : CombatEventL3
                // 7 : CombatEventL4

                // This function randomly assigns new tracks to each event at the start of every event and on awake.

                var musicList = MusicMan.instance.m_music;
                var rand = new System.Random();
                    
                for (int i = 4; i < 8; i++)
                {
                    musicList[i].m_clips[0] = psyTracks[rand.Next(0, 5)];
                    musicList[i].m_savedPlaybackPos = 0;                        // Always start event track at beginning
                    //musicList[i].m_volume = 1f;                               // not tested - get this value from config
                }
            }
        }

        private void LoadMusic()
        {
            var watch = new System.Diagnostics.Stopwatch();

            psyTracks.Clear();

            string fpath1 = "https://drive.google.com/uc?id=1fCBrsj7MMR9YRfFliYXQNyCBwdzVbCyf&export=download";
            string fpath2 = "https://drive.google.com/uc?id=1To5jfnZm6BD5e1r6Dt38cC_RrC6cJxUV&export=download";
            string fpath3 = "https://drive.google.com/uc?id=1ONpBuSoMtrMiQeG8f5UT2ZJG6mmjz8lx&export=download";     
            string fpath4 = "https://drive.google.com/uc?id=1_MZhuT0J6a7M6RnsdNsbrIpp0on1Ai_D&export=download";
            string fpath5 = "https://drive.google.com/uc?id=1qLS4T8bLEyCsqbgLMxrwdj6AuUQkSXjx&export=download";

            watch.Start();

            // Send paths to be loaded into psyTracks array.
            StartCoroutine(GetPsytrance(fpath1));
            StartCoroutine(GetPsytrance(fpath2));
            StartCoroutine(GetPsytrance(fpath3));
            StartCoroutine(GetPsytrance(fpath4));
            StartCoroutine(GetPsytrance(fpath5));

            watch.Stop();

            TimeSpan ts = watch.Elapsed;
            UnityEngine.Debug.Log("psytrance: Tracks loaded in " + ts.TotalSeconds + " seconds.");

            //---- FOR LOADING TRACKS LOCALLY -------------------------------------------------------------------------
            // Used for local file I/O
            //using System.IO;
            //using System.Reflection;

            // Retrieves local path in this format file:///X:/Steam/steamapps/common/Valheim/BepInEx/plugins/psy/psy.wav
            //string fpath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "psy\\");
            //fpath = "file:///" + fpath.Replace("\\", "/");

            // Sends Paths for local retrieval.
            //StartCoroutine(GetPsytrance(fpath + "psy1.wav")); //CombatEventL1
            //StartCoroutine(GetPsytrance(fpath + "psy2.wav")); //CombatEventL2
            //StartCoroutine(GetPsytrance(fpath + "psy3.wav")); //CombatEventL3
            //StartCoroutine(GetPsytrance(fpath + "psy4.wav")); //CombatEventL4
            //---------------------------------------------------------------------------------------------------------
        }

        IEnumerator GetPsytrance(string uri)
        {
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.WAV))

            {
                yield return www.SendWebRequest();

                if (www.isHttpError || www.isNetworkError)
                {
                    UnityEngine.Debug.Log(www.error);
                }
                else
                {
                    AudioClip psy = DownloadHandlerAudioClip.GetContent(www);
                    psyTracks.Add(psy);
                }
            }
        }
    }
}