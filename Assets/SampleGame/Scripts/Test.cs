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
        Global.EventManager.Subscribe(TestEventArgs.Id, OnClick);
        _eventBtn.onClick.AddListener(() =>
        {
            Global.EventManager.Publish(TestEventArgs.Id, new TestEventArgs("Hello, world!"));
        });
        _quitBtn.onClick.AddListener(() =>
        {
            Global.Shutdown();
        });
    }

    void OnDestroy()
    {
        Global.EventManager.Unsubscribe(TestEventArgs.Id, OnClick);
    }

    void OnClick(IEventArgs args)
    {
        var testArgs = args as TestEventArgs;
        Log.Info(testArgs.Message);
    }
}
