using System.ComponentModel;
using System.IO;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Services;

namespace Schedule1ModdingTool.Views;

public partial class GlbSceneEditorWindow : Window
{
    private const string Host = "scene-editor.invalid";
    private readonly QuestProject _project;
    private readonly string? _addModelId;
    private readonly string _previewDirectory = Path.Combine(Path.GetTempPath(), "Schedule1ModdingTool", "SceneEditor", Guid.NewGuid().ToString("N"));
    private readonly Dictionary<string, string> _modelCopies = new(StringComparer.Ordinal);
    private MemoryStream? _exportBuffer;
    private string? _exportId;
    private int _exportExpected;
    private bool _dirty;
    private bool _closed;
    private bool _savingToClose;
    private bool _allowClose;

    public static void ShowEditor(Window? owner, QuestProject project, ModelAsset? addModel = null)
    {
        if (string.IsNullOrWhiteSpace(project.FilePath))
        {
            MessageBox.Show(owner, "Save the project before editing a 3D scene.", "3D Scene Editor", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        new GlbSceneEditorWindow(project, addModel?.Id) { Owner = owner }.ShowDialog();
    }

    public GlbSceneEditorWindow(QuestProject project, string? addModelId)
    {
        InitializeComponent();
        _project = project;
        _addModelId = addModelId;
        Loaded += Initialize;
        Closing += OnClosing;
        Closed += OnClosed;
    }

    private async void Initialize(object sender, RoutedEventArgs e)
    {
        Loaded -= Initialize;
        try
        {
            Directory.CreateDirectory(_previewDirectory);
            var assets = Path.Combine(AppContext.BaseDirectory, "ModelPreview");
            File.Copy(Path.Combine(assets, "scene-editor.html"), Path.Combine(_previewDirectory, "scene-editor.html"));
            File.Copy(Path.Combine(assets, "scene-editor.js"), Path.Combine(_previewDirectory, "scene-editor.js"));
            var userData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Schedule1ModdingTool", "WebView2");
            var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: userData);
            if (_closed) return;
            await Editor.EnsureCoreWebView2Async(environment);
            if (_closed) return;
            var core = Editor.CoreWebView2;
            core.Settings.AreDefaultContextMenusEnabled = false;
            core.Settings.AreDevToolsEnabled = false;
            core.Settings.IsWebMessageEnabled = true;
            core.SetVirtualHostNameToFolderMapping(Host, _previewDirectory, CoreWebView2HostResourceAccessKind.DenyCors);
            core.NavigationStarting += (_, args) => args.Cancel = !IsEditorUri(args.Uri);
            core.NewWindowRequested += (_, args) => args.Handled = true;
            core.PermissionRequested += (_, args) => args.State = CoreWebView2PermissionState.Deny;
            core.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
            core.WebResourceRequested += (_, args) =>
            {
                if (!IsEditorUri(args.Request.Uri) && !args.Request.Uri.StartsWith("blob:", StringComparison.Ordinal))
                    args.Response = environment.CreateWebResourceResponse(null, 403, "External resources are disabled", "");
            };
            core.WebMessageReceived += ReceiveMessage;
            core.Navigate($"https://{Host}/scene-editor.html");
        }
        catch (Exception ex)
        {
            if (_closed) return;
            Editor.Visibility = Visibility.Collapsed;
            ErrorMessage.Visibility = Visibility.Visible;
            ErrorMessage.Text = $"The 3D Scene Editor could not start: {ex.Message}";
        }
    }

    private static bool IsEditorUri(string uri) => Uri.TryCreate(uri, UriKind.Absolute, out var parsed) &&
        parsed.Scheme == "https" && parsed.Host == Host;

    private async void ReceiveMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        if (_closed || !IsEditorUri(args.Source)) return;
        JObject message;
        try { message = JObject.Parse(args.WebMessageAsJson); }
        catch (JsonException) { return; }
        var type = (string?)message["type"];
        if (type == "ready")
        {
            Editor.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(new
            {
                type = "initialize",
                models = _project.Models.Where(model => model.IsGlb).Select(model => new
                {
                    id = model.Id,
                    name = model.DisplayName
                }).ToArray(),
                objects = _project.ModelScene.Objects,
                addModelId = _addModelId
            }));
            return;
        }
        if (type == "dirtyChanged") { _dirty = (bool?)message["dirty"] == true; return; }
        if (type == "closeEditor" && _savingToClose)
        {
            _allowClose = true;
            Close();
            return;
        }
        if (type == "closeSaveFailed") { _savingToClose = false; return; }
        var token = (int?)message["token"];
        if (token is not > 0) return;
        try
        {
            object? extra = type switch
            {
                "requestModel" => await CopyModel((string?)message["modelId"]),
                "saveScene" => SaveScene(message["objects"]),
                "exportBegin" => BeginExport(message),
                "exportChunk" => AddExportChunk(message),
                "exportFinish" => FinishExport(message),
                _ => throw new InvalidOperationException("Unknown scene editor request.")
            };
            if (!_closed) Editor.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(new
            {
                type = "ack", token, error = (string?)null,
                url = extra is string url ? url : null,
                name = extra is ModelAsset asset ? asset.DisplayName : null,
                modelId = extra is ModelAsset exported ? exported.Id : null
            }));
        }
        catch (Exception ex)
        {
            if (!_closed) Editor.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(new { type = "ack", token, error = ex.Message }));
        }
    }

    private async Task<string> CopyModel(string? modelId)
    {
        var model = _project.Models.FirstOrDefault(asset => asset.Id == modelId && asset.IsGlb)
            ?? throw new InvalidOperationException("This GLB is no longer in the project library.");
        if (_modelCopies.TryGetValue(model.Id, out var existing)) return existing;
        var projectFolder = Path.GetDirectoryName(_project.FilePath)!;
        var modelRoot = Path.GetFullPath(Path.Combine(projectFolder, "Models")) + Path.DirectorySeparatorChar;
        var source = Path.GetFullPath(Path.Combine(projectFolder, model.RelativePath));
        if (!source.StartsWith(modelRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The GLB must be inside the project Models folder.");
        GlbValidationService.ValidateFile(source);
        var folder = Path.Combine(_previewDirectory, "models");
        Directory.CreateDirectory(folder);
        var name = Guid.NewGuid().ToString("N") + ".glb";
        await Task.Run(() => File.Copy(source, Path.Combine(folder, name)));
        var url = $"https://{Host}/models/{name}";
        _modelCopies[model.Id] = url;
        return url;
    }

    private object? SaveScene(JToken? data)
    {
        if (data is not JArray array || array.Count > 200)
            throw new InvalidDataException("A scene can contain at most 200 objects.");
        var objects = array.ToObject<List<ModelSceneObject>>() ?? throw new InvalidDataException("Invalid scene objects.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in objects)
        {
            if (string.IsNullOrWhiteSpace(item.Id) || !ids.Add(item.Id) || item.Id.Length > 80 ||
                string.IsNullOrWhiteSpace(item.Name) || item.Name.Length > 80)
                throw new InvalidDataException("Scene object IDs and names must be unique and nonempty.");
            if (item.Kind == "model")
            {
                if (!_project.Models.Any(model => model.IsGlb && model.Id == item.ModelId))
                    throw new InvalidDataException($"A GLB used by '{item.Name}' is missing from the model library.");
            }
            else if (item.Kind is not ("cube" or "sphere" or "cylinder" or "cone" or "plane"))
                throw new InvalidDataException($"Unsupported shape: {item.Kind}");
            if (item.Position?.Length != 3 || item.Rotation?.Length != 3 || item.Scale?.Length != 3 ||
                item.Position.Concat(item.Rotation).Any(value => !float.IsFinite(value) || Math.Abs(value) > 10000) ||
                item.Scale.Any(value => !float.IsFinite(value) || value <= 0 || value > 1000))
                throw new InvalidDataException($"Invalid transform on '{item.Name}'.");
            if (!System.Text.RegularExpressions.Regex.IsMatch(item.Color ?? "", "^#[0-9a-fA-F]{6}$"))
                throw new InvalidDataException($"Invalid color on '{item.Name}'.");
        }
        _project.ModelScene = new ModelSceneDocument { Objects = objects };
        _project.SaveToFile(_project.FilePath);
        _dirty = false;
        return null;
    }

    private object? BeginExport(JObject message)
    {
        var size = (int?)message["byteLength"] ?? 0;
        if (size <= 0 || size > GlbValidationService.MaximumFileBytes)
            throw new InvalidDataException("Exported GLB must be at most 64 MB.");
        _exportBuffer?.Dispose();
        _exportBuffer = new MemoryStream(size);
        _exportExpected = size;
        _exportId = (string?)message["exportId"];
        if (string.IsNullOrWhiteSpace(_exportId)) throw new InvalidDataException("Missing export ID.");
        return null;
    }

    private object? AddExportChunk(JObject message)
    {
        CheckExportId((string?)message["exportId"]);
        var chunk = Convert.FromBase64String((string?)message["data"] ?? string.Empty);
        if (chunk.Length == 0 || _exportBuffer!.Length + chunk.Length > _exportExpected)
            throw new InvalidDataException("Invalid GLB export chunk.");
        _exportBuffer.Write(chunk, 0, chunk.Length);
        return null;
    }

    private ModelAsset FinishExport(JObject message)
    {
        CheckExportId((string?)message["exportId"]);
        try
        {
            if (_exportBuffer!.Length != _exportExpected)
                throw new InvalidDataException("The GLB export was incomplete.");
            _exportBuffer.Position = 0;
            GlbValidationService.Validate(_exportBuffer);
            var projectFolder = Path.GetDirectoryName(_project.FilePath)!;
            var folder = Path.Combine(projectFolder, "Models");
            Directory.CreateDirectory(folder);
            var fileName = "SceneModel-" + Guid.NewGuid().ToString("N")[..8] + ".glb";
            var path = Path.Combine(folder, fileName);
            File.WriteAllBytes(path, _exportBuffer.ToArray());
            var asset = new ModelAsset
            {
                DisplayName = "Scene Model " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                RelativePath = Path.Combine("Models", fileName)
            };
            _project.AddModel(asset);
            _project.SaveToFile(_project.FilePath);
            return asset;
        }
        finally
        {
            _exportBuffer?.Dispose();
            _exportBuffer = null;
            _exportId = null;
            _exportExpected = 0;
        }
    }

    private void CheckExportId(string? value)
    {
        if (_exportBuffer == null || string.IsNullOrWhiteSpace(value) || value != _exportId)
            throw new InvalidDataException("The GLB export session is no longer active.");
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_allowClose || !_dirty) return;
        e.Cancel = true;
        if (_savingToClose) return;
        var answer = MessageBox.Show(this, "Save scene changes before closing?", "3D Scene Editor", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        if (answer == MessageBoxResult.No)
        {
            e.Cancel = false;
        }
        else if (answer == MessageBoxResult.Yes)
        {
            _savingToClose = true;
            Editor.CoreWebView2?.PostWebMessageAsJson("{\"type\":\"saveAndClose\"}");
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _closed = true;
        _exportBuffer?.Dispose();
        Editor.Dispose();
        try { if (Directory.Exists(_previewDirectory)) Directory.Delete(_previewDirectory, recursive: true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
