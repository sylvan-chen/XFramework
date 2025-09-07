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
        private readonly GameInput _gameInput;

        public PlayerInputAdapter()
        {
            _gameInput = new GameInput();
            _gameInput.Player.SetCallbacks(this);
        }

        ~PlayerInputAdapter()
        {
            _gameInput.Dispose();
        }

        public void Enable() => _gameInput.Player.Enable();
        public void Disable() => _gameInput.Player.Disable();

        #region 回调型Action

        public event System.Action OnJumpEvent = delegate { };

        #endregion

        #region 轮询型Action

        public Vector2 MoveInput => _gameInput.Player.Move.ReadValue<Vector2>();

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
