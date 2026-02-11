import type React from 'react';
import { useState, useCallback } from 'react';
import { uploadCsv } from '../services/api';
import type { UploadResult } from '../types';

/**
 * ファイルアップロードコンポーネントの props
 */
interface FileUploadProps {
  onUploadSuccess: (result: UploadResult) => void;
}

/**
 * ファイルアップロードコンポーネント
 * CSV ファイルを選択してバックエンドにアップロードする
 */
export const FileUpload: React.FC<FileUploadProps> = ({ onUploadSuccess }) => {
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [isUploading, setIsUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  /** ファイル選択時 */
  const handleFileChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0] ?? null;
    setSelectedFile(file);
    setError(null);
  }, []);

  /** アップロード実行 */
  const handleUpload = useCallback(async () => {
    if (!selectedFile) return;

    setIsUploading(true);
    setError(null);
    try {
      const result = await uploadCsv(selectedFile);
      onUploadSuccess(result);
    } catch {
      setError('ファイルのアップロードに失敗しました。');
    } finally {
      setIsUploading(false);
    }
  }, [selectedFile, onUploadSuccess]);

  return (
    <div className="file-upload">
      <h2>CSV ファイルアップロード</h2>
      <div className="file-upload__controls">
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
      </div>
      {selectedFile && <p className="file-upload__filename">選択ファイル: {selectedFile.name}</p>}
      {error && <p className="file-upload__error">{error}</p>}
    </div>
  );
};
