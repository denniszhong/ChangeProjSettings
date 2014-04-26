
namespace ChangeVcxproj.Modifier
{
    public abstract class ModifierFactory
    {
        public abstract Modifier CreateModifier();
    }

    public class VcxporjModifierFactory : ModifierFactory
    {
        public override Modifier CreateModifier()
        {
            return new VcxprojModifier();
        }
    }

    public class CsprojModifierFactory : ModifierFactory
    {
        public override Modifier CreateModifier()
        {
            return new CsprojModifier();
        }
    }
}
