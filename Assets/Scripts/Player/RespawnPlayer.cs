using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RespawnPlayer : MonoBehaviour
{
    Vector3 startPos;
    public float respawnLevel = 0.7f;

    void Start()
    {
        startPos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if(transform.position.y < respawnLevel)
            transform.position = startPos;
    }
}