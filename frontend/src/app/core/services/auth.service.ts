import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap, catchError, throwError, timer, Subscription } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface User {
  id: string;
  email: string;
  displayName: string;
  roles: string[];
}

export interface LoginRequest {
  email: string;
  password: string;
  rememberMe?: boolean;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiration: string;
  user: User;
}

export interface RegisterRequest {
  email: string;
  name?: string;
  displayName?: string;
  password: string;
  confirmPassword?: string;
}

export interface RefreshTokenRequest {
  accessToken: string;
  refreshToken: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly TOKEN_KEY = 'auth_token';
  private readonly REFRESH_TOKEN_KEY = 'refresh_token';
  private readonly USER_KEY = 'user_info';
  private readonly TOKEN_EXPIRATION_KEY = 'token_expiration';

  // Token yenileme için buffer süresi (dakika)
  private readonly tokenRefreshBuffer = 5;
  
  // Token yenileme subscription
  private refreshSubscription: Subscription | null = null;

  private currentUserSignal = signal<User | null>(null);
  private isAuthenticatedSignal = signal<boolean>(false);

  // Computed signals for reactive state
  currentUser = computed(() => this.currentUserSignal());
  isAuthenticated = computed(() => this.isAuthenticatedSignal());

  constructor(
    private http: HttpClient,
    private router: Router
  ) {
    this.loadStoredUser();
    this.scheduleTokenRefresh();
  }

  private loadStoredUser(): void {
    if (!this.checkTokenExpiration()) {
      this.clearStorage();
      return;
    }

    const token = localStorage.getItem(this.TOKEN_KEY);
    const userJson = localStorage.getItem(this.USER_KEY);
    
    if (token && userJson) {
      try {
        const user = JSON.parse(userJson);
        this.currentUserSignal.set(user);
        this.isAuthenticatedSignal.set(true);
      } catch {
        this.clearStorage();
      }
    }
  }

