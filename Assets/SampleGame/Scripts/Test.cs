using System.Collections.Generic;
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

    private Stack<Image> _imgStack = new();

    private void Awake()
    {
        _eventBtn = transform.Find("EventBtn").GetComponent<Button>();
        _spawnBtn = transform.Find("SpawnBtn").GetComponent<Button>();
        _unspawnBtn = transform.Find("UnspawnBtn").GetComponent<Button>();
        _quitBtn = transform.Find("QuitBtn").GetComponent<Button>();
    }

    private void Start()
    {
        Global.EventManager.Subscribe(TestEvent.Id, OnEventBtnClick);

        Pool<Image> imagePool = Global.PoolManager.CreatePool<Image>(1, 10f, 15f);

        _eventBtn.onClick.AddListener(() =>
        {
            Global.EventManager.PublishLater(TestEvent.Id, TestEvent.Create("Hello, world!"), 60);
        });
        int index = 0;
        _spawnBtn.onClick.AddListener(() =>
        {
            Image lastImg = imagePool.Spawn();
            if (lastImg == null)
            {
                lastImg = Instantiate(_targetImg);
                lastImg.gameObject.SetActive(true);
                lastImg.transform.SetParent(_targetImgParent);
                lastImg.name = $"Image_{index++}";
                _imgStack.Push(lastImg);
                Log.Debug($"Create: {lastImg.name}");
                imagePool.Register
                (
                    lastImg,
                    (target) =>
                    {
                        target.gameObject.SetActive(true);
                        _imgStack.Push(target);
                    },
                    (target) =>
                    {
                        target.gameObject.SetActive(false);
                    },
                    (target) =>
                    {
                        if (target != null)
                        {
                            Destroy(target.gameObject);
                        }
                    }
                );
            }
            else
            {
                Log.Debug($"Spawn: {lastImg.name}");
            }
        });
        _unspawnBtn.onClick.AddListener(() =>
        {
            Image lastImg = _imgStack.Pop();
            if (lastImg != null)
            {
                Log.Debug($"Unspawn: {lastImg.name}");
                imagePool.Unspawn(lastImg);
                lastImg = null;
            }
        });
        _quitBtn.onClick.AddListener(() =>
        {
            Global.Shutdown();
        });
    }

    private void OnDestroy()
    {
        if (Global.EventManager != null)
        {
            Global.EventManager.Unsubscribe(TestEvent.Id, OnEventBtnClick);
        }
    }

    private void OnEventBtnClick(IEvent args)
    {
        var testArgs = args as TestEvent;
        Log.Info(testArgs.Message);
    }
}
