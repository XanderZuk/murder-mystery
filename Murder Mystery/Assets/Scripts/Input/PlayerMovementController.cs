using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;
using System;
using System.IO.MemoryMappedFiles;
using Scripts.Player;
using Scripts.Networking;

namespace Scripts.Input
{
    public class PlayerMovementController : NetworkBehaviour
    {
        [SerializeField] public float movementSpeed = 10f;
        [SerializeField] public float gravity = -16.81f;
        [SerializeField] public float crouchMultiplier = 1f;
        [SerializeField] public float sprintMultiplier = 1f;
        [SerializeField] private CharacterController controller = null;
        private Life life = null;

        private NetworkManagerLobby room;
        private NetworkManagerLobby Room
        {
            get
            {
                if (room != null)
                {
                    return room;
                }
                return room = NetworkManager.singleton as NetworkManagerLobby;
            }
        }

        public Animator anim;
        public Transform groundCheck;
        public LayerMask groundMask;
        public float groundDistance = 0.4f;
        public float jumpHeight = 3f;
        private Vector2 previousInput;
        private bool isGrounded;
        private bool isSprinting;
        private bool isCrouching;
        private bool isDead;
        
        private Vector3 lastInput;
        private Vector3 right;
        private Vector3 forward;
        Vector3 velocity;


        public override void OnStartAuthority()
        {
            enabled = true;
            life = gameObject.GetComponent<Life>();
            InputManager.Controls.Player.Move.performed += ctx => SetMovement(ctx.ReadValue<Vector2>());
            InputManager.Controls.Player.Move.canceled += ctx => ResetMovement();
            InputManager.Controls.Player.Jump.performed += ctx => Jump();
            InputManager.Controls.Player.Crouch.performed += ctx => isCrouching = true;
            InputManager.Controls.Player.Crouch.canceled += ctx => isCrouching = false;
            InputManager.Controls.Player.Sprint.performed += ctx => isSprinting = true;
            InputManager.Controls.Player.Sprint.canceled += ctx => isSprinting = false;
            InputManager.Controls.Player.Kill.performed += ctx => KillEnemy();
        }


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
                if (isDead)
                {
                    anim.SetBool("isCrouching", false);
                    anim.SetBool("isWalking", false);
                    anim.SetBool("isJumping", false);
                    anim.SetBool("isDead", true);
                    return;
                }
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

            // Forces player onto ground
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
                Debug.Log("Jumped");
                anim.SetBool("isCrouching", false);
                anim.SetBool("isWalking", false);
                anim.SetBool("isJumping", true);
                anim.SetBool("isDead", false);

                lastInput = previousInput.x * right.normalized + previousInput.y * forward.normalized;
                lastInput.y = 0;

                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                controller.Move(velocity * Time.deltaTime);

            }
        }
        
        [Client]
        private void KillEnemy()
        {
            Debug.LogWarning("Ran Kill Enemy");
            if (((life.IsMurderer) || (life.IsDetective)) && life.IsAlive)
            {
                CmdKillEnemy();
                Debug.LogWarning("Ran CmdKill Enemy");
            }
            
        }

        [Command]   
        private void CmdKillEnemy()
        {
            Debug.Log("CmdKillEnemy");
            NetworkGamePlayerLobby closestPlayer = null;
            float distanceToPlayer = 1000000;

            foreach (var player in Room.LivingPlayers)
            {
                float distance = Vector3.Distance(player.playerGameManager.gameObject.transform.position, gameObject.transform.position);
                if ((distance < distanceToPlayer) && distance != 0) 
                {
                    distanceToPlayer = distance;
                    closestPlayer = player;
                }
            }

            if (distanceToPlayer <= 5)
            {
                Debug.LogWarning($"closest player = {closestPlayer.DisplayName}");
                Debug.LogWarning($"**************** found closest player at {distanceToPlayer}");
                closestPlayer.playerGameManager.life.KillPlayer();
                Debug.LogWarning("Killed player");
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

