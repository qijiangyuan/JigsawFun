using UnityEngine;
using UnityEngine.UI;

namespace JigsawFun.Audio
{
    [RequireComponent(typeof(Toggle))]
    public class UIToggleSound : MonoBehaviour
    {
        public AudioClip toggleSound;
        public bool playOnOn = true;
        public bool playOnOff = false;

        private Toggle toggle;

        private void Awake()
        {
            toggle = GetComponent<Toggle>();
            if (toggle != null)
            {
                toggle.onValueChanged.AddListener(HandleValueChanged);
            }
        }

        private void HandleValueChanged(bool isOn)
        {
            if ((isOn && !playOnOn) || (!isOn && !playOnOff)) return;

            if (toggleSound != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayUISound(toggleSound);
            }
            else if (AudioManager.Instance != null && AudioManager.Instance.HasSoundGroup("UIClick"))
            {
                AudioManager.Instance.PlayRandomSound("UIClick");
            }
        }

        private void OnDestroy()
        {
            if (toggle != null)
            {
                toggle.onValueChanged.RemoveListener(HandleValueChanged);
            }
        }
    }
}

