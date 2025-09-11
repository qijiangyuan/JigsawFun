using UnityEngine;

namespace JigsawFun.Audio
{
    /// <summary>
    /// 音频扩展方法
    /// 提供便捷的音频操作扩展方法
    /// </summary>
    public static class AudioExtensions
    {
        /// <summary>
        /// 播放UI点击音效
        /// </summary>
        /// <param name="component">MonoBehaviour组件</param>
        /// <param name="clip">音效剪辑</param>
        public static void PlayUIClickSound(this MonoBehaviour component, AudioClip clip = null)
        {
            if (AudioManager.Instance != null)
            {
                if (clip != null)
                {
                    AudioManager.Instance.PlayUISound(clip);
                }
                else if (AudioManager.Instance.HasSoundGroup("UIClick"))
                {
                    AudioManager.Instance.PlayRandomSound("UIClick");
                }
            }
        }

        /// <summary>
        /// 播放UI悬停音效
        /// </summary>
        /// <param name="component">MonoBehaviour组件</param>
        /// <param name="clip">音效剪辑</param>
        public static void PlayUIHoverSound(this MonoBehaviour component, AudioClip clip = null)
        {
            if (AudioManager.Instance != null)
            {
                if (clip != null)
                {
                    AudioManager.Instance.PlayUISound(clip);
                }
                else if (AudioManager.Instance.HasSoundGroup("UIHover"))
                {
                    AudioManager.Instance.PlayRandomSound("UIHover");
                }
            }
        }

        /// <summary>
        /// 播放游戏音效
        /// </summary>
        /// <param name="component">MonoBehaviour组件</param>
        /// <param name="clip">音效剪辑</param>
        /// <param name="volume">音量</param>
        public static void PlayGameSound(this MonoBehaviour component, AudioClip clip, float volume = 1.0f)
        {
            if (AudioManager.Instance != null && clip != null)
            {
                AudioManager.Instance.PlaySound(clip, volume);
            }
        }

        /// <summary>
        /// 从指定组播放随机游戏音效
        /// </summary>
        /// <param name="component">MonoBehaviour组件</param>
        /// <param name="groupName">音效组名称</param>
        /// <param name="volume">音量</param>
        public static void PlayRandomGameSound(this MonoBehaviour component, string groupName, float volume = 1.0f)
        {
            if (AudioManager.Instance != null && !string.IsNullOrEmpty(groupName))
            {
                AudioManager.Instance.PlayRandomSound(groupName, volume);
            }
        }
    }
}