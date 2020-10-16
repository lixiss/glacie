using System;

namespace Glacie.Data.Resources.V1
{
    // Asset Types: Texture, Cube Map, Mesh, Animation, Shader, Bitmap, Font, TextFile, Trigger/Quest,
    //   Particle, Map, Binary, Wave, MP3.

    [Obsolete("ResourceType is domain type, so better to move into Glacie.Core.")]
    public enum ResourceType
    {
        None = 0,

        /// <summary>
        /// Template resource (.tpl).
        /// </summary>
        Template,

        //Texture,
        //CubeMap,
        //Mesh,
        //Animation,
        //Shader,
        //Bitmap,
        //Font,
        //TextFile,
        //Trigger_Quest,
        //Particle,
        //Map,
        //Binary,
        //AudioWave,
        //AudioMp3,
    }
}
