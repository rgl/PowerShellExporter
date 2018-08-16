dist: dist/PowerShellExporter.zip

dist/PowerShellExporter.zip: dist/PowerShellExporter.exe dist/metrics.yml dist/metrics.psm1
	cd dist && \
		rm -f PowerShellExporter.zip && \
		zip -9 PowerShellExporter.zip \
			PowerShellExporter.exe{,.config} \
			metrics.yml \
			metrics.psm1 && \
		unzip -l PowerShellExporter.zip && \
		sha256sum PowerShellExporter.zip

dist/PowerShellExporter.exe: PowerShellExporter/bin/Release/PowerShellExporter.exe tmp/libz.exe
	mkdir -p dist
	# NB to be able to load Serilog.Sinks.File from .config we need to use Scenario 4 as
	#    described at https://github.com/MiloszKrajewski/LibZ/blob/master/doc/scenarios.md
	cd PowerShellExporter/bin/Release && \
		../../../tmp/libz add --libz PowerShellExporter.libz --include '*.dll' --move && \
		../../../tmp/libz inject-libz --assembly PowerShellExporter.exe --libz PowerShellExporter.libz --move && \
		../../../tmp/libz instrument --assembly PowerShellExporter.exe --libz-resources
	cp PowerShellExporter/bin/Release/PowerShellExporter.exe* dist

dist/metrics.yml: PowerShellExporter/metrics.yml
	mkdir -p dist
	cp $< $@

dist/metrics.psm1: PowerShellExporter/metrics.psm1
	mkdir -p dist
	cp $< $@

tmp/libz.exe:
	mkdir -p tmp
	wget -Otmp/libz-1.2.0.0-tool.zip https://github.com/MiloszKrajewski/LibZ/releases/download/1.2.0.0/libz-1.2.0.0-tool.zip
	unzip -d tmp tmp/libz-1.2.0.0-tool.zip

PowerShellExporter/bin/Release/PowerShellExporter.exe: PowerShellExporter/*
	MSBuild.exe -m -p:Configuration=Release -t:restore -t:build

clean:
	MSBuild.exe -m -p:Configuration=Release -t:clean
	rm -rf tmp dist

.PHONY: dist clean
