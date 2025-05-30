using UnityEngine;

/// <summary>
/// UI 组件
/// </summary>
public abstract class UIWidget : MonoBehaviour
{
    private int _widgetID;
    private string _uiAssetName;
    private UIPanel _uiPanel;
    private int _depth;
    private bool _isPauseOnCovered;

    private bool _isAvailable = false;
    private bool _isVisiable = false;
    private Transform _cachedTransform = null;
    private int _originalLayer = 0;

    /// <summary>
    /// UI 组件的 ID
    /// </summary>
    public int WidgetID => _widgetID;

    /// <summary>
    /// UI组件的资源名称
    /// </summary>
    public string UIAssetName => _uiAssetName;

    /// <summary>
    /// UI 组件所属的 UI 面板
    /// </summary>
    public UIPanel UIPanel => _uiPanel;

    public void Init(int widgetID, string uiAssetName, UIPanel uiPanel, bool isPauseOnCovered, object userData)
    {

    }

    protected virtual void OnInit(object userData)
    {


        if (_cachedTransform == null)
        {
            _cachedTransform = transform;
        }
        _originalLayer = gameObject.layer;
    }
}