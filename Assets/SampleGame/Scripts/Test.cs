using UnityEngine;
using UnityEngine.UI;
using XFramework;
using XFramework.Utils;

public class Test : MonoBehaviour
{
    [SerializeField]
    private Button _eventBtn;
    [SerializeField]
    private Button _quitBtn;

    void Start()
    {
        Global.EventManager.Subscribe(TestEvent.ID, OnClick);
        _eventBtn.onClick.AddListener(() =>
        {
            Global.EventManager.PublishLater(TestEvent.ID, TestEvent.Create("Hello, world!"), 60);
        });
        _quitBtn.onClick.AddListener(() =>
        {
            Global.Shutdown();
        });
    }

    void OnDestroy()
    {
        Global.EventManager.Unsubscribe(TestEvent.ID, OnClick);
    }

    void OnClick(IEvent args)
    {
        var testArgs = args as TestEvent;
        Log.Info(testArgs.Message);
    }
}
