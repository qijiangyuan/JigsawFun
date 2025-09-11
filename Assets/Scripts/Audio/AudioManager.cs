using System.Collections.Generic;
using UnityEngine;

namespace JigsawFun.Audio
{
    /// <summary>
    /// 音频管理器
    /// 负责管理游戏中的所有音频，包括背景音乐和音效
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [System.Serializable]
        public class SoundGroup
        {
            public string groupName;
            public AudioClip[] clips;
        }

        #region 单例
        public static AudioManager Instance { get; private set; }

        private void Awake()
        {
            // 单例模式实现
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAudioSources();
                CacheSoundGroups();
                LoadAudioConfig();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            // 注册到游戏管理器
            RegisterGameSounds();

            if (playMusicOnAwake && backgroundMusic.Length > 0)
            {
                PlayBackgroundMusic(0);
            }
        }
        
        /// <summary>
        /// 注册游戏音效到PuzzleGameManager
        /// </summary>
        private void RegisterGameSounds()
        {
            var gameManager = PuzzleGameManager.Instance;
            if (gameManager != null)
            {
                // 如果PuzzleGameManager已经有音效设置，则将其添加到音效组
                if (gameManager.snapSound != null)
                {
                    AddSoundToGroup("Snap", gameManager.snapSound);
                }
                
                if (gameManager.completeSound != null)
                {
                    AddSoundToGroup("Complete", gameManager.completeSound);
                }
                
                if (gameManager.errorSound != null)
                {
                    AddSoundToGroup("Error", gameManager.errorSound);
                }
            }
        }
        
        /// <summary>
        /// 加载音频配置
        /// </summary>
        private void LoadAudioConfig()
        {
            AudioConfig config = AudioConfig.Load();
            
            // 应用配置
            SetMasterVolume(config.masterVolume);
            SetMusicVolume(config.musicVolume);
            SetSfxVolume(config.sfxVolume);
            SetUISoundVolume(config.uiSoundVolume);
            
            // 应用静音设置
            MuteMusic(config.isMusicMuted);
            MuteSfx(config.isSfxMuted);
            MuteUISound(config.isUISoundMuted);
        }
        
        /// <summary>
        /// 保存音频配置
        /// </summary>
        public void SaveAudioConfig()
        {
            AudioConfig config = new AudioConfig
            {
                masterVolume = masterVolume,
                musicVolume = musicVolume,
                sfxVolume = sfxVolume,
                uiSoundVolume = uiSoundVolume,
                isMusicMuted = musicSource.mute,
                isSfxMuted = sfxSources[0].mute,
                isUISoundMuted = uiSoundMuted
            };
            
            config.SaveSettings();
        }
        #endregion

        [Header("音频设置")]
        [Range(0f, 1f)]
        public float masterVolume = 1.0f;
        [Range(0f, 1f)]
        public float musicVolume = 0.7f;
        [Range(0f, 1f)]
        public float sfxVolume = 1.0f;
        [Range(0f, 1f)]
        public float uiSoundVolume = 0.8f;
        
        // 静音状态
        private bool uiSoundMuted = false;

        [Header("背景音乐")]
        public AudioClip[] backgroundMusic;
        public bool playMusicOnAwake = true;
        public bool loopBackgroundMusic = true;

        [Header("音效")]
        public SoundGroup[] soundGroups;

        // 音频源
        private AudioSource musicSource;
        private List<AudioSource> sfxSources = new List<AudioSource>();
        private int maxSfxSources = 5;

        // 缓存音效
        private Dictionary<string, AudioClip[]> soundCache = new Dictionary<string, AudioClip[]>();

        /// <summary>
        /// 初始化音频源
        /// </summary>
        private void InitializeAudioSources()
        {
            // 创建音乐播放器
            GameObject musicObj = new GameObject("MusicSource");
            musicObj.transform.SetParent(transform);
            musicSource = musicObj.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = loopBackgroundMusic;
            musicSource.volume = musicVolume * masterVolume;

            // 创建音效播放器池
            for (int i = 0; i < maxSfxSources; i++)
            {
                GameObject sfxObj = new GameObject($"SFXSource_{i}");
                sfxObj.transform.SetParent(transform);
                AudioSource source = sfxObj.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.volume = sfxVolume * masterVolume;
                sfxSources.Add(source);
            }
        }

