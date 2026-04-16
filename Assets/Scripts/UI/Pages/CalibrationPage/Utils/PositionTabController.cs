using UnityEngine;
using UnityEngine.UIElements;


public class PositionTabController
{
    private VisualElement _container;

    public void Initialize(CalibrationPageController parent, VisualElement container)
    {
        _container = container;
        var rows = _container.Query("Horizontal_slot").ToList();

        rows[0].Q<Button>("Left").clicked  += () => parent.Move(new Vector3(-0.5f,  0,  0));
        rows[0].Q<Button>("Right").clicked += () => parent.Move(new Vector3( 0.5f,  0,  0));
        rows[1].Q<Button>("Left").clicked  += () => parent.Move(new Vector3( 0,  0.5f, 0));
        rows[1].Q<Button>("Right").clicked += () => parent.Move(new Vector3( 0, -0.5f, 0));
        rows[2].Q<Button>("Left").clicked  += () => parent.Move(new Vector3( 0,  0, -0.5f));
        rows[2].Q<Button>("Right").clicked += () => parent.Move(new Vector3( 0,  0,  0.5f));
    }
}
