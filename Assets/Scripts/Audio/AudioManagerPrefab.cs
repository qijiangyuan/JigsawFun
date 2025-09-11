using UnityEngine;

namespace JigsawFun.Audio
{
    /// <summary>
    /// AudioManager预制体组件
    /// 用于在Unity编辑器中创建AudioManager预制体
    /// </summary>
    public class AudioManagerPrefab : MonoBehaviour
    {
        [Header("UI音效")]
        public AudioClip uiClickSound;
        public AudioClip uiHoverSound;
        public AudioClip uiBackSound;
        
        [Header("背景音乐")]
        public AudioClip[] backgroundMusic;
        
        private void Awake()
        {
            // 确保场景中只有一个AudioManager
            if (AudioManager.Instance != null && AudioManager.Instance.gameObject != gameObject)
            {
                Destroy(gameObject);
                return;
            }
            
            // 添加AudioManager组件
            AudioManager audioManager = gameObject.GetComponent<AudioManager>();
            if (audioManager == null)
            {
                audioManager = gameObject.AddComponent<AudioManager>();
            }
            
            // 设置背景音乐
            if (backgroundMusic != null && backgroundMusic.Length > 0)
            {
                audioManager.backgroundMusic = backgroundMusic;
            }
            
            // 设置UI音效
            if (uiClickSound != null)
            {
                audioManager.AddSoundToGroup("UIClick", uiClickSound);
            }
            
            if (uiHoverSound != null)
            {
                audioManager.AddSoundToGroup("UIHover", uiHoverSound);
            }
            
            if (uiBackSound != null)
            {
                audioManager.AddSoundToGroup("UIBack", uiBackSound);
            }
        }
    }
}