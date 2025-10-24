using UnityEngine;
using UnityEngine.UI;

public class CookieClicker : MonoBehaviour
{
    [Header("UI 参照")]
    [SerializeField] private Text cookieCountText;
    [SerializeField] private Text upgradeButtonText;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Text cpsText;
    [SerializeField] private Text cookiePerSecondText;
    [SerializeField] private Button autoAddButton;
    [SerializeField] private Text autoAddButtonText;

    [Header("設定パラメータ")]
    [SerializeField, Tooltip("最初のアップグレードに必要なクッキー枚数")]
    private int initialUpgradeCost = 100;

    [SerializeField, Tooltip("アップグレードごとのコスト倍率")]
    private int costMultiplier = 2;

    [SerializeField, Tooltip("クリック力アップ倍率")]
    private int powerMultiplier = 2;

    [SerializeField, Tooltip("CPS計測の更新間隔（秒）")]
    private float cpsUpdateInterval = 1f;

    [Header("自動生産設定")]
    [SerializeField, Tooltip("自動生産の実行間隔（秒）")]
    private float autoAddInterval = 1f;


    // 定数（マジックナンバー排除）

    private const int INITIAL_AUTO_ADD_LEVEL = -1;
    private const string MAX_LEVEL_TEXT = "最大レベル！";
    private const string INSUFFICIENT_COOKIE_LOG = "クッキーが足りません！";

    //自動生産の段階設定
    [System.Serializable]
    public class AutoAddLevel
    {
        [Tooltip("このレベルを解除するためのコスト")]
        public int cost;

        [Tooltip("このレベルでの1秒あたり追加クッキー枚数")]
        public int addAmount;
    }

    [SerializeField, Tooltip("自動生産レベルリスト（コストと生産量）")]
    private AutoAddLevel[] autoAddLevels = new AutoAddLevel[]
    {
        new AutoAddLevel { cost = 100, addAmount = 1 },
        new AutoAddLevel { cost = 500, addAmount = 2 },
        new AutoAddLevel { cost = 1000, addAmount = 5 },
        new AutoAddLevel { cost = 5000, addAmount = 10 },
        new AutoAddLevel { cost = 20000, addAmount = 20 }
    };


    // 内部変数

    private int cookieCount = 0;
    private int clickPower = 1;
    private int currentUpgradeCost;
    private float currentCPS = 0f;

    private int clicksInCurrentInterval = 0;
    private float cpsTimer = 0f;

    private bool autoAddUnlocked = false;
    private float autoAddTimer = 0f;
    private int currentAutoAddLevel = INITIAL_AUTO_ADD_LEVEL;
    private int autoAddAmount = 0;



    // 初期化

    private void Start()
    {
        currentUpgradeCost = initialUpgradeCost;

        upgradeButton.onClick.AddListener(OnUpgradeButton);
        autoAddButton.onClick.AddListener(OnAutoAddButton);

        UpdateUI();
    }


    // 毎フレーム更新

    private void Update()
    {
        UpdateCPS();
        UpdateAutoProduction();
    }

    //CPS（クリック毎秒）計測
    private void UpdateCPS()
    {
        cpsTimer += Time.deltaTime;
        if (cpsTimer >= cpsUpdateInterval)
        {
            currentCPS = clicksInCurrentInterval / cpsTimer;
            clicksInCurrentInterval = 0;
            cpsTimer = 0f;
            UpdateCpsText();
        }
    }

    //自動クッキー生産
    private void UpdateAutoProduction()
    {
        if (!autoAddUnlocked) return;

        autoAddTimer += Time.deltaTime;
        if (autoAddTimer >= autoAddInterval)
        {
            autoAddTimer = 0f;
            cookieCount += autoAddAmount;
            UpdateUI();
        }
    }

    //クッキークリック時
    public void OnClickCookie()
    {
        cookieCount += clickPower;
        clicksInCurrentInterval++;
        UpdateUI();
    }

    //アップグレードボタン
    private void OnUpgradeButton()
    {
        if (cookieCount < currentUpgradeCost) return;

        cookieCount -= currentUpgradeCost;
        clickPower *= powerMultiplier;
        currentUpgradeCost *= costMultiplier;
        UpdateUI();
    }

    //自動生産解除 or 強化ボタン
    private void OnAutoAddButton()
    {
        int nextLevel = currentAutoAddLevel + 1;

        if (nextLevel >= autoAddLevels.Length)
        {
            SetAutoAddButtonToMaxLevel();
            return;
        }

        var levelData = autoAddLevels[nextLevel];

        if (cookieCount < levelData.cost)
        {
            Debug.Log(INSUFFICIENT_COOKIE_LOG);
            return;
        }

        cookieCount -= levelData.cost;
        currentAutoAddLevel = nextLevel;
        autoAddUnlocked = true;
        autoAddAmount = levelData.addAmount;
        autoAddTimer = 0f;

        if (nextLevel + 1 >= autoAddLevels.Length)
        {
            SetAutoAddButtonToMaxLevel();
        }
        else
        {
            var next = autoAddLevels[nextLevel + 1];
            autoAddButtonText.text = $"次の自動生産Lv{nextLevel + 2} ({next.cost}枚で+{next.addAmount}/秒)";
        }

        UpdateUI();
    }

    //最大レベルボタン状態設定
    private void SetAutoAddButtonToMaxLevel()
    {
        autoAddButtonText.text = MAX_LEVEL_TEXT;
        autoAddButton.interactable = false;
    }

    //UI更新
    private void UpdateUI()
    {
        cookieCountText.text = $"クッキー: {cookieCount}枚";

        upgradeButtonText.text =
            $"次のアップグレード：{currentUpgradeCost}枚で\nクリック力 ×{powerMultiplier}！";
        upgradeButton.interactable = cookieCount >= currentUpgradeCost;

        UpdateAutoAddButtonUI();
        UpdateCpsText();
        UpdateCookiePerSecondText();
    }

    // 自動生産ボタンUI更新
    private void UpdateAutoAddButtonUI()
    {
        if (currentAutoAddLevel == INITIAL_AUTO_ADD_LEVEL)
        {
            var next = autoAddLevels[0];
            autoAddButtonText.text = $"自動生産を解除 ({next.cost}枚で+{next.addAmount}/秒)";
            autoAddButton.interactable = cookieCount >= next.cost;
        }
        else if (currentAutoAddLevel < autoAddLevels.Length - 1)
        {
            var next = autoAddLevels[currentAutoAddLevel + 1];
            autoAddButtonText.text = $"自動生産Lv{currentAutoAddLevel + 1}中\n次Lv({next.cost}枚で+{next.addAmount}/秒)";
            autoAddButton.interactable = cookieCount >= next.cost;
        }
        else
        {
            SetAutoAddButtonToMaxLevel();
        }
    }

    private void UpdateCpsText()
    {
        cpsText.text = $"1秒あたり: {currentCPS:F1}クリック";
    }

    private void UpdateCookiePerSecondText()
    {
        float cookiesPerSecond =
            (currentCPS * clickPower) +
            (autoAddUnlocked ? (autoAddAmount / autoAddInterval) : 0f);
        cookiePerSecondText.text = $"1秒あたりのクッキー増加: {cookiesPerSecond:F1}枚";
    }
}
