using YukkuriMovieMaker.Plugin.Shape;
using YukkuriMovieMaker.Project;

namespace MineSkinShape
{
    public class MineSkinShapePlugin : IShapePlugin
    {
        public string Name => "Minecraftスキン";

        public bool IsExoShapeSupported => false;

        public bool IsExoMaskSupported => false;

        public IShapeParameter CreateShapeParameter(SharedDataStore? sharedData)
        {
            return new MineSkinShapeParameter(sharedData);
        }
    }
}
