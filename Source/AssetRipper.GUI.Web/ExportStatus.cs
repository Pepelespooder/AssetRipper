namespace AssetRipper.GUI.Web;

public sealed record ExportStatus(bool IsExporting, int Current, int Total, string? Error, string[] Log)
{
	public static ExportStatus Snapshot => new(
		GameFileLoader.IsExporting,
		GameFileLoader.ExportProgressCurrent,
		GameFileLoader.ExportProgressTotal,
		GameFileLoader.ExportError,
		GameFileLoader.ExportLogSnapshot);
}
