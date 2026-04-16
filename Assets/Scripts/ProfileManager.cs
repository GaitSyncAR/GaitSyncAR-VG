// ProfileManager.cs
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class ProfileManager
{
    // ── Singleton ──────────────────────────────────────────────
    private static ProfileManager _instance;
    public static ProfileManager Instance => _instance ??= new ProfileManager();

    public UserProfile currentProfile;
    private string _saveDirectory = "";

    // ── Constructor ─────────────────────────────────────────────
    private ProfileManager()
    {
        _saveDirectory = Application.persistentDataPath + "/Profiles/";

        if (!Directory.Exists(_saveDirectory))
        {
            Directory.CreateDirectory(_saveDirectory);
            Debug.Log("Created Profiles directory at: " + _saveDirectory);
        }
    }

    // ── Save ────────────────────────────────────────────────────
    public void SaveProfile()
    {
        if (currentProfile == null)
        {
            Debug.LogWarning("[ProfileManager] No profile loaded to save.");
            return;
        }

        string json    = JsonUtility.ToJson(currentProfile, true);
        string filePath = _saveDirectory + currentProfile.profileName + ".json";

        File.WriteAllText(filePath, json);
        Debug.Log("[ProfileManager] Saved: " + filePath);
    }

    // ── Load ────────────────────────────────────────────────────
    public void LoadProfile(string profileName)
    {
        string filePath = _saveDirectory + profileName + ".json";

        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            currentProfile = JsonUtility.FromJson<UserProfile>(json);
            Debug.Log("[ProfileManager] Loaded: " + profileName);
        }
        else
        {
            Debug.LogWarning("[ProfileManager] Profile not found, creating new.");
            currentProfile = new UserProfile(profileName);
            SaveProfile();
        }
    }

    // ── Get All ─────────────────────────────────────────────────
    public List<string> GetAvailableProfiles()
    {
        var names = new List<string>();

        if (Directory.Exists(_saveDirectory))
        {
            foreach (var path in Directory.GetFiles(_saveDirectory, "*.json"))
                names.Add(Path.GetFileNameWithoutExtension(path));
        }

        return names;
    }

    // ── Delete ───────────────────────────────────────────────────
    public void DeleteProfile(string profileName)
    {
        string filePath = _saveDirectory + profileName + ".json";

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            Debug.Log("[ProfileManager] Deleted: " + profileName);
        }
        else
        {
            Debug.LogWarning("[ProfileManager] Profile not found: " + profileName);
        }
    }

    // ── Rename ──────────────────────────────────────────────────
    public void RenameProfile(string oldName, string newName)
    {
        string oldPath = _saveDirectory + oldName + ".json";
        string newPath = _saveDirectory + newName + ".json";

        if (!File.Exists(oldPath))
        {
            Debug.LogWarning("[ProfileManager] Profile not found: " + oldName);
            return;
        }

        if (File.Exists(newPath))
        {
            Debug.LogWarning("[ProfileManager] A profile named '" + newName + "' already exists.");
            return;
        }

        File.Move(oldPath, newPath);
        Debug.Log($"[ProfileManager] Renamed '{oldName}' → '{newName}'");

        // Keep currentProfile.name in sync if we renamed the active one
        if (currentProfile?.profileName == oldName)
            currentProfile.profileName = newName;
    }
}
