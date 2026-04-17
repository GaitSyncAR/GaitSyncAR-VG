using UnityEngine;
using UnityEngine.UIElements;

public class ShapeTabController
{
    private MetronomeController _metronome;
    private CalibrationPageController _parent;

    public ShapeTabController(CalibrationPageController parent, VisualElement container, MetronomeController metronome)
    {
        var rows = container.Query("Horizontal_slot").ToList();
        _metronome = metronome;
        _parent = parent;

        rows[0].Q<Button>("Left").clicked  += () => ScaleUniform(-0.1f);
        rows[0].Q<Button>("Right").clicked += () => ScaleUniform(0.1f);
        rows[1].Q<Button>("Left").clicked  += () => ScaleStretch(-2f);
        rows[1].Q<Button>("Right").clicked += () => ScaleStretch(2f);
    }

    public void ScaleUniform(float amount)
    {
        if (_metronome == null) return;
        _metronome.UniformScale(amount);
        ProfileManager.Instance.currentProfile.metronomeSize = _metronome.transform.localScale;

        _parent.PlayHaptic();
    }

    public void ScaleStretch(float amount)
    {
        if (_metronome != null)
        {
            _metronome.ApplyStretch(amount);
            ProfileManager.Instance.currentProfile.metronomeBarScaleY = _metronome.metronomeBar.localScale.y;
        }
        _parent.PlayHaptic();
    }
}
