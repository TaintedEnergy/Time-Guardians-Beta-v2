using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class ShotEffectsManager : MonoBehaviour
{
    public static ParticleSystem[] impactEffect;
    public static int impactIndex;

    [SerializeField] ParticleSystem muzzleFlash;
    [SerializeField] AudioSource gunAudio;
    [SerializeField] GameObject impactPrefab;
    List<LineRenderer> lines = new List<LineRenderer>();
    int line = -1;

    // ParticleSystem impactEffect;

    //Create the impact effect for our shots
    void Start()
    {
        if (impactEffect == null)
        {
            int size = 10;
            impactEffect = new ParticleSystem[size];

            for (int i = 0; i < size; i++)
            {
                impactEffect[i] = Instantiate(impactPrefab).GetComponent<ParticleSystem>();
            }
        }
        if (lines.Count == 0)
        {
            if (GetComponent<LineRenderer>() != null)
            {
                lines.Add(GetComponent<LineRenderer>());
            }
            if (GetComponentsInChildren<LineRenderer>() != null)
            {
                foreach (LineRenderer l in GetComponentsInChildren<LineRenderer>())
                {
                    lines.Add(l);
                }
            }
        }
    }

    //Play muzzle flash and audio
    public void PlayShotEffects(Vector3 shootDirection)
    {
        if (muzzleFlash != null)
        {
            muzzleFlash.Stop(true);
            muzzleFlash.Play(true);
        }

        if (lines.Count != 0)
        {
            line++;
            if (line >= lines.Count)
            {
                line -= lines.Count;
            }

            lines[line].enabled = true;

            Quaternion q = transform.rotation;
            if (muzzleFlash != null)
            {
                transform.LookAt(impactEffect[impactIndex].gameObject.transform.position);
            }

            Ray ray = new Ray(transform.position, shootDirection);
            RaycastHit hit;

            lines[line].SetPosition(0, ray.origin);
            if (Physics.Raycast(ray, out hit, 500))
            {
                lines[line].SetPosition(1, hit.point);
            }
            else
            {
                lines[line].SetPosition(1, ray.GetPoint(500));
            }

            StartCoroutine("DisableLine", line);
        }
    }

    IEnumerator DisableLine(int value)
    {
        yield return new WaitForSeconds(Time.deltaTime * 3);

        lines[value].enabled = false;
    }

    //Play impact effect and target position
    public void PlayImpactEffect(Vector3 impactPosition)
    {
        impactEffect[impactIndex].transform.position = impactPosition;
        impactEffect[impactIndex].Stop();
        impactEffect[impactIndex].Play();

        impactIndex++;
        if (impactIndex >= impactEffect.Length)
        {
            impactIndex = 0;
        }
    }
}