"""
パフォーマンスモニタのテストデータを生成するスクリプト

リーク傾向あり/なしの2種類のプロセスを含む60時間分のCSVデータを生成します。
"""
import csv
from datetime import datetime, timedelta

def generate_test_data(filename: str, hours: int = 60):
    """
    テスト用のCSVデータを生成
    
    Args:
        filename: 出力ファイル名
        hours: 生成するデータの時間数（デフォルト: 60時間）
    """
    with open(filename, 'w', newline='', encoding='utf-8') as f:
        writer = csv.writer(f)
        
        # ヘッダー行
        writer.writerow([
            '(PDH-CSV 4.0)',
            '\\\\SERVER\\Process(app1)\\Private Bytes',
            '\\\\SERVER\\Process(app2)\\Private Bytes',
            '\\\\SERVER\\Process(app3)\\Private Bytes'
        ])
        
        base_time = datetime(2026, 1, 15, 10, 0, 0)
        data_points = hours * 60  # 1分間隔
        
        for i in range(data_points):
            timestamp = (base_time + timedelta(minutes=i)).strftime('%m/%d/%Y %H:%M:%S.000')
            
            # app1: メモリリークあり（毎分 5KB 増加）
            app1_value = 10000000 + i * 5120
            
            # app2: リークなし（変動のみ）
            import random
            random.seed(i)  # 再現性のために固定シード
            app2_value = 20000000 + random.randint(-50000, 50000)
            
            # app3: 段階的なリーク（30分ごとに大きく増加）
            step = i // 30
            app3_value = 15000000 + step * 100000 + random.randint(-10000, 10000)
            
            writer.writerow([timestamp, app1_value, app2_value, app3_value])
    
    print(f"✓ {filename} を作成しました ({data_points} データポイント)")

def generate_small_test_data(filename: str):
    """
    小さなテスト用CSVデータを生成（動作確認用）
    
    Args:
        filename: 出力ファイル名
    """
    with open(filename, 'w', newline='', encoding='utf-8') as f:
        writer = csv.writer(f)
        
        # ヘッダー行
        writer.writerow([
            '(PDH-CSV 4.0)',
            '\\\\SERVER\\Process(test1)\\Private Bytes',
            '\\\\SERVER\\Process(test2)\\Private Bytes'
        ])
        
        base_time = datetime(2026, 2, 11, 10, 0, 0)
        
        # 30分間のデータ（30ポイント）
        for i in range(30):
            timestamp = (base_time + timedelta(minutes=i)).strftime('%m/%d/%Y %H:%M:%S.000')
            test1_value = 5000000 + i * 10240  # 毎分10KB増加
            test2_value = 8000000 + (i % 5) * 5000  # 変動のみ
            
            writer.writerow([timestamp, test1_value, test2_value])
    
    print(f"✓ {filename} を作成しました (30 データポイント)")

if __name__ == '__main__':
    # 60時間分の本格的なテストデータ
    generate_test_data('test_data_60h.csv', hours=60)
    
    # 動作確認用の小さなテストデータ
    generate_small_test_data('test_data_small.csv')
    
    print("\n全てのテストデータを生成しました！")
