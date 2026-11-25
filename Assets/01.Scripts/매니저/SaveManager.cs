using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

// 기존 매니저들의 Start 초기화가 끝난 후 진행 데이터를 복원한다.
[DefaultExecutionOrder(100)]
public class SaveManager : MonoBehaviour
{
    private const int SaveVersion = 1;

    [Header("시스템 참조")]
    [SerializeField] private DialoguePlayer dialoguePlayer;

    private DayManager dayManager;
    private EconomyManager economyManager;
    private UpgradeManager upgradeManager;

    private string progressPath;
    private string memoirPath;

    private bool progressWritable;
    private bool memoirWritable;

    // 백업에서 복구했다면 다음 저장 때 정상 백업을 보존한다.
    private bool preserveProgressBackup;
    private bool preserveMemoirBackup;

    private bool memoirDirty;

    // 같은 대화를 중복해서 등록하지 않는다.
    private readonly HashSet<string> unlockedDialogues =
        new HashSet<string>(StringComparer.Ordinal);

    private void Awake()
    {
        string directory = Path.Combine(
            Application.persistentDataPath,
            "SaveData");

        progressPath = Path.Combine(directory, "Progress.json");
        memoirPath = Path.Combine(directory, "Memoir.json");

        Debug.Log($"저장 폴더: {directory}", this);

        // 회고록은 다른 게임 매니저의 초기화와 관계없이 읽을 수 있다.
        memoirWritable = LoadOrNew<MemoirSaveData>(
            memoirPath,
            RestoreMemoir,
            out preserveMemoirBackup);
    }

    private void Start()
    {
        dayManager = DayManager.Instance;
        economyManager = EconomyManager.Instance;
        upgradeManager = UpgradeManager.Instance;

        if (dialoguePlayer == null)
        {
            Debug.LogError("SaveManager의 DialoguePlayer를 연결해주세요.", this);
        }

        if (dayManager == null || !dayManager.isActiveAndEnabled ||
            economyManager == null || !economyManager.isActiveAndEnabled ||
            upgradeManager == null || !upgradeManager.isActiveAndEnabled ||
            !upgradeManager.IsInitialized)
        {
            Debug.LogError("진행 저장에 필요한 매니저가 준비되지 않았습니다.", this);
            return;
        }

        progressWritable = LoadOrNew<GameSaveData>(
            progressPath,
            RestoreProgress,
            out preserveProgressBackup);

        // 최초 로드가 끝난 후 저장 이벤트를 구독한다.
        if (progressWritable && isActiveAndEnabled)
            dayManager.StateChanged += HandleDayStateChanged;
    }

    private void OnEnable()
    {
        if (dialoguePlayer != null)
            dialoguePlayer.DialogueStarted += RecordDialogue;

        // Start 이후 다시 활성화되는 경우의 재구독.
        if (progressWritable && dayManager != null)
            dayManager.StateChanged += HandleDayStateChanged;
    }

    private void OnDisable()
    {
        if (dialoguePlayer != null)
            dialoguePlayer.DialogueStarted -= RecordDialogue;

        if (dayManager != null)
            dayManager.StateChanged -= HandleDayStateChanged;
    }

    // ────────────────────
    // 게임 진행 저장
    // ────────────────────

    private void HandleDayStateChanged(DayState state)
    {
        // 낮 종료 후 판매금 지급까지 완료된 시점에만 저장한다.
        if (!progressWritable || state != DayState.Result)
            return;

        var data = new GameSaveData
        {
            saveVersion = SaveVersion,
            day = dayManager.DayNumber,
            gold = economyManager.Gold,
            upgrades = upgradeManager.CaptureLevels()
        };

        if (WriteJson(progressPath, data, ref preserveProgressBackup))
        {
            Debug.Log($"DAY {data.day} 진행 저장 완료", this);
        }
    }

    private bool RestoreProgress(GameSaveData data)
    {
        if (data.saveVersion != SaveVersion ||
            data.day < 1 ||
            data.gold < 0 ||
            data.upgrades == null ||
            dayManager.CurrentState != DayState.Ready)
        {
            return false;
        }

        // 구매 레벨 복원과 강화 효과 적용.
        // 잘못된 노드 데이터라면 골드와 날짜를 변경하지 않는다.
        if (!upgradeManager.TryRestoreLevels(data.upgrades))
            return false;

        economyManager.RestoreGoldWithoutNotification(data.gold);

        // 저장된 날짜의 밤으로 복귀한다.
        dayManager.RestoreCompletedDay(data.day);

        // 전체 상태가 복원된 뒤 관련 UI에 알린다.
        economyManager.NotifyGoldChanged();
        upgradeManager.NotifyUpgradesChanged();

        Debug.Log($"DAY {data.day} 진행 불러오기 완료", this);

        return true;
    }

