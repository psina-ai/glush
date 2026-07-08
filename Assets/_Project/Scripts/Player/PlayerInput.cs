using UnityEngine;
using System;

namespace Glush.Player
{
    public class PlayerInput : MonoBehaviour
    {
        [SerializeField] private bool _logInputs = false;
        
        private PlayerInputActions _inputActions;
        private PlayerInputActions.PlayerActions _playerActions;
        
        private Vector2 _moveInput;
        private Vector2 _lookInput;
        private bool _sprintHeld;
        
        public Vector2 MoveInput => _moveInput;
        public Vector2 LookInput => _lookInput;
        public bool SprintHeld => _sprintHeld;
        
        public event Action OnJumpPressed;
        public event Action OnInteractPressed;
        
        private void Awake()
        {
            _inputActions = new PlayerInputActions();
            _playerActions = _inputActions.Player;
        }
        
        private void OnEnable()
        {
            _playerActions.Enable();
            
            _playerActions.Jump.performed += ctx => 
            {
                OnJumpPressed?.Invoke();
                if (_logInputs) Debug.Log("[PlayerInput] Jump pressed");
            };
            
            _playerActions.Interact.performed += ctx => 
            {
                OnInteractPressed?.Invoke();
                if (_logInputs) Debug.Log("[PlayerInput] Interact pressed");
            };
        }
        
        private void OnDisable()
        {
            _playerActions.Jump.performed -= ctx => OnJumpPressed?.Invoke();
            _playerActions.Interact.performed -= ctx => OnInteractPressed?.Invoke();
            
            _playerActions.Disable();
        }
        
        private void Update()
        {
            _moveInput = _playerActions.Move.ReadValue<Vector2>();
            _lookInput = _playerActions.Look.ReadValue<Vector2>();
            _sprintHeld = _playerActions.Sprint.ReadValue<float>() > 0.5f;
            
            if (_logInputs)
            {
                Debug.Log($"[PlayerInput] Move: ({_moveInput.x:F2}, {_moveInput.y:F2}), Sprint: {_sprintHeld}");
            }
        }
    }
}