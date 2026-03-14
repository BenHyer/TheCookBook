using Microsoft.AspNetCore.Components;

namespace Cookbook.Shared.Boards;

public partial class BoardDetails
{
    [Parameter]
    public BoardViewModel? Board { get; set; }
}
