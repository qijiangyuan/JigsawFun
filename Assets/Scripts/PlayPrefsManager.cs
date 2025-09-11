using UnityEngine;

public class PlayPrefsManager : MonoBehaviour
{
    private const string LEVEL_PROGRESS_KEY = "LevelProgress";
    private const string HINT_FREE_KEY = "HintManager_FreeHints";
    private const string HINT_REWARDED_KEY = "HintManager_RewardedHints";
    private const string HINT_LAST_TIME_KEY = "HintManager_LastHintTime";
    private const string HINT_COOLDOWN_KEY = "HintManager_IsOnCooldown";
    private const string PUZZLE_STATE_KEY = "PuzzleState";
    private const string PUZZLE_PIECES_KEY = "PuzzlePieces";
    private const string ENCRYPTION_KEY = "JigsawFun2024";

    private static PlayPrefsManager instance;
    public static PlayPrefsManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<PlayPrefsManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("PlayPrefsManager");
                    instance = go.AddComponent<PlayPrefsManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SaveLevelProgress(int levelNumber, bool isCompleted)
    {
        string key = $"{LEVEL_PROGRESS_KEY}_{levelNumber}";
        string encryptedValue = EncryptData(isCompleted.ToString());
        PlayerPrefs.SetString(key, encryptedValue);
        PlayerPrefs.Save();
    }

    public bool GetLevelProgress(int levelNumber)
    {
        string key = $"{LEVEL_PROGRESS_KEY}_{levelNumber}";
        if (!PlayerPrefs.HasKey(key))
        {
            return false;
        }

        string encryptedValue = PlayerPrefs.GetString(key);
        string decryptedValue = DecryptData(encryptedValue);
        return bool.Parse(decryptedValue);
    }

    public void ClearAllProgress()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }

    #region Puzzle State Management
    [System.Serializable]
    public class PuzzlePieceData
    {
        public int row;
        public int col;
        public Vector3 currentPosition;
        public Vector3 correctPosition;
        public bool isPlaced;
    }

    [System.Serializable]
    public class PuzzleStateData
    {
        public int gridSize;
        public PuzzlePieceData[] pieces;
    }

    public void SavePuzzleState(int gridSize, PuzzlePiece[] pieces)
    {
        PuzzleStateData stateData = new PuzzleStateData
        {
            gridSize = gridSize,
            pieces = new PuzzlePieceData[pieces.Length]
        };

        for (int i = 0; i < pieces.Length; i++)
        {
            stateData.pieces[i] = new PuzzlePieceData
            {
                row = pieces[i].row,
                col = pieces[i].col,
                currentPosition = pieces[i].transform.position,
                correctPosition = pieces[i].correctPosition,
                isPlaced = pieces[i].isPlaced
            };
        }

        string json = JsonUtility.ToJson(stateData);
        string encryptedJson = EncryptData(json);
        PlayerPrefs.SetString(PUZZLE_STATE_KEY, encryptedJson);
        PlayerPrefs.Save();
    }

    public PuzzleStateData LoadPuzzleState()
    {
        if (!PlayerPrefs.HasKey(PUZZLE_STATE_KEY))
        {
            return null;
        }

        string encryptedJson = PlayerPrefs.GetString(PUZZLE_STATE_KEY);
        string json = DecryptData(encryptedJson);
        return JsonUtility.FromJson<PuzzleStateData>(json);
    }

    public void ClearPuzzleState()
    {
        PlayerPrefs.DeleteKey(PUZZLE_STATE_KEY);
        PlayerPrefs.Save();
    }

    #endregion

    #region Hint System Data Management
    public void SaveHintData(int freeHints, int rewardedHints, float lastHintTime, bool isOnCooldown)
    {
        PlayerPrefs.SetInt(HINT_FREE_KEY, freeHints);
        PlayerPrefs.SetInt(HINT_REWARDED_KEY, rewardedHints);
        PlayerPrefs.SetFloat(HINT_LAST_TIME_KEY, lastHintTime);
        PlayerPrefs.SetInt(HINT_COOLDOWN_KEY, isOnCooldown ? 1 : 0);
        PlayerPrefs.Save();
    }

    public (int freeHints, int rewardedHints, float lastHintTime, bool isOnCooldown) LoadHintData(int defaultFreeHints)
    {
        int freeHints = PlayerPrefs.GetInt(HINT_FREE_KEY, defaultFreeHints);
        int rewardedHints = PlayerPrefs.GetInt(HINT_REWARDED_KEY, 0);
        float lastHintTime = PlayerPrefs.GetFloat(HINT_LAST_TIME_KEY, 0f);
        bool isOnCooldown = PlayerPrefs.GetInt(HINT_COOLDOWN_KEY, 0) == 1;

        return (freeHints, rewardedHints, lastHintTime, isOnCooldown);
    }
    #endregion

    private string EncryptData(string data)
    {
        string result = "";
        for (int i = 0; i < data.Length; i++)
        {
            result += (char)(data[i] ^ ENCRYPTION_KEY[i % ENCRYPTION_KEY.Length]);
        }
        return System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(result));
    }

    private string DecryptData(string encryptedData)
    {
        byte[] base64Bytes = System.Convert.FromBase64String(encryptedData);
        string base64Decoded = System.Text.Encoding.UTF8.GetString(base64Bytes);
        string result = "";
        for (int i = 0; i < base64Decoded.Length; i++)
        {
            result += (char)(base64Decoded[i] ^ ENCRYPTION_KEY[i % ENCRYPTION_KEY.Length]);
        }
        return result;
    }
}