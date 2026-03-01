using UnityEngine;
using System.IO;

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
}