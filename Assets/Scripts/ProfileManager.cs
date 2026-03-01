using UnityEngine;
using System.IO;

public class ProfileManager : MonoBehaviour
{
    public UserProfile currentProfile;
    private string saveDirectory;

    void Awake()
    {
        // Set the path to the device's persistent data folder
        saveDirectory = Application.persistentDataPath + "/Profiles/";
        
        // Ensuring the directory exists
        if (!Directory.Exists(saveDirectory))
        {
            Directory.CreateDirectory(saveDirectory);
        }
    }

    public void SaveProfile()
    {
        if (currentProfile == null) return;

        // Convert the C# object into a JSON string
        string json = JsonUtility.ToJson(currentProfile, true); 

        // Create a unique file path based on the profile name
        string filePath = saveDirectory + currentProfile.profileName + ".json";

        // Write the text to the file
        File.WriteAllText(filePath, json);
        Debug.Log("Profile saved to: " + filePath);
    }

    public void LoadProfile(string profileName)
    {
        string filePath = saveDirectory + profileName + ".json";

        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            
            // Convert the JSON string back into the C# object
            currentProfile = JsonUtility.FromJson<UserProfile>(json);
            Debug.Log("Loaded profile: " + profileName);
            
            // Applying settings throughout my application
        }
        else
        {
            Debug.LogWarning("Profile not found. Creating a new one.");
            currentProfile = new UserProfile(profileName);
            SaveProfile();
        }
    }
}