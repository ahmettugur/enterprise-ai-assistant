/**
 * Document Management Models
 * Angular equivalents of backend DTOs for document and category management
 */

// ==================== ENUMS ====================

/**
 * Döküman tipi
 */
export enum DocumentType {
  Document = 0,
  QuestionAnswer = 1
}

// ==================== DOCUMENT CATEGORY ====================

/**
 * Döküman kategorisi
 */
export interface DocumentCategory {
  id: string;
  displayName: string;
  description?: string;
  userId?: string;
  isActive: boolean;
  documentCount: number;
  createdAt: Date;
  updatedAt?: Date;
}

/**
 * Select2/Dropdown için basit kategori
 */
export interface DocumentCategorySelect {
  id: string;
  text: string;
  description?: string;
}

/**
 * Kategori oluşturma isteği
 */
export interface CreateDocumentCategoryRequest {
  id: string;
  displayName: string;
  description?: string;
}

/**
 * Kategori güncelleme isteği
 */
export interface UpdateDocumentCategoryRequest {
  displayName: string;
  description?: string;
  userId?: string;
  isActive?: boolean;
}

// ==================== DOCUMENT DISPLAY INFO ====================

/**
 * Döküman görüntüleme bilgisi (detaylı)
 */
export interface DocumentDisplayInfo {
  id: string;
  fileName: string;
  documentType: DocumentType;
  displayName: string;
  description?: string;
  keywords?: string;
  categoryId?: string;
  categoryName?: string;
  userId?: string;
  isActive: boolean;
  createdBy?: string;
  createdAt: Date;
  updatedAt?: Date;
  hasEmbeddings: boolean;
  chunkCount: number;
}

/**
 * Döküman listesi için özet
 */
export interface DocumentDisplayInfoList {
  id: string;
  fileName: string;
  documentType: DocumentType;
  displayName: string;
  description?: string;
  categoryId?: string;
  categoryName?: string;
  userId?: string;
  isActive: boolean;
  hasEmbeddings: boolean;
  chunkCount: number;
  createdAt: Date;
}

/**
 * Select2/Dropdown için basit döküman
 */
export interface DocumentDisplayInfoSelect {
  id: string;
  text: string;
  fileName: string;
  documentType: DocumentType;
}

/**
 * Döküman oluşturma isteği (dosya yükleme için metadata)
 */
export interface CreateDocumentDisplayInfoRequest {
  displayName: string;
  documentType?: DocumentType;
  description?: string;
  keywords?: string;
  categoryId?: string;
}

/**
 * Döküman güncelleme isteği
 */
export interface UpdateDocumentDisplayInfoRequest {
  displayName: string;
  documentType?: DocumentType;
  description?: string;
  keywords?: string;
  categoryId?: string;
  isActive?: boolean;
}

/**
 * Dosya yükleme için FormData modeli
 */
export interface DocumentUploadData {
  file: File;
  displayName: string;
  documentType: DocumentType;
  description?: string;
  keywords?: string;
  categoryId?: string;
}

// ==================== API RESPONSE ====================

/**
 * Generic API response wrapper
 */
export interface ApiResult<T> {
  isSucceed: boolean;
  resultData?: T;
  userMessage?: string;
  systemMessage?: string;
}

// ==================== UPLOAD PROGRESS ====================

/**
 * Yükleme durumu
 */
export interface UploadProgress {
  percent: number;
  status: 'idle' | 'uploading' | 'processing' | 'embedding' | 'completed' | 'error';
  message: string;
}
