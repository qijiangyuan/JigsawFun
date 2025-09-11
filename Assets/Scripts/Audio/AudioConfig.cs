using UnityEngine;

namespace JigsawFun.Audio
{
    /// <summary>
    /// 音频配置类
    /// 负责存储和管理音频设置
    /// </summary>
    [System.Serializable]
    public class AudioConfig
    {
        // 音量设置
        public float masterVolume = 1.0f;
        public float musicVolume = 0.7f;
        public float sfxVolume = 1.0f;
        public float uiSoundVolume = 0.8f;
        
        // 静音设置
        public bool isMusicMuted = false;
        public bool isSfxMuted = false;
        public bool isUISoundMuted = false;
        
        // PlayerPrefs 键名
        private const string MASTER_VOLUME_KEY = "MasterVolume";
        private const string MUSIC_VOLUME_KEY = "MusicVolume";
        private const string SFX_VOLUME_KEY = "SfxVolume";
        private const string UI_VOLUME_KEY = "UIVolume";
        private const string MUSIC_MUTED_KEY = "MusicMuted";
        private const string SFX_MUTED_KEY = "SfxMuted";
        private const string UI_MUTED_KEY = "UIMuted";
        
        /// <summary>
        /// 从PlayerPrefs加载音频设置
        /// </summary>
        public void LoadSettings()
        {
            masterVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1.0f);
            musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 0.7f);
            sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1.0f);
            uiSoundVolume = PlayerPrefs.GetFloat(UI_VOLUME_KEY, 0.8f);
            
            isMusicMuted = PlayerPrefs.GetInt(MUSIC_MUTED_KEY, 0) == 1;
            isSfxMuted = PlayerPrefs.GetInt(SFX_MUTED_KEY, 0) == 1;
            isUISoundMuted = PlayerPrefs.GetInt(UI_MUTED_KEY, 0) == 1;
        }
        
        /// <summary>
        /// 保存音频设置到PlayerPrefs
        /// </summary>
        public void SaveSettings()
        {
            PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, masterVolume);
            PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, musicVolume);
            PlayerPrefs.SetFloat(SFX_VOLUME_KEY, sfxVolume);
            PlayerPrefs.SetFloat(UI_VOLUME_KEY, uiSoundVolume);
            
            PlayerPrefs.SetInt(MUSIC_MUTED_KEY, isMusicMuted ? 1 : 0);
            PlayerPrefs.SetInt(SFX_MUTED_KEY, isSfxMuted ? 1 : 0);
            PlayerPrefs.SetInt(UI_MUTED_KEY, isUISoundMuted ? 1 : 0);
            
            PlayerPrefs.Save();
        }
        
        /// <summary>
        /// 应用设置到AudioManager
        /// </summary>
        public void ApplySettings()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMasterVolume(masterVolume);
                AudioManager.Instance.SetMusicVolume(musicVolume);
                AudioManager.Instance.SetSfxVolume(sfxVolume);
                AudioManager.Instance.SetUISoundVolume(uiSoundVolume);
                
                // 应用静音设置
                AudioManager.Instance.MuteMusic(isMusicMuted);
                AudioManager.Instance.MuteSfx(isSfxMuted);
                AudioManager.Instance.MuteUISound(isUISoundMuted);
            }
        }
        
        /// <summary>
        /// 重置为默认设置
        /// </summary>
        public void ResetToDefaults()
        {
            masterVolume = 1.0f;
            musicVolume = 0.7f;
            sfxVolume = 1.0f;
            uiSoundVolume = 0.8f;
            
            isMusicMuted = false;
            isSfxMuted = false;
            isUISoundMuted = false;
            
            SaveSettings();
        }
        
        /// <summary>
        /// 静态方法：加载音频配置
        /// </summary>
        /// <returns>加载的音频配置</returns>
        public static AudioConfig Load()
        {
            AudioConfig config = new AudioConfig();
            config.LoadSettings();
            return config;
        }
    }
}