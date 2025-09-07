using UnityEngine;
using UnityEngine.InputSystem;
using XGame.InputSystem.Internal;

namespace XGame.InputSystem
{
    /// <summary>
    /// 玩家输入适配器
    /// </summary>
    public class PlayerInputAdapter : GameInput.IPlayerActions
    {
        public PlayerInputAdapter()
        {
            GameInputWrapper.Instance.Player.SetCallbacks(this);
        }

        public void Enable() => GameInputWrapper.Instance.Player.Enable();
        public void Disable() => GameInputWrapper.Instance.Player.Disable();
        public void Dispose() => GameInputWrapper.Instance.Dispose();

        #region 回调型Action

        public event System.Action OnJumpEvent = delegate { };

        #endregion

        #region 轮询型Action

        public Vector2 MoveInput => GameInputWrapper.Instance.Player.Move.ReadValue<Vector2>();

        #endregion

        #region 接口实现

        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                OnJumpEvent.Invoke();
            }
        }

        public void OnMove(InputAction.CallbackContext context)
        {
        }

        #endregion
    }
}
