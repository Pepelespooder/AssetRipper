using AssetRipper.Assets.Bundles;
using AssetRipper.Export.Configuration;
using AssetRipper.Export.PrimaryContent;
using AssetRipper.Export.UnityProjects;
using AssetRipper.Import.Logging;
using AssetRipper.Import.Structure.Assembly.Managers;
using AssetRipper.IO.Files;
using AssetRipper.NativeDialogs;
using AssetRipper.Processing;

namespace AssetRipper.GUI.Web;

public static class GameFileLoader
{
	private static GameData? GameData { get; set; }
	[MemberNotNullWhen(true, nameof(GameData))]
	public static bool IsLoaded => GameData is not null;
	public static GameBundle GameBundle => GameData!.GameBundle;
	public static IAssemblyManager AssemblyManager => GameData!.AssemblyManager;
	public static FullConfiguration Settings { get; } = LoadSettings();
	public static bool Headless { get; set; }

	public static ExportHandler ExportHandler
	{
		private get;
		set
		{
			ArgumentNullException.ThrowIfNull(value);
			value.ThrowIfSettingsDontMatch(Settings);
			field = value;
		}
	} = new(Settings);

	/// <summary>
	/// Is this the premium edition?
	/// </summary>
	/// <remarks>
	/// This is purely for UI functionality and has no direct effect on the presense of features.
	/// </remarks>
	public static bool Premium => ExportHandler.GetType() != typeof(ExportHandler);

	public static bool IsLoading { get; private set; }
	public static string? LoadError { get; private set; }
	private static readonly List<string> s_loadLog = [];
	public static string[] LoadLogSnapshot { get { lock (s_loadLog) { return [.. s_loadLog]; } } }

	public static bool IsExporting { get; private set; }
	public static int ExportProgressCurrent { get; private set; }
	public static int ExportProgressTotal { get; private set; }
	public static string? ExportError { get; private set; }

	private static readonly List<string> s_exportLog = [];
	public static string[] ExportLogSnapshot { get { lock (s_exportLog) { return [.. s_exportLog]; } } }

	public static void Reset()
	{
		if (GameData is not null)
		{
			GameData = null;
			GC.Collect();
			Logger.Info(LogCategory.General, "Data was reset.");
		}
	}

	public static void LoadAndProcess(IReadOnlyList<string> paths)
	{
		Reset();
		Settings.LogConfigurationValues();
		GameData = ExportHandler.LoadAndProcess(paths, LocalFileSystem.Instance);
	}

	public static void BeginLoadAndProcess(IReadOnlyList<string> paths)
	{
		IsLoading = true;
		LoadError = null;
		lock (s_loadLog) { s_loadLog.Clear(); }

		CaptureLogger captureLogger = new(s_loadLog);
		Logger.Add(captureLogger);

		_ = Task.Run(() =>
		{
			try
			{
				Reset();
				Settings.LogConfigurationValues();
				GameData = ExportHandler.LoadAndProcess(paths, LocalFileSystem.Instance);
			}
			catch (Exception ex)
			{
				LoadError = ex.Message;
				Logger.Error(LogCategory.Import, $"Load failed: {ex.Message}");
			}
			finally
			{
				IsLoading = false;
				Logger.Remove(captureLogger);
			}
		});
	}

	public static async Task<string?> PrepareExportUnityProject(string path)
	{
		if (!IsLoaded || IsExporting || !IsValidExportDirectory(path))
		{
			return null;
		}

		if (IsNonEmptyDirectory(path))
		{
			if (!await UserConsentsToDeletion())
			{
				Logger.Info(LogCategory.Export, "User declined to delete existing export directory. Aborting export.");
				return null;
			}
			try
			{
				Directory.Delete(path, true);
			}
			catch (IOException ex)
			{
				Logger.Error(LogCategory.Export, $"Could not clear export directory — a file is locked by another process: {ex.Message}");
				return null;
			}
		}

		Directory.CreateDirectory(path);
		return path;
	}

