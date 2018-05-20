using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace Prototype.NetworkLobby
{
    public class LobbyTopPanel : MonoBehaviour
    {
        public bool isInGame = false;

        protected bool isDisplayed = true;
        protected Image panelImage;

        public static List<string> cursorLocks = new List<string>();

        void Start()
        {
            panelImage = GetComponent<Image>();
        }


        void Update()
        {
            if (!isInGame)
                return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ToggleVisibility(!isDisplayed);
                
                if (isDisplayed)
                {
                    CursorLocked("pause", "add");
                }
                else
                {
                    CursorLocked("pause", "remove");
                }
            }
            if (Input.GetKeyDown("i"))
            {
                foreach(string value in cursorLocks)
                {
                    print(value);
                }
            }

        }

        public void ToggleVisibility(bool visible)
        {
            isDisplayed = visible;
            foreach (Transform t in transform)
            {
                t.gameObject.SetActive(isDisplayed);
            }

            if (panelImage != null)
            {
                panelImage.enabled = isDisplayed;
            }
        }

        public static void CursorLocked(string value, string type)
        {
            if (type == "clear")
            {
                cursorLocks.Clear();
            }
            if (type == "remove")
            {
                for (int i = 0; i < cursorLocks.Count; i++)
                {
                    if (cursorLocks[i] == value)
                    {
                        cursorLocks.RemoveAt(i);
                        // End
                        i = cursorLocks.Count;
                    }
                }
            }
            if (type == "add")
            {
                cursorLocks.Add(value);
            }

            if (cursorLocks.Count == 0)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }
}