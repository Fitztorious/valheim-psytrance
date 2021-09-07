// Structure of m_music

// m_music  is a [list of NamedMusic].
// Each NamedMusic has a property m_clips [list of AudioClips].
// Each NamedMusic has a property m_name [string].
// There is only 1 audio clip at position [0] in each NamedMusic at this time.
// The idea is to replace m_clips[0] where m_name is one of the random events.

// m_name [string]
// m_clips[0] [AudioClip]

// CombatEventL1 
// ForestIsMovingLv1 (UnityEngine.AudioClip)
// CombatEventL2 
// ForestIsMovingLv2 (UnityEngine.AudioClip)
// CombatEventL3 
// ForestIsMovingLv3 (UnityEngine.AudioClip)
// CombatEventL4 
// ForestIsMovingLv4 (UnityEngine.AudioClip)

// Longest event is army_bonemass at 150 seconds (2min 30sec)

// index position : track name in [musicList]
// 2 : menu
// 4 : CombatEventL1
// 5 : CombatEventL2
// 6 : CombatEventL3
// 7 : CombatEventL4

using HarmonyLib;
using UnityEngine;
using UnityEngine.Networking;
using BepInEx;
using BepInEx.Configuration;
using System.Collections;
using System.Collections.Generic;

namespace Psytrance
{
    [BepInPlugin(modGUID, "Psytrance Events", "1.1.0")]
    [BepInProcess("valheim.exe")]

    public class Psytrance : BaseUnityPlugin
    {
        private readonly Harmony harmony = new Harmony(modGUID);
        private const string modGUID = "ca.fitztorious.valheim.plugins.psytrance";
        private static ConfigEntry<float> configMusicVolume;
        private static readonly List<AudioClip> psyTracks = new List<AudioClip>();
        private static readonly Dictionary<string, int> trackAssignments = new Dictionary<string, int>();
        private static string lastEvent;
        private static int dontPlay = -1;

        void Awake()
        { 
            configMusicVolume = Config.Bind<float>("Music Controls",
                                                   "MusicVolume",
                                                   0.6f,
                                                   "Adjust music volume during base attacks. [0 - 1.0]" +
                                                   "\nTry adjusting in increments of 0.1");

            LoadMusic();

            harmony.PatchAll();
        }

        void OnDestroy()
        {
            harmony.UnpatchSelf();
        }

        [HarmonyPatch(typeof(MusicMan), "Awake")]
        class MusicManAwakePatch
        {
            [HarmonyPostfix]
            static void SetPsyMusic()
            {
                PsyOverride.RandomPsy();
            }
        }

        [HarmonyPatch(typeof(MusicMan), "HandleEventMusic")]
        class MusicManEventPatch
        {
            [HarmonyPostfix]
            static void GetPsyEvent()
            {
                var musicList = MusicMan.instance;

                // Gets the name of the most recent event track so it can be prevented from playing again.
                if (!musicList.m_randomEventMusic.IsNullOrWhiteSpace())
                {
                    lastEvent = musicList.m_randomEventMusic;
                }
            }
        }

        [HarmonyPatch(typeof(RandEventSystem), "StartRandomEvent")]
        class PsyOverride
        {
            [HarmonyPrefix]
            public static void RandomPsy()
            {
                var musicList = MusicMan.instance.m_music;
                var rand = new System.Random();
                int pos;

                // Determines what track to not repeat, will only run after first event.
                if (!lastEvent.IsNullOrWhiteSpace() && trackAssignments.Count != 0)
                {
                    try
                    {
                        trackAssignments.TryGetValue(lastEvent, out dontPlay);
                    }
                    catch (KeyNotFoundException)
                    {
                        Debug.Log("Psytrance Events: Could not find " + lastEvent + " in the available tracks.\n");
                    }
                }

                trackAssignments.Clear();

                for (int i = 4; i < 8; i++)
                {
                    do
                    {
                        pos = rand.Next(0, 5);

                    } while (pos == dontPlay);

                    try
                    {
                        trackAssignments.Add(musicList[i].m_name, pos);
                        musicList[i].m_clips[0] = psyTracks[pos];
                        musicList[i].m_savedPlaybackPos = 0;
                        musicList[i].m_volume = configMusicVolume.Value;

                    } catch
                    {
                        Debug.Log("Psytrance Events: Unable to load " + musicList[i].m_name + " from position " + pos + " in psyTracks.\n");
                    }
                }

                if (MessageHud.instance != null)
                {
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, "Poetic god Bragi unleashes psytrance upon the land!");
                }
            }
        }

        private void LoadMusic()
        {
            string filePath = "";

            Dictionary<string, string> trackDictionary = new Dictionary<string, string>
            {
                { "fpath0", "https://drive.google.com/uc?id=140Q_0Vzj9fO0DeYBMZz3ygRcZzPa4i4e&export=download" },
                { "fpath1", "https://drive.google.com/uc?id=19YyhlxWhlCrOzdIor5qw9M2Ms9RSWSO1&export=download" },
                { "fpath2", "https://drive.google.com/uc?id=1oOhB455tAJGl3SIMQPtl_V9Dm810621S&export=download" },
                { "fpath3", "https://drive.google.com/uc?id=1sRV-eT7zTTJfyZh-z6NbjCEmJePkle32&export=download" },
                { "fpath4", "https://drive.google.com/uc?id=1xOE2IhIeKtQLCwU9POJVbUVPaPhDoZ0D&export=download" }
            };

            psyTracks.Clear();

            for (var i = 0; i < trackDictionary.Count; i++)
            {
                try
                {
                    trackDictionary.TryGetValue("fpath" + i, out filePath);
                    StartCoroutine(GetPsytrance(filePath));
                }
                catch (KeyNotFoundException)
                {
                    Debug.Log("Psytrance Events: Track " + filePath + " could not be loaded.\n");
                }
                filePath = "";
            }
        }

        private static IEnumerator GetPsytrance(string uri)
        {
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.OGGVORBIS))

            {
                yield return www.SendWebRequest();

                if (www.isHttpError || www.isNetworkError)
                {
                    Debug.Log(www.error);
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