	public static void BeginExportUnityProject(string path)
	{
		if (!IsLoaded || IsExporting)
		{
			return;
		}

		IsExporting = true;
		ExportProgressCurrent = 0;
		ExportProgressTotal = 0;
		ExportError = null;
		lock (s_exportLog) { s_exportLog.Clear(); }

		ExportHandler.ExportProgressUpdated += OnExportProgress;
		ExportHandler.ExportCollectionStarted += OnExportCollectionStarted;

		_ = Task.Run(() =>
		{
			try
			{
				ExportHandler.Export(GameData, path, LocalFileSystem.Instance);
			}
			catch (Exception ex)
			{
				ExportError = ex.Message;
				Logger.Error(LogCategory.Export, $"Export failed: {ex.Message}");
			}
			finally
			{
				IsExporting = false;
				ExportHandler.ExportProgressUpdated -= OnExportProgress;
				ExportHandler.ExportCollectionStarted -= OnExportCollectionStarted;
			}
		});
	}

	public static async Task<string?> PrepareExportPrimaryContent(string path)
	{
		if (!IsLoaded || IsExporting || !IsValidExportDirectory(path))
		{
			return null;
		}

		if (IsNonEmptyDirectory(path))
		{
			if (!await UserConsentsToDeletion())
			{
				Logger.Info(LogCategory.Export, "User declined to delete existing export directory. Aborting export.");
				return null;
			}
			try
			{
				Directory.Delete(path, true);
			}
			catch (IOException ex)
			{
				Logger.Error(LogCategory.Export, $"Could not clear export directory — a file is locked by another process: {ex.Message}");
				return null;
			}
		}

		Directory.CreateDirectory(path);
		return path;
	}

	public static void BeginExportPrimaryContent(string path)
	{
		if (!IsLoaded || IsExporting)
		{
			return;
		}

		IsExporting = true;
		ExportProgressCurrent = 0;
		ExportProgressTotal = 0;
		ExportError = null;
		lock (s_exportLog) { s_exportLog.Clear(); }

		_ = Task.Run(() =>
		{
			try
			{
				Logger.Info(LogCategory.Export, "Starting primary content export");
				Logger.Info(LogCategory.Export, $"Attempting to export assets to {path}...");
				Settings.ExportRootPath = path;
				PrimaryContentExporter.CreateDefault(GameData, Settings).Export(GameBundle, Settings, LocalFileSystem.Instance);
				Logger.Info(LogCategory.Export, "Finished exporting primary content.");
			}
			catch (Exception ex)
			{
				ExportError = ex.Message;
				Logger.Error(LogCategory.Export, $"Primary content export failed: {ex.Message}");
			}
			finally
			{
				IsExporting = false;
			}
		});
	}

	public static async Task ExportUnityProject(string path)
	{
		string? prepared = await PrepareExportUnityProject(path);
		if (prepared is not null)
		{
			BeginExportUnityProject(prepared);
		}
	}

	public static async Task ExportPrimaryContent(string path)
	{
		string? prepared = await PrepareExportPrimaryContent(path);
		if (prepared is not null)
		{
			BeginExportPrimaryContent(prepared);
		}
	}

	private static void OnExportProgress(int current, int total)
	{
		ExportProgressCurrent = current;
		ExportProgressTotal = total;
	}

	private static void OnExportCollectionStarted(string name)
	{
		lock (s_exportLog) { s_exportLog.Add(name); }
	}

	private static FullConfiguration LoadSettings()
	{
		FullConfiguration settings = new();
		settings.LoadFromDefaultPath();
		return settings;
	}

	private static bool IsValidExportDirectory(string path)
	{
		if (string.IsNullOrEmpty(path))
		{
			Logger.Error(LogCategory.Export, "Export path is empty");
			return false;
		}
		string directoryName = Path.GetFileName(path);
		if (directoryName is "Desktop" or "Documents" or "Downloads")
		{
			Logger.Error(LogCategory.Export, $"Export path '{path}' is a system directory");
			return false;
		}
		return true;
	}

	private static bool IsNonEmptyDirectory(string path)
	{
		return Directory.Exists(path) && Directory.EnumerateFileSystemEntries(path).Any();
	}

	private static async Task<bool> UserConsentsToDeletion()
	{
		if (Headless)
		{
			return true;
		}
		ConfirmationDialog.Options options = new()
		{
			Message = Localization.ExportDirectoryDeleteUserConfirmation,
			Type = ConfirmationDialog.Type.YesNo,
		};
		bool? result = await ConfirmationDialog.Confirm(options);
		return result ?? false;
	}
}
