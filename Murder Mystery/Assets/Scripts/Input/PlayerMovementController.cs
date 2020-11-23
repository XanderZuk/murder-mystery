using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;
using System;
using System.IO.MemoryMappedFiles;

namespace Scripts.Input
{
    public class PlayerMovementController : NetworkBehaviour
    {
        [SerializeField] public float movementSpeed = 10f;
        [SerializeField] public float gravity = -16.81f;
        [SerializeField] public float crouchMultiplier = 1f;
        [SerializeField] public float sprintMultiplier = 1f;
        [SerializeField] private CharacterController controller = null;

        public Animator anim;
        public Transform groundCheck;
        public LayerMask groundMask;
        public float groundDistance = 0.4f;
        public float jumpHeight = 3f;
        private Vector2 previousInput;
        private Controls controls;
        private bool isGrounded;
        private bool isSprinting;
        private bool isCrouching;
        private bool isWalking;
        private bool isJumping;
        private Vector3 lastInput;
        private Vector3 right;
        private Vector3 forward;

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
            Controls.Player.Crouch.performed += ctx => isCrouching = true;
            Controls.Player.Crouch.canceled += ctx => isCrouching = false;
            Controls.Player.Sprint.performed += ctx => isSprinting = true;
            Controls.Player.Sprint.canceled += ctx => isSprinting = false;
        }

        [ClientCallback]
        private void OnEnable() => Controls.Enable();

        [ClientCallback]
        private void OnDisable() => Controls.Disable();

        [ClientCallback]
        private void Update()
        {
            Move();
            UpdateAnimations();
        }
        private void UpdateAnimations()
        {
            if (isGrounded)
            {
                if (isCrouching)
                {
                    anim.SetBool("isCrouching", true);
                    anim.SetBool("isWalking", false);
                    anim.SetBool("isJumping", false);
                    anim.SetBool("isDead", false);
                }
                else
                {
                    anim.SetBool("isCrouching", false);
                    anim.SetBool("isWalking", true);
                    anim.SetBool("isJumping", false);
                    anim.SetBool("isDead", false);
                }
                
                if (isSprinting)
                {
                    anim.SetFloat("xInput", previousInput.normalized.x);
                    anim.SetFloat("yInput", previousInput.normalized.y);
                }
                else if (isCrouching)
                {
                    anim.SetFloat("xInput", previousInput.normalized.x);
                    anim.SetFloat("yInput", previousInput.normalized.y);
                }
                else
                {
                    anim.SetFloat("xInput", previousInput.normalized.x / 2);
                    anim.SetFloat("yInput", previousInput.normalized.y / 2);
                }
            }
        }
   
        [Client]
        private void Move()
        {
            if (isCrouching == true && isSprinting == true)
            {
                isCrouching = false;
            }

            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }

            right = controller.transform.right;
            forward = controller.transform.forward;
            right.y = 0f;
            forward.y = 0f;
            Vector3 movement;


            if (isGrounded)
            {
                isJumping = false;
                movement = right.normalized * previousInput.x + forward.normalized * previousInput.y;
            }
            else
            {
                movement = right.normalized * previousInput.x + lastInput;
            }

            controller.Move(movement * CalculateMovementSpeed() * Time.deltaTime);


            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
        }

        private float CalculateMovementSpeed()
        {
            if (previousInput.y > 0 && isCrouching)
            {
                return movementSpeed * crouchMultiplier;
            }
            else if (previousInput.y > 0 && isSprinting)
            {
                return movementSpeed * sprintMultiplier;
            }
            return movementSpeed;
        }

        [Client]
        private void Jump()
        {
            if (isGrounded)
            {   
                anim.SetBool("isCrouching", false);
                anim.SetBool("isWalking", false);
                anim.SetBool("isJumping", true);
                anim.SetBool("isDead", false);
                
                isJumping = true;
                lastInput = previousInput.x * right.normalized + previousInput.y * forward.normalized;
                lastInput.y = 0;
                
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

