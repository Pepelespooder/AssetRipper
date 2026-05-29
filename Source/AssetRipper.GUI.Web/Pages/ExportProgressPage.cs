namespace AssetRipper.GUI.Web.Pages;

public sealed class ExportProgressPage : DefaultPage
{
	public static ExportProgressPage Instance { get; } = new();

	public override string GetTitle() => Localization.ExportInProgressTitle;

	public override void WriteInnerContent(TextWriter writer)
	{
		using (new Div(writer).WithClass("text-center mt-4").End())
		{
			new H1(writer).WithId("page-title").Close(Localization.ExportInProgressTitle);

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

			new A(writer)
				.WithId("continue-btn")
				.WithHref("/Commands")
				.WithClass("btn btn-success mt-3")
				.WithStyle("display:none")
				.Close(Localization.Continue);
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
						appendLog(s.Log);
						if (s.Error) {
							document.getElementById('page-title').textContent = 'Export Failed';
							document.getElementById('status-text').textContent = s.Error;
							document.getElementById('progress-bar').classList.remove('progress-bar-animated');
							document.getElementById('progress-bar').classList.add('bg-danger');
							document.getElementById('progress-text').textContent = 'Error';
							document.getElementById('continue-btn').style.display = 'inline-block';
							return;
						}
						if (!s.IsExporting) {
							document.getElementById('page-title').textContent = 'Export Complete!';
							document.getElementById('progress-bar').classList.remove('progress-bar-animated');
							document.getElementById('progress-bar').classList.add('bg-success');
							document.getElementById('progress-bar').style.width = '100%';
							document.getElementById('progress-text').textContent = '100%';
							document.getElementById('status-text').textContent = s.Total + ' assets exported.';
							document.getElementById('continue-btn').style.display = 'inline-block';
							return;
						}
						const pct = s.Total > 0 ? Math.round(s.Current / s.Total * 100) : 0;
						const bar = document.getElementById('progress-bar');
						bar.style.width = pct + '%';
						bar.setAttribute('aria-valuenow', pct);
						document.getElementById('progress-text').textContent = pct + '%';
						document.getElementById('status-text').textContent = s.Current + ' / ' + s.Total;
					} catch (e) {}
					setTimeout(poll, 500);
				}
				function appendLog(lines) {
					if (!lines) return;
					const con = document.getElementById('export-console');
					for (let i = lastLogCount; i < lines.length; i++) {
						const line = document.createElement('div');
						line.textContent = lines[i];
						con.appendChild(line);
						if (con.children.length > 500) con.removeChild(con.firstChild);
					}
					lastLogCount = lines.length;
					con.scrollTop = con.scrollHeight;
				}
				poll();
				""");
		}
	}
}
