using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PostProcessing;

public class QualityCheck : MonoBehaviour {

    // Set In Inspector

    public bool main;
    public PostProcessingBehaviour postProcessBehaviour;
    public GameObject[] lights;

    // Values for what to set
    
    public PostProcessingProfile postProcessProfile;

    bool done;

    void Awake()
    {
        if (!done)
        {
            if (QualitySettings.GetQualityLevel() == 5)
            {
                if (main)
                {

                }
                if (postProcessBehaviour != null)
                {
                    postProcessBehaviour.profile = postProcessProfile;
                }
                foreach (GameObject g in lights)
                {
                    g.SetActive(true);
                }


                done = true;
            }
        }
    }
}
