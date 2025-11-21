using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using GorillaLocomotion;
using GorillaNetworking;

namespace Editing
{
    [BepInPlugin("Creator.JUNETHE1ST.GorillaMusicPlayer", "GorillaMusicPlayer", "1.0.0")]
    public class GorillaMusicPlayer : BaseUnityPlugin
    {
        internal static GorillaMusicPlayer Instance;
        internal ManualLogSource LogSource => Logger;

        private string musicFolder;
        private readonly List<string> trackPaths = new List<string>();
        private readonly List<string> trackNames = new List<string>();

        private AudioSource musicSource;
        private AudioClip currentClip;
        private int currentTrackIndex = 0;

        private bool isPaused = false;
        private bool isLoading = false;

        private GameObject menuRoot;

        private UnityEngine.UI.Image backgroundImage;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI trackNameText;
        private TextMeshProUGUI statusText;
        private TextMeshProUGUI counterText;
        private TextMeshProUGUI timerText;
        private TextMeshProUGUI folderText;
        private TextMeshProUGUI hintText;

        private bool lastLeftX = false;
        private bool lastRightTrigger = false;
        private bool lastLeftTrigger = false;

        private void Awake()
        {
            Instance = this;

            try
            {
                string dllDir = Path.GetDirectoryName(Info.Location);
                musicFolder = Path.Combine(dllDir, "music");
                if (!Directory.Exists(musicFolder))
                    Directory.CreateDirectory(musicFolder);
            }
            catch
            {
                musicFolder = BepInEx.Paths.PluginPath;
            }

            ScanForTracks();
            SetupAudioSource();
        }

        private void SetupAudioSource()
        {
            GameObject audioGO = new GameObject("GorillaMusicPlayer_Audio");
            DontDestroyOnLoad(audioGO);
            musicSource = audioGO.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = false;
            musicSource.spatialBlend = 0f;
            musicSource.volume = 1f;
        }

        private void ScanForTracks()
        {
            trackPaths.Clear();
            trackNames.Clear();

            if (!Directory.Exists(musicFolder))
                Directory.CreateDirectory(musicFolder);

            string[] files = Directory.GetFiles(musicFolder, "*.mp3");
            foreach (string f in files)
            {
                trackPaths.Add(f);
                trackNames.Add(Path.GetFileNameWithoutExtension(f));
            }
        }

        private void Update()
        {
            if (ControllerInputPoller.instance == null)
                return;

            bool leftX = SimpleInputs.LeftX;
            bool rt = SimpleInputs.RightTrigger;
            bool lt = SimpleInputs.LeftTrigger;
            bool rA = SimpleInputs.RightA;

            if (leftX && !lastLeftX)
            {
                if (menuRoot == null)
                {
                    SpawnMenu();
                    UpdateFullUI();
                }
                else
                {
                    menuRoot.SetActive(!menuRoot.activeSelf);
                }
            }

            bool uiOpen = menuRoot != null && menuRoot.activeSelf;

            if (uiOpen)
            {
                if (rt && !lastRightTrigger && rA && !lt)
                    NextTrackAndPlay();

                else if (lt && !lastLeftTrigger && rA)
                    PreviousTrackAndPlay();

                else if (rt && !lastRightTrigger && !rA && !lt)
                    TogglePlayPause();
            }

            lastLeftX = leftX;
            lastRightTrigger = rt;
            lastLeftTrigger = lt;

            if (musicSource.clip != null &&
                !musicSource.isPlaying &&
                !isPaused &&
                !isLoading &&
                uiOpen)
            {
                NextTrackAndPlay();
            }

            UpdateUIPanelPosition();
            UpdateTimerText();
        }

        private void TogglePlayPause()
        {
            if (trackPaths.Count == 0)
            {
                StatusText("No tracks");
                return;
            }

            if (musicSource.clip == null)
            {
                StartCoroutine(LoadAndPlayTrack(currentTrackIndex));
                return;
            }

            if (musicSource.isPlaying)
            {
                musicSource.Pause();
                isPaused = true;
                StatusText("Paused");
            }
            else
            {
                musicSource.Play();
                isPaused = false;
                StatusText("Playing");
            }

            UpdateFullUI();
        }

        private void NextTrackAndPlay()
        {
            if (trackPaths.Count == 0)
                return;

            currentTrackIndex++;
            if (currentTrackIndex >= trackPaths.Count)
                currentTrackIndex = 0;

            StartCoroutine(LoadAndPlayTrack(currentTrackIndex));
        }

        private void PreviousTrackAndPlay()
        {
            if (trackPaths.Count == 0)
                return;

            currentTrackIndex--;
            if (currentTrackIndex < 0)
                currentTrackIndex = trackPaths.Count - 1;

            StartCoroutine(LoadAndPlayTrack(currentTrackIndex));
        }

