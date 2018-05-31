using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;
using System.Collections.Generic;

public class Body : NetworkBehaviour
{
    [SyncVar(hook = "OnNameChange")] public string playerName;
    [SyncVar(hook = "OnIdChange")] public string identified;

    Rigidbody rigid;
    float maxVelocity = 100f;

    private void Start()
    {
        rigid = GetComponent<Rigidbody>();

        foreach (Player p in Player.players)
        {
            if (p.playerName == playerName)
            {
                p.GetComponentInChildren<RendererToggler>().DisableRenderers();
            }
        }
    }

    void OnNameChange(string value)
    {
        playerName = value;
    }
    void OnIdChange(string value)
    {
        playerName = value;
    }

    private void FixedUpdate()
    {
        if (rigid.velocity.magnitude > maxVelocity)
        {
            rigid.velocity /= rigid.velocity.magnitude / maxVelocity;
        }
        if (transform.position.y < -1000f && !rigid.isKinematic)
        {
            rigid.isKinematic = true;
        }
    }
}
