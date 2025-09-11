# AudioManager 使用说明

## 简介

AudioManager是一个用于管理游戏中背景音乐和音效的系统。它提供了以下功能：

- 背景音乐的播放、暂停、恢复和停止
- 音效的播放，包括随机播放和组内播放
- UI音效的播放
- 音量控制和静音功能
- 音频配置的保存和加载

## 使用方法

### 1. 在场景中添加AudioManager

有两种方式可以在场景中添加AudioManager：

#### 方式一：使用预制体（推荐）

1. 在场景中创建一个空的GameObject
2. 添加`AudioManagerPrefab`组件
3. 设置UI音效和背景音乐

#### 方式二：通过代码创建

```csharp
GameObject audioManagerObj = new GameObject("AudioManager");
JigsawFun.Audio.AudioManager audioManager = audioManagerObj.AddComponent<JigsawFun.Audio.AudioManager>();
```

### 2. 播放背景音乐

```csharp
// 播放指定的背景音乐
AudioManager.Instance.PlayMusic(musicClip);

// 暂停背景音乐
AudioManager.Instance.PauseMusic();

// 恢复背景音乐
AudioManager.Instance.ResumeMusic();

// 停止背景音乐
AudioManager.Instance.StopMusic();

// 设置背景音乐音量（0.0f - 1.0f）
AudioManager.Instance.SetMusicVolume(0.8f);
```

### 3. 播放音效

```csharp
// 播放单个音效
AudioManager.Instance.PlaySound(soundClip);

// 添加音效到组
AudioManager.Instance.AddSoundToGroup("Explosion", explosionClip);

// 随机播放组内音效
AudioManager.Instance.PlayRandomSound("Explosion");

// 设置音效音量（0.0f - 1.0f）
AudioManager.Instance.SetSfxVolume(0.7f);
```

### 4. 播放UI音效

```csharp
// 播放UI音效
AudioManager.Instance.PlayUISound(uiSoundClip);

// 使用扩展方法播放UI点击音效
button.PlayUIClickSound();

// 使用扩展方法播放UI悬停音效
button.PlayUIHoverSound();
```

### 5. 音量控制和静音

```csharp
// 设置主音量（0.0f - 1.0f）
AudioManager.Instance.SetMasterVolume(0.9f);

// 静音/取消静音背景音乐
AudioManager.Instance.MuteMusic(true); // 静音
AudioManager.Instance.MuteMusic(false); // 取消静音

// 静音/取消静音音效
AudioManager.Instance.MuteSfx(true); // 静音
AudioManager.Instance.MuteSfx(false); // 取消静音

// 静音/取消静音UI音效
AudioManager.Instance.MuteUISound(true); // 静音
AudioManager.Instance.MuteUISound(false); // 取消静音
```

### 6. 保存和加载音频配置

```csharp
// 保存当前音频配置
AudioManager.Instance.SaveAudioConfig();

// 加载音频配置
AudioManager.Instance.LoadAudioConfig();
```

## 注意事项

1. AudioManager使用单例模式，可以通过`AudioManager.Instance`在任何地方访问
2. 音频配置会自动保存到PlayerPrefs中
3. 如果需要在游戏启动时自动播放背景音乐，可以在AudioManagerPrefab中设置背景音乐