using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Relationship Type Database", menuName = "Red Date/Dialogue/Relationship Type Database")]
public sealed class RelationshipTypeDatabase : ScriptableObject
{
    [SerializeField] private List<RelationshipTypeData> relationshipTypes = new List<RelationshipTypeData>();

    public IReadOnlyList<RelationshipTypeData> RelationshipTypes => relationshipTypes;

    public RelationshipTypeData GetByName(string relationshipName)
    {
        if (string.IsNullOrWhiteSpace(relationshipName))
        {
            return null;
        }

        for (int i = 0; i < relationshipTypes.Count; i++)
        {
            RelationshipTypeData relationshipType = relationshipTypes[i];
            if (relationshipType != null && relationshipType.RelationshipName == relationshipName)
            {
                return relationshipType;
            }
        }

        return null;
    }
}
