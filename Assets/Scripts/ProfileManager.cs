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
    private string saveDirectory;

    // Constructor
    private ProfileManager()
    {
        saveDirectory = Application.persistentDataPath + "/Profiles/";
        
        if (!Directory.Exists(saveDirectory))
        {
            Directory.CreateDirectory(saveDirectory);
        }
    }

    public void SaveProfile()
    {
        if (currentProfile == null) return;

        string json = JsonUtility.ToJson(currentProfile, true); 
        string filePath = saveDirectory + currentProfile.profileName + ".json";
        File.WriteAllText(filePath, json);
        Debug.Log("Profile saved to: " + filePath);
    }

    public void LoadProfile(string profileName)
    {
        string filePath = saveDirectory + profileName + ".json";

        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            currentProfile = JsonUtility.FromJson<UserProfile>(json);
            Debug.Log("Loaded profile: " + profileName);
        }
        else
        {
            Debug.LogWarning("Profile not found. Creating a new one.");
            currentProfile = new UserProfile(profileName);
            SaveProfile();
        }
    }

    public List<string> GetAvailableProfiles()
    {
        List<string> profileNames = new List<string>();

        // Check if the directory even exists yet
        if (Directory.Exists(saveDirectory))
        {
            // Get all files in the folder that end in .json
            string[] filePaths = Directory.GetFiles(saveDirectory, "*.json");

            foreach (string path in filePaths)
            {
                // Strip away the folder path and the .json extension to just get the name
                string fileName = Path.GetFileNameWithoutExtension(path);
                profileNames.Add(fileName);
            }
        }

        return profileNames;
    }
}