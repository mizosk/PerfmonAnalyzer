using PerfmonAnalyzer.Api.Models;

namespace PerfmonAnalyzer.Api.Services;

/// <summary>
/// Perfmon CSV ファイルをインポートするサービスのインターフェース
/// </summary>
public interface ICsvImporter
{
    /// <summary>
    /// CSV ストリームを読み込み、カウンタ情報のリストに変換する
    /// </summary>
    /// <param name="csvStream">CSV データのストリーム</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>カウンタ情報のリスト</returns>
    Task<List<CounterInfo>> ImportAsync(Stream csvStream, CancellationToken cancellationToken = default);
}
