using CalculatriceMaui.Models;
using CalculatriceMaui.PageModels;

namespace CalculatriceMaui.Pages
{
    public partial class MainPage : ContentPage
    {
        public MainPage(MainPageModel model)
        {
            InitializeComponent();
            BindingContext = model;
        }
    }
}