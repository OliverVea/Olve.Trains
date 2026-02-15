using MemoryPack;

namespace Olve.Engine3D.Commands;

[MemoryPackable]
public partial record CommandRequest(string Command);

[MemoryPackable]
public partial record CommandResponse(bool Success, string Output, string[] Errors);