        /// <summary>
        /// 缓存所有音效组
        /// </summary>
        private void CacheSoundGroups()
        {
            foreach (var group in soundGroups)
            {
                if (!string.IsNullOrEmpty(group.groupName) && group.clips != null && group.clips.Length > 0)
                {
                    soundCache[group.groupName] = group.clips;
                }
            }
        }
        
        /// <summary>
        /// 添加音效到指定组
        /// </summary>
        /// <param name="groupName">组名称</param>
        /// <param name="clip">音效剪辑</param>
        public void AddSoundToGroup(string groupName, AudioClip clip)
        {
            if (string.IsNullOrEmpty(groupName) || clip == null)
                return;
                
            // 如果组不存在，创建新组
            if (!soundCache.ContainsKey(groupName))
            {
                soundCache[groupName] = new AudioClip[] { clip };
            }
            else
            {
                // 检查是否已存在该音效
                bool exists = false;
                foreach (var existingClip in soundCache[groupName])
                {
                    if (existingClip == clip)
                    {
                        exists = true;
                        break;
                    }
                }
                
                // 如果不存在，添加到组中
                if (!exists)
                {
                    AudioClip[] newArray = new AudioClip[soundCache[groupName].Length + 1];
                    soundCache[groupName].CopyTo(newArray, 0);
                    newArray[newArray.Length - 1] = clip;
                    soundCache[groupName] = newArray;
                }
            }
        }



        #region 背景音乐控制

        /// <summary>
        /// 播放背景音乐
        /// </summary>
        /// <param name="index">音乐索引</param>
        public void PlayBackgroundMusic(int index)
        {
            if (backgroundMusic.Length == 0 || index < 0 || index >= backgroundMusic.Length)
                return;

            musicSource.clip = backgroundMusic[index];
            musicSource.volume = musicVolume * masterVolume;
            musicSource.Play();
        }

        /// <summary>
        /// 播放指定的背景音乐
        /// </summary>
        /// <param name="clip">音乐剪辑</param>
        public void PlayBackgroundMusic(AudioClip clip)
        {
            if (clip == null)
                return;

            musicSource.clip = clip;
            musicSource.volume = musicVolume * masterVolume;
            musicSource.Play();
        }

        /// <summary>
        /// 暂停背景音乐
        /// </summary>
        public void PauseBackgroundMusic()
        {
            if (musicSource.isPlaying)
            {
                musicSource.Pause();
            }
        }

        /// <summary>
        /// 恢复背景音乐
        /// </summary>
        public void ResumeBackgroundMusic()
        {
            if (!musicSource.isPlaying)
            {
                musicSource.UnPause();
            }
        }

        /// <summary>
        /// 停止背景音乐
        /// </summary>
        public void StopBackgroundMusic()
        {
            musicSource.Stop();
        }

