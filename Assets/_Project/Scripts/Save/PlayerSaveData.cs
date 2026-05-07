using System;
using System.Collections.Generic;

[Serializable]
public sealed class PlayerSaveData
{
    public List<RelationshipValueSaveData> relationships = new List<RelationshipValueSaveData>();
}
