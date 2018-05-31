using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PostProcessing;
using UnityEngine.Rendering.PostProcessing;

public class QualityControl_Forest : MonoBehaviour {

    public  int[] forestTreeBillboardDistances = new int[] { 25, 50, 50, 50, 75, 100 };

    public  float[] forestWindTurbulance = new float[] { 0,0, 0, 0.5f, 1, 2 };

    public Terrain[] terrains;

    public bool[] testQuality = new bool[6];

    public WindZone wind;

    public GameObject[] postVols;
   

	void Start () {
      
        
        for (int i = 0; i < testQuality.Length; i++ )
        {
            if (testQuality[i])
            {
                QualitySettings.SetQualityLevel(i);
            }
        }

        foreach (GameObject post in postVols)
        {
            Debug.Log(QualitySettings.GetQualityLevel());
            if (QualitySettings.GetQualityLevel() == 0)
            {
                post.SetActive(false);
                Debug.Log(QualitySettings.GetQualityLevel());
            }
        }





        for (int i = 0; i < forestTreeBillboardDistances.Length; i++)
        {
            if (QualitySettings.GetQualityLevel() == i)
            {
                for (int j = 0; j < terrains.Length; j++)
                {
                    terrains[j].treeBillboardDistance = forestTreeBillboardDistances[i];
                    
                }
                wind.windTurbulence = forestWindTurbulance[i];
            }

           
        }
		
        
    }
	
	
}
