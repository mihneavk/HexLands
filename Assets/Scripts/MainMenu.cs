using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    private bool isBlueSelected = true;

    private void OnGUI()
    {
        // 0. Desenăm un panou transparent închis la culoare în spate pentru aspect modern
        GUIStyle panelStyle = new GUIStyle(GUI.skin.box);
        panelStyle.normal.background = MakeTex(2, 2, new Color(0f, 0f, 0f, 0.6f));
        GUI.Box(new Rect(Screen.width / 2 - 350, Screen.height / 2 - 250, 700, 500), "", panelStyle);

        // 1. Stilul pentru Titlu
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 75; // Ușor redus ca să nu se mai taie
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;

        // Efect de umbră (Desenăm textul negru puțin mai jos și la dreapta)
        titleStyle.normal.textColor = Color.black;
        GUI.Label(new Rect(Screen.width / 2 - 300 + 4, Screen.height / 2 - 220 + 4, 600, 100), "CATAN - VS AI", titleStyle);

        // Textul principal alb (Peste umbră)
        titleStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(Screen.width / 2 - 300, Screen.height / 2 - 220, 600, 100), "CATAN - VS AI", titleStyle);

        // 2. Stilul pentru Subtitlu
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 30;
        labelStyle.alignment = TextAnchor.MiddleCenter;
        labelStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f); // Gri deschis

        GUI.Label(new Rect(Screen.width / 2 - 250, Screen.height / 2 - 100, 500, 50), "Alege Culoarea Ta:", labelStyle);

        // 3. Butoane de selecție (Sunt mult mai elegante decât bifele clasice)
        GUIStyle blueBtnStyle = new GUIStyle(GUI.skin.button);
        blueBtnStyle.fontSize = 25;
        blueBtnStyle.fontStyle = FontStyle.Bold;
        blueBtnStyle.normal.textColor = isBlueSelected ? Color.white : Color.gray;

        if (GUI.Button(new Rect(Screen.width / 2 - 225, Screen.height / 2 - 30, 450, 50), isBlueSelected ? "► Jucător 1 (ALBASTRU) ◄" : "Jucător 1 (ALBASTRU)", blueBtnStyle))
        {
            isBlueSelected = true;
        }

        GUIStyle orangeBtnStyle = new GUIStyle(GUI.skin.button);
        orangeBtnStyle.fontSize = 25;
        orangeBtnStyle.fontStyle = FontStyle.Bold;
        orangeBtnStyle.normal.textColor = !isBlueSelected ? Color.white : Color.gray;

        if (GUI.Button(new Rect(Screen.width / 2 - 225, Screen.height / 2 + 30, 450, 50), !isBlueSelected ? "► Jucător 2 (PORTOCALIU) ◄" : "Jucător 2 (PORTOCALIU)", orangeBtnStyle))
        {
            isBlueSelected = false;
        }

        // 4. Butonul de START (Buton imens)
        GUIStyle playBtnStyle = new GUIStyle(GUI.skin.button);
        playBtnStyle.fontSize = 40;
        playBtnStyle.fontStyle = FontStyle.Bold;
        playBtnStyle.normal.textColor = Color.green;

        if (GUI.Button(new Rect(Screen.width / 2 - 150, Screen.height / 2 + 130, 300, 80), "START JOC", playBtnStyle))
        {
            PlayerPrefs.SetString("HumanColor", isBlueSelected ? "Blue" : "Orange");
            PlayerPrefs.Save();

            SceneManager.LoadScene(1);
        }
    }

    // Funcție mică pentru a genera textura semi-transparentă din spate
    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; ++i)
        {
            pix[i] = col;
        }
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
}