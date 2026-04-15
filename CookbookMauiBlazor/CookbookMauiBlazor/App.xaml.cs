namespace CookbookMauiBlazor
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
#if ANDROID
            Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.Application
                .SetWindowSoftInputModeAdjust(this, Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.WindowSoftInputModeAdjust.Resize);
#endif
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new MainPage()) { Title = "CookbookMauiBlazor" };
        }
    }
}
