using MemoryPack;

namespace BFWikiScrapper.Model;

[MemoryPackable]
public readonly partial record struct LevelData(int Level, long XpRequired);