using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class ProfileManager 
{
    // Singleton Instance
    private static ProfileManager _instance;
    public static ProfileManager Instance
    {
        get
        {
            // If the manager doesn't exist yet, create it.
            if (_instance == null)
            {
                _instance = new ProfileManager();
            }
            return _instance;
        }
    }

    public UserProfile currentProfile;
    private string _saveDirectory = "";

    // Constructor
    private ProfileManager() {
        _saveDirectory = Application.persistentDataPath + "/Profiles/";

        // Ensure the directory exists
        if (!Directory.Exists(_saveDirectory))
        {
            Directory.CreateDirectory(_saveDirectory);
            Debug.Log("Created Profiles directory at: " + _saveDirectory);
        }

        UISettingsControllerV3 settingsController = UnityEngine.Object.FindFirstObjectByType<UISettingsControllerV3>();
        settingsController.PopulateProfileList();
    }

    public void SaveProfile()
    {
        if (currentProfile == null) {
            Debug.LogWarning("No profile loaded to save.");
            return;
        }

        string json = JsonUtility.ToJson(currentProfile, true); 
        string filePath = _saveDirectory + currentProfile.profileName + ".json";
        File.WriteAllText(filePath, json);
        Debug.Log("Profile saved to: " + filePath);
    }

    public void LoadProfile(string profileName)
    {
        string filePath = _saveDirectory + profileName + ".json";

        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            currentProfile = JsonUtility.FromJson<UserProfile>(json);
            Debug.Log("Loaded profile: " + profileName);
        }
        else
        {
            Debug.LogWarning("Profile not found. Creating a new one.");
            currentProfile = new UserProfile("Default Profile");
            SaveProfile();
        }
    }

    public List<string> GetAvailableProfiles()
    {
        List<string> profileNames = new List<string>();

        // Check if the directory even exists yet
        if (Directory.Exists(_saveDirectory))
        {
            // Get all files in the folder that end in .json
            string[] filePaths = Directory.GetFiles(_saveDirectory, "*.json");

            foreach (string path in filePaths)
            {
                // Strip away the folder path and the .json extension to just get the name
                string fileName = Path.GetFileNameWithoutExtension(path);
                profileNames.Add(fileName);
            }
        }

        return profileNames;
    }

    public void DeleteProfile(string profileName)
    {
        string filePath = _saveDirectory + profileName + ".json";

        // ensure we are not deleting the currently loaded profile
        if (currentProfile != null && currentProfile.profileName == profileName)
        {
            Debug.LogWarning("Cannot delete the currently loaded profile: " + profileName);
            return;
        }

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            Debug.Log("Deleted profile: " + profileName);
        }
        else
        {
            Debug.LogWarning("Profile not found: " + profileName);
        }
    }

    public void RenameProfile(string oldName, string newName)
    {
        string oldFilePath = _saveDirectory + oldName + ".json";
        string newFilePath = _saveDirectory + newName + ".json";

        if (File.Exists(oldFilePath))
        {
            if (File.Exists(newFilePath))
            {
                Debug.LogWarning("A profile with the new name already exists: " + newName);
                return;
            }

            File.Move(oldFilePath, newFilePath);
            Debug.Log("Renamed profile from " + oldName + " to " + newName);

            // If the renamed profile is currently loaded, update the currentProfile reference
            if (currentProfile != null && currentProfile.profileName == oldName)
            {
                currentProfile.profileName = newName;
            }
        }
        else
        {
            Debug.LogWarning("Profile not found: " + oldName);
        }
    }
}