namespace Glacie.Abstractions
{
    // Asset Types: Texture, Cube Map, Mesh, Animation, Shader, Bitmap, Font, TextFile, Trigger/Quest,
    //   Particle, Map, Binary, Wave, MP3.

    public enum ResourceType
    {
        None = 0,

        /// <summary>
        /// Template resource (.tpl).
        /// </summary>
        Template,


        // Currently not supported types.
        //           //        GD     TQAE
        Unknown_ANM, // .anm   2797   3841
        Unknown_MSH, // .msh   8096   14066
        Unknown_TEX, // .tex   15579  21659
        Unknown_PFX, // .pfx   3271   1736
        Unknown_FNT, // .fnt   327    9
        Unknown_TGA, // .tga   -      4

        // Note, .txt files may appear anywhere, not neceserray there is to be a game data.
        // Sometimes it is just notes.
        Unknown_TXT, // .txt   23     2

        Unknown_MAP, // .map   2      1
        Unknown_MP3, // .mp3   -      5497
        Unknown_WAV, // .wav   6562   3114
        Unknown_QST, // .qst   391    225
        Unknown_BIN, // .bin   -      3
        Unknown_SSH, // .ssh   231    147

        // GD-specific resources
        Unknown_CNV, // .cnv   392    -
        Unknown_LUA, // .lua   207    -
        Unknown_OGG, // .ogg   130    -

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

        // Not a game resource, development only.
        GlacieMetadataModule,       // .gxm
        GlacieMetadata,             // .gxmd
        GlacieMetadataInclude,      // .gxmdi
        GlacieMetadataPatch,        // .gxmp
        GlacieMetadataPatchInclude, // .gxmpi
    }
}
