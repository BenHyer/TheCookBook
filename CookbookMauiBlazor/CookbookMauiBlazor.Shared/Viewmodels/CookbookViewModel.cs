using CommunityToolkit.Mvvm.ComponentModel;

namespace CookbookMauiBlazor.Shared.Viewmodels
{
    public partial class CookbookViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool showNewRecipeForm;

        public void ShowAddRecipeForm()
        {
            ShowNewRecipeForm = true;
        }

        public void HideAddRecipeForm()
        {
            ShowNewRecipeForm = false;
        }
    }
}
