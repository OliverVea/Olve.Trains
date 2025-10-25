using Olve.Utilities.Ids;

namespace Olve.Engine3D.GUI.Elements;

public record GuiElementArgs(Id<GuiElement> ElementId, Id<GuiElementRegistrations> RegistrationId, Id<GuiNode> NodeId);