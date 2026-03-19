using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

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
    private const string SAVE_INDEX_KEY = "PuzzleSaveIndex";

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
        public float elapsedSeconds;
        public PuzzlePieceData[] pieces;
    }

    [System.Serializable]
    public class CompletedPuzzleData
    {
        public string imageId;
        public int difficulty;
        public float completionTimeSeconds;
        public long completedAtTicksUtc;
    }

    [System.Serializable]
    private class CompletedListWrapper { public CompletedPuzzleData[] items; }

    private const string COMPLETED_INDEX_KEY = "CompletedIndex";

    private string GetStateKey(string imageId)
    {
        return $"{PUZZLE_STATE_KEY}_{imageId}";
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

    private System.Collections.Generic.List<string> LoadSaveIndex()
    {
        string raw = PlayerPrefs.GetString(SAVE_INDEX_KEY, "");
        var list = new System.Collections.Generic.List<string>();
        if (!string.IsNullOrEmpty(raw))
        {
            try
            {
                var arr = JsonUtility.FromJson<StringListWrapper>(raw);
                if (arr != null && arr.items != null) list.AddRange(arr.items);
            }
            catch
            {
                string[] parts = raw.Split(',');
                foreach (var p in parts)
                {
                    if (!string.IsNullOrEmpty(p)) list.Add(p);
                }
            }
        }
        return list;
    }

    private void SaveSaveIndex(System.Collections.Generic.List<string> list)
    {
        var wrapper = new StringListWrapper { items = list.ToArray() };
        string json = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString(SAVE_INDEX_KEY, json);
        PlayerPrefs.Save();
    }

    [System.Serializable]
    private class StringListWrapper { public string[] items; }

    public System.Collections.Generic.List<string> GetAllUnfinishedImageIds()
    {
        var list = LoadSaveIndex();
        var filtered = new System.Collections.Generic.List<string>();
        foreach (var id in list)
        {
            var state = LoadPuzzleStateForImage(id);
            if (state != null && state.pieces != null && state.pieces.Length > 0)
            {
                bool unfinished = false;
                for (int i = 0; i < state.pieces.Length; i++)
                {
                    if (!state.pieces[i].isPlaced) { unfinished = true; break; }
                }
                if (unfinished) filtered.Add(id);
            }
        }
        SaveSaveIndex(filtered);
        return filtered;
    }

    private void AddToSaveIndex(string imageId)
    {
        var list = LoadSaveIndex();
        if (!list.Contains(imageId))
        {
            list.Add(imageId);
            SaveSaveIndex(list);
        }
    }

    private void RemoveFromSaveIndex(string imageId)
    {
        var list = LoadSaveIndex();
        if (list.Remove(imageId))
        {
            SaveSaveIndex(list);
        }
    }

    public void SaveCurrentSceneState(string imageId, int gridSize)
    {
        SaveCurrentSceneState(imageId, gridSize, -1f);
    }

    public void SaveCurrentSceneState(string imageId, int gridSize, float elapsedSeconds)
    {
        if (string.IsNullOrEmpty(imageId)) return;
        // 既要保存未放置的块（有 TileMovement），也要保存已放置的块（TileMovement 已被销毁）
        // 以 Tile GameObject 的命名规则 "TileGameObe_x_y" 为准收集全部块
        var allRenderers = GameObject.FindObjectsOfType<SpriteRenderer>(true);
        var pieces = new System.Collections.Generic.List<GameObject>();
        if (allRenderers != null)
        {
            for (int i = 0; i < allRenderers.Length; i++)
            {
                var sr = allRenderers[i];
                if (sr == null) continue;
                var go = sr.gameObject;
                if (go == null) continue;
                if (go.name.StartsWith("TileGameObe_"))
                {
                    pieces.Add(go);
                }
            }
        }
        if (pieces.Count == 0)
        {
            Debug.Log($"[PlayPrefs] SaveCurrentSceneState no pieces found for image={imageId}");
            return;
        }
        bool allPlaced = true;
        int placedCount = 0;
        int expected = Mathf.Max(1, gridSize) * Mathf.Max(1, gridSize);
        PuzzleStateData stateData = new PuzzleStateData
        {
            gridSize = gridSize,
            elapsedSeconds = elapsedSeconds >= 0f ? elapsedSeconds : 0f,
            pieces = new PuzzlePieceData[Mathf.Max(expected, pieces.Count)]
        };
        // 初始化为默认（避免缺块导致数组有 null）
        for (int idx = 0; idx < stateData.pieces.Length; idx++)
        {
            stateData.pieces[idx] = new PuzzlePieceData();
        }

        int filled = 0;
        for (int p = 0; p < pieces.Count; p++)
        {
            var go = pieces[p];
            if (go == null) continue;

            // 解析索引
            int cx = 0, cy = 0;
            bool parsed = false;
            // 期望格式：TileGameObe_x_y
            var parts = go.name.Split('_');
            if (parts.Length >= 3)
            {
                parsed = int.TryParse(parts[1], out cx) && int.TryParse(parts[2], out cy);
            }
            var tm = go.GetComponent<TileMovement>();
            if (tm != null && tm.tile != null)
            {
                cx = tm.tile.xIndex;
                cy = tm.tile.yIndex;
                parsed = true;
            }
            if (!parsed)
            {
                // 回退：根据世界坐标估算所在格子索引
                cx = Mathf.Clamp(Mathf.RoundToInt(go.transform.position.x / Tile.tileSize), 0, Mathf.Max(1, gridSize) - 1);
                cy = Mathf.Clamp(Mathf.RoundToInt(go.transform.position.y / Tile.tileSize), 0, Mathf.Max(1, gridSize) - 1);
            }
            Vector3 correct = new Vector3(cx * Tile.tileSize, cy * Tile.tileSize, 0f);
            // 已放置的块会被销毁 TileMovement，因此以此作为可靠标记；否则用位置判定兜底
            bool placed = tm == null || (go.transform.position - correct).sqrMagnitude < 1e-4f;
            if (placed) placedCount++;
            if (!placed) allPlaced = false;
            // 按格子索引映射到数组（row-major），保证数组长度为 gridSize*gridSize 时顺序稳定
            int slot = Mathf.Clamp(cy * Mathf.Max(1, gridSize) + cx, 0, stateData.pieces.Length - 1);
            stateData.pieces[slot] = new PuzzlePieceData
            {
                row = cy,
                col = cx,
                currentPosition = go.transform.position,
                correctPosition = correct,
                isPlaced = placed
            };
            filled++;
        }
        Debug.Log($"[PlayPrefs] SaveCurrentSceneState image={imageId} grid={gridSize} piecesFound={pieces.Count} expected={expected} filled={filled} placed={placedCount} allPlaced={allPlaced}");
        if (allPlaced)
        {
            ClearPuzzleStateForImage(imageId);
            RemoveFromSaveIndex(imageId);
            return;
        }
        string json = JsonUtility.ToJson(stateData);
        Debug.Log($"[PlayPrefs] JSON state for image={imageId}:\n{json}");
        // 写入文件
        string path = GetSaveFilePath(imageId);
        EnsureDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, json);
        Debug.Log($"[PlayPrefs] Saved to file: {path}, bytes={json.Length}");
        // 同时写入 PlayerPrefs（作为回退）
        string encryptedJson = EncryptData(json);
        PlayerPrefs.SetString(GetStateKey(imageId), encryptedJson);
        PlayerPrefs.Save();
        Debug.Log($"[PlayPrefs] Saved key={GetStateKey(imageId)} length={encryptedJson.Length}");
        AddToSaveIndex(imageId);
    }

    public PuzzleStateData LoadPuzzleStateForImage(string imageId)
    {
        if (string.IsNullOrEmpty(imageId)) return null;
        // 优先从文件读取
        string path = GetSaveFilePath(imageId);
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            Debug.Log($"[PlayPrefs] Load from file: {path}, jsonLen={json.Length}");
            return JsonUtility.FromJson<PuzzleStateData>(json);
        }
        // 回退到 PlayerPrefs
        string key = GetStateKey(imageId);
        if (!PlayerPrefs.HasKey(key)) return null;
        string encryptedJson = PlayerPrefs.GetString(key);
        string jsonPrefs = DecryptData(encryptedJson);
        Debug.Log($"[PlayPrefs] Load from PlayerPrefs image={imageId} key={key} jsonLen={jsonPrefs?.Length}");
        return JsonUtility.FromJson<PuzzleStateData>(jsonPrefs);
    }

    public bool HasUnfinishedStateForImage(string imageId)
    {
        var state = LoadPuzzleStateForImage(imageId);
        if (state == null || state.pieces == null || state.pieces.Length == 0) return false;
        for (int i = 0; i < state.pieces.Length; i++)
        {
            if (!state.pieces[i].isPlaced) return true;
        }
        return false;
    }

    public void ClearPuzzleStateForImage(string imageId)
    {
        if (string.IsNullOrEmpty(imageId)) return;
        string key = GetStateKey(imageId);
        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
        // 删除文件
        string path = GetSaveFilePath(imageId);
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"[PlayPrefs] Deleted save file: {path}");
        }
        RemoveFromSaveIndex(imageId);
    }

    #endregion

    #region Completed Puzzle Management

    public void AddCompletedPuzzle(string imageId, int difficulty, float completionTimeSeconds)
    {
        if (string.IsNullOrEmpty(imageId)) return;
        var list = LoadCompletedList();
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (list[i] != null && list[i].imageId == imageId) list.RemoveAt(i);
        }
        list.Insert(0, new CompletedPuzzleData
        {
            imageId = imageId,
            difficulty = difficulty,
            completionTimeSeconds = Mathf.Max(0f, completionTimeSeconds),
            completedAtTicksUtc = DateTime.UtcNow.Ticks
        });
        SaveCompletedList(list);
    }

    public List<CompletedPuzzleData> GetCompletedPuzzles()
    {
        return LoadCompletedList();
    }

    public void RemoveCompletedPuzzle(string imageId)
    {
        if (string.IsNullOrEmpty(imageId)) return;
        var list = LoadCompletedList();
        bool changed = false;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (list[i] != null && list[i].imageId == imageId)
            {
                list.RemoveAt(i);
                changed = true;
            }
        }
        if (changed) SaveCompletedList(list);

        try
        {
            string file = Path.Combine(Application.persistentDataPath, "CompletedPreviews", SanitizeFileName(imageId) + ".png");
            if (File.Exists(file)) File.Delete(file);
        }
        catch
        {
        }
    }

    public void SaveCompletedPreview(string imageId, Texture2D previewTex)
    {
        if (string.IsNullOrEmpty(imageId) || previewTex == null) return;
        string folder = Path.Combine(Application.persistentDataPath, "CompletedPreviews");
        EnsureDirectory(folder);
        string file = Path.Combine(folder, SanitizeFileName(imageId) + ".png");
        try
        {
            byte[] png = previewTex.EncodeToPNG();
            File.WriteAllBytes(file, png);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PlayPrefs] SaveCompletedPreview failed: {e.Message}");
        }
    }

    public Sprite LoadCompletedPreviewSprite(string imageId)
    {
        if (string.IsNullOrEmpty(imageId)) return null;
        string file = Path.Combine(Application.persistentDataPath, "CompletedPreviews", SanitizeFileName(imageId) + ".png");
        if (!File.Exists(file)) return null;
        try
        {
            byte[] bytes = File.ReadAllBytes(file);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(bytes)) return null;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.name = imageId + "_Preview";
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PlayPrefs] LoadCompletedPreviewSprite failed: {e.Message}");
            return null;
        }
    }

    private List<CompletedPuzzleData> LoadCompletedList()
    {
        string raw = PlayerPrefs.GetString(COMPLETED_INDEX_KEY, "");
        var list = new List<CompletedPuzzleData>();
        if (string.IsNullOrEmpty(raw)) return list;
        try
        {
            var wrapper = JsonUtility.FromJson<CompletedListWrapper>(raw);
            if (wrapper != null && wrapper.items != null) list.AddRange(wrapper.items);
        }
        catch
        {
        }
        list.RemoveAll(x => x == null || string.IsNullOrEmpty(x.imageId));
        list.Sort((a, b) => b.completedAtTicksUtc.CompareTo(a.completedAtTicksUtc));
        return list;
    }

    private void SaveCompletedList(List<CompletedPuzzleData> list)
    {
        var wrapper = new CompletedListWrapper { items = list != null ? list.ToArray() : Array.Empty<CompletedPuzzleData>() };
        string json = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString(COMPLETED_INDEX_KEY, json);
        PlayerPrefs.Save();
    }

    private static string SanitizeFileName(string s)
    {
        if (string.IsNullOrEmpty(s)) return "unknown";
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            s = s.Replace(c, '_');
        }
        return s;
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

    private string GetSaveFilePath(string imageId)
    {
        string dir = Application.isEditor ? Path.Combine(Application.dataPath, "Saves") : Path.Combine(Application.persistentDataPath, "Saves");
        string safeName = imageId.Replace('/', '_').Replace('\\', '_');
        return Path.Combine(dir, $"puzzle_{safeName}.json");
    }

    private void EnsureDirectory(string dir)
    {
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    public void ClearAllPuzzleSaves()
    {
        var ids = GetAllUnfinishedImageIds();
        for (int i = 0; i < ids.Count; i++)
        {
            ClearPuzzleStateForImage(ids[i]);
        }
        var list = new System.Collections.Generic.List<string>();
        SaveSaveIndex(list);
        string dirAssets = Path.Combine(Application.dataPath, "Saves");
        if (Directory.Exists(dirAssets))
        {
            var files = Directory.GetFiles(dirAssets, "*.json", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                try { File.Delete(files[i]); } catch {}
            }
        }
        string dirPersist = Path.Combine(Application.persistentDataPath, "Saves");
        if (Directory.Exists(dirPersist))
        {
            var files = Directory.GetFiles(dirPersist, "*.json", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                try { File.Delete(files[i]); } catch {}
            }
        }
        PlayerPrefs.DeleteKey(PUZZLE_STATE_KEY);
        PlayerPrefs.Save();
        Debug.Log("[PlayPrefs] Cleared all puzzle saves");
    }
}
