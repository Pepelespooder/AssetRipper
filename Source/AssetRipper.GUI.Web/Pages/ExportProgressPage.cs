namespace AssetRipper.GUI.Web.Pages;

public sealed class ExportProgressPage : DefaultPage
{
	public static ExportProgressPage Instance { get; } = new();

	public override string GetTitle() => Localization.ExportInProgressTitle;

	public override void WriteInnerContent(TextWriter writer)
	{
		using (new Div(writer).WithClass("text-center mt-4").End())
		{
			new H1(writer).Close(Localization.ExportInProgressTitle);

			using (new Div(writer).WithClass("progress mt-3 mb-2").WithStyle("height:30px;position:relative").End())
			{
				new Div(writer)
					.WithId("progress-bar")
					.WithClass("progress-bar progress-bar-striped progress-bar-animated")
					.WithRole("progressbar")
					.WithStyle("width: 0%")
					.WithCustomAttribute("aria-valuenow", "0")
					.WithCustomAttribute("aria-valuemin", "0")
					.WithCustomAttribute("aria-valuemax", "100")
					.Close();
				new Span(writer)
					.WithId("progress-text")
					.WithStyle("position:absolute;width:100%;text-align:center;line-height:30px;top:0;left:0;font-weight:bold;text-shadow:0 0 3px #000")
					.Close("0%");
			}

			new P(writer).WithId("status-text").WithClass("text-muted").Close(Localization.ExportPreparing);
		}

		using (new Div(writer)
			.WithId("export-console")
			.WithStyle("height:250px;overflow-y:auto;background:#1e1e1e;color:#d4d4d4;font-family:monospace;font-size:12px;padding:8px;border-radius:4px;margin-top:12px;text-align:left")
			.End())
		{
		}

		using (new Script(writer).End())
		{
			writer.Write("""
				let lastLogCount = 0;
				async function poll() {
					try {
						const s = await fetch('/Export/Status').then(r => r.json());
						if (s.Error) {
							document.getElementById('status-text').textContent = s.Error;
							document.getElementById('progress-bar').classList.remove('progress-bar-animated');
							document.getElementById('progress-bar').classList.add('bg-danger');
							return;
						}
						if (!s.IsExporting) {
							window.location.href = '/Commands';
							return;
						}
						const pct = s.Total > 0 ? Math.round(s.Current / s.Total * 100) : 0;
						const bar = document.getElementById('progress-bar');
						bar.style.width = pct + '%';
						bar.setAttribute('aria-valuenow', pct);
						document.getElementById('progress-text').textContent = pct + '%';
						document.getElementById('status-text').textContent = s.Current + ' / ' + s.Total;
						if (s.Log && s.Log.length > lastLogCount) {
							const con = document.getElementById('export-console');
							for (let i = lastLogCount; i < s.Log.length; i++) {
								const line = document.createElement('div');
								line.textContent = s.Log[i];
								con.appendChild(line);
								if (con.children.length > 500) con.removeChild(con.firstChild);
							}
							lastLogCount = s.Log.length;
							con.scrollTop = con.scrollHeight;
						}
					} catch (e) {}
					setTimeout(poll, 500);
				}
				poll();
				""");
		}
	}
}
