import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../services/api.service';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';

@Component({
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <h2>Internal OTP Login</h2>
    <form [formGroup]="mobileForm" (ngSubmit)="requestOtp()" class="card" *ngIf="!otpSent">
      <input formControlName="mobile" placeholder="Registered Mobile" />
      <button type="submit" [disabled]="mobileForm.invalid">Request OTP</button>
    </form>

    <form [formGroup]="otpForm" (ngSubmit)="verifyOtp()" class="card" *ngIf="otpSent">
      <input formControlName="otp" placeholder="Enter OTP" />
      <button type="submit" [disabled]="otpForm.invalid">Verify</button>
    </form>

    <p *ngIf="demoOtp">Demo OTP: {{ demoOtp }}</p>
  `
})
export class LoginComponent {
  private fb = inject(FormBuilder);
  private api = inject(ApiService);
  private router = inject(Router);

  otpSent = false;
  demoOtp = '';

  mobileForm = this.fb.group({ mobile: ['', Validators.required] });
  otpForm = this.fb.group({ otp: ['', Validators.required] });

  requestOtp() {
    const mobile = this.mobileForm.value.mobile!;
    this.api.requestOtp(mobile).subscribe((res: any) => {
      this.otpSent = true;
      this.demoOtp = res.otpForDemo;
    });
  }

  verifyOtp() {
    const mobile = this.mobileForm.value.mobile!;
    const otp = this.otpForm.value.otp!;

    this.api.verifyOtp(mobile, otp).subscribe((res: any) => {
      localStorage.setItem('token', res.token);
      localStorage.setItem('role', res.role);
      this.router.navigate(['/dashboard']);
    });
  }
}
