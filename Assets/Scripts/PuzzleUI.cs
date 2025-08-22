using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 拼图游戏UI控制器
/// </summary>
public class PuzzleUI : MonoBehaviour
{
    [Header("UI组件")]
    public Button shuffleButton;
    public Button resetButton;
    public Button solutionButton;
    public Button newGameButton;
    public Slider difficultySlider;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI difficultyText;
    public GameObject completionPanel;
    public TextMeshProUGUI completionText;
    
    [Header("游戏设置")]
    public PuzzleBoard puzzleBoard;
    
    private bool gameCompleted = false;
    
    void Start()
    {
        // 查找PuzzleBoard组件
        if (puzzleBoard == null)
        {
            puzzleBoard = FindObjectOfType<PuzzleBoard>();
        }
        
        // 设置按钮事件
        SetupButtons();
        
        // 设置难度滑块
        SetupDifficultySlider();
        
        // 隐藏完成面板
        if (completionPanel != null)
        {
            completionPanel.SetActive(false);
        }
    }
    
    void Update()
    {
        // 更新进度显示
        UpdateProgressDisplay();
        
        // 检查游戏完成状态
        CheckGameCompletion();
    }
    
    /// <summary>
    /// 设置按钮事件
    /// </summary>
    void SetupButtons()
    {
        if (shuffleButton != null)
        {
            shuffleButton.onClick.AddListener(OnShuffleClicked);
        }
        
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(OnResetClicked);
        }
        
        if (solutionButton != null)
        {
            solutionButton.onClick.AddListener(OnSolutionClicked);
        }
        
        if (newGameButton != null)
        {
            newGameButton.onClick.AddListener(OnNewGameClicked);
        }
    }
    
    /// <summary>
    /// 设置难度滑块
    /// </summary>
    void SetupDifficultySlider()
    {
        if (difficultySlider != null)
        {
            difficultySlider.minValue = 2;
            difficultySlider.maxValue = 8;
            difficultySlider.wholeNumbers = true;
            difficultySlider.value = puzzleBoard != null ? puzzleBoard.gridSize : 4;
            
            difficultySlider.onValueChanged.AddListener(OnDifficultyChanged);
            
            // 初始化难度文本
            UpdateDifficultyText((int)difficultySlider.value);
        }
    }
    
    /// <summary>
    /// 更新进度显示
    /// </summary>
    void UpdateProgressDisplay()
    {
        if (progressText != null && puzzleBoard != null)
        {
            float progress = puzzleBoard.GetGameProgress();
            int percentage = Mathf.RoundToInt(progress * 100);
            progressText.text = $"进度: {percentage}%";
        }
    }
    
    /// <summary>
    /// 检查游戏完成状态
    /// </summary>
    void CheckGameCompletion()
    {
        if (puzzleBoard != null && !gameCompleted)
        {
            if (puzzleBoard.IsGameCompleted())
            {
                gameCompleted = true;
                ShowCompletionPanel();
            }
        }
    }
    
    /// <summary>
    /// 显示完成面板
    /// </summary>
    void ShowCompletionPanel()
    {
        if (completionPanel != null)
        {
            completionPanel.SetActive(true);
            
            if (completionText != null)
            {
                completionText.text = "恭喜！拼图完成！";
            }
            
            // 3秒后自动隐藏
            Invoke(nameof(HideCompletionPanel), 3f);
        }
    }
    
    /// <summary>
    /// 隐藏完成面板
    /// </summary>
    void HideCompletionPanel()
    {
        if (completionPanel != null)
        {
            completionPanel.SetActive(false);
        }
        gameCompleted = false;
    }
    
    /// <summary>
    /// 打乱按钮点击事件
    /// </summary>
    void OnShuffleClicked()
    {
        if (puzzleBoard != null)
        {
            puzzleBoard.ShufflePieces();
            gameCompleted = false;
            HideCompletionPanel();
        }
    }
    
    /// <summary>
    /// 重置按钮点击事件
    /// </summary>
    void OnResetClicked()
    {
        if (puzzleBoard != null)
        {
            puzzleBoard.ResetPuzzle();
            gameCompleted = false;
            HideCompletionPanel();
        }
    }
    
    /// <summary>
    /// 解决方案按钮点击事件
    /// </summary>
    void OnSolutionClicked()
    {
        //if (puzzleBoard != null)
        //{
        //    puzzleBoard.ShowSolution();
        //}
    }
    
    /// <summary>
    /// 新游戏按钮点击事件
    /// </summary>
    void OnNewGameClicked()
    {
        if (puzzleBoard != null && difficultySlider != null)
        {
            int newDifficulty = (int)difficultySlider.value;
            puzzleBoard.SetDifficulty(newDifficulty);
            gameCompleted = false;
            HideCompletionPanel();
        }
    }
    
    /// <summary>
    /// 难度改变事件
    /// </summary>
    void OnDifficultyChanged(float value)
    {
        int difficulty = (int)value;
        UpdateDifficultyText(difficulty);
    }
    
    /// <summary>
    /// 更新难度文本
    /// </summary>
    void UpdateDifficultyText(int difficulty)
    {
        if (difficultyText != null)
        {
            string difficultyName = GetDifficultyName(difficulty);
            difficultyText.text = $"难度: {difficulty}x{difficulty} ({difficultyName})";
        }
    }
    
    /// <summary>
    /// 获取难度名称
    /// </summary>
    string GetDifficultyName(int gridSize)
    {
        switch (gridSize)
        {
            case 2: return "入门";
            case 3: return "简单";
            case 4: return "普通";
            case 5: return "中等";
            case 6: return "困难";
            case 7: return "专家";
            case 8: return "大师";
            default: return "自定义";
        }
    }
    
    /// <summary>
    /// 设置按钮可交互状态
    /// </summary>
    public void SetButtonsInteractable(bool interactable)
    {
        if (shuffleButton != null) shuffleButton.interactable = interactable;
        if (resetButton != null) resetButton.interactable = interactable;
        if (solutionButton != null) solutionButton.interactable = interactable;
        if (newGameButton != null) newGameButton.interactable = interactable;
        if (difficultySlider != null) difficultySlider.interactable = interactable;
    }
    
    /// <summary>
    /// 显示提示信息
    /// </summary>
    public void ShowHint(string message)
    {
        if (completionText != null)
        {
            completionText.text = message;
            completionPanel.SetActive(true);
            
            // 2秒后隐藏
            Invoke(nameof(HideCompletionPanel), 2f);
        }
    }
}