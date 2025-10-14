using MemoryPack;

namespace BFWikiScrapper.Model;

[MemoryPackable]
public partial record struct UnitData(string UnitId, string Name, string Rarity, string UnitDataId, string ImageUrl);