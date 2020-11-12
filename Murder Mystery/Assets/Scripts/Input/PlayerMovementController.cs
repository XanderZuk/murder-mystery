using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;
using System;

namespace Scripts.Input
{
    public class PlayerMovementController : NetworkBehaviour
    {
        [SerializeField] public float movementSpeed = 10f;
        [SerializeField] public float gravity = -9.81f;
        [SerializeField] private CharacterController controller = null;

        public Transform groundCheck;
        public LayerMask groundMask;
        public float groundDistance = 0.4f;
        public float jumpHeight = 3f;
        private Vector2 previousInput;
        private Controls controls;
        private bool isGrounded;

        Vector3 velocity;

        private Controls Controls
        {
            get
            {
                if (controls != null)
                {
                    return controls;
                }
                return controls = new Controls();
            }
        }

        public override void OnStartAuthority()
        {
            enabled = true;
            Controls.Player.Move.performed += ctx => SetMovement(ctx.ReadValue<Vector2>());
            Controls.Player.Move.canceled += ctx => ResetMovement();
            Controls.Player.Jump.performed += ctx => Jump();
        }

        [ClientCallback]
        private void OnEnable() => Controls.Enable();

        [ClientCallback]
        private void OnDisable() => Controls.Disable();

        [ClientCallback]
        private void Update() => Move();

        [Client]
        private void Move()
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }

            Vector3 right = controller.transform.right;
            Vector3 forward = controller.transform.forward;
            right.y = 0f;
            forward.y = 0f;

            if (isGrounded)
            {
                Vector3 movement = right.normalized * previousInput.x + forward.normalized * previousInput.y;
                controller.Move(movement * movementSpeed * Time.deltaTime);
            }
         
            
            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
        }

        [Client]
        private void Jump()
        {
            if (isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                controller.Move(velocity * Time.deltaTime);
            }
        }

        [Client]
        private void ResetMovement()
        {
            previousInput = Vector2.zero;
        }

        [Client]
        private void SetMovement(Vector2 movement)
        {
            previousInput = movement;  
        }
    }
}

