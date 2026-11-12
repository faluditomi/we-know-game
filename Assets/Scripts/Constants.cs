using System.Collections.Generic;

public class Constants
{
    public static class AddressablePrefixes
    {
        public const string Prefabs = "prefabs/";
        public const string ScriptableObjects = "scriptable_objects/";
    }

    public static class AddressablePrefabs
    {
        public const string Tile = AddressablePrefixes.Prefabs + "tile";
    }

    public static class AddressableLabels
    {
        public const string Tile = "tile";
    }

    public static class GameObjectNames
    {
        public const string BoardRoot = "Board Root";
        public const string TileBody = "Body";
        public const string TileBackFace = "BackFaceLogo";
        public const string TileFrontFace = "FrontFace";
        public const string TileFrontFaceLogo = "FrontFaceLogo";
        public const string TileDescriptionText = "DescriptionText";
    }
}
