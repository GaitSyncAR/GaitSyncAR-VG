using UnityEngine;

[System.Serializable]
public class UserProfile
{
    public string profileName;
    public float bpm;
    public float metronomeUniformScale;
    public float metronomeBarScaleY;
    public Vector3 metronomePosition;
    public Color metronomeColour;
    
    // Constructor
    public UserProfile(string name)
    {
        profileName = name;

        // default values
        bpm = 120f;
        metronomeUniformScale = 1f;
        metronomeBarScaleY = 1f;
        metronomePosition = new Vector3(-0.003609717f, 1.191586f, 7.757648f);
        metronomeColour = Color.white;
    }
}