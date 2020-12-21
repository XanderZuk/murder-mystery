using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Mirror;
using System;

namespace Scripts.Player
{
    public class PlayerNameTag : NetworkBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameText = null;

        

        private void SetName()
        {
            nameText.text = gameObject.GetComponent<PlayerGameManager>().gamePlayerLobby.DisplayName;
        }
    }
}

