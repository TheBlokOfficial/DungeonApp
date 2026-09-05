using System.Collections.Generic;
using DungeonApp.Core.DataBlocks;

namespace DungeonApp.Core.Tools;

public interface ITool
{
    IReadOnlyList<DataBlockId> Uses { get; }
}
