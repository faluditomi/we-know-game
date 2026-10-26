using UnityEngine;

[CreateAssetMenu(fileName = "Tile", menuName = "Scriptable Objects/Tile")]
public class Tile : ScriptableObject
{
    public string tileName;
    public string foundDescription;
    public Sprite backLogo;
}
