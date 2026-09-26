using System.ComponentModel;
using System.IO;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Services;

namespace Schedule1ModdingTool.Views;

public partial class GlbPreviewWindow : Window
{
    private readonly string _modelPath;
    private readonly ItemBlueprint? _item;
    private readonly string _previewDirectory = Path.Combine(Path.GetTempPath(), "Schedule1ModdingTool", "GlbPreview", Guid.NewGuid().ToString("N"));
    private bool _closed;

    public static void ShowModel(Window? owner, QuestProject project, string relativePath, ItemBlueprint? item = null)
    {
        try
        {
            var projectDirectory = Path.GetDirectoryName(project.FilePath);
            if (string.IsNullOrWhiteSpace(projectDirectory)) throw new InvalidOperationException("Save the project before previewing models.");
            var path = Path.GetFullPath(Path.Combine(projectDirectory, relativePath));
            var modelRoot = Path.GetFullPath(Path.Combine(projectDirectory, "Models")) + Path.DirectorySeparatorChar;
            if (!path.StartsWith(modelRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The model must be inside this project's Models folder.");
            new GlbPreviewWindow(path, item) { Owner = owner }.ShowDialog();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Model preview", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    public GlbPreviewWindow(string modelPath, ItemBlueprint? item = null)
    {
        InitializeComponent();
        _modelPath = modelPath;
        _item = item;
        DataContext = item;
        ModelName.Text = Path.GetFileName(modelPath);
        PlacementPanel.Visibility = item == null ? Visibility.Collapsed : Visibility.Visible;
        if (item != null) item.PropertyChanged += ItemChanged;
        Loaded += InitializePreview;
        Closed += ClosePreview;
    }

    private async void InitializePreview(object sender, RoutedEventArgs e)
    {
        Loaded -= InitializePreview;
        try
        {
            GlbValidationService.ValidateFile(_modelPath);
            Directory.CreateDirectory(_previewDirectory);
            var assets = Path.Combine(AppContext.BaseDirectory, "ModelPreview");
            foreach (var name in new[] { "index.html", "viewer.js" })
                File.Copy(Path.Combine(assets, name), Path.Combine(_previewDirectory, name));
            File.Copy(_modelPath, Path.Combine(_previewDirectory, "model.glb"));
            var userData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Schedule1ModdingTool", "WebView2");
            var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: userData);
            if (_closed) return;
            await Viewer.EnsureCoreWebView2Async(environment);
            if (_closed) return;
            var core = Viewer.CoreWebView2;
            core.Settings.AreDefaultContextMenusEnabled = false;
            core.Settings.AreDevToolsEnabled = false;
            core.Settings.IsWebMessageEnabled = true;
            core.SetVirtualHostNameToFolderMapping("model-preview.invalid", _previewDirectory, CoreWebView2HostResourceAccessKind.DenyCors);
            core.NavigationStarting += (_, args) => args.Cancel = !IsPreviewUri(args.Uri);
            core.NewWindowRequested += (_, args) => args.Handled = true;
            core.PermissionRequested += (_, args) => args.State = CoreWebView2PermissionState.Deny;
            core.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
            core.WebResourceRequested += (_, args) =>
            {
                if (!IsPreviewUri(args.Request.Uri) && !args.Request.Uri.StartsWith("blob:", StringComparison.Ordinal))
                    args.Response = environment.CreateWebResourceResponse(null, 403, "External resources are disabled", "");
            };
            core.WebMessageReceived += (_, args) =>
            {
                if (IsPreviewUri(args.Source)) SendTransform();
            };
            core.Navigate("https://model-preview.invalid/index.html");
        }
        catch (Exception ex)
        {
            if (_closed) return;
            Viewer.Visibility = Visibility.Collapsed;
            ErrorMessage.Visibility = Visibility.Visible;
            ErrorMessage.Text = $"The model preview could not start. {ex.Message}\n\nThe preview requires Microsoft Edge WebView2 Runtime. Import and export remain available without it.";
        }
    }

    private static bool IsPreviewUri(string uri) => Uri.TryCreate(uri, UriKind.Absolute, out var parsed) &&
        parsed.Scheme == "https" && parsed.Host == "model-preview.invalid";

    private void ItemChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName?.StartsWith("Model", StringComparison.Ordinal) == true) SendTransform();
    }

    private void SendTransform()
    {
        if (_closed || _item == null || Viewer.CoreWebView2 == null) return;
        var values = new[] { _item.ModelScale, _item.ModelRotationX, _item.ModelRotationY, _item.ModelRotationZ,
            _item.ModelOffsetX, _item.ModelOffsetY, _item.ModelOffsetZ };
        if (values.Any(value => !float.IsFinite(value))) return;
        Viewer.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(new
        {
            scale = _item.ModelScale, rx = _item.ModelRotationX, ry = _item.ModelRotationY, rz = _item.ModelRotationZ,
            x = _item.ModelOffsetX, y = _item.ModelOffsetY, z = _item.ModelOffsetZ
        }));
    }

    private void ClosePreview(object? sender, EventArgs e)
    {
        _closed = true;
        if (_item != null) _item.PropertyChanged -= ItemChanged;
        Viewer.Dispose();
        try { if (Directory.Exists(_previewDirectory)) Directory.Delete(_previewDirectory, recursive: true); }
        catch (IOException) { /* WebView2 may briefly retain a handle after disposal. */ }
        catch (UnauthorizedAccessException) { }
    }
}
