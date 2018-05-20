using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class C4 : NetworkBehaviour
{
    [SyncVar] public bool armed;
    [SyncVar] public bool planted;

    bool countingDown;

    int time = 45;

    string minutes = "00";
    string seconds = "45";
    
    public Text minutesText;
    public Text secondsText;

    public AudioSource[] audioSources;
    public int currentAudio;
    
    public int touching;

    private void FixedUpdate()
    {
        if (armed && !countingDown)
        {
            CountDown();
            countingDown = true;
        }
    }

    public void OpenMenu()
    {
        print("Ooga");

        PlayerCanvas.canvas.c4 = this;
        TextFormatting();
        PlayerCanvas.canvas.C4Recieve(true, armed, planted, minutes, seconds);
    }

    [Command]
    public void CmdArmed()
    {
        armed = true;
        PlayerCanvas.canvas.C4Recieve(PlayerCanvas.canvas.c4Panel.activeInHierarchy, armed, planted, minutes, seconds);
    }

    [Command]
    public void CmdPlanted()
    {
        if (touching != 0)
        {
            planted = true;
            transform.root.GetComponent<Rigidbody>().isKinematic = true;
            PlayerCanvas.canvas.C4Recieve(PlayerCanvas.canvas.c4Panel.activeInHierarchy, armed, planted, minutes, seconds);
        }
    }

    void CountDown()
    {
        time--;

        // Minutes and Seconds Text Formating

        TextFormatting();

        // Play Sound

        PlaySound();
        if (time <= 3)
        {
            Invoke("PlaySound", 0.25f);
            Invoke("PlaySound", 0.5f);
            Invoke("PlaySound", 0.75f);
        }
        if (time <= 5)
        {
            Invoke("PlaySound", 0.333f);
            Invoke("PlaySound", 0.667f);
        }
        else if (time <= 10)
        {
            Invoke("PlaySound", 0.5f);
        }

        // Other

        if (time == 0)
        {
            print("Boom");

            transform.root.GetComponent<Throwable>().StartCoroutine("C4Explode");
        }
        else
        {
            Invoke("CountDown", 1f);
        }
    }

    void PlaySound()
    {
        audioSources[currentAudio].Play();
        currentAudio++;
        if (currentAudio >= 5)
        {
            currentAudio = 0;
        }
    }

    void TextFormatting()
    {
        int sec = time;

        minutes = "00";
        seconds = "00";

        int min = 0;
        while (sec > 60)
        {
            min++;
            sec -= 60;
        }
        minutes = min + "";
        if (minutes.Length == 1) { minutes = "0" + minutes; }

        seconds = sec + "";
        if (seconds.Length == 1) { seconds = "0" + seconds; }


        // Send to Canvases
        if (PlayerCanvas.canvas.c4Panel.activeInHierarchy && PlayerCanvas.canvas.c4 == this)
        {
            PlayerCanvas.canvas.C4Recieve(true, armed, planted, minutes, seconds);
        }

        minutesText.text = minutes;
        secondsText.text = seconds;
    }
}
