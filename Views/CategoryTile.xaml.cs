using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Schedule1ModdingTool.Models;

namespace Schedule1ModdingTool.Views
{
    /// <summary>
    /// Interaction logic for CategoryTile.xaml
    /// </summary>
    public partial class CategoryTile : UserControl
    {
        public static readonly DependencyProperty CategoryProperty =
            DependencyProperty.Register("Category", typeof(ModCategoryInfo), typeof(CategoryTile), new PropertyMetadata(null));

        public ModCategoryInfo Category
        {
            get { return (ModCategoryInfo)GetValue(CategoryProperty); }
            set { SetValue(CategoryProperty, value); }
        }

        public CategoryTile()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Category?.IsEnabled == true)
            {
                var mainWindow = Window.GetWindow(this);
                if (mainWindow?.DataContext is ViewModels.MainViewModel vm)
                {
                    vm.SelectCategoryCommand?.Execute(Category.Category);
                }
            }
        }

        private void Border_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Right-click context menu will be handled by parent workspace
            e.Handled = false;
        }
    }
}

