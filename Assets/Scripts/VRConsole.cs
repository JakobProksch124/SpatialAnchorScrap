using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class VRConsole : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] private TMP_Text consoleText; // Dein TMP_Text für die Konsole
    [SerializeField] private int maxLines = 20;    // Max Logs sichtbar

    [Header("Filter Settings")]
    [Tooltip("Wenn true, zeigt nur Errors und Exceptions an.")]
    [SerializeField] private bool showOnlyErrors = true;

    private readonly Queue<string> logQueue = new Queue<string>();

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        // Filter: nur Errors und Exceptions
        if (showOnlyErrors && type != LogType.Error && type != LogType.Exception)
            return;

        // Farbe für Error/Exception
        string coloredLog = $"<color=red>{logString}</color>";

        // Queue verwalten
        logQueue.Enqueue(coloredLog);
        if (logQueue.Count > maxLines)
            logQueue.Dequeue();

        // Text aktualisieren
        consoleText.text = string.Join("\n", logQueue);
    }
}
