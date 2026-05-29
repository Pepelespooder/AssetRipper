namespace AssetRipper.GUI.Web;

public sealed record LoadStatus(bool IsLoading, bool IsLoaded, string? Error, string[] Log)
{
	public static LoadStatus Snapshot => new(
		GameFileLoader.IsLoading,
		GameFileLoader.IsLoaded,
		GameFileLoader.LoadError,
		GameFileLoader.LoadLogSnapshot);
}
