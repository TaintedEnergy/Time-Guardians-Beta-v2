using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Test : MonoBehaviour {

    public Camera camera;
    public GameObject empty;
    public Image image;

    private void Update()
    {
        image.transform.position = camera.WorldToScreenPoint(empty.transform.position);
    }
}