  /**
   * Kullanıcı girişi
   */
  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${environment.apiUrl}/api/auth/login`, request)
      .pipe(
        tap(response => this.handleAuthResponse(response)),
        catchError(error => {
          console.error('Login error:', error);
          if (error.status === 401) {
            return throwError(() => new Error('Geçersiz email veya şifre'));
          }
          return throwError(() => error);
        })
      );
  }

  /**
   * Windows Authentication ile giriş
   */
  windowsLogin(rememberMe: boolean = false): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(
      `${environment.apiUrl}/api/auth/windows-login`,
      { rememberMe },
      { withCredentials: true }
    ).pipe(
      tap(response => this.handleAuthResponse(response)),
      catchError(error => {
        console.error('Windows login error:', error);
        if (error.status === 401) {
          return throwError(() => new Error('Windows kimlik doğrulaması başarısız'));
        }
        return throwError(() => error);
      })
    );
  }

  /**
   * Kullanıcı kaydı
   */
  register(request: RegisterRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${environment.apiUrl}/api/auth/register`, request)
      .pipe(
        tap(response => this.handleAuthResponse(response)),
        catchError(error => {
          console.error('Register error:', error);
          if (error.status === 400) {
            return throwError(() => new Error('Geçersiz kayıt bilgileri'));
          }
          if (error.status === 409) {
            return throwError(() => new Error('Bu email adresi zaten kullanılıyor'));
          }
          return throwError(() => error);
        })
      );
  }

  /**
   * Token yenileme
   */
  refreshTokens(): Observable<LoginResponse> {
    const accessToken = localStorage.getItem(this.TOKEN_KEY);
    const refreshToken = localStorage.getItem(this.REFRESH_TOKEN_KEY);
    
    if (!accessToken || !refreshToken) {
      return throwError(() => new Error('Token bulunamadı'));
    }

    const request: RefreshTokenRequest = { accessToken, refreshToken };

    return this.http.post<LoginResponse>(`${environment.apiUrl}/api/auth/refresh`, request)
      .pipe(
        tap(response => this.handleAuthResponse(response)),
        catchError(error => {
          console.error('Token refresh error:', error);
          this.logout();
          return throwError(() => new Error('Token yenileme başarısız'));
        })
      );
  }

  /**
   * Çıkış işlemi
   */
  logout(): void {
    const refreshToken = localStorage.getItem(this.REFRESH_TOKEN_KEY);
    
    if (refreshToken) {
      this.http.post(`${environment.apiUrl}/api/auth/logout`, JSON.stringify(refreshToken), {
        headers: { 'Content-Type': 'application/json' }
      }).subscribe({
        error: (err) => console.error('Logout error:', err)
      });
    }

    this.clearStorage();
    this.currentUserSignal.set(null);
    this.isAuthenticatedSignal.set(false);
    
    if (this.refreshSubscription) {
      this.refreshSubscription.unsubscribe();
      this.refreshSubscription = null;
    }
    
    this.router.navigate(['/login']);
  }

  /**
   * Tüm oturumları kapat
   */
  logoutAll(): void {
    this.http.post(`${environment.apiUrl}/api/auth/logout-all`, {}).subscribe({
      error: (err) => console.error('Logout all error:', err),
      complete: () => {
        this.clearStorage();
        this.currentUserSignal.set(null);
        this.isAuthenticatedSignal.set(false);
        this.router.navigate(['/login']);
      }
    });
  }

  /**
   * Şifre değiştirme
   */
  changePassword(request: ChangePasswordRequest): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/api/auth/change-password`, request)
      .pipe(
        catchError(error => {
          if (error.status === 401) {
            return throwError(() => new Error('Mevcut şifre yanlış'));
          }
          return throwError(() => new Error('Şifre değiştirme başarısız'));
        })
      );
  }

  /**
   * Mevcut kullanıcı bilgilerini API'den getir
   */
  fetchCurrentUser(): Observable<User> {
    return this.http.get<User>(`${environment.apiUrl}/api/auth/me`)
      .pipe(
        tap(user => {
          localStorage.setItem(this.USER_KEY, JSON.stringify(user));
          this.currentUserSignal.set(user);
        }),
        catchError(error => {
          console.error('Get current user error:', error);
          return throwError(() => error);
        })
      );
  }

  /**
   * Access token'ı getir
   */
  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  /**
   * Kullanıcının belirli bir rolü var mı kontrol et
   */
  hasRole(role: string): boolean {
    const user = this.currentUserSignal();
    return user?.roles?.includes(role) ?? false;
  }

  /**
   * Kullanıcı admin mi kontrol et
   */
  isAdmin(): boolean {
    return this.hasRole('Admin');
  }

  /**
   * Token süresini kontrol et
   */
  private checkTokenExpiration(): boolean {
    const expiration = localStorage.getItem(this.TOKEN_EXPIRATION_KEY);
    
    if (!expiration) {
      return false;
    }
    
    const expirationDate = new Date(expiration);
    return expirationDate > new Date();
  }

  /**
   * Token yenileme zamanlayıcısı
   */
  private scheduleTokenRefresh(): void {
    if (this.refreshSubscription) {
      this.refreshSubscription.unsubscribe();
    }
    
    const expiration = localStorage.getItem(this.TOKEN_EXPIRATION_KEY);
    if (!expiration) return;
    
    const expirationDate = new Date(expiration);
    const now = new Date();
    
    // Token süresinin dolmasına X dakika kala yenile
    const refreshTime = expirationDate.getTime() - now.getTime() - (this.tokenRefreshBuffer * 60 * 1000);
    
    if (refreshTime > 0) {
      this.refreshSubscription = timer(refreshTime).subscribe(() => {
        this.refreshTokens().subscribe({
          next: () => console.log('Token refreshed successfully'),
          error: (err) => console.error('Token refresh failed:', err)
        });
      });
    }
  }

  private handleAuthResponse(response: LoginResponse): void {
    localStorage.setItem(this.TOKEN_KEY, response.accessToken);
    localStorage.setItem(this.REFRESH_TOKEN_KEY, response.refreshToken);
    localStorage.setItem(this.TOKEN_EXPIRATION_KEY, response.accessTokenExpiration);
    localStorage.setItem(this.USER_KEY, JSON.stringify(response.user));
    
    this.currentUserSignal.set(response.user);
    this.isAuthenticatedSignal.set(true);
    
    // Token yenileme zamanlayıcısını başlat
    this.scheduleTokenRefresh();
  }

  private clearStorage(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.REFRESH_TOKEN_KEY);
    localStorage.removeItem(this.TOKEN_EXPIRATION_KEY);
    localStorage.removeItem(this.USER_KEY);
  }
}
