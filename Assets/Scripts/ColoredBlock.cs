using UnityEngine;

public class ColoredBlock : Block, IInteractable
{
    private int _groupID = -1;
    public int GroupID { get => _groupID; set => _groupID = value; }

    public void OnInteract()
    {
        if(_groupID == -1) { return; }

        BoardCreator boardCreator = GetComponentInParent<BoardCreator>();
        boardCreator.BlastGroup(_groupID);
    }

    private void OnMouseDown()
    {
        Debug.Log(name + " clicked on " + _groupID);
        OnInteract();
    }
}
