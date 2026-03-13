using UnityEngine;
using UnityEngine.UIElements;

[CreateAssetMenu(fileName = "CharacterData", menuName = "ScriptableObjects/CharacterData", order = 1)]
public class CharacterDataSO : ScriptableObject
{
    //public Sprite CharacterIcon1;
    public VectorImage CharacterIcon;
    public string Name;
    public string ClassName;
    
}
