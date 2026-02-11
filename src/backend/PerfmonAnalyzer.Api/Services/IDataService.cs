using PerfmonAnalyzer.Api.Models;

namespace PerfmonAnalyzer.Api.Services;

/// <summary>
/// セッション管理とデータ提供を行うサービスのインターフェース
/// </summary>
public interface IDataService
{
    /// <summary>
    /// 新しいセッションを作成し、カウンターデータを保存する
    /// </summary>
    /// <param name="counters">保存するカウンタ情報</param>
    /// <returns>発行されたセッションID</returns>
    string CreateSession(List<CounterInfo> counters);

    /// <summary>
    /// セッションIDからカウンターデータを取得する
    /// </summary>
    /// <param name="sessionId">セッションID</param>
    /// <returns>カウンタ情報のリスト</returns>
    /// <exception cref="KeyNotFoundException">セッションが存在しない場合</exception>
    List<CounterInfo> GetCounters(string sessionId);

    /// <summary>
    /// 期間を指定してカウンターデータを取得する
    /// </summary>
    /// <param name="sessionId">セッションID</param>
    /// <param name="startTime">取得開始時刻</param>
    /// <param name="endTime">取得終了時刻</param>
    /// <returns>時間範囲でフィルタされたカウンタ情報のリスト</returns>
    /// <exception cref="KeyNotFoundException">セッションが存在しない場合</exception>
    List<CounterInfo> GetCounters(string sessionId, DateTime startTime, DateTime endTime);

    /// <summary>
    /// セッションが存在するかチェックする
    /// </summary>
    /// <param name="sessionId">セッションID</param>
    /// <returns>存在する場合 true</returns>
    bool SessionExists(string sessionId);

    /// <summary>
    /// 期限切れセッションを削除する
    /// </summary>
    /// <param name="expirationTime">セッションの有効期間</param>
    void CleanupExpiredSessions(TimeSpan expirationTime);
}
