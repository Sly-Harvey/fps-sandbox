using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDevTools : MonoBehaviour
{
    Vector3 startPos;

    Quaternion startRotation;

    public KeyCode respawnKey = KeyCode.E;

    private void Start()
    {
        startPos = transform.position;
    }

    private void Update()
    {
        if (Input.GetKeyDown(respawnKey))
            transform.position = startPos;
    }
}
