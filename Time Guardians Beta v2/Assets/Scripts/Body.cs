using UnityEngine;
using System;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;
using System.Collections.Generic;

public class Body : NetworkBehaviour
{
    [SyncVar(hook = "OnNameChange")] public string playerName;
    [SyncVar(hook = "OnIdChange")] public string identified;

    Rigidbody rigid;
    float maxVelocity = 50f;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody>();

        if (isServer)
        {
            DisableRender();
        }
    }

    void OnNameChange(string value)
    {
        playerName = value;

        // Disable Render

        DisableRender();
    }
    void OnIdChange(string value)
    {
        playerName = value;
    }

    private void FixedUpdate()
    {
        if (rigid.velocity.magnitude > maxVelocity)
        {
            rigid.velocity *= 0.9f;
        }
        if (transform.position.y < -500f)
        {
            gameObject.SetActive(false);
        }
    }

    void DisableRender()
    {
        foreach (GameObject g in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (g.GetComponent<Player>().playerName == playerName)
            {
                g.GetComponentInChildren<RendererToggler>().DisableRenderers();
            }
        }

        print("Body tells to disable at " + DateTime.Now);
    }
}
