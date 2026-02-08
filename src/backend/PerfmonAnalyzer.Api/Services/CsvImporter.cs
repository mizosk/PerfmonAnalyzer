using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;
using PerfmonAnalyzer.Api.Models;

namespace PerfmonAnalyzer.Api.Services;

/// <summary>
/// Perfmon CSVファイルをインポートするサービス実装
/// </summary>
public partial class CsvImporter : ICsvImporter
{
    /// <summary>
    /// Perfmon ヘッダを分解する正規表現
    /// 例: \\SERVER\Processor(_Total)\% Processor Time
    /// </summary>
    [GeneratedRegex(@"\\\\([^\\]+)\\(.+?)(?:\(([^)]*)\))?\\(.+)")]
    private static partial Regex PerfmonHeaderRegex();

    /// <inheritdoc />
    public async Task<List<CounterInfo>> ImportAsync(Stream csvStream)
    {
        // ストリーム全体をメモリに読み込み（エンコーディング判定のため）
        using var memoryStream = new MemoryStream();
        await csvStream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        var encoding = DetectEncoding(memoryStream);
        memoryStream.Position = 0;

        using var reader = new StreamReader(memoryStream, encoding);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.None,
        };
        using var csv = new CsvReader(reader, config);

        // ヘッダ読み込み
        await csv.ReadAsync();
        csv.ReadHeader();
        var headers = csv.HeaderRecord
            ?? throw new InvalidOperationException("CSV にヘッダ行がありません。");

        // ヘッダからカウンタ情報を抽出（最初の列はタイムスタンプ）
        var counterInfos = new List<CounterInfo>();
        for (var i = 1; i < headers.Length; i++)
        {
            var header = headers[i].Trim();
            var info = ParseHeader(header);
            counterInfos.Add(info);
        }

        // データ行を読み込み
        var dataPointsPerCounter = new List<List<DataPoint>>();
        for (var i = 0; i < counterInfos.Count; i++)
        {
            dataPointsPerCounter.Add([]);
        }

        while (await csv.ReadAsync())
        {
            var timestampStr = csv.GetField(0)?.Trim() ?? "";
            if (!DateTime.TryParse(timestampStr, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var timestamp))
            {
                continue; // タイムスタンプがパースできない行はスキップ
            }

            for (var i = 0; i < counterInfos.Count; i++)
            {
                var fieldStr = csv.GetField(i + 1)?.Trim() ?? "";
                var value = ParseDoubleValue(fieldStr);
                dataPointsPerCounter[i].Add(new DataPoint
                {
                    Timestamp = timestamp,
                    Value = value,
                });
            }
        }

        // データポイントをカウンタ情報に格納
        for (var i = 0; i < counterInfos.Count; i++)
        {
            counterInfos[i].DataPoints = dataPointsPerCounter[i].ToArray();
        }

        return counterInfos;
    }

    /// <summary>
    /// BOM を確認してエンコーディングを判定する。
    /// BOM があれば UTF-8、なければ Shift-JIS として扱う。
    /// </summary>
    private static Encoding DetectEncoding(Stream stream)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var bom = new byte[3];
        var bytesRead = stream.Read(bom, 0, 3);
        stream.Position = 0;

        // UTF-8 BOM: EF BB BF
        if (bytesRead >= 3 && bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
        {
            return Encoding.UTF8;
        }

        // BOM なしは Shift-JIS として扱う
        return Encoding.GetEncoding("shift_jis");
    }

    /// <summary>
    /// Perfmon ヘッダ文字列をパースしてカウンタ情報に変換する
    /// </summary>
    private static CounterInfo ParseHeader(string header)
    {
        var match = PerfmonHeaderRegex().Match(header);
        if (match.Success)
        {
            return new CounterInfo
            {
                MachineName = match.Groups[1].Value,
                Category = match.Groups[2].Value,
                InstanceName = match.Groups[3].Value, // キャプチャされなければ空文字
                CounterName = match.Groups[4].Value,
                DisplayName = header,
            };
        }

        // 正規表現にマッチしない場合はそのまま表示名として扱う
        return new CounterInfo
        {
            DisplayName = header,
        };
    }

    /// <summary>
    /// 文字列を double に変換する。空文字やパース不能な値は NaN を返す。
    /// </summary>
    private static double ParseDoubleValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return double.NaN;
        }

        return double.TryParse(value, CultureInfo.InvariantCulture, out var result)
            ? result
            : double.NaN;
    }
}
