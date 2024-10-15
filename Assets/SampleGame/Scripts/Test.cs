using UnityEngine;
using UnityEngine.UI;
using XFramework;
using XFramework.Utils;

public class Test : MonoBehaviour
{

    [SerializeField]
    private Image _targetImg;

    [SerializeField]
    private Transform _targetImgParent;

    private Button _eventBtn;

    private Button _spawnBtn;

    private Button _unspawnBtn;

    private Button _quitBtn;

    private Image _lastImg;

    private void Awake()
    {
        _eventBtn = transform.Find("EventBtn").GetComponent<Button>();
        _spawnBtn = transform.Find("SpawnBtn").GetComponent<Button>();
        _unspawnBtn = transform.Find("UnspawnBtn").GetComponent<Button>();
        _quitBtn = transform.Find("QuitBtn").GetComponent<Button>();
    }

    private void Start()
    {
        Global.EventManager.Subscribe(TestEvent.ID, OnEventBtnClick);

        Pool<Image> imagePool = Global.PoolManager.CreatePool<Image>();

        _eventBtn.onClick.AddListener(() =>
        {
            Global.EventManager.PublishLater(TestEvent.ID, TestEvent.Create("Hello, world!"), 60);
        });
        int index = 0;
        _spawnBtn.onClick.AddListener(() =>
        {
            Log.Debug($"imagePool.Count: {imagePool.Count}");
            _lastImg = imagePool.Spawn();
            if (_lastImg == null)
            {
                _lastImg = Instantiate(_targetImg);
                _lastImg.gameObject.SetActive(true);
                _lastImg.transform.SetParent(_targetImgParent);
                _lastImg.name = $"Image_{index++}";
                Log.Debug($"Create: {_lastImg.name} from pool {imagePool.Capacity}");
                imagePool.Register(_lastImg, (target) => { target.gameObject.SetActive(true); }, (target) => { target.gameObject.SetActive(false); });
            }
            else
            {
                Log.Debug($"Spawn: {_lastImg.name}");
            }
        });
        _unspawnBtn.onClick.AddListener(() =>
        {
            if (_lastImg != null)
            {
                Log.Debug($"Unspawn: {_lastImg.name}");
                imagePool.Unspawn(_lastImg);
                _lastImg = null;
            }
        });
        _quitBtn.onClick.AddListener(() =>
        {
            Global.Shutdown();
        });
    }

    private void OnDestroy()
    {
        Global.EventManager.Unsubscribe(TestEvent.ID, OnEventBtnClick);
    }

    private void OnEventBtnClick(IEvent args)
    {
        var testArgs = args as TestEvent;
        Log.Info(testArgs.Message);
    }
}
