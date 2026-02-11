import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { FileUpload } from '../FileUpload';
import type { UploadResult } from '../../types';

// API モジュールをモック
vi.mock('../../services/api', () => ({
  uploadCsv: vi.fn(),
}));

import { uploadCsv } from '../../services/api';
const mockUploadCsv = vi.mocked(uploadCsv);

/** ファイル入力要素を取得するヘルパー */
function getFileInput(): HTMLInputElement {
  const input = document.querySelector('input[type="file"]');
  if (!input) throw new Error('File input not found');
  return input as HTMLInputElement;
}

describe('FileUpload', () => {
  const mockOnUploadSuccess = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('ファイル入力とアップロードボタンを表示する', () => {
    render(<FileUpload onUploadSuccess={mockOnUploadSuccess} />);
    expect(screen.getByText('CSV ファイルアップロード')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'アップロード' })).toBeInTheDocument();
  });

  it('ファイル未選択時はアップロードボタンが無効', () => {
    render(<FileUpload onUploadSuccess={mockOnUploadSuccess} />);
    expect(screen.getByRole('button', { name: 'アップロード' })).toBeDisabled();
  });

  it('ファイルを選択するとアップロードボタンが有効になる', () => {
    render(<FileUpload onUploadSuccess={mockOnUploadSuccess} />);
    const input = getFileInput();
    const file = new File(['test'], 'test.csv', { type: 'text/csv' });
    fireEvent.change(input, { target: { files: [file] } });
    expect(screen.getByRole('button', { name: 'アップロード' })).toBeEnabled();
  });

  it('アップロード成功時にコールバックを呼び出す', async () => {
    const mockResult: UploadResult = {
      sessionId: 'test-session-id',
      counters: [],
    };
    mockUploadCsv.mockResolvedValueOnce(mockResult);

    render(<FileUpload onUploadSuccess={mockOnUploadSuccess} />);
    const input = getFileInput();
    const file = new File(['test'], 'test.csv', { type: 'text/csv' });
    fireEvent.change(input, { target: { files: [file] } });

    const button = screen.getByRole('button', { name: 'アップロード' });
    fireEvent.click(button);

    await waitFor(() => {
      expect(mockUploadCsv).toHaveBeenCalledWith(file);
      expect(mockOnUploadSuccess).toHaveBeenCalledWith(mockResult);
    });
  });

  it('アップロード失敗時にエラーメッセージを表示する', async () => {
    mockUploadCsv.mockRejectedValueOnce(new Error('Upload failed'));

    render(<FileUpload onUploadSuccess={mockOnUploadSuccess} />);
    const input = getFileInput();
    const file = new File(['test'], 'test.csv', { type: 'text/csv' });
    fireEvent.change(input, { target: { files: [file] } });

    const button = screen.getByRole('button', { name: 'アップロード' });
    fireEvent.click(button);

    await waitFor(() => {
      expect(screen.getByText('ファイルのアップロードに失敗しました。')).toBeInTheDocument();
    });
  });
});

