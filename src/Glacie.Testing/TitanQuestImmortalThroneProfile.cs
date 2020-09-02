using System.Collections.Generic;

namespace Glacie.Testing
{
    internal sealed class TitanQuestImmortalThroneProfile
    {
        // TODO: (Glacie.Testing) Split profiles into TQ and TQ/IT.

        private static readonly string[] _knownArcFiles =
        {
            "Audio/Dialog.arc",
            "Audio/Music.arc",
            "Audio/Sounds.arc",
            "Resources/Creatures.arc",
            "Resources/Effects.arc",
            "Resources/Fonts.arc",
            "Resources/InGameUI.arc",
            "Resources/Items.arc",
            "Resources/Levels.arc",
            "Resources/Lights.arc",
            "Resources/Menu.arc",
            "Resources/OutGameElements.arc",
            "Resources/Particles.arc",
            "Resources/Quests.arc",
            "Resources/SceneryBabylon.arc",
            "Resources/SceneryEgypt.arc",
            "Resources/SceneryGreece.arc",
            "Resources/SceneryOlympus.arc",
            "Resources/SceneryOrient.arc",
            "Resources/Shaders.arc",
            "Resources/System.arc",
            "Resources/TerrainTextures.arc",
            "Resources/UI.arc",
            "Resources/Underground.arc",
            "Text/Text_EN.arc",
            "Immortal Throne/Resources/Creatures.arc",
            "Immortal Throne/Resources/Dialog.arc",
            "Immortal Throne/Resources/Effects.arc",
            "Immortal Throne/Resources/InGameUI.arc",
            "Immortal Throne/Resources/Items.arc",
            "Immortal Throne/Resources/Levels.arc",
            "Immortal Throne/Resources/LMesh.arc",
            "Immortal Throne/Resources/LSounds.arc",
            "Immortal Throne/Resources/LTex.arc",
            "Immortal Throne/Resources/Menu.arc",
            "Immortal Throne/Resources/Music.arc",
            "Immortal Throne/Resources/OutGameElements.arc",
            "Immortal Throne/Resources/Shaders.arc",
            "Immortal Throne/Resources/Sounds.arc",
            "Immortal Throne/Resources/System.arc",
            "Immortal Throne/Resources/Text_EN.arc",
            "Immortal Throne/Resources/Underground.arc",
            "Immortal Throne/Resources/XPack/Allskins.arc",
            "Immortal Throne/Resources/XPack/CPF_Effects.arc",
            "Immortal Throne/Resources/XPack/CPF_Textures.arc",
            "Immortal Throne/Resources/XPack/Creatures.arc",
            "Immortal Throne/Resources/XPack/Dialog.arc",
            "Immortal Throne/Resources/XPack/Effects.arc",
            "Immortal Throne/Resources/XPack/Items.arc",
            "Immortal Throne/Resources/XPack/Menu.arc",
            "Immortal Throne/Resources/XPack/Quests.arc",
            "Immortal Throne/Resources/XPack/SceneryHades.arc",
            "Immortal Throne/Resources/XPack/SceneryMedit.arc",
            "Immortal Throne/Resources/XPack/SceneryUnderground.arc",
            "Immortal Throne/Resources/XPack/Shaders.arc",
            "Immortal Throne/Resources/XPack/Skill Icons.arc",
            "Immortal Throne/Resources/XPack/System.arc",
            "Immortal Throne/Resources/XPack/TerrainTextures.arc",
            "Immortal Throne/Resources/XPack/UI.arc",
        };

        public IReadOnlyCollection<string> GetKnownArcFiles() => _knownArcFiles;
    }
}