        private IEnumerator LoadAndPlayTrack(int index)
        {
            if (trackPaths.Count == 0)
                yield break;

            isLoading = true;
            isPaused = false;

            string path = trackPaths[index];
            string url = "file://" + path;

            StatusText("Loading...");
            UpdateTrackUI();

            UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG);
            yield return www.SendWebRequest();

#if UNITY_2020_1_OR_NEWER || UNITY_6000_0_OR_NEWER
            if (www.result != UnityWebRequest.Result.Success)
#else
            if (www.isHttpError || www.isNetworkError)
#endif
            {
                StatusText("Error");
                isLoading = false;
                yield break;
            }

            AudioClip newClip = DownloadHandlerAudioClip.GetContent(www);

            if (currentClip != null)
                Destroy(currentClip);

            currentClip = newClip;
            musicSource.clip = newClip;
            musicSource.Play();
            isPaused = false;

            StatusText("Playing");
            UpdateFullUI();

            isLoading = false;
        }

        private void SpawnMenu()
        {
            menuRoot = new GameObject("GorillaMusicPlayerMenu");

            Canvas canvas = menuRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;

            CanvasScaler scaler = menuRoot.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 1000f;

            menuRoot.AddComponent<GraphicRaycaster>();

            RectTransform rt = menuRoot.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(180f, 300f);
            menuRoot.transform.localScale = Vector3.one * 0.002f;

            GameObject bg = new GameObject("BG");
            bg.transform.SetParent(menuRoot.transform, false);

            backgroundImage = bg.AddComponent<UnityEngine.UI.Image>();
            backgroundImage.color = Color.black;

            var layout = bg.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 5f;

            var fit = bg.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            titleText = CreateField(bg.transform, "<b>GorillaMusicPlayer</b>", 7f);
            trackNameText = CreateField(bg.transform, "Track: (none)");
            statusText = CreateField(bg.transform, "Status: Idle");
            counterText = CreateField(bg.transform, "0 / 0");
            timerText = CreateField(bg.transform, "0:00 / 0:00");

            folderText = CreateField(
                bg.transform,
                "Folder:\n/music/",
                4.8f
            );

            hintText = CreateField(
                bg.transform,
                "<b>Controls</b>\nLeft X = Menu\nRT = Play/Pause\nA+RT = Next\nA+LT = Previous",
                4.6f
            );

            UpdateFullUI();
        }

        private TextMeshProUGUI CreateField(Transform parent, string text, float size = 5.5f)
        {
            GameObject obj = new GameObject("TXT");
            obj.transform.SetParent(parent, false);

            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            obj.GetComponent<RectTransform>().sizeDelta = new Vector2(300f, 24f);

            return tmp;
        }

        private void UpdateUIPanelPosition()
        {
            if (menuRoot == null || !menuRoot.activeSelf)
                return;

            var player = GTPlayer.Instance;
            var hand = player?.LeftHand.controllerTransform;
            if (hand == null)
                return;

            menuRoot.transform.position =
                hand.position +
                hand.right * 0.16f +
                hand.up * 0.09f +
                hand.forward * 0.03f;

            menuRoot.transform.rotation =
                hand.rotation * Quaternion.Euler(108f, 0f, 10f);
        }

        private void UpdateTrackUI()
        {
            if (trackPaths.Count == 0)
            {
                trackNameText.text = "Track: (none)";
                counterText.text = "0 / 0";
                return;
            }

            trackNameText.text = $"Track: {trackNames[currentTrackIndex]}";
            counterText.text = $"{currentTrackIndex + 1} / {trackPaths.Count}";
        }

        private void UpdateFullUI()
        {
            if (menuRoot == null)
                return;

            UpdateTrackUI();
            UpdatePlayStateText();

            if (folderText != null)
                folderText.text = $"Folder:\n{musicFolder}";

            UpdateTimerText();
        }

        private void UpdateTimerText()
        {
            if (timerText == null || musicSource.clip == null)
                return;

            float t = musicSource.time;
            float total = musicSource.clip.length;

            TimeSpan cur = TimeSpan.FromSeconds(t);
            TimeSpan max = TimeSpan.FromSeconds(total);

            timerText.text =
                $"{cur.Minutes}:{cur.Seconds:00} / {max.Minutes}:{max.Seconds:00}";
        }

        private void UpdatePlayStateText()
        {
            if (statusText == null)
                return;

            if (isLoading)
            {
                statusText.text = "Status: Loading...";
                return;
            }

            if (musicSource.clip == null)
            {
                statusText.text = "Status: Ready";
                return;
            }

            if (isPaused)
                statusText.text = "Status: Paused";
            else if (musicSource.isPlaying)
                statusText.text = "Status: Playing";
            else
                statusText.text = "Status: Stopped";
        }

        private void StatusText(string msg)
        {
            if (statusText != null)
                statusText.text = "Status: " + msg;
        }
    }
}
