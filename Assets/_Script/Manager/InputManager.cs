using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class InputManager : MonoBehaviour
{
    private PlayerInput playerInput;

    // 현재 설정된 키들 (기본값)
    public Key keyUp = Key.UpArrow;
    public Key keyDown = Key.DownArrow;
    public Key keyLeft = Key.LeftArrow;
    public Key keyRight = Key.RightArrow;
    public Key keySpike = Key.Enter;

    // 연속 입력되는 액션 - 프로퍼티로 노출
    public static float MoveInput {  get; private set; }

    // 1회성 액션 - 이벤트로 노출
    public static event Action OnJump;
    public static event Action OnSpike;

    // Input System의 액션들
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction spikeAction;

    // 콜백 함수들을 저장할 변수들
    private Action<InputAction.CallbackContext> onMovePerformed;
    private Action<InputAction.CallbackContext> onMoveCanceled;
    private Action<InputAction.CallbackContext> onJumpPerformed;
    private Action<InputAction.CallbackContext> onSpikePerformed;

    private void Awake()
    {
      
    }

    private void OnEnable()
    {
        // PlayerInput 컴포넌트 초기화
        playerInput = GetComponent<PlayerInput>();
        playerInput.defaultActionMap = "Player";
        playerInput.defaultControlScheme = "Keyboard&Mouse";
        playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;

        // 각각 액션 찾기
        moveAction = playerInput.actions["Move"];
        jumpAction = playerInput.actions["Jump"];
        spikeAction = playerInput.actions["Spike"];

        // Move Action 콜백 등록
        if (moveAction != null)
        {
            onMovePerformed = ctx => MoveInput = ctx.ReadValue<float>();
            onMoveCanceled = ctx => MoveInput = 0f;
            moveAction.performed += onMovePerformed;
            moveAction.canceled += onMoveCanceled;
        }

        // Jump Action 콜백 등록
        if (jumpAction != null)
        {
            onJumpPerformed = ctx => OnJump?.Invoke();
            jumpAction.performed += onJumpPerformed;
        }

        // Spike Action 콜백 등록
        if (spikeAction != null)
        {
            onSpikePerformed = ctx => OnSpike?.Invoke();
            spikeAction.performed += onSpikePerformed;
        }
    }

    private void OnDisable()
    {
        // Move Action 콜백 해제
        if (moveAction != null)
        {
            if (onMovePerformed != null)
            {
                moveAction.performed -= onMovePerformed;
                onMovePerformed = null;
            }

            if (onMoveCanceled != null)
            {
                moveAction.canceled -= onMoveCanceled;
                onMoveCanceled = null;
            }
        }

        // Jump Action 콜백해제
        if (jumpAction != null && onJumpPerformed != null)
        {
            jumpAction.performed -= onJumpPerformed;
            onJumpPerformed = null;
        }

        // Spike Action 콜백해제
        if ( spikeAction != null && onSpikePerformed != null)
        {
            spikeAction.performed -= onSpikePerformed;
            onSpikePerformed = null;
        }
    }
}
