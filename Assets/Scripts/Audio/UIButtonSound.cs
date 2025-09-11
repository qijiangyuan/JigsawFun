using UnityEngine;
using UnityEngine.UI;

namespace JigsawFun.Audio
{
    /// <summary>
    /// UI按钮音效组件
    /// 将此组件挂载到按钮上，可以在按钮点击时播放指定的音效
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UIButtonSound : MonoBehaviour
    {
        [Tooltip("按钮点击时播放的音效")]
        public AudioClip buttonSound;

        private Button button;

        /// <summary>
        /// 初始化
        /// </summary>
        private void Awake()
        {
            // 获取按钮组件
            button = GetComponent<Button>();

            // 添加点击事件监听
            if (button != null)
            {
                button.onClick.AddListener(PlayButtonSound);
            }
        }

        /// <summary>
        /// 播放按钮音效
        /// </summary>
        private void PlayButtonSound()
        {
            // 如果设置了音效，则播放指定音效
            if (buttonSound != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayUISound(buttonSound);
            }
            // 否则尝试播放默认的UI点击音效
            else if (AudioManager.Instance != null && AudioManager.Instance.HasSoundGroup("UIClick"))
            {
                AudioManager.Instance.PlayRandomSound("UIClick");
            }
        }

        /// <summary>
        /// 组件销毁时移除事件监听
        /// </summary>
        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(PlayButtonSound);
            }
        }
    }
}