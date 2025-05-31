using UnityEngine;

public abstract class UIPanelBase : MonoBehaviour
{
    private UIPanelInfo _info;

    public void Init(UIPanelInfo info)
    {
        _info = info;
        OnInit();
    }

    protected virtual void OnInit()
    {
    }

    protected virtual void OnOpen()
    {
    }

    protected virtual void OnClose()
    {
    }

    protected virtual void OnShow()
    {
    }

    protected virtual void OnHide()
    {
    }
}
