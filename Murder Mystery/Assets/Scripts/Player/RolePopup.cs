using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Scripts.Input;

namespace Scripts.Player
{
    public class RolePopup : NetworkBehaviour
    {
        [SerializeField] private Life life = null;

        [SerializeField] private Animator animator = null;
        [SerializeField] private Canvas canvas = null;
        [SerializeField] private GameObject murdererText = null;
        [SerializeField] private GameObject detectiveText = null;
        [SerializeField] private GameObject innocentText = null;
        public void StartAnimation()
        {
            if (life.IsMurderer)
            {
                murdererText.SetActive(true);
            }
            else if (life.IsDetective)
            {
                detectiveText.SetActive(true);
            }
            else
            {
                innocentText.SetActive(true);
            }
            animator.enabled = true;
        }

        public void EndAnimation()
        {
            animator.enabled = false;
            canvas.enabled = false;
        }
    }
}

