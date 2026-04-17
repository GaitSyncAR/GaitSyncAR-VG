using UnityEngine;
using UnityEngine.UIElements;


public class PositionTabController
{
    private VisualElement _container;
    private MetronomeController _metronome;
    private CalibrationPageController _parent;

    public PositionTabController(CalibrationPageController parent, VisualElement container, MetronomeController metronome)
    {
        _container = container;
        _metronome = metronome;
        _parent = parent;

        var rows = _container.Query("Horizontal_slot").ToList();

        rows[0].Q<Button>("Left").clicked  += () => Move(new Vector3(-0.5f,  0,  0));
        rows[0].Q<Button>("Right").clicked += () => Move(new Vector3( 0.5f,  0,  0));
        rows[1].Q<Button>("Left").clicked  += () => Move(new Vector3( 0,  0.5f, 0));
        rows[1].Q<Button>("Right").clicked += () => Move(new Vector3( 0, -0.5f, 0));
        rows[2].Q<Button>("Left").clicked  += () => Move(new Vector3( 0,  0, -0.5f));
        rows[2].Q<Button>("Right").clicked += () => Move(new Vector3( 0,  0,  0.5f));
    }

    public void Move(Vector3 delta)
    {
        if (_metronome != null)
        {
            _metronome.Move(delta);
            ProfileManager.Instance.currentProfile.metronomePosition = _metronome.transform.position;
        }
        _parent.PlayHaptic();
    }
}
