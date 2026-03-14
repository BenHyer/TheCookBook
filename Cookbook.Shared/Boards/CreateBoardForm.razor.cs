using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;

namespace Cookbook.Shared.Boards;

public partial class CreateBoardForm
{
    [Inject]
    public required IBoardService BoardService { get; set; }

    [Parameter]
    [EditorRequired]
    public string OwnerUserId { get; set; } = string.Empty;

    [Parameter]
    public EventCallback<BoardViewModel> BoardCreated { get; set; }

    private readonly CreateBoardFormModel _model = new();
    private bool _isSubmitting;
    private string? _errorMessage;

    private async Task HandleValidSubmitAsync()
    {
        if (_isSubmitting)
        {
            return;
        }

        _isSubmitting = true;
        _errorMessage = null;

        try
        {
            var createdBoard = await BoardService.CreateBoardAsync(
                _model.Name.Trim(),
                string.IsNullOrWhiteSpace(_model.Description) ? null : _model.Description.Trim(),
                OwnerUserId);

            _model.Name = string.Empty;
            _model.Description = null;

            if (BoardCreated.HasDelegate)
            {
                await BoardCreated.InvokeAsync(createdBoard);
            }
        }
        catch (Exception)
        {
            _errorMessage = "Unable to create the board. Please try again.";
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private sealed class CreateBoardFormModel
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Description { get; set; }
    }
}
