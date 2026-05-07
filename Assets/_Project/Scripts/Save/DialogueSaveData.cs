using System;
using System.Collections.Generic;

[Serializable]
public sealed class DialogueSaveData
{
    public string nodeName = "Start";
    public int elementIndex;
    public string currentBackgroundName;
    public List<string> visibleCharacterNames = new List<string>();
    public List<int> usedOnceChoiceLineNumbers = new List<int>();
}
