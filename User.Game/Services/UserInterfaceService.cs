using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Entities;
using User.Game.Actors.UserInterface;

namespace User.Game.Services;

public partial class UserInterfaceService : Service
{
    [Component] private ExperienceBarWidget _experienceBar;
    [Component] private PerkSelectorWidget _perkSelector;
    
}
