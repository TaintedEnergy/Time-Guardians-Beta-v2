using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Options : MonoBehaviour {

    public static float sensitivity = 2;

	public void Set(InputField value)
    {
        float f = 2;
        sensitivity = 2;

        f = float.Parse(value.text);

        if (f > 0 && f < 10)
        {
            sensitivity = f;
        }
    }
}
