using UnityEngine;
using ValheimRadioMod;
public class RadioLoadingUI : MonoBehaviour
{
    private void OnGUI()
    {
        if (AudioManager.Instance == null || !AudioManager.Instance.IsLoading) return;

        var am = AudioManager.Instance;
        float progress = am.TotalStations > 0 ? (float)am.LoadedStations / am.TotalStations : 0f;

        float width = 520f;
        float height = 200f;
        float x = (Screen.width - width) / 2f;
        float y = (Screen.height - height) / 2f - Screen.height * 0.05f;

        // Hintergrund
        Color oldColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.75f);
        GUI.Box(new Rect(x - 10, y - 10, width + 20, height + 20), "");
        GUI.color = oldColor;

        GUILayout.BeginArea(new Rect(x, y, width, height));
        GUILayout.Space(12);   // <-- hier den Text nach unten schieben (Wert anpassen)

        var centerStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 14
        };

        var titleStyle = new GUIStyle(centerStyle)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold
        };

        GUILayout.Label("Valheim Radio Mod", titleStyle);
        GUILayout.Space(8);

        GUILayout.Label(Localization.instance.Localize("$radio_loading_wait"), centerStyle);
        GUILayout.Label(Localization.instance.Localize("$radio_loading_stations"), centerStyle);

        Rect barRect = GUILayoutUtility.GetRect(width - 40, 22);
        barRect.x += 10; // leicht einrücken
        GUI.Box(barRect, "");
        GUI.Box(new Rect(barRect.x, barRect.y, barRect.width * progress, barRect.height), "");

        GUILayout.Space(6);

        GUILayout.Label(string.Format(
            Localization.instance.Localize("$radio_loading_station"),
            am.LoadedStations + 1, am.TotalStations, am.CurrentStationName), centerStyle);

        GUILayout.Label(string.Format(
            Localization.instance.Localize("$radio_loading_track"),
            am.CurrentTrackIndex, am.CurrentStationTracks, am.CurrentTrackName), centerStyle);

        GUILayout.EndArea();
    }
}