        /// <summary>
        /// 设置背景音乐音量
        /// </summary>
        /// <param name="volume">音量值 (0-1)</param>
        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            musicSource.volume = musicVolume * masterVolume;
        }

        #endregion

        #region 音效控制

        /// <summary>
        /// 获取可用的音效播放器
        /// </summary>
        /// <returns>音效播放器</returns>
        private AudioSource GetAvailableSfxSource()
        {
            // 查找未在播放的音频源
            foreach (var source in sfxSources)
            {
                if (!source.isPlaying)
                {
                    return source;
                }
            }

            // 如果所有音频源都在播放，使用第一个（最早的）
            return sfxSources[0];
        }

        /// <summary>
        /// 播放音效
        /// </summary>
        /// <param name="clip">音效剪辑</param>
        /// <param name="volume">音量 (可选)</param>
        /// <returns>音频源</returns>
        public AudioSource PlaySound(AudioClip clip, float volume = 1.0f)
        {
            if (clip == null)
                return null;

            AudioSource source = GetAvailableSfxSource();
            source.clip = clip;
            source.volume = sfxVolume * masterVolume * volume;
            source.Play();

            return source;
        }

        /// <summary>
        /// 检查是否存在指定的音效组
        /// </summary>
        /// <param name="groupName">音效组名称</param>
        /// <returns>是否存在</returns>
        public bool HasSoundGroup(string groupName)
        {
            return !string.IsNullOrEmpty(groupName) && soundCache.ContainsKey(groupName) && soundCache[groupName].Length > 0;
        }
        
        /// <summary>
        /// 从指定组中播放随机音效
        /// </summary>
        /// <param name="groupName">音效组名称</param>
        /// <param name="volume">音量 (可选)</param>
        /// <returns>音频源</returns>
        public AudioSource PlayRandomSound(string groupName, float volume = 1.0f)
        {
            if (!HasSoundGroup(groupName))
                return null;

            AudioClip[] clips = soundCache[groupName];
            int randomIndex = Random.Range(0, clips.Length);
            return PlaySound(clips[randomIndex], volume);
        }

        /// <summary>
        /// 从指定组中播放指定索引的音效
        /// </summary>
        /// <param name="groupName">音效组名称</param>
        /// <param name="index">音效索引</param>
        /// <param name="volume">音量 (可选)</param>
        /// <returns>音频源</returns>
        public AudioSource PlaySoundFromGroup(string groupName, int index, float volume = 1.0f)
        {
            if (!soundCache.ContainsKey(groupName) || 
                index < 0 || 
                index >= soundCache[groupName].Length)
                return null;

            return PlaySound(soundCache[groupName][index], volume);
        }

        /// <summary>
        /// 播放UI音效
        /// </summary>
        /// <param name="clip">音效剪辑</param>
        /// <returns>音频源</returns>
        public AudioSource PlayUISound(AudioClip clip)
        {
            if (clip == null)
                return null;

            AudioSource source = GetAvailableSfxSource();
            source.clip = clip;
            source.volume = uiSoundVolume * masterVolume;
            source.mute = uiSoundMuted; // 应用UI音效静音设置
            source.Play();

            return source;
        }

        /// <summary>
        /// 设置音效音量
        /// </summary>
        /// <param name="volume">音量值 (0-1)</param>
        public void SetSfxVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            foreach (var source in sfxSources)
            {
                if (source.isPlaying)
                {
                    source.volume = sfxVolume * masterVolume;
                }
            }
        }

        /// <summary>
        /// 设置UI音效音量
        /// </summary>
        /// <param name="volume">音量值 (0-1)</param>
        public void SetUISoundVolume(float volume)
        {
            uiSoundVolume = Mathf.Clamp01(volume);
        }

        #endregion

        #region 主音量控制

        /// <summary>
        /// 设置主音量
        /// </summary>
        /// <param name="volume">音量值 (0-1)</param>
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            
            // 更新所有音频源的音量
            musicSource.volume = musicVolume * masterVolume;
            
            foreach (var source in sfxSources)
            {
                if (source.isPlaying)
                {
                    // 保持相对音量比例
                    source.volume = sfxVolume * masterVolume;
                }
            }
        }

        /// <summary>
        /// 静音所有音频
        /// </summary>
        /// <param name="mute">是否静音</param>
        public void MuteAll(bool mute)
        {
            musicSource.mute = mute;
            foreach (var source in sfxSources)
            {
                source.mute = mute;
            }
        }
        
        /// <summary>
        /// 静音背景音乐
        /// </summary>
        /// <param name="mute">是否静音</param>
        public void MuteMusic(bool mute)
        {
            musicSource.mute = mute;
        }
        
        /// <summary>
        /// 静音音效
        /// </summary>
        /// <param name="mute">是否静音</param>
        public void MuteSfx(bool mute)
        {
            foreach (var source in sfxSources)
            {
                source.mute = mute;
            }
        }
        
        /// <summary>
        /// 静音UI音效
        /// </summary>
        /// <param name="mute">是否静音</param>
        public void MuteUISound(bool mute)
        {
            // UI音效使用的是同一组音效播放器，这里通过标记来控制
            // 实际静音效果会在播放UI音效时应用
            uiSoundMuted = mute;
        }

        #endregion
    }
}