using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Scripts.Player
{
    public class Billboard : MonoBehaviour
    {
        private Transform mainCamera;
        void Start()
        {
            mainCamera = Camera.main.transform;
        }
        private void LateUpdate()
        {
            transform.LookAt(transform.position + mainCamera.rotation * Vector3.forward, mainCamera.rotation * Vector3.up);
        }
    }
}

