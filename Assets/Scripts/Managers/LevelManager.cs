using UnityEngine;

public class LevelManager : BaseSingleton<LevelManager>
{
    [SerializeField] private LevelBoardData[] _levelDatas;
    private int currentLevel;

    protected override void Awake()
    {
        base.Awake();

        if (_levelDatas == null || _levelDatas.Length == 0)
        {
            Debug.LogWarning("LevelDatas array is empty. Attempting to load from Resources.");

            _levelDatas = Resources.LoadAll<LevelBoardData>("Level Datas"); // Load all LevelData assets in the Resources folder

            if (_levelDatas.Length == 0)
            {
                Debug.LogWarning("No LevelData assets found in Resources. Creating a default one.");
                LevelBoardData defaultLevelData = ScriptableObject.CreateInstance<LevelBoardData>();
                defaultLevelData.InitializeDefaults();
                _levelDatas = new[] { defaultLevelData };
            }
        }
    }

    public LevelBoardData GetLevelData()
    {
        return _levelDatas[currentLevel];
    }

    public void LoadNextLevel()
    {
        currentLevel++;
    }

    private void OnValidate()
    {
        if (_levelDatas == null)
        {
            Debug.LogWarning("LevelDatas are not assigned in LevelManager.");
        }
    }
}
