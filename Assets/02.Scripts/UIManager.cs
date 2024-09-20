using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    private static UIManager _instance;
    public static UIManager Instance
    {
        get
        {
            // 싱글톤 구현
            if (!_instance)
            {
                _instance = FindObjectOfType(typeof(UIManager)) as UIManager;

                if (_instance == null)
                    Debug.Log("인스턴스를 생성합니다");
            }
            return _instance;
        }
    }
    Player player;

    [Header("Level & EXP")]
    [SerializeField] private Slider _expBar;
    [SerializeField] private Text _levelText;
    [SerializeField] private Text _lvPoint;
    [Space(10f)]
    [Header("Gold")]
    [SerializeField] private Text _goldText;

    [SerializeField] private Transform _RespawnUI;

    public Button[] _buttons;
    public GameObject[] _locked;

    private void Awake()
    {
        // 인스턴스가 없을 때 해당 오브젝트로 설정
        if (_instance == null)
            _instance = this;

        // 인스턴스가 존재한다면 현재 오브젝트 파괴
        else if (_instance != null)
            Destroy(gameObject);

        // 씬 로드시에도 파괴되지않음 
        DontDestroyOnLoad(gameObject);
        player = GameManager.Instance.player;
    }
    

    // EXP 값이 변경될 때 UI 변경
    public void ExpChanged()
    {
        if(player != null)
            _expBar.value = player.Exp / player.NextExp;
    }

    // Level 값이 변경될 때 UI 변경
    public void LevelChanged()
    {
        if (player != null)
            _levelText.text = player.Level.ToString();
    }

    // Gold 골드의 값이 변경될 때 UI 값 변경
    public void GoldChanged()
    {
        if (player != null)
            _goldText.text = player.Gold.ToString();
    }

    // Die의 값이 true가 되면 Respawn UI 실행
    public void OnRespawn()
    {
        if (player != null)
            _RespawnUI.gameObject.SetActive(true);
    }

    // 레벨 포인트 값이 변경되면 UI 값 변경
    public void LvPointChange() 
    {
        if(player != null)
            _lvPoint.text = player.LvPoint.ToString();
    }
}
