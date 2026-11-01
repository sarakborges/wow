namespace Wow.Core.Content;

/// <summary>
/// Identifies the game generation from which an imported asset or record came.
/// It is source provenance, not a runtime ruleset selection.
/// </summary>
public enum ContentSource
{
    Classic,
    WrathOfTheLichKing,
    Midnight
}

public enum ContentCategory
{
    TerrainTile,
    WorldModel,
    CharacterModel,
    Skeleton,
    Animation,
    Texture,
    Creature,
    Item,
    Spell,
    Talent,
    Quest,
    UiDefinition
}
