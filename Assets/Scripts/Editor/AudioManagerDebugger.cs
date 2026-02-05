using UnityEngine;
using UnityEditor;

public class AudioManagerDebugger : EditorWindow
{
    private AudioManager audioManager;
    private float fadeTime = 2f;
    
    // Volume sliders
    private float bgmVolumeSlider = 0.7f;
    private float sfxVolumeSlider = 1f;
    private float backgroundVolumeSlider = 0.5f;
    
    // Scroll position for sound effects
    private Vector2 scrollPosition;

    [MenuItem("Tools/Audio Manager Debugger")]
    public static void ShowWindow()
    {
        GetWindow<AudioManagerDebugger>("Audio Manager Debugger");
    }

    private void OnEnable()
    {
        // Find AudioManager in scene
        RefreshAudioManager();
    }

    private void RefreshAudioManager()
    {
        audioManager = FindObjectOfType<AudioManager>();
        if (audioManager != null)
        {
            // Get current volume values from AudioManager
            bgmVolumeSlider = GetPrivateField<float>(audioManager, "bgmVolume");
            sfxVolumeSlider = GetPrivateField<float>(audioManager, "sfxVolume");
            backgroundVolumeSlider = GetPrivateField<float>(audioManager, "backgroundVolume");
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("Audio Manager Debugger", EditorStyles.boldLabel);
        
        // Check if AudioManager exists
        if (audioManager == null)
        {
            EditorGUILayout.HelpBox("AudioManager not found in scene!", MessageType.Warning);
            if (GUILayout.Button("Refresh"))
            {
                RefreshAudioManager();
            }
            return;
        }

        // Status display
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"AudioManager Found: {audioManager.name}");
        
        var bgmSource = GetPrivateField<AudioSource>(audioManager, "bgmSource");
        var backgroundSource = GetPrivateField<AudioSource>(audioManager, "backgroundSource");
        
        if (bgmSource != null)
            EditorGUILayout.LabelField($"BGM Playing: {(bgmSource.isPlaying ? "Yes" : "No")}");
        if (backgroundSource != null)
            EditorGUILayout.LabelField($"Background Audio Playing: {(backgroundSource.isPlaying ? "Yes" : "No")}");

        EditorGUILayout.Space();

        // Volume Controls
        EditorGUILayout.LabelField("Volume Controls", EditorStyles.boldLabel);
        
        EditorGUI.BeginChangeCheck();
        bgmVolumeSlider = EditorGUILayout.Slider("BGM Volume", bgmVolumeSlider, 0f, 1f);
        if (EditorGUI.EndChangeCheck())
        {
            audioManager.SetBGMVolume(bgmVolumeSlider);
        }
        
        EditorGUI.BeginChangeCheck();
        sfxVolumeSlider = EditorGUILayout.Slider("SFX Volume", sfxVolumeSlider, 0f, 1f);
        if (EditorGUI.EndChangeCheck())
        {
            audioManager.SetSFXVolume(sfxVolumeSlider);
        }
        
        EditorGUI.BeginChangeCheck();
        backgroundVolumeSlider = EditorGUILayout.Slider("Background Volume", backgroundVolumeSlider, 0f, 1f);
        if (EditorGUI.EndChangeCheck())
        {
            audioManager.SetBackgroundVolume(backgroundVolumeSlider);
        }

        EditorGUILayout.Space();

        // BGM Controls
        EditorGUILayout.LabelField("Background Music Controls", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Play BGM"))
        {
            audioManager.PlayBGM();
        }
        if (GUILayout.Button("Stop BGM"))
        {
            audioManager.StopBGM();
        }
        EditorGUILayout.EndHorizontal();
        
        fadeTime = EditorGUILayout.FloatField("Fade Time", fadeTime);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Fade In BGM"))
        {
            audioManager.FadeInBGM(fadeTime);
        }
        if (GUILayout.Button("Fade Out BGM"))
        {
            audioManager.FadeOutBGM(fadeTime);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Background Audio Controls
        EditorGUILayout.LabelField("Background Audio Controls", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Play Forest Wind"))
        {
            audioManager.PlayForestWind();
        }
        if (GUILayout.Button("Stop Background Audio"))
        {
            audioManager.StopBackgroundAudio();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Fade In Background"))
        {
            audioManager.FadeInBackgroundAudio(fadeTime);
        }
        if (GUILayout.Button("Fade Out Background"))
        {
            audioManager.FadeOutBackgroundAudio(fadeTime);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Sound Effects
        EditorGUILayout.LabelField("Sound Effects", EditorStyles.boldLabel);
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
        
        if (GUILayout.Button("Logo Appear SFX"))
        {
            audioManager.PlayLogoAppear();
        }
        
        if (GUILayout.Button("Drum Hit SFX"))
        {
            audioManager.PlayDrumHit();
        }
        
        if (GUILayout.Button("Samurai Dash SFX"))
        {
            audioManager.PlaySamuraiDash();
        }
        
        EditorGUILayout.LabelField("Sword Clash Effects:", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Sword Clash 1"))
        {
            audioManager.PlaySwordClash1();
        }
        if (GUILayout.Button("Sword Clash 2"))
        {
            audioManager.PlaySwordClash2();
        }
        if (GUILayout.Button("Sword Clash 3"))
        {
            audioManager.PlaySwordClash3();
        }
        EditorGUILayout.EndHorizontal();
        
        if (GUILayout.Button("Random Sword Clash"))
        {
            audioManager.PlayRandomSwordClash();
        }
        
        if (GUILayout.Button("Japanese Drum SFX"))
        {
            audioManager.PlayJapaneseDrum();
        }
        
        if (GUILayout.Button("Heavy Drum SFX"))
        {
            audioManager.PlayHeavyDrum();
        }
        
        if (GUILayout.Button("Hurt SFX"))
        {
            audioManager.PlayHurt();
        }
        
        if (GUILayout.Button("Sword Unsheath SFX"))
        {
            audioManager.PlaySwordUnsheath();
        }
        
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        // Quick Actions
        EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Stop All Audio"))
        {
            audioManager.StopBGM();
            audioManager.StopBackgroundAudio();
        }
        if (GUILayout.Button("Refresh Manager"))
        {
            RefreshAudioManager();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Audio Source Info
        if (GUILayout.Button("Show Audio Sources Info"))
        {
            ShowAudioSourceInfo();
        }
    }

    private void ShowAudioSourceInfo()
    {
        if (audioManager == null) return;

        var bgmSource = GetPrivateField<AudioSource>(audioManager, "bgmSource");
        var sfxSource = GetPrivateField<AudioSource>(audioManager, "sfxSource");
        var backgroundSource = GetPrivateField<AudioSource>(audioManager, "backgroundSource");

        string info = "Audio Sources Information:\n\n";
        
        if (bgmSource != null)
        {
            info += $"BGM Source:\n";
            info += $"  - Volume: {bgmSource.volume:F2}\n";
            info += $"  - Playing: {bgmSource.isPlaying}\n";
            info += $"  - Clip: {(bgmSource.clip != null ? bgmSource.clip.name : "None")}\n\n";
        }
        
        if (sfxSource != null)
        {
            info += $"SFX Source:\n";
            info += $"  - Volume: {sfxSource.volume:F2}\n";
            info += $"  - Playing: {sfxSource.isPlaying}\n\n";
        }
        
        if (backgroundSource != null)
        {
            info += $"Background Source:\n";
            info += $"  - Volume: {backgroundSource.volume:F2}\n";
            info += $"  - Playing: {backgroundSource.isPlaying}\n";
            info += $"  - Clip: {(backgroundSource.clip != null ? backgroundSource.clip.name : "None")}\n";
        }

        EditorUtility.DisplayDialog("Audio Sources Info", info, "OK");
    }

    private T GetPrivateField<T>(object obj, string fieldName)
    {
        var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            return (T)field.GetValue(obj);
        }
        return default(T);
    }
}