using UnityEngine;
using UnityEngine.UIElements;

public class ShapeTabController
{
    public void Initialize(CalibrationPageController parent, VisualElement container)
    {
        var rows = container.Query("Horizontal_slot").ToList();

        rows[0].Q<Button>("Left").clicked  += () => parent.ScaleUniform(-0.1f);
        rows[0].Q<Button>("Right").clicked += () => parent.ScaleUniform(0.1f);
        rows[1].Q<Button>("Left").clicked  += () => parent.ScaleStretch(-2f);
        rows[1].Q<Button>("Right").clicked += () => parent.ScaleStretch(2f);
    }
}
