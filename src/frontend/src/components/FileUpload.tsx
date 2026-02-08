import type React from 'react';
import { useState, useCallback } from 'react';

/**
 * ファイルアップロードコンポーネント
 * CSV ファイルを選択してバックエンドにアップロードする
 */

interface FileUploadProps {
  onUploadSuccess?: (fileName: string) => void;
  onUploadError?: (error: string) => void;
}

export const FileUpload: React.FC<FileUploadProps> = ({ onUploadSuccess, onUploadError }) => {
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [isUploading, setIsUploading] = useState(false);

  const handleFileChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0] ?? null;
    setSelectedFile(file);
  }, []);

  const handleUpload = useCallback(async () => {
    if (!selectedFile) return;

    setIsUploading(true);
    try {
      // TODO: API 呼び出しを実装（タスク003で実装予定）
      onUploadSuccess?.(selectedFile.name);
    } catch {
      onUploadError?.('ファイルのアップロードに失敗しました');
    } finally {
      setIsUploading(false);
    }
  }, [selectedFile, onUploadSuccess, onUploadError]);

  return (
    <div className="file-upload">
      <h2>CSV ファイルアップロード</h2>
      <input
        type="file"
        accept=".csv"
        onChange={handleFileChange}
        disabled={isUploading}
      />
      <button
        onClick={handleUpload}
        disabled={!selectedFile || isUploading}
      >
        {isUploading ? 'アップロード中...' : 'アップロード'}
      </button>
      {selectedFile && <p>選択ファイル: {selectedFile.name}</p>}
    </div>
  );
};
