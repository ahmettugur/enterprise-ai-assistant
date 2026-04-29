import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login.html',
  styleUrl: './login.css'
})
export class Login {
  activeTab = signal<'login' | 'register'>('login');
  
  // Login form - default credentials for development
  loginEmail = 'admin@system.local';
  loginPassword = 'Admin123!';
  rememberMe = false;
  
  // Register form
  registerName = '';
  registerEmail = '';
  registerPassword = '';
  registerPasswordConfirm = '';
  
  // UI state
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  showPassword = signal(false);
  showRegisterPassword = signal(false);
  passwordStrength = signal<'weak' | 'medium' | 'strong' | null>(null);

  constructor(
    private authService: AuthService,
    private router: Router
  ) {
    // Redirect if already authenticated
    if (this.authService.isAuthenticated()) {
      this.router.navigate(['/']);
    }
  }

  switchTab(tab: 'login' | 'register'): void {
    this.activeTab.set(tab);
    this.clearMessages();
  }

  togglePassword(): void {
    this.showPassword.update(v => !v);
  }

  toggleRegisterPassword(): void {
    this.showRegisterPassword.update(v => !v);
  }

  checkPasswordStrength(): void {
    const password = this.registerPassword;
    let strength = 0;
    
    if (password.length >= 6) strength++;
    if (password.length >= 8) strength++;
    if (/[A-Z]/.test(password)) strength++;
    if (/[0-9]/.test(password)) strength++;
    if (/[^A-Za-z0-9]/.test(password)) strength++;

    if (password.length === 0) {
      this.passwordStrength.set(null);
    } else if (strength <= 2) {
      this.passwordStrength.set('weak');
    } else if (strength <= 3) {
      this.passwordStrength.set('medium');
    } else {
      this.passwordStrength.set('strong');
    }
  }

  async onLogin(): Promise<void> {
    this.clearMessages();

    if (!this.loginEmail || !this.loginPassword) {
      this.errorMessage.set('Lütfen tüm alanları doldurun.');
      return;
    }

    this.isLoading.set(true);

    try {
      await this.authService.login({
        email: this.loginEmail,
        password: this.loginPassword,
        rememberMe: this.rememberMe
      }).toPromise();

      this.router.navigate(['/']);
    } catch (error: any) {
      this.errorMessage.set(error?.error?.message || 'Giriş yapılamadı. Lütfen bilgilerinizi kontrol edin.');
    } finally {
      this.isLoading.set(false);
    }
  }

  async onRegister(): Promise<void> {
    this.clearMessages();

    if (!this.registerName || !this.registerEmail || !this.registerPassword || !this.registerPasswordConfirm) {
      this.errorMessage.set('Lütfen tüm alanları doldurun.');
      return;
    }

    if (this.registerPassword !== this.registerPasswordConfirm) {
      this.errorMessage.set('Şifreler eşleşmiyor.');
      return;
    }

    if (this.registerPassword.length < 6) {
      this.errorMessage.set('Şifre en az 6 karakter olmalıdır.');
      return;
    }

    this.isLoading.set(true);

    try {
      await this.authService.register({
        name: this.registerName,
        email: this.registerEmail,
        password: this.registerPassword
      }).toPromise();

      this.successMessage.set('Kayıt başarılı! Yönlendiriliyorsunuz...');
      setTimeout(() => this.router.navigate(['/']), 1500);
    } catch (error: any) {
      this.errorMessage.set(error?.error?.message || 'Kayıt yapılamadı. Lütfen tekrar deneyin.');
    } finally {
      this.isLoading.set(false);
    }
  }

  async onWindowsLogin(): Promise<void> {
    this.clearMessages();
    this.isLoading.set(true);

    try {
      await this.authService.windowsLogin().toPromise();
      this.router.navigate(['/']);
    } catch (error: any) {
      this.errorMessage.set(error?.error?.message || 'Windows kimlik doğrulaması başarısız.');
    } finally {
      this.isLoading.set(false);
    }
  }

  private clearMessages(): void {
    this.errorMessage.set(null);
    this.successMessage.set(null);
  }
}