    // ────────────────────
    // 회고록 저장
    // ────────────────────

    private void RecordDialogue(DialogueData dialogue)
    {
        if (dialogue == null ||
            string.IsNullOrWhiteSpace(dialogue.DialogueId))
        {
            Debug.LogWarning("회고록에 등록할 대화 ID가 없습니다.", this);
            return;
        }

        if (!memoirWritable)
            return;

        if (unlockedDialogues.Add(dialogue.DialogueId))
            memoirDirty = true;

        // 새로운 발견이 없으면 파일을 다시 쓰지 않는다.
        // 이전 저장에 실패했다면 다음 대화 때 다시 시도한다.
        if (!memoirDirty)
            return;

        var data = new MemoirSaveData
        {
            saveVersion = SaveVersion,
            dialogueIds = new List<string>(unlockedDialogues)
        };

        data.dialogueIds.Sort(StringComparer.Ordinal);

        if (WriteJson(memoirPath, data, ref preserveMemoirBackup))
        {
            memoirDirty = false;

            Debug.Log(
                $"회고록 저장 완료: {dialogue.MemoirTitle}",
                this);
        }
    }

    private bool RestoreMemoir(MemoirSaveData data)
    {
        if (data.saveVersion != SaveVersion ||
            data.dialogueIds == null)
        {
            return false;
        }

        foreach (string id in data.dialogueIds)
        {
            if (string.IsNullOrWhiteSpace(id))
                return false;
        }

        unlockedDialogues.Clear();

        foreach (string id in data.dialogueIds)
            unlockedDialogues.Add(id);

        return true;
    }

    // 이후 회고록 UI에서 대화의 발견 여부를 확인할 때 사용한다.
    public bool IsDialogueUnlocked(string dialogueId)
    {
        return unlockedDialogues.Contains(dialogueId);
    }

    // ────────────────────
    // 공통 파일 처리
    // ────────────────────

    private bool LoadOrNew<T>(
        string path,
        Func<T, bool> restore,
        out bool preserveBackup) where T : class
    {
        preserveBackup = false;
        string backupPath = path + ".bak";

        // 저장 파일이 없는 최초 실행은 정상적인 새 게임이다.
        if (!File.Exists(path) && !File.Exists(backupPath))
            return true;

        if (TryReadJson(path, out T data) && restore(data))
            return true;

        // 원본 복원에 실패하면 이전 백업으로 복구를 시도한다.
        if (TryReadJson(backupPath, out data) && restore(data))
        {
            preserveBackup = true;

            Debug.LogWarning(
                $"백업에서 복원했습니다: {backupPath}",
                this);

            return true;
        }

        // 복원 실패 상태에서 새로운 데이터로 덮어쓰지 않는다.
        Debug.LogError(
            $"저장 파일을 복원할 수 없습니다. " +
            $"이번 실행에서는 해당 파일을 덮어쓰지 않습니다: {path}",
            this);

        return false;
    }

    private bool TryReadJson<T>(
        string path,
        out T data) where T : class
    {
        data = null;

        if (!File.Exists(path))
            return false;

        try
        {
            string json = File.ReadAllText(path);
            data = JsonUtility.FromJson<T>(json);

            return data != null;
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"파일 읽기 실패: {path}\n{exception.Message}",
                this);

            return false;
        }
    }

    private bool WriteJson<T>(
        string path,
        T data,
        ref bool preserveBackup)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            string tempPath = path + ".tmp";
            string backupPath = path + ".bak";

            // 기존 파일에 직접 덮어쓰지 않고 임시 파일에 먼저 기록한다.
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(tempPath, json);

            if (File.Exists(path))
            {
                File.Replace(
                    tempPath,
                    path,
                    preserveBackup ? null : backupPath);
            }
            else
            {
                File.Move(tempPath, path);
            }

            preserveBackup = false;
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"파일 저장 실패: {path}\n{exception.Message}",
                this);

            return false;
        }
    }
}