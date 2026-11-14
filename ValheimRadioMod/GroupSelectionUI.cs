using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ValheimRadioMod
{
    public class GroupSelectionUI : MonoBehaviour
    {
        public static GroupSelectionUI Instance { get; private set; }

        private List<string> displayGroups = new List<string>();
        private int selectedIndex = 0;
        private bool isOpen = false;
        public static float LastConfirmTime = 0f;
        private float lastDpadTime = 0f;

        private static readonly string[] DefaultSuggestions = { "Home", "Work", "Travel" };

        private void Awake()
        {
            if (Instance != null) { Destroy(this); return; }
            Instance = this;
        }

        private void Update()
        {
            isOpen = TextInput.IsVisible();
            if (!isOpen) return;

            RefreshGroupList();

            // Navigation – direkt über KeyCode, damit es nicht von ZInput-Patches abhängt
            if (Input.GetKeyDown(KeyCode.JoystickButton4) || Input.GetKeyDown(KeyCode.UpArrow)) // DPad Up oft Button oder Achse
                selectedIndex = Mathf.Max(0, selectedIndex - 1);

            if (Input.GetKeyDown(KeyCode.JoystickButton6) || Input.GetKeyDown(KeyCode.DownArrow))
                selectedIndex = Mathf.Min(displayGroups.Count - 1, selectedIndex + 1);

            // DPad Navigation (Buttons, nicht Achsen)
            if ((ZInput.GetButtonDown("JoyDPadUp") || Input.GetKeyDown(KeyCode.UpArrow)) && Time.time - lastDpadTime > 0.2f)
            {
                selectedIndex = Mathf.Max(0, selectedIndex - 1);
                lastDpadTime = Time.time;
            }

            if ((ZInput.GetButtonDown("JoyDPadDown") || Input.GetKeyDown(KeyCode.DownArrow)) && Time.time - lastDpadTime > 0.2f)
            {
                selectedIndex = Mathf.Min(displayGroups.Count - 1, selectedIndex + 1);
                lastDpadTime = Time.time;
            }
            // Bestätigen (A / X / Controller)
            if (ZInput.GetButtonDown("JoyButtonA") || ZInput.GetButtonDown("JoyButtonX") || Input.GetKeyDown(KeyCode.JoystickButton0))
                ConfirmSelected();
            // Enter / KeypadEnter → getippten Text übernehmen, sonst Listeneintrag
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                string typed = TryGetTextInputContent();
                if (!string.IsNullOrWhiteSpace(typed))
                {
                    var radio = RadioBehaviour.GetFirstHovered();
                    if (radio != null)
                    {
                        radio.SetText(typed.Trim());
                        LastConfirmTime = Time.time;
                        TextInput.instance?.Hide();
                        return;
                    }
                }
                ConfirmSelected(); // Fallback
                return;
            }
            // Löschen
            if (ZInput.GetButtonDown("JoyButtonY") || Input.GetKeyDown(KeyCode.JoystickButton3) || Input.GetKeyDown(KeyCode.Delete))
                DeleteSelected();
        }

        private void OnGUI()
        {
            if (!isOpen || displayGroups.Count == 0) return;

            float width = 300f;
            float rowHeight = 26f;
            float height = 56f + displayGroups.Count * rowHeight;
            float x = (Screen.width - width) / 2f;
            float y = Screen.height * 0.30f;

            GUI.Box(new Rect(x, y, width, height), "Gruppe wählen");

            for (int i = 0; i < displayGroups.Count; i++)
            {
                Rect row = new Rect(x + 8, y + 30 + i * rowHeight, width - 16, rowHeight - 2);
                bool isSelected = i == selectedIndex;

                GUI.backgroundColor = isSelected ? new Color(1f, 0.9f, 0.4f) : Color.white;

                if (GUI.Button(row, displayGroups[i]))
                {
                    selectedIndex = i;
                    ConfirmSelected();
                }

                // Rechtsklick löschen
                if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && row.Contains(Event.current.mousePosition))
                {
                    selectedIndex = i;
                    DeleteSelected();
                    Event.current.Use();
                }

                GUI.backgroundColor = Color.white;
            }

            GUI.Label(new Rect(x + 8, y + height - 22, width - 16, 20),
                "Enter/A = wählen    Y/Entf/Rechtsklick = löschen");
        }

        private void RefreshGroupList()
        {
            var set = new HashSet<string>(DefaultSuggestions);

            foreach (var radio in RadioBehaviour.AllRadios)
            {
                if (radio != null && !string.IsNullOrEmpty(radio.Group))
                    set.Add(radio.Group);
            }

            displayGroups = set.OrderBy(g => g).ToList();

            if (selectedIndex >= displayGroups.Count)
                selectedIndex = Mathf.Max(0, displayGroups.Count - 1);
        }

        private void ConfirmSelected()
        {
            if (selectedIndex < 0 || selectedIndex >= displayGroups.Count) return;

            string name = displayGroups[selectedIndex];
            var radio = RadioBehaviour.GetFirstHovered();
            if (radio != null)
            {
                radio.SetGroup(name);
                LastConfirmTime = Time.time;
                TextInput.instance?.Hide();
            }
        }

        private void DeleteSelected()
        {
            if (selectedIndex < 0 || selectedIndex >= displayGroups.Count) return;

            string name = displayGroups[selectedIndex];

            foreach (var radio in RadioBehaviour.AllRadios)
            {
                if (radio != null && radio.Group == name)
                    radio.SetGroup("");
            }

            RefreshGroupList();
        }

        private string TryGetTextInputContent()
        {
            try
            {
                if (TextInput.instance == null) return "";

                var field = AccessTools.Field(typeof(TextInput), "m_textField")
                         ?? AccessTools.Field(typeof(TextInput), "m_inputField");

                if (field == null) return "";

                var inputField = field.GetValue(TextInput.instance);
                if (inputField == null) return "";

                var textProp = inputField.GetType().GetProperty("text");
                return textProp?.GetValue(inputField)?.ToString() ?? "";
            }
            catch (Exception e)
            {
                Jotunn.Logger.LogWarning($"Could not read TextInput content: {e.Message}");
                return "";
            }
        }
    }
}