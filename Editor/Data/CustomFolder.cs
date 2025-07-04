using System;

namespace com.neuru5278.assetorganizer.Data
{
    [Serializable]
    public class CustomFolder
    {
        public string name;
        public bool active = true;
        public ManageAction action;
        
        public CustomFolder() { }

        public CustomFolder(string newName, ManageAction action = ManageAction.Skip)
        {
            name = newName;
            this.action = action;
        }
    }
} 