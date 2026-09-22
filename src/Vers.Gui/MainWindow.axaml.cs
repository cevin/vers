using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Selection;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Vers.Core;
using Vers.Platform;

namespace Vers.Gui;

public sealed partial class MainWindow : Window
{
    private readonly AppContext _context;
    private readonly ObservableCollection<EnvironmentRow> _environmentRows = [];
    private readonly ObservableCollection<ProjectRow> _projectRows = [];
    private string? _selectedGroupName;
    private string? _selectedVersionName;
    private Vers.Platform.PathStatus _pathStatus;
    private bool _loading;

    public MainWindow()
        : this(BootstrapService.Initialize(
            Environment.CurrentDirectory,
            Vers.Platform.PlatformAdapterFactory.Create()))
    {
    }

    public MainWindow(AppContext context)
    {
        _context = context;
        _pathStatus = context.PathStatus;
        LocalizationService.SetCulture(
            context.Settings.UiCulture ?? LocalizationService.DetectSystemCulture());
        InitializeComponent();

        EnvironmentGrid.ItemsSource = _environmentRows;
        ProjectsGrid.ItemsSource = _projectRows;
        LanguageCombo.ItemsSource = LocalizationService.Languages;
        LanguageCombo.SelectedItem = LocalizationService.Languages.First(language =>
            language.CultureName == LocalizationService.CurrentCulture);

        ApplyLocalizedText();
        RefreshGroups();
        UpdateEditorState();
    }

    private async void OnAddGroupClick(object? sender, RoutedEventArgs eventArgs)
    {
        var name = await new PromptWindow(
            LocalizationService.Get("NewGroup"),
            LocalizationService.Get("GroupName")).ShowDialog<string?>(this);
        if (name is null)
        {
            return;
        }

        try
        {
            SettingsValidator.ValidateGroupName(name);
            if (_context.Settings.Groups.ContainsKey(name))
            {
                throw new InvalidDataException($"Group '{name}' already exists.");
            }

            FlushEditors();
            _context.Settings.Groups.Add(name, new ToolGroup());
            SettingsStore.Save(_context.SettingsPath, _context.Settings);
            BootstrapService.EnsureProxy(_context, name);
            RefreshGroups(name);
            SetStatus(BootstrapService.GetProxyPath(_context, name));
        }
        catch (Exception exception)
        {
            await ShowErrorAsync(exception.Message);
        }
    }

    private async void OnDeleteGroupClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (_selectedGroupName is null)
        {
            return;
        }

        var groupName = _selectedGroupName;
        var confirmed = await new MessageWindow(
            LocalizationService.Get("Confirm"),
            string.Format(LocalizationService.Get("ConfirmDeleteGroup"), groupName),
            showCancel: true).ShowDialog<bool>(this);
        if (!confirmed)
        {
            return;
        }

