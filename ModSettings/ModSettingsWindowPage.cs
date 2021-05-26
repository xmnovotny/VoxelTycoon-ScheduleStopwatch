using VoxelTycoon.Game.UI;

namespace ModSettings
{
    public abstract class ModSettingsWindowPage : GameSettingsWindowPage
    {
        protected override bool HasFooter { get => false; }

        public void Initialize()
        {
            base.InitializePage();
        }
    }
}
