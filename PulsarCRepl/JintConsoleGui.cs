using UnityEngine;

namespace PulsarCRepl
{
    public class JintConsoleGui
    {
        private  bool ShowJsConsole;
        public string CodeString = "";
        private Rect windowRect = new Rect(20, 20, 1200, 500);
        Vector2 CodeScroll;
        private Vector2 OutputScroll;
        private JintInstance CodeInstance;
        
        public JintConsoleGui()
        {
            CodeInstance = new JintInstance();
        }
        
        public void OnGUICode()
        {
            if (!ShowJsConsole) return;
            // Make a background box
            windowRect = GUI.ModalWindow(0, windowRect,ConsoleWindowDisplay, "Javascript Console");
        }
        public void ConsoleWindowDisplay(int windowID)
        {
            

            GUILayout.BeginVertical();
            GUILayout.Label("Insert Javascript Code below");
            CodeScroll = GUILayout.BeginScrollView(CodeScroll);
            CodeString = GUILayout.TextArea(CodeString);
            GUILayout.EndScrollView();
            if (GUILayout.Button("Run Code",GUILayout.Width(100), GUILayout.Height(100)))
            {
                CodeInstance.ExecuteCode(CodeString);
            }
            GUILayout.Label("Code Results Below");
            OutputScroll = GUILayout.BeginScrollView(OutputScroll);
            GUILayout.TextArea(CodeInstance.GetOutput());
            GUILayout.EndScrollView();
            if (GUILayout.Button("Close Console",GUILayout.Width(100), GUILayout.Height(100)))
            {
                ShowJsConsole = false;
            }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        public void OpenConsole()
        {
            ShowJsConsole = true;
        }
        
    }
}