        try
        {
            _context.Settings.Groups.Remove(groupName);
            SettingsStore.Save(_context.SettingsPath, _context.Settings);
            BootstrapService.DeleteProxy(_context, groupName);
            _selectedGroupName = null;
            _selectedVersionName = null;
            RefreshGroups();
            SetStatus(LocalizationService.Get("Saved"));
        }
        catch (Exception exception)
        {
            await ShowErrorAsync(exception.Message);
        }
    }

    private async void OnAddVersionClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (!TryGetSelectedGroup(out var group))
        {
            return;
        }

        var name = await new PromptWindow(
            LocalizationService.Get("NewVersion"),
            LocalizationService.Get("VersionName")).ShowDialog<string?>(this);
        if (name is null)
        {
            return;
        }

        try
        {
            FlushEditors();
            if (group.Versions.ContainsKey(name))
            {
                throw new InvalidDataException($"Version '{name}' already exists.");
            }

            group.Versions.Add(name, new ToolVersion { Executable = " " });
            if (group.DefaultVersion is null)
            {
                group.DefaultVersion = name;
            }

            RefreshVersions(name);
            ExecutableTextBox.Text = string.Empty;
        }
        catch (Exception exception)
        {
            await ShowErrorAsync(exception.Message);
        }
    }

    private async void OnDeleteVersionClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (!TryGetSelectedGroup(out var group) || _selectedVersionName is null)
        {
            return;
        }

        var versionName = _selectedVersionName;
        var confirmed = await new MessageWindow(
            LocalizationService.Get("Confirm"),
            string.Format(LocalizationService.Get("ConfirmDeleteVersion"), versionName),
            showCancel: true).ShowDialog<bool>(this);
        if (!confirmed)
        {
            return;
        }

        group.Versions.Remove(versionName);
        foreach (var path in group.Projects.Where(pair =>
                     pair.Value.Equals(versionName, StringComparison.OrdinalIgnoreCase)).Select(pair => pair.Key).ToArray())
        {
            group.Projects.Remove(path);
        }

        if (string.Equals(group.DefaultVersion, versionName, StringComparison.OrdinalIgnoreCase))
        {
            group.DefaultVersion = group.Versions.Keys.FirstOrDefault();
        }

        _selectedVersionName = null;
        LoadProjects(group);
        RefreshVersions();
    }

    private async void OnBrowseExecutableClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (_selectedVersionName is null)
        {
            return;
        }

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            Title = LocalizationService.Get("Executable")
        });
        if (files.Count > 0)
        {
            ExecutableTextBox.Text = files[0].TryGetLocalPath() ?? files[0].Path.LocalPath;
        }
    }

    private void OnDefaultVersionClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (TryGetSelectedGroup(out var group) &&
            _selectedVersionName is not null &&
            DefaultVersionCheckBox.IsChecked == true)
        {
            group.DefaultVersion = _selectedVersionName;
        }
    }

    private void OnAddEnvironmentClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (_selectedVersionName is null)
        {
            return;
        }

        var suggestedName = EnvironmentSuggestionBox.Text?.Trim() ?? string.Empty;
        if (suggestedName.Length > 0 && _environmentRows.Any(row =>
                row.Name.Equals(suggestedName, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        var row = new EnvironmentRow { Name = suggestedName };
        _environmentRows.Add(row);
        EnvironmentGrid.SelectedItem = row;
        EnvironmentSuggestionBox.Text = string.Empty;
    }

    private void OnDeleteEnvironmentClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (EnvironmentGrid.SelectedItem is EnvironmentRow row)
        {
            _environmentRows.Remove(row);
        }
    }

    private async void OnAddProjectClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (_selectedVersionName is null)
        {
            return;
        }

        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            AllowMultiple = false,
            Title = LocalizationService.Get("ProjectPath")
        });
        if (folders.Count == 0)
        {
            return;
        }

        var path = folders[0].TryGetLocalPath() ?? folders[0].Path.LocalPath;
        var existing = _projectRows.FirstOrDefault(row =>
            row.Path.Equals(path, OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal));
        if (existing is not null)
        {
            existing.Version = _selectedVersionName;
            ProjectsGrid.ItemsSource = null;
            ProjectsGrid.ItemsSource = _projectRows;
            return;
        }

        var row = new ProjectRow { Path = path, Version = _selectedVersionName };
        _projectRows.Add(row);
        ProjectsGrid.SelectedItem = row;
    }

    private void OnDeleteProjectClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (ProjectsGrid.SelectedItem is ProjectRow row)
        {
            _projectRows.Remove(row);
        }
    }

    private async void OnSaveClick(object? sender, RoutedEventArgs eventArgs)
    {
        try
        {
            FlushEditors();
            SettingsStore.Save(_context.SettingsPath, _context.Settings);
            foreach (var groupName in _context.Settings.Groups.Keys)
            {
                BootstrapService.EnsureProxy(_context, groupName);
            }

            RefreshGroupPathStatuses();
            SetStatus(LocalizationService.Get("Saved"));
        }
        catch (Exception exception)
        {
            await ShowErrorAsync(exception.Message);
        }
    }

    private async void OnConfigurePathClick(object? sender, RoutedEventArgs eventArgs)
    {
        try
        {
            _pathStatus = _context.PlatformAdapter.EnsureBinOnPath(_context.BinDirectory);
            UpdatePathStatus();
            RefreshGroupPathStatuses();
        }
        catch (UnauthorizedAccessException) when (OperatingSystem.IsWindows() && !Program.IsElevatedRelaunch)
        {
            if (App.TryRelaunchElevated(setPath: true))
            {
                Close();
                return;
            }

            await ShowErrorAsync(LocalizationService.Get("PathElevationCancelled"));
        }
        catch (Exception exception)
        {
            await ShowErrorAsync(exception.Message);
        }
    }

    private void OnGroupSelectionChanged(object? sender, SelectionChangedEventArgs eventArgs)
    {
        if (_loading)
        {
            return;
        }

        FlushEditors();
        _selectedGroupName = (GroupList.SelectedItem as GroupListItem)?.Name;
        _selectedVersionName = null;
        if (TryGetSelectedGroup(out var group))
        {
            LoadProjects(group);
            RefreshVersions();
        }
        else
        {
            _projectRows.Clear();
            VersionList.ItemsSource = null;
        }

        EnvironmentSuggestionBox.ItemsSource = EnvironmentSuggestionCatalog.ForGroup(_selectedGroupName);
        UpdateEditorState();
    }

    private void OnVersionSelectionChanged(object? sender, SelectionChangedEventArgs eventArgs)
    {
        if (_loading)
        {
            return;
        }

        FlushVersionEditor();
        _selectedVersionName = VersionList.SelectedItem as string;
        LoadVersionEditor();
        UpdateEditorState();
    }

    private void OnLanguageChanged(object? sender, SelectionChangedEventArgs eventArgs)
    {
        if (_loading || LanguageCombo.SelectedItem is not LanguageOption language)
        {
            return;
        }

        LocalizationService.SetCulture(language.CultureName);
        _context.Settings.UiCulture = language.CultureName;
        SettingsStore.Save(_context.SettingsPath, _context.Settings);
        ApplyLocalizedText();
        RefreshGroupPathStatuses();
    }

    private void RefreshGroups(string? select = null)
    {
        _loading = true;
        try
        {
            var groups = _context.Settings.Groups.Keys.Order(StringComparer.OrdinalIgnoreCase).ToArray();
            var groupItems = CreateGroupListItems(groups);
            GroupList.ItemsSource = groupItems;
            GroupList.SelectedItem = groupItems.FirstOrDefault(item =>
                item.Name.Equals(select ?? groups.FirstOrDefault(), StringComparison.OrdinalIgnoreCase));
            _selectedGroupName = (GroupList.SelectedItem as GroupListItem)?.Name;
            _selectedVersionName = null;
            if (TryGetSelectedGroup(out var group))
            {
                LoadProjects(group);
                RefreshVersions();
            }
            else
            {
                VersionList.ItemsSource = null;
                _projectRows.Clear();
            }

            EnvironmentSuggestionBox.ItemsSource = EnvironmentSuggestionCatalog.ForGroup(_selectedGroupName);
        }
        finally
        {
            _loading = false;
            UpdateEditorState();
        }
    }

    private void RefreshVersions(string? select = null)
    {
        _loading = true;
        try
        {
            if (!TryGetSelectedGroup(out var group))
            {
                VersionList.ItemsSource = null;
                return;
            }

            var versions = group.Versions.Keys.Order(StringComparer.OrdinalIgnoreCase).ToArray();
            VersionList.ItemsSource = versions;
            VersionList.SelectedItem = select ?? versions.FirstOrDefault();
            _selectedVersionName = VersionList.SelectedItem as string;
            LoadVersionEditor();
        }
        finally
        {
            _loading = false;
            UpdateEditorState();
        }
    }

    private void LoadVersionEditor()
    {
        _environmentRows.Clear();
        if (!TryGetSelectedVersion(out var group, out var version))
        {
            ExecutableTextBox.Text = string.Empty;
            DefaultVersionCheckBox.IsChecked = false;
            return;
        }

        ExecutableTextBox.Text = version.Executable.Trim();
        DefaultVersionCheckBox.IsChecked = string.Equals(
            group.DefaultVersion,
            _selectedVersionName,
            StringComparison.OrdinalIgnoreCase);
        foreach (var (name, value) in version.Environment.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            _environmentRows.Add(new EnvironmentRow { Name = name, Value = value });
        }

        UpdateOverrideHint();
    }

    private void LoadProjects(ToolGroup group)
    {
        _projectRows.Clear();
        foreach (var (path, version) in group.Projects.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            _projectRows.Add(new ProjectRow { Path = path, Version = version });
        }
    }

    private void FlushEditors()
    {
        FlushVersionEditor();
        if (TryGetSelectedGroup(out var group))
        {
            group.Projects = _projectRows
                .Where(row => !string.IsNullOrWhiteSpace(row.Path))
                .ToDictionary(row => row.Path.Trim(), row => row.Version.Trim(), StringComparer.OrdinalIgnoreCase);
        }
    }

    private void FlushVersionEditor()
    {
        if (!TryGetSelectedVersion(out var group, out var version))
        {
            return;
        }

        version.Executable = ExecutableTextBox.Text?.Trim() ?? string.Empty;
        version.Environment = _environmentRows
            .Where(row => !string.IsNullOrWhiteSpace(row.Name))
            .ToDictionary(row => row.Name.Trim(), row => row.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase);
        if (DefaultVersionCheckBox.IsChecked == true)
        {
            group.DefaultVersion = _selectedVersionName;
        }
    }

    private bool TryGetSelectedGroup(out ToolGroup group)
    {
        if (_selectedGroupName is not null &&
            _context.Settings.Groups.TryGetValue(_selectedGroupName, out var found))
        {
            group = found;
            return true;
        }

        group = null!;
        return false;
    }

    private bool TryGetSelectedVersion(out ToolGroup group, out ToolVersion version)
    {
        if (TryGetSelectedGroup(out group) &&
            _selectedVersionName is not null &&
            group.Versions.TryGetValue(_selectedVersionName, out var found))
        {
            version = found;
            return true;
        }

        version = null!;
        return false;
    }

    private void UpdateEditorState()
    {
        var hasGroup = _selectedGroupName is not null;
        var hasVersion = _selectedVersionName is not null;
        DeleteGroupButton.IsEnabled = hasGroup;
        AddVersionButton.IsEnabled = hasGroup;
        DeleteVersionButton.IsEnabled = hasVersion;
        VersionEditor.IsEnabled = hasVersion;
        AddProjectButton.IsEnabled = hasVersion;
        DeleteProjectButton.IsEnabled = hasGroup;
        SaveButton.IsEnabled = hasGroup;
        if (!hasGroup)
        {
            SetStatus(LocalizationService.Get("SelectGroup"));
        }
        else if (!hasVersion)
        {
            SetStatus(LocalizationService.Get("SelectVersion"));
        }
    }

    private void ApplyLocalizedText()
    {
        Title = LocalizationService.Get("AppTitle");
        LanguageLabel.Text = LocalizationService.Get("Language");
        GroupsLabel.Text = LocalizationService.Get("Groups");
        AddGroupButton.Content = LocalizationService.Get("Add");
        DeleteGroupButton.Content = LocalizationService.Get("Delete");
        VersionsTab.Header = LocalizationService.Get("Versions");
        ProjectsTab.Header = LocalizationService.Get("Projects");
        AddVersionButton.Content = LocalizationService.Get("Add");
        DeleteVersionButton.Content = LocalizationService.Get("Delete");
        ExecutableLabel.Text = LocalizationService.Get("Executable");
        BrowseExecutableButton.Content = LocalizationService.Get("Browse");
        DefaultVersionCheckBox.Content = LocalizationService.Get("DefaultVersion");
        EnvironmentLabel.Text = LocalizationService.Get("EnvironmentVariables");
        EnvironmentHintText.Text = LocalizationService.Get("EnvironmentHint");
        AddEnvironmentButton.Content = LocalizationService.Get("Add");
        DeleteEnvironmentButton.Content = LocalizationService.Get("Delete");
        AddProjectButton.Content = LocalizationService.Get("Add");
        DeleteProjectButton.Content = LocalizationService.Get("Delete");
        SaveButton.Content = LocalizationService.Get("Save");
        UpdatePathStatus();
        EnvironmentSuggestionBox.PlaceholderText = LocalizationService.Get("VariableName");
        RebuildGridColumns();
        UpdateOverrideHint();
    }

    private void RebuildGridColumns()
    {
        EnvironmentGrid.Columns.Clear();
        EnvironmentGrid.Columns.Add(new DataGridTextColumn
        {
            Header = LocalizationService.Get("VariableName"),
            Binding = new Binding(nameof(EnvironmentRow.Name)),
            Width = new DataGridLength(0.38, DataGridLengthUnitType.Star)
        });
        EnvironmentGrid.Columns.Add(new DataGridTextColumn
        {
            Header = LocalizationService.Get("Value"),
            Binding = new Binding(nameof(EnvironmentRow.Value)),
            Width = new DataGridLength(0.62, DataGridLengthUnitType.Star)
        });
        ProjectsGrid.Columns.Clear();
        ProjectsGrid.Columns.Add(new DataGridTextColumn
        {
            Header = LocalizationService.Get("ProjectPath"),
            Binding = new Binding(nameof(ProjectRow.Path)),
            Width = new DataGridLength(0.72, DataGridLengthUnitType.Star)
        });
        ProjectsGrid.Columns.Add(new DataGridTextColumn
        {
            Header = LocalizationService.Get("Version"),
            Binding = new Binding(nameof(ProjectRow.Version)),
            Width = new DataGridLength(0.28, DataGridLengthUnitType.Star)
        });
    }

    private void UpdateOverrideHint()
    {
        var group = _selectedGroupName?.Select(character =>
            char.IsLetterOrDigit(character) ? char.ToUpperInvariant(character) : '_').ToArray();
        OverrideHintText.Text = group is null
            ? string.Empty
            : string.Format(LocalizationService.Get("OverrideHint"), new string(group));
    }

    private void UpdatePathStatus()
    {
        ConfigurePathButton.Content = _pathStatus.IsConfigured
            ? LocalizationService.Get("PathReady")
            : LocalizationService.Get("AddToPath");
        ConfigurePathButton.IsEnabled = !_pathStatus.IsConfigured;
        ToolTip.SetTip(
            ConfigurePathButton,
            LocalizationService.Get(_pathStatus.IsConfigured ? "PathReadyHint" : "PathActionHint"));
    }

    private GroupListItem[] CreateGroupListItems(IReadOnlyList<string> groupNames)
    {
        var priority = _context.PlatformAdapter.GetCommandPriorityStatus(
            _context.BinDirectory,
            groupNames);
        return priority.Probes.Select(probe => probe.State switch
        {
            CommandPathState.Active => new GroupListItem(probe.CommandName, false, null),
            CommandPathState.Conflict => new GroupListItem(
                probe.CommandName,
                true,
                string.Format(
                    LocalizationService.Get("CommandPathConflict"),
                    probe.ResolvedPath,
                    probe.ExpectedPath)),
            CommandPathState.Missing => new GroupListItem(
                probe.CommandName,
                true,
                string.Format(LocalizationService.Get("CommandPathMissing"), probe.ExpectedPath)),
            _ => new GroupListItem(
                probe.CommandName,
                true,
                string.Format(LocalizationService.Get("CommandProxyMissing"), probe.ExpectedPath))
        }).ToArray();
    }

    private void RefreshGroupPathStatuses()
    {
        var selectedName = _selectedGroupName;
        var groupNames = _context.Settings.Groups.Keys.Order(StringComparer.OrdinalIgnoreCase).ToArray();
        var items = CreateGroupListItems(groupNames);
        _loading = true;
        try
        {
            GroupList.ItemsSource = items;
            GroupList.SelectedItem = items.FirstOrDefault(item =>
                item.Name.Equals(selectedName, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            _loading = false;
        }
    }

    private void SetStatus(string text) => StatusText.Text = text;

    private async Task ShowErrorAsync(string message) =>
        await new MessageWindow(LocalizationService.Get("Error"), message, showCancel: false)
            .ShowDialog<bool>(this);
}
