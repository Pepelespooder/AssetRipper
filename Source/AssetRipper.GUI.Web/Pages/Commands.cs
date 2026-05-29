using AssetRipper.NativeDialogs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace AssetRipper.GUI.Web.Pages;

public static class Commands
{
	private const string RootPath = "/";
	private const string CommandsPath = "/Commands";

	/// <summary>
	/// For documentation purposes
	/// </summary>
	/// <param name="Path">The file system path.</param>
	internal record PathFormData(string Path);

	internal static RouteHandlerBuilder AcceptsFormDataContainingPath(this RouteHandlerBuilder builder)
	{
		return builder.Accepts<PathFormData>("application/x-www-form-urlencoded");
	}

	private static bool TryGetCreateSubfolder(IFormCollection form)
	{
		if (form.TryGetValue("CreateSubfolder", out StringValues values))
		{
			return values == "true";
		}

		return false;
	}

	public readonly struct LoadFile : ICommand
	{
		static async Task<string?> ICommand.Execute(HttpRequest request)
		{
			IFormCollection form = await request.ReadFormAsync();

			string[]? paths;
			if (form.TryGetValue("Path", out StringValues values))
			{
				paths = values;
			}
			else if (NativeDialog.Supported)
			{
				paths = await OpenFileDialog.OpenFiles();
			}
			else
			{
				return CommandsPath;
			}

			if (paths is { Length: > 0 })
			{
				GameFileLoader.BeginLoadAndProcess(paths);
				return "/Load/Progress";
			}
			return CommandsPath;
		}
	}

	public readonly struct LoadFolder : ICommand
	{
		static async Task<string?> ICommand.Execute(HttpRequest request)
		{
			IFormCollection form = await request.ReadFormAsync();

			string[]? paths;
			if (form.TryGetValue("Path", out StringValues values))
			{
				paths = values;
			}
			else if (NativeDialog.Supported)
			{
				paths = await OpenFolderDialog.OpenFolders();
			}
			else
			{
				return CommandsPath;
			}

			if (paths is { Length: > 0 })
			{
				GameFileLoader.BeginLoadAndProcess(paths);
				return "/Load/Progress";
			}
			return CommandsPath;
		}
	}

	public readonly struct ExportUnityProject : ICommand
	{
		static async Task<string?> ICommand.Execute(HttpRequest request)
		{
			IFormCollection form = await request.ReadFormAsync();

			if (!form.TryGetValue("Path", out StringValues values))
			{
				return CommandsPath;
			}

			string? path = values;
			if (string.IsNullOrEmpty(path))
			{
				return CommandsPath;
			}

			bool createSubfolder = TryGetCreateSubfolder(form);
			path = MaybeAppendTimestampedSubfolder(path, createSubfolder);

			string? prepared = await GameFileLoader.PrepareExportUnityProject(path);
			if (prepared is null)
			{
				return CommandsPath;
			}

			GameFileLoader.BeginExportUnityProject(prepared);
			return "/Export/Progress";
		}
	}

	public readonly struct ExportPrimaryContent : ICommand
	{
		static async Task<string?> ICommand.Execute(HttpRequest request)
		{
			IFormCollection form = await request.ReadFormAsync();

			if (!form.TryGetValue("Path", out StringValues values))
			{
				return CommandsPath;
			}

			string? path = values;
			if (string.IsNullOrEmpty(path))
			{
				return CommandsPath;
			}

			bool createSubfolder = TryGetCreateSubfolder(form);
			path = MaybeAppendTimestampedSubfolder(path, createSubfolder);

			string? prepared = await GameFileLoader.PrepareExportPrimaryContent(path);
			if (prepared is null)
			{
				return CommandsPath;
			}

			GameFileLoader.BeginExportPrimaryContent(prepared);
			return "/Export/Progress";
		}
	}

	private static string MaybeAppendTimestampedSubfolder(string path, bool append)
	{
		if (append)
		{
			string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
			string subfolder = $"AssetRipper_export_{timestamp}";
			return Path.Combine(path, subfolder);
		}

		return path;
	}

	public readonly struct Reset : ICommand
	{
		static Task<string?> ICommand.Execute(HttpRequest request)
		{
			GameFileLoader.Reset();
			return Task.FromResult<string?>(null);
		}
	}

	public static async Task HandleCommand<T>(HttpContext context) where T : ICommand
	{
		string? redirectionTarget = await T.Execute(context.Request);
		context.Response.Redirect(redirectionTarget ?? RootPath);
	}
}
