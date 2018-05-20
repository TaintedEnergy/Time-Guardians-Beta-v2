using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Throwable : NetworkBehaviour
{
    [SyncVar(hook = "OnItemChange")] public string item;
    [SyncVar] public string thrower;

    [Header("Effects")]

    public GameObject fragParticleEffect;

    public GameObject smokeParticleEffect;
    public GameObject smokedGrenadeObject;

    public GameObject flashParticleEffect;
    public GameObject flashedGrenadeObject;

    public GameObject c4ParticleEffect;

    [Header("Essentials")]

    public GameObject[] items;
    public GameObject selectedItem;

    Rigidbody rigid;

    ParticleSystem ps;

    int hitElapsedTime;
    
    [ServerCallback]
    void Start()
    {
        OnItemChange(item);
    }

    public void OnItemChange(string value)
    {
        item = value;
        // 
        rigid = GetComponent<Rigidbody>();

        // Set Effects
        if (item == "fragGrenade")
        {
            StartCoroutine("FragExplode", 3);
        }
        if (item == "smokeGrenade")
        {
            StartCoroutine("Smoke", 3);
        }
        if (item == "flashGrenade")
        {
            StartCoroutine("Flash", 3);
        }

        foreach (GameObject g in items)
        {
            if (g.name == item)
            {
                selectedItem = g;
                selectedItem.SetActive(true);
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hitElapsedTime > 5 && collision.transform.root.transform.gameObject.name != Player.player.transform.gameObject.name && selectedItem.GetComponent<AudioSource>() != null)
        {
            selectedItem.GetComponent<AudioSource>().Play();
        }

        hitElapsedTime = 0;

        if (selectedItem.name == "c4")
        {
            if (collision.transform.root.transform.GetComponent<Rigidbody>() == null)
            {
                selectedItem.GetComponent<C4>().touching++;
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (selectedItem.name == "c4")
        {
            if (collision.transform.root.transform.GetComponent<Rigidbody>() == null)
            {
                selectedItem.GetComponent<C4>().touching--;
            }
        }
    }

    private void FixedUpdate()
    {
        hitElapsedTime++;
    }

    IEnumerator FragExplode(int time)
    {
        yield return new WaitForSeconds(time);

        fragParticleEffect.SetActive(true);
        foreach (GameObject g in items)
        {
            g.SetActive(false);
        }
        rigid.isKinematic = true;
        transform.rotation = Quaternion.identity;

        Collider[] objectsInRange = Physics.OverlapSphere(transform.position, 10f);

        foreach (Collider col in objectsInRange)
        {
            // bool doContinue = false;
            //
            Quaternion rot = transform.rotation;
            Vector3 pos = transform.position;

            RaycastHit hit;
            transform.position += new Vector3(0, 0.25f, 0);
            transform.LookAt(col.transform);
            Ray ray = new Ray(transform.position, transform.forward);
            bool result = Physics.Raycast(ray, out hit, 8);

            Rigidbody newRigid = null;
            if (result && hit.transform.root.transform == col.transform.root.transform)
            {
                if (col.transform.root.GetComponent<Rigidbody>() != null && isServer)
                {
                    newRigid = col.transform.root.GetComponent<Rigidbody>();
                    newRigid.AddExplosionForce(5 + 2 * newRigid.mass, transform.position, 8, 0.5f + 0.25f * newRigid.mass, ForceMode.VelocityChange);

                    // Detect Hit Effects

                    if (hit.transform.root.transform.GetComponent<Throwable>() != null && hit.transform.root.transform.GetComponent<Throwable>().selectedItem.name == "c4")
                    {
                        if (Vector3.Distance(transform.position, hit.transform.position) < 3f)
                        {
                            col.transform.root.transform.GetComponent<Throwable>().RpcForceExplode();
                        }
                    }
                }
                if (col.transform.root.GetComponent<Player>() != null && col.transform.root.GetComponent<Player>().isLocalPlayer)
                {
                    int damage = 10;
                    if (Vector3.Distance(transform.position, hit.point) < 1.5f) { damage = 100; }
                    else if (Vector3.Distance(transform.position, hit.point) < 3f) { damage = 75; }
                    else if (Vector3.Distance(transform.position, hit.point) < 6f) { damage = 40; }

                    int direction = 0;

                    if (result)
                    {
                        Vector3 toTarget = (transform.position - hit.transform.root.transform.position).normalized;

                        if (Mathf.Abs(Vector3.Dot(toTarget, hit.transform.root.transform.forward)) > Mathf.Abs(Vector3.Dot(toTarget, hit.transform.root.transform.right)))
                        {
                            direction = 0;
                        }
                        else
                        {
                            if (Vector3.Dot(toTarget, hit.transform.root.transform.right) > 0)
                            {
                                direction = 1;
                            }
                            else
                            {
                                direction = 2;
                            }
                        }
                    }
                    Player.player.CmdSendDamage(col.transform.root.GetComponent<Player>().playerName, damage, direction, thrower);
                }
            }

            transform.position = pos;
            transform.rotation = rot;
        }

        yield return new WaitForSeconds(1);
        Destroy(gameObject);
    }

    IEnumerator Smoke(int time)
    {
        yield return new WaitForSeconds(time);

        smokeParticleEffect.SetActive(true);
        selectedItem.SetActive(false);
        smokedGrenadeObject.SetActive(true);

        yield return new WaitForSeconds(10);

        smokeParticleEffect.transform.parent = null;

        yield return new WaitForSeconds(10);

        ps = smokeParticleEffect.GetComponent<ParticleSystem>();
        var em = ps.emission;
        em.SetBursts(new ParticleSystem.Burst[] { });

        yield return new WaitForSeconds(5);
        Destroy(smokeParticleEffect);
    }

    IEnumerator Flash(int time)
    {
        yield return new WaitForSeconds(time);

        // Check Flashbang Effect

        GameObject target = Player.player.playerShooting.cameras[0].transform.gameObject;
        Vector3 toTarget = (transform.position - target.transform.position).normalized;
        if (Vector3.Dot(toTarget, target.transform.root.transform.forward) > 1 - (target.GetComponent<Camera>().fieldOfView/90))
        {
            selectedItem.transform.LookAt(target.transform.position);
            RaycastHit hit;

            Ray ray = new Ray(selectedItem.transform.position, selectedItem.transform.forward);
            bool result = Physics.Raycast(ray, out hit, 500f);

            if (result && hit.transform.root.transform.GetComponent<Rigidbody>() != null)
            {
                PlayerCanvas.canvas.Flashbang();
            }
        }

        // Other shit

        flashParticleEffect.SetActive(true);
        flashParticleEffect.transform.parent = null;
        selectedItem.SetActive(false);
        flashedGrenadeObject.SetActive(true);

        yield return new WaitForSeconds(1);

        Destroy(flashParticleEffect);
    }

    [ClientRpc]
    public void RpcForceExplode()
    {
        StartCoroutine("C4Explode");
    }

    IEnumerator C4Explode()
    {
        c4ParticleEffect.SetActive(true);
        foreach (GameObject g in items)
        {
            g.SetActive(false);
        }
        rigid.isKinematic = true;
        transform.rotation = Quaternion.identity;

        Collider[] objectsInRange = Physics.OverlapSphere(transform.position, 25f);

        foreach (Collider col in objectsInRange)
        {
            Rigidbody newRigid = null;

            if (col.transform.root.GetComponent<Rigidbody>() != null && isServer)
            {
                newRigid = col.transform.root.GetComponent<Rigidbody>();
                newRigid.AddExplosionForce(10 + 3 * newRigid.mass, transform.position, 25, 1.5f + 0.5f * newRigid.mass, ForceMode.VelocityChange);

                // Detect Hit Effects

                if (col.transform.root.transform.GetComponent<Throwable>() != null && col.transform.root.transform.GetComponent<Throwable>().selectedItem.name == "c4")
                {
                    if (Vector3.Distance(transform.position, col.transform.position) < 8f)
                    {
                        col.transform.root.transform.GetComponent<Throwable>().RpcForceExplode();
                    }
                }
            }
            if (col.transform.root.GetComponent<Player>() != null && col.transform.root.GetComponent<Player>().isLocalPlayer)
            {
                int damage = 25;
                if (Vector3.Distance(transform.position, col.transform.position) < 3f) { damage = 1000; }
                else if (Vector3.Distance(transform.position, col.transform.position) < 5f) { damage = 250; }
                else if (Vector3.Distance(transform.position, col.transform.position) < 10f) { damage = 100; }
                else if (Vector3.Distance(transform.position, col.transform.position) < 15f) { damage = 75; }
                else if (Vector3.Distance(transform.position, col.transform.position) < 20f) { damage = 50; }

                int direction = 0;

                Vector3 toTarget = (transform.position - col.transform.root.transform.position).normalized;

                if (Mathf.Abs(Vector3.Dot(toTarget, col.transform.root.transform.forward)) > Mathf.Abs(Vector3.Dot(toTarget, col.transform.root.transform.right)))
                {
                    direction = 0;
                }
                else
                {
                    if (Vector3.Dot(toTarget, col.transform.root.transform.right) > 0)
                    {
                        direction = 1;
                    }
                    else
                    {
                        direction = 2;
                    }
                }
                Player.player.CmdSendDamage(col.transform.root.GetComponent<Player>().playerName, damage, direction, thrower);
            }
        }

        yield return new WaitForSeconds(5);
        Destroy(gameObject);
    }
}