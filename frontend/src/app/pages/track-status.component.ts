import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../services/api.service';
import { CommonModule } from '@angular/common';

@Component({
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <h2>Track Application Status</h2>
    <form [formGroup]="form" (ngSubmit)="track()" class="card">
      <input formControlName="applicationNumber" placeholder="Application Number" />
      <button type="submit" [disabled]="form.invalid">Track</button>
    </form>
    <div *ngIf="status" class="card">
      <p><strong>Status:</strong> {{ status }}</p>
    </div>
  `
})
export class TrackStatusComponent {
  private fb = inject(FormBuilder);
  private api = inject(ApiService);

  status = '';

  form = this.fb.group({
    applicationNumber: ['', Validators.required]
  });

  track() {
    this.api.trackStatus(this.form.value.applicationNumber!).subscribe((res: any) => {
      this.status = res.currentStatus;
    });
  }
}
