using UnityEngine.InputSystem;

namespace XGame.InputSystem.Internal
{
    /// <summary>
    /// UI输入适配器
    /// </summary>
    public class UIInputAdapter : GameInput.IUIActions
    {
        private readonly GameInput _gameInput;

        public UIInputAdapter()
        {
            _gameInput = new GameInput();
            _gameInput.UI.SetCallbacks(this);
        }

        ~UIInputAdapter()
        {
            _gameInput.Dispose();
        }

        #region 接口实现

        public void OnCancel(InputAction.CallbackContext context)
        {
            throw new System.NotImplementedException();
        }

        public void OnClick(InputAction.CallbackContext context)
        {
            throw new System.NotImplementedException();
        }

        public void OnMiddleClick(InputAction.CallbackContext context)
        {
            throw new System.NotImplementedException();
        }

        public void OnNavigate(InputAction.CallbackContext context)
        {
            throw new System.NotImplementedException();
        }

        public void OnPoint(InputAction.CallbackContext context)
        {
            throw new System.NotImplementedException();
        }

        public void OnRightClick(InputAction.CallbackContext context)
        {
            throw new System.NotImplementedException();
        }

        public void OnScrollWheel(InputAction.CallbackContext context)
        {
            throw new System.NotImplementedException();
        }

        public void OnSubmit(InputAction.CallbackContext context)
        {
            throw new System.NotImplementedException();
        }

        public void OnTrackedDeviceOrientation(InputAction.CallbackContext context)
        {
            throw new System.NotImplementedException();
        }

        public void OnTrackedDevicePosition(InputAction.CallbackContext context)
        {
            throw new System.NotImplementedException();
        }

        #endregion
    }
}