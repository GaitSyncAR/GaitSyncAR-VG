using UnityEngine;

[System.Serializable]
public class UserProfile
{
    public string profileName;
    public float bpm;
    public float metronomeBarScaleY;
    public Vector3 metronomeSize;
    public Vector3 metronomePosition;
    public Color metronomeColour;
    
    // Constructor
    public UserProfile(string name)
    {
        profileName = name;

        // default values
        bpm = 120f;
        metronomeSize = Vector3.one;
        metronomeBarScaleY = 129.3212f;
        metronomePosition = new Vector3(-0.003609717f, 1.191586f, 7.757648f);
        metronomeColour = new Color(0.99607f, 0.99607f, 0f); // yellow
    }
}