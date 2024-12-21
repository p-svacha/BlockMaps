using UnityEngine;

/// <summary>
/// Writes all console output to the screen. Useful if you want to see the console on a build.
/// </summary>
public class ConsoleToGUI : MonoBehaviour
{
    public bool showConsoleLog = false;

    private static string myLog = "";
    private string output;
    private string stack;

    void OnEnable()
    {
        Application.logMessageReceived += Log;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= Log;
    }

    public void Log(string logString, string stackTrace, LogType type)
    {
        output = logString;
        stack = stackTrace;
        myLog = output + "\n" + myLog;

        // Make sure the log doesn't grow indefinitely
        if (myLog.Length > 5000)
        {
            myLog = myLog.Substring(0, 4000);
        }
    }

    private void OnGUI()
    {
        // Only show the console if toggled on
        if (!showConsoleLog) return;

        // Get the text currently displayed in the TextArea
        string displayedText = GUI.TextArea(
            new Rect(10, 10, Screen.width * 0.25f, Screen.height * 0.25f),
            myLog
        );

        // Revert any edits to preserve the original log
        // (This trick preserves text selection/copy in most Unity versions.)
        if (displayedText != myLog)
        {
            // Overwrite any user-typed changes with the original text
            displayedText = myLog;

            // Optional: remove focus if you want to immediately kill edit actions
            //GUI.FocusControl(null);
        }

        // Assign it back (most likely unchanged)
        myLog = displayedText;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            showConsoleLog = !showConsoleLog;
        }
    }
}