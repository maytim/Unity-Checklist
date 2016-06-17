//Tim Maytom 2016
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class CheckListTool : EditorWindow {
    //Entry class to organize each entries data
    [System.Serializable]
    private class Entry
    {
        public bool complete;
        public string text;
        
        public Entry()
        {
            complete = false;
            text = "";
        }

        public Entry(bool b, string s)
        {
            complete = b;
            text = s;
        }
    }

    //A wrapper for the Entry class to allow JsonUtility to stringify a list of Entry objects
    [System.Serializable]
    private struct EntryListWrapper
    {
        public List<Entry> list;
    }

    //The Check List data variables
    private List<Entry> mylist;
    private string newItem = "";

    //The Check List GUI variables
    private Vector2 scroll;
    private bool activeFoldout = true;
    private bool completeFoldout = true;

    //Action Queue to manage changes to the Entry list outside of OnGUI()
    private Queue<System.Action> guiEvents = new Queue<System.Action>();

    //Check List constructor that initializes the data in the entry List
    public CheckListTool()
    {
        if (EditorPrefs.HasKey("CheckList"))
        {
            mylist = JsonUtility.FromJson<EntryListWrapper>(EditorPrefs.GetString("CheckList")).list;
        }
        else
        {
            mylist = new List<Entry>();

            mylist.Add(new Entry(false, "Sample entry."));
        }
    }

    //Method to manage the Check List window attributes
    //Note the '%&#_c' is used to create a shortcut for the Check List
    [MenuItem ("Window/Check List %&#_c")]
    static void OpenCheckListTool()
    {
        CheckListTool window = EditorWindow.GetWindow<CheckListTool>(false);
        window.minSize = new Vector2(300, 200);
        GUIContent title = new GUIContent("Check List");
        window.titleContent = title;
    }
	
	void OnGUI()
    {
        //Check for 'Enter' key being pressed in the new item TextArea to add that item to the Entry list
        if (Event.current.isKey && GUI.GetNameOfFocusedControl() == "newTextArea")
        {
            switch (Event.current.keyCode)
            {
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    if (newItem.Length > 0)
                    {
                        guiEvents.Enqueue(() =>
                        {
                           mylist.Add(new Entry(false, newItem));
                           newItem = "";
                            
                        });
                    }
                    break;
            }
        }

        //Begin the GUI Layout
        scroll = EditorGUILayout.BeginScrollView(scroll, false, false);

        //Split the Tasks between Active and Complete Foldouts for better organization
        activeFoldout = EditorGUILayout.Foldout(activeFoldout, "Active");

        if (activeFoldout)
        {
            DisplayActiveTasks();
        }

        completeFoldout = EditorGUILayout.Foldout(completeFoldout, "Complete");

        if (completeFoldout)
        {
            DisplayFinishedTasks();
        }

        //This is the special TextArea for new entries
        DisplayNewEntry();

        GUILayout.EndScrollView();

    }

    //I used the Update loop to process any changes to the entry list
    void Update()
    {
        while (guiEvents.Count > 0)
        {
            guiEvents.Dequeue().Invoke();
        }
    }

    //Load the Check List data in EditorPrefs when the window is focused
    void OnFocus()
    {
        if (EditorPrefs.HasKey("CheckList"))
        {
            mylist = JsonUtility.FromJson<EntryListWrapper>(EditorPrefs.GetString("CheckList")).list;
        }
    }

    //Save the Check List data in EditorPrefs when the window loses focus
    void OnLostFocus()
    {
        EntryListWrapper w;
        w.list = mylist;
        EditorPrefs.SetString("CheckList", JsonUtility.ToJson(w));
    }

    //Save the Check List data in EditorPrefs when the window is closed
    void OnDestroy()
    {
        EntryListWrapper w;
        w.list = mylist;
        EditorPrefs.SetString("CheckList", JsonUtility.ToJson(w));
    }

    private void DisplayFinishedTasks()
    {
        foreach (Entry e in mylist.ToArray())
        {
            if (e.complete)
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    e.complete = EditorGUILayout.Toggle(e.complete, GUILayout.MaxWidth(10));
                    EditorGUILayout.LabelField(StrikeThrough(e.text), GUILayout.MinWidth(200));
                    if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.MaxWidth(20)))
                    {
                        var entryCopy = e;
                        guiEvents.Enqueue(() =>
                        {
                            mylist.Remove(entryCopy);
                        });
                    }
                    GUILayout.FlexibleSpace();
                }
                GUILayout.EndHorizontal();
            }
        }
    }

    private void DisplayActiveTasks()
    {
        foreach (Entry e in mylist.ToArray())
        {
            if (!e.complete)
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    e.complete = EditorGUILayout.Toggle(e.complete, GUILayout.MaxWidth(12));
                    e.text = GUILayout.TextArea(e.text, 300, GUILayout.MinWidth(200));
                    if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.MaxWidth(20)))
                    {
                        var entryCopy = e;
                        guiEvents.Enqueue(() =>
                        {
                            mylist.Remove(entryCopy);
                        });
                    }
                    GUILayout.FlexibleSpace();
                }
                GUILayout.EndHorizontal();
            }
        }
    }

    private void DisplayNewEntry()
    {
        GUILayout.BeginHorizontal();
        {
            GUILayout.FlexibleSpace();
            GUILayout.Space(18);
            GUI.SetNextControlName("newTextArea");
            newItem = GUILayout.TextArea(newItem, 300, GUILayout.MinWidth(200));
            newItem = newItem.Replace("\n", "");
            if (GUILayout.Button("+", EditorStyles.miniButton, GUILayout.MaxWidth(20)))
            {
                if (newItem.Length > 0)
                {
                    guiEvents.Enqueue(() =>
                    {
                        mylist.Add(new Entry(false, newItem));
                        newItem = "";
                    });
                }
            }
            GUILayout.FlexibleSpace();
        }
        GUILayout.EndHorizontal();
    }

    //Helper method to covert regular unicode text into strikethrough text
    public string StrikeThrough(string s)
    {
        string strikethrough = "";
        foreach (char c in s)
        {
            strikethrough = strikethrough + c + '\u0336';
        }
        return strikethrough;
    }
}