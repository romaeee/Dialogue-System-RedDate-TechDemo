using UnityEngine;

[CreateAssetMenu(fileName = "New Relationship Type", menuName = "Red Date/Dialogue/Relationship Type")]
public sealed class RelationshipTypeData : ScriptableObject
{
    [SerializeField] private string relationshipName;

    public string RelationshipName => relationshipName;